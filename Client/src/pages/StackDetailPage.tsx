// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {useEffect, useState} from "react";
import {Link, useLocation, useParams} from "react-router-dom";
import {Alert, Box, Button, Container, Paper, Divider} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import {StackSource} from "@/api/orchestrator";
import {
  StackActionBar,
  StackContainersPanel,
  StackConfigurationSection,
  StackInfoSection,
} from "@/features/stacks/components";
import {useStack} from "@/features/stacks/hooks/useStack";
import {LoadingSpinner} from "@/features/shared/components";
import {createRoute} from "@/utils/routing";

function StackDetailPage() {
  const {id} = useParams<{id: string}>();
  const location = useLocation();
  const backUrl = (location.state as {from?: string} | null)?.from ?? createRoute("/stacks");
  const {stack, isLoading, error} = useStack(id!);

  // Open config accordion immediately when navigating here after a clone
  const [configOpen, setConfigOpen] = useState(
    () => !!(location.state as {openConfig?: boolean} | null)?.openConfig,
  );

  useEffect(() => {
    setConfigOpen(!!(location.state as {openConfig?: boolean} | null)?.openConfig);
  }, [id]); // eslint-disable-line react-hooks/exhaustive-deps

  if (isLoading) return <LoadingSpinner />;

  if (!stack) {
    return (
      <Box sx={{p: 3}}>
        <Alert severity="error">Stack nicht gefunden</Alert>
        <Button component={Link} to={backUrl} sx={{mt: 2}}>
          Zurück
        </Button>
      </Box>
    );
  }

  const isExternalStack = stack.source === StackSource.EXTERNAL;

  return (
    <Container maxWidth="xl" sx={{py: 4}}>
      <Button component={Link} to={backUrl} startIcon={<ArrowBackIcon />} sx={{mb: 2}}>
        Zurück zur Übersicht
      </Button>

      {error && (
        <Alert severity="error" sx={{mb: 3}}>
          Fehler beim Laden des Stacks
        </Alert>
      )}

      {/* ── 1. Stack info ────────────────────────────────────────── */}
      <Paper sx={{p: 3, mb: 2, borderRadius: 2}}>
        <StackInfoSection stack={stack} />

        <Divider sx={{my: 2}} />

        <StackActionBar stack={stack} isExternal={isExternalStack} />
      </Paper>

      {/* ── 2. Container ─────────────────────────────────────────── */}
      <Paper sx={{p: 3, mb: 2, borderRadius: 2}}>
        <StackContainersPanel stackId={id!} projectName={stack.dockerProjectName} showPorts />
      </Paper>

      <StackConfigurationSection
        stack={stack}
        configOpen={configOpen}
        onConfigOpenChange={setConfigOpen}
      />
    </Container>
  );
}

export default StackDetailPage;
