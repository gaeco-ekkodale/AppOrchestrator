// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useMemo, useState} from "react";
import {
  Alert,
  Box,
  Container,
  FormControl,
  Grid,
  InputAdornment,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  TextField,
  Typography,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/Search";
import StorefrontIcon from "@mui/icons-material/Storefront";
import ErrorOutlineIcon from "@mui/icons-material/ErrorOutline";
import type {AppWithRegistry} from "@/features/registryClient/registryApiClient";
import {useAllApps} from "@/features/registryClient/hooks/useAllApps";
import {AppStoreCard, AppStoreModal} from "@/features/store/components";
import {LoadingSpinner} from "@/features/shared/components";
import {ProjectBasketDrawer} from "@/features/projectDeploy/components/ProjectBasketDrawer";
import {useProjectBasket} from "@/features/projectDeploy/context/ProjectBasketContext";

// Height of both navbars (MockHostNavigation 64px + AppNavigation 64px)
const NAVBAR_TOTAL_HEIGHT = 128;
// Basket panel width
const BASKET_WIDTH = 300;

function AppStorePage() {
  const {apps, isLoading, errors, registryCount} = useAllApps();
  const basket = useProjectBasket();
  const [search, setSearch] = useState("");
  const [registryFilter, setRegistryFilter] = useState("");
  const [selected, setSelected] = useState<AppWithRegistry | null>(null);

  const registryOptions = useMemo(() => {
    const map = new Map<string, string>();
    apps.forEach((a) => map.set(a.registryId, a.registryName));
    return [...map.entries()].map(([id, name]) => ({id, name}));
  }, [apps]);

  const filtered = apps.filter((app) => {
    if (registryFilter && app.registryId !== registryFilter) return false;
    return (
      !search ||
      app.name.toLowerCase().includes(search.toLowerCase()) ||
      app.packageId.toLowerCase().includes(search.toLowerCase()) ||
      (app.description ?? "").toLowerCase().includes(search.toLowerCase()) ||
      (app.tags ?? []).some((t: string) => t.toLowerCase().includes(search.toLowerCase())) ||
      app.registryName.toLowerCase().includes(search.toLowerCase())
    );
  });

  if (isLoading && apps.length === 0) return <LoadingSpinner />;

  const hasBasket = basket.apps.length > 0;
  const basketReserveWidth = hasBasket ? BASKET_WIDTH - 10 : 0; // 16px gap + 8px right padding

  return (
    <>
      <Box
        sx={{display: "flex", gap: 1, px: 1, py: 4, pr: hasBasket ? `${basketReserveWidth}px` : 1}}
      >
        <Container maxWidth="xl" sx={{p: 0, flex: 1}}>
          {/* Header + search */}
          <Paper sx={{px: 3, py: 2, mb: 3, borderRadius: 2}}>
            <Box sx={{display: "flex", alignItems: "center", gap: 1.5, mb: 2}}>
              <StorefrontIcon color="primary" />
              <Box sx={{flex: 1}}>
                <Typography variant="h5" fontWeight="bold">
                  App Store
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {registryCount} Registr{registryCount === 1 ? "y" : "ies"}
                  &ensp;&middot;&ensp;{apps.length} App
                  {apps.length === 1 ? "" : "s"} verfügbar
                </Typography>
              </Box>
            </Box>

            <Box sx={{display: "flex", gap: 1.5}}>
              <TextField
                sx={{flex: 1}}
                size="small"
                placeholder="Apps suchen nach Name, Package-ID, Tag …"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
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
              <FormControl size="small" sx={{minWidth: 190}}>
                <InputLabel>Registry</InputLabel>
                <Select
                  label="Registry"
                  value={registryFilter}
                  onChange={(e) => setRegistryFilter(e.target.value)}
                >
                  <MenuItem value="">Alle Registries</MenuItem>
                  {registryOptions.map((r) => (
                    <MenuItem key={r.id} value={r.id}>
                      {r.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Box>
          </Paper>

          {errors.length > 0 && (
            <Alert severity="warning" icon={<ErrorOutlineIcon />} sx={{mb: 3}}>
              {errors.length} Registr{errors.length === 1 ? "y" : "ies"} konnte
              {errors.length === 1 ? "" : "n"} nicht erreicht werden. Apps anderer Registries werden
              trotzdem angezeigt.
            </Alert>
          )}

          {filtered.length === 0 ? (
            <Paper sx={{p: 6, textAlign: "center", borderRadius: 2}}>
              <StorefrontIcon sx={{fontSize: 64, color: "text.secondary", mb: 2}} />
              <Typography variant="h6" color="text.secondary">
                {search ? `Keine Apps für „${search}" gefunden` : "Keine Apps verfügbar"}
              </Typography>
            </Paper>
          ) : (
            <Grid container spacing={2.5}>
              {filtered.map((app) => (
                <Grid
                  key={`${app.registryId}:${app.packageId}`}
                  size={{xs: 12, sm: 6, md: 4, lg: 3}}
                >
                  <AppStoreCard app={app} onClick={() => setSelected(app)} />
                </Grid>
              ))}
            </Grid>
          )}

          <AppStoreModal app={selected} onClose={() => setSelected(null)} />
        </Container>
      </Box>

      {/* Fixed basket panel — always visible, scrollable internally */}
      {hasBasket && (
        <Box
          sx={{
            position: "fixed",
            top: NAVBAR_TOTAL_HEIGHT,
            right: 0,
            bottom: 0,
            width: BASKET_WIDTH,
            overflow: "auto",
            pt: 4,
            pr: 3,
            pb: 2,
          }}
        >
          <ProjectBasketDrawer />
        </Box>
      )}
    </>
  );
}

export default AppStorePage;
