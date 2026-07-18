import { env } from "cloudflare:workers";
import { getAgendaD1 } from "../../db/index";

const DEFAULT_ROOT_DOMAIN = "minhaagendalivre.com.br";
const DEFAULT_LICENSE_SECRET = "BalcaoLivrePDV-local-license-v1";
const ACTIVE_STATUSES = new Set(["requested", "pending", "confirmed"]);
const TERMINAL_STATUSES = new Set([
  "rejected",
  "cancelled",
  "completed",
  "slot_conflict",
]);
const ALLOWED_STATUSES = new Set([...ACTIVE_STATUSES, ...TERMINAL_STATUSES]);
const RESERVED_SLUGS = new Set([
  "www",
  "app",
  "admin",
  "pdv",
  "cardapio",
  "api",
]);
const FIFTEEN_MINUTES_MS = 15 * 60 * 1000;
const FOUR_HOURS_MS = 4 * 60 * 60 * 1000;
const PUBLIC_LOGO_MAXIMUM_DATA_URL_CHARACTERS = 132_000;
const PUBLIC_LOGO_MAXIMUM_BYTES = 96 * 1024;
const PUBLIC_LOGO_MAXIMUM_DIMENSION = 128;

type JsonRecord = Record<string, unknown>;

type StoreRow = {
  id: string;
  instance: string;
  license_hash: string;
  machine_hash: string;
  machine_code: string;
  desired_slug: string;
  slug: string;
  name: string;
  segment: string;
  theme_json: string;
  generated_at: string;
  last_synced_at: number;
};

type SnapshotRow = {
  services_json: string;
  generated_at: string;
  received_at: number;
};

type BookingRow = {
  id: string;
  store_id: string;
  source: string;
  status: string;
  idempotency_key: string;
  slot_key: string;
  service_id: string;
  service_name: string;
  slot_id: string;
  starts_at: string;
  starts_at_ms: number;
  duration_minutes: number;
  price_cents: number;
  professional_id: string;
  professional_name: string;
  resource_name: string;
  customer_name: string;
  customer_phone: string;
  notes: string;
  appointment_id: string | null;
  message: string | null;
  confirmation_sent_at: number | null;
  reminder_sent_at: number | null;
  confirmed_at: number | null;
  created_at: number;
  updated_at: number;
};

type BookingSlot = {
  id: string;
  time: string;
  start: string;
  professionalId: string;
  professionalName: string;
  resourceName: string;
};

type BookingDay = {
  date: string;
  label: string;
  availableSlots: BookingSlot[];
};

type BookingService = {
  id: string;
  name: string;
  durationMinutes: number;
  price: number;
  days: BookingDay[];
};

type InternalIdentity = {
  license: string;
  licenseHash: string;
  machineHash: string;
  machineCode: string;
};

export class AgendaBookingError extends Error {
  readonly status: number;
  readonly code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.name = "AgendaBookingError";
    this.status = status;
    this.code = code;
  }
}

function runtimeValue(name: string) {
  const runtime = env as unknown as Record<string, unknown>;
  const value = runtime?.[name];
  if (typeof value === "string" && value.trim()) return value.trim();
  return typeof process !== "undefined" ? process.env[name]?.trim() || "" : "";
}

function rootDomain() {
  return (
    runtimeValue("AGENDA_BOOKING_ROOT_DOMAIN") ||
    runtimeValue("NEXT_PUBLIC_BOOKING_ROOT_DOMAIN") ||
    DEFAULT_ROOT_DOMAIN
  )
    .toLowerCase()
    .replace(/^https?:\/\//, "")
    .replace(/\/$/, "");
}

function publicUrl(slug: string) {
  return `https://${slug}.${rootDomain()}`;
}

function snapshotTtlMs() {
  const parsed = Number(runtimeValue("AGENDA_SNAPSHOT_TTL_SECONDS") || "90");
  const seconds = Number.isFinite(parsed) ? Math.min(300, Math.max(30, parsed)) : 90;
  return seconds * 1000;
}

function jsonHeaders() {
  return {
    "Cache-Control": "no-store, max-age=0",
    "Content-Type": "application/json; charset=utf-8",
    "Referrer-Policy": "no-referrer",
    "X-Content-Type-Options": "nosniff",
  };
}

export function agendaJson(data: unknown, status = 200) {
  return new Response(JSON.stringify(data), { status, headers: jsonHeaders() });
}

export function agendaErrorResponse(error: unknown) {
  if (error instanceof AgendaBookingError) {
    return agendaJson(
      { ok: false, error: { code: error.code, message: error.message } },
      error.status,
    );
  }

  console.error("Agenda booking route failed", error);
  return agendaJson(
    {
      ok: false,
      error: {
        code: "internal_error",
        message: "Não foi possível concluir a operação agora.",
      },
    },
    500,
  );
}

async function readJsonBody(request: Request, maxBytes: number): Promise<JsonRecord> {
  const declaredLength = Number(request.headers.get("content-length") || "0");
  if (Number.isFinite(declaredLength) && declaredLength > maxBytes) {
    throw new AgendaBookingError(413, "payload_too_large", "O conteúdo enviado é muito grande.");
  }

  const text = await request.text();
  if (!text || new TextEncoder().encode(text).byteLength > maxBytes) {
    throw new AgendaBookingError(400, "invalid_body", "Envie os dados solicitados.");
  }

  try {
    const parsed = JSON.parse(text);
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) throw new Error();
    return parsed as JsonRecord;
  } catch {
    throw new AgendaBookingError(400, "invalid_json", "O conteúdo enviado não é um JSON válido.");
  }
}

