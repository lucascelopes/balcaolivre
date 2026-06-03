import { applySnapshot, getSettings, markEventsSynced, pendingEvents, saveSettings } from "../data/db";
import { Session } from "../types";
import { bootstrapMobile, syncMobile } from "./licenseApi";

export async function bootstrapAndApply(session: Session) {
  const result = await bootstrapMobile(session);
  if (result.snapshot) {
    await applySnapshot(result.snapshot);
  }
  return result;
}

export async function flushSync(session: Session) {
  const events = await pendingEvents(50);
  if (events.length === 0) return { ok: true, synced: 0 };
  const result = await syncMobile(session, events);
  const ids = result.acceptedEventIds?.length ? result.acceptedEventIds : events.map((event) => event.id);
  await markEventsSynced(ids);
  const settings = await getSettings();
  await saveSettings({ ...settings, lastSyncAt: new Date().toISOString() });
  return { ok: true, synced: ids.length };
}
