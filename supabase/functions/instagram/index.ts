import { createClient } from "https://esm.sh/@supabase/supabase-js@2.57.4";

const DEFAULT_GRAPH_VERSION = "v25.0";
const DEFAULT_OAUTH_SCOPES = [
  "instagram_business_basic",
  "instagram_business_manage_messages",
  "instagram_business_manage_comments",
  "instagram_business_content_publish",
];
const OAUTH_STATE_TTL_MINUTES = 15;
const MAX_WEBHOOK_BYTES = 2 * 1024 * 1024;
const MAX_CLIENT_BODY_BYTES = 64 * 1024;
const MAX_MESSAGE_LENGTH = 1000;
const MAX_CAPTION_LENGTH = 2200;
const CONNECTIONS_TABLE = "balcao_instagram_connections";
const OAUTH_STATES_TABLE = "balcao_instagram_oauth_states";
const WEBHOOK_EVENTS_TABLE = "balcao_instagram_webhook_events";
const MESSAGES_TABLE = "balcao_instagram_messages";
const PUBLICATIONS_TABLE = "balcao_instagram_publications";
const RATE_LIMITS_TABLE = "balcao_instagram_rate_limits";

const routeRateLimits: Record<string, { max: number; windowSeconds: number }> =
  {
    "/oauth/start": { max: 5, windowSeconds: 10 * 60 },
    "/status": { max: 120, windowSeconds: 60 },
    "/disconnect": { max: 10, windowSeconds: 10 * 60 },
    "/messages": { max: 60, windowSeconds: 60 },
    "/messages/send": { max: 30, windowSeconds: 60 },
    "/publications": { max: 60, windowSeconds: 60 },
    "/publish": { max: 10, windowSeconds: 60 * 60 },
  };

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": [
    "authorization",
    "x-client-info",
    "apikey",
    "content-type",
    "x-balcao-license",
    "x-balcao-machine",
    "x-balcao-machine-code",
    "x-balcao-app-version",
    "x-hub-signature-256",
  ].join(", "),
  "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
};

type JsonObject = Record<string, unknown>;

type Database = {
  public: {
    Tables: Record<string, {
      Row: JsonObject;
      Insert: JsonObject;
      Update: JsonObject;
      Relationships: [];
    }>;
    Views: Record<string, never>;
    Functions: Record<string, never>;
    Enums: Record<string, never>;
    CompositeTypes: Record<string, never>;
  };
};

type ServiceClient = ReturnType<typeof createClient<Database>>;

type ClientIdentity = {
  licenseKey: string;
  machineHash: string;
  machineCode: string;
  appVersion: string;
};

type InstagramConnection = {
  license_key: string;
  machine_hash: string;
  instagram_user_id: string;
  username: string;
  display_name: string;
  account_type: string;
  access_token: string;
  token_type: string;
  token_expires_at: string | null;
  scopes: string[];
  status: string;
  webhook_subscribed_at: string | null;
  last_error: string;
  connected_at: string;
  updated_at: string;
  meta_payload: JsonObject;
};

type OAuthState = {
  state: string;
  license_key: string;
  machine_hash: string;
  redirect_uri: string;
  scopes: string[];
  expires_at: string;
  used_at: string | null;
  created_at: string;
};

type ClientAuthResult =
  | {
    ok: true;
    client: ServiceClient;
    identity: ClientIdentity;
    connection: InstagramConnection | null;
  }
  | { ok: false; status: number; message: string };

type MetaResult = {
  ok: boolean;
  status: number;
  data: JsonObject;
  message: string;
};

class HttpError extends Error {
  constructor(public status: number, message: string) {
    super(message);
    this.name = "HttpError";
  }
}

let cachedServiceClient: ServiceClient | null | undefined;

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const url = new URL(req.url);
    const route = routeFromPath(url.pathname);

    if (route === "/health" && req.method === "GET") {
      return health();
    }

    if (route === "/oauth/start" && req.method === "POST") {
      return await startOAuth(req, url);
    }

    if (route === "/oauth/callback" && req.method === "GET") {
      return await completeOAuth(url);
    }

    if (route === "/webhook") {
      if (req.method === "GET") return verifyWebhook(url);
      if (req.method === "POST") return await receiveWebhook(req);
      return json({ ok: false, message: "Metodo nao permitido." }, 405);
    }

    if (req.method !== "POST") {
      return json({ ok: false, message: "Metodo nao permitido." }, 405);
    }

    if (route === "/status") return await connectionStatus(req, url);
    if (route === "/disconnect") return await disconnect(req, url);
    if (route === "/messages") return await listMessages(req, url);
    if (route === "/messages/send") return await sendMessage(req, url);
    if (route === "/publications") return await listPublications(req, url);
    if (route === "/publish") return await publish(req, url);

    return json({ ok: false, message: "Rota Instagram nao encontrada." }, 404);
  } catch (error) {
    const status = error instanceof HttpError ? error.status : 500;
    return json({ ok: false, message: messageFromError(error) }, status);
  }
});

function health() {
  const appIdConfigured = Boolean(metaAppId());
  const appSecretConfigured = Boolean(metaAppSecret());
  const verifyTokenConfigured = Boolean(metaVerifyToken());
  const databaseConfigured = Boolean(serviceClient());

  return json({
    ok: true,
    provider: "instagram",
    configured: appIdConfigured && appSecretConfigured &&
      verifyTokenConfigured && databaseConfigured,
    appIdConfigured,
    appSecretConfigured,
    verifyTokenConfigured,
    databaseConfigured,
    graphVersion: graphVersion(),
    signedLicenseFallbackEnabled: allowSignedLicenseFallback(),
  });
}

async function startOAuth(req: Request, url: URL) {
  const payload = await readJson(req);
  if (!metaAppId() || !metaAppSecret()) {
    return json({
      ok: false,
      message: "Instagram Meta nao configurado no Supabase.",
    }, 503);
  }

  const auth = await authenticateClient(req, url, payload);
  if (!auth.ok) return json({ ok: false, message: auth.message }, auth.status);

  const redirectUri = metaRedirectUri(url);
  const scopes = oauthScopes();
  const state = randomToken(32);
  const expiresAt = new Date(Date.now() + OAUTH_STATE_TTL_MINUTES * 60_000)
    .toISOString();

  await auth.client
    .from(OAUTH_STATES_TABLE)
    .delete()
    .lt("expires_at", new Date().toISOString());

  const { error } = await auth.client.from(OAUTH_STATES_TABLE).insert({
    state,
    license_key: auth.identity.licenseKey,
    machine_hash: auth.identity.machineHash,
    redirect_uri: redirectUri,
    scopes,
    expires_at: expiresAt,
  });

  if (error) {
    return json({
      ok: false,
      message: `Nao foi possivel iniciar o OAuth: ${error.message}`,
    }, 503);
  }

  const authorizationUrl = buildInstagramAuthorizationUrl({
    state,
    redirectUri,
    scopes,
  });

  return json({
    ok: true,
    authorizationUrl,
    message: "Abra a autorizacao da Meta para conectar o Instagram.",
    expiresAt,
  });
}