function stringValue(value: unknown, maxLength: number, fallback = "") {
  const text = String(value ?? "").trim();
  return (text || fallback).slice(0, maxLength);
}

function normalizeSlug(value: unknown) {
  const normalized = stringValue(value, 100)
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .replace(/-{2,}/g, "-")
    .slice(0, 48)
    .replace(/-+$/g, "");

  const candidate = normalized.length >= 3 ? normalized : `agenda-${normalized || "loja"}`;
  return RESERVED_SLUGS.has(candidate) ? `${candidate}-agenda` : candidate;
}

function normalizeInstance(value: unknown) {
  const instance = stringValue(value, 128).toLowerCase();
  if (!/^[a-z0-9][a-z0-9_-]{2,127}$/.test(instance)) {
    throw new AgendaBookingError(400, "invalid_instance", "A identificação da agenda é inválida.");
  }
  return instance;
}

function bytesToHex(bytes: ArrayBuffer) {
  return Array.from(new Uint8Array(bytes))
    .map((value) => value.toString(16).padStart(2, "0"))
    .join("")
    .toUpperCase();
}

function bytesToBase64Url(bytes: ArrayBuffer) {
  let binary = "";
  for (const value of new Uint8Array(bytes)) binary += String.fromCharCode(value);
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/g, "");
}

async function sha256(value: string) {
  return bytesToHex(await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value)));
}

async function hmacSha256(message: string, secret: string) {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  return crypto.subtle.sign("HMAC", key, new TextEncoder().encode(message));
}

function constantTimeEqual(left: string, right: string) {
  if (left.length !== right.length) return false;
  let difference = 0;
  for (let index = 0; index < left.length; index += 1) {
    difference |= left.charCodeAt(index) ^ right.charCodeAt(index);
  }
  return difference === 0;
}

function parseLicenseExpiration(value: string) {
  if (!/^\d{12}$/.test(value)) return null;
  const year = Number(value.slice(0, 4));
  const month = Number(value.slice(4, 6));
  const day = Number(value.slice(6, 8));
  const hour = Number(value.slice(8, 10));
  const minute = Number(value.slice(10, 12));
  if (
    year < 2024 ||
    month < 1 ||
    month > 12 ||
    day < 1 ||
    day > 31 ||
    hour > 23 ||
    minute > 59
  ) {
    return null;
  }
  const localCheck = new Date(Date.UTC(year, month - 1, day, hour, minute));
  if (
    localCheck.getUTCFullYear() !== year ||
    localCheck.getUTCMonth() !== month - 1 ||
    localCheck.getUTCDate() !== day
  ) {
    return null;
  }
  // The signed value is produced in America/Sao_Paulo local time (UTC-03).
  return Date.UTC(year, month - 1, day, hour + 3, minute, 59, 999);
}

async function authenticateInternal(request: Request): Promise<InternalIdentity> {
  const license = stringValue(request.headers.get("x-agenda-license"), 256).toUpperCase();
  const machineHash = stringValue(request.headers.get("x-agenda-machine"), 256).toLowerCase();
  const machineCode = stringValue(request.headers.get("x-agenda-machine-code"), 64).toUpperCase();
  const parts = license.split("-").filter(Boolean);

  if (
    parts.length !== 4 ||
    parts[0] !== "BLV" ||
    !/^[A-Z0-9]{8,64}$/.test(parts[2]) ||
    !/^[A-F0-9]{10}$/.test(parts[3]) ||
    !/^[a-f0-9]{32,128}$/.test(machineHash) ||
    !/^[A-F0-9]{8,32}$/.test(machineCode)
  ) {
    throw new AgendaBookingError(401, "invalid_license", "Credenciais da agenda inválidas.");
  }

  const expiresAt = parseLicenseExpiration(parts[1]);
  if (!expiresAt || expiresAt <= Date.now()) {
    throw new AgendaBookingError(401, "expired_license", "A licença da agenda está vencida.");
  }
  if (
    machineHash.slice(0, machineCode.length).toUpperCase() !== machineCode ||
    !parts[2].endsWith(machineCode)
  ) {
    throw new AgendaBookingError(401, "machine_mismatch", "A licença não pertence a este computador.");
  }

  const secret = runtimeValue("AGENDA_LICENSE_SECRET") || DEFAULT_LICENSE_SECRET;
  const signature = bytesToHex(
    await hmacSha256(`BLV|${parts[1]}|${parts[2]}`, secret),
  ).slice(0, 10);
  if (!constantTimeEqual(signature, parts[3])) {
    throw new AgendaBookingError(401, "invalid_license", "Credenciais da agenda inválidas.");
  }

  return {
    license,
    licenseHash: await sha256(license),
    machineHash,
    machineCode,
  };
}

