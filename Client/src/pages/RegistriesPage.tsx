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
import {Link, useNavigate} from "react-router-dom";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  CardActions,
  Chip,
  Container,
  Grid,
  Paper,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import StorageIcon from "@mui/icons-material/Storage";
import LayersIcon from "@mui/icons-material/Layers";
import {DeleteRegistryDialog} from "@/features/appRegistries/components";
import {useDeleteAppRegistryMutation} from "@/features/appRegistries/hooks/useAppRegistryMutations";
import {useRegistries} from "@/features/appRegistries/hooks/useRegistries";
import {LoadingSpinner} from "@/features/shared/components";
import {createRoute} from "@/utils/routing";

function RegistriesPage() {
  const {registries, isLoading, error} = useRegistries();
  const navigate = useNavigate();

  const [deleteTarget, setDeleteTarget] = useState<{
    id: string;
    name: string;
  } | null>(null);

  const deleteMutation = useDeleteAppRegistryMutation(() => {
    setDeleteTarget(null);
  });

  if (isLoading) return <LoadingSpinner />;

  return (
    <Container maxWidth="xl" sx={{py: 4}}>
      <Paper sx={{px: 3, py: 2, mb: 3, borderRadius: 2}}>
        <Box sx={{display: "flex", alignItems: "center", gap: 1.5}}>
          <StorageIcon color="primary" />
          <Typography variant="h5" fontWeight="bold" sx={{flexGrow: 1}}>
            Registries
          </Typography>
          <Chip label={registries.length} size="small" color="primary" sx={{mr: 1}} />
          <Button
            component={Link}
            to={createRoute("/registries/new")}
            variant="contained"
            color="primary"
            startIcon={<AddIcon />}
            size="small"
          >
            Registry hinzufügen
          </Button>
        </Box>
      </Paper>

      {error && (
        <Alert severity="error" sx={{mb: 3}}>
          Fehler beim Laden der Registries
        </Alert>
      )}

      {registries.length === 0 ? (
        <Paper sx={{p: 6, textAlign: "center"}}>
          <StorageIcon sx={{fontSize: 64, color: "text.secondary", mb: 2}} />
          <Typography variant="h6" color="text.secondary" gutterBottom>
            Keine Registries vorhanden
          </Typography>
          <Button
            component={Link}
            to={createRoute("/registries/new")}
            variant="contained"
            startIcon={<AddIcon />}
            sx={{mt: 1}}
          >
            Erste Registry hinzufügen
          </Button>
        </Paper>
      ) : (
        <Grid container spacing={3}>
          {registries.map((registry) => (
            <Grid key={registry.id} size={{xs: 12, sm: 6, md: 4}}>
              <Card
                sx={{
                  borderRadius: 2,
                  height: "100%",
                  display: "flex",
                  flexDirection: "column",
                }}
              >
                <CardContent sx={{flexGrow: 1}}>
                  <Box
                    sx={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "flex-start",
                      mb: 1,
                    }}
                  >
                    <Typography variant="h6" fontWeight="bold">
                      {registry.name}
                    </Typography>
                    <Chip
                      label={`${registry.stackCount ?? 0} Stacks`}
                      size="small"
                      icon={<LayersIcon />}
                      variant="outlined"
                    />
                  </Box>
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{wordBreak: "break-all", mb: 2}}
                  >
                    {registry.baseUrl}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Erstellt:{" "}
                    {registry.createdAt
                      ? new Date(registry.createdAt).toLocaleDateString("de-DE")
                      : "–"}
                  </Typography>
                </CardContent>
                <CardActions sx={{px: 2, pb: 2, gap: 1}}>
                  <Button
                    size="small"
                    startIcon={<EditIcon />}
                    variant="outlined"
                    onClick={() => navigate(createRoute(`/registries/${registry.id}/edit`))}
                  >
                    Bearbeiten
                  </Button>
                  <Button
                    size="small"
                    startIcon={<DeleteIcon />}
                    variant="outlined"
                    color="error"
                    onClick={() =>
                      setDeleteTarget({
                        id: registry.id!,
                        name: registry.name!,
                      })
                    }
                  >
                    Löschen
                  </Button>
                </CardActions>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      <DeleteRegistryDialog
        open={!!deleteTarget}
        registryName={deleteTarget?.name}
        loading={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate(deleteTarget!.id)}
        onClose={() => setDeleteTarget(null)}
      />
    </Container>
  );
}

export default RegistriesPage;
