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
  DndContext,
  DragOverlay,
  closestCorners,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  useDroppable,
  type DragStartEvent,
  type DragEndEvent,
} from "@dnd-kit/core";
import {
  SortableContext,
  sortableKeyboardCoordinates,
  verticalListSortingStrategy,
  arrayMove,
} from "@dnd-kit/sortable";
import {
  Box,
  Button,
  Chip,
  Divider,
  IconButton,
  Paper,
  Tooltip,
  Typography,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import DownloadIcon from "@mui/icons-material/Download";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import type { DeploymentStep, ProjectApp } from "../types";
import { SortableAppItem, AppItemCard } from "./SortableAppItem";

export interface DeploymentStepsBoardProps {
  steps: DeploymentStep[];
  onStepsChange: (steps: DeploymentStep[]) => void;
  onExportBlueprint: () => void;
  onBackToStore: () => void;
  deployedPackageIds?: Set<string>;
}

/** Makes a step container droppable (needed for empty containers + cross-container) */
function DroppableStepContainer({
  id,
  children,
  isOver,
}: {
  id: string;
  children: React.ReactNode;
  isOver: boolean;
}) {
  const { setNodeRef } = useDroppable({ id });
  return (
    <Box
      ref={setNodeRef}
      sx={{
        minHeight: 60,
        p: 1,
        transition: "background-color 0.15s",
        bgcolor: isOver ? "primary.50" : undefined,
        borderRadius: 1,
      }}
    >
      {children}
    </Box>
  );
}

export function DeploymentStepsBoard({
  steps,
  onStepsChange,
  onExportBlueprint,
  onBackToStore,
  deployedPackageIds,
}: DeploymentStepsBoardProps) {
  const [activeApp, setActiveApp] = useState<ProjectApp | null>(null);
  const [overStepId, setOverStepId] = useState<string | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  const findAppStep = (appId: string) =>
    steps.find((s) => s.apps.some((a) => a.id === appId));

  const handleDragStart = ({ active }: DragStartEvent) => {
    const step = findAppStep(String(active.id));
    const app = step?.apps.find((a) => a.id === String(active.id)) ?? null;
    setActiveApp(app);
  };

  const handleDragEnd = ({ active, over }: DragEndEvent) => {
    setActiveApp(null);
    setOverStepId(null);
    if (!over) return;

    const activeId = String(active.id);
    const overId = String(over.id);
    if (activeId === overId) return;

    const sourceStep = findAppStep(activeId);
    if (!sourceStep) return;

    // over.id is either a step id (dropped on empty area) or an app id
    const targetStep =
      steps.find((s) => s.id === overId) ?? findAppStep(overId);
    if (!targetStep) return;

    const app = sourceStep.apps.find((a) => a.id === activeId)!;

    if (sourceStep.id === targetStep.id) {
      // Same container — reorder
      const oldIndex = sourceStep.apps.findIndex((a) => a.id === activeId);
      const newIndex = sourceStep.apps.findIndex((a) => a.id === overId);
      if (newIndex >= 0 && oldIndex !== newIndex) {
        onStepsChange(
          steps.map((s) =>
            s.id === sourceStep.id
              ? { ...s, apps: arrayMove(s.apps, oldIndex, newIndex) }
              : s,
          ),
        );
      }
    } else {
      // Cross-container — move app to target step
      onStepsChange(
        steps.map((s) => {
          if (s.id === sourceStep.id) {
            return { ...s, apps: s.apps.filter((a) => a.id !== activeId) };
          }
          if (s.id === targetStep.id) {
            // Insert before the over-app if there is one
            const overIndex = s.apps.findIndex((a) => a.id === overId);
            const newApps = [...s.apps];
            if (overIndex >= 0) newApps.splice(overIndex, 0, app);
            else newApps.push(app);
            return { ...s, apps: newApps };
          }
          return s;
        }),
      );
    }
  };

  const handleDragOver = ({ active, over }: any) => {
    if (!over) { setOverStepId(null); return; }
    const overId = String(over.id);
    // Highlight the step we're currently hovering
    const step = steps.find((s) => s.id === overId) ?? findAppStep(overId);
    setOverStepId(step?.id ?? null);
    // Also handle live cross-container move for better preview
    const activeId = String(active.id);
    const sourceStep = findAppStep(activeId);
    if (!sourceStep || !step || sourceStep.id === step.id) return;
    const app = sourceStep.apps.find((a) => a.id === activeId)!;
    onStepsChange(
      steps.map((s) => {
        if (s.id === sourceStep.id) return { ...s, apps: s.apps.filter((a) => a.id !== activeId) };
        if (s.id === step.id) {
          const overIndex = s.apps.findIndex((a) => a.id === overId);
          const newApps = [...s.apps];
          if (overIndex >= 0) newApps.splice(overIndex, 0, app);
          else newApps.push(app);
          return { ...s, apps: newApps };
        }
        return s;
      }),
    );
  };

  const addStep = () =>
    onStepsChange([...steps, { id: crypto.randomUUID(), apps: [] }]);

  const removeStep = (stepId: string) => {
    if (steps.length <= 1) return;
    const step = steps.find((s) => s.id === stepId)!;
    const remaining = steps.filter((s) => s.id !== stepId);
    if (step.apps.length > 0) {
      onStepsChange(
        remaining.map((s, i) =>
          i === 0 ? { ...s, apps: [...s.apps, ...step.apps] } : s,
        ),
      );
    } else {
      onStepsChange(remaining);
    }
  };

  const removeApp = (appId: string) =>
    onStepsChange(
      steps.map((s) => ({ ...s, apps: s.apps.filter((a) => a.id !== appId) })),
    );

  const totalApps = steps.reduce((acc, s) => acc + s.apps.length, 0);

  return (
    <Box>
      {/* Toolbar */}
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 2, flexWrap: "wrap" }}>
        <Typography variant="h6" sx={{ flex: 1 }}>
          Deployment-Schritte ({totalApps} App{totalApps !== 1 ? "s" : ""} in{" "}
          {steps.length} Schritt{steps.length !== 1 ? "en" : ""})
        </Typography>
        <Button size="small" startIcon={<ArrowBackIcon />} onClick={onBackToStore}>
          Weitere Apps wählen
        </Button>
        <Button
          size="small"
          startIcon={<DownloadIcon />}
          onClick={onExportBlueprint}
          disabled={totalApps === 0}
        >
          Blueprint exportieren
        </Button>
        <Button size="small" startIcon={<AddIcon />} variant="outlined" onClick={addStep}>
          Schritt hinzufügen
        </Button>
      </Box>

      <DndContext
        sensors={sensors}
        collisionDetection={closestCorners}
        onDragStart={handleDragStart}
        onDragOver={handleDragOver}
        onDragEnd={handleDragEnd}
      >
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
          {steps.map((step, stepIndex) => (
            <Paper key={step.id} variant="outlined" sx={{ borderRadius: 2, overflow: "hidden" }}>
              {/* Step header */}
              <Box
                sx={{
                  display: "flex",
                  alignItems: "center",
                  px: 2,
                  py: 1,
                  bgcolor: "primary.main",
                  color: "primary.contrastText",
                }}
              >
                <Typography variant="subtitle2" fontWeight="bold" sx={{ flex: 1 }}>
                  Schritt {stepIndex + 1}
                  {step.apps.length > 1 && (
                    <Chip
                      label={`${step.apps.length} parallel`}
                      size="small"
                      sx={{
                        ml: 1,
                        bgcolor: "primary.dark",
                        color: "primary.contrastText",
                        height: 18,
                        fontSize: "0.7rem",
                      }}
                    />
                  )}
                </Typography>
                <Tooltip title={steps.length <= 1 ? "Mindestens ein Schritt erforderlich" : "Schritt löschen"}>
                  <span>
                    <IconButton
                      size="small"
                      onClick={() => removeStep(step.id)}
                      disabled={steps.length <= 1}
                      sx={{ color: "inherit" }}
                    >
                      <DeleteOutlineIcon fontSize="small" />
                    </IconButton>
                  </span>
                </Tooltip>
              </Box>

              <SortableContext
                id={step.id}
                items={step.apps.map((a) => a.id)}
                strategy={verticalListSortingStrategy}
              >
                <DroppableStepContainer
                  id={step.id}
                  isOver={overStepId === step.id && activeApp !== null}
                >
                  {step.apps.length === 0 ? (
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      sx={{ textAlign: "center", py: 1.5 }}
                    >
                      Apps hierher ziehen
                    </Typography>
                  ) : (
                    step.apps.map((app, appIndex) => (
                      <SortableAppItem
                        key={app.id}
                        app={app}
                        index={appIndex + 1}
                        onRemove={() => removeApp(app.id)}
                        alreadyDeployed={deployedPackageIds?.has(app.packageId)}
                      />
                    ))
                  )}
                </DroppableStepContainer>
              </SortableContext>
            </Paper>
          ))}
        </Box>

        <DragOverlay dropAnimation={null}>
          {activeApp ? (
            <AppItemCard
              app={activeApp}
              index={0}
              alreadyDeployed={deployedPackageIds?.has(activeApp.packageId)}
            />
          ) : null}
        </DragOverlay>
      </DndContext>

      {totalApps === 0 && (
        <>
          <Divider sx={{ my: 2 }} />
          <Typography variant="body2" color="text.secondary" textAlign="center">
            Noch keine Apps ausgewählt. Gehe zurück zum Store und füge Apps hinzu.
          </Typography>
        </>
      )}
    </Box>
  );
}
