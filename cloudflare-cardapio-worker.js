const CARDAPIO_PREFIX = "/cardapio";

export default {
  async fetch(request, env) {
    if (request.method !== "GET" && request.method !== "HEAD") {
      return new Response("Method not allowed", { status: 405 });
    }

    const url = new URL(request.url);
    const assetPath = resolveAssetPath(url.pathname);
    const assetUrl = new URL(request.url);
    assetUrl.pathname = assetPath;

    return env.ASSETS.fetch(new Request(assetUrl.toString(), request));
  }
};

function resolveAssetPath(pathname) {
  const path = stripCardapioPrefix(pathname || "/");
  if (path === "/" || isDocumentPath(path)) {
    return "/index.html";
  }

  return path;
}

function stripCardapioPrefix(pathname) {
  if (pathname === CARDAPIO_PREFIX || pathname === `${CARDAPIO_PREFIX}/`) {
    return "/";
  }

  if (pathname.startsWith(`${CARDAPIO_PREFIX}/`)) {
    return pathname.slice(CARDAPIO_PREFIX.length) || "/";
  }

  return pathname;
}

function isDocumentPath(pathname) {
  const lastSegment = pathname.split("/").filter(Boolean).pop() || "";
  return !lastSegment.includes(".");
}
