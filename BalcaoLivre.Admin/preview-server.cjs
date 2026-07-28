const http = require("http");
const fs = require("fs");
const path = require("path");
const { URL } = require("url");

const root = path.join(__dirname, "wwwroot");
const previewCookie = "bl_admin_preview";
const previewUsers = [
  process.env.BVPDV_ADMIN_USER,
  process.env.BVPDV_ADMIN_USER_2
].filter(Boolean).map((value) => normalizeLogin(value));
const previewPassword = process.env.BVPDV_ADMIN_PASSWORD || "";
const contentTypes = {
  ".css": "text/css; charset=utf-8",
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".png": "image/png",
  ".svg": "image/svg+xml"
};

function normalizeLogin(value) {
  return String(value || "").trim().toLowerCase().replaceAll(",", ".");
}

function sendJson(response, statusCode, payload, headers = {}) {
  response.writeHead(statusCode, {
    "Cache-Control": "no-store",
    "Content-Type": "application/json; charset=utf-8",
    ...headers
  });
  response.end(JSON.stringify(payload));
}

function readJson(request) {
  return new Promise((resolve, reject) => {
    let body = "";
    request.setEncoding("utf8");
    request.on("data", (chunk) => {
      body += chunk;
      if (body.length > 32_768) {
        reject(new Error("Payload muito grande."));
        request.destroy();
      }
    });
    request.on("end", () => {
      try {
        resolve(body ? JSON.parse(body) : {});
      } catch (error) {
        reject(error);
      }
    });
    request.on("error", reject);
  });
}

async function handleApi(request, response, pathname) {
  if (pathname === "/api/health" && request.method === "GET") {
    sendJson(response, 200, { ok: true, app: "Balcão Livre Admin Preview", storage: "preview" });
    return;
  }

  if (pathname === "/api/session" && request.method === "GET") {
    const authenticated = String(request.headers.cookie || "")
      .split(";")
      .some((item) => item.trim() === `${previewCookie}=1`);
    sendJson(response, 200, { authenticated, preview: authenticated });
    return;
  }

  if (pathname === "/api/login" && request.method === "POST") {
    try {
      const body = await readJson(request);
      const user = normalizeLogin(body.user);
      const password = String(body.password || "");
      const configuredCredentials = previewUsers.length > 0 && Boolean(previewPassword);
      const validCredentials = configuredCredentials
        ? previewUsers.includes(user) && password === previewPassword
        : Boolean(user && password);
      if (!validCredentials) {
        sendJson(response, 401, { ok: false, message: "Login ou senha inválidos." });
        return;
      }
      sendJson(response, 200, { ok: true, user, preview: true }, {
        "Set-Cookie": `${previewCookie}=1; HttpOnly; SameSite=Strict; Path=/; Max-Age=43200`
      });
    } catch {
      sendJson(response, 400, { ok: false, message: "Dados de login inválidos." });
    }
    return;
  }

  if (pathname === "/api/logout" && request.method === "POST") {
    sendJson(response, 200, { ok: true }, {
      "Set-Cookie": `${previewCookie}=; HttpOnly; SameSite=Strict; Path=/; Max-Age=0`
    });
    return;
  }

  sendJson(response, 404, { ok: false, message: "Endpoint indisponível na prévia local." });
}

http.createServer(async (request, response) => {
  const url = new URL(request.url, "http://127.0.0.1:5188");
  const pathname = url.pathname === "/" ? "/index.html" : url.pathname;
  if (pathname.startsWith("/api/")) {
    await handleApi(request, response, pathname);
    return;
  }
  const file = path.resolve(root, `.${pathname}`);
  if (!file.startsWith(root)) {
    response.writeHead(403).end("Forbidden");
    return;
  }
  fs.readFile(file, (error, bytes) => {
    if (error) {
      response.writeHead(404).end("Not found");
      return;
    }
    response.setHeader("Cache-Control", "no-store");
    response.setHeader("Content-Type", contentTypes[path.extname(file)] || "application/octet-stream");
    response.end(bytes);
  });
}).listen(5188, "127.0.0.1");
