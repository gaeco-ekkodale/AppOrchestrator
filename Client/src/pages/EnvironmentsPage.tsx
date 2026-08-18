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
  Container,
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
import {useNetworks} from "@/features/networks/hooks/useNetworks";
import {
  useCreateNetworkMutation,
  useDeleteNetworkMutation,
  useUpdateNetworkMutation,
} from "@/features/networks/hooks/useNetworkMutations";
import {
  getSharedVariablesForNetwork,
  toNetworkEnvironmentVariables,
} from "@/features/networks/sharedVariables";
import {LoadingSpinner} from "@/features/shared/components";
import {EnvEditor, type EnvEntry} from "@/features/stacks/components";
import {getApiErrorMessage} from "@/utils/errorMessages";

// ---------------------------------------------------------------------------
// VersionFilterEditor
// ---------------------------------------------------------------------------
type VersionMode = "all" | "stable" | "custom";

function getVersionMode(suffixes: string[]): VersionMode {
  if (suffixes.length === 0) return "all";
  if (suffixes.length === 1 && suffixes[0] === "") return "stable";
  return "custom";
}

interface VersionFilterEditorProps {
  suffixes: string[];
  onChange: (suffixes: string[]) => void;
}

function VersionFilterEditor({suffixes, onChange}: VersionFilterEditorProps) {
  const [customInput, setCustomInput] = useState("");
  const [mode, setModeState] = useState<VersionMode>(() => getVersionMode(suffixes));

  const setMode = (newMode: VersionMode) => {
    setModeState(newMode);
    if (newMode === "all") onChange([]);
    else if (newMode === "stable") onChange([""]);
    else onChange(suffixes.filter((s) => s !== ""));
  };

  const includesStable = suffixes.includes("");
  const toggleStable = () => {
    includesStable ? onChange(suffixes.filter((s) => s !== "")) : onChange([...suffixes, ""]);
  };

  const addCustom = () => {
    const trimmed = customInput.trim().toLowerCase();
    if (!trimmed || suffixes.includes(trimmed)) return;
    onChange([...suffixes, trimmed]);
    setCustomInput("");
  };
  const removeCustom = (s: string) => onChange(suffixes.filter((x) => x !== s));

  const modeDefs: {value: VersionMode; label: string; color: "primary" | "success" | "info"}[] = [
    {value: "all", label: "Alles erlaubt", color: "primary"},
    {value: "stable", label: "Nur Stabile", color: "success"},
    {value: "custom", label: "Benutzerdefiniert", color: "info"},
  ];

  return (
    <Box>
      <Box sx={{display: "flex", gap: 1, mb: 2, flexWrap: "wrap"}}>
        {modeDefs.map(({value, label, color}) => (
          <Chip
            key={value}
            label={label}
            clickable
            color={mode === value ? color : "default"}
            variant={mode === value ? "filled" : "outlined"}
            onClick={() => setMode(value)}
          />
        ))}
      </Box>

      {mode === "all" && (
        <Typography variant="body2" color="text.secondary">
          Updates werden für alle Versionen angezeigt – egal ob fertig oder Vorabversion.
        </Typography>
      )}

      {mode === "stable" && (
        <Typography variant="body2" color="text.secondary">
          Nur fertige Versionen ohne Zusatz werden angezeigt (z.&nbsp;B. <code>1.0.0</code>).
          Vorabversionen wie <code>1.0.0-beta</code> werden ausgeblendet.
        </Typography>
      )}

      {mode === "custom" && (
        <>
          <Typography variant="body2" color="text.secondary" sx={{mb: 1.5}}>
            Wähle, welche Versionszusätze erlaubt sind. Du kannst auch stabile Versionen mit
            einschließen.
          </Typography>
          <Box sx={{display: "flex", flexWrap: "wrap", gap: 1, mb: 1.5, alignItems: "center"}}>
            <Chip
              label="Stabile einschließen"
              size="small"
              color={includesStable ? "success" : "default"}
              variant={includesStable ? "filled" : "outlined"}
              clickable
              onClick={toggleStable}
            />
            {suffixes
              .filter((s) => s !== "")
              .map((s) => (
                <Chip
                  key={s}
                  label={`-${s}`}
                  size="small"
                  color="info"
                  variant="outlined"
                  onDelete={() => removeCustom(s)}
                />
              ))}
            {suffixes.filter((s) => s !== "").length === 0 && !includesStable && (
              <Typography variant="body2" color="text.secondary" sx={{fontStyle: "italic"}}>
                Noch keine Einschränkung gewählt …
              </Typography>
            )}
          </Box>
          <Box sx={{display: "flex", gap: 1}}>
            <TextField
              size="small"
              placeholder="Eigenen Zusatz eingeben, z. B. beta"
              value={customInput}
              onChange={(e) => setCustomInput(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  addCustom();
                }
              }}
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <Typography variant="body2" color="text.secondary">
                        -
                      </Typography>
                    </InputAdornment>
                  ),
                },
              }}
              sx={{flexGrow: 1, maxWidth: 300}}
            />
            <Button
              variant="outlined"
              size="small"
              startIcon={<AddIcon />}
              onClick={addCustom}
              disabled={!customInput.trim() || suffixes.includes(customInput.trim().toLowerCase())}
            >
              Hinzufügen
            </Button>
          </Box>
        </>
      )}
    </Box>
  );
}

