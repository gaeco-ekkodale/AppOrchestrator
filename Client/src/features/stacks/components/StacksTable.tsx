// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import React, {useState} from "react";
import {Link} from "react-router-dom";
import {
  Avatar,
  Box,
  Checkbox,
  Chip,
  CircularProgress,
  IconButton,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from "@mui/material";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import StopIcon from "@mui/icons-material/Stop";
import RestartAltIcon from "@mui/icons-material/RestartAlt";
import DeleteIcon from "@mui/icons-material/Delete";
import KeyboardArrowDownIcon from "@mui/icons-material/KeyboardArrowDown";
import KeyboardArrowRightIcon from "@mui/icons-material/KeyboardArrowRight";
import SystemUpdateAltIcon from "@mui/icons-material/SystemUpdateAlt";
import {StackSource, type StackDTO} from "@/api/orchestrator";
import {StackStatusChip} from "./StackStatusChip";
import {StackContainersPanel} from "./StackContainersPanel";
import {AppIcon} from "@/features/store/components";
import {createRoute} from "@/utils/routing";
import type {StackSelection} from "../hooks/useStackSelection";
import {canMutate, canRestart, canStart, canStop} from "../stackStatus";

interface StacksTableProps {
  stacks: StackDTO[];
  isBusyStack?: (id: string) => boolean;
  /** registryId:packageId → iconUrl, built from the app store cache. */
  appIconLookup?: Map<string, string>;
  /** Stack ID → newest eligible app-store version, for stacks with an update available. */
  updateAvailableStackIds?: Map<string, string>;
  /** Row selection driving the bulk action bar. */
  selection: StackSelection;
  onStart: (id: string) => void;
  onStop: (id: string) => void;
  onRestart: (id: string) => void;
  onDelete: (id: string, name: string) => void;
}

/**
 * A `<tr>` can't be wrapped in an `<a>`, so the name link is stretched over the whole row
 * instead: the browser then treats a click anywhere on the row as a link click, including
 * middle-click, Ctrl+click and "open in new tab" from the context menu.
 */
const stretchedLinkSx = {
  "&::after": {
    content: '""',
    position: "absolute",
    inset: 0,
  },
} as const;

/** Lifts interactive cell content above the stretched row link so it stays clickable. */
const aboveRowLinkSx = {position: "relative", zIndex: 1} as const;

/**
 * The theme pins every table cell to `vertical-align: top`, which leaves checkboxes, icons and
 * buttons hanging at the top edge of a row made tall by the two-line name cell. The rule has to
 * target the cells themselves — a value inherited from the row loses against the theme's own
 * `.MuiTableCell-root`. Only this table's own rows are matched, so the nested container table in
 * an expanded row keeps the default alignment.
 */
const centeredCellsSx = {
  "& > thead > tr > th, & > tbody > tr > td": {verticalAlign: "middle"},
} as const;

/** Sub-row rendered when a stack row is expanded. Fetches containers lazily. */
function ContainersSubRow({stackId, colSpan}: {stackId: string; colSpan: number}) {
  return (
    <TableRow>
      <TableCell colSpan={colSpan} sx={{p: 0, borderBottom: "none"}}>
        <Box
          sx={{
            bgcolor: "grey.50",
            py: 1,
            width: "100%",
            maxWidth: "100%",
            overflow: "hidden",
            borderBottom: "1px solid",
            borderColor: "divider",
          }}
        >
          <StackContainersPanel stackId={stackId} projectName={stackId} hideHeader={true} />
        </Box>
      </TableCell>
    </TableRow>
  );
}

function getPackageDisplay(stack: StackDTO): string {
  if (stack.source === StackSource.APP_STORE) {
    return stack.packageId || "";
  }

  if (stack.source === StackSource.CUSTOM_COMPOSE) {
    return "Custom Compose";
  }

  if (stack.source === StackSource.EXTERNAL) {
    return "External";
  }

  return stack.packageId || "";
}

function isPackageSourceChip(stack: StackDTO): boolean {
  return stack.source === StackSource.CUSTOM_COMPOSE || stack.source === StackSource.EXTERNAL;
}

function getCreatedDisplay(createdAt?: string | null): string {
  if (!createdAt) {
    return "";
  }

  const parsed = new Date(createdAt);
  if (Number.isNaN(parsed.getTime()) || parsed.getUTCFullYear() <= 1) {
    return "";
  }

  return parsed.toLocaleDateString("de-DE");
}

export function StacksTable({
  stacks,
  isBusyStack,
  appIconLookup,
  updateAvailableStackIds,
  selection,
  onStart,
  onStop,
  onRestart,
  onDelete,
}: StacksTableProps) {
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  const toggleExpand = (projectName: string, e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(projectName)) {
        next.delete(projectName);
      } else {
        next.add(projectName);
      }
      return next;
    });
  };

  // 10 columns: select | expand | icon | name | package | version | registry | status | created | actions
  const colSpan = 10;

  return (
    <TableContainer component={Paper} sx={{borderRadius: 2, width: "100%", overflowX: "auto"}}>
      <Table sx={{width: "100%", minWidth: 1100, tableLayout: "auto", ...centeredCellsSx}}>
        <TableHead>
          <TableRow sx={{"& th": {fontWeight: "bold", bgcolor: "grey.50"}}}>
            <TableCell padding="checkbox" align="center">
              <Tooltip title={selection.allSelected ? "Auswahl aufheben" : "Alle auswählen"}>
                <Checkbox
                  size="small"
                  checked={selection.allSelected}
                  indeterminate={selection.someSelected}
                  onChange={selection.toggleAll}
                  slotProps={{input: {"aria-label": "Alle Stacks auswählen"}}}
                />
              </Tooltip>
            </TableCell>
            <TableCell sx={{width: 40, pr: 0}} />
            <TableCell sx={{width: 44, pl: 1, pr: 0}} />
            <TableCell sx={{minWidth: 220}}>Name</TableCell>
            <TableCell sx={{minWidth: 170}}>Package</TableCell>
            <TableCell sx={{width: 130}}>Version</TableCell>
            <TableCell sx={{minWidth: 170}}>Registry</TableCell>
            <TableCell sx={{width: "10%"}}>Status</TableCell>
            <TableCell sx={{width: "9%"}}>Erstellt</TableCell>
            <TableCell align="right" sx={{width: 188}}>
              Aktionen
            </TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {stacks.map((stack) => {
            const rowBusy = isBusyStack?.(stack.dockerProjectName!) ?? false;
            const isExpanded = expanded.has(stack.dockerProjectName!);
            const updateVersion = updateAvailableStackIds?.get(stack.dockerProjectName!);
            const startDisabled = !canStart(stack.status);
            const stopDisabled = !canStop(stack.status);
            const restartDisabled = !canRestart(stack.status);
            const mutateDisabled = !canMutate(stack.status);
            const detailPath = createRoute(`/stacks/${stack.dockerProjectName}`);
            const isChecked = selection.isSelected(stack.dockerProjectName!);

            return (
              <React.Fragment key={stack.dockerProjectName}>
                <TableRow
                  hover
                  selected={isChecked}
                  sx={{position: "relative", cursor: "pointer"}}
                >
                  {/* Selection — sits above the row link so it doesn't navigate */}
                  <TableCell padding="checkbox" align="center" sx={aboveRowLinkSx}>
                    <Checkbox
                      size="small"
                      checked={isChecked}
                      onChange={() => selection.toggle(stack.dockerProjectName!)}
                      slotProps={{
                        input: {"aria-label": `${stack.stackName ?? ""} auswählen`},
                      }}
                    />
                  </TableCell>

                  {/* Expand toggle — sits above the row link so it doesn't navigate */}
                  <TableCell
                    sx={{width: 40, pr: 0, ...aboveRowLinkSx}}
                    onClick={(e) => toggleExpand(stack.dockerProjectName!, e)}
                  >
                    <Tooltip title={isExpanded ? "Container ausblenden" : "Container anzeigen"}>
                      <IconButton size="small">
                        {isExpanded ? (
                          <KeyboardArrowDownIcon fontSize="small" />
                        ) : (
                          <KeyboardArrowRightIcon fontSize="small" />
                        )}
                      </IconButton>
                    </Tooltip>
                  </TableCell>

                  <TableCell sx={{pl: 1, pr: 0, width: 44}}>
                    {stack.appRegistryId ? (
                      <AppIcon
                        name={stack.packageId ?? stack.stackName ?? "?"}
                        iconUrl={appIconLookup?.get(`${stack.appRegistryId}:${stack.packageId}`)}
                        size={32}
                      />
                    ) : (
                      <Avatar
                        sx={{
                          width: 32,
                          height: 32,
                          fontSize: "0.85rem",
                          fontWeight: "bold",
                          bgcolor: "grey.400",
                        }}
                      >
                        {(stack.stackName ?? "?").charAt(0).toUpperCase()}
                      </Avatar>
                    )}
                  </TableCell>

                  <TableCell>
                    {/* The row's link target — stretched across the whole row, see stretchedLinkSx.
                        Rendered as an anchor but styled like plain text, so the table keeps its
                        look and only the browser knows it's a link. */}
                    <Typography
                      component={Link}
                      to={detailPath}
                      fontWeight="medium"
                      sx={{
                        color: "inherit",
                        textDecoration: "none",
                        wordBreak: "break-word",
                        display: "block",
                        ...stretchedLinkSx,
                      }}
                    >
                      {stack.stackName}
                    </Typography>
                    <Typography
                      variant="caption"
                      color="text.secondary"
                      sx={{wordBreak: "break-word"}}
                    >
                      {stack.dockerProjectName}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    {isPackageSourceChip(stack) ? (
                      <Chip
                        label={getPackageDisplay(stack)}
                        size="small"
                        variant="outlined"
                        sx={{
                          fontSize: "0.7rem",
                          color: "text.secondary",
                          borderColor: "grey.400",
                        }}
                      />
                    ) : (
                      <Typography
                        variant="body2"
                        color="text.primary"
                        sx={{wordBreak: "break-word"}}
                      >
                        {getPackageDisplay(stack)}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell>
                    <Box sx={{display: "flex", alignItems: "center", gap: 1}}>
                      <Typography
                        variant="body2"
                        color={stack.packageVersion ? "text.primary" : "text.disabled"}
                      >
                        {stack.packageVersion ?? "–"}
                      </Typography>
                      {updateVersion && (
                        <Tooltip
                          title={`Update verfügbar: ${stack.packageVersion ?? "–"} → ${updateVersion}`}
                        >
                          <SystemUpdateAltIcon
                            fontSize="small"
                            sx={{
                              color: "warning.main",
                              opacity: 0.85,
                              flexShrink: 0,
                              // Above the row link, otherwise the tooltip never sees the hover
                              ...aboveRowLinkSx,
                            }}
                          />
                        </Tooltip>
                      )}
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Typography
                      variant="body2"
                      color={stack.appRegistryName ? "text.primary" : "text.disabled"}
                      sx={{wordBreak: "break-word"}}
                    >
                      {stack.appRegistryName ?? "–"}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <StackStatusChip status={stack.status} />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2">{getCreatedDisplay(stack.createdAt)}</Typography>
                  </TableCell>

                  {/* Actions — sit above the row link so they don't navigate */}
                  <TableCell align="right" sx={{whiteSpace: "nowrap", ...aboveRowLinkSx}}>
                    {rowBusy && <CircularProgress size={18} thickness={5} sx={{mx: 1}} />}
                    <Tooltip title={startDisabled ? "Stack läuft bereits" : "Starten"}>
                      <span>
                        <IconButton
                          size="small"
                          color="success"
                          onClick={() => onStart(stack.dockerProjectName!)}
                          disabled={rowBusy || startDisabled}
                        >
                          <PlayArrowIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>

                    <Tooltip title={stopDisabled ? "Stack ist bereits gestoppt" : "Stoppen"}>
                      <span>
                        <IconButton
                          size="small"
                          color="warning"
                          onClick={() => onStop(stack.dockerProjectName!)}
                          disabled={rowBusy || stopDisabled}
                        >
                          <StopIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>

                    <Tooltip title={restartDisabled ? "Nicht verfügbar" : "Neu starten"}>
                      <span>
                        <IconButton
                          size="small"
                          color="info"
                          onClick={() => onRestart(stack.dockerProjectName!)}
                          disabled={rowBusy || restartDisabled}
                        >
                          <RestartAltIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>

                    <Tooltip title="Löschen">
                      <span>
                        <IconButton
                          size="small"
                          color="error"
                          onClick={() => onDelete(stack.dockerProjectName!, stack.stackName!)}
                          disabled={rowBusy || mutateDisabled}
                        >
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>
                  </TableCell>
                </TableRow>

                {/* Expandable container sub-row */}
                {isExpanded && (
                  <ContainersSubRow stackId={stack.dockerProjectName!} colSpan={colSpan} />
                )}
              </React.Fragment>
            );
          })}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
