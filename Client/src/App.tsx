// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useAuth} from "react-oidc-context";
import {Navigate, Route, Routes} from "react-router-dom";
import {useEffect} from "react";
import {OpenAPI as OrchestratorOpenAPI} from "./api/orchestrator";
import {ToastProvider} from "./features/shared/contexts/ToastContext";
import {ProjectBasketProvider} from "./features/projectDeploy/context/ProjectBasketContext";
import {AppNavigation} from "./layout/AppNavigation";
import StacksPage from "./pages/StacksPage";
import StackDetailPage from "./pages/StackDetailPage";
import DeployStackPage from "./pages/DeployStackPage";
import RegistriesPage from "./pages/RegistriesPage";
import RegistryFormPage from "./pages/RegistryFormPage";
import ContainerRegistriesPage from "./pages/ContainerRegistriesPage";
import ContainerRegistryFormPage from "./pages/ContainerRegistryFormPage";
import AppStorePage from "./pages/AppStorePage";
import DeployFromStorePage from "./pages/DeployFromStorePage";
import ProjectDeployPage from "./pages/ProjectDeployPage";
import EnvironmentsPage from "./pages/EnvironmentsPage";
import {QueryClient, QueryClientProvider} from "@tanstack/react-query";
import {Box, CssBaseline, ThemeProvider} from "@mui/material";
import {appTheme} from "./theme/appTheme";
import Tour from "./features/tour/Tour";
import {TOUR_KEY, TOUR_MODULE_NAME, TOUR_PANELS} from "./features/tour/tourContent";
import "./index.css";

OrchestratorOpenAPI.BASE = import.meta.env.VITE_API_URL;
const queryClient = new QueryClient();

function App() {
  const auth = useAuth();

  // Set tokens synchronously during render so queries in child components
  // never fire a request without a token (avoids a 401 on first render).
  const token = auth.user?.access_token;
  OrchestratorOpenAPI.TOKEN = token;

  useEffect(() => {
    OrchestratorOpenAPI.TOKEN = token;
  }, [token]);

  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={appTheme}>
        <CssBaseline />
        <ToastProvider>
          <ProjectBasketProvider>
          <Box
            sx={{
              display: "flex",
              flexDirection: "column",
              minHeight: "100vh",
            }}
          >
            <AppNavigation />
            {/* Fixed to the bottom right of the viewport, same as in every other module. */}
            <Tour tourKey={TOUR_KEY} moduleName={TOUR_MODULE_NAME} panels={TOUR_PANELS} />
            <Box sx={{flex: 1, overflow: "auto"}}>
              <Routes>
                {/* Redirect root to stacks */}
                <Route path="/" element={<Navigate to="stacks" replace />} />

                {/* Stacks */}
                <Route path="/stacks" element={<StacksPage />} />
                <Route path="/stacks/deploy" element={<DeployStackPage />} />
                <Route path="/stacks/:id" element={<StackDetailPage />} />

                {/* Registries */}
                <Route path="/registries" element={<RegistriesPage />} />
                <Route path="/registries/new" element={<RegistryFormPage />} />
                <Route path="/registries/:id/edit" element={<RegistryFormPage />} />

                {/* Container Registries */}
                <Route path="/container-registries" element={<ContainerRegistriesPage />} />
                <Route path="/container-registries/new" element={<ContainerRegistryFormPage />} />
                <Route
                  path="/container-registries/:id/edit"
                  element={<ContainerRegistryFormPage />}
                />

                {/* App Store */}
                <Route path="/store" element={<AppStorePage />} />
                <Route
                  path="/store/configure/:registryId/:packageId/:version"
                  element={<DeployFromStorePage />}
                />
                <Route path="/store/project-deploy" element={<ProjectDeployPage />} />

                {/* Environments */}
                <Route path="/environments" element={<EnvironmentsPage />} />
              </Routes>
            </Box>
          </Box>
          </ProjectBasketProvider>
        </ToastProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
