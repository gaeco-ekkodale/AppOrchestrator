// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import CloudDownloadIcon from "@mui/icons-material/CloudDownload";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import { Link } from "react-router-dom";
import type { ApplicationVersionDTO } from "@/features/registryClient/registryApiClient";
import { VersionBadges } from "./VersionBadges";
import { createRoute } from "@/utils/routing";

interface AppVersionListProps {
  versions: ApplicationVersionDTO[];
  isLoading: boolean;
  hasError: boolean;
  registryId: string;
}

export function AppVersionList({
  versions,
  isLoading,
  hasError,
  registryId,
}: AppVersionListProps) {
  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
        <CircularProgress size={28} />
      </Box>
    );
  }

  if (hasError) {
    return (
      <Alert severity="error" sx={{ mt: 1 }}>
        Versionen konnten nicht geladen werden.
      </Alert>
    );
  }

  if (versions.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary" sx={{ py: 2 }}>
        Keine Versionen verfügbar.
      </Typography>
    );
  }

  return (
    <Stack spacing={1.5}>
      {versions.map((v) => (
        <Box
          key={v.version}
          sx={{
            p: 2,
            border: "1px solid",
            borderColor: v.isDeprecated ? "error.light" : "divider",
            borderRadius: 2,
            bgcolor: v.isDeprecated ? "error.50" : "background.paper",
          }}
        >
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "flex-start",
              gap: 1,
              flexWrap: "wrap",
            }}
          >
            <Box>
              <Box
                sx={{ display: "flex", alignItems: "center", gap: 1, mb: 0.5 }}
              >
                <Typography variant="subtitle1" fontWeight="bold">
                  v{v.version}
                </Typography>
                <VersionBadges version={v} />
              </Box>

              {v.description && (
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{ mb: 0.5 }}
                >
                  {v.description}
                </Typography>
              )}

              <Box sx={{ display: "flex", gap: 2, alignItems: "center" }}>
                <Tooltip title="Downloads">
                  <Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
                    <CloudDownloadIcon
                      fontSize="small"
                      sx={{ color: "text.disabled" }}
                    />
                    <Typography variant="caption" color="text.secondary">
                      {v.downloads}
                    </Typography>
                  </Box>
                </Tooltip>
                <Typography variant="caption" color="text.secondary">
                  {new Date(v.createdAt).toLocaleDateString("de-DE")}
                </Typography>
              </Box>
            </Box>

            <Tooltip title={v.isDeprecated ? "Diese Version ist veraltet" : ""}>
              <span>
                <Button
                  component={Link}
                  to={createRoute(
                    `/store/configure/${encodeURIComponent(registryId)}/${encodeURIComponent(v.packageId ?? "")}/${encodeURIComponent(v.version ?? "")}`,
                  )}
                  variant={v.isDeprecated ? "outlined" : "contained"}
                  color={v.isDeprecated ? "warning" : "primary"}
                  size="small"
                  startIcon={
                    v.isDeprecated ? (
                      <WarningAmberIcon fontSize="small" />
                    ) : (
                      <CloudUploadIcon fontSize="small" />
                    )
                  }
                >
                  Konfigurieren & Deployen
                </Button>
              </span>
            </Tooltip>
          </Box>
        </Box>
      ))}
    </Stack>
  );
}
