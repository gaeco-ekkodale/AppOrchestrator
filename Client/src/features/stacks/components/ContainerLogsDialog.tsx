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
  Button,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Typography,
} from "@mui/material";
import { useEffect, useMemo, useRef, useState, type UIEvent } from "react";
import {
  type ContainerDTO,
  type ContainerLogLineDTO,
} from "@/api/orchestrator";
import { useToast } from "@/features/shared/contexts/ToastContext";
import { useStackContainerLogsApi } from "@/features/stacks/hooks/useStackContainerLogsApi";

interface ContainerLogsDialogProps {
  open: boolean;
  projectName?: string;
  container?: ContainerDTO | null;
  pollIntervalMs?: number;
  onClose: () => void;
}

type LogRangeKey = "5m" | "15m" | "1h" | "6h" | "24h" | "all";

const LOG_RANGE_OPTIONS: Array<{
  key: LogRangeKey;
  label: string;
  minutes: number | null;
}> = [
  { key: "5m", label: "Letzte 5 Minuten", minutes: 5 },
  { key: "15m", label: "Letzte 15 Minuten", minutes: 15 },
  { key: "1h", label: "Letzte 1 Stunde", minutes: 60 },
  { key: "6h", label: "Letzte 6 Stunden", minutes: 360 },
  { key: "24h", label: "Letzte 24 Stunden", minutes: 1440 },
  { key: "all", label: "Alles (begrenzt)", minutes: null },
];

const INITIAL_TAIL = 600;
const INITIAL_LIMIT = 600;
const POLL_TAIL = 300;
const POLL_LIMIT = 300;

const getSinceForRange = (range: LogRangeKey): string | null => {
  const option = LOG_RANGE_OPTIONS.find((item) => item.key === range);
  if (!option || option.minutes === null) return null;

  return new Date(Date.now() - option.minutes * 60 * 1000).toISOString();
};

