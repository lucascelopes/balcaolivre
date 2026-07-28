/** Cloudflare Worker entry point for the Agenda Livre public booking site. */
import {
  DEFAULT_DEVICE_SIZES,
  DEFAULT_IMAGE_SIZES,
  handleImageOptimization,
} from "vinext/server/image-optimization";
import handler from "vinext/server/app-router-entry";

interface Env {
  ASSETS: Fetcher;
  DB: D1Database;
  OPENROUTER_API_KEY?: string;
  ADMIN_AI_BRIDGE_SECRET?: string;
  AGENDA_BOOKING_ROOT_DOMAIN?: string;
  NEXT_PUBLIC_BOOKING_ROOT_DOMAIN?: string;
  IMAGES: {
    input(stream: ReadableStream): {
      transform(options: Record<string, unknown>): {
        output(options: {
          format: string;
          quality: number;
        }): Promise<{ response(): Response }>;
      };
    };
  };
}

async function handleAdminAiBridge(request: Request, env: Env) {
  if (request.method !== "POST") {
    return new Response("Method not allowed", { status: 405 });
  }
  const suppliedSecret = request.headers.get("x-admin-ai-bridge-secret") || "";
  if (
    !env.ADMIN_AI_BRIDGE_SECRET ||
    suppliedSecret !== env.ADMIN_AI_BRIDGE_SECRET
  ) {
    return new Response("Unauthorized", { status: 401 });
  }
  if (!env.OPENROUTER_API_KEY) {
    return new Response("OpenRouter not configured", { status: 503 });
  }

  let payload: Record<string, unknown>;
  try {
    payload = await request.json() as Record<string, unknown>;
  } catch {
    return new Response("Invalid JSON", { status: 400 });
  }

  const messages = Array.isArray(payload.messages)
    ? payload.messages.slice(0, 4)
    : [];
  if (!messages.length) {
    return new Response("Messages are required", { status: 400 });
  }

  const response = await fetch("https://openrouter.ai/api/v1/chat/completions", {
    method: "POST",
    headers: {
      authorization: `Bearer ${env.OPENROUTER_API_KEY}`,
      "content-type": "application/json",
      "x-title": "Balcao Livre Admin",
    },
    body: JSON.stringify({
      model: "openrouter/free",
      temperature: 0.2,
      max_tokens: 650,
      messages,
    }),
  });

  return new Response(response.body, {
    status: response.status,
    headers: {
      "content-type": response.headers.get("content-type") || "application/json",
      "cache-control": "no-store",
    },
  });
}

interface ExecutionContext {
  waitUntil(promise: Promise<unknown>): void;
  passThroughOnException(): void;
}

const RESERVED_SUBDOMAINS = new Set([
  "www",
  "app",
  "admin",
  "pdv",
  "cardapio",
  "api",
  "customers",
]);

const APP_ASSET_PREFIX = "/agenda-livre/app";

function isStaticAssetPath(pathname: string) {
  if (pathname === "/_vinext/image" || pathname.startsWith("/api/")) {
    return false;
  }

  return (
    pathname.startsWith("/assets/") ||
    pathname.startsWith("/.vite/") ||
    pathname.startsWith("/_next/") ||
    pathname.includes(".")
  );
}

