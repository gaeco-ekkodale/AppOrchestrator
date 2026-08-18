// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useEffect, useState, type ReactNode } from "react";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  InputAdornment,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import InfoOutlinedIcon from "@mui/icons-material/InfoOutlined";
import VisibilityIcon from "@mui/icons-material/Visibility";
import VisibilityOffIcon from "@mui/icons-material/VisibilityOff";
import LinkIcon from "@mui/icons-material/Link";
import AddLinkIcon from "@mui/icons-material/AddLink";
import DataObjectIcon from "@mui/icons-material/DataObject";
import { useEnvSchema } from "@/features/registryClient/hooks/useEnvSchema";
import type { EnvSchemaField } from "@/features/registryClient/registryApiClient";
import { useAddSharedVariable } from "@/features/networks/hooks/useAddSharedVariable";
import type { NetworkSharedVariable } from "@/features/networks/sharedVariables";

// ─── Password field ───────────────────────────────────────────────────────────
function PasswordField({
  value,
  onChange,
  label,
  helperText,
  required,
  extraAdornment,
}: {
  value: string;
  onChange: (v: string) => void;
  label: string;
  helperText?: string;
  required?: boolean;
  extraAdornment?: ReactNode;
}) {
  const [show, setShow] = useState(false);
  return (
    <TextField
      label={label}
      type={show ? "text" : "password"}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      required={required}
      helperText={helperText}
      fullWidth
      slotProps={{
        input: {
          endAdornment: (
            <InputAdornment position="end">
              <IconButton
                size="small"
                onClick={() => setShow((s) => !s)}
                edge="end"
                aria-label={show ? "Passwort ausblenden" : "Passwort anzeigen"}
              >
                {show ? (
                  <VisibilityOffIcon fontSize="small" />
                ) : (
                  <VisibilityIcon fontSize="small" />
                )}
              </IconButton>
              {extraAdornment}
            </InputAdornment>
          ),
        },
      }}
    />
  );
}

// ─── Single schema field ──────────────────────────────────────────────────────
function SchemaField({
  field,
  value,
  onChange,
  sharedValue,
  showVarNames,
  onShare,
  shareHint,
}: {
  field: EnvSchemaField;
  value: string;
  onChange: (v: string) => void;
  sharedValue?: string;
  showVarNames: boolean;
  /** Set in expert mode: promotes this field to a shared variable of the environment. */
  onShare?: () => void;
  /** Tooltip for the share button; when set without onShare the button is disabled. */
  shareHint?: string;
}) {
  const isShared = sharedValue !== undefined;
  const displayValue = isShared ? sharedValue : value;
  const label = showVarNames ? field.name : field.label || field.name;
  const sharedHelperText =
    "Wird automatisch aus den Shared-Variablen des Environments übernommen";

  const shareButton = shareHint ? (
    <Tooltip title={shareHint}>
      <span>
        <IconButton
          size="small"
          edge="end"
          color="primary"
          onClick={onShare}
          disabled={!onShare}
          aria-label="Zu den geteilten Variablen hinzufügen"
        >
          <AddLinkIcon fontSize="small" />
        </IconButton>
      </span>
    </Tooltip>
  ) : undefined;

  if (field.type === "password" && !isShared)
    return (
      <PasswordField
        label={label}
        value={value}
        onChange={onChange}
        required={field.required}
        helperText={field.description}
        extraAdornment={shareButton}
      />
    );

  const endAdornment = isShared ? (
    <InputAdornment position="end">
      <Tooltip title="Shared Variable aus dem Environment">
        <LinkIcon fontSize="small" color="primary" />
      </Tooltip>
    </InputAdornment>
  ) : shareButton ? (
    <InputAdornment position="end">{shareButton}</InputAdornment>
  ) : undefined;

  return (
    <TextField
      label={label}
      type="text"
      value={displayValue}
      onChange={(e) => onChange(e.target.value)}
      required={!isShared && field.required}
      helperText={isShared ? sharedHelperText : field.description}
      fullWidth
      disabled={isShared}
      slotProps={endAdornment ? {input: {endAdornment}} : undefined}
    />
  );
}

// ─── Public component ─────────────────────────────────────────────────────────
export interface EnvSchemaFormProps {
  registryId: string;
  packageId: string;
  version: string;
  values: Record<string, string>;
  onChange: (values: Record<string, string>) => void;
  sharedVariables?: NetworkSharedVariable[];
  /**
   * Environment the stack is deployed to. Enables promoting single variables to that
   * environment's shared variables while the variable names are shown (expert mode).
   */
  networkName?: string;
}

