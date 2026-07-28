const FLUTTER_CACHE_NAMES = [
  'flutter-app-cache',
  'flutter-temp-cache',
  'flutter-app-manifest',
];

self.addEventListener('install', (event) => {
  event.waitUntil(self.skipWaiting());
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    (async () => {
      await Promise.all(
        FLUTTER_CACHE_NAMES.map((cacheName) => caches.delete(cacheName)),
      );
      await self.clients.claim();
      await self.registration.unregister();
    })(),
  );
});

self.addEventListener('fetch', (event) => {
  event.respondWith(fetch(event.request));
});
