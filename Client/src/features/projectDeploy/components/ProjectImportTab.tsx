// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useRef } from "react";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Button,
  Typography,
} from "@mui/material";
import AccountTreeIcon from "@mui/icons-material/AccountTree";
import UploadIcon from "@mui/icons-material/Upload";
import { parseBlueprint, validateBlueprintStructure } from "../utils/blueprintUtils";
import { useToast } from "@/features/shared/contexts/ToastContext";
import { createRoute } from "@/utils/routing";

export function ProjectImportTab() {
  const navigate = useNavigate();
  const { showToast } = useToast();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleImport = () => fileInputRef.current?.click();

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const content = e.target?.result as string;
        const blueprint = parseBlueprint(content);
        const errors = validateBlueprintStructure(blueprint);

        if (errors.length > 0) {
          showToast(`Blueprint-Fehler: ${errors.join(", ")}`, "error");
          return;
        }

        navigate(createRoute("/store/project-deploy"), {
          state: { blueprint },
        });
      } catch (err) {
        showToast(
          `Import-Fehler: ${err instanceof Error ? err.message : "Unbekannter Fehler"}`,
          "error",
        );
      }
    };

    reader.readAsText(file);
    event.target.value = "";
  };

  return (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        gap: 2,
        py: 4,
        textAlign: "center",
      }}
    >
      <AccountTreeIcon sx={{ fontSize: 48, color: "text.secondary" }} />
      <Box>
        <Typography variant="h6" fontWeight="bold" gutterBottom>
          Projekt-Blueprint importieren
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ maxWidth: 440 }}>
          Importiere eine Blueprint-YAML-Datei, um mehrere Apps mit vordefinierten
          Deployment-Schritten schnell zu deployen. Blueprint-Dateien enthalten keine Secrets —
          diese werden im Wizard abgefragt.
        </Typography>
      </Box>

      <Button variant="contained" startIcon={<UploadIcon />} onClick={handleImport}>
        Blueprint importieren
      </Button>

      <input
        ref={fileInputRef}
        type="file"
        accept=".yaml,.yml"
        onChange={handleFileChange}
        style={{ display: "none" }}
      />
    </Box>
  );
}
