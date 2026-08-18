// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useNavigate } from "react-router-dom";
import {
  Box,
  Button,
  Chip,
  Divider,
  IconButton,
  Paper,
  Tooltip,
  Typography,
} from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import ShoppingCartIcon from "@mui/icons-material/ShoppingCart";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import { useProjectBasket } from "../context/ProjectBasketContext";
import { AppIcon } from "@/features/store/components/AppIcon";
import { createRoute } from "@/utils/routing";

export function ProjectBasketDrawer() {
  const navigate = useNavigate();
  const { apps, removeApp, clear } = useProjectBasket();

  if (apps.length === 0) return null;

  return (
    <Paper
      elevation={2}
      sx={{
        borderRadius: 2,
        overflow: "hidden",
        display: "flex",
        flexDirection: "column",
        height: "100%",
      }}
    >
      {/* Header */}
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          gap: 1,
          px: 2,
          py: 1.5,
          bgcolor: "primary.main",
          color: "primary.contrastText",
          flexShrink: 0,
        }}
      >
        <ShoppingCartIcon fontSize="small" />
        <Typography variant="subtitle2" fontWeight="bold" sx={{ flex: 1 }}>
          Deployment ({apps.length})
        </Typography>
        <Tooltip title="Alle entfernen">
          <IconButton size="small" onClick={clear} sx={{ color: "inherit" }}>
            <DeleteOutlineIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Box>

      {/* App list */}
      <Box sx={{ flex: 1, overflow: "auto", px: 1, py: 1 }}>
        {apps.map((app) => (
          <Box
            key={app.id}
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 1,
              py: 0.75,
              px: 0.5,
              borderRadius: 1,
              "&:hover": { bgcolor: "action.hover" },
            }}
          >
            <AppIcon name={app.name} iconUrl={app.iconUrl} size={28} />
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography variant="body2" noWrap fontWeight="medium">
                {app.name}
              </Typography>
              <Chip
                label={app.version}
                size="small"
                variant="outlined"
                sx={{ height: 16, fontSize: "0.65rem" }}
              />
            </Box>
            <IconButton size="small" onClick={() => removeApp(app.id)} aria-label="entfernen">
              <CloseIcon sx={{ fontSize: 14 }} />
            </IconButton>
          </Box>
        ))}
      </Box>

      <Divider />

      {/* Footer */}
      <Box sx={{ p: 1.5, flexShrink: 0 }}>
        <Button
          variant="contained"
          fullWidth
          startIcon={<CloudUploadIcon />}
          onClick={() => navigate(createRoute("/store/project-deploy"))}
        >
          Konfigurieren & Deployen
        </Button>
      </Box>
    </Paper>
  );
}