// ---------------------------------------------------------------------------
// EnvironmentsPage
// ---------------------------------------------------------------------------
function EnvironmentsPage() {
  const {networks, isLoading, error} = useNetworks();

  const [createOpen, setCreateOpen] = useState(false);
  const [newName, setNewName] = useState("");
  const [createSharedVarEntries, setCreateSharedVarEntries] = useState<EnvEntry[]>([]);
  const [createSuffixes, setCreateSuffixes] = useState<string[]>([]);
  const [deleteTarget, setDeleteTarget] = useState<string | null>(null);
  const [selectedEnvironment, setSelectedEnvironment] = useState<string | null>(null);
  const [sharedVarEntries, setSharedVarEntries] = useState<EnvEntry[]>([]);
  const [suffixes, setSuffixes] = useState<string[]>([]);

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
    setCreateSharedVarEntries([]);
    setCreateSuffixes([]);
  };

  const handleCreate = () => {
    createMutation.mutate(
      {
        name: newName.trim(),
        environmentVariables: toNetworkEnvironmentVariables(createSharedVarEntries),
        allowedVersionSuffixes: createSuffixes,
      },
      {
        onSuccess: () => {
          setCreateSharedVarEntries([]);
          setCreateSuffixes([]);
        },
      },
    );
  };

  const openEnvironmentDialog = (name?: string) => {
    const networkName = (name ?? "").trim();
    if (!networkName) return;
    const selectedNetwork = networks.find((n) => n.name === networkName);
    setSelectedEnvironment(networkName);
    setSharedVarEntries(getSharedVariablesForNetwork(selectedNetwork));
    setSuffixes(selectedNetwork?.allowedVersionSuffixes ?? []);
  };

  const closeEnvironmentDialog = () => {
    setSelectedEnvironment(null);
    setSharedVarEntries([]);
    setSuffixes([]);
  };

  const saveSharedVariables = () => {
    if (!selectedEnvironment) return;
    updateMutation.mutate({
      name: selectedEnvironment,
      environmentVariables: toNetworkEnvironmentVariables(sharedVarEntries),
      allowedVersionSuffixes: suffixes,
    });
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
    <Container maxWidth="xl" sx={{py: 4}}>
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

      {networks.length === 0 ? (
        <Paper sx={{p: 6, textAlign: "center"}}>
          <PublicIcon sx={{fontSize: 64, color: "text.secondary", mb: 2}} />
          <Typography variant="h6" color="text.secondary" gutterBottom>
            Keine Environments vorhanden
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{mb: 3}}>
            Stacks im gleichen Environment können miteinander kommunizieren und gemeinsame Variablen
            nutzen.
          </Typography>
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
            Erstes Environment anlegen
          </Button>
        </Paper>
      ) : (
        <Card sx={{borderRadius: 2}}>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell sx={{fontWeight: "bold"}}>Name</TableCell>
                  <TableCell sx={{fontWeight: "bold"}}>Stacks</TableCell>
                  <TableCell sx={{fontWeight: "bold", whiteSpace: "nowrap"}}>
                    Geteilte Vars
                  </TableCell>
                  <TableCell sx={{fontWeight: "bold", whiteSpace: "nowrap"}}>
                    Versionierung
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
                    <TableCell>
                      <Box sx={{display: "flex", alignItems: "center", gap: 1}}>
                        <PublicIcon fontSize="small" color="primary" />
                        <Typography fontWeight="medium" noWrap>
                          {env.name}
                        </Typography>
                      </Box>
                    </TableCell>

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
                      {(() => {
                        const suffixList = env.allowedVersionSuffixes ?? [];
                        const vMode = getVersionMode(suffixList);
                        if (vMode === "all") {
                          return (
                            <Typography variant="body2" color="text.secondary">
                              Alle
                            </Typography>
                          );
                        }
                        if (vMode === "stable") {
                          return (
                            <Chip
                              label="Nur Stabile"
                              size="small"
                              color="success"
                              variant="outlined"
                            />
                          );
                        }
                        return (
                          <Box sx={{display: "flex", flexWrap: "wrap", gap: 0.5}}>
                            {suffixList.map((s) => (
                              <Chip
                                key={s === "" ? "__stable" : s}
                                label={s === "" ? "Stabile" : `-${s}`}
                                size="small"
                                color={s === "" ? "success" : "info"}
                                variant="outlined"
                              />
                            ))}
                          </Box>
                        );
                      })()}
                    </TableCell>

                    <TableCell>
                      <Typography variant="body2" color="text.secondary">
                        {formatDate(env.createdAt)}
                      </Typography>
                    </TableCell>

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

      {/* Create Dialog */}
      <Dialog open={createOpen} onClose={handleCreateClose} maxWidth="sm" fullWidth>
        <DialogTitle>Neues Environment anlegen</DialogTitle>
        <Divider />
        <DialogContent sx={{pt: 2}}>
          <Typography variant="body2" color="text.secondary" sx={{mb: 2}}>
            Stacks im gleichen Environment können miteinander reden und gemeinsame Variablen nutzen.
          </Typography>
          <TextField
            label="Name"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            fullWidth
            autoFocus
            required
            helperText="Eindeutiger Name, z. B. production oder staging"
            onKeyDown={(e) => {
              if (e.key === "Enter" && newName.trim()) handleCreate();
            }}
            sx={{mb: 3}}
          />

          <Divider sx={{mb: 2}} />

          <Typography variant="subtitle2" fontWeight="bold" sx={{mb: 0.5}}>
            Geteilte Variablen{" "}
            <Typography component="span" variant="caption" color="text.secondary">
              (optional)
            </Typography>
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{mb: 2}}>
            Geteilte Variablen werden an alle Stacks in diesem Environment vererbt.
          </Typography>
          <EnvEditor
            entries={createSharedVarEntries}
            onChange={setCreateSharedVarEntries}
            emptyLabel="Noch keine Variablen. Klicke auf + um eine hinzuzufügen."
          />

          <Divider sx={{my: 2}} />

          <Typography variant="subtitle2" fontWeight="bold" sx={{mb: 0.5}}>
            Welche Versionen sind erlaubt?{" "}
            <Typography component="span" variant="caption" color="text.secondary">
              (optional)
            </Typography>
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{mb: 2}}>
            Legt fest, welche Updates für Stacks in diesem Environment angezeigt werden.
          </Typography>
          <VersionFilterEditor suffixes={createSuffixes} onChange={setCreateSuffixes} />
        </DialogContent>
        <DialogActions sx={{px: 3, pb: 2}}>
          <Button onClick={handleCreateClose} disabled={createMutation.isPending}>
            Abbrechen
          </Button>
          <Button
            variant="contained"
            onClick={handleCreate}
            disabled={!newName.trim() || createMutation.isPending}
            startIcon={
              createMutation.isPending ? (
                <CircularProgress size={16} color="inherit" />
              ) : (
                <AddIcon />
              )
            }
          >
            Anlegen
          </Button>
        </DialogActions>
      </Dialog>

      {/* Environment Settings Dialog */}
      <Dialog
        open={selectedEnvironment !== null}
        onClose={closeEnvironmentDialog}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Environment bearbeiten – {selectedEnvironment ?? "Environment"}</DialogTitle>
        <Divider />
        <DialogContent sx={{pt: 2}}>
          {/* Shared Variables */}
          <Typography variant="subtitle2" fontWeight="bold" sx={{mb: 0.5}}>
            Geteilte Variablen
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{mb: 2}}>
            Geteilte Variablen werden an alle Stacks in diesem Environment vererbt.
          </Typography>
          <EnvEditor
            entries={sharedVarEntries}
            onChange={setSharedVarEntries}
            emptyLabel="Noch keine geteilten Variablen. Klicke auf + um eine hinzuzufügen."
          />

          <Divider sx={{my: 3}} />

          {/* Version filter */}
          <Typography variant="subtitle2" fontWeight="bold" sx={{mb: 0.5}}>
            Welche Versionen sind erlaubt?
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{mb: 2}}>
            Legt fest, welche Updates für Stacks in diesem Environment angezeigt werden.
          </Typography>
          <VersionFilterEditor suffixes={suffixes} onChange={setSuffixes} />
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

      {/* Delete Confirmation */}
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
    </Container>
  );
}

export default EnvironmentsPage;