async function completeOAuth(url: URL) {
  const oauthError = redactSensitiveText(stringValue(
    url.searchParams.get("error_message") ||
      url.searchParams.get("error_description") ||
      url.searchParams.get("error"),
  ));
  if (oauthError) {
    return html(oauthPage(false, "Instagram nao conectado", oauthError), 400);
  }

  const stateKey = stringValue(url.searchParams.get("state"));
  const code = stringValue(url.searchParams.get("code"));
  if (!stateKey || !code) {
    return html(
      oauthPage(
        false,
        "Instagram nao conectado",
        "A Meta nao retornou codigo e state validos.",
      ),
      400,
    );
  }

  const consumed = await consumeOAuthState(stateKey);
  if (!consumed.ok) {
    return html(
      oauthPage(false, "Conexao expirada", consumed.message),
      consumed.status,
    );
  }

  const shortToken = await exchangeAuthorizationCode(
    code,
    consumed.state.redirect_uri,
  );
  if (!shortToken.ok) {
    return html(
      oauthPage(false, "Falha ao obter acesso", shortToken.message),
      502,
    );
  }

  const shortAccessToken = stringValue(shortToken.data.access_token);
  const shortUserId = stringValue(
    shortToken.data.user_id || shortToken.data.id,
  );
  if (!shortAccessToken) {
    return html(
      oauthPage(
        false,
        "Falha ao obter acesso",
        "A Meta nao retornou um token de acesso.",
      ),
      502,
    );
  }

  const longToken = await exchangeLongLivedToken(shortAccessToken);
  const accessToken = longToken.ok
    ? stringValue(longToken.data.access_token)
    : shortAccessToken;
  const expiresIn = positiveNumber(
    longToken.ok ? longToken.data.expires_in : shortToken.data.expires_in,
    longToken.ok ? 60 * 24 * 60 * 60 : 60 * 60,
  );
  const tokenExpiresAt = new Date(Date.now() + expiresIn * 1000).toISOString();

  const profile = await fetchInstagramProfile(accessToken);
  if (!profile.ok) {
    return html(oauthPage(false, "Falha ao ler perfil", profile.message), 502);
  }

  const instagramUserId = stringValue(
    profile.data.id || profile.data.user_id || shortUserId,
  );
  const username = stringValue(profile.data.username);
  if (!instagramUserId) {
    return html(
      oauthPage(
        false,
        "Falha ao ler perfil",
        "A Meta nao retornou o ID da conta profissional.",
      ),
      502,
    );
  }

  const client = serviceClient();
  if (!client) {
    return html(
      oauthPage(
        false,
        "Backend indisponivel",
        "Supabase service role nao configurado.",
      ),
      503,
    );
  }

  const { data: owner, error: ownerError } = await client
    .from(CONNECTIONS_TABLE)
    .select("license_key")
    .eq("instagram_user_id", instagramUserId)
    .maybeSingle();
  if (ownerError) {
    return html(
      oauthPage(false, "Falha ao salvar conexao", ownerError.message),
      503,
    );
  }
  if (
    owner &&
    normalizeLicense(owner.license_key) !==
      normalizeLicense(consumed.state.license_key)
  ) {
    return html(
      oauthPage(
        false,
        "Conta ja vinculada",
        "Esta conta Instagram ja esta vinculada a outra licenca.",
      ),
      409,
    );
  }

  const subscription = await subscribeInstagramWebhooks(
    accessToken,
    instagramUserId,
  );
  const now = new Date().toISOString();
  const connection: InstagramConnection = {
    license_key: normalizeLicense(consumed.state.license_key),
    machine_hash: stringValue(consumed.state.machine_hash),
    instagram_user_id: instagramUserId,
    username,
    display_name: stringValue(profile.data.name),
    account_type: stringValue(profile.data.account_type),
    access_token: accessToken,
    token_type: stringValue(
      longToken.data.token_type || shortToken.data.token_type || "bearer",
    ),
    token_expires_at: tokenExpiresAt,
    scopes: consumed.state.scopes ?? oauthScopes(),
    status: "ATIVO",
    webhook_subscribed_at: subscription.ok ? now : null,
    last_error: subscription.ok ? "" : subscription.message,
    connected_at: now,
    updated_at: now,
    meta_payload: {
      profile: withoutSensitiveFields(profile.data),
      longLivedToken: longToken.ok,
      webhookSubscriptionOk: subscription.ok,
      webhookSubscriptionStatus: subscription.status,
    },
  };

  const { error: saveError } = await client
    .from(CONNECTIONS_TABLE)
    .upsert(connection, { onConflict: "license_key" });
  if (saveError) {
    return html(
      oauthPage(false, "Falha ao salvar conexao", saveError.message),
      503,
    );
  }

  const detail = subscription.ok
    ? `Conta @${
      username || instagramUserId
    } conectada. Pode voltar ao Agenda Livre.`
    : `Conta @${
      username || instagramUserId
    } conectada, mas o webhook ficou pendente: ${subscription.message}`;
  return html(oauthPage(true, "Instagram conectado", detail));
}

async function connectionStatus(req: Request, url: URL) {
  const payload = await readJson(req);
  const auth = await authenticateClient(req, url, payload);
  if (!auth.ok) return json({ ok: false, message: auth.message }, auth.status);

  if (!auth.connection) {
    return json({
      ok: true,
      connected: false,
      username: "",
      displayName: "",
      instagramUserId: "",
      status: "NAO_CONECTADO",
      message: "Instagram nao conectado.",
      tokenExpiresAt: null,
    });
  }

  const refreshed = await ensureFreshConnection(auth.client, auth.connection);
  const connection = refreshed.connection;

  return json({
    ok: true,
    connected: refreshed.ok && connection.status === "ATIVO",
    message: refreshed.ok ? "Instagram conectado." : refreshed.message,
    ...safeConnection(connection),
  });
}

async function disconnect(req: Request, url: URL) {
  const payload = await readJson(req);
  const auth = await authenticateClient(req, url, payload);
  if (!auth.ok) return json({ ok: false, message: auth.message }, auth.status);

  const connection = auth.connection;
  if (!connection) {
    return json({
      ok: true,
      disconnected: true,
      message: "Instagram ja estava desconectado.",
    });
  }

  await revokeInstagramAccess(connection.access_token);

  const { error } = await auth.client
    .from(CONNECTIONS_TABLE)
    .delete()
    .eq("license_key", auth.identity.licenseKey)
    .eq("machine_hash", auth.identity.machineHash);
  if (error) {
    return json({
      ok: false,
      message: `Nao foi possivel desconectar: ${error.message}`,
    }, 503);
  }

  return json({
    ok: true,
    disconnected: true,
    message: "Instagram desconectado.",
  });
}

function verifyWebhook(url: URL) {
  const configuredToken = metaVerifyToken();
  if (!configuredToken) {
    return text("Instagram verify token not configured.", 503);
  }

  const mode = stringValue(url.searchParams.get("hub.mode"));
  const token = stringValue(url.searchParams.get("hub.verify_token"));
  const challenge = stringValue(url.searchParams.get("hub.challenge"));

  if (
    mode.toLowerCase() === "subscribe" && challenge &&
    constantTimeEqual(token, configuredToken)
  ) {
    return text(challenge);
  }

  return text("Instagram webhook verification failed.", 403);
}

async function receiveWebhook(req: Request) {
  const rawBody = await readBodyText(
    req,
    MAX_WEBHOOK_BYTES,
    "Webhook excedeu o tamanho permitido.",
  );

  const signature = stringValue(req.headers.get("x-hub-signature-256"));
  if (!await verifyMetaSignature(rawBody, signature)) {
    return json({ ok: false, message: "Assinatura Meta invalida." }, 401);
  }

  let payload: JsonObject;
  try {
    const parsed = JSON.parse(rawBody);
    payload = asRecord(parsed);
  } catch (_error) {
    return json({ ok: false, message: "Webhook Instagram invalido." }, 400);
  }

  if (stringValue(payload.object).toLowerCase() !== "instagram") {
    return json(
      { ok: false, message: "Objeto de webhook nao suportado." },
      400,
    );
  }

  const client = serviceClient();
  if (!client) {
    return json({
      ok: false,
      message: "Supabase service role nao configurado.",
    }, 503);
  }

  const persisted = await persistWebhookPayload(client, payload);
  if (!persisted.ok) {
    return json({ ok: false, message: persisted.message }, 503);
  }

  return json({
    ok: true,
    received: true,
    accepted: persisted.accepted,
    duplicates: persisted.duplicates,
  });
}

async function listMessages(req: Request, url: URL) {
  const payload = await readJson(req);
  const auth = await authenticateClient(req, url, payload);
  if (!auth.ok) return json({ ok: false, message: auth.message }, auth.status);

  const limit = Math.min(
    200,
    Math.max(1, Math.trunc(positiveNumber(payload.limit, 100))),
  );
  const since = validIsoDate(payload.since);
  const scopedUserId = stringValue(
    payload.instagramScopedUserId || payload.recipientId,
  );

  let query = auth.client
    .from(MESSAGES_TABLE)
    .select(
      "id,instagram_user_id,instagram_scoped_user_id,meta_message_id,direction,message_type,message_text,status,payload,created_at,sent_at",
    )
    .eq("license_key", auth.identity.licenseKey)
    .order("created_at", { ascending: false })
    .limit(limit);

  if (since) query = query.gt("created_at", since);
  if (scopedUserId) query = query.eq("instagram_scoped_user_id", scopedUserId);

  const { data, error } = await query;
  if (error) {
    return json({
      ok: false,
      message: `Nao foi possivel carregar mensagens: ${error.message}`,
    }, 503);
  }

  return json({
    ok: true,
    messages: (data ?? []).map((row) => {
      const messagePayload = asRecord(row.payload);
      const sender = asRecord(messagePayload.sender);
      return {
        id: row.id,
        instagramScopedId: row.instagram_scoped_user_id,
        senderName: stringValue(sender.name),
        senderUsername: stringValue(sender.username),
        text: row.message_text,
        direction: row.direction,
        createdAt: row.created_at,
        status: row.status,
      };
    }),
  });
}

