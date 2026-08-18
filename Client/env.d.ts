// ============================================================================
// Environment Variables
// ============================================================================
// Add new variables here.

// Zentrale Definition aller erlaubten Environment-Variablen mit Default-Werten.
// Variablen ohne Default-Wert (null) müssen über docker-compose.yml gesetzt werden.
export const ENV_SCHEMA = {
  VITE_API_URL: null, // Orchestrator API – Muss gesetzt werden
  VITE_MOUNT_PATH: null, // Muss gesetzt werden
  VITE_KEYCLOAK_AUTHORITY: "",
  VITE_KEYCLOAK_CLIENT_ID: "",
  VITE_AUTH_DISABLED: "", // "true" disables the OIDC login gate (local bootstrap only)
} as const;

// ============================================================================
// Auto-generated TypeScript Types (Do not modify below this line)
// ============================================================================

export const ENV_KEYS = Object.keys(ENV_SCHEMA) as Array<
  keyof typeof ENV_SCHEMA
>;

type GeneratedEnv = {
  readonly [K in keyof typeof ENV_SCHEMA]: string;
};

declare global {
  interface ImportMetaEnv extends GeneratedEnv {}
  interface ImportMeta {
    readonly env: ImportMetaEnv;
  }
}

export {};
