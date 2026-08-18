// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {Link} from "react-router-dom";
import {Box, Button, Container, Paper, Typography} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import {useNetworks} from "@/features/networks/hooks/useNetworks";
import {createRoute} from "@/utils/routing";
import {DeployStackForm} from "@/features/stacks/components";

function DeployStackPage() {
  const {networks} = useNetworks();

  return (
    <Container maxWidth="xl" sx={{py: 4}}>
      <Button
        component={Link}
        to={createRoute("/stacks")}
        startIcon={<ArrowBackIcon />}
        sx={{mb: 2}}
      >
        Zurück zur Übersicht
      </Button>

      <Paper sx={{px: 3, py: 2, mb: 3, borderRadius: 2}}>
        <Box sx={{display: "flex", alignItems: "center", gap: 1}}>
          <CloudUploadIcon color="primary" />
          <Box>
            <Typography variant="h5" fontWeight="bold">
              Stack deployen
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Eigenes docker-compose.yml einfügen oder direkt aus dem App Store deployen
            </Typography>
          </Box>
        </Box>
      </Paper>

      <DeployStackForm networks={networks} />
    </Container>
  );
}

export default DeployStackPage;