async function listPublications(req: Request, url: URL) {
  const payload = await readJson(req);
  const auth = await authenticateClient(req, url, payload);
  if (!auth.ok) return json({ ok: false, message: auth.message }, auth.status);
  if (!auth.connection) {
    return json({
      ok: false,
      connected: false,
      publications: [],
      message: "Conecte uma conta Instagram profissional primeiro.",
    }, 428);
  }

  const refreshed = await ensureFreshConnection(auth.client, auth.connection);
  if (!refreshed.ok) {
    return json({
      ok: false,
      connected: false,
      reconnect: true,
      publications: [],
      message: refreshed.message,
    }, 401);
  }

  const limit = Math.min(
    50,
    Math.max(1, Math.trunc(positiveNumber(payload.limit, 30))),
  );
  const connection = refreshed.connection;
  const fields = [
    "id",
    "caption",
    "media_type",
    "media_url",
    "thumbnail_url",
    "permalink",
    "timestamp",
    "username",
    "like_count",
    "comments_count",
  ].join(",");
  const requestUrl = new URL(
    `${graphBaseUrl()}/${graphVersion()}/${
      encodeURIComponent(connection.instagram_user_id)
    }/media`,
  );
  requestUrl.searchParams.set("fields", fields);
  requestUrl.searchParams.set("limit", String(limit));

  const result = await metaRequest(requestUrl.toString(), {
    method: "GET",
    headers: { Authorization: `Bearer ${connection.access_token}` },
  });
  if (!result.ok) {
    return json({
      ok: false,
      connected: true,
      publications: [],
      message: result.message,
    }, result.status || 502);
  }

  const rows = Array.isArray(result.data.data) ? result.data.data : [];
  return json({
    ok: true,
    connected: true,
    username: connection.username,
    publications: rows.map((raw) => {
      const item = asRecord(raw);
      return {
        id: stringValue(item.id),
        caption: stringValue(item.caption),
        mediaType: stringValue(item.media_type),
        mediaUrl: stringValue(item.media_url),
        thumbnailUrl: stringValue(item.thumbnail_url || item.media_url),
        permalink: stringValue(item.permalink),
        timestamp: stringValue(item.timestamp),
        username: stringValue(item.username || connection.username),
        likeCount: Math.max(0, Math.trunc(positiveNumber(item.like_count, 0))),
        commentsCount: Math.max(
          0,
          Math.trunc(positiveNumber(item.comments_count, 0)),
        ),
      };
    }),
  });
}

async function sendMessage(req: Request, url: URL) {
  const payload = await readJson(req);
  const auth = await authenticateClient(req, url, payload);
  if (!auth.ok) return json({ ok: false, message: auth.message }, auth.status);
  if (!auth.connection) {
    return json({
      ok: false,
      message: "Conecte uma conta Instagram profissional primeiro.",
    }, 428);
  }

  const recipientId = stringValue(
    payload.instagramScopedUserId || payload.recipientId,
  );
  const message = stringValue(payload.message || payload.text).slice(
    0,
    MAX_MESSAGE_LENGTH,
  );
  if (!recipientId || recipientId.length > 160) {
    return json({
      ok: false,
      message: "Instagram-scoped ID do destinatario e obrigatorio.",
    }, 400);
  }
  if (!message) {
    return json({ ok: false, message: "Mensagem Instagram vazia." }, 400);
  }

  const conversationWindowStart = new Date(
    Date.now() - 24 * 60 * 60 * 1000,
  ).toISOString();
  const { data: inbound, error: inboundError } = await auth.client
    .from(MESSAGES_TABLE)
    .select("id,created_at")
    .eq("license_key", auth.identity.licenseKey)
    .eq("instagram_user_id", auth.connection.instagram_user_id)
    .eq("instagram_scoped_user_id", recipientId)
    .eq("direction", "entrada")
    .gte("created_at", conversationWindowStart)
    .order("created_at", { ascending: false })
    .limit(1)
    .maybeSingle();
  if (inboundError) {
    return json({
      ok: false,
      message: `Nao foi possivel validar a conversa: ${inboundError.message}`,
    }, 503);
  }
  if (!inbound) {
    return json({
      ok: false,
      message:
        "A janela de 24 horas expirou. O cliente precisa enviar uma nova mensagem no Instagram.",
    }, 428);
  }

  const refreshed = await ensureFreshConnection(auth.client, auth.connection);
  if (!refreshed.ok) {
    return json(
      { ok: false, reconnect: true, message: refreshed.message },
      401,
    );
  }
  const connection = refreshed.connection;

  const result = await metaRequest(
    `${graphBaseUrl()}/${graphVersion()}/${
      encodeURIComponent(connection.instagram_user_id)
    }/messages`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${connection.access_token}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        recipient: { id: recipientId },
        message: { text: message },
      }),
    },
  );

  const messageId = stringValue(result.data.message_id || result.data.id) ||
    `local:${randomToken(18)}`;
  const now = new Date().toISOString();
  const { error: saveError } = await auth.client.from(MESSAGES_TABLE).upsert({
    license_key: auth.identity.licenseKey,
    instagram_user_id: connection.instagram_user_id,
    instagram_scoped_user_id: recipientId,
    meta_message_id: messageId,
    direction: "saida",
    message_type: "text",
    message_text: message,
    status: result.ok ? "enviada" : "erro",
    payload: result.ok
      ? withoutSensitiveFields(result.data)
      : { error: result.message, status: result.status },
    created_at: now,
    sent_at: result.ok ? now : null,
  }, { onConflict: "meta_message_id", ignoreDuplicates: true });

  if (saveError) {
    return json({
      ok: false,
      message: `Mensagem enviada, mas nao foi registrada: ${saveError.message}`,
    }, 503);
  }
  if (!result.ok) {
    return json(
      { ok: false, message: result.message, status: result.status },
      502,
    );
  }

  return json({
    ok: true,
    message: "Mensagem enviada pelo Instagram.",
    remoteMessageId: messageId,
  });
}

