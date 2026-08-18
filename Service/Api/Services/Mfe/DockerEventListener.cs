// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Api.Services._Interfaces.Mfe;
using AppOrchestrator.Domain.Repositories;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Collections.Concurrent;

namespace AppOrchestrator.Api.Services.Mfe;

/// <summary>
/// Background service that watches the Docker event stream and keeps plugin hosts
/// informed about the MFE containers running in their network.
///
/// Plugin hosts are discovered dynamically from container labels in each network:
/// <c>orchestrator.host=true</c> and <c>orchestrator.apiKey=&lt;key&gt;</c>.
/// MFE plugin metadata is collected from the known <c>app.mfe.*</c> labels defined in <see cref="MfeLabels"/>.
///
/// On every relevant lifecycle event (container <c>start</c> / <c>die</c>) a full snapshot
/// of all MFE containers in the affected network is pushed to the registered host so it can
/// reconcile its state.
/// The <c>die</c> event is used for stopped containers (not <c>stop</c>) because <c>die</c>
/// fires only after the process has actually exited, giving an accurate running/stopped state.
///
/// Startup sequence:
/// <list type="number">
///   <item>Find all host-labeled containers.</item>
///   <item>For each host network call <see cref="IMfeSyncService.SyncNetworkAsync"/>.</item>
/// </list>
///
/// When the Docker socket connection drops the listener reconnects with exponential
/// backoff (2 s to 4 s ... capped at 60 s).
/// </summary>
public class DockerEventListener(
    IDockerClient dockerClient,
    IMfeSyncService mfeSyncService,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DockerEventListener> logger)
    : BackgroundService, IDockerEventListener
{
    /// <summary>
    /// Time a sync waits before it reads the container state. A <c>docker compose up</c> emits
    /// one event per container, and every event would otherwise push its own full snapshot.
    /// </summary>
    private static readonly TimeSpan SyncDebounce = TimeSpan.FromMilliseconds(500);

    private readonly ConcurrentDictionary<string, NetworkSyncGate> _syncGates =
        new(StringComparer.OrdinalIgnoreCase);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartupDiscoveryAsync(stoppingToken);

        var delay = TimeSpan.FromSeconds(2);
        while (!stoppingToken.IsCancellationRequested)
        {
            var connectedAt = DateTimeOffset.UtcNow;
            try
            {
                logger.LogInformation("MfeEventListener connecting to Docker event stream.");
                await ListenAsync(stoppingToken);
                logger.LogWarning("Docker event stream ended. Reconnecting in {Delay}s.", delay.TotalSeconds);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Docker event stream error. Reconnecting in {Delay}s.", delay.TotalSeconds);
            }

            delay = (DateTimeOffset.UtcNow - connectedAt).TotalSeconds > 30
                ? TimeSpan.FromSeconds(2)
                : TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 60));

            await Task.Delay(delay, stoppingToken);
        }

        logger.LogInformation("MfeEventListener stopped.");
    }

    // -----------------------------------------------------------------------
    // Startup discovery
    // -----------------------------------------------------------------------

    private async Task StartupDiscoveryAsync(CancellationToken ct)
    {
        try
        {
            var networks = await ListSyncTargetNetworksAsync(ct);
            if (networks.Count == 0)
            {
                logger.LogDebug("Startup: no sync target networks found (registered network + host label required). MFE sync skipped.");
                return;
            }

            logger.LogInformation("Startup: syncing {Count} registered host network(s).", networks.Count);
            foreach (var networkName in networks)
                await SyncNetworkCoalescedAsync(networkName, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MfeEventListener startup discovery failed - continuing.");
        }
    }

    // -----------------------------------------------------------------------
    // Event stream
    // -----------------------------------------------------------------------

    private async Task ListenAsync(CancellationToken ct)
    {
        await dockerClient.System.MonitorEventsAsync(
            new ContainerEventsParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["type"] = new Dictionary<string, bool> { ["container"] = true },
                    // react to all lifecycle events, create/start/stop/die etc. - filtering is done in HandleEvent to avoid missing relevant events
                    // react also to delete/remove events to trigger syncs when containers are removed outside of the orchestrator's control (e.g. via CLI)
                    ["event"] = new Dictionary<string, bool> { ["start"] = true, ["stop"] = true, ["die"] = true, ["destroy"] = true }
                }
            },
            new Progress<Message>(HandleEvent),
            ct);
    }

    private void HandleEvent(Message msg)
    {
        var status = string.IsNullOrEmpty(msg.Action) ? msg.Status : msg.Action;
        var attrs = msg.Actor?.Attributes;
        var containerId = msg.Actor?.ID;

        if (string.IsNullOrEmpty(status) || attrs is null || string.IsNullOrEmpty(containerId))
            return;

        var looksLikePlugin = attrs.Keys.Any(MfeLabels.IsPluginLabel);
        var looksLikeHost = attrs.TryGetValue(MfeLabels.HostEnabled, out var hostEnabled)
            && string.Equals(hostEnabled, "true", StringComparison.OrdinalIgnoreCase);

        if (!looksLikePlugin && !looksLikeHost)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                // Inspect the container directly to find which networks it belongs to.
                // For "destroy" events (docker rm) the container may already be gone — in that
                // case we fall back to syncing all host networks so the host still receives
                // an up-to-date snapshot and can remove the plugin entry.
                ContainerInspectResponse? inspect = null;
                try
                {
                    inspect = await dockerClient.Containers.InspectContainerAsync(containerId, CancellationToken.None);
                }
                catch (DockerContainerNotFoundException)
                {
                    logger.LogDebug(
                        "Event [{Status}]: container [{Id}] is already gone - syncing all registered networks.",
                        status, containerId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Event [{Status}]: could not inspect container [{Id}] - syncing all registered networks as fallback.",
                        status, containerId);
                }

                IEnumerable<string> matchedNetworks;
                if (inspect is not null)
                {
                    var syncTargetNetworks = await ListSyncTargetNetworksAsync(CancellationToken.None);
                    if (syncTargetNetworks.Count == 0)
                        return;

                    var containerNetworks = inspect.NetworkSettings?.Networks?.Keys
                                           ?? Enumerable.Empty<string>();
                    matchedNetworks = containerNetworks
                        .Where(syncTargetNetworks.Contains)
                        .ToList();

                    if (!matchedNetworks.Any())
                    {
                        logger.LogDebug(
                            "Event [{Status}] for container [{Id}]: no registered network found - MFE event ignored.",
                            status, containerId);
                        return;
                    }
                }
                else
                {
                    // Container is gone: sync every registered network — the sync service
                    // will produce an accurate snapshot without the removed container.
                    matchedNetworks = await ListSyncTargetNetworksAsync(CancellationToken.None);
                }

                foreach (var networkName in matchedNetworks)
                {
                    logger.LogInformation(
                        "MFE container [{Status}] in network [{Network}] - syncing.", status, networkName);
                    await SyncNetworkCoalescedAsync(networkName, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Error handling Docker event [{Status}] for container [{Id}].", status, containerId);
            }
        });
    }

    // -----------------------------------------------------------------------
    // Sync scheduling
    // -----------------------------------------------------------------------

    /// <summary>
    /// Pushes a snapshot for the given network, serialized and coalesced per network.
    /// Container events arrive in bursts and are handled on independent threads; without this
    /// gate several full snapshots would be applied to the same plugin host concurrently and
    /// race each other while inserting the same plugins.
    /// </summary>
    private async Task SyncNetworkCoalescedAsync(string networkName, CancellationToken ct)
    {
        var gate = _syncGates.GetOrAdd(networkName, _ => new NetworkSyncGate());

        // At most one sync may wait per network: a queued sync reads the full container state
        // when it runs, so it already covers every event that arrives until then.
        if (Interlocked.Increment(ref gate.Queued) > 1)
        {
            Interlocked.Decrement(ref gate.Queued);
            logger.LogDebug(
                "MFE sync for network [{Network}] is already queued - event coalesced.", networkName);
            return;
        }

        var queueSlotReleased = false;
        try
        {
            await gate.Mutex.WaitAsync(ct);
            try
            {
                await Task.Delay(SyncDebounce, ct);

                // Release the queue slot before syncing: events arriving from here on describe
                // state this run may not see yet and must schedule a follow-up sync.
                Interlocked.Decrement(ref gate.Queued);
                queueSlotReleased = true;

                await mfeSyncService.SyncNetworkAsync(networkName, ct);
            }
            finally
            {
                gate.Mutex.Release();
            }
        }
        finally
        {
            // Never leave the slot taken - the network would stop syncing for good.
            if (!queueSlotReleased)
                Interlocked.Decrement(ref gate.Queued);
        }
    }

    private sealed class NetworkSyncGate
    {
        public readonly SemaphoreSlim Mutex = new(1, 1);
        public int Queued;
    }

    private async Task<HashSet<string>> ListHostNetworksAsync(CancellationToken ct)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["label"] = new Dictionary<string, bool>
                    {
                        [$"{MfeLabels.HostEnabled}=true"] = true
                    }
                }
            },
            ct);

        return containers
            .SelectMany(c => c.NetworkSettings?.Networks?.Keys ?? Enumerable.Empty<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> ListRegisteredNetworksAsync(CancellationToken ct)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var networkRepository = scope.ServiceProvider.GetRequiredService<INetworkRepository>();
        var networks = await networkRepository.ListAsync(ct);

        return networks
            .Select(n => n.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> ListSyncTargetNetworksAsync(CancellationToken ct)
    {
        var hostNetworks = await ListHostNetworksAsync(ct);
        if (hostNetworks.Count == 0)
            return [];

        var registeredNetworks = await ListRegisteredNetworksAsync(ct);
        if (registeredNetworks.Count == 0)
            return [];

        hostNetworks.IntersectWith(registeredNetworks);
        return hostNetworks;
    }

}