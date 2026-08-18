// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {
  Box,
  Chip,
  CircularProgress,
  IconButton,
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
import ArticleIcon from "@mui/icons-material/Article";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import type { ContainerDTO } from "@/api/orchestrator";

interface CompactStackContainersTableProps {
  containers: ContainerDTO[];
  busyContainerId: string | null;
  canManageContainers: boolean;
  /** Show the Ports column. Enable on the detail page, disable in the overview list. */
  showPorts?: boolean;
  getStateColor: (
    state?: string,
  ) => "success" | "error" | "warning" | "default";
  onStart: (containerId?: string) => void;
  onStop: (containerId?: string) => void;
  onLogs: (container: ContainerDTO) => void;
}

/** Strip the IP/interface prefix from a Docker port string.
 *  e.g. "0.0.0.0:8080->80/tcp" → "8080->80/tcp"
 *       ":::443->443/tcp"       → "443->443/tcp"
 *       "80/tcp"                → "80/tcp"  (no public port, unchanged)
 */
function stripPortIp(port: string): string {
  return port.replace(/^.*?(\d+->)/, "$1");
}

export function CompactStackContainersTable({
  containers,
  busyContainerId,
  canManageContainers,
  showPorts = false,
  getStateColor,
  onStart,
  onStop,
  onLogs,
}: CompactStackContainersTableProps) {
  return (
    <TableContainer
      sx={{ width: "100%", maxWidth: "100%", overflowX: "hidden" }}
    >
      {/*
       * Column widths mirror the parent StacksTable so that when this table is
       * rendered as a sub-row the columns align visually:
       *   40px  | 44px | minWidth:220 | minWidth:170 (colSpan=3 covers +130 +170) | 10% | 9% | 188px
       *  expand   icon    Name/Service   Package/Image                               Status  Ports  Actions
       */}
      <Table size="small" sx={{ width: "100%", tableLayout: "auto" }}>
        <TableHead>
          <TableRow
            sx={{
              "& th": {
                fontWeight: 600,
                color: "text.secondary",
                fontSize: "0.75rem",
              },
            }}
          >
            <TableCell sx={{ width: 40, pr: 0 }} />
            <TableCell sx={{ width: 44, pl: 1, pr: 0 }} />
            <TableCell sx={{ minWidth: 220, pl: 2 }}>Service</TableCell>
            <TableCell sx={{ minWidth: 170 }} colSpan={3}>
              Image
            </TableCell>
            <TableCell sx={{ width: "10%" }}>Status</TableCell>
            <TableCell sx={{ width: "9%" }}>
              {showPorts ? "Ports" : ""}
            </TableCell>
            <TableCell sx={{ width: 188 }} align="right">
              Controls
            </TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {containers.map((container) => (
            <TableRow key={container.id} hover>
              <TableCell sx={{ width: 40, pr: 0 }} />
              <TableCell sx={{ width: 44, pl: 1, pr: 0 }} />
              <TableCell sx={{ overflow: "hidden", pl: 2 }}>
                <Typography variant="body2" noWrap>
                  {container.service ?? "–"}
                </Typography>
              </TableCell>
              <TableCell sx={{ overflow: "hidden" }} colSpan={3}>
                <Typography
                  variant="body2"
                  noWrap
                  sx={{
                    fontFamily: "monospace",
                    fontSize: "0.75rem",
                    color: "text.secondary",
                  }}
                >
                  {container.image ?? "–"}
                </Typography>
              </TableCell>
              <TableCell>
                <Chip
                  label={container.state ?? "unknown"}
                  size="small"
                  color={getStateColor(container.state)}
                  variant="outlined"
                  sx={{ fontSize: "0.7rem", height: 20 }}
                />
              </TableCell>
              <TableCell sx={{ overflow: "hidden" }}>
                {showPorts &&
                  (container.ports ?? []).map((p, i) => (
                    <Typography
                      key={i}
                      variant="caption"
                      noWrap
                      sx={{
                        fontFamily: "monospace",
                        fontSize: "0.7rem",
                        color: "text.secondary",
                        display: "block",
                      }}
                    >
                      {stripPortIp(p)}
                    </Typography>
                  ))}
              </TableCell>
              <TableCell align="right">
                <Box
                  sx={{ display: "flex", justifyContent: "flex-end", gap: 0.5 }}
                >
                  {busyContainerId === container.id && (
                    <CircularProgress size={14} sx={{ mt: 0.5, mr: 0.5 }} />
                  )}

                  <Tooltip
                    title={
                      container.state?.toLowerCase() === "running"
                        ? "Container läuft bereits"
                        : "Starten"
                    }
                  >
                    <span>
                      <IconButton
                        size="small"
                        color="success"
                        disabled={
                          !canManageContainers ||
                          busyContainerId === container.id ||
                          container.state?.toLowerCase() === "running"
                        }
                        onClick={() => onStart(container.id)}
                      >
                        <PlayArrowIcon fontSize="small" />
                      </IconButton>
                    </span>
                  </Tooltip>

                  <Tooltip
                    title={
                      container.state?.toLowerCase() === "running"
                        ? "Stoppen"
                        : "Container ist bereits gestoppt"
                    }
                  >
                    <span>
                      <IconButton
                        size="small"
                        color="warning"
                        disabled={
                          !canManageContainers ||
                          busyContainerId === container.id ||
                          container.state?.toLowerCase() !== "running"
                        }
                        onClick={() => onStop(container.id)}
                      >
                        <StopIcon fontSize="small" />
                      </IconButton>
                    </span>
                  </Tooltip>

                  <Tooltip title="Logs anzeigen">
                    <span>
                      <IconButton
                        size="small"
                        color="info"
                        disabled={!canManageContainers || !container.id}
                        onClick={() => onLogs(container)}
                      >
                        <ArticleIcon fontSize="small" />
                      </IconButton>
                    </span>
                  </Tooltip>

                  {container.traefikUrl &&
                  container.state?.toLowerCase() === "running" ? (
                    <Tooltip title={container.traefikUrl}>
                      <IconButton
                        size="small"
                        color="primary"
                        onClick={() =>
                          window.open(
                            container.traefikUrl!,
                            "_blank",
                            "noopener,noreferrer",
                          )
                        }
                      >
                        <OpenInNewIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  ) : (
                    <IconButton size="small" sx={{ visibility: "hidden" }}>
                      <OpenInNewIcon fontSize="small" />
                    </IconButton>
                  )}
                </Box>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