async function publish(req: Request, url: URL) {
  const payload = await readJson(req);
  const auth = await authenticateClient(req, url, payload);
  if (!auth.ok) return json({ ok: false, message: auth.message }, auth.status);
  if (!auth.connection) {
    return json({
      ok: false,
      message: "Conecte uma conta Instagram profissional primeiro.",
    }, 428);
  }

  const refreshed = await ensureFreshConnection(auth.client, auth.connection);
  if (!refreshed.ok) {
    return json(
      { ok: false, reconnect: true, message: refreshed.message },
      401,
    );
  }
  const connection = refreshed.connection;

  const requestedPublicationId = stringValue(payload.publicationId);
  let publication: JsonObject;

  if (requestedPublicationId) {
    const { data, error } = await auth.client
      .from(PUBLICATIONS_TABLE)
      .select("*")
      .eq("id", requestedPublicationId)
      .eq("license_key", auth.identity.licenseKey)
      .maybeSingle();
    if (error) return json({ ok: false, message: error.message }, 503);
    if (!data) {
      return json({ ok: false, message: "Publicacao nao encontrada." }, 404);
    }
    publication = asRecord(data);
    if (stringValue(publication.status) === "PUBLICADO") {
      return json({
        ok: true,
        pending: false,
        publicationId: publication.id,
        containerId: publication.container_id,
        mediaId: publication.media_id,
        status: "PUBLICADO",
      });
    }
  } else {
    const mediaUrl = validatePublicMediaUrl(
      payload.mediaUrl || payload.imageUrl || payload.videoUrl,
    );
    if (!mediaUrl.ok) {
      return json({ ok: false, message: mediaUrl.message }, 400);
    }

    const mediaType = normalizeMediaType(payload.mediaType);
    if (!mediaType) {
      return json({
        ok: false,
        message: "mediaType deve ser IMAGE, REELS ou STORIES.",
      }, 400);
    }
    const caption = stringValue(payload.caption).slice(0, MAX_CAPTION_LENGTH);
    const isVideo = Boolean(payload.isVideo) || mediaType === "REELS" ||
      looksLikeVideoUrl(mediaUrl.url);
    const shareToFeed = payload.shareToFeed !== false;

    const { data, error } = await auth.client.from(PUBLICATIONS_TABLE).insert({
      license_key: auth.identity.licenseKey,
      instagram_user_id: connection.instagram_user_id,
      media_type: mediaType,
      media_url: mediaUrl.url,
      caption,
      status: "CRIANDO",
      payload: { isVideo, shareToFeed },
    }).select("*").single();
    if (error || !data) {
      return json({
        ok: false,
        message: `Nao foi possivel registrar a publicacao: ${
          error?.message ?? "resposta vazia"
        }`,
      }, 503);
    }
    publication = asRecord(data);
  }

  const publicationId = stringValue(publication.id);
  let containerId = stringValue(publication.container_id);
  const mediaType = stringValue(publication.media_type).toUpperCase();
  const metadata = asRecord(publication.payload);

  if (!containerId) {
    const createResult = await createMediaContainer(connection, {
      mediaType,
      mediaUrl: stringValue(publication.media_url),
      caption: stringValue(publication.caption),
      isVideo: Boolean(metadata.isVideo),
      shareToFeed: metadata.shareToFeed !== false,
    });
    if (!createResult.ok) {
      await markPublicationError(
        auth.client,
        publicationId,
        createResult.message,
      );
      return json(
        { ok: false, publicationId, message: createResult.message },
        502,
      );
    }

    containerId = stringValue(createResult.data.id);
    if (!containerId) {
      const message = "A Meta nao retornou o ID do container.";
      await markPublicationError(auth.client, publicationId, message);
      return json({ ok: false, publicationId, message }, 502);
    }

    const { error } = await auth.client.from(PUBLICATIONS_TABLE).update({
      container_id: containerId,
      status: "PROCESSANDO",
      updated_at: new Date().toISOString(),
    }).eq("id", publicationId).eq("license_key", auth.identity.licenseKey);
    if (error) {
      return json({ ok: false, publicationId, message: error.message }, 503);
    }
  }

  const ready = await waitForContainer(connection, containerId);
  if (!ready.ok) {
    await markPublicationError(auth.client, publicationId, ready.message);
    return json({
      ok: false,
      publicationId,
      containerId,
      message: ready.message,
    }, 502);
  }
  if (!ready.finished) {
    return json({
      ok: true,
      pending: true,
      publicationId,
      containerId,
      status: "PROCESSANDO",
      message:
        "A Meta ainda esta processando a midia. Repita /publish com publicationId.",
    }, 202);
  }

  const publishResult = await publishMediaContainer(connection, containerId);
  if (!publishResult.ok) {
    await markPublicationError(
      auth.client,
      publicationId,
      publishResult.message,
    );
    return json({
      ok: false,
      publicationId,
      containerId,
      message: publishResult.message,
    }, 502);
  }

  const mediaId = stringValue(publishResult.data.id);
  const now = new Date().toISOString();
  const { error: updateError } = await auth.client.from(PUBLICATIONS_TABLE)
    .update({
      media_id: mediaId || null,
      status: "PUBLICADO",
      error_message: "",
      updated_at: now,
      published_at: now,
    }).eq("id", publicationId).eq("license_key", auth.identity.licenseKey);
  if (updateError) {
    return json({
      ok: false,
      publicationId,
      mediaId,
      message: updateError.message,
    }, 503);
  }

  return json({
    ok: true,
    pending: false,
    publicationId,
    containerId,
    mediaId,
    status: "PUBLICADO",
  });
}

async function authenticateClient(
  req: Request,
  url: URL,
  payload: JsonObject,
): Promise<ClientAuthResult> {
  const identity = clientIdentity(req, payload);
  if (!identity.licenseKey) {
    return {
      ok: false,
      status: 401,
      message: "Licenca obrigatoria para usar Instagram.",
    };
  }
  if (!validMachineHash(identity.machineHash)) {
    return {
      ok: false,
      status: 401,
      message: "Identificacao da maquina obrigatoria.",
    };
  }

  const client = serviceClient();
  if (!client) {
    return {
      ok: false,
      status: 503,
      message: "Supabase service role nao configurado.",
    };
  }

  const signedLicenseValid = allowSignedLicenseFallback() &&
    await validateSignedActivationLicense(
      identity.licenseKey,
      identity.machineHash,
      identity.machineCode,
    );
  const { data: license, error: licenseError } = await client
    .from("bv_licenses")
    .select("key,status,expires_at,machine_hash")
    .eq("key", identity.licenseKey)
    .maybeSingle();

  if (licenseError && !signedLicenseValid) {
    return {
      ok: false,
      status: 503,
      message: `Nao foi possivel validar a licenca: ${licenseError.message}`,
    };
  }
  if (!license && !signedLicenseValid) {
    return {
      ok: false,
      status: 401,
      message: "Licenca invalida ou nao encontrada.",
    };
  }

  if (license) {
    const status = stringValue(license.status).toUpperCase();
    if (["BLOQUEADA", "CANCELADA", "EXPIRADA", "INATIVA"].includes(status)) {
      return {
        ok: false,
        status: 403,
        message: "Licenca bloqueada ou inativa.",
      };
    }
    if (!["ATIVA", "ATIVO"].includes(status) && !signedLicenseValid) {
      return {
        ok: false,
        status: 403,
        message: "Licenca ainda nao foi ativada neste computador.",
      };
    }
    const expiresAt = Date.parse(stringValue(license.expires_at));
    if (Number.isFinite(expiresAt) && expiresAt <= Date.now()) {
      return { ok: false, status: 403, message: "Licenca expirada." };
    }
    const licensedMachine = stringValue(license.machine_hash);
    if (
      licensedMachine &&
      !constantTimeEqual(licensedMachine, identity.machineHash)
    ) {
      return {
        ok: false,
        status: 403,
        message: "Licenca pertence a outro computador.",
      };
    }
    if (!licensedMachine && !signedLicenseValid) {
      return {
        ok: false,
        status: 403,
        message: "Licenca sem vinculo de computador.",
      };
    }
  }

  const rateLimit = await enforceRateLimit(
    client,
    identity.licenseKey,
    routeFromPath(url.pathname),
  );
  if (!rateLimit.ok) {
    return {
      ok: false,
      status: rateLimit.status,
      message: rateLimit.message,
    };
  }

  const { data: connectionData, error: connectionError } = await client
    .from(CONNECTIONS_TABLE)
    .select("*")
    .eq("license_key", identity.licenseKey)
    .maybeSingle();
  if (connectionError) {
    return {
      ok: false,
      status: 503,
      message: `Nao foi possivel ler a conexao: ${connectionError.message}`,
    };
  }

  const connection = connectionData ? connectionFromRow(connectionData) : null;
  if (
    connection?.machine_hash &&
    !constantTimeEqual(connection.machine_hash, identity.machineHash)
  ) {
    return {
      ok: false,
      status: 403,
      message: "Conexao Instagram pertence a outro computador.",
    };
  }

  return { ok: true, client, identity, connection };
}

async function enforceRateLimit(
  client: ServiceClient,
  licenseKey: string,
  route: string,
): Promise<
  { ok: true } | { ok: false; status: number; message: string }
> {
  const policy = routeRateLimits[route];
  if (!policy) return { ok: true };

  const now = Date.now();
  const cutoff = new Date(now - policy.windowSeconds * 1000).toISOString();
  const { count, error } = await client
    .from(RATE_LIMITS_TABLE)
    .select("id", { count: "exact", head: true })
    .eq("license_key", licenseKey)
    .eq("route", route)
    .gte("created_at", cutoff);
  if (error) {
    return {
      ok: false,
      status: 503,
      message: `Nao foi possivel validar o limite da rota: ${error.message}`,
    };
  }
  if ((count ?? 0) >= policy.max) {
    return {
      ok: false,
      status: 429,
      message:
        "Muitas solicitacoes. Aguarde alguns instantes e tente novamente.",
    };
  }

  const { error: insertError } = await client.from(RATE_LIMITS_TABLE).insert({
    license_key: licenseKey,
    route,
    created_at: new Date(now).toISOString(),
  });
  if (insertError) {
    return {
      ok: false,
      status: 503,
      message:
        `Nao foi possivel registrar o limite da rota: ${insertError.message}`,
    };
  }

  await client
    .from(RATE_LIMITS_TABLE)
    .delete()
    .lt("created_at", new Date(now - 24 * 60 * 60 * 1000).toISOString());

  return { ok: true };
}

