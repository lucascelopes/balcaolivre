export type EvolutionConfig = {
  baseUrl: string;
  apiKey: string;
};

export type WhatsAppProvider =
  | { kind: "meta" }
  | { kind: "evolution"; config: EvolutionConfig }
  | { kind: "invalid"; message: string };

type EnvReader = (name: string) => string | undefined;
type Fetcher = (
  input: string | URL | Request,
  init?: RequestInit,
) => Response | Promise<Response>;

type EvolutionSuccess<T> = {
  ok: true;
  status: number;
  data: T;
};

type EvolutionFailure = {
  ok: false;
  status: number;
  message: string;
  notFound: boolean;
};

export type EvolutionResult<T = Record<string, unknown>> =
  | EvolutionSuccess<T>
  | EvolutionFailure;

export type EvolutionConnectionState = {
  state: string;
  raw: Record<string, unknown>;
};

export type EvolutionQr = {
  image: string;
  pairingCode: string;
  code: string;
};

export type EvolutionQrImage = {
  contentType: "image/png" | "image/jpeg";
  bytes: Uint8Array;
};

export type EvolutionDisconnectResult = {
  loggedOut: boolean;
  deleted: boolean;
};

const DEFAULT_TIMEOUT_MS = 4_000;
const DEFAULT_WEBHOOK_URL = "http://bot:8090/webhook/evolution";

export function resolveWhatsAppProvider(
  env: EnvReader = (name) => Deno.env.get(name),
): WhatsAppProvider {
  const rawBaseUrl = stringValue(env("WHATSAPP_EVOLUTION_BASE_URL")).replace(
    /\/+$/,
    "",
  );
  const apiKey = stringValue(env("WHATSAPP_EVOLUTION_API_KEY"));

  if (!rawBaseUrl && !apiKey) {
    return { kind: "meta" };
  }

  if (!rawBaseUrl || !apiKey) {
    return {
      kind: "invalid",
      message: "Configuracao Evolution incompleta no Supabase.",
    };
  }

  try {
    const url = new URL(rawBaseUrl);
    const localHost = ["localhost", "127.0.0.1", "host.docker.internal"]
      .includes(url.hostname);
    if (url.protocol !== "https:" && !(url.protocol === "http:" && localHost)) {
      return {
        kind: "invalid",
        message: "A URL publica da Evolution precisa usar HTTPS.",
      };
    }

    return {
      kind: "evolution",
      config: {
        baseUrl: url.toString().replace(/\/+$/, ""),
        apiKey,
      },
    };
  } catch (_error) {
    return {
      kind: "invalid",
      message: "URL publica da Evolution invalida no Supabase.",
    };
  }
}

export function createOnboardingStateKey(provider: "meta" | "evolution") {
  const prefix = provider === "evolution" ? "evo" : "meta";
  return `${prefix}_${crypto.randomUUID().replace(/-/g, "")}`;
}

export function onboardingProviderFromState(state: string) {
  return String(state ?? "").startsWith("evo_")
    ? "evolution" as const
    : "meta" as const;
}

export async function evolutionInstanceName(licenseKey: string) {
  const normalized = String(licenseKey ?? "").trim().toUpperCase();
  if (!normalized) {
    throw new Error(
      "Licenca obrigatoria para identificar a instancia Evolution.",
    );
  }

  const digest = new Uint8Array(
    await crypto.subtle.digest(
      "SHA-256",
      new TextEncoder().encode(`balcao-whatsapp:evolution:v1:${normalized}`),
    ),
  );
  const hex = Array.from(digest)
    .map((value) => value.toString(16).padStart(2, "0"))
    .join("");
  return `bl-${hex.slice(0, 32)}`;
}

export function evolutionHealth(
  config: EvolutionConfig,
  fetcher: Fetcher = fetch,
): Promise<EvolutionResult> {
  return evolutionRequest(
    config,
    "/instance/fetchInstances",
    { method: "GET" },
    fetcher,
    8_000,
  );
}

export async function evolutionConnectionState(
  config: EvolutionConfig,
  instanceName: string,
  fetcher: Fetcher = fetch,
): Promise<EvolutionResult<EvolutionConnectionState>> {
  const result = await evolutionRequest<Record<string, unknown>>(
    config,
    `/instance/connectionState/${encodeURIComponent(instanceName)}`,
    { method: "GET" },
    fetcher,
    2_500,
  );
  if (!result.ok) return result;

  const instance = recordValue(result.data.instance);
  const state = stringValue(instance.state || result.data.state).toLowerCase();
  if (!state) {
    return {
      ok: false,
      status: 502,
      message: "Evolution retornou um estado de conexao invalido.",
      notFound: false,
    };
  }

  return {
    ok: true,
    status: result.status,
    data: { state, raw: result.data },
  };
}

