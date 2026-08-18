// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, Button } from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { Link } from "react-router-dom";
import { createRoute } from "@/utils/routing";

interface PageHeaderProps {
  backTo: string;
  backLabel: string;
  disabled?: boolean;
}

export function PageHeader({
  backTo,
  backLabel,
  disabled = false,
}: PageHeaderProps) {
  return (
    <Box sx={{ mb: 3 }}>
      <Button
        component={Link}
        to={createRoute(backTo)}
        startIcon={<ArrowBackIcon />}
        disabled={disabled}
      >
        {backLabel}
      </Button>
    </Box>
  );
}
