// ============================================================================
// Environment Variables
// ============================================================================
// Add new variables here.

// Central definition of all allowed environment variables with default values.
// Variables without a default value (null) must be set via docker-compose.yml.
// All variables must have the VITE_ prefix.
export const ENV_SCHEMA = {
	VITE_INSTANCE_SERVER_URL: null,
	VITE_USECASE_SERVER_URL: null,
	VITE_ONTOLOGY_SERVER_URL: null,
	VITE_ACCESS_SERVER_URL: null,
	VITE_GUIDELINE_SERVER_URL: null,
	VITE_MOUNT_PATH: null,
} as const

// Variables only used in StandaloneApp (local development / standalone mode).
// StandaloneApp is NOT exported via module federation and therefore these
// variables do NOT need to be provided in Docker. Set them in .env.
export const DEV_ONLY_ENV_SCHEMA = {
	VITE_KEYCLOAK_AUTHORITY: null,
	VITE_KEYCLOAK_CLIENT_ID: null,
} as const

// ============================================================================
// Auto-generated TypeScript Types (Do not modify below this line)
// ============================================================================

export const ENV_KEYS = Object.keys(ENV_SCHEMA) as Array<keyof typeof ENV_SCHEMA>

type GeneratedEnv = {
	readonly [K in keyof typeof ENV_SCHEMA]: string
} & {
	readonly [K in keyof typeof DEV_ONLY_ENV_SCHEMA]: string
}

declare global {
	interface ImportMetaEnv extends GeneratedEnv {}
	interface ImportMeta {
		readonly env: ImportMetaEnv
	}
}

export {}