export function createEvolutionInstance(
  config: EvolutionConfig,
  instanceName: string,
  fetcher: Fetcher = fetch,
  webhookUrl = stringValue(Deno.env.get("WHATSAPP_EVOLUTION_WEBHOOK_URL")) ||
    DEFAULT_WEBHOOK_URL,
): Promise<EvolutionResult> {
  return evolutionRequest(
    config,
    "/instance/create",
    {
      method: "POST",
      body: JSON.stringify({
        instanceName,
        integration: "WHATSAPP-BAILEYS",
        qrcode: false,
        rejectCall: true,
        webhook: {
          enabled: true,
          url: webhookUrl,
          byEvents: false,
          base64: false,
          events: [
            "QRCODE_UPDATED",
            "MESSAGES_UPSERT",
            "MESSAGES_UPDATE",
            "CONNECTION_UPDATE",
            "SEND_MESSAGE",
          ],
        },
      }),
    },
    fetcher,
    4_000,
  );
}

export function connectEvolutionInstance(
  config: EvolutionConfig,
  instanceName: string,
  fetcher: Fetcher = fetch,
): Promise<EvolutionResult> {
  return evolutionRequest(
    config,
    `/instance/connect/${encodeURIComponent(instanceName)}`,
    { method: "GET" },
    fetcher,
    12_000,
  );
}

export function sendEvolutionText(
  config: EvolutionConfig,
  instanceName: string,
  phone: string,
  message: string,
  fetcher: Fetcher = fetch,
): Promise<EvolutionResult> {
  return evolutionRequest(
    config,
    `/message/sendText/${encodeURIComponent(instanceName)}`,
    {
      method: "POST",
      body: JSON.stringify({
        number: phone,
        text: message,
        delay: 800,
        linkPreview: false,
      }),
    },
    fetcher,
    4_500,
  );
}

export function findEvolutionMessages(
  config: EvolutionConfig,
  instanceName: string,
  limit: unknown = 50,
  fetcher: Fetcher = fetch,
): Promise<EvolutionResult<unknown>> {
  return evolutionRequest(
    config,
    `/chat/findMessages/${encodeURIComponent(instanceName)}`,
    {
      method: "POST",
      body: JSON.stringify({
        where: {},
        page: 1,
        limit: normalizeEvolutionMessageLimit(limit),
      }),
    },
    fetcher,
    8_000,
  );
}

export async function disconnectEvolutionInstance(
  config: EvolutionConfig,
  instanceName: string,
  fetcher: Fetcher = fetch,
): Promise<EvolutionResult<EvolutionDisconnectResult>> {
  const encodedInstance = encodeURIComponent(instanceName);
  const loggedOut = await evolutionRequest(
    config,
    `/instance/logout/${encodedInstance}`,
    { method: "DELETE" },
    fetcher,
    8_000,
  );

  // Excluir tambem remove credenciais persistidas e permite conectar outro numero.
  // A exclusao continua mesmo se o logout falhar, pois a instancia pode ja estar fechada.
  const deleted = await evolutionRequest(
    config,
    `/instance/delete/${encodedInstance}`,
    { method: "DELETE" },
    fetcher,
    8_000,
  );
  if (!deleted.ok && !deleted.notFound) {
    return deleted;
  }

  return {
    ok: true,
    status: deleted.ok ? deleted.status : loggedOut.ok ? loggedOut.status : 200,
    data: {
      loggedOut: loggedOut.ok,
      deleted: deleted.ok,
    },
  };
}

export function normalizeEvolutionMessageLimit(value: unknown) {
  const parsed = typeof value === "number"
    ? value
    : Number.parseInt(String(value ?? ""), 10);
  if (!Number.isFinite(parsed)) return 50;
  return Math.min(100, Math.max(1, Math.trunc(parsed)));
}

export function extractEvolutionMessages(value: unknown, limit: unknown = 50) {
  const maximum = normalizeEvolutionMessageLimit(limit);
  const root = recordValue(value);
  const rootMessages = root.messages;
  const data = recordValue(root.data);
  const dataMessages = data.messages;
  const candidates = [
    Array.isArray(value) ? value : null,
    Array.isArray(root.records) ? root.records : null,
    Array.isArray(rootMessages) ? rootMessages : null,
    Array.isArray(recordValue(rootMessages).records)
      ? recordValue(rootMessages).records
      : null,
    Array.isArray(root.data) ? root.data : null,
    Array.isArray(data.records) ? data.records : null,
    Array.isArray(dataMessages) ? dataMessages : null,
    Array.isArray(recordValue(dataMessages).records)
      ? recordValue(dataMessages).records
      : null,
  ];

  const records =
    candidates.find((candidate): candidate is unknown[] =>
      Array.isArray(candidate)
    ) ?? [];
  return records.slice(0, maximum);
}