function sanitizeTheme(value: unknown) {
  if (typeof value === "string") return stringValue(value, 80);
  if (!value || typeof value !== "object" || Array.isArray(value)) return {};

  const allowed = new Set([
    "id",
    "name",
    "key",
    "theme",
    "accent",
    "accentcolor",
    "accentsoft",
    "accentdark",
    "onaccent",
    "primary",
    "primarycolor",
    "secondary",
    "secondarycolor",
    "background",
    "backgroundcolor",
    "appbackground",
    "surface",
    "surfacecolor",
    "foreground",
    "text",
    "textcolor",
    "muted",
    "border",
    "line",
    "ink",
    "fontfamily",
    "palette",
    "colors",
    "logourl",
  ]);
  const output: JsonRecord = {};
  for (const [key, raw] of Object.entries(value as JsonRecord).slice(0, 40)) {
    if (!allowed.has(key.toLowerCase())) continue;
    if (key.toLowerCase() === "logourl") {
      output.logoUrl = sanitizePublicLogoDataUrl(raw);
      continue;
    }
    if (typeof raw === "string") {
      output[key] = stringValue(raw, 160);
    } else if (typeof raw === "number" || typeof raw === "boolean") {
      output[key] = raw;
    } else if (
      raw &&
      typeof raw === "object" &&
      !Array.isArray(raw) &&
      ["palette", "colors"].includes(key.toLowerCase())
    ) {
      const nested: JsonRecord = {};
      for (const [nestedKey, nestedValue] of Object.entries(raw as JsonRecord).slice(0, 30)) {
        if (typeof nestedValue === "string") nested[nestedKey] = stringValue(nestedValue, 160);
      }
      output[key] = nested;
    }
  }
  return output;
}

function sanitizePublicLogoDataUrl(value: unknown) {
  if (typeof value !== "string") {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é inválida.");
  }
  const dataUrl = value.trim();
  if (!dataUrl) return "";
  if (dataUrl.length > PUBLIC_LOGO_MAXIMUM_DATA_URL_CHARACTERS) {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é muito grande.");
  }

  const match = /^data:image\/png;base64,([A-Za-z0-9+/]+={0,2})$/.exec(dataUrl);
  if (!match || match[1].length % 4 !== 0) {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é inválida.");
  }

  let bytes: string;
  try {
    bytes = atob(match[1]);
  } catch {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é inválida.");
  }
  if (bytes.length < 24 || bytes.length > PUBLIC_LOGO_MAXIMUM_BYTES) {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é inválida.");
  }

  const pngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
  if (pngSignature.some((expected, index) => bytes.charCodeAt(index) !== expected)) {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja é inválida.");
  }
  const readUint32 = (offset: number) =>
    (((bytes.charCodeAt(offset) << 24) >>> 0) |
      (bytes.charCodeAt(offset + 1) << 16) |
      (bytes.charCodeAt(offset + 2) << 8) |
      bytes.charCodeAt(offset + 3)) >>> 0;
  const width = readUint32(16);
  const height = readUint32(20);
  if (
    width < 1 ||
    height < 1 ||
    width > PUBLIC_LOGO_MAXIMUM_DIMENSION ||
    height > PUBLIC_LOGO_MAXIMUM_DIMENSION
  ) {
    throw new AgendaBookingError(400, "invalid_logo", "A imagem da loja possui dimensões inválidas.");
  }

  return dataUrl;
}

