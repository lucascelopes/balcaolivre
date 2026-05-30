import { cp, mkdir, rm, writeFile, copyFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const outputDir = process.argv[2]
  ? path.resolve(root, process.argv[2])
  : path.join(root, "dist", "cloudflare-site");
const publicApexDomain = process.env.BALCAO_PUBLIC_APEX_DOMAIN || "balcaolivrepdv.com.br";
const adminApiOrigin = process.env.BALCAO_ADMIN_API_ORIGIN || "https://balcaolivrepdv.onrender.com";
const publicMenuSupabaseUrl = process.env.BALCAO_SUPABASE_URL || "https://hzvplpotsdzxygkxrgyi.supabase.co";
const publicMenuPublishableKey =
  process.env.BALCAO_SUPABASE_PUBLISHABLE_KEY ||
  "sb_publishable_qNl5_EGAeuhN6PqTzRIeyQ_YQV2MdV6";

const fromRoot = (...parts) => path.join(root, ...parts);
const toOutput = (...parts) => path.join(outputDir, ...parts);

async function copyDirectory(source, target) {
  await cp(source, target, { recursive: true, force: true });
}

async function copyLanding() {
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "preview.html"),
    toOutput("index.html")
  );

  await mkdir(toOutput("termos"), { recursive: true });
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "termos.html"),
    toOutput("termos", "index.html")
  );

  await mkdir(toOutput("como-usar"), { recursive: true });
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "como-usar.html"),
    toOutput("como-usar", "index.html")
  );

  await mkdir(toOutput("app"), { recursive: true });
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "app", "globals.css"),
    toOutput("app", "globals.css")
  );

  await copyDirectory(
    fromRoot("BalcaoLivreLadingPage", "public"),
    toOutput("public")
  );
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "public", "balcao-livre-icon.png"),
    toOutput("balcao-livre-icon.png")
  );
  await copyFile(
    fromRoot("BalcaoLivreLadingPage", "public", "balcao-livre-logo.png"),
    toOutput("balcao-livre-logo.png")
  );
}

async function copyPdv() {
  await mkdir(toOutput("pdv"), { recursive: true });
  await Promise.all([
    copyFile(fromRoot("BalcaoLivre.PDV.Web", "index.html"), toOutput("pdv", "index.html")),
    copyFile(fromRoot("BalcaoLivre.PDV.Web", "styles.css"), toOutput("pdv", "styles.css")),
    copyFile(fromRoot("BalcaoLivre.PDV.Web", "sw.js"), toOutput("pdv", "sw.js")),
    copyFile(
      fromRoot("BalcaoLivre.PDV.Web", "manifest.webmanifest"),
      toOutput("pdv", "manifest.webmanifest")
    ),
    copyDirectory(fromRoot("BalcaoLivre.PDV.Web", "src"), toOutput("pdv", "src")),
    copyDirectory(fromRoot("BalcaoLivre.PDV.Web", "assets"), toOutput("pdv", "assets"))
  ]);
}

async function copyAdmin() {
  await copyDirectory(fromRoot("BalcaoLivre.Admin", "wwwroot"), toOutput("admin"));
}

async function copyCardapio() {
  await copyDirectory(fromRoot("BalcaoLivre.Cardapio.Web"), toOutput("cardapio"));
  const config = {
    supabaseUrl: publicMenuSupabaseUrl,
    publishableKey: publicMenuPublishableKey,
    licenseFunctionUrl: `${publicMenuSupabaseUrl.replace(/\/$/, "")}/functions/v1/license`,
    apexDomain: publicApexDomain
  };

  await writeFile(
    toOutput("cardapio", "config.js"),
    `window.BALCAO_CARDAPIO_CONFIG = ${JSON.stringify(config, null, 2)};\n`,
    "utf8"
  );
}

await rm(outputDir, { recursive: true, force: true });
await mkdir(outputDir, { recursive: true });

await copyLanding();
await copyPdv();
await copyAdmin();
await copyCardapio();
await writeFile(toOutput("_worker.js"), cloudflareWorker(publicApexDomain, adminApiOrigin), "utf8");
await writeFile(toOutput("_redirects"), "", "utf8");

console.log(`Cloudflare Pages site generated at ${path.relative(root, outputDir)}`);

