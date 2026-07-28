import { createReadStream } from "node:fs";
import { stat } from "node:fs/promises";
import { createServer } from "node:http";
import path from "node:path";

const root = path.resolve(process.argv[2] || "build/web");
const port = Number(process.argv[3] || 4227);
const types = {
  ".css": "text/css; charset=utf-8",
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".png": "image/png",
  ".svg": "image/svg+xml",
  ".wasm": "application/wasm",
  ".woff2": "font/woff2",
};

createServer(async (request, response) => {
  const requestPath = decodeURIComponent(
    new URL(request.url || "/", "http://localhost").pathname,
  );
  const candidate = path.resolve(root, `.${requestPath}`);
  const safeCandidate = candidate.startsWith(root) ? candidate : root;
  let file = safeCandidate;
  try {
    if ((await stat(file)).isDirectory()) file = path.join(file, "index.html");
  } catch {
    file = path.join(root, "index.html");
  }
  response.setHeader("Content-Type", types[path.extname(file)] || "application/octet-stream");
  response.setHeader("Cache-Control", "no-store");
  createReadStream(file)
    .on("error", () => {
      response.statusCode = 404;
      response.end("Not found");
    })
    .pipe(response);
}).listen(port, "127.0.0.1");