export function extractEvolutionQr(value: unknown): EvolutionQr {
  const root = recordValue(value);
  const qr = recordValue(root.qrcode);
  const image = safeQrImage(firstNonEmpty(qr.base64, root.base64));
  return {
    image,
    pairingCode: firstNonEmpty(qr.pairingCode, root.pairingCode),
    code: firstNonEmpty(qr.code, root.code),
  };
}

export function decodeEvolutionQrImage(value: string): EvolutionQrImage | null {
  const match = /^data:image\/(png|jpeg);base64,([a-z0-9+/=]+)$/i.exec(
    String(value ?? ""),
  );
  if (!match || match[2].length > 2_000_000) return null;

  try {
    const decoded = atob(match[2]);
    const bytes = Uint8Array.from(
      decoded,
      (character) => character.charCodeAt(0),
    );
    return {
      contentType: match[1].toLowerCase() === "jpeg"
        ? "image/jpeg"
        : "image/png",
      bytes,
    };
  } catch (_error) {
    return null;
  }
}

async function evolutionRequest<T = Record<string, unknown>>(
  config: EvolutionConfig,
  path: string,
  init: RequestInit,
  fetcher: Fetcher,
  timeoutMs = DEFAULT_TIMEOUT_MS,
): Promise<EvolutionResult<T>> {
  const headers = new Headers(init.headers);
  headers.set("apikey", config.apiKey);
  if (init.body && !headers.has("content-type")) {
    headers.set("content-type", "application/json");
  }

  try {
    const response = await fetcher(`${config.baseUrl}${path}`, {
      ...init,
      headers,
      signal: init.signal ?? AbortSignal.timeout(timeoutMs),
    });
    const body = await response.text();
    const parsed = parseJson(body);
    if (!response.ok) {
      return {
        ok: false,
        status: response.status,
        message: evolutionErrorMessage(response.status, parsed, body),
        notFound: response.status === 404,
      };
    }

    return {
      ok: true,
      status: response.status,
      data: parsed as T,
    };
  } catch (error) {
    return {
      ok: false,
      status: 502,
      message: error instanceof DOMException && error.name === "TimeoutError"
        ? "Evolution demorou demais para responder."
        : "Servidor Evolution indisponivel agora.",
      notFound: false,
    };
  }
}

function evolutionErrorMessage(status: number, parsed: unknown, raw: string) {
  const root = recordValue(parsed);
  const response = recordValue(root.response);
  const candidates = [response.message, root.message, root.error];
  for (const candidate of candidates) {
    if (Array.isArray(candidate)) {
      const text = candidate.map(stringValue).filter(Boolean).join(" ");
      if (text) return safeMessage(text);
    }
    const text = stringValue(candidate);
    if (text) return safeMessage(text);
  }

  const compact = safeMessage(raw);
  return compact || `Evolution retornou HTTP ${status}.`;
}

function safeMessage(value: unknown) {
  const compact = stringValue(value).replace(/\s+/g, " ");
  return compact.length > 240 ? compact.slice(0, 240) : compact;
}

function safeQrImage(value: unknown) {
  const raw = stringValue(value);
  if (/^data:image\/(?:png|jpeg);base64,[a-z0-9+/=\r\n]+$/i.test(raw)) {
    return raw.replace(/[\r\n]/g, "");
  }
  if (/^[a-z0-9+/=\r\n]+$/i.test(raw) && raw.length >= 100) {
    return `data:image/png;base64,${raw.replace(/[\r\n]/g, "")}`;
  }
  return "";
}

function parseJson(value: string): unknown {
  if (!value) return {};
  try {
    return JSON.parse(value);
  } catch (_error) {
    return {};
  }
}

function recordValue(value: unknown): Record<string, unknown> {
  return value && typeof value === "object" && !Array.isArray(value)
    ? value as Record<string, unknown>
    : {};
}

function firstNonEmpty(...values: unknown[]) {
  for (const value of values) {
    const text = stringValue(value);
    if (text) return text;
  }
  return "";
}

function stringValue(value: unknown) {
  return String(value ?? "").trim();
}
