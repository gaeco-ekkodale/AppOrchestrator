// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useState} from "react";
import {
  Alert,
  Box,
  Button,
  Card,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Divider,
  IconButton,
  InputAdornment,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import PublicIcon from "@mui/icons-material/Public";
import LayersIcon from "@mui/icons-material/Layers";
import {useNetworks} from "../hooks/useNetworks";
import {
  useCreateNetworkMutation,
  useDeleteNetworkMutation,
  useUpdateNetworkMutation,
} from "../hooks/useNetworkMutations";
import {getSharedVariablesForNetwork, toNetworkEnvironmentVariables} from "../sharedVariables";
import {LoadingSpinner} from "@/features/shared/components";
import {EnvEditor, type EnvEntry} from "@/features/stacks/components";
import {getApiErrorMessage} from "@/utils/errorMessages";

export function EnvironmentsList() {
  const {networks, isLoading, error} = useNetworks();

  // Create dialog (network)
  const [createOpen, setCreateOpen] = useState(false);
  const [newName, setNewName] = useState("");

  // Delete dialog (network)
  const [deleteTarget, setDeleteTarget] = useState<string | null>(null);

  // Shared vars dialog
  const [selectedEnvironment, setSelectedEnvironment] = useState<string | null>(null);
  const [sharedVarEntries, setSharedVarEntries] = useState<EnvEntry[]>([]);
  const [suffixes, setSuffixes] = useState<string[]>([]);
  const [suffixInput, setSuffixInput] = useState("");

  const createMutation = useCreateNetworkMutation(() => {
    setCreateOpen(false);
    setNewName("");
  });

  const deleteMutation = useDeleteNetworkMutation(() => {
    setDeleteTarget(null);
  });

  const updateMutation = useUpdateNetworkMutation(() => {
    closeEnvironmentDialog();
  });

  const handleCreateClose = () => {
    setCreateOpen(false);
    setNewName("");
  };

  const openEnvironmentDialog = (name?: string) => {
    const networkName = (name ?? "").trim();
    if (!networkName) return;
    const selectedNetwork = networks.find((n) => n.name === networkName);
    setSelectedEnvironment(networkName);
    setSharedVarEntries(getSharedVariablesForNetwork(selectedNetwork));
    setSuffixes(selectedNetwork?.allowedVersionSuffixes ?? []);
    setSuffixInput("");
  };

  const closeEnvironmentDialog = () => {
    setSelectedEnvironment(null);
    setSharedVarEntries([]);
    setSuffixes([]);
    setSuffixInput("");
  };

  const saveSharedVariables = () => {
    if (!selectedEnvironment) return;
    updateMutation.mutate({
      name: selectedEnvironment,
      environmentVariables: toNetworkEnvironmentVariables(sharedVarEntries),
      allowedVersionSuffixes: suffixes,
    });
  };

  const addSuffix = () => {
    const trimmed = suffixInput.trim().toLowerCase();
    if (!trimmed || suffixes.includes(trimmed)) return;
    setSuffixes((prev) => [...prev, trimmed]);
    setSuffixInput("");
  };

  const removeSuffix = (suffix: string) => {
    setSuffixes((prev) => prev.filter((s) => s !== suffix));
  };

  const formatDate = (iso?: string) => {
    if (!iso) return "–";
    return new Date(iso).toLocaleDateString("de-DE", {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  };

  if (isLoading) return <LoadingSpinner />;

  return (
    <>
      <Paper sx={{px: 3, py: 2, mb: 3, borderRadius: 2}}>
        <Box sx={{display: "flex", alignItems: "center", gap: 1.5}}>
          <PublicIcon color="primary" />
          <Typography variant="h5" fontWeight="bold" sx={{flexGrow: 1}}>
            Environments
          </Typography>
          <Chip label={networks.length} size="small" color="primary" sx={{mr: 1}} />
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            size="small"
            onClick={() => setCreateOpen(true)}
          >
            Neues Environment
          </Button>
        </Box>
      </Paper>

      {error && (
        <Alert severity="error" sx={{mb: 3}}>
          {getApiErrorMessage(error, "Fehler beim Laden der Environments")}
        </Alert>
      )}

      {/* ── Empty state ─────────────────────────────────────────────── */}
      {networks.length === 0 ? (
        <Paper sx={{p: 6, textAlign: "center"}}>
          <PublicIcon sx={{fontSize: 64, color: "text.secondary", mb: 2}} />
          <Typography variant="h6" color="text.secondary" gutterBottom>
            Keine Environments vorhanden
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{mb: 3}}>
            Environments (Docker-Netzwerke) ermöglichen die Vernetzung mehrerer Stacks
            untereinander.
          </Typography>
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
            Erstes Environment erstellen
          </Button>
        </Paper>
      ) : (
        /* ── Environment list ──────────────────────────────────────── */
        <Card sx={{borderRadius: 2}}>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell sx={{fontWeight: "bold"}}>Name</TableCell>
                  <TableCell sx={{fontWeight: "bold"}}>Stacks</TableCell>
                  <TableCell sx={{fontWeight: "bold", whiteSpace: "nowrap"}}>Shared Vars</TableCell>
                  <TableCell sx={{fontWeight: "bold", whiteSpace: "nowrap"}}>
                    Versions-Filter
                  </TableCell>
                  <TableCell sx={{fontWeight: "bold", whiteSpace: "nowrap"}}>Erstellt am</TableCell>
                  <TableCell align="right" sx={{width: 56}} />
                </TableRow>
              </TableHead>
              <TableBody>
                {networks.map((env) => (
                  <TableRow
                    key={env.name}
                    hover
                    onClick={() => openEnvironmentDialog(env.name)}
                    sx={{cursor: "pointer"}}
                  >
                    {/* Name */}
                    <TableCell>
                      <Box sx={{display: "flex", alignItems: "center", gap: 1}}>
                        <PublicIcon fontSize="small" color="primary" />
                        <Typography fontWeight="medium" noWrap>
                          {env.name}
                        </Typography>
                      </Box>
                    </TableCell>

                    {/* Stacks */}
                    <TableCell>
                      {env.stacks && env.stacks.length > 0 ? (
                        <Box sx={{display: "flex", flexWrap: "wrap", gap: 0.5}}>
                          {env.stacks.map((s) => (
                            <Chip
                              key={s.dockerProjectName}
                              label={s.stackName ?? s.dockerProjectName}
                              size="small"
                              icon={<LayersIcon />}
                              variant="outlined"
                              sx={{maxWidth: "100%"}}
                            />
                          ))}
                        </Box>
                      ) : (
                        <Typography variant="body2" color="text.secondary">
                          Keine Stacks
                        </Typography>
                      )}
                    </TableCell>

                    <TableCell>
                      <Chip
                        label={getSharedVariablesForNetwork(env).length}
                        size="small"
                        variant="outlined"
                      />
                    </TableCell>

                    <TableCell>
                      {env.allowedVersionSuffixes && env.allowedVersionSuffixes.length > 0 ? (
                        <Box sx={{display: "flex", flexWrap: "wrap", gap: 0.5}}>
                          {env.allowedVersionSuffixes.map((s) => (
                            <Chip
                              key={s}
                              label={`-${s}`}
                              size="small"
                              color="info"
                              variant="outlined"
                            />
                          ))}
                        </Box>
                      ) : (
                        <Typography variant="body2" color="text.secondary">
                          Alle
                        </Typography>
                      )}
                    </TableCell>

                    {/* Created */}
                    <TableCell>
                      <Typography variant="body2" color="text.secondary">
                        {formatDate(env.createdAt)}
                      </Typography>
                    </TableCell>

                    {/* Actions */}
                    <TableCell align="right">
                      <Tooltip
                        title={
                          env.stacks && env.stacks.length > 0
                            ? "Environment kann nicht gelöscht werden – Stacks sind zugewiesen"
                            : "Löschen"
                        }
                      >
                        <span>
                          <IconButton
                            size="small"
                            color="error"
                            onClick={(e) => {
                              e.stopPropagation();
                              setDeleteTarget(env.name ?? "");
                            }}
                            disabled={
                              deleteMutation.isPending ||
                              (env.stacks != null && env.stacks.length > 0)
                            }
                          >
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </span>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Card>
      )}

      {/* ── Create Dialog ──────────────────────────────────────────── */}
      <Dialog open={createOpen} onClose={handleCreateClose} maxWidth="xs" fullWidth>
        <DialogTitle>Neues Environment erstellen</DialogTitle>
        <Divider />
        <DialogContent sx={{pt: 2}}>
          <Typography variant="body2" color="text.secondary" sx={{mb: 2}}>
            Ein Environment entspricht einem Docker-Netzwerk. Stacks, die demselben Environment
            zugeordnet sind, können miteinander kommunizieren.
          </Typography>
          <TextField
            label="Name"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            fullWidth
            autoFocus
            required
            helperText="Eindeutiger Name des Environments (z. B. production)"
            onKeyDown={(e) => {
              if (e.key === "Enter" && newName.trim()) {
                createMutation.mutate({name: newName.trim()});
              }
            }}
          />
        </DialogContent>
        <DialogActions sx={{px: 3, pb: 2}}>
          <Button onClick={handleCreateClose} disabled={createMutation.isPending}>
            Abbrechen
          </Button>
          <Button
            variant="contained"
            onClick={() => createMutation.mutate({name: newName.trim()})}
            disabled={!newName.trim() || createMutation.isPending}
            startIcon={
              createMutation.isPending ? (
                <CircularProgress size={16} color="inherit" />
              ) : (
                <AddIcon />
              )
            }
          >
            Erstellen
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Environment Settings Dialog ──────────────────────────────── */}
      <Dialog
        open={selectedEnvironment !== null}
        onClose={closeEnvironmentDialog}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>
          Environment konfigurieren – {selectedEnvironment ?? "Environment"}
        </DialogTitle>
        <Divider />
        <DialogContent sx={{pt: 2}}>
          {/* ── Shared Variables ─────────────────────────────────── */}
          <Typography variant="subtitle2" fontWeight="bold" sx={{mb: 0.5}}>
            Shared Variables
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{mb: 2}}>
            Diese Variablen gelten für alle Stacks im gleichen Environment. Stack-spezifische
            Variablen überschreiben Shared Variables mit gleichem Key.
          </Typography>
          <EnvEditor
            entries={sharedVarEntries}
            onChange={setSharedVarEntries}
            emptyLabel="Keine Shared Variables definiert. Klicke auf + um eine hinzuzufügen."
          />

          <Divider sx={{my: 3}} />

          {/* ── Allowed Version Suffixes ──────────────────────────── */}
          <Typography variant="subtitle2" fontWeight="bold" sx={{mb: 0.5}}>
            Erlaubte Versions-Suffixe
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{mb: 2}}>
            Stacks in diesem Environment zeigen nur Updates für Versionen an, deren Suffix (der
            Teil nach dem ersten <code>-</code>) mit einem der eingetragenen Suffixe gleicht (z. B.
            erlaubt <code>test</code> die Version <code>1.0.0-test</code>, aber nicht{" "}
            <code>1.0.0-local-test</code>). Leer bedeutet: alle Versionen erlaubt.
          </Typography>

          <Box sx={{display: "flex", gap: 1, mb: 1.5}}>
            <TextField
              size="small"
              placeholder="Suffix eingeben, z. B. test"
              value={suffixInput}
              onChange={(e) => setSuffixInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  addSuffix();
                }
              }}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Typography variant="body2" color="text.secondary">
                      -
                    </Typography>
                  </InputAdornment>
                ),
              }}
              sx={{flexGrow: 1, maxWidth: 300}}
            />
            <Button
              variant="outlined"
              size="small"
              startIcon={<AddIcon />}
              onClick={addSuffix}
              disabled={!suffixInput.trim() || suffixes.includes(suffixInput.trim().toLowerCase())}
            >
              Hinzufügen
            </Button>
          </Box>

          <Box sx={{mb: 1.5}}>
            <Chip
              label="Stabile Versionen (ohne Suffix)"
              color={suffixes.includes("") ? "success" : "default"}
              variant={suffixes.includes("") ? "filled" : "outlined"}
              size="small"
              onClick={() =>
                suffixes.includes("") ? removeSuffix("") : setSuffixes((prev) => [...prev, ""])
              }
            />
          </Box>

          <Box sx={{display: "flex", flexWrap: "wrap", gap: 1, minHeight: 32}}>
            {suffixes.length === 0 ? (
              <Typography variant="body2" color="text.secondary">
                Keine Einschränkung – alle Versionen erlaubt.
              </Typography>
            ) : (
              suffixes.map((s) => (
                <Chip
                  key={s === "" ? "__stable" : s}
                  label={s === "" ? "Stabile Versionen" : `-${s}`}
                  color={s === "" ? "success" : "info"}
                  variant="outlined"
                  size="small"
                  onDelete={() => removeSuffix(s)}
                />
              ))
            )}
          </Box>
        </DialogContent>
        <DialogActions sx={{px: 3, pb: 2}}>
          <Button onClick={closeEnvironmentDialog} disabled={updateMutation.isPending}>
            Abbrechen
          </Button>
          <Button
            variant="contained"
            onClick={saveSharedVariables}
            disabled={updateMutation.isPending}
          >
            Speichern
          </Button>
        </DialogActions>
      </Dialog>

      {/* ── Delete Confirmation ────────────────────────────────────── */}
      <Dialog
        open={deleteTarget !== null}
        onClose={() => setDeleteTarget(null)}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle>Environment löschen?</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Das Environment <strong>{deleteTarget}</strong> wird unwiderruflich gelöscht. Das
            zugehörige Docker-Netzwerk wird ebenfalls entfernt.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteTarget(null)} disabled={deleteMutation.isPending}>
            Abbrechen
          </Button>
          <Button
            variant="contained"
            color="error"
            onClick={() => deleteTarget && deleteMutation.mutate(deleteTarget)}
            disabled={deleteMutation.isPending}
            startIcon={
              deleteMutation.isPending ? (
                <CircularProgress size={16} color="inherit" />
              ) : (
                <DeleteIcon />
              )
            }
          >
            Löschen
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