function sanitizeServices(value: unknown): BookingService[] {
  if (!Array.isArray(value)) {
    throw new AgendaBookingError(400, "invalid_services", "A disponibilidade enviada é inválida.");
  }

  const services: BookingService[] = [];
  const seenServices = new Set<string>();
  for (const rawService of value.slice(0, 50)) {
    if (!rawService || typeof rawService !== "object" || Array.isArray(rawService)) continue;
    const source = rawService as JsonRecord;
    const id = stringValue(source.id, 128);
    const name = stringValue(source.name, 160);
    if (!id || !name || seenServices.has(id)) continue;
    seenServices.add(id);

    const durationNumber = Math.round(Number(source.durationMinutes));
    const durationMinutes = Number.isFinite(durationNumber)
      ? Math.min(480, Math.max(15, durationNumber))
      : 30;
    const rawPrice = Number(source.price);
    const price = Number.isFinite(rawPrice)
      ? Math.round(Math.min(1_000_000, Math.max(0, rawPrice)) * 100) / 100
      : 0;
    const days: BookingDay[] = [];
    const seenDays = new Set<string>();

    if (Array.isArray(source.days)) {
      for (const rawDay of source.days.slice(0, 31)) {
        if (!rawDay || typeof rawDay !== "object" || Array.isArray(rawDay)) continue;
        const daySource = rawDay as JsonRecord;
        const date = stringValue(daySource.date, 10);
        if (!/^\d{4}-\d{2}-\d{2}$/.test(date) || seenDays.has(date)) continue;
        seenDays.add(date);
        const slots: BookingSlot[] = [];
        const seenSlots = new Set<string>();

        if (Array.isArray(daySource.availableSlots)) {
          for (const rawSlot of daySource.availableSlots.slice(0, 200)) {
            if (!rawSlot || typeof rawSlot !== "object" || Array.isArray(rawSlot)) continue;
            const slotSource = rawSlot as JsonRecord;
            const slotId = stringValue(slotSource.id, 160);
            const professionalId = stringValue(slotSource.professionalId, 128);
            const startMs = Date.parse(stringValue(slotSource.start, 80));
            if (
              !slotId ||
              !professionalId ||
              seenSlots.has(slotId) ||
              !Number.isFinite(startMs)
            ) {
              continue;
            }
            seenSlots.add(slotId);
            slots.push({
              id: slotId,
              time: /^\d{2}:\d{2}$/.test(stringValue(slotSource.time, 5))
                ? stringValue(slotSource.time, 5)
                : new Date(startMs).toISOString().slice(11, 16),
              start: new Date(startMs).toISOString(),
              professionalId,
              professionalName: stringValue(slotSource.professionalName, 160, "Profissional"),
              resourceName: stringValue(slotSource.resourceName, 160),
            });
          }
        }

        if (slots.length) {
          days.push({
            date,
            label: stringValue(daySource.label, 80, date),
            availableSlots: slots,
          });
        }
      }
    }

    if (days.length) services.push({ id, name, durationMinutes, price, days });
  }
  return services;
}

function parseStoredServices(value: string): BookingService[] {
  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? (parsed as BookingService[]) : [];
  } catch {
    return [];
  }
}

function parseStoredTheme(value: string) {
  try {
    return JSON.parse(value);
  } catch {
    return {};
  }
}

async function allocateSlug(
  database: D1Database,
  desired: string,
  storeId: string | null,
  instance: string,
) {
  const digest = (await sha256(instance)).slice(0, 6).toLowerCase();
  for (let attempt = 0; attempt < 100; attempt += 1) {
    const suffix = attempt === 0 ? "" : `-${digest}${attempt === 1 ? "" : `-${attempt}`}`;
    const base = desired.slice(0, Math.max(3, 48 - suffix.length)).replace(/-+$/g, "");
    const candidate = `${base}${suffix}`;
    const owner = await database
      .prepare("SELECT id FROM agenda_stores WHERE slug = ?1 LIMIT 1")
      .bind(candidate)
      .first<{ id: string }>();
    if (!owner || owner.id === storeId) return candidate;
  }
  throw new AgendaBookingError(409, "slug_unavailable", "Não foi possível reservar o endereço desta loja.");
}

function assertStoreIdentity(store: StoreRow, identity: InternalIdentity) {
  if (
    store.license_hash !== identity.licenseHash ||
    store.machine_hash !== identity.machineHash ||
    store.machine_code !== identity.machineCode
  ) {
    throw new AgendaBookingError(403, "store_identity_mismatch", "Esta agenda pertence a outra instalação.");
  }
}

function bookingInternal(row: BookingRow, instance: string) {
  const now = Date.now();
  const reminderDue =
    row.status === "confirmed" &&
    !row.reminder_sent_at &&
    row.starts_at_ms > now &&
    row.starts_at_ms <= now + FOUR_HOURS_MS;
  return {
    id: row.id,
    instance,
    source: row.source,
    leadId: row.id,
    phone: row.customer_phone,
    customerName: row.customer_name,
    serviceId: row.service_id,
    serviceName: row.service_name,
    slotId: row.slot_id,
    start: row.starts_at,
    durationMinutes: row.duration_minutes,
    professionalId: row.professional_id,
    professionalName: row.professional_name,
    resourceName: row.resource_name,
    price: row.price_cents / 100,
    notes: row.notes,
    status: row.status,
    appointmentId: row.appointment_id,
    message: row.message,
    confirmationSentAt: row.confirmation_sent_at
      ? new Date(row.confirmation_sent_at).toISOString()
      : null,
    reminderSentAt: row.reminder_sent_at
      ? new Date(row.reminder_sent_at).toISOString()
      : null,
    needsConfirmation: row.status === "confirmed" && !row.confirmation_sent_at,
    reminderDue,
    reminderAt: new Date(row.starts_at_ms - FOUR_HOURS_MS).toISOString(),
  };
}

