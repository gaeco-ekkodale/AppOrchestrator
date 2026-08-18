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
import {Link} from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Chip,
  Container,
  InputAdornment,
  MenuItem,
  Paper,
  TextField,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import LayersIcon from "@mui/icons-material/Layers";
import SearchIcon from "@mui/icons-material/Search";
import {DeleteStackDialog, StacksBulkActionBar, StacksTable} from "@/features/stacks/components";
import {useStackActions} from "@/features/stacks/hooks/useStackActions";
import {useStackSelection} from "@/features/stacks/hooks/useStackSelection";
import {useStacksFilter} from "@/features/stacks/hooks/useStacksFilter";
import {LoadingSpinner} from "@/features/shared/components";
import {createRoute} from "@/utils/routing";
import {getApiErrorMessage} from "@/utils/errorMessages";

function StacksPage() {
  const {
    stacks,
    filtered,
    isLoading,
    error,
    networks,
    appIconLookup,
    updateAvailableStackIds,
    search,
    setSearch,
    sourceFilter,
    setSourceFilter,
    environmentFilter,
    setEnvironmentFilter,
  } = useStacksFilter();

  const [deleteTarget, setDeleteTarget] = useState<{
    projectName: string;
    name: string;
  } | null>(null);

  const {startMutation, stopMutation, restartMutation, deleteMutation, isBusyStack} =
    useStackActions();

  const selection = useStackSelection(filtered.map((s) => s.dockerProjectName!));

  if (isLoading) return <LoadingSpinner />;

  return (
    <Container maxWidth="xl" sx={{py: 4}}>
      <Paper sx={{px: 3, py: 2, mb: 3, borderRadius: 2}}>
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            flexWrap: "wrap",
            gap: 2,
          }}
        >
          <Box sx={{display: "flex", alignItems: "center", gap: 1.5}}>
            <LayersIcon color="primary" />
            <Box sx={{display: "flex", alignItems: "baseline", gap: 1}}>
              <Typography variant="h5" fontWeight="bold">
                Stacks
              </Typography>
              <Chip
                label={
                  filtered.length === stacks.length
                    ? stacks.length
                    : `${filtered.length} / ${stacks.length}`
                }
                size="small"
                color="primary"
              />
            </Box>
          </Box>

          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 1.5,
              flexWrap: "wrap",
            }}
          >
            <TextField
              select
              size="small"
              label="Typ"
              value={sourceFilter}
              onChange={(e) => setSourceFilter(e.target.value as typeof sourceFilter)}
              sx={{width: 160}}
            >
              <MenuItem value="all">Alle</MenuItem>
              <MenuItem value="managed">Managed</MenuItem>
              <MenuItem value="external">Nur Extern</MenuItem>
            </TextField>

            <TextField
              select
              size="small"
              label="Environment"
              value={environmentFilter}
              onChange={(e) => setEnvironmentFilter(e.target.value)}
              sx={{width: 180}}
            >
              <MenuItem value="">Alle Environments</MenuItem>
              {networks.map((n) => (
                <MenuItem key={n.name} value={n.name ?? ""}>
                  {n.name}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              size="small"
              placeholder="Suche …"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              sx={{width: 220}}
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon fontSize="small" />
                    </InputAdornment>
                  ),
                },
              }}
            />
            <Button
              component={Link}
              to={createRoute("/stacks/deploy")}
              variant="contained"
              startIcon={<AddIcon />}
              sx={{whiteSpace: "nowrap"}}
            >
              Deployen
            </Button>
          </Box>
        </Box>
      </Paper>

      {error && (
        <Alert severity="error" sx={{mb: 3}}>
          {getApiErrorMessage(error, "Fehler beim Laden der Stacks")}
        </Alert>
      )}

      <StacksBulkActionBar stacks={filtered} selection={selection} />

      {stacks.length === 0 ? (
        <Paper sx={{p: 6, textAlign: "center"}}>
          <LayersIcon sx={{fontSize: 64, color: "text.secondary", mb: 2}} />
          <Typography variant="h6" color="text.secondary" gutterBottom>
            Keine Stacks vorhanden
          </Typography>
          <Button
            component={Link}
            to={createRoute("/stacks/deploy")}
            variant="contained"
            startIcon={<AddIcon />}
            sx={{mt: 1}}
          >
            Ersten Stack deployen
          </Button>
        </Paper>
      ) : filtered.length === 0 ? (
        <Paper sx={{p: 4, textAlign: "center"}}>
          <Typography variant="body1" color="text.secondary">
            Keine Stacks für die gewählten Filter gefunden.
          </Typography>
        </Paper>
      ) : (
        <StacksTable
          stacks={filtered}
          isBusyStack={isBusyStack}
          appIconLookup={appIconLookup}
          updateAvailableStackIds={updateAvailableStackIds}
          selection={selection}
          onStart={(id) => startMutation.mutate(id)}
          onStop={(id) => stopMutation.mutate(id)}
          onRestart={(id) => restartMutation.mutate(id)}
          onDelete={(id, name) => setDeleteTarget({projectName: id, name})}
        />
      )}

      <DeleteStackDialog
        open={!!deleteTarget}
        stackName={deleteTarget?.name}
        loading={deleteMutation.isPending}
        onConfirm={() =>
          deleteMutation.mutate(deleteTarget!.projectName, {
            onSuccess: () => setDeleteTarget(null),
          })
        }
        onClose={() => setDeleteTarget(null)}
      />
    </Container>
  );
}

export default StacksPage;
