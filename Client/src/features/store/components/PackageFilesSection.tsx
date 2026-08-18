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
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  Tooltip,
  Typography,
} from "@mui/material";
import DownloadIcon from "@mui/icons-material/Download";
import VisibilityIcon from "@mui/icons-material/Visibility";
import type { PackageFileDTO } from "@/features/registryClient/registryApiClient";
import { downloadRegistryFile, useTextFile } from "../hooks/useTextFile";
import { useToast } from "@/features/shared/contexts/ToastContext";

interface PackageFilesSectionProps {
  files: PackageFileDTO[];
}

/**
 * Lists the data files a package ships and mounts into its containers, with download and a
 * plain-text preview so the content can be inspected before it is deployed.
 */
export function PackageFilesSection({ files }: PackageFilesSectionProps) {
  const [previewFile, setPreviewFile] = useState<PackageFileDTO | null>(null);
  const { showToast } = useToast();

  const handleDownload = (file: PackageFileDTO) =>
    downloadRegistryFile(file.downloadUrl, file.name).catch(() =>
      showToast(`${file.name} konnte nicht geladen werden`, "error"),
    );

  if (files.length === 0) return null;

  return (
    <>
      <Divider sx={{ mb: 1.5 }} />
      <Typography variant="subtitle2" fontWeight="bold" sx={{ mb: 0.5 }}>
        Paket-Dateien
      </Typography>
      <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 1 }}>
        Werden beim Deployen entpackt und in die Container gemountet.
      </Typography>

      <Box sx={{ mb: 2 }}>
        {files.map((file) => (
          <Box
            key={file.name}
            sx={{
              display: "flex",
              alignItems: "flex-start",
              gap: 1,
              py: 0.5,
            }}
          >
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography
                variant="body2"
                sx={{ fontFamily: "monospace", wordBreak: "break-all" }}
              >
                {file.name}
              </Typography>
              {file.description && (
                <Typography variant="caption" color="text.secondary">
                  {file.description}
                </Typography>
              )}
            </Box>

            <Tooltip title="Inhalt ansehen">
              <IconButton size="small" onClick={() => setPreviewFile(file)}>
                <VisibilityIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Herunterladen">
              <IconButton size="small" onClick={() => handleDownload(file)}>
                <DownloadIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Box>
        ))}
      </Box>

      <PackageFilePreviewDialog
        file={previewFile}
        onClose={() => setPreviewFile(null)}
      />
    </>
  );
}

const MAX_PREVIEW_LENGTH = 1_000_000;

function PackageFilePreviewDialog({
  file,
  onClose,
}: {
  file: PackageFileDTO | null;
  onClose: () => void;
}) {
  const { data, isLoading, error } = useTextFile(file?.downloadUrl, !!file);

  const truncated = !!data && data.length > MAX_PREVIEW_LENGTH;
  const content = truncated ? data.slice(0, MAX_PREVIEW_LENGTH) : data;

  return (
    <Dialog open={!!file} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle sx={{ fontFamily: "monospace", fontSize: "1rem" }}>
        {file?.name}
      </DialogTitle>
      <DialogContent dividers>
        {isLoading ? (
          <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
            <CircularProgress size={24} />
          </Box>
        ) : error ? (
          <Typography variant="body2" color="error">
            Datei konnte nicht geladen werden.
          </Typography>
        ) : (
          <>
            {truncated && (
              <Typography variant="caption" color="text.secondary">
                Nur die ersten {MAX_PREVIEW_LENGTH.toLocaleString("de-DE")} Zeichen -
                für den vollen Inhalt herunterladen.
              </Typography>
            )}
            {/* Rendered as plain text: package files come from whoever published the package. */}
            <Box
              component="pre"
              sx={{
                m: 0,
                mt: truncated ? 1 : 0,
                fontSize: "0.75rem",
                whiteSpace: "pre-wrap",
                wordBreak: "break-word",
                maxHeight: "60vh",
                overflow: "auto",
              }}
            >
              {content}
            </Box>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