function clientIdentity(
  req: Request,
  payload: JsonObject,
): ClientIdentity {
  return {
    licenseKey: normalizeLicense(
      payload.licenseKey || payload.LicenseKey ||
        req.headers.get("x-balcao-license"),
    ),
    machineHash: stringValue(
      payload.machineHash || payload.MachineHash ||
        req.headers.get("x-balcao-machine"),
    ),
    machineCode: stringValue(
      payload.machineCode || payload.MachineCode ||
        req.headers.get("x-balcao-machine-code"),
    ),
    appVersion: stringValue(
      payload.appVersion || payload.AppVersion ||
        req.headers.get("x-balcao-app-version"),
    ),
  };
}

async function consumeOAuthState(state: string): Promise<
  | { ok: true; state: OAuthState }
  | { ok: false; status: number; message: string }
> {
  const client = serviceClient();
  if (!client) {
    return {
      ok: false,
      status: 503,
      message: "Supabase service role nao configurado.",
    };
  }

  const now = new Date().toISOString();
  const { data, error } = await client
    .from(OAUTH_STATES_TABLE)
    .update({ used_at: now })
    .eq("state", state)
    .is("used_at", null)
    .gt("expires_at", now)
    .select("*")
    .maybeSingle();

  if (error) return { ok: false, status: 503, message: error.message };
  if (!data) {
    return {
      ok: false,
      status: 400,
      message: "State OAuth invalido, expirado ou ja utilizado.",
    };
  }
  return { ok: true, state: data as OAuthState };
}

async function persistWebhookPayload(
  client: NonNullable<ReturnType<typeof serviceClient>>,
  payload: JsonObject,
): Promise<
  { ok: true; accepted: number; duplicates: number } | {
    ok: false;
    message: string;
  }
> {
  let accepted = 0;
  let duplicates = 0;
  const entries = Array.isArray(payload.entry)
    ? payload.entry.slice(0, 100)
    : [];

  try {
    for (const rawEntry of entries) {
      const entry = asRecord(rawEntry);
      const instagramUserId = stringValue(entry.id);
      const connection = instagramUserId
        ? await readConnectionByInstagramId(client, instagramUserId)
        : null;

      const messaging = Array.isArray(entry.messaging)
        ? entry.messaging.slice(0, 200)
        : [];
      for (const rawMessaging of messaging) {
        const item = asRecord(rawMessaging);
        const message = asRecord(item.message);
        const postback = asRecord(item.postback);
        const referral = asRecord(item.referral);
        const sender = stringValue(asRecord(item.sender).id);
        const recipient = stringValue(asRecord(item.recipient).id);
        const messageId = stringValue(message.mid || postback.mid);
        const eventType = messageId || Object.keys(message).length > 0
          ? "messages"
          : Object.keys(postback).length > 0
          ? "messaging_postbacks"
          : Object.keys(referral).length > 0
          ? "messaging_referral"
          : "messaging_event";
        const eventKey = `${instagramUserId}:${eventType}:${
          messageId || await sha256Hex(JSON.stringify(item))
        }`;

        const registered = await registerWebhookEvent(client, {
          event_key: eventKey,
          license_key: connection?.license_key ?? null,
          instagram_user_id: instagramUserId,
          event_type: eventType,
          signature_valid: true,
          payload: item,
          processed_at: null,
        });
        if (!registered.ok) {
          return { ok: false, message: registered.message };
        }
        if (!registered.shouldProcess) {
          duplicates++;
          continue;
        }

        if (
          connection &&
          (messageId || Object.keys(postback).length > 0)
        ) {
          const direction = sender === instagramUserId || item.is_self === true
            ? "saida"
            : "entrada";
          const scopedUserId = direction === "entrada" ? sender : recipient;

          if (scopedUserId) {
            const textValue = firstNonEmpty(
              message.text,
              postback.title,
              postback.payload,
              referral.ref,
              attachmentSummary(message.attachments),
            ).slice(0, 10_000);
            const createdAt = timestampToIso(item.timestamp);
            const storedMessageId = messageId || eventKey;
            const { error: messageError } = await client.from(MESSAGES_TABLE)
              .upsert({
                license_key: connection.license_key,
                instagram_user_id: instagramUserId,
                instagram_scoped_user_id: scopedUserId,
                meta_message_id: storedMessageId,
                direction,
                message_type: stringValue(message.type || eventType),
                message_text: textValue,
                status: direction === "entrada" ? "recebida" : "enviada",
                payload: item,
                created_at: createdAt,
                sent_at: direction === "saida" ? createdAt : null,
              }, { onConflict: "meta_message_id", ignoreDuplicates: true });
            if (messageError) {
              return { ok: false, message: messageError.message };
            }
          }
        }

        const processed = await markWebhookEventProcessed(client, eventKey);
        if (!processed.ok) return { ok: false, message: processed.message };
        accepted++;
      }

      const changes = Array.isArray(entry.changes)
        ? entry.changes.slice(0, 200)
        : [];
      for (const rawChange of changes) {
        const change = asRecord(rawChange);
        const value = asRecord(change.value);
        const eventType = stringValue(change.field || "change");
        const nativeId = firstNonEmpty(
          value.id,
          value.comment_id,
          value.media_id,
        );
        const eventKey = `${instagramUserId}:${eventType}:${
          nativeId || await sha256Hex(JSON.stringify(change))
        }`;
        const registered = await registerWebhookEvent(client, {
          event_key: eventKey,
          license_key: connection?.license_key ?? null,
          instagram_user_id: instagramUserId,
          event_type: eventType,
          signature_valid: true,
          payload: change,
          processed_at: null,
        });
        if (!registered.ok) {
          return { ok: false, message: registered.message };
        }
        if (!registered.shouldProcess) {
          duplicates++;
          continue;
        }
        const processed = await markWebhookEventProcessed(client, eventKey);
        if (!processed.ok) return { ok: false, message: processed.message };
        accepted++;
      }
    }
  } catch (error) {
    return { ok: false, message: messageFromError(error) };
  }

  return { ok: true, accepted, duplicates };
}

async function registerWebhookEvent(
  client: NonNullable<ReturnType<typeof serviceClient>>,
  row: JsonObject,
): Promise<
  { ok: true; shouldProcess: boolean } | { ok: false; message: string }
> {
  const { error } = await client.from(WEBHOOK_EVENTS_TABLE).insert(row);
  if (!error) return { ok: true, shouldProcess: true };
  if (error.code !== "23505") return { ok: false, message: error.message };

  const existing = await client
    .from(WEBHOOK_EVENTS_TABLE)
    .select("processed_at")
    .eq("event_key", stringValue(row.event_key))
    .maybeSingle();
  if (existing.error) return { ok: false, message: existing.error.message };
  return {
    ok: true,
    shouldProcess: !stringValue(existing.data?.processed_at),
  };
}

async function markWebhookEventProcessed(
  client: ServiceClient,
  eventKey: string,
): Promise<{ ok: true } | { ok: false; message: string }> {
  const { error } = await client
    .from(WEBHOOK_EVENTS_TABLE)
    .update({ processed_at: new Date().toISOString() })
    .eq("event_key", eventKey)
    .is("processed_at", null);
  return error ? { ok: false, message: error.message } : { ok: true };
}

async function readConnectionByInstagramId(
  client: NonNullable<ReturnType<typeof serviceClient>>,
  instagramUserId: string,
): Promise<InstagramConnection | null> {
  const { data, error } = await client
    .from(CONNECTIONS_TABLE)
    .select("*")
    .eq("instagram_user_id", instagramUserId)
    .maybeSingle();
  if (error) throw new Error(error.message);
  return data ? connectionFromRow(data) : null;
}

async function ensureFreshConnection(
  client: NonNullable<ReturnType<typeof serviceClient>>,
  connection: InstagramConnection,
): Promise<
  { ok: true; connection: InstagramConnection } | {
    ok: false;
    connection: InstagramConnection;
    message: string;
  }