export function ContainerLogsDialog({
  open,
  projectName,
  container,
  pollIntervalMs = 3000,
  onClose,
}: ContainerLogsDialogProps) {
  const { showToast } = useToast();
  const { fetchContainerLogs } = useStackContainerLogsApi();
  const [logLines, setLogLines] = useState<ContainerLogLineDTO[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isPolling, setIsPolling] = useState(false);
  const [isAutoScrollEnabled, setIsAutoScrollEnabled] = useState(true);
  const [selectedRange, setSelectedRange] = useState<LogRangeKey>("15m");
  const logsScrollRef = useRef<HTMLDivElement | null>(null);
  const nextSinceRef = useRef<string | undefined>(undefined);

  const title = useMemo(() => {
    if (!container) return "Container Logs";
    return `Logs: ${container.service || container.name || container.id || "Container"}`;
  }, [container]);

  useEffect(() => {
    if (!open) {
      setLogLines([]);
      setIsLoading(false);
      setIsPolling(false);
      setIsAutoScrollEnabled(true);
      setSelectedRange("15m");
      nextSinceRef.current = undefined;
    }
  }, [open]);

  useEffect(() => {
    const containerId = container?.id;
    if (!open || !projectName || !containerId) return;

    let disposed = false;

    const lineKey = (line: ContainerLogLineDTO) =>
      `${line.timestamp ?? ""}|${line.stream ?? ""}|${line.message ?? ""}`;

    setLogLines([]);
    setIsAutoScrollEnabled(true);
    nextSinceRef.current = undefined;

    const poll = async (initial: boolean) => {
      try {
        if (initial) setIsLoading(true);
        else setIsPolling(true);

        const since = initial
          ? getSinceForRange(selectedRange)
          : (nextSinceRef.current ?? null);
        const tail = initial ? INITIAL_TAIL : POLL_TAIL;
        const limit = initial ? INITIAL_LIMIT : POLL_LIMIT;

        const response = await fetchContainerLogs({
          projectName,
          containerId,
          since,
          tail,
          limit,
        });

        if (disposed) return;

        const incoming = response.lines ?? [];
        if (incoming.length > 0) {
          setLogLines((prev) => {
            const seen = new Set(prev.map(lineKey));
            const additions = incoming.filter(
              (line) => !seen.has(lineKey(line)),
            );
            return [...prev, ...additions];
          });
        }

        if (response.nextSince) {
          nextSinceRef.current = response.nextSince;
        }
      } catch {
        if (!disposed && initial) {
          showToast("Logs konnten nicht geladen werden", "error");
        }
      } finally {
        if (!disposed) {
          setIsLoading(false);
          setIsPolling(false);
        }
      }
    };

    void poll(true);
    const intervalId = window.setInterval(() => {
      void poll(false);
    }, pollIntervalMs);

    return () => {
      disposed = true;
      window.clearInterval(intervalId);
    };
  }, [
    open,
    projectName,
    container?.id,
    pollIntervalMs,
    selectedRange,
    fetchContainerLogs,
    showToast,
  ]);

  useEffect(() => {
    if (!isAutoScrollEnabled) return;

    const scrollContainer = logsScrollRef.current;
    if (!scrollContainer) return;

    scrollContainer.scrollTop = scrollContainer.scrollHeight;
  }, [logLines, isAutoScrollEnabled]);

  const handleScroll = (event: UIEvent<HTMLDivElement>) => {
    const target = event.currentTarget;
    const distanceToBottom =
      target.scrollHeight - target.scrollTop - target.clientHeight;
    setIsAutoScrollEnabled(distanceToBottom < 24);
  };

  const scrollToEnd = () => {
    const scrollContainer = logsScrollRef.current;
    if (scrollContainer) {
      scrollContainer.scrollTop = scrollContainer.scrollHeight;
    }
    setIsAutoScrollEnabled(true);
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="lg">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent dividers>
        <Box
          sx={{
            mb: 1.5,
            display: "flex",
            justifyContent: "space-between",
            gap: 1,
            flexWrap: "wrap",
          }}
        >
          <FormControl size="small" sx={{ minWidth: 220 }}>
            <InputLabel id="logs-range-label">Zeitraum</InputLabel>
            <Select
              labelId="logs-range-label"
              value={selectedRange}
              label="Zeitraum"
              onChange={(event) =>
                setSelectedRange(event.target.value as LogRangeKey)
              }
            >
              {LOG_RANGE_OPTIONS.map((option) => (
                <MenuItem key={option.key} value={option.key}>
                  {option.label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ alignSelf: "center" }}
          >
            Große Logs werden serverseitig begrenzt geladen.
          </Typography>
        </Box>

        {isLoading ? (
          <Box sx={{ display: "flex", alignItems: "center", gap: 1, py: 1 }}>
            <CircularProgress size={16} />
            <Typography variant="body2" color="text.secondary">
              Logs werden geladen …
            </Typography>
          </Box>
        ) : (
          <Box
            ref={logsScrollRef}
            onScroll={handleScroll}
            sx={{
              bgcolor: "grey.900",
              color: "grey.100",
              borderRadius: 1,
              p: 1.5,
              minHeight: 280,
              maxHeight: 520,
              overflowY: "auto",
              fontFamily: "monospace",
              fontSize: "0.8rem",
              whiteSpace: "pre-wrap",
              wordBreak: "break-word",
            }}
          >
            {logLines.length === 0 ? (
              <Typography variant="body2" color="grey.400">
                Keine Logs vorhanden.
              </Typography>
            ) : (
              logLines.map((line, index) => (
                <Box key={`${line.timestamp}-${line.stream}-${index}`}>
                  [{line.timestamp}] {line.message}
                </Box>
              ))
            )}
          </Box>
        )}

        <Box sx={{ mt: 1, display: "flex", justifyContent: "space-between" }}>
          <Typography variant="caption" color="text.secondary">
            Polling: {isPolling ? "aktiv" : "idle"} · Auto-Scroll:{" "}
            {isAutoScrollEnabled ? "an" : "aus"}
          </Typography>
          <Box sx={{ display: "flex", gap: 1 }}>
            <Button size="small" variant="outlined" onClick={scrollToEnd}>
              Zum Ende
            </Button>
            <Button onClick={onClose} size="small" variant="outlined">
              Schließen
            </Button>
          </Box>
        </Box>
      </DialogContent>
    </Dialog>
  );
}
