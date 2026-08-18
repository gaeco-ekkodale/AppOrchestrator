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
  CardActions,
  CardContent,
  Chip,
  Container,
  Grid,
  Paper,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import DnsIcon from "@mui/icons-material/Dns";
import {DeleteContainerRegistryDialog} from "@/features/dockerRegistries/components";
import {useDeleteContainerRegistryMutation} from "@/features/dockerRegistries/hooks/useContainerRegistryMutations";
import {useContainerRegistries} from "@/features/dockerRegistries/hooks/useContainerRegistries";
import {LoadingSpinner} from "@/features/shared/components";
import {createRoute} from "@/utils/routing";

function ContainerRegistriesPage() {
  const {containerRegistries, isLoading, error} = useContainerRegistries();
  const navigate = useNavigate();

  const [deleteTarget, setDeleteTarget] = useState<{
    id: string;
    name: string;
    serverAddress: string;
  } | null>(null);

  const deleteMutation = useDeleteContainerRegistryMutation(() => {
    setDeleteTarget(null);
  });

  if (isLoading) return <LoadingSpinner />;

  return (
    <Container maxWidth="xl" sx={{py: 4}}>
      <Paper sx={{px: 3, py: 2, mb: 3, borderRadius: 2}}>
        <Box sx={{display: "flex", alignItems: "center", gap: 1.5}}>
          <DnsIcon color="primary" />
          <Typography variant="h5" fontWeight="bold" sx={{flexGrow: 1}}>
            Container-Registries
          </Typography>
          <Chip label={containerRegistries.length} size="small" color="primary" sx={{mr: 1}} />
          <Button
            component={Link}
            to={createRoute("/container-registries/new")}
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
          Fehler beim Laden der Container-Registries
        </Alert>
      )}

      {containerRegistries.length === 0 ? (
        <Paper sx={{p: 6, textAlign: "center"}}>
          <DnsIcon sx={{fontSize: 64, color: "text.secondary", mb: 2}} />
          <Typography variant="h6" color="text.secondary" gutterBottom>
            Keine Container-Registries konfiguriert
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{mb: 3}}>
            Füge eine Registry hinzu, um private Docker-Images zu pullen.
          </Typography>
          <Button
            component={Link}
            to={createRoute("/container-registries/new")}
            variant="contained"
            startIcon={<AddIcon />}
          >
            Erste Registry hinzufügen
          </Button>
        </Paper>
      ) : (
        <Grid container spacing={3}>
          {containerRegistries.map((registry) => (
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
                  <Typography variant="h6" fontWeight="bold" gutterBottom>
                    {registry.name}
                  </Typography>
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{wordBreak: "break-all", mb: 2}}
                  >
                    {registry.serverAddress}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Hinzugefügt:{" "}
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
                    onClick={() =>
                      navigate(createRoute(`/container-registries/${registry.id}/edit`))
                    }
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
                        serverAddress: registry.serverAddress!,
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

      <DeleteContainerRegistryDialog
        open={!!deleteTarget}
        registryName={deleteTarget?.name}
        serverAddress={deleteTarget?.serverAddress}
        loading={deleteMutation.isPending}
        onConfirm={() => deleteMutation.mutate(deleteTarget!.id)}
        onClose={() => setDeleteTarget(null)}
      />
    </Container>
  );
}

export default ContainerRegistriesPage;