> {
  if (!connection.access_token) {
    return {
      ok: false,
      connection,
      message: "Token Instagram ausente. Reconecte a conta.",
    };
  }

  const expiresAt = Date.parse(stringValue(connection.token_expires_at));
  const shouldRefresh = Number.isFinite(expiresAt) &&
    expiresAt <= Date.now() + 7 * 24 * 60 * 60 * 1000;
  if (!shouldRefresh) return { ok: true, connection };

  const refreshUrl = new URL(`${graphBaseUrl()}/refresh_access_token`);
  refreshUrl.searchParams.set("grant_type", "ig_refresh_token");
  refreshUrl.searchParams.set("access_token", connection.access_token);
  const refreshed = await metaRequest(refreshUrl.toString(), { method: "GET" });

  if (!refreshed.ok || !stringValue(refreshed.data.access_token)) {
    const expired = Number.isFinite(expiresAt) && expiresAt <= Date.now();
    const status = expired ? "EXPIRADO" : connection.status;
    const message = refreshed.message ||
      "Nao foi possivel renovar o token Instagram.";
    await client.from(CONNECTIONS_TABLE).update({
      status,
      last_error: message,
      updated_at: new Date().toISOString(),
    }).eq("license_key", connection.license_key);
    return expired
      ? {
        ok: false,
        connection: { ...connection, status, last_error: message },
        message,
      }
      : { ok: true, connection: { ...connection, last_error: message } };
  }

  const accessToken = stringValue(refreshed.data.access_token);
  const seconds = positiveNumber(refreshed.data.expires_in, 60 * 24 * 60 * 60);
  const tokenExpiresAt = new Date(Date.now() + seconds * 1000).toISOString();
  const updated = {
    ...connection,
    access_token: accessToken,
    token_expires_at: tokenExpiresAt,
    status: "ATIVO",
    last_error: "",
    updated_at: new Date().toISOString(),
  };
  const { error } = await client.from(CONNECTIONS_TABLE).update({
    access_token: accessToken,
    token_expires_at: tokenExpiresAt,
    status: "ATIVO",
    last_error: "",
    updated_at: updated.updated_at,
  }).eq("license_key", connection.license_key);
  if (error) return { ok: false, connection, message: error.message };
  return { ok: true, connection: updated };
}

function exchangeAuthorizationCode(code: string, redirectUri: string) {
  const form = new FormData();
  form.set("client_id", metaAppId());
  form.set("client_secret", metaAppSecret());
  form.set("grant_type", "authorization_code");
  form.set("redirect_uri", redirectUri);
  form.set("code", code);
  return metaRequest(metaTokenUrl(), { method: "POST", body: form });
}

function exchangeLongLivedToken(accessToken: string) {
  const url = new URL(`${graphBaseUrl()}/access_token`);
  url.searchParams.set("grant_type", "ig_exchange_token");
  url.searchParams.set("client_secret", metaAppSecret());
  url.searchParams.set("access_token", accessToken);
  return metaRequest(url.toString(), { method: "GET" });
}

async function fetchInstagramProfile(accessToken: string) {
  const first = await metaRequest(
    `${graphBaseUrl()}/${graphVersion()}/me?fields=id,username,name,account_type`,
    { headers: { Authorization: `Bearer ${accessToken}` } },
  );
  if (first.ok) return first;
  return metaRequest(
    `${graphBaseUrl()}/${graphVersion()}/me?fields=user_id,username,name,account_type`,
    { headers: { Authorization: `Bearer ${accessToken}` } },
  );
}

function subscribeInstagramWebhooks(
  accessToken: string,
  instagramUserId: string,
) {
  const form = new URLSearchParams();
  form.set("subscribed_fields", "messages,messaging_postbacks,comments");
  return metaRequest(
    `${graphBaseUrl()}/${graphVersion()}/${
      encodeURIComponent(instagramUserId)
    }/subscribed_apps`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        "Content-Type": "application/x-www-form-urlencoded",
      },
      body: form,
    },
  );
}

async function revokeInstagramAccess(accessToken: string) {
  if (!accessToken) return;
  await metaRequest(`${graphBaseUrl()}/${graphVersion()}/me/permissions`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${accessToken}` },
  });
}

function createMediaContainer(
  connection: InstagramConnection,
  options: {
    mediaType: string;
    mediaUrl: string;
    caption: string;
    isVideo: boolean;
    shareToFeed: boolean;
  },
) {
  const form = new URLSearchParams();
  if (options.mediaType === "REELS") {
    form.set("media_type", "REELS");
    form.set("video_url", options.mediaUrl);
    form.set("share_to_feed", String(options.shareToFeed));
  } else if (options.mediaType === "STORIES") {
    form.set("media_type", "STORIES");
    form.set(options.isVideo ? "video_url" : "image_url", options.mediaUrl);
  } else {
    form.set("image_url", options.mediaUrl);
  }
  if (options.caption && options.mediaType !== "STORIES") {
    form.set("caption", options.caption);
  }

  return metaRequest(
    `${graphBaseUrl()}/${graphVersion()}/${
      encodeURIComponent(connection.instagram_user_id)
    }/media`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${connection.access_token}`,
        "Content-Type": "application/x-www-form-urlencoded",
      },
      body: form,
    },
  );
}

async function waitForContainer(
  connection: InstagramConnection,
  containerId: string,
): Promise<
  { ok: true; finished: boolean } | {
    ok: false;
    finished: false;
    message: string;
  }
> {
  for (let attempt = 0; attempt < 12; attempt++) {
    const result = await metaRequest(
      `${graphBaseUrl()}/${graphVersion()}/${
        encodeURIComponent(containerId)
      }?fields=status_code,status`,
      { headers: { Authorization: `Bearer ${connection.access_token}` } },
    );
    if (!result.ok) {
      return { ok: false, finished: false, message: result.message };
    }

    const status = stringValue(result.data.status_code || result.data.status)
      .toUpperCase();
    if (status === "FINISHED" || status === "PUBLISHED") {
      return { ok: true, finished: true };
    }
    if (["ERROR", "EXPIRED"].includes(status)) {
      return {
        ok: false,
        finished: false,
        message: `A Meta nao conseguiu processar a midia (${status}).`,
      };
    }
    if (attempt < 11) await delay(1000);
  }
  return { ok: true, finished: false };
}

function publishMediaContainer(
  connection: InstagramConnection,
  containerId: string,
) {
  const form = new URLSearchParams();
  form.set("creation_id", containerId);
  return metaRequest(
    `${graphBaseUrl()}/${graphVersion()}/${
      encodeURIComponent(connection.instagram_user_id)
    }/media_publish`,
    {
      method: "POST",
      headers: {
        Authorization: `Bearer ${connection.access_token}`,
        "Content-Type": "application/x-www-form-urlencoded",
      },
      body: form,
    },
  );
}

async function markPublicationError(
  client: NonNullable<ReturnType<typeof serviceClient>>,
  publicationId: string,
  message: string,
) {
  await client.from(PUBLICATIONS_TABLE).update({
    status: "ERRO",
    error_message: message.slice(0, 2000),
    updated_at: new Date().toISOString(),
  }).eq("id", publicationId);
}

async function metaRequest(
  input: string,
  init: RequestInit = {},
): Promise<MetaResult> {
  try {
    const response = await fetch(input, init);
    const body = await response.text();
    let data: JsonObject = {};
    if (body) {
      try {
        data = asRecord(JSON.parse(body));
      } catch (_error) {
        data = {};
      }
    }
    return {
      ok: response.ok,
      status: response.status,
      data,
      message: response.ok ? "" : extractMetaError(response.status, data),
    };
  } catch (error) {
    console.error(
      "Instagram Meta request failed:",
      redactSensitiveText(messageFromError(error)),
    );
    return {
      ok: false,
      status: 0,
      data: {},
      message: "Nao foi possivel comunicar com a Meta.",
    };
  }
}

async function verifyMetaSignature(rawBody: string, signatureHeader: string) {
  const secret = metaAppSecret();
  if (!secret || !signatureHeader.toLowerCase().startsWith("sha256=")) {
    return false;
  }
  const received = signatureHeader.slice("sha256=".length).toLowerCase();
  if (!/^[a-f0-9]{64}$/.test(received)) return false;
  const expected = await hmacSha256Hex(secret, rawBody);
  return constantTimeEqual(received, expected);
}

async function validateSignedActivationLicense(
  licenseKey: string,
  machineHash: string,
  machineCode: string,
) {
  const normalized = normalizeLicense(licenseKey);
  if (!normalized) return false;
  if (normalized === "BL-TESTE-2026") return allowTestLicense();

  const parts = normalized.split("-").filter(Boolean);
  if (parts.length === 4 && parts[0] === "BLV") {
    const expiresAt = parseActivationExpiration(parts[1]);
    if (!expiresAt || expiresAt.getTime() <= Date.now()) return false;
    const expected = (await activationSignature(`BLV|${parts[1]}|${parts[2]}`))
      .slice(0, 10);
    if (!constantTimeEqual(expected, parts[3].toUpperCase())) return false;

    const scope = parts[2].toUpperCase();
    if (!scope.startsWith("AGENDALIVRE")) return false;

    const normalizedMachineCode = stringValue(machineCode).toUpperCase();
    if (!/^[A-F0-9]{8}$/.test(normalizedMachineCode)) return false;
    if (!constantTimeEqual(scope, `AGENDALIVRE${normalizedMachineCode}`)) {
      return false;
    }
    if (
      !constantTimeEqual(
        stringValue(machineHash).slice(0, 8).toUpperCase(),
        normalizedMachineCode,
      )
    ) {
      return false;
    }
    return true;
  }
  return false;
}

