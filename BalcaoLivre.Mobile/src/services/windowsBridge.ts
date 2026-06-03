import { pendingEvents } from "../data/db";
import { Settings } from "../types";

async function postJson<T>(baseUrl: string, path: string, body: unknown): Promise<T> {
  const response = await fetch(`${baseUrl.replace(/\/$/, "")}${path}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify(body)
  });
  const data = await response.json();
  if (!response.ok || data.ok === false) {
    throw new Error(data.message || `Windows bridge retornou HTTP ${response.status}.`);
  }
  return data as T;
}

export async function checkWindowsBridge(settings: Settings) {
  const response = await fetch(`${settings.windowsBridgeUrl.replace(/\/$/, "")}/api/mobile/status`);
  const data = await response.json();
  if (!response.ok || data.ok === false) {
    throw new Error(data.message || "Windows bridge indisponivel.");
  }
  return data;
}

export async function pushPendingToWindows(settings: Settings) {
  const events = await pendingEvents(100);
  if (events.length === 0) return { ok: true, imported: 0 };
  const result = await postJson<{ ok: boolean; message?: string }>(settings.windowsBridgeUrl, "/api/mobile/import", { events });
  return { ok: true, imported: events.length, message: result.message };
}