async function pendingBookings(database: D1Database, store: StoreRow) {
  const now = Date.now();
  const dueBefore = now + FOUR_HOURS_MS;
  const result = await database
    .prepare(
      `SELECT * FROM agenda_bookings
       WHERE store_id = ?1
         AND (
           status IN ('requested', 'pending')
           OR (
             status = 'confirmed'
             AND (
               confirmation_sent_at IS NULL
               OR (reminder_sent_at IS NULL AND starts_at_ms > ?2 AND starts_at_ms <= ?3)
             )
           )
         )
       ORDER BY created_at ASC
       LIMIT 100`,
    )
    .bind(store.id, now, dueBefore)
    .all<BookingRow>();
  return (result.results || []).map((row) => bookingInternal(row, store.instance));
}

export async function syncAgenda(request: Request) {
  const identity = await authenticateInternal(request);
  const body = await readJsonBody(request, 2_000_000);
  const instance = normalizeInstance(body.instance);
  const storeName = stringValue(body.storeName, 160);
  if (storeName.length < 2) {
    throw new AgendaBookingError(400, "invalid_store_name", "Informe o nome da loja.");
  }

  const desiredSlug = normalizeSlug(body.desiredSlug || storeName);
  const segment = stringValue(body.segment, 120, "Serviços");
  const theme = sanitizeTheme(body.theme);
  const services = sanitizeServices(body.bookingServices);
  const parsedGeneratedAt = Date.parse(stringValue(body.generatedAt, 80));
  const generatedAt = Number.isFinite(parsedGeneratedAt)
    ? new Date(parsedGeneratedAt).toISOString()
    : new Date().toISOString();
  const now = Date.now();
  const database = getAgendaD1();
  const existing = await database
    .prepare("SELECT * FROM agenda_stores WHERE instance = ?1 LIMIT 1")
    .bind(instance)
    .first<StoreRow>();
  if (existing) assertStoreIdentity(existing, identity);
  if (!Object.prototype.hasOwnProperty.call(theme, "logoUrl") && existing) {
    const previousTheme = parseStoredTheme(existing.theme_json) as JsonRecord;
    if (typeof previousTheme.logoUrl === "string" && previousTheme.logoUrl) {
      try {
        theme.logoUrl = sanitizePublicLogoDataUrl(previousTheme.logoUrl);
      } catch {
        // Invalid legacy image data is discarded instead of blocking the current sync.
      }
    }
  }

  const storeId = existing?.id || crypto.randomUUID();
  const slug =
    existing && existing.desired_slug === desiredSlug
      ? existing.slug
      : await allocateSlug(database, desiredSlug, existing?.id || null, instance);
  const themeJson = JSON.stringify(theme);
  const servicesJson = JSON.stringify(services);

  const storeStatement = existing
    ? database
        .prepare(
          `UPDATE agenda_stores
           SET desired_slug = ?1, slug = ?2, name = ?3, segment = ?4,
               theme_json = ?5, generated_at = ?6, last_synced_at = ?7, updated_at = ?7
           WHERE id = ?8`,
        )
        .bind(desiredSlug, slug, storeName, segment, themeJson, generatedAt, now, storeId)
    : database
        .prepare(
          `INSERT INTO agenda_stores (
             id, instance, license_hash, machine_hash, machine_code, desired_slug, slug,
             name, segment, theme_json, generated_at, last_synced_at, created_at, updated_at
           ) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?12, ?12)`,
        )
        .bind(
          storeId,
          instance,
          identity.licenseHash,
          identity.machineHash,
          identity.machineCode,
          desiredSlug,
          slug,
          storeName,
          segment,
          themeJson,
          generatedAt,
          now,
        );

  await database.batch([
    storeStatement,
    database
      .prepare(
        `INSERT INTO agenda_snapshots (store_id, services_json, generated_at, received_at)
         VALUES (?1, ?2, ?3, ?4)
         ON CONFLICT(store_id) DO UPDATE SET
           services_json = excluded.services_json,
           generated_at = excluded.generated_at,
           received_at = excluded.received_at`,
      )
      .bind(storeId, servicesJson, generatedAt, now),
  ]);

  const stored: StoreRow = {
    id: storeId,
    instance,
    license_hash: identity.licenseHash,
    machine_hash: identity.machineHash,
    machine_code: identity.machineCode,
    desired_slug: desiredSlug,
    slug,
    name: storeName,
    segment,
    theme_json: themeJson,
    generated_at: generatedAt,
    last_synced_at: now,
  };
  return agendaJson({
    ok: true,
    slug,
    publicUrl: publicUrl(slug),
    bookings: await pendingBookings(database, stored),
  });
}

