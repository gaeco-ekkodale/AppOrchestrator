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
  Alert,
  Box,
  Chip,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  List,
  ListItemButton,
  ListItemText,
  Typography,
} from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import StorageIcon from "@mui/icons-material/Storage";
import type { AppWithRegistry } from "@/features/registryClient/registryApiClient";
import type { ApplicationVersionDTO } from "@/features/registryClient/registryApiClient";
import { AppIcon } from "./AppIcon";
import { useAppVersions } from "@/features/registryClient/hooks/useAppVersions";
import { VersionBadges } from "./VersionBadges";
import { VersionDetailPanel } from "./VersionDetailPanel";

interface AppStoreModalProps {
  app: AppWithRegistry | null;
  onClose: () => void;
}

export function AppStoreModal({ app, onClose }: AppStoreModalProps) {
  const { versions, isLoading, error } = useAppVersions(
    app?.registryId ?? "",
    app?.packageId ?? "",
  );

  const [selected, setSelected] = useState<ApplicationVersionDTO | null>(null);

  // Always select the latest (first) version when versions load or app changes
  useEffect(() => {
    setSelected(versions.length > 0 ? versions[0] : null);
  }, [versions]);

  return (
    <Dialog
      open={!!app}
      onClose={onClose}
      maxWidth="lg"
      fullWidth
      PaperProps={{
        sx: { borderRadius: 3, overflow: "hidden", height: "80vh" },
      }}
    >
      {app && (
        <>
          {/* ── Header ─────────────────────────────────────────────── */}
          <DialogTitle
            sx={{
              bgcolor: "grey.50",
              borderBottom: "1px solid",
              borderColor: "divider",
              px: 3,
              py: 2,
              pr: 7,
            }}
          >
            <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
              <AppIcon name={app.name} iconUrl={app.iconUrl} size={48} />
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="h6" fontWeight="bold" noWrap>
                  {app.name}
                </Typography>
                <Box
                  sx={{
                    display: "flex",
                    alignItems: "center",
                    gap: 1,
                    mt: 0.25,
                  }}
                >
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    noWrap
                    title={app.packageId}
                  >
                    {app.packageId}
                  </Typography>
                  {app.ownerUsername && (
                    <Typography variant="caption" color="text.secondary">
                      · von {app.ownerUsername}
                    </Typography>
                  )}
                  <Chip
                    icon={
                      <StorageIcon sx={{ fontSize: "0.8rem !important" }} />
                    }
                    label={app.registryName}
                    size="small"
                    color="primary"
                    variant="outlined"
                    sx={{ ml: 0.5 }}
                  />
                </Box>
              </Box>
            </Box>
            <IconButton
              onClick={onClose}
              size="small"
              sx={{ position: "absolute", right: 14, top: 14 }}
              aria-label="Schließen"
            >
              <CloseIcon fontSize="small" />
            </IconButton>
          </DialogTitle>

          {/* ── Content: split layout ──────────────────────────────── */}
          <DialogContent
            sx={{ p: 0, display: "flex", overflow: "hidden", flex: 1 }}
          >
            {isLoading ? (
              <Box
                sx={{
                  display: "flex",
                  justifyContent: "center",
                  alignItems: "center",
                  width: "100%",
                  py: 6,
                }}
              >
                <CircularProgress size={32} />
              </Box>
            ) : error ? (
              <Box sx={{ p: 3, width: "100%" }}>
                <Alert severity="error">
                  Versionen konnten nicht geladen werden.
                </Alert>
              </Box>
            ) : versions.length === 0 ? (
              <Box sx={{ p: 3, width: "100%" }}>
                <Typography variant="body2" color="text.secondary">
                  Keine Versionen verfügbar.
                </Typography>
              </Box>
            ) : (
              <>
                {/* ── Left: version list ────────────────────────────── */}
                <Box
                  sx={{
                    width: 240,
                    minWidth: 240,
                    borderRight: "1px solid",
                    borderColor: "divider",
                    overflow: "auto",
                  }}
                >
                  <Typography
                    variant="overline"
                    sx={{
                      px: 2,
                      pt: 2,
                      pb: 0.5,
                      display: "block",
                      color: "text.secondary",
                    }}
                  >
                    Versionen ({versions.length})
                  </Typography>
                  <List dense disablePadding>
                    {versions.map((v) => (
                      <ListItemButton
                        key={v.version}
                        selected={selected?.version === v.version}
                        onClick={() => setSelected(v)}
                        sx={{
                          px: 2,
                          py: 1,
                          borderLeft: "3px solid",
                          borderColor:
                            selected?.version === v.version
                              ? "primary.main"
                              : "transparent",
                        }}
                      >
                        <ListItemText
                          primary={
                            <Box
                              sx={{
                                display: "flex",
                                alignItems: "center",
                                gap: 0.75,
                              }}
                            >
                              <Typography
                                variant="body2"
                                fontWeight={
                                  selected?.version === v.version
                                    ? "bold"
                                    : "medium"
                                }
                                noWrap
                              >
                                v{v.version}
                              </Typography>
                              <VersionBadges version={v} />
                            </Box>
                          }
                          secondary={new Date(v.createdAt).toLocaleDateString(
                            "de-DE",
                          )}
                          secondaryTypographyProps={{
                            variant: "caption",
                          }}
                        />
                      </ListItemButton>
                    ))}
                  </List>
                </Box>

                {/* ── Right: version details ────────────────────────── */}
                <Box
                  sx={{
                    flex: 1,
                    overflow: "auto",
                    p: 3,
                    display: "flex",
                    flexDirection: "column",
                  }}
                >
                  {selected ? (
                    <VersionDetailPanel
                      version={selected}
                      registryId={app.registryId}
                      registryUrl={app.registryBaseUrl}
                      registryName={app.registryName}
                      appName={app.name}
                      iconUrl={app.iconUrl}
                      repositoryUrl={app.repositoryUrl}
                      documentationUrl={app.documentationUrl}
                    />
                  ) : (
                    <Typography
                      variant="body2"
                      color="text.secondary"
                      sx={{ py: 4, textAlign: "center" }}
                    >
                      Wähle eine Version aus.
                    </Typography>
                  )}
                </Box>
              </>
            )}
          </DialogContent>
        </>
      )}
    </Dialog>
  );
}
