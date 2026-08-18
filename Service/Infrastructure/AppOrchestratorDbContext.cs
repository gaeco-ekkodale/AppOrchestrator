// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AppOrchestrator.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AppOrchestrator.Infrastructure;

/// <summary>
/// Entity Framework Core context for AppOrchestrator persistence.
/// Defines registry, stack, and container-registry entity sets and relationships.
/// </summary>
public class AppOrchestratorDbContext : DbContext
{
    /// <summary>
    /// DbSet for application registries.
    /// </summary>
    public DbSet<AppRegistry> AppRegistries { get; set; }

    /// <summary>
    /// DbSet for deployed stacks (base type – includes both RegistryStack and CustomStack via TPH).
    /// </summary>
    public DbSet<Stack> Stacks { get; set; }

    /// <summary>
    /// Convenience DbSet for registry-managed stacks.
    /// </summary>
    public DbSet<RegistryStack> RegistryStacks { get; set; }

    /// <summary>
    /// Convenience DbSet for custom-compose stacks.
    /// </summary>
    public DbSet<CustomStack> CustomStacks { get; set; }

    /// <summary>
    /// DbSet for container registries.
    /// </summary>
    public DbSet<ContainerRegistry> ContainerRegistries { get; set; }

    /// <summary>
    /// DbSet for Docker networks.
    /// </summary>
    public DbSet<Network> Networks { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppOrchestratorDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public AppOrchestratorDbContext(DbContextOptions<AppOrchestratorDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the entity relationships and database schema.
    /// Sets up keys and foreign-key relationships.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for the context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Define primary keys.
        modelBuilder.Entity<AppRegistry>()
            .HasKey(r => new { r.Id });

        modelBuilder.Entity<AppRegistry>()
            .HasIndex(r => r.BaseUrl)
            .IsUnique();

        modelBuilder.Entity<Stack>()
            .HasKey(s => new { s.Id });

        modelBuilder.Entity<Stack>()
            .HasIndex(s => s.DockerProjectName)
            .IsUnique();

        // TPH discriminator for Stack hierarchy.
        modelBuilder.Entity<Stack>()
            .HasDiscriminator<string>("StackType")
            .HasValue<RegistryStack>("Registry")
            .HasValue<CustomStack>("Custom");

        modelBuilder.Entity<ContainerRegistry>()
            .HasKey(c => new { c.Id });

        modelBuilder.Entity<ContainerRegistry>()
            .HasIndex(c => c.ServerAddress)
            .IsUnique();

        modelBuilder.Entity<Network>()
            .HasKey(n => n.Name);

        // Network owns a collection of shared EnvironmentVariables (stored in a separate table).
        // Composite PK ensures variable names are unique per network.
        modelBuilder.Entity<Network>()
            .OwnsMany(n => n.EnvironmentVariables, ev =>
            {
                ev.WithOwner().HasForeignKey("NetworkName");
                ev.HasKey("NetworkName", nameof(EnvironmentVariable.Name));
                ev.Property(e => e.Name).HasMaxLength(100).IsRequired();
                ev.Property(e => e.Value).HasMaxLength(500).IsRequired();
            });

        // Network owns a collection of AllowedVersionSuffixes for update-channel filtering.
        modelBuilder.Entity<Network>()
            .OwnsMany(n => n.AllowedVersionSuffixes, s =>
            {
                s.WithOwner().HasForeignKey("NetworkName");
                s.HasKey("NetworkName", nameof(AllowedVersionSuffix.Suffix));
                s.Property(e => e.Suffix).HasMaxLength(100).IsRequired();
            });

        // Network to Stacks relationship (1:N).
        modelBuilder.Entity<Network>()
            .HasMany(n => n.Stacks)
            .WithOne(s => s.Network!)
            .HasForeignKey(s => s.NetworkName)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // AppRegistry to RegistryStacks relationship.
        modelBuilder.Entity<AppRegistry>()
            .HasMany(e => e.Stacks)
            .WithOne(e => e.AppRegistry!)
            .HasForeignKey(e => e.AppRegistryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        base.OnModelCreating(modelBuilder);
    }
}