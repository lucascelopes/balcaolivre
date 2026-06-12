import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const LICENSE_SECRET = "BalcaoLivrePDV-local-license-v1";
const OFFLINE_INSTALLER_URL = "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows/BalcaoLivrePDV-Setup-1.2.2026.1.exe";
const ONLINE_INSTALLER_URL = "https://hzvplpotsdzxygkxrgyi.supabase.co/storage/v1/object/public/balcao-livre-updates/windows-online/BalcaoLivrePDVOnline-Setup-1.8.2026.21.exe";
const TRIAL_SOURCE = "landing_trial_download";
const TRIAL_DAYS = 7;
const TRIAL_WHATSAPP_URL = "https://wa.me/5527981267551?text=Ola%2C%20preciso%20liberar%20outro%20teste%20do%20Balcao%20Livre%20PDV.";
const TRIAL_ONLINE_FEATURES = ["pdv", "whatsapp", "cardapio", "garcom", "mercado-pago", "nfce", "equipe", "entregadores"];
const OFFLINE_FEATURES = ["pdv", "caixa", "estoque", "nfce"];

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "GET, OPTIONS",
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  if (req.method !== "GET") {
    return html("Metodo nao permitido", "Use o botao de download da pagina.", false, 405);
  }

  try {
    return await handleTrialDownload(req);
  } catch (error) {
    return html("Download indisponivel", messageFromError(error), false, 500);
  }
});

async function handleTrialDownload(req: Request) {
  const url = new URL(req.url);
  const kind = normalizeTrialKind(url.searchParams.get("plan"));
  const installerUrl = kind === "online" ? ONLINE_INSTALLER_URL : OFFLINE_INSTALLER_URL;
  const clientKind = kind === "online" ? "windows-online" : "windows-offline";
  const now = new Date();
  const expiresAt = new Date(now.getTime() + TRIAL_DAYS * 24 * 60 * 60 * 1000);
  const ip = requestIp(req);
  const userAgent = stringValue(req.headers.get("user-agent"));
  const trialIpHash = (await signature(`trial-ip|${ip || "unknown"}`)).slice(0, 32);
  const userAgentHash = userAgent ? (await signature(`trial-ua|${userAgent}`)).slice(0, 32) : "";
  const supabase = serviceClient();

  const existing = await supabase
    .from("bv_licenses")
    .select("key, expires_at")
    .in("status", ["DISPONIVEL", "ATIVA"])
    .contains("profile", { source: TRIAL_SOURCE, installer: kind, trial_ip_hash: trialIpHash })
    .gt("expires_at", now.toISOString())
    .limit(1);

  if (existing.error) {
    throw new Error(`Supabase recusou gerar a chave de teste: ${existing.error.message}`);
  }

  if ((existing.data ?? []).length > 0) {
    await supabase.from("bv_license_events").insert({
      license_key: stringValue(existing.data?.[0]?.key),
      event_type: "trial.download.blocked_by_ip",
      message: "Nova chave de teste bloqueada por IP.",
      payload: { source: TRIAL_SOURCE, installer: kind, trialIpHash, userAgentHash },
    });
    return Response.redirect(TRIAL_WHATSAPP_URL, 303);
  }

  const expiresText = activationExpirationText(expiresAt);
  const serialPrefix = kind === "online" ? "ONL" : "OFF";
  const serial = `${serialPrefix}${crypto.randomUUID().replaceAll("-", "").slice(0, 9).toUpperCase()}`;
  const activationSignature = (await signature(`BLV|${expiresText}|${serial}`)).slice(0, 10);
  const key = `BLV-${expiresText}-${serial}-${activationSignature}`;
  const profile = {
    source: TRIAL_SOURCE,
    installer: kind,
    trial_days: TRIAL_DAYS,
    features: kind === "online" ? TRIAL_ONLINE_FEATURES : OFFLINE_FEATURES,
    ifood_enabled: false,
    whatsapp_enabled: kind === "online",
    trial_ip_hash: trialIpHash,
    user_agent_hash: userAgentHash,
    generated_at: now.toISOString(),
    installer_url: installerUrl,
  };

  const created = await supabase.from("bv_licenses").insert({
    key,
    status: "DISPONIVEL",
    plan: kind === "online" ? "Teste Online 7 dias" : "Teste Offline 7 dias",
    customer_name: "Teste gerado no download",
    client_kind: clientKind,
    profile,
    settings: {},
    metrics: {},
    expires_at: expiresAt.toISOString(),
    updated_at: now.toISOString(),
  });

  if (created.error) {
    throw new Error(`Supabase recusou salvar a chave de teste: ${created.error.message}`);
  }

  await supabase.from("bv_license_events").insert({
    license_key: key,
    event_type: "trial.download.generated",
    message: "Chave de teste gerada no download.",
    payload: { source: TRIAL_SOURCE, installer: kind, trialIpHash, userAgentHash, generatedAt: now.toISOString() },
  });

  return new Response(null, {
    status: 302,
    headers: {
      ...corsHeaders,
      Location: installerUrl,
      "Cache-Control": "no-store, max-age=0",
    },
  });
}
function serviceClient() {
  const url = Deno.env.get("SUPABASE_URL") ?? "";
  const key = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "";
  if (!url || !key) {
    throw new Error("Supabase service role indisponivel.");
  }
  return createClient(url, key, { auth: { persistSession: false } });
}

