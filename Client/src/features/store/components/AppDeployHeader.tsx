// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, Chip, Paper, Typography } from "@mui/material";
import RocketLaunchIcon from "@mui/icons-material/RocketLaunch";
import StorageIcon from "@mui/icons-material/Storage";
import ProtectedImage from "@/features/shared/components/ProtectedImage";

interface AppDeployHeaderProps {
  iconUrl?: string;
  name?: string;
  packageId: string;
  version: string;
  registryName?: string;
}

export function AppDeployHeader({
  iconUrl,
  name,
  packageId,
  version,
  registryName,
}: AppDeployHeaderProps) {
  const displayName = name ?? packageId;
  const initial = displayName.charAt(0).toUpperCase();

  return (
    <Paper sx={{ p: 3, borderRadius: 2, mb: 3 }}>
      <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
        {iconUrl ? (
          <ProtectedImage
            url={iconUrl}
            alt={`${displayName} icon`}
            width={64}
            height={64}
            style={{ borderRadius: "12px", objectFit: "cover", flexShrink: 0 }}
          />
        ) : (
          <Box
            sx={{
              width: 64,
              height: 64,
              borderRadius: "12px",
              bgcolor: "primary.main",
              color: "primary.contrastText",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: 28,
              fontWeight: "bold",
              flexShrink: 0,
            }}
          >
            {initial}
          </Box>
        )}

        <Box sx={{ flex: 1 }}>
          <Box
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 1,
              flexWrap: "wrap",
            }}
          >
            <Typography variant="h5" fontWeight="bold">
              {displayName}
            </Typography>
            <Chip label={`v${version}`} size="small" color="primary" />
            {registryName && (
              <Chip
                icon={<StorageIcon />}
                label={registryName}
                size="small"
                variant="outlined"
              />
            )}
          </Box>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ wordBreak: "break-all" }}
          >
            {packageId}
          </Typography>
        </Box>

        <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
          <RocketLaunchIcon color="primary" fontSize="large" />
          <Typography variant="h6" fontWeight="bold" color="primary">
            Konfigurieren & Deployen
          </Typography>
        </Box>
      </Box>
    </Paper>
  );
}
