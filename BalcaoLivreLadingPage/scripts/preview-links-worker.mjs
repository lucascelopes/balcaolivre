import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { extname, join, normalize } from "node:path";
import { getHtml } from "../deploy/links-worker/index.js";

const port = Number(process.argv[2] || 4216);
const assetsRoot = join(
  process.cwd(),
  "deploy",
  "links-worker",
  "assets",
);

const contentTypes = {
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".webp": "image/webp",
  ".svg": "image/svg+xml",
};

createServer(async (request, response) => {
  const url = new URL(request.url || "/", `http://${request.headers.host}`);

  if (url.pathname === "/links" || url.pathname === "/links/") {
    response.writeHead(200, { "content-type": "text/html; charset=UTF-8" });
    response.end(getHtml());
    return;
  }

  const localPath = normalize(url.pathname).replace(/^([/\\])+/, "");
  const absolutePath = join(assetsRoot, localPath);

  if (!absolutePath.startsWith(assetsRoot)) {
    response.writeHead(403);
    response.end("Forbidden");
    return;
  }

  try {
    const file = await readFile(absolutePath);
    response.writeHead(200, {
      "content-type": contentTypes[extname(absolutePath).toLowerCase()] || "application/octet-stream",
    });
    response.end(file);
  } catch {
    response.writeHead(404);
    response.end("Not found");
  }
}).listen(port, "127.0.0.1", () => {
  console.log(`Links preview ready on http://127.0.0.1:${port}/links`);
});
