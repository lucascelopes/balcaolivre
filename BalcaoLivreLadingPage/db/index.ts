import { env } from "cloudflare:workers";

export function getAgendaD1(): D1Database {
  const database = env.DB as D1Database | undefined;
  if (!database) {
    throw new Error(
      "Cloudflare D1 binding `DB` is unavailable. Configure d1 as DB in .openai/hosting.json.",
    );
  }
  return database;
}

export function getAgendaAndroidR2(): R2Bucket {
  const bucket = env.AGENDA_ANDROID_ASSETS as R2Bucket | undefined;
  if (!bucket) {
    throw new Error(
      "Cloudflare R2 binding `AGENDA_ANDROID_ASSETS` is unavailable. Configure r2 in .openai/hosting.json.",
    );
  }
  return bucket;
}

export function getAgendaCatalogR2(): R2Bucket {
  const bucket = env.AGENDA_ANDROID_ASSETS as R2Bucket | undefined;
  if (!bucket) {
    throw new Error(
      "Cloudflare R2 binding `AGENDA_ANDROID_ASSETS` is unavailable. Configure r2 in .openai/hosting.json.",
    );
  }
  return bucket;
}

export function getOptionalAgendaCatalogR2(): R2Bucket | null {
  return (env.AGENDA_ANDROID_ASSETS as R2Bucket | undefined) ?? null;
}
