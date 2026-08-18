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
import React from "react";
import { Link } from "react-router-dom";
import Apps from "@mui/icons-material/Apps";
import { useAuth } from "react-oidc-context";

/**
 * MockHostNavigation - Simulated host application navigation bar
 *
 * This component simulates a navigation bar that would normally be provided by the host application.
 * In standalone mode, it's displayed as a grayed-out version to indicate it's a simulation.
 */
const MockHostNavigation: React.FC = () => {
  // These would be actual navigation items in the host application

  const auth = useAuth();

  return (
    <nav
      className="sticky top-0 h-16 w-full bg-gray-400 text-light"
      role="navigation"
    >
      <div className="flex h-full items-center justify-between">
        <Link className="flex items-center gap-4 p-2" to={"/"}>
          <img src="/icon.svg" alt="Logo" width={40} height={40} />
          <span className="text-xl font-bold text-white">App-Orchestrator</span>
        </Link>
        <div className="p-2 flex items-center gap-4">
          <Apps sx={{ color: "Background" }} />
          <Avatar className="" onClick={() => auth.signoutRedirect()}>
            {auth.user?.profile.preferred_username
              ? auth.user.profile.preferred_username.charAt(0).toUpperCase()
              : "U"}
          </Avatar>
        </div>
      </div>
    </nav>
  );
};

export default MockHostNavigation;