function parseActivationExpiration(value: string) {
  if (!/^\d{8}(\d{4})?$/.test(value)) return null;
  const year = Number(value.slice(0, 4));
  const month = Number(value.slice(4, 6));
  const day = Number(value.slice(6, 8));
  if (!year || month < 1 || month > 12 || day < 1 || day > 31) return null;
  if (value.length === 8) {
    return new Date(Date.UTC(year, month - 1, day, 26, 59, 59, 999));
  }
  const hour = Number(value.slice(8, 10));
  const minute = Number(value.slice(10, 12));
  if (hour > 23 || minute > 59) return null;
  return new Date(Date.UTC(year, month - 1, day, hour + 3, minute, 0, 0));
}

async function activationSignature(message: string) {
  const secret = stringValue(Deno.env.get("BVPDV_LICENSE_HMAC_SECRET")) ||
    "BalcaoLivrePDV-local-license-v1";
  return (await hmacSha256Hex(secret, message)).toUpperCase();
}

async function hmacSha256Hex(secret: string, message: string) {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(secret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const signature = await crypto.subtle.sign(
    "HMAC",
    key,
    new TextEncoder().encode(message),
  );
  return bytesToHex(new Uint8Array(signature));
}

async function sha256Hex(message: string) {
  const digest = await crypto.subtle.digest(
    "SHA-256",
    new TextEncoder().encode(message),
  );
  return bytesToHex(new Uint8Array(digest));
}

function bytesToHex(bytes: Uint8Array) {
  return Array.from(bytes).map((value) => value.toString(16).padStart(2, "0"))
    .join("");
}

function constantTimeEqual(left: string, right: string) {
  const a = String(left ?? "");
  const b = String(right ?? "");
  let difference = a.length ^ b.length;
  const length = Math.max(a.length, b.length);
  for (let index = 0; index < length; index++) {
    difference |= (a.charCodeAt(index) || 0) ^ (b.charCodeAt(index) || 0);
  }
  return difference === 0;
}

function serviceClient(): ServiceClient | null {
  if (cachedServiceClient !== undefined) return cachedServiceClient;
  const url = stringValue(Deno.env.get("SUPABASE_URL"));
  const key = serviceRoleKey();
  if (!url || !key) {
    cachedServiceClient = null;
    return null;
  }
  cachedServiceClient = createClient<Database>(url, key, {
    auth: { persistSession: false, autoRefreshToken: false },
  });
  return cachedServiceClient;
}

function serviceRoleKey() {
  const legacy = stringValue(Deno.env.get("SUPABASE_SERVICE_ROLE_KEY"));
  if (legacy) return legacy;
  const secretKeys = stringValue(Deno.env.get("SUPABASE_SECRET_KEYS"));
  if (!secretKeys) return "";
  try {
    const parsed = asRecord(JSON.parse(secretKeys));
    return stringValue(parsed.default || Object.values(parsed)[0]);
  } catch (_error) {
    return "";
  }
}

function metaAppId() {
  return firstNonEmpty(
    Deno.env.get("META_INSTAGRAM_APP_ID"),
    Deno.env.get("META_WHATSAPP_APP_ID"),
    Deno.env.get("WHATSAPP_META_APP_ID"),
  );
}

function metaAppSecret() {
  return firstNonEmpty(
    Deno.env.get("META_INSTAGRAM_APP_SECRET"),
    Deno.env.get("META_WHATSAPP_APP_SECRET"),
    Deno.env.get("WHATSAPP_META_APP_SECRET"),
  );
}

function metaVerifyToken() {
  return stringValue(Deno.env.get("META_INSTAGRAM_VERIFY_TOKEN"));
}

function metaRedirectUri(requestUrl: URL) {
  const configured = stringValue(Deno.env.get("META_INSTAGRAM_REDIRECT_URI"));
  if (configured) return configured;
  const next = new URL(requestUrl.toString());
  const marker = "/functions/v1/instagram";
  const index = next.pathname.indexOf(marker);
  next.protocol = "https:";
  next.pathname = index >= 0
    ? `${next.pathname.slice(0, index + marker.length)}/oauth/callback`
    : `${marker}/oauth/callback`;
  next.search = "";
  next.hash = "";
  return next.toString();
}

function graphVersion() {
  return (firstNonEmpty(
    Deno.env.get("META_INSTAGRAM_GRAPH_VERSION"),
    Deno.env.get("META_GRAPH_VERSION"),
  ) || DEFAULT_GRAPH_VERSION)
    .replace(/^\/+|\/+$/g, "");
}

function graphBaseUrl() {
  return (stringValue(Deno.env.get("META_INSTAGRAM_GRAPH_BASE_URL")) ||
    "https://graph.instagram.com")
    .replace(/\/+$/g, "");
}

function metaAuthorizeUrl() {
  return stringValue(Deno.env.get("META_INSTAGRAM_AUTHORIZE_URL")) ||
    "https://www.instagram.com/oauth/authorize";
}

function metaTokenUrl() {
  return stringValue(Deno.env.get("META_INSTAGRAM_TOKEN_URL")) ||
    "https://api.instagram.com/oauth/access_token";
}

function oauthScopes() {
  const configured = stringValue(Deno.env.get("META_INSTAGRAM_OAUTH_SCOPES"));
  const values = configured ? configured.split(/[\s,]+/) : DEFAULT_OAUTH_SCOPES;
  return [...new Set(values.map((value) => value.trim()).filter(Boolean))];
}

function allowTestLicense() {
  return ["1", "true", "yes", "sim"].includes(
    stringValue(Deno.env.get("META_INSTAGRAM_ALLOW_TEST_LICENSE"))
      .toLowerCase(),
  );
}

function allowSignedLicenseFallback() {
  return ["1", "true", "yes", "sim"].includes(
    stringValue(Deno.env.get("META_INSTAGRAM_ALLOW_SIGNED_LICENSES"))
      .toLowerCase(),
  );
}

function buildInstagramAuthorizationUrl(
  options: { state: string; redirectUri: string; scopes: string[] },
) {
  const url = new URL(metaAuthorizeUrl());
  url.searchParams.set("client_id", metaAppId());
  url.searchParams.set("redirect_uri", options.redirectUri);
  url.searchParams.set("response_type", "code");
  url.searchParams.set("scope", options.scopes.join(","));
  url.searchParams.set("state", options.state);
  url.searchParams.set("enable_fb_login", "0");
  url.searchParams.set("force_authentication", "1");
  return url.toString();
}

function safeConnection(connection: InstagramConnection) {
  const expiresAt = Date.parse(stringValue(connection.token_expires_at));
  const expired = Number.isFinite(expiresAt) && expiresAt <= Date.now();
  return {
    status: expired ? "EXPIRADO" : connection.status,
    username: connection.username,
    displayName: connection.display_name,
    instagramUserId: connection.instagram_user_id,
    accountType: connection.account_type,
    scopes: connection.scopes ?? [],
    tokenExpiresAt: connection.token_expires_at,
    webhookSubscribedAt: connection.webhook_subscribed_at,
    connectedAt: connection.connected_at,
    updatedAt: connection.updated_at,
    lastError: connection.last_error,
  };
}

function connectionFromRow(row: JsonObject): InstagramConnection {
  return {
    license_key: normalizeLicense(row.license_key),
    machine_hash: stringValue(row.machine_hash),
    instagram_user_id: stringValue(row.instagram_user_id),
    username: stringValue(row.username),
    display_name: stringValue(row.display_name),
    account_type: stringValue(row.account_type),
    access_token: stringValue(row.access_token),
    token_type: stringValue(row.token_type),
    token_expires_at: stringValue(row.token_expires_at) || null,
    scopes: Array.isArray(row.scopes)
      ? row.scopes.map(stringValue).filter(Boolean)
      : [],
    status: stringValue(row.status),
    webhook_subscribed_at: stringValue(row.webhook_subscribed_at) || null,
    last_error: stringValue(row.last_error),
    connected_at: stringValue(row.connected_at),
    updated_at: stringValue(row.updated_at),
    meta_payload: asRecord(row.meta_payload),
  };
}

function withoutSensitiveFields(data: JsonObject) {
  return stripSensitiveFields(data) as JsonObject;
}

function stripSensitiveFields(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(stripSensitiveFields);
  if (!value || typeof value !== "object") return value;

  const output: JsonObject = {};
  for (const [key, child] of Object.entries(value as JsonObject)) {
    const normalizedKey = key.toLowerCase().replace(/[^a-z0-9]/g, "");
    if (
      ["accesstoken", "token", "clientsecret", "appsecret"].includes(
        normalizedKey,
      )
    ) continue;
    output[key] = stripSensitiveFields(child);
  }
  return output;
}

function extractMetaError(status: number, data: JsonObject) {
  const error = asRecord(data.error);
  return redactSensitiveText(firstNonEmpty(
    error.error_user_msg,
    error.message,
    data.message,
    `Meta retornou HTTP ${status}.`,
  ))
    .slice(0, 1000);
}

function validatePublicMediaUrl(
  value: unknown,
): { ok: true; url: string } | { ok: false; message: string } {
  const raw = stringValue(value);
  try {
    const url = new URL(raw);
    if (url.protocol !== "https:") throw new Error("protocol");
    if (
      !url.hostname ||
      ["localhost", "127.0.0.1", "::1"].includes(url.hostname.toLowerCase())
    ) {
      throw new Error("host");
    }
    return { ok: true, url: url.toString() };
  } catch (_error) {
    return {
      ok: false,
      message: "A midia precisa ter uma URL HTTPS publica acessivel pela Meta.",
    };
  }
}

function normalizeMediaType(value: unknown) {
  const normalized = (stringValue(value) || "IMAGE").toUpperCase();
  return ["IMAGE", "REELS", "STORIES"].includes(normalized) ? normalized : "";
}

function looksLikeVideoUrl(value: string) {
  try {
    return /\.(mp4|mov|m4v|webm)$/i.test(new URL(value).pathname);
  } catch (_error) {
    return false;
  }
}

function attachmentSummary(value: unknown) {
  if (!Array.isArray(value)) return "";
  const types = value.map((item) => stringValue(asRecord(item).type)).filter(
    Boolean,
  );
  return types.length ? `[Anexo: ${types.join(", ")}]` : "";
}

function timestampToIso(value: unknown) {
  const numeric = Number(value);
  if (Number.isFinite(numeric) && numeric > 0) {
    const milliseconds = numeric < 10_000_000_000 ? numeric * 1000 : numeric;
    const parsed = new Date(milliseconds);
    if (!Number.isNaN(parsed.getTime())) return parsed.toISOString();
  }
  const textValue = stringValue(value);
  const parsed = Date.parse(textValue);
  return Number.isFinite(parsed)
    ? new Date(parsed).toISOString()
    : new Date().toISOString();
}

function validIsoDate(value: unknown) {
  const parsed = Date.parse(stringValue(value));
  return Number.isFinite(parsed) ? new Date(parsed).toISOString() : "";
}

function validMachineHash(value: string) {
  return value.length >= 8 && value.length <= 256 &&
    /^[A-Za-z0-9._:-]+$/.test(value);
}

function normalizeLicense(value: unknown) {
  return stringValue(value).toUpperCase().replace(/\s+/g, "").slice(0, 256);
}

function routeFromPath(pathname: string) {
  const marker = "/instagram";
  const index = pathname.indexOf(marker);
  if (index < 0) return "/";
  const route = pathname.slice(index + marker.length) || "/";
  return route.endsWith("/") && route.length > 1 ? route.slice(0, -1) : route;
}

function randomToken(size: number) {
  const bytes = new Uint8Array(size);
  crypto.getRandomValues(bytes);
  return btoa(String.fromCharCode(...bytes))
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/g, "");
}

