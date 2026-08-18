// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, IconButton, TextField, Tooltip, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import RemoveCircleOutlineIcon from "@mui/icons-material/RemoveCircleOutline";

export interface EnvEntry {
  key: string;
  value: string;
}

interface EnvEditorProps {
  entries: EnvEntry[];
  onChange: (entries: EnvEntry[]) => void;
  emptyLabel?: string;
}

export function EnvEditor({
  entries,
  onChange,
  emptyLabel = "Keine Variablen definiert. Klicke auf + um eine hinzuzufügen.",
}: EnvEditorProps) {
  const add = () => onChange([...entries, { key: "", value: "" }]);

  const remove = (idx: number) => onChange(entries.filter((_, i) => i !== idx));

  const update = (idx: number, field: "key" | "value", val: string) =>
    onChange(
      entries.map((entry, i) =>
        i === idx ? { ...entry, [field]: val } : entry,
      ),
    );

  return (
    <>
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          mb: 1,
        }}
      >
        <Typography variant="subtitle2">Environment-Variablen</Typography>
        <Tooltip title="Variable hinzufügen">
          <IconButton size="small" color="primary" onClick={add}>
            <AddIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Box>

      {entries.length === 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {emptyLabel}
        </Typography>
      )}

      {entries.map((entry, idx) => (
        <Box
          key={idx}
          sx={{ display: "flex", gap: 1, mb: 1, alignItems: "center" }}
        >
          <TextField
            label="Key"
            value={entry.key}
            onChange={(e) => update(idx, "key", e.target.value)}
            size="small"
            sx={{ flex: 1 }}
          />
          <TextField
            label="Value"
            value={entry.value}
            onChange={(e) => update(idx, "value", e.target.value)}
            size="small"
            sx={{ flex: 2 }}
          />
          <Tooltip title="Entfernen">
            <IconButton size="small" color="error" onClick={() => remove(idx)}>
              <RemoveCircleOutlineIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        </Box>
      ))}
    </>
  );
}
