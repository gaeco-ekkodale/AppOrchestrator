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
  AppBar,
  Box,
  Button,
  Container,
  Toolbar,
  Typography,
} from "@mui/material";
import LayersIcon from "@mui/icons-material/Layers";
import StorageIcon from "@mui/icons-material/Storage";
import DnsIcon from "@mui/icons-material/Dns";
import StorefrontIcon from "@mui/icons-material/Storefront";
import PublicIcon from "@mui/icons-material/Public";
import { Link, useLocation } from "react-router-dom";
import { createRoute } from "@/utils/routing";

/**
 * AppNavigation - Top navigation bar for the AppOrchestrator frontend.
 */
export function AppNavigation() {
  const location = useLocation();

  const isActive = (prefix: string) =>
    location.pathname.startsWith(createRoute(prefix));

  return (
    <AppBar
      position="sticky"
      elevation={1}
      color="default"
      sx={{ bgcolor: "background.paper" }}
    >
      <Container maxWidth="xl">
        <Toolbar disableGutters>
          <Typography
            variant="h6"
            fontWeight="bold"
            color="primary"
            sx={{ mr: 4 }}
          >
            AppOrchestrator
          </Typography>

          {/* Left: deployment-oriented pages */}
          <Box sx={{ display: "flex", gap: 1 }}>
            <Button
              component={Link}
              to={createRoute("/stacks")}
              startIcon={<LayersIcon />}
              color={isActive("/stacks") ? "primary" : "inherit"}
              variant={isActive("/stacks") ? "contained" : "text"}
              size="small"
            >
              Stacks
            </Button>
            <Button
              component={Link}
              to={createRoute("/store")}
              startIcon={<StorefrontIcon />}
              color={isActive("/store") ? "primary" : "inherit"}
              variant={isActive("/store") ? "contained" : "text"}
              size="small"
            >
              App Store
            </Button>
            <Button
              component={Link}
              to={createRoute("/environments")}
              startIcon={<PublicIcon />}
              color={isActive("/environments") ? "primary" : "inherit"}
              variant={isActive("/environments") ? "contained" : "text"}
              size="small"
            >
              Environments
            </Button>
          </Box>

          {/* Right: registry / infrastructure configuration */}
          <Box sx={{ display: "flex", gap: 1, ml: "auto" }}>
            <Button
              component={Link}
              to={createRoute("/registries")}
              startIcon={<StorageIcon />}
              color={isActive("/registries") ? "primary" : "inherit"}
              variant={isActive("/registries") ? "contained" : "text"}
              size="small"
            >
              App Registries
            </Button>
            <Button
              component={Link}
              to={createRoute("/container-registries")}
              startIcon={<DnsIcon />}
              color={isActive("/container-registries") ? "primary" : "inherit"}
              variant={isActive("/container-registries") ? "contained" : "text"}
              size="small"
            >
              Container Registries
            </Button>
          </Box>
        </Toolbar>
      </Container>
    </AppBar>
  );
}