async function publicStore(database: D1Database, slugValue: string) {
  const slug = normalizeSlug(slugValue);
  if (slug !== slugValue.toLowerCase()) {
    throw new AgendaBookingError(404, "store_not_found", "Agenda não encontrada.");
  }
  const store = await database
    .prepare("SELECT * FROM agenda_stores WHERE slug = ?1 LIMIT 1")
    .bind(slug)
    .first<StoreRow>();
  if (!store) throw new AgendaBookingError(404, "store_not_found", "Agenda não encontrada.");
  const snapshot = await database
    .prepare("SELECT * FROM agenda_snapshots WHERE store_id = ?1 LIMIT 1")
    .bind(store.id)
    .first<SnapshotRow>();
  if (!snapshot) {
    throw new AgendaBookingError(503, "availability_offline", "A agenda está temporariamente indisponível.");
  }
  return { store, snapshot };
}

function assertFresh(store: StoreRow, snapshot: SnapshotRow) {
  const receivedAt = Number(snapshot.received_at || store.last_synced_at);
  if (!Number.isFinite(receivedAt) || Date.now() - receivedAt > snapshotTtlMs()) {
    throw new AgendaBookingError(
      503,
      "availability_stale",
      "A loja está atualizando os horários. Tente novamente em instantes.",
    );
  }
}

function lockMoments(startsAtMs: number, durationMinutes: number) {
  const count = Math.ceil(durationMinutes / 15);
  return Array.from({ length: count }, (_, index) => startsAtMs + index * FIFTEEN_MINUTES_MS);
}

function resourceLockId(resourceName: string) {
  const normalized = resourceName
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, 120);
  return normalized ? `resource:${normalized}` : "";
}

async function availableServices(
  database: D1Database,
  store: StoreRow,
  snapshot: SnapshotRow,
) {
  const services = parseStoredServices(snapshot.services_json);
  const lockResult = await database
    .prepare(
      "SELECT professional_id, lock_start_ms FROM agenda_booking_locks WHERE store_id = ?1",
    )
    .bind(store.id)
    .all<{ professional_id: string; lock_start_ms: number }>();
  const locks = new Set(
    (lockResult.results || []).map(
      (lock) => `${lock.professional_id}|${Number(lock.lock_start_ms)}`,
    ),
  );
  const now = Date.now();

  return services
    .map((service) => ({
      ...service,
      days: service.days
        .map((day) => ({
          ...day,
          availableSlots: day.availableSlots.filter((slot) => {
            const startsAtMs = Date.parse(slot.start);
            if (!Number.isFinite(startsAtMs) || startsAtMs <= now) return false;
            const resourceId = resourceLockId(slot.resourceName);
            return !lockMoments(startsAtMs, service.durationMinutes).some(
              (moment) =>
                locks.has(`${slot.professionalId}|${moment}`) ||
                (resourceId ? locks.has(`${resourceId}|${moment}`) : false),
            );
          }),
        }))
        .filter((day) => day.availableSlots.length > 0),
    }))
    .filter((service) => service.days.length > 0);
}

export async function getAvailability(slug: string) {
  const database = getAgendaD1();
  const { store, snapshot } = await publicStore(database, slug);
  assertFresh(store, snapshot);
  return agendaJson({
    ok: true,
    store: {
      slug: store.slug,
      name: store.name,
      segment: store.segment,
      theme: parseStoredTheme(store.theme_json),
      publicUrl: publicUrl(store.slug),
      generatedAt: snapshot.generated_at,
    },
    services: await availableServices(database, store, snapshot),
  });
}

function normalizeBrazilPhone(value: unknown) {
  let digits = String(value ?? "").replace(/\D/g, "");
  if (digits.startsWith("0055")) digits = digits.slice(4);
  else if (digits.startsWith("55") && digits.length >= 12) digits = digits.slice(2);
  if (!/^\d{10,11}$/.test(digits) || /^([0-9])\1+$/.test(digits)) {
    throw new AgendaBookingError(400, "invalid_phone", "Informe um WhatsApp brasileiro válido com DDD.");
  }
  const areaCode = Number(digits.slice(0, 2));
  if (areaCode < 11 || areaCode > 99) {
    throw new AgendaBookingError(400, "invalid_phone", "Informe um WhatsApp brasileiro válido com DDD.");
  }
  return `55${digits}`;
}

function publicBooking(row: BookingRow, statusToken: string) {
  return {
    id: row.id,
    status: row.status,
    statusToken,
    startsAt: row.starts_at,
    serviceName: row.service_name,
  };
}

async function statusTokenFor(storeId: string, idempotencyKey: string) {
  const secret =
    runtimeValue("AGENDA_STATUS_TOKEN_SECRET") ||
    runtimeValue("AGENDA_LICENSE_SECRET") ||
    DEFAULT_LICENSE_SECRET;
  return bytesToBase64Url(
    await hmacSha256(`agenda-status|${storeId}|${idempotencyKey}`, secret),
  );
}

function bookingFromSnapshot(
  services: BookingService[],
  serviceId: string,
  slotId: string,
) {
  const service = services.find((item) => item.id === serviceId);
  if (!service) {
    throw new AgendaBookingError(409, "service_unavailable", "Este serviço não está mais disponível.");
  }
  for (const day of service.days) {
    const slot = day.availableSlots.find((item) => item.id === slotId);
    if (slot) return { service, slot };
  }
  throw new AgendaBookingError(409, "slot_unavailable", "Este horário não está mais disponível.");
}

