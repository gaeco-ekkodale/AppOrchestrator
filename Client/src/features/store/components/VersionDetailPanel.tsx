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
  Divider,
  Tooltip,
  Typography,
} from "@mui/material";
import CloudUploadIcon from "@mui/icons-material/CloudUpload";
import CloudDownloadIcon from "@mui/icons-material/CloudDownload";
import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import OpenInNewIcon from "@mui/icons-material/OpenInNew";
import AddShoppingCartIcon from "@mui/icons-material/AddShoppingCart";
import CheckCircleOutlineIcon from "@mui/icons-material/CheckCircleOutline";
import { Link } from "react-router-dom";
import ReactMarkdown from "react-markdown";
import type { ApplicationVersionDTO } from "@/features/registryClient/registryApiClient";
import { VersionBadges } from "./VersionBadges";
import { PackageFilesSection } from "./PackageFilesSection";
import { createRoute } from "@/utils/routing";
import { useTextFile } from "../hooks/useTextFile";
import { useProjectBasket } from "@/features/projectDeploy/context/ProjectBasketContext";
import { makeProjectApp } from "@/features/projectDeploy/types";

interface VersionDetailPanelProps {
  version: ApplicationVersionDTO;
  registryId: string;
  registryUrl: string;
  registryName: string;
  appName: string;
  iconUrl?: string | null;
  repositoryUrl?: string | null;
  documentationUrl?: string | null;
}

