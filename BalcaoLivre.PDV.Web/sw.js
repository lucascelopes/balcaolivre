const CACHE_NAME = "balcao-livre-pdv-web-v13";
const APP_SHELL = [
  "./",
  "./index.html",
  "./styles.css?v=13",
  "./src/app.js?v=13",
  "./src/db.js?v=13",
  "./src/data.js?v=13",
  "./src/supabaseAuth.js?v=13",
  "./manifest.webmanifest",
  "./assets/balcao-livre-icon.png",
  "./assets/balcao-livre-logo.png"
];

const indexUrl = new URL("./index.html", self.location.href).href;

self.addEventListener("install", (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => cache.addAll(APP_SHELL))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener("fetch", (event) => {
  const request = event.request;
  if (request.method !== "GET") return;

  event.respondWith(
    fetch(request)
      .then((response) => {
        const copy = response.clone();
        caches.open(CACHE_NAME).then((cache) => cache.put(request, copy));
        return response;
      })
      .catch(() => {
        return caches.match(request).then((cached) => {
          if (cached) return cached;
          if (request.mode === "navigate") {
            return caches.match(indexUrl);
          }
          return Response.error();
        });
      })
  );
});