export async function createAppointment(request: Request, slug: string) {
  const body = await readJsonBody(request, 20_000);
  const serviceId = stringValue(body.serviceId, 128);
  const slotId = stringValue(body.slotId, 160);
  const customerName = stringValue(body.customerName, 100).replace(/\s+/g, " ");
  const customerPhone = normalizeBrazilPhone(body.customerPhone);
  const notes = stringValue(body.notes, 500);
  const idempotencyKey = stringValue(
    body.idempotencyKey || request.headers.get("idempotency-key"),
    128,
  );
  if (customerName.length < 2) {
    throw new AgendaBookingError(400, "invalid_name", "Informe seu nome.");
  }
  if (!serviceId || !slotId || !/^[A-Za-z0-9._:-]{8,128}$/.test(idempotencyKey)) {
    throw new AgendaBookingError(400, "invalid_request", "Selecione um serviço e um horário válidos.");
  }

  const database = getAgendaD1();
  const { store, snapshot } = await publicStore(database, slug);
  assertFresh(store, snapshot);
  const statusToken = await statusTokenFor(store.id, idempotencyKey);
  const statusTokenHash = await sha256(statusToken);
  const existing = await database
    .prepare(
      "SELECT * FROM agenda_bookings WHERE store_id = ?1 AND idempotency_key = ?2 LIMIT 1",
    )
    .bind(store.id, idempotencyKey)
    .first<BookingRow>();
  if (existing) return agendaJson({ ok: true, booking: publicBooking(existing, statusToken) });

  const services = parseStoredServices(snapshot.services_json);
  const { service, slot } = bookingFromSnapshot(services, serviceId, slotId);
  const startsAtMs = Date.parse(slot.start);
  if (!Number.isFinite(startsAtMs) || startsAtMs <= Date.now()) {
    throw new AgendaBookingError(409, "slot_unavailable", "Este horário não está mais disponível.");
  }

  const bookingId = crypto.randomUUID();
  const now = Date.now();
  const slotKey = `${slot.professionalId}:${startsAtMs}`;
  const bookingStatement = database
    .prepare(
      `INSERT INTO agenda_bookings (
         id, store_id, source, status, status_token_hash, idempotency_key, slot_key,
         service_id, service_name, slot_id, starts_at, starts_at_ms, duration_minutes,
         price_cents, professional_id, professional_name, resource_name, customer_name,
         customer_phone, notes, created_at, updated_at
       ) VALUES (
         ?1, ?2, 'web', 'requested', ?3, ?4, ?5,
         ?6, ?7, ?8, ?9, ?10, ?11,
         ?12, ?13, ?14, ?15, ?16,
         ?17, ?18, ?19, ?19
       )`,
    )
    .bind(
      bookingId,
      store.id,
      statusTokenHash,
      idempotencyKey,
      slotKey,
      service.id,
      service.name,
      slot.id,
      slot.start,
      startsAtMs,
      service.durationMinutes,
      Math.round(service.price * 100),
      slot.professionalId,
      slot.professionalName,
      slot.resourceName,
      customerName,
      customerPhone,
      notes,
      now,
    );
  const lockOwners = [slot.professionalId];
  const resourceId = resourceLockId(slot.resourceName);
  if (resourceId) lockOwners.push(resourceId);
  const lockStatements = lockOwners.flatMap((ownerId) =>
    lockMoments(startsAtMs, service.durationMinutes).map((lockStartMs) =>
      database
        .prepare(
          `INSERT INTO agenda_booking_locks
             (store_id, professional_id, lock_start_ms, booking_id)
           VALUES (?1, ?2, ?3, ?4)`,
        )
        .bind(store.id, ownerId, lockStartMs, bookingId),
    ),
  );

  try {
    await database.batch([bookingStatement, ...lockStatements]);
  } catch (error) {
    const idempotent = await database
      .prepare(
        "SELECT * FROM agenda_bookings WHERE store_id = ?1 AND idempotency_key = ?2 LIMIT 1",
      )
      .bind(store.id, idempotencyKey)
      .first<BookingRow>();
    if (idempotent) {
      return agendaJson({ ok: true, booking: publicBooking(idempotent, statusToken) });
    }
    console.warn("Agenda slot reservation conflict", error);
    throw new AgendaBookingError(409, "slot_unavailable", "Este horário acabou de ser reservado.");
  }

  const created = await database
    .prepare("SELECT * FROM agenda_bookings WHERE id = ?1 LIMIT 1")
    .bind(bookingId)
    .first<BookingRow>();
  if (!created) throw new Error("Booking insert succeeded but row was not found");
  return agendaJson({ ok: true, booking: publicBooking(created, statusToken) }, 201);
}

