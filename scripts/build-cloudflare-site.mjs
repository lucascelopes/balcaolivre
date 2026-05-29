import { cp, mkdir, rm, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { execFile } from "node:child_process";
import { promisify } from "node:util";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const execFileAsync = promisify(execFile);
const outputDir = process.argv[2]
  ? path.resolve(root, process.argv[2])
  : path.join(root, "dist", "cloudflare-site");
const netlifyOutput = path.join(root, "dist", "cloudflare-netlify-stage");
const publicApexDomain = process.env.BALCAO_PUBLIC_APEX_DOMAIN || "balcaolivrepdv.com.br";
const adminApiOrigin = process.env.BALCAO_ADMIN_API_ORIGIN || "https://balcaolivrepdv.onrender.com";

const fromRoot = (...parts) => path.join(root, ...parts);
const toOutput = (...parts) => path.join(outputDir, ...parts);

await rm(outputDir, { recursive: true, force: true });
await mkdir(outputDir, { recursive: true });

await execFileAsync(process.execPath, [fromRoot("scripts", "build-netlify-site.mjs"), netlifyOutput], { cwd: root });
await cp(netlifyOutput, outputDir, { recursive: true, force: true });

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
