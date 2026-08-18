// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useState } from "react";
import {
  Box,
  TextField,
  Typography,
  Card,
  CardContent,
  Button,
  List,
  ListItem,
  ListItemText,
  Paper,
  InputAdornment,
  CircularProgress,
} from "@mui/material";
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
} from "@dnd-kit/core";
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import SearchIcon from "@mui/icons-material/Search";
import DownloadIcon from "@mui/icons-material/Download";
import UploadIcon from "@mui/icons-material/Upload";
import type { AppWithRegistry } from "@/features/registryClient/registryApiClient";
import { AppIcon } from "@/features/store/components";
import { SortableAppItem } from "./SortableAppItem";
import type { ProjectApp } from "../types";
import { makeAppId } from "../types";

export type { ProjectApp };

export interface AppPickerPanelProps {
  apps: ProjectApp[];
  onAddApp: (app: ProjectApp) => void;
  onRemoveApp: (packageId: string) => void;
  onReorderApps: (apps: ProjectApp[]) => void;
  onExportBlueprint: () => void;
  onImportBlueprint: () => void;
  availableApps: AppWithRegistry[];
  registries: Array<any>; // Not used in this component
  isLoadingApps: boolean;
}

export function AppPickerPanel({
  apps,
  onAddApp,
  onRemoveApp,
  onReorderApps,
  onExportBlueprint,
  onImportBlueprint,
  availableApps,
  registries: _registries, // Not used directly in this component
  isLoadingApps,
}: AppPickerPanelProps) {
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedVersion, setSelectedVersion] = useState<Record<string, string>>({});

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    })
  );

  const filteredApps = availableApps
    .filter((app) => {
      const query = searchQuery.toLowerCase();
      return (
        app.name?.toLowerCase().includes(query) ||
        app.packageId?.toLowerCase().includes(query)
      );
    })
    .slice(0, 10); // Limit results for UI performance

  const handleAddApp = (app: AppWithRegistry) => {
    const version = selectedVersion[app.packageId!] || app.defaultVersion || "latest";
    const stackNameSlug = app.name!
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, "-")
      .replace(/^-+|-+$/g, "");

    onAddApp({
      id: makeAppId(app.packageId!, version),
      registryId: app.registryId,
      registryUrl: app.registryBaseUrl,
      registryName: app.registryName,
      packageId: app.packageId!,
      name: app.name!,
      iconUrl: app.iconUrl,
      version,
      stackName: stackNameSlug,
    });

    setSearchQuery("");
    setSelectedVersion((prev) => {
      const updated = { ...prev };
      delete updated[app.packageId!];
      return updated;
    });
  };

  const handleDragEnd = (event: any) => {
    const { active, over } = event;
    if (!over || active.id === over.id) return;

    const oldIndex = apps.findIndex((a) => a.id === active.id);
    const newIndex = apps.findIndex((a) => a.id === over.id);

    const newOrder = arrayMove(apps, oldIndex, newIndex);
    onReorderApps(newOrder);
  };

  return (
    <Box sx={{ display: "flex", gap: 3 }}>
      {/* Left: App Search & Selection */}
      <Box sx={{ flex: 1 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Apps hinzufügen
        </Typography>

        <TextField
          placeholder="Suche nach App-Namen oder PackageId..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          fullWidth
          size="small"
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  {isLoadingApps ? <CircularProgress size={20} /> : <SearchIcon />}
                </InputAdornment>
              ),
            },
          }}
          sx={{ mb: 2 }}
        />

        {searchQuery && (
          <Card variant="outlined">
            <CardContent sx={{ p: 1, "&:last-child": { pb: 1 } }}>
              {filteredApps.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  Keine Apps gefunden
                </Typography>
              ) : (
                <List dense>
                  {filteredApps.map((app) => (
                    <ListItem
                      key={`${app.registryId}:${app.packageId}`}
                      secondaryAction={
                        <Button
                          size="small"
                          onClick={() => handleAddApp(app)}
                        >
                          Hinzufügen
                        </Button>
                      }
                    >
                      <Box sx={{ mr: 1 }}>
                        <AppIcon name={app.name} iconUrl={app.iconUrl} size={40} />
                      </Box>
                      <ListItemText
                        primary={app.name}
                        secondary={app.packageId}
                        primaryTypographyProps={{ variant: "body2" }}
                        secondaryTypographyProps={{ variant: "caption" }}
                      />
                    </ListItem>
                  ))}
                </List>
              )}
            </CardContent>
          </Card>
        )}
      </Box>

      {/* Right: Project Apps List */}
      <Box sx={{ flex: 1 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 2 }}>
          <Typography variant="h6">
            Apps im Projekt ({apps.length})
          </Typography>
          <Box sx={{ ml: "auto", display: "flex", gap: 1 }}>
            <Button
              size="small"
              startIcon={<DownloadIcon />}
              onClick={onExportBlueprint}
              disabled={apps.length === 0}
            >
              Export
            </Button>
            <Button size="small" startIcon={<UploadIcon />} onClick={onImportBlueprint}>
              Import
            </Button>
          </Box>
        </Box>

        {apps.length === 0 ? (
          <Paper sx={{ p: 2, textAlign: "center", bgcolor: "action.hover" }}>
            <Typography variant="body2" color="text.secondary">
              Noch keine Apps ausgewählt
            </Typography>
          </Paper>
        ) : (
          <DndContext
            sensors={sensors}
            collisionDetection={closestCenter}
            onDragEnd={handleDragEnd}
          >
            <SortableContext
              items={apps.map((a) => a.id)}
              strategy={verticalListSortingStrategy}
            >
              <List dense sx={{ bgcolor: "background.paper", borderRadius: 1 }}>
                {apps.map((app, index) => (
                  <SortableAppItem
                    key={app.id}
                    app={app}
                    index={index + 1}
                    onRemove={() => onRemoveApp(app.packageId)}
                  />
                ))}
              </List>
            </SortableContext>
          </DndContext>
        )}
      </Box>
    </Box>
  );
}
