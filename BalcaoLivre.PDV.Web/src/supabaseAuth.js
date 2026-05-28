const configEndpoint = "/api/supabase-config";
const sessionKey = "blpdv_supabase_session";

async function loadRemoteConfig() {
  try {
    const response = await fetch(configEndpoint, { cache: "no-store" });
    if (!response.ok) return {};
    return await response.json();
  } catch {
    return {};
  }
}

function readWindowConfig() {
  return {
    url: window.BALCAO_SUPABASE_URL || "",
    anonKey: window.BALCAO_SUPABASE_ANON_KEY || ""
  };
}

function loadStoredSession() {
  try {
    const session = JSON.parse(localStorage.getItem(sessionKey) || "null");
    if (!session?.accessToken || !session?.expiresAt) return null;
    if (new Date(session.expiresAt).getTime() <= Date.now() + 120000) return null;
    return session;
  } catch {
    return null;
  }
}

function saveSession(session) {
  localStorage.setItem(sessionKey, JSON.stringify(session));
}

export async function createSupabaseAuth(settings) {
  const remote = await loadRemoteConfig();
  const fromWindow = readWindowConfig();
  const enabled = settings.supabaseAuthEnabled !== false;
  const url = (settings.supabaseUrl || remote.url || fromWindow.url || "").trim().replace(/\/$/, "");
  const anonKey = (settings.supabaseAnonKey || remote.anonKey || fromWindow.anonKey || "").trim();
  const storedSession = loadStoredSession();

  return {
    enabled,
    configured: Boolean(enabled && url && anonKey),
    url,
    anonKey,
    session: storedSession,
    user: storedSession?.user || null
  };
}

export async function signInSupabase(auth, email, password) {
  if (!auth?.configured) {
    return { ok: false, message: "Supabase nao configurado." };
  }

  try {
    const response = await fetch(`${auth.url}/auth/v1/token?grant_type=password`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        apikey: auth.anonKey,
        Authorization: `Bearer ${auth.anonKey}`
      },
      body: JSON.stringify({ email, password })
    });
    const data = await response.json().catch(() => ({}));
    if (!response.ok) {
      return {
        ok: false,
        message: data.error_description || data.msg || data.message || data.error || "Login Supabase recusado."
      };
    }

    const expiresIn = Number(data.expires_in || 3600);
    const session = {
      accessToken: data.access_token || "",
      refreshToken: data.refresh_token || "",
      expiresAt: new Date(Date.now() + Math.max(60, expiresIn - 30) * 1000).toISOString(),
      user: {
        id: data.user?.id || "",
        email: data.user?.email || email,
        appMetadata: data.user?.app_metadata || {},
        userMetadata: data.user?.user_metadata || {}
      }
    };
    saveSession(session);
    auth.session = session;
    auth.user = session.user;
    return { ok: true, session, user: session.user };
  } catch (error) {
    return { ok: false, message: `Falha ao conectar no Supabase: ${error.message}` };
  }
}

export async function signOutSupabase(auth) {
  localStorage.removeItem(sessionKey);
  if (auth) {
    auth.session = null;
    auth.user = null;
  }
}