export function VersionDetailPanel({
  version: v,
  registryId,
  registryUrl,
  registryName,
  appName,
  iconUrl,
  repositoryUrl,
  documentationUrl,
}: VersionDetailPanelProps) {
  const { data: readme, isLoading: readmeLoading } = useTextFile(v.readmeUrl);
  const basket = useProjectBasket();
  const appId = `${v.packageId ?? ""}:${v.version}`;
  const inBasket = basket.has(appId);

  const handleAddToBasket = () => {
    basket.addApp(
      makeProjectApp({
        registryId,
        registryUrl,
        registryName,
        packageId: v.packageId ?? "",
        name: appName,
        iconUrl: iconUrl ?? undefined,
        version: v.version,
      }),
    );
  };

  return (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        height: "100%",
        minHeight: 0,
      }}
    >
      {/* ── Version header ─────────────────────────────────────── */}
      <Box sx={{ mb: 2 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 0.5 }}>
          <Typography variant="h5" fontWeight="bold">
            v{v.version}
          </Typography>
          <VersionBadges version={v} />
        </Box>

        <Box sx={{ display: "flex", gap: 2, alignItems: "center" }}>
          <Tooltip title="Downloads">
            <Box sx={{ display: "flex", alignItems: "center", gap: 0.5 }}>
              <CloudDownloadIcon
                fontSize="small"
                sx={{ color: "text.disabled" }}
              />
              <Typography variant="caption" color="text.secondary">
                {v.downloads}
              </Typography>
            </Box>
          </Tooltip>
          <Typography variant="caption" color="text.secondary">
            {new Date(v.createdAt).toLocaleDateString("de-DE")}
          </Typography>
        </Box>
      </Box>

      {/* ── Description ────────────────────────────────────────── */}
      {v.description && (
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ mb: 2, lineHeight: 1.6 }}
        >
          {v.description}
        </Typography>
      )}

      {/* ── Links ──────────────────────────────────────────────── */}
      {(repositoryUrl || documentationUrl) && (
        <Box sx={{ display: "flex", gap: 1, mb: 2, flexWrap: "wrap" }}>
          {repositoryUrl && (
            <Button
              size="small"
              variant="outlined"
              startIcon={<OpenInNewIcon />}
              href={repositoryUrl}
              target="_blank"
              rel="noopener noreferrer"
            >
              Repository
            </Button>
          )}
          {documentationUrl && (
            <Button
              size="small"
              variant="outlined"
              startIcon={<OpenInNewIcon />}
              href={documentationUrl}
              target="_blank"
              rel="noopener noreferrer"
            >
              Dokumentation
            </Button>
          )}
        </Box>
      )}

      {/* ── Dependencies ───────────────────────────────────────── */}
      {v.dependencies && v.dependencies.length > 0 && (
        <>
          <Divider sx={{ mb: 1.5 }} />
          <Typography variant="subtitle2" fontWeight="bold" sx={{ mb: 0.5 }}>
            Abhängigkeiten
          </Typography>
          <Box sx={{ mb: 2 }}>
            {v.dependencies.map((dep: { name: string; version: string }) => (
              <Typography
                key={`${dep.name}-${dep.version}`}
                variant="body2"
                color="text.secondary"
              >
                {dep.name} v{dep.version}
              </Typography>
            ))}
          </Box>
        </>
      )}

      {/* ── Package files ──────────────────────────────────────── */}
      <PackageFilesSection files={v.packageFiles ?? []} />

      {/* ── Readme ─────────────────────────────────────────────── */}
      {v.readmeUrl && (
        <>
          <Divider sx={{ mb: 1.5 }} />
          <Typography variant="subtitle2" fontWeight="bold" sx={{ mb: 1 }}>
            Readme
          </Typography>
          <Box
            sx={{
              flex: 1,
              minHeight: 0,
              overflow: "auto",
              bgcolor: "grey.50",
              borderRadius: 1.5,
              p: 2,
              mb: 2,
              "& img": { maxWidth: "100%" },
              "& pre": {
                bgcolor: "grey.200",
                p: 1.5,
                borderRadius: 1,
                overflow: "auto",
                fontSize: "0.85rem",
              },
              "& code": {
                fontSize: "0.85rem",
                bgcolor: "grey.200",
                px: 0.5,
                borderRadius: 0.5,
              },
              "& pre code": { bgcolor: "transparent", p: 0 },
              "& h1": { fontSize: "1.25rem", mt: 2, mb: 1 },
              "& h2": { fontSize: "1.1rem", mt: 1.5, mb: 0.75 },
              "& h3": { fontSize: "1rem", mt: 1, mb: 0.5 },
              "& p": { mb: 1, lineHeight: 1.6 },
              "& ul, & ol": { pl: 3, mb: 1 },
            }}
          >
            {readmeLoading ? (
              <Box sx={{ display: "flex", justifyContent: "center", py: 3 }}>
                <CircularProgress size={24} />
              </Box>
            ) : readme ? (
              <ReactMarkdown>{readme}</ReactMarkdown>
            ) : (
              <Typography variant="body2" color="text.secondary">
                Readme konnte nicht geladen werden.
              </Typography>
            )}
          </Box>
        </>
      )}

      {/* ── Spacer pushes button to bottom ─────────────────────── */}
      <Box sx={{ flex: v.readmeUrl ? 0 : 1 }} />

      {/* ── Action buttons ─────────────────────────────────────── */}
      <Box sx={{ display: "flex", justifyContent: "flex-end", gap: 1, pt: 1, flexWrap: "wrap" }}>
        <Tooltip title={inBasket ? "Bereits im Deployment-Warenkorb" : "Zum Projekt-Deployment hinzufügen"}>
          <span>
            <Button
              variant="outlined"
              color={inBasket ? "success" : "secondary"}
              size="large"
              startIcon={inBasket ? <CheckCircleOutlineIcon /> : <AddShoppingCartIcon />}
              onClick={handleAddToBasket}
              disabled={inBasket}
            >
              {inBasket ? "Hinzugefügt ✓" : "Hinzufügen"}
            </Button>
          </span>
        </Tooltip>
        <Tooltip title={v.isDeprecated ? "Diese Version ist veraltet" : ""}>
          <span>
            <Button
              component={Link}
              to={createRoute(
                `/store/configure/${encodeURIComponent(registryId)}/${encodeURIComponent(v.packageId ?? "")}/${encodeURIComponent(v.version ?? "")}`,
              )}
              variant={v.isDeprecated ? "outlined" : "contained"}
              color={v.isDeprecated ? "warning" : "primary"}
              size="large"
              startIcon={
                v.isDeprecated ? <WarningAmberIcon /> : <CloudUploadIcon />
              }
            >
              Konfigurieren & Deployen
            </Button>
          </span>
        </Tooltip>
      </Box>
    </Box>
  );
}