function cloudflareWorker(apexDomain, apiOrigin) {
  return `const APEX_DOMAIN = ${JSON.stringify(apexDomain)};
const ADMIN_API_ORIGIN = ${JSON.stringify(apiOrigin)};

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    const host = url.hostname.toLowerCase();

    if (host === "www." + APEX_DOMAIN) {
      url.hostname = APEX_DOMAIN;
      return Response.redirect(url.toString(), 301);
    }

    if (isHost(host, "admin")) {
      if (url.pathname === "/admin-api" || url.pathname.startsWith("/admin-api/")) {
        return proxyAdminApi(request, url);
      }
      return servePrefixedAsset(request, env, "/admin", "/admin/index.html");
    }

    if (isHost(host, "pdv")) {
      return servePrefixedAsset(request, env, "/pdv", "/pdv/index.html");
    }

    if (isHost(host, "cardapio")) {
      return servePrefixedAsset(request, env, "/cardapio", "/cardapio/index.html");
    }

    if (url.pathname === "/admin" || url.pathname.startsWith("/admin/")) {
      return servePathAsset(request, env, "/admin", "/admin/index.html");
    }

    if (url.pathname === "/pdv" || url.pathname.startsWith("/pdv/")) {
      return servePathAsset(request, env, "/pdv", "/pdv/index.html");
    }

    if (url.pathname === "/cardapio" || url.pathname.startsWith("/cardapio/")) {
      return servePathAsset(request, env, "/cardapio", "/cardapio/index.html");
    }

    if (url.pathname === "/admin-api" || url.pathname.startsWith("/admin-api/")) {
      return proxyAdminApi(request, url);
    }

    return env.ASSETS.fetch(request);
  },
};

function isHost(host, subdomain) {
  return host === subdomain + "." + APEX_DOMAIN || host.startsWith(subdomain + ".");
}

async function servePrefixedAsset(request, env, prefix, fallbackPath) {
  const originalUrl = new URL(request.url);
  const assetUrl = new URL(request.url);
  const path = originalUrl.pathname === "/" ? "/" : originalUrl.pathname;
  assetUrl.pathname = joinPath(prefix, path);

  let response = await env.ASSETS.fetch(new Request(assetUrl.toString(), request));
  if (response.status !== 404) {
    return response;
  }

  if (acceptsHtml(request)) {
    const fallbackUrl = new URL(request.url);
    fallbackUrl.pathname = fallbackPath;
    response = await env.ASSETS.fetch(new Request(fallbackUrl.toString(), request));
  }

  return response;
}

async function servePathAsset(request, env, prefix, fallbackPath) {
  const url = new URL(request.url);
  if (url.pathname === prefix || url.pathname === fallbackPath) {
    return serveFallbackAsset(request, env, fallbackPath);
  }

  let response = await env.ASSETS.fetch(request);
  if (response.status !== 404) {
    return response;
  }

  if (acceptsHtml(request)) {
    response = await serveFallbackAsset(request, env, fallbackPath);
  }

  return response;
}

async function serveFallbackAsset(request, env, fallbackPath) {
  const fallbackUrl = new URL(request.url);
  fallbackUrl.pathname = fallbackPath;
  return env.ASSETS.fetch(new Request(fallbackUrl.toString(), request));
}

function joinPath(prefix, path) {
  if (path === "/" || path === "") return prefix + "/index.html";
  const normalized = path.startsWith("/") ? path : "/" + path;
  return prefix + (normalized.endsWith("/") ? normalized + "index.html" : normalized);
}

function acceptsHtml(request) {
  const accept = request.headers.get("accept") || "";
  return accept.includes("text/html") || accept.includes("*/*");
}

async function proxyAdminApi(request, sourceUrl) {
  const targetUrl = new URL(ADMIN_API_ORIGIN);
  targetUrl.pathname = sourceUrl.pathname.replace(/^\\/admin-api\\/?/, "/api/");
  targetUrl.search = sourceUrl.search;

  const headers = new Headers(request.headers);
  headers.set("host", targetUrl.hostname);
  headers.set("x-forwarded-host", sourceUrl.hostname);
  headers.set("x-forwarded-proto", sourceUrl.protocol.replace(":", ""));

  const init = {
    method: request.method,
    headers,
    redirect: "manual",
  };

  if (request.method !== "GET" && request.method !== "HEAD") {
    init.body = request.body;
  }

  return fetch(new Request(targetUrl.toString(), init));
}
`;
}