export function EnvSchemaForm({
  registryId,
  packageId,
  version,
  values,
  onChange,
  sharedVariables = [],
  networkName,
}: EnvSchemaFormProps) {
  const sharedMap = Object.fromEntries(
    sharedVariables.map(({ key, value }) => [key, value]),
  );
  const [showVarNames, setShowVarNames] = useState(false);
  const [shareTarget, setShareTarget] = useState<EnvSchemaField | null>(null);
  const { schema, isLoading, error } = useEnvSchema(
    registryId,
    packageId,
    version,
  );
  const {
    add: addSharedVariable,
    canAdd: canShare,
    isPending: isSharing,
  } = useAddSharedVariable(networkName);

  // Pre-fill defaults when schema arrives
  useEffect(() => {
    if (schema.length > 0) {
      onChange(
        Object.fromEntries(
          schema
            // Shared fields are supplied by the environment. A local value for them would win
            // over the shared one when the env config is merged, so none is kept.
            .filter((f) => sharedMap[f.name] === undefined)
            .map((f) => [f.name, values[f.name] ?? String(f.default ?? "")]),
        ),
      );
    }
    // Only run when schema changes
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [schema]);

  const update = (name: string, val: string) =>
    onChange({ ...values, [name]: val });

  /** Tooltip for the share button, or undefined when the button shouldn't be rendered. */
  const shareHintFor = (field: EnvSchemaField): string | undefined => {
    if (!showVarNames || sharedMap[field.name] !== undefined) return undefined;
    if (!canShare) return "Erst ein Environment auswählen";
    return `${field.name} zu den geteilten Variablen von "${networkName}" hinzufügen`;
  };

  const confirmShare = () => {
    if (!shareTarget) return;
    const { name } = shareTarget;
    addSharedVariable(name, values[name] ?? "", () => {
      // The value now comes from the environment. Dropping the local copy keeps this stack
      // following the shared variable instead of pinning today's value (explicit values win
      // over shared ones when the env config is merged).
      const rest = { ...values };
      delete rest[name];
      onChange(rest);
      setShareTarget(null);
    });
  };

  return (
    <>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 1 }}>
        <Typography variant="h6" fontWeight="bold">
          Konfiguration
        </Typography>
        <Tooltip title="Felder aus dem .env.schema.yaml der App-Version">
          <InfoOutlinedIcon fontSize="small" sx={{ color: "text.secondary" }} />
        </Tooltip>
        <Tooltip
          title={showVarNames ? "Labels anzeigen" : "Variablennamen anzeigen"}
        >
          <IconButton
            size="small"
            onClick={() => setShowVarNames((v) => !v)}
            color={showVarNames ? "primary" : "default"}
            sx={{ ml: "auto" }}
          >
            <DataObjectIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Box>

      {isLoading && (
        <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
          <CircularProgress size={28} />
        </Box>
      )}

      {!!error && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          Schema konnte nicht geladen werden.
        </Alert>
      )}

      {!isLoading && !error && schema.length === 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Keine Environment-Variablen definiert.
        </Typography>
      )}

      {!isLoading && schema.length > 0 && (
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
          {schema.map((field) => {
            const shareHint = shareHintFor(field);
            return (
              <SchemaField
                key={field.name}
                field={field}
                value={values[field.name] ?? ""}
                onChange={(v) => update(field.name, v)}
                sharedValue={sharedMap[field.name]}
                showVarNames={showVarNames}
                shareHint={shareHint}
                onShare={
                  shareHint && canShare ? () => setShareTarget(field) : undefined
                }
              />
            );
          })}
        </Box>
      )}

      <Dialog
        open={shareTarget !== null}
        onClose={() => setShareTarget(null)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Zu den geteilten Variablen hinzufügen</DialogTitle>
        <DialogContent>
          <DialogContentText>
            <strong>{shareTarget?.name}</strong> wird als geteilte Variable im Environment{" "}
            <strong>{networkName}</strong> gespeichert und damit an alle Stacks dieses
            Environments vererbt – wirksam beim nächsten Deploy des jeweiligen Stacks.
          </DialogContentText>
          <Box
            sx={{
              mt: 2,
              p: 1.5,
              borderRadius: 1,
              bgcolor: "grey.100",
              fontFamily: "monospace",
              fontSize: "0.8rem",
              wordBreak: "break-all",
            }}
          >
            {shareTarget?.name}=
            {shareTarget?.type === "password"
              ? "••••••••"
              : (values[shareTarget?.name ?? ""] ?? "")}
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShareTarget(null)} disabled={isSharing}>
            Abbrechen
          </Button>
          <Button
            variant="contained"
            onClick={confirmShare}
            disabled={isSharing}
            startIcon={
              isSharing ? (
                <CircularProgress size={16} color="inherit" />
              ) : (
                <AddLinkIcon />
              )
            }
          >
            Hinzufügen
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

/** Returns true when all required fields are non-empty. Shared fields are skipped. */
export function isEnvSchemaValid(
  schema: EnvSchemaField[],
  values: Record<string, string>,
  sharedKeys: Set<string> = new Set(),
): boolean {
  return schema
    .filter((f) => f.required && !sharedKeys.has(f.name))
    .every((f) => String(values[f.name] ?? "").trim() !== "");
}

/** Re-export so callers don't need to import the hook separately. */
export { useEnvSchema };