async function signature(message: string) {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(LICENSE_SECRET),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const data = await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(message));
  return Array.from(new Uint8Array(data))
    .map((byte) => byte.toString(16).padStart(2, "0"))
    .join("")
    .toUpperCase();
}

function activationExpirationText(date: Date) {
  const pad = (value: number) => String(value).padStart(2, "0");
  return `${date.getUTCFullYear()}${pad(date.getUTCMonth() + 1)}${pad(date.getUTCDate())}${pad(date.getUTCHours())}${pad(date.getUTCMinutes())}`;
}

function normalizeTrialKind(value: unknown): "offline" | "online" {
  return stringValue(value).toLowerCase().includes("online") ? "online" : "offline";
}

function requestIp(req: Request) {
  const forwardedFor = stringValue(req.headers.get("x-forwarded-for")).split(",")[0]?.trim();
  return stringValue(req.headers.get("cf-connecting-ip"))
    || stringValue(req.headers.get("x-real-ip"))
    || stringValue(req.headers.get("x-nf-client-connection-ip"))
    || forwardedFor
    || "unknown";
}

function html(title: string, message: string, ok: boolean, status = ok ? 200 : 400) {
  return new Response(`<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${escapeHtml(title)}</title><body style="font-family:Segoe UI,Arial,sans-serif;background:#eef3f6;color:#17212b;margin:0;display:grid;place-items:center;min-height:100vh"><main style="max-width:560px;background:white;border:1px solid #d8e2ec;border-radius:14px;padding:30px;box-shadow:0 18px 44px rgba(22,34,45,.10)"><h1 style="margin:0 0 12px;color:${ok ? "#0f766e" : "#a11d1d"}">${escapeHtml(title)}</h1><p style="font-size:17px;line-height:1.55">${escapeHtml(message)}</p><a href="https://wa.me/5527981267551?text=Ola%2C%20preciso%20liberar%20um%20teste%20do%20Balcao%20Livre%20PDV." style="display:inline-flex;margin-top:10px;padding:12px 16px;border-radius:8px;background:#0f766e;color:white;text-decoration:none;font-weight:800">Falar no WhatsApp</a></main></body></html>`, {
    status: 200,
    headers: { ...corsHeaders, "Content-Type": "text/html; charset=utf-8", "Cache-Control": "no-store, max-age=0" },
  });
}

function stringValue(value: unknown) {
  return String(value ?? "").trim();
}

function escapeHtml(value: unknown) {
  return stringValue(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function messageFromError(error: unknown) {
  return error instanceof Error ? error.message : String(error);
}
