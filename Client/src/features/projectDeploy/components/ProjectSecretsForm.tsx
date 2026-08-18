// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useEffect, useState } from "react";
import {
  Box,
  Typography,
  TextField,
  Alert,
  CircularProgress,
  InputAdornment,
  IconButton,
  Tooltip,
} from "@mui/material";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import VisibilityIcon from "@mui/icons-material/Visibility";
import VisibilityOffIcon from "@mui/icons-material/VisibilityOff";
import { aggregateEnvSchemas, type AggregatedField } from "../utils/envAggregation";
import { useMergedEnvSchemas, type AppWithSchema } from "../hooks/useMergedEnvSchemas";

export interface ProjectSecretsFormProps {
  apps: AppWithSchema[];
  values: Record<string, string>;
  onChange: (values: Record<string, string>) => void;
  sharedVarKeys: Set<string>;
}

export function ProjectSecretsForm({
  apps,
  values,
  onChange,
  sharedVarKeys,
}: ProjectSecretsFormProps) {
  const { schemas, isLoading, error } = useMergedEnvSchemas(apps);
  const [aggregatedFields, setAggregatedFields] = useState<AggregatedField[]>([]);
  const [showPasswords, setShowPasswords] = useState<Record<string, boolean>>({});

  useEffect(() => {
    if (Object.keys(schemas).length > 0) {
      const aggregated = aggregateEnvSchemas(schemas, sharedVarKeys);
      setAggregatedFields(aggregated);
    }
  }, [schemas, sharedVarKeys]);

  const handleFieldChange = (fieldName: string, value: string) => {
    onChange({ ...values, [fieldName]: value });
  };

  const toggleShowPassword = (fieldName: string) => {
    setShowPasswords((prev) => ({
      ...prev,
      [fieldName]: !prev[fieldName],
    }));
  };

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
        <CircularProgress size={28} />
      </Box>
    );
  }

  if (error) {
    return (
      <Alert severity="warning">
        Env-Schemas konnten nicht geladen werden: {error}
      </Alert>
    );
  }

  if (aggregatedFields.length === 0) {
    return (
      <Alert severity="info">
        Alle Env-Variablen haben Defaults oder werden vom Environment bereitgestellt.
        Keine zusätzliche Konfiguration erforderlich.
      </Alert>
    );
  }

  return (
    <Box>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 2 }}>
        <Typography variant="h6" fontWeight="bold">
          Erforderliche Einstellungen
        </Typography>
        <Tooltip title="Nur Felder ohne Standard-Werte und nicht vom Environment bereitgestellt">
          <InfoOutlinedIcon fontSize="small" sx={{ color: "text.secondary" }} />
        </Tooltip>
      </Box>

      <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
        {aggregatedFields.map((field) => (
          <Box key={field.name}>
            <TextField
              label={field.label}
              type={field.type === "password" && !showPasswords[field.name] ? "password" : "text"}
              value={values[field.name] ?? ""}
              onChange={(e) => handleFieldChange(field.name, e.target.value)}
              required={field.required}
              helperText={field.description}
              fullWidth
              size="small"
              slotProps={{
                input:
                  field.type === "password"
                    ? {
                        endAdornment: (
                          <InputAdornment position="end">
                            <IconButton
                              size="small"
                              onClick={() => toggleShowPassword(field.name)}
                              edge="end"
                              aria-label={
                                showPasswords[field.name]
                                  ? "Passwort ausblenden"
                                  : "Passwort anzeigen"
                              }
                            >
                              {showPasswords[field.name] ? (
                                <VisibilityOffIcon fontSize="small" />
                              ) : (
                                <VisibilityIcon fontSize="small" />
                              )}
                            </IconButton>
                          </InputAdornment>
                        ),
                      }
                    : undefined,
              }}
            />

            {field.appliesTo.length > 1 && (
              <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5, display: "block" }}>
                Gilt für: {field.appliesTo.join(", ")}
              </Typography>
            )}
          </Box>
        ))}
      </Box>
    </Box>
  );
}
