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
