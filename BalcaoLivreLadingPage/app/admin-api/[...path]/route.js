const adminApiUrl = (
  process.env.BALCAO_ADMIN_API_URL || "https://balcaolivrepdv.onrender.com"
).replace(/\/$/, "");

const hopByHopHeaders = new Set([
  "connection",
  "content-length",
  "host",
  "keep-alive",
  "proxy-authenticate",
  "proxy-authorization",
  "te",
  "trailer",
  "transfer-encoding",
  "upgrade"
]);

function targetUrl(request, params) {
  const path = Array.isArray(params?.path) ? params.path.join("/") : "";
  const url = new URL(request.url);
  const search = url.search || "";
  return `${adminApiUrl}/api/${path}${search}`;
}

function forwardHeaders(request) {
  const headers = new Headers();
  request.headers.forEach((value, key) => {
    if (!hopByHopHeaders.has(key.toLowerCase())) {
      headers.set(key, value);
    }
  });
  return headers;
}

async function proxyAdminApi(request, context) {
  const method = request.method.toUpperCase();
  const init = {
    method,
    headers: forwardHeaders(request),
    redirect: "manual"
  };

  if (!["GET", "HEAD"].includes(method)) {
    init.body = await request.arrayBuffer();
  }

  const upstream = await fetch(targetUrl(request, context.params), init);
  const headers = new Headers();
  upstream.headers.forEach((value, key) => {
    if (!hopByHopHeaders.has(key.toLowerCase())) {
      headers.set(key, value);
    }
  });

  return new Response(upstream.body, {
    status: upstream.status,
    statusText: upstream.statusText,
    headers
  });
}

export const dynamic = "force-dynamic";

export const GET = proxyAdminApi;
export const POST = proxyAdminApi;
export const PUT = proxyAdminApi;
export const PATCH = proxyAdminApi;
export const DELETE = proxyAdminApi;
export const HEAD = proxyAdminApi;
