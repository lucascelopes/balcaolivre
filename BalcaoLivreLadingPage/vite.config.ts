import vinext from "vinext";
import { defineConfig } from "vite";
import hostingConfig from "./.openai/hosting.json";
import { sites } from "./build/sites-vite-plugin";

const SITE_CREATOR_PLACEHOLDER_DATABASE_ID =
  "00000000-0000-4000-8000-000000000000";
const CLOUDFLARE_D1_DATABASE_ID =
  process.env.CLOUDFLARE_D1_DATABASE_ID ||
  "f01d2bda-21de-4695-9a92-64b464ae8c7d";
const BOOKING_ROOT_DOMAIN =
  process.env.AGENDA_BOOKING_ROOT_DOMAIN || "minhaagendalivre.com.br";

process.env.NEXT_PUBLIC_SITE_URL ??= "https://minhaagendalivre.com.br";
process.env.NEXT_PUBLIC_AGENDA_SITE_URL ??= "https://minhaagendalivre.com.br";
process.env.NEXT_PUBLIC_BOOKING_ROOT_DOMAIN ??= BOOKING_ROOT_DOMAIN;

const isCodexSeatbeltSandbox = process.env.CODEX_SANDBOX === "seatbelt";
const isDirectCloudflareDeploy =
  process.env.DIRECT_CLOUDFLARE_DEPLOY === "true";

export default defineConfig(async () => {
  process.env.WRANGLER_WRITE_LOGS ??= "false";
  process.env.WRANGLER_LOG_PATH ??= ".wrangler/logs";
  process.env.MINIFLARE_REGISTRY_PATH ??= ".wrangler/registry";

  const { cloudflare } = await import("@cloudflare/vite-plugin");

  return {
    server: {
      allowedHosts: [".minhaagendalivre.com.br"],
      ...(isCodexSeatbeltSandbox
        ? { watch: { useFsEvents: false, usePolling: true } }
        : {}),
    },
    plugins: [
      vinext(),
      sites(),
      cloudflare({
        viteEnvironment: { name: "rsc", childEnvironments: ["ssr"] },
        config: {
          name: process.env.CLOUDFLARE_WORKER_NAME || "agenda-livre-platform",
          main: "./worker/index.ts",
          assets: {
            binding: "ASSETS",
            run_worker_first: true,
          },
          compatibility_flags: ["nodejs_compat"],
          vars: {
            AGENDA_BOOKING_ROOT_DOMAIN: BOOKING_ROOT_DOMAIN,
            NEXT_PUBLIC_BOOKING_ROOT_DOMAIN: BOOKING_ROOT_DOMAIN,
            AGENDA_SNAPSHOT_TTL_SECONDS:
              process.env.AGENDA_SNAPSHOT_TTL_SECONDS || "90",
          },
          routes: isDirectCloudflareDeploy
            ? [
                {
                  pattern: "*/*",
                  zone_name: BOOKING_ROOT_DOMAIN,
                },
              ]
            : [],
          d1_databases: hostingConfig.d1
            ? [
                {
                  binding: hostingConfig.d1,
                  database_name: "agenda-livre-booking",
                  database_id:
                    CLOUDFLARE_D1_DATABASE_ID ||
                    SITE_CREATOR_PLACEHOLDER_DATABASE_ID,
                },
              ]
            : [],
          // Sites provisions this logical binding. Direct Wrangler deploys must
          // omit it when R2 is not enabled for the Cloudflare account.
          r2_buckets: hostingConfig.r2 && !isDirectCloudflareDeploy
            ? [
                {
                  binding: hostingConfig.r2,
                  bucket_name:
                    process.env.CLOUDFLARE_R2_BUCKET_NAME || "agenda-livre-android-assets",
                },
              ]
            : [],
        },
      }),
    ],
  };
});