function validSentAt(value: unknown) {
  if (value === true) return Date.now();
  if (typeof value !== "string") return undefined;
  const parsed = Date.parse(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}

function assertStatusTransition(current: string, next: string) {
  if (current === next) return;
  if (!ALLOWED_STATUSES.has(next)) {
    throw new AgendaBookingError(400, "invalid_status", "Status de agendamento inválido.");
  }
  if (TERMINAL_STATUSES.has(current)) {
    throw new AgendaBookingError(409, "booking_closed", "Este agendamento já foi encerrado.");
  }
  if (current === "confirmed" && !["cancelled", "completed"].includes(next)) {
    throw new AgendaBookingError(409, "invalid_transition", "Não é possível voltar o status deste agendamento.");
  }
}

export async function patchInternalBooking(request: Request, bookingId: string) {
  const identity = await authenticateInternal(request);
  const body = await readJsonBody(request, 20_000);
  const database = getAgendaD1();
  const booking = await database
    .prepare("SELECT * FROM agenda_bookings WHERE id = ?1 LIMIT 1")
    .bind(stringValue(bookingId, 128))
    .first<BookingRow>();
  if (!booking) throw new AgendaBookingError(404, "booking_not_found", "Agendamento não encontrado.");
  const store = await database
    .prepare("SELECT * FROM agenda_stores WHERE id = ?1 LIMIT 1")
    .bind(booking.store_id)
    .first<StoreRow>();
  if (!store) throw new AgendaBookingError(404, "store_not_found", "Agenda não encontrada.");
  assertStoreIdentity(store, identity);

  const nextStatus = body.status === undefined
    ? booking.status
    : stringValue(body.status, 32).toLowerCase();
  assertStatusTransition(booking.status, nextStatus);
  const appointmentId = body.appointmentId === undefined
    ? booking.appointment_id
    : stringValue(body.appointmentId, 128) || null;
  const message = body.message === undefined
    ? booking.message
    : stringValue(body.message, 500) || null;
  const confirmationSentAt = body.confirmationSentAt === undefined
    ? booking.confirmation_sent_at
    : validSentAt(body.confirmationSentAt);
  const reminderSentAt = body.reminderSentAt === undefined
    ? booking.reminder_sent_at
    : validSentAt(body.reminderSentAt);
  if (body.confirmationSentAt !== undefined && confirmationSentAt === undefined) {
    throw new AgendaBookingError(400, "invalid_confirmation_time", "Data de confirmação inválida.");
  }
  if (body.reminderSentAt !== undefined && reminderSentAt === undefined) {
    throw new AgendaBookingError(400, "invalid_reminder_time", "Data de lembrete inválida.");
  }

  const now = Date.now();
  const confirmedAt = nextStatus === "confirmed"
    ? booking.confirmed_at || now
    : booking.confirmed_at;
  const update = database
    .prepare(
      `UPDATE agenda_bookings
       SET status = ?1, appointment_id = ?2, message = ?3,
           confirmation_sent_at = ?4, reminder_sent_at = ?5,
           confirmed_at = ?6, updated_at = ?7
       WHERE id = ?8 AND store_id = ?9`,
    )
    .bind(
      nextStatus,
      appointmentId,
      message,
      confirmationSentAt ?? null,
      reminderSentAt ?? null,
      confirmedAt ?? null,
      now,
      booking.id,
      store.id,
    );
  if (TERMINAL_STATUSES.has(nextStatus)) {
    await database.batch([
      update,
      database.prepare("DELETE FROM agenda_booking_locks WHERE booking_id = ?1").bind(booking.id),
    ]);
  } else {
    await update.run();
  }

  const updated = await database
    .prepare("SELECT * FROM agenda_bookings WHERE id = ?1 LIMIT 1")
    .bind(booking.id)
    .first<BookingRow>();
  if (!updated) throw new Error("Booking update succeeded but row was not found");
  return agendaJson({ ok: true, booking: bookingInternal(updated, store.instance) });
}

export async function getBookingStatus(request: Request, slug: string, bookingId: string) {
  const token = stringValue(new URL(request.url).searchParams.get("token"), 256);
  if (!token) {
    throw new AgendaBookingError(401, "status_token_required", "Informe o código de acompanhamento.");
  }
  const database = getAgendaD1();
  const { store } = await publicStore(database, slug);
  const tokenHash = await sha256(token);
  const booking = await database
    .prepare(
      `SELECT * FROM agenda_bookings
       WHERE id = ?1 AND store_id = ?2 AND status_token_hash = ?3
       LIMIT 1`,
    )
    .bind(stringValue(bookingId, 128), store.id, tokenHash)
    .first<BookingRow>();
  if (!booking) {
    throw new AgendaBookingError(404, "booking_not_found", "Agendamento não encontrado.");
  }
  return agendaJson({
    ok: true,
    booking: {
      id: booking.id,
      status: booking.status,
      message: booking.message || "",
      startsAt: booking.starts_at,
      serviceName: booking.service_name,
    },
  });
}
