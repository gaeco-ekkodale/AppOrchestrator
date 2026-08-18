// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, Chip } from "@mui/material";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import LockIcon from "@mui/icons-material/Lock";
import type { ApplicationVersionDTO } from "@/features/registryClient/registryApiClient";

interface VersionBadgesProps {
  version: ApplicationVersionDTO;
}

export function VersionBadges({ version: v }: VersionBadgesProps) {
  return (
    <Box sx={{ display: "flex", gap: 0.5, flexWrap: "wrap" }}>
      {v.isPreRelease && (
        <Chip
          label="Pre-Release"
          size="small"
          color="warning"
          variant="outlined"
        />
      )}
      {v.isDeprecated && (
        <Chip
          label="Deprecated"
          size="small"
          color="error"
          variant="outlined"
          icon={<WarningAmberIcon fontSize="small" />}
        />
      )}
      {v.isPrivate && (
        <Chip
          label="Privat"
          size="small"
          variant="outlined"
          icon={<LockIcon fontSize="small" />}
        />
      )}
    </Box>
  );
}
