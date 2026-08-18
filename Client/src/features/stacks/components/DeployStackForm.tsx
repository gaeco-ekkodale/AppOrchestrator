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
import {useNavigate} from "react-router-dom";
import {Link} from "react-router-dom";
import {Box, Button, Card, CardContent, Tab, Tabs, Typography} from "@mui/material";
import StorefrontIcon from "@mui/icons-material/Storefront";
import CodeIcon from "@mui/icons-material/Code";
import AccountTreeIcon from "@mui/icons-material/AccountTree";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import {useCreateCustomStackMutation} from "../hooks/useStackMutations";
import {DeployCustomForm} from ".";
import {createRoute} from "@/utils/routing";
import type {NetworkDTO} from "@/api/orchestrator";
import {ProjectImportTab} from "@/features/projectDeploy/components/ProjectImportTab";

type DeployMode = "registry" | "custom" | "project";

interface DeployStackFormProps {
  networks: NetworkDTO[];
}

export function DeployStackForm({networks}: DeployStackFormProps) {
  const navigate = useNavigate();
  const [mode, setMode] = useState<DeployMode>("registry");

  const deployCustomMutation = useCreateCustomStackMutation((data) => {
    navigate(createRoute(`/stacks/${data.dockerProjectName}`));
  });

  const noEnvironments = networks.length === 0;

  return (
    <Card sx={{borderRadius: 2}}>
      <Tabs
        value={mode}
        onChange={(_, v) => setMode(v as DeployMode)}
        sx={{borderBottom: 1, borderColor: "divider", px: 2}}
      >
        <Tab
          value="registry"
          label="Aus App-Registry"
          icon={<StorefrontIcon fontSize="small" />}
          iconPosition="start"
        />
        <Tab
          value="custom"
          label="Custom (docker-compose)"
          icon={<CodeIcon fontSize="small" />}
          iconPosition="start"
        />
        <Tab
          value="project"
          label="Projekt"
          icon={<AccountTreeIcon fontSize="small" />}
          iconPosition="start"
        />
      </Tabs>

      <CardContent sx={{p: 3}}>
        {mode === "registry" && (
          <Box
            sx={{
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              gap: 2,
              py: 4,
              textAlign: "center",
            }}
          >
            <StorefrontIcon sx={{fontSize: 48, color: "text.secondary"}} />
            <Box>
              <Typography variant="h6" fontWeight="bold" gutterBottom>
                Apps aus dem App Store deployen
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{maxWidth: 400}}>
                Wechsle in den App Store, wähle eine App aus und deploye sie direkt von dort aus.
              </Typography>
            </Box>
            <Button
              component={Link}
              to={createRoute("/store")}
              variant="contained"
              startIcon={<StorefrontIcon />}
              endIcon={<OpenInNewIcon fontSize="small" />}
            >
              Zum App Store
            </Button>
          </Box>
        )}

        {mode === "custom" && (
          <DeployCustomForm
            networks={networks}
            noEnvironments={noEnvironments}
            isPending={deployCustomMutation.isPending}
            isError={deployCustomMutation.isError}
            onSubmit={(data) => deployCustomMutation.mutate(data)}
          />
        )}

        {mode === "project" && <ProjectImportTab />}
      </CardContent>
    </Card>
  );
}
