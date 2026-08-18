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
  Box,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  Typography,
} from "@mui/material";
import StorageIcon from "@mui/icons-material/Storage";
import type { AppWithRegistry } from "@/features/registryClient/registryApiClient";
import { AppIcon } from "./AppIcon";

interface AppStoreCardProps {
  app: AppWithRegistry;
  onClick: () => void;
}

export function AppStoreCard({ app, onClick }: AppStoreCardProps) {
  return (
    <Card
      sx={{
        height: "100%",
        display: "flex",
        flexDirection: "column",
        borderRadius: 2,
        transition: "box-shadow 0.15s, transform 0.15s",
        "&:hover": {
          boxShadow: 6,
          transform: "translateY(-2px)",
        },
      }}
      elevation={2}
    >
      <CardActionArea
        onClick={onClick}
        sx={{
          flex: 1,
          display: "flex",
          flexDirection: "column",
          alignItems: "stretch",
          p: 0,
        }}
      >
        <CardContent
          sx={{ flex: 1, display: "flex", flexDirection: "column", gap: 1.5 }}
        >
          {/* Icon + Title row */}
          <Box sx={{ display: "flex", gap: 1.5, alignItems: "center" }}>
            <AppIcon name={app.name} iconUrl={app.iconUrl} size={48} />
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography fontWeight="bold" noWrap>
                {app.name}
              </Typography>
              <Typography
                variant="caption"
                color="text.secondary"
                noWrap
                sx={{ display: "block" }}
              >
                {app.packageId}
              </Typography>
            </Box>
          </Box>

          {/* Description */}
          {app.description ? (
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{
                display: "-webkit-box",
                WebkitLineClamp: 3,
                WebkitBoxOrient: "vertical",
                overflow: "hidden",
                flexGrow: 1,
              }}
            >
              {app.description}
            </Typography>
          ) : (
            <Box sx={{ flexGrow: 1 }} />
          )}

          {/* Bottom meta */}
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5, mt: "auto" }}>
            <Chip
              icon={<StorageIcon sx={{ fontSize: "0.75rem !important" }} />}
              label={app.registryName}
              size="small"
              color="primary"
              variant="outlined"
              sx={{ maxWidth: 160 }}
            />
            {(app.tags ?? []).slice(0, 2).map((tag: string) => (
              <Chip key={tag} label={tag} size="small" variant="outlined" />
            ))}
            {(app.tags ?? []).length > 2 && (
              <Chip label={`+${(app.tags ?? []).length - 2}`} size="small" />
            )}
          </Box>

          {/* Version */}
          <Box sx={{ display: "flex", justifyContent: "flex-end" }}>
            <Chip
              label={`v${app.defaultVersion}`}
              size="small"
              color="primary"
              variant="outlined"
            />
          </Box>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