function bookingRootDomain(env: Env) {
  return (
    env.AGENDA_BOOKING_ROOT_DOMAIN ||
    env.NEXT_PUBLIC_BOOKING_ROOT_DOMAIN ||
    "minhaagendalivre.com.br"
  )
    .toLowerCase()
    .replace(/^https?:\/\//, "")
    .replace(/\/$/, "");
}

function canonicalAppRedirect(request: Request, env: Env) {
  const url = new URL(request.url);
  const rootDomain = bookingRootDomain(env);
  const hostname = (request.headers.get("host") || url.hostname)
    .split(":")[0]
    .toLowerCase()
    .replace(/\.$/, "");

  if (hostname !== rootDomain && hostname !== `www.${rootDomain}`) return null;
  if (
    url.pathname !== APP_ASSET_PREFIX &&
    !url.pathname.startsWith(`${APP_ASSET_PREFIX}/`)
  ) {
    return null;
  }

  const appPath = url.pathname.slice(APP_ASSET_PREFIX.length);
  url.protocol = "https:";
  url.hostname = `app.${rootDomain}`;
  url.port = "";
  url.pathname = appPath || "/";
  return Response.redirect(url.toString(), 308);
}

function appAssetRequest(request: Request, env: Env) {
  const url = new URL(request.url);
  const hostname = (request.headers.get("host") || url.hostname)
    .split(":")[0]
    .toLowerCase()
    .replace(/\.$/, "");

  if (hostname !== `app.${bookingRootDomain(env)}`) return null;
  if (url.pathname.startsWith("/api/")) return null;

  if (!url.pathname.startsWith(`${APP_ASSET_PREFIX}/`)) {
    url.pathname =
      url.pathname === "/"
        ? `${APP_ASSET_PREFIX}/`
        : `${APP_ASSET_PREFIX}${url.pathname}`;
  }

  return new Request(url, request);
}

function bookingSlugForHost(hostname: string, rootDomain: string) {
  const normalizedHost = hostname.toLowerCase().replace(/\.$/, "");
  const normalizedRoot = rootDomain.toLowerCase().replace(/^https?:\/\//, "").replace(/\/$/, "");
  if (!normalizedHost.endsWith(`.${normalizedRoot}`)) return "";
  const prefix = normalizedHost.slice(0, -(normalizedRoot.length + 1));
  if (!/^[a-z0-9](?:[a-z0-9-]{1,46}[a-z0-9])?$/.test(prefix)) return "";
  return RESERVED_SUBDOMAINS.has(prefix) ? "" : prefix;
}

async function customDomainSlug(hostname: string, env: Env) {
  const row = await env.DB
    .prepare(
      `SELECT stores.slug AS slug
       FROM agenda_store_domains domains
       JOIN agenda_stores stores ON stores.id = domains.store_id
       WHERE domains.hostname = ?1 AND domains.status = 'active'
       LIMIT 1`,
    )
    .bind(hostname)
    .first<{ slug: string }>();
  return row?.slug || "";
}

async function rewritePlatformRequest(request: Request, env: Env) {
  const url = new URL(request.url);
  const rootDomain = bookingRootDomain(env);
  const forwardedHost = (request.headers.get("host") || "")
    .split(":")[0]
    .toLowerCase()
    .replace(/\.$/, "");
  const hostname = forwardedHost || url.hostname.toLowerCase();

  if (hostname === rootDomain || hostname === `www.${rootDomain}`) {
    if (url.pathname === "/") {
      url.pathname = "/agenda-livre";
      return new Request(url, request);
    }
    return request;
  }

  if (hostname === `app.${rootDomain}`) {
    if (
      url.pathname.startsWith("/api/") ||
      url.pathname.startsWith(`${APP_ASSET_PREFIX}/`)
    ) {
      return request;
    }

    url.pathname =
      url.pathname === "/"
        ? `${APP_ASSET_PREFIX}/`
        : `${APP_ASSET_PREFIX}${url.pathname}`;
    return new Request(url, request);
  }

  if (
    url.pathname.startsWith("/_next/") ||
    url.pathname.startsWith("/_vinext/") ||
    url.pathname.startsWith("/api/") ||
    url.pathname === "/favicon.ico" ||
    url.pathname.includes(".")
  ) {
    return request;
  }

  const slug = bookingSlugForHost(hostname, rootDomain) || await customDomainSlug(hostname, env);
  if (!slug) return request;

  if (url.pathname === `/agendar/${slug}` || url.pathname.startsWith(`/agendar/${slug}/`)) {
    return request;
  }

  url.pathname = `/agendar/${slug}${url.pathname === "/" ? "" : url.pathname}`;
  return new Request(url, request);
}

const worker = {
  async fetch(request: Request, env: Env, ctx: ExecutionContext): Promise<Response> {
    const url = new URL(request.url);
    if (url.pathname === "/api/internal/admin/openrouter") {
      return handleAdminAiBridge(request, env);
    }
    const appRedirect = canonicalAppRedirect(request, env);
    if (appRedirect) return appRedirect;
    const assetRequest = appAssetRequest(request, env);
    if (assetRequest) {
      return env.ASSETS.fetch(assetRequest);
    }

    if (url.pathname === "/_vinext/image") {
      const allowedWidths = [...DEFAULT_DEVICE_SIZES, ...DEFAULT_IMAGE_SIZES];
      return handleImageOptimization(
        request,
        {
          fetchAsset: (path) =>
            env.ASSETS.fetch(new Request(new URL(path, request.url))),
          transformImage: async (body, { width, format, quality }) => {
            const result = await env.IMAGES.input(body)
              .transform(width > 0 ? { width } : {})
              .output({ format, quality });
            return result.response();
          },
        },
        allowedWidths,
      );
    }

    if (isStaticAssetPath(url.pathname)) {
      return env.ASSETS.fetch(request);
    }

    return handler.fetch(await rewritePlatformRequest(request, env), env, ctx);
  },
};

export default worker;
