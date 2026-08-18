// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { createTheme } from "@mui/material/styles";

export const appTheme = createTheme({
  shape: {
    borderRadius: 12,
  },
  typography: {
    h5: {
      fontWeight: 700,
      letterSpacing: "-0.01em",
    },
    h6: {
      fontWeight: 700,
      letterSpacing: "-0.01em",
    },
    button: {
      textTransform: "none",
      fontWeight: 600,
    },
  },
  components: {
    MuiPaper: {
      defaultProps: {
        elevation: 0,
      },
      styleOverrides: {
        root: ({ theme }) => ({
          border: `1px solid ${theme.palette.divider}`,
          borderRadius: theme.shape.borderRadius,
        }),
      },
    },
    MuiCard: {
      defaultProps: {
        elevation: 0,
      },
      styleOverrides: {
        root: ({ theme }) => ({
          border: `1px solid ${theme.palette.divider}`,
          borderRadius: theme.shape.borderRadius,
        }),
      },
    },
    MuiButton: {
      styleOverrides: {
        root: ({ theme }) => ({
          borderRadius: 10,
          boxShadow: "none",
          paddingLeft: theme.spacing(1.5),
          paddingRight: theme.spacing(1.5),
        }),
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: ({ theme }) => ({
          borderRadius: 10,
          backgroundColor: theme.palette.background.paper,
        }),
      },
    },
    MuiTableCell: {
      styleOverrides: {
        root: {
          whiteSpace: "normal",
          wordBreak: "break-word",
          overflowWrap: "anywhere",
          verticalAlign: "top",
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          maxWidth: "100%",
          height: "auto",
        },
        label: {
          display: "block",
          whiteSpace: "normal",
          lineHeight: 1.3,
          paddingTop: 4,
          paddingBottom: 4,
        },
      },
    },
  },
});