function positiveNumber(value: unknown, fallback: number) {
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

function firstNonEmpty(...values: unknown[]) {
  for (const value of values) {
    const textValue = stringValue(value);
    if (textValue) return textValue;
  }
  return "";
}

function stringValue(value: unknown) {
  return String(value ?? "").trim();
}

function asRecord(value: unknown): JsonObject {
  return value && typeof value === "object" && !Array.isArray(value)
    ? value as JsonObject
    : {};
}

async function readJson(req: Request): Promise<JsonObject> {
  const rawBody = await readBodyText(
    req,
    MAX_CLIENT_BODY_BYTES,
    "Corpo da solicitacao excedeu o tamanho permitido.",
  );
  if (!rawBody.trim()) return {};
  try {
    return asRecord(JSON.parse(rawBody));
  } catch (_error) {
    throw new HttpError(400, "JSON da solicitacao invalido.");
  }
}

async function readBodyText(
  req: Request,
  maxBytes: number,
  limitMessage: string,
) {
  const contentLength = Number(req.headers.get("content-length") ?? 0);
  if (Number.isFinite(contentLength) && contentLength > maxBytes) {
    throw new HttpError(413, limitMessage);
  }
  if (!req.body) return "";

  const reader = req.body.getReader();
  const chunks: Uint8Array[] = [];
  let totalBytes = 0;
  try {
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
      totalBytes += value.byteLength;
      if (totalBytes > maxBytes) {
        await reader.cancel();
        throw new HttpError(413, limitMessage);
      }
      chunks.push(value);
    }
  } finally {
    reader.releaseLock();
  }

  const body = new Uint8Array(totalBytes);
  let offset = 0;
  for (const chunk of chunks) {
    body.set(chunk, offset);
    offset += chunk.byteLength;
  }
  return new TextDecoder().decode(body);
}

function messageFromError(error: unknown) {
  const message = error instanceof Error
    ? error.message
    : String(error ?? "Erro inesperado.");
  return redactSensitiveText(message);
}

function redactSensitiveText(value: unknown) {
  return stringValue(value)
    .replace(
      /([?&](?:access_token|client_secret|app_secret|token)=)[^&\s]+/gi,
      "$1[REDACTED]",
    )
    .replace(/(bearer\s+)[a-z0-9._~+\/=:-]+/gi, "$1[REDACTED]")
    .slice(0, 2000);
}

function delay(milliseconds: number) {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}

function json(data: unknown, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: {
      ...corsHeaders,
      "Content-Type": "application/json; charset=utf-8",
    },
  });
}

function text(body: string, status = 200) {
  return new Response(body, {
    status,
    headers: { ...corsHeaders, "Content-Type": "text/plain; charset=utf-8" },
  });
}

function html(body: string, status = 200) {
  return new Response(body, {
    status,
    headers: {
      ...corsHeaders,
      "Content-Type": "text/html; charset=utf-8",
      "Content-Security-Policy":
        "default-src 'none'; style-src 'unsafe-inline'; base-uri 'none'; frame-ancestors 'none'",
      "X-Content-Type-Options": "nosniff",
      "Referrer-Policy": "no-referrer",
    },
  });
}

function oauthPage(ok: boolean, title: string, message: string) {
  const color = ok ? "#166534" : "#991b1b";
  const background = ok ? "#ecfdf3" : "#fff1f2";
  return `<!doctype html>
<html lang="pt-BR">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>${escapeHtml(title)}</title>
</head>
<body style="margin:0;min-height:100vh;display:grid;place-items:center;background:#f7f4f1;font-family:Segoe UI,Arial,sans-serif;color:#1c1b1a;padding:20px">
  <main style="width:min(560px,100%);box-sizing:border-box;background:white;border:1px solid #e8e3de;border-radius:18px;padding:30px;box-shadow:0 18px 50px rgba(28,27,26,.10)">
    <span style="display:inline-flex;padding:7px 11px;border-radius:999px;background:${background};color:${color};font-weight:800">${
    ok ? "Conectado" : "Atencao"
  }</span>
    <h1 style="margin:16px 0 10px;font-size:28px;color:${color}">${
    escapeHtml(title)
  }</h1>
    <p style="font-size:17px;line-height:1.55;color:#5f5954">${
    escapeHtml(message)
  }</p>
    <p style="margin-top:22px;color:#716b66">Pode fechar esta janela e voltar ao Agenda Livre.</p>
  </main>
</body>
</html>`;
}

function escapeHtml(value: unknown) {
  return stringValue(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}
