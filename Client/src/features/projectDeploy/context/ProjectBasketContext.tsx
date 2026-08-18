// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { createContext, useContext, useState, type ReactNode } from "react";
import type { ProjectApp } from "../types";

interface ProjectBasketContextValue {
  apps: ProjectApp[];
  addApp: (app: ProjectApp) => void;
  removeApp: (id: string) => void;
  clear: () => void;
  has: (id: string) => boolean;
}

const ProjectBasketContext = createContext<ProjectBasketContextValue | null>(null);

export function ProjectBasketProvider({ children }: { children: ReactNode }) {
  const [apps, setApps] = useState<ProjectApp[]>([]);

  const addApp = (app: ProjectApp) => {
    setApps((prev) => {
      if (prev.some((a) => a.id === app.id)) return prev;
      return [...prev, app];
    });
  };

  const removeApp = (id: string) => {
    setApps((prev) => prev.filter((a) => a.id !== id));
  };

  const clear = () => setApps([]);

  const has = (id: string) => apps.some((a) => a.id === id);

  return (
    <ProjectBasketContext.Provider value={{ apps, addApp, removeApp, clear, has }}>
      {children}
    </ProjectBasketContext.Provider>
  );
}

export function useProjectBasket(): ProjectBasketContextValue {
  const ctx = useContext(ProjectBasketContext);
  if (!ctx) throw new Error("useProjectBasket must be used inside ProjectBasketProvider");
  return ctx;
}
