// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Avatar } from "@mui/material";
import ProtectedImage from "@/features/shared/components/ProtectedImage";

interface AppIconProps {
  name: string;
  iconUrl?: string;
  size?: number;
}

export function AppIcon({ name, iconUrl, size = 56 }: AppIconProps) {
  const initial = name?.charAt(0)?.toUpperCase() ?? "?";
  if (!iconUrl)
    return <Avatar sx={{ width: size, height: size }}>{initial}</Avatar>;
  return (
    <ProtectedImage
      url={iconUrl}
      alt={`${name} icon`}
      width={size}
      height={size}
      style={{ objectFit: "cover" }}
    />
  );
}
