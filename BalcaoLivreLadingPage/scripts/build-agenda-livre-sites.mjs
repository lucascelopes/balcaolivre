import { cp, mkdir, readFile, rm, stat, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const projectDirectory = path.resolve(scriptDirectory, "..");
const repositoryDirectory = path.resolve(projectDirectory, "..");
const nextDirectory = path.join(projectDirectory, ".next");
const publicDirectory = path.join(projectDirectory, "public", "agenda-livre");
const productBuildDirectory = path.join(repositoryDirectory, "AgendaLivre.Flutter", "build");
const webAppDirectory = path.join(productBuildDirectory, "web");
const routeHtml = path.join(nextDirectory, "server", "app", "agenda-livre.html");
const distDirectory = path.join(projectDirectory, "dist");
const clientDirectory = path.join(distDirectory, "client");
const serverDirectory = path.join(distDirectory, "server");
const agendaClientDirectory = path.join(clientDirectory, "agenda-livre");

if (path.dirname(distDirectory) !== projectDirectory || path.basename(distDirectory) !== "dist") {
  throw new Error("Diretório de saída inválido.");
}

await Promise.all([
  stat(routeHtml),
  stat(publicDirectory),
  stat(path.join(nextDirectory, "static")),
  stat(path.join(webAppDirectory, "index.html"))
]);
await rm(distDirectory, { recursive: true, force: true });
await mkdir(path.join(clientDirectory, "_next"), { recursive: true });
await mkdir(agendaClientDirectory, { recursive: true });
await mkdir(serverDirectory, { recursive: true });

await cp(path.join(nextDirectory, "static"), path.join(clientDirectory, "_next", "static"), {
  recursive: true
});
await cp(publicDirectory, agendaClientDirectory, { recursive: true });
await rm(path.join(agendaClientDirectory, "downloads"), { recursive: true, force: true });
await cp(webAppDirectory, path.join(agendaClientDirectory, "app"), {
  recursive: true,
  force: true
});

const webIndexPath = path.join(agendaClientDirectory, "app", "index.html");
const webIndex = await readFile(webIndexPath, "utf8");
await writeFile(
  webIndexPath,
  webIndex.replace(/<base href="[^"]*">/, '<base href="/">'),
  "utf8"
);

const html = await readFile(routeHtml, "utf8");
await writeFile(path.join(clientDirectory, "index.html"), html, "utf8");
await writeFile(path.join(clientDirectory, "agenda-livre", "index.html"), html, "utf8");

const worker = `const SECURITY_HEADERS = {
  "Referrer-Policy": "strict-origin-when-cross-origin",
  "X-Content-Type-Options": "nosniff",
  "X-Frame-Options": "SAMEORIGIN"
};

function withHeaders(response) {
  const headers = new Headers(response.headers);
  for (const [name, value] of Object.entries(SECURITY_HEADERS)) headers.set(name, value);
  return new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers
  });
}

const worker = {
  async fetch(request, env) {
    if (!["GET", "HEAD"].includes(request.method)) {
      return new Response("Method not allowed", { status: 405 });
    }

    const direct = await env.ASSETS.fetch(request);
    if (direct.status !== 404) return withHeaders(direct);

    const url = new URL(request.url);
    let fallbackPath = null;
    if (url.pathname === "/" || url.pathname === "") fallbackPath = "/index.html";
    if (url.pathname === "/agenda-livre" || url.pathname === "/agenda-livre/") {
      fallbackPath = "/agenda-livre/index.html";
    }
    if (!fallbackPath) return withHeaders(direct);

    const fallbackUrl = new URL(fallbackPath, request.url);
    const fallback = await env.ASSETS.fetch(
      new Request(fallbackUrl, { method: request.method, headers: request.headers })
    );
    return withHeaders(fallback);
  }
};

export { worker };
export default worker;
`;

await writeFile(path.join(serverDirectory, "index.js"), worker, "utf8");
console.log("Agenda Livre preparado em dist/ para publicação.");
