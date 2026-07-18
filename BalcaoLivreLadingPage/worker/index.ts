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
]);

const APP_ASSET_PREFIX = "/agenda-livre/app";

function bookingSlugForHost(hostname: string, rootDomain: string) {
  const normalizedHost = hostname.toLowerCase().replace(/\.$/, "");
  const normalizedRoot = rootDomain.toLowerCase().replace(/^https?:\/\//, "").replace(/\/$/, "");
  if (!normalizedHost.endsWith(`.${normalizedRoot}`)) return "";
  const prefix = normalizedHost.slice(0, -(normalizedRoot.length + 1));
  if (!/^[a-z0-9](?:[a-z0-9-]{1,46}[a-z0-9])?$/.test(prefix)) return "";
  return RESERVED_SUBDOMAINS.has(prefix) ? "" : prefix;
}

function rewritePlatformRequest(request: Request, env: Env) {
  const url = new URL(request.url);
  const rootDomain = (
    env.AGENDA_BOOKING_ROOT_DOMAIN ||
    env.NEXT_PUBLIC_BOOKING_ROOT_DOMAIN ||
    "minhaagendalivre.com.br"
  )
    .toLowerCase()
    .replace(/^https?:\/\//, "")
    .replace(/\/$/, "");
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
        ? `${APP_ASSET_PREFIX}/index.html`
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

  const slug = bookingSlugForHost(hostname, rootDomain);
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

    return handler.fetch(rewritePlatformRequest(request, env), env, ctx);
  },
};

export default worker;
