const worker = {
  async fetch(request, env) {
    const assetResponse = await env.ASSETS.fetch(request);
    const acceptsHtml = (request.headers.get("accept") ?? "").includes(
      "text/html",
    );

    if (
      assetResponse.status !== 404 ||
      !["GET", "HEAD"].includes(request.method) ||
      !acceptsHtml
    ) {
      return assetResponse;
    }

    const indexUrl = new URL("/index.html", request.url);
    return env.ASSETS.fetch(
      new Request(indexUrl, {
        method: request.method,
        headers: request.headers,
        redirect: request.redirect,
      }),
    );
  },
};

export default worker;
