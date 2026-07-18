'use strict';
const MANIFEST = 'flutter-app-manifest';
const TEMP = 'flutter-temp-cache';
const CACHE_NAME = 'flutter-app-cache';

const RESOURCES = {"assets/AssetManifest.bin": "5e5359f61ec7242b9473dc6d8feb1562",
"assets/AssetManifest.bin.json": "f82ce8a25d1c1fdac64986314c16d869",
"assets/assets/branding/agenda-livre-logo-dark-transparent.png": "e993663b7e91dcd58b14a38274e41742",
"assets/assets/branding/agenda-livre-logo-source.png": "83385bc7ce2d14ac63c7a6cbd4c1fc4b",
"assets/assets/branding/agenda-livre-mark.png": "c4691398ad5e27d83aadf7647555df1a",
"assets/assets/branding/balcao-livre-logo-image.png": "c62fedc6d44df84eeaf29442f68efb79",
"assets/assets/branding/balcao-livre-logo-ref.png": "2615f233a07ca09027375b2c5b592137",
"assets/assets/branding/balcao-livre-logo.png": "f5cdb84775da633f922f9cf531317027",
"assets/assets/branding/onboarding-address.png": "648aa861024187f680ed9a99c889198c",
"assets/assets/branding/onboarding-goal.png": "b5182eda545a6ab02e85ceeee147ac32",
"assets/assets/branding/onboarding-review.png": "e3c1164c2efc6c26281ee7f0b1f95f59",
"assets/assets/branding/onboarding-segment.png": "7a96feeaf2a4fa206cabf89a360e1381",
"assets/assets/branding/onboarding-store-calendar.png": "b43b45334a37cdfcbea0929660c31dbc",
"assets/assets/branding/onboarding-team.png": "d2597a4cbe4895da38c48e857243dabd",
"assets/assets/branding/onboarding-theme.png": "d9d042d6b15c7d6ed85aafc4adfc7f8d",
"assets/assets/themes/aesthetic-coral.png": "8afe48caf5646b17ebb9d39b9c694527",
"assets/assets/themes/aesthetic-lavender.png": "253d5acf4dc9be14256b6a28549e08f4",
"assets/assets/themes/aesthetic-sage.png": "ee5c4d23bef9ba014e1a783cb6e19a48",
"assets/assets/themes/barber-emerald.png": "2c45cd2bf629897fe0cc694dc1bf0c66",
"assets/assets/themes/barber-midnight.png": "b39cb70ceec54c257d8a1a204de0e04a",
"assets/assets/themes/barber-navy.png": "742072419e3ced68f53fca7ffd1bda53",
"assets/assets/themes/default-warm.png": "60fe2732ba59849a34ce6f41a6187334",
"assets/assets/themes/medical-blue.png": "d244f2244ffd5796fca27fa9137eba7d",
"assets/assets/themes/medical-green.png": "e11bf8356e6a850c66fdf1ed6109decf",
"assets/assets/themes/medical-teal.png": "cf38fd5d58b00d9a8941fd635b547651",
"assets/assets/themes/pet-coral.png": "2b363b864250871472532e75e37963b0",
"assets/assets/themes/pet-lilac.png": "15d474509df651eab129359705e7a3d0",
"assets/assets/themes/pet-teal.png": "402338c84108583eb6dec46b7bcb1748",
"assets/assets/themes/podology-blue.png": "5172d6810ecb0e21572257f82810c27f",
"assets/assets/themes/podology-mint.png": "6619a1e49f05d6df27c845af6cdeb149",
"assets/assets/themes/podology-terracotta.png": "bc25fa6ffd15b0e3953293ae526889dd",
"assets/assets/themes/salon-classic-gold.png": "ba2d8b0489cc9d6d20febf5144041997",
"assets/assets/themes/salon-lilac-glow.png": "7f7b330cf07156c59e01e25eb733246d",
"assets/assets/themes/salon-rose-luxe.png": "fdfeca46fad14ec43a89b9ff7a33435d",
"assets/assets/themes/spa-aqua.png": "46924804f6886109abadab5bde173bd6",
"assets/assets/themes/spa-forest.png": "fb01013a7ef3708feb4afaeb67350b74",
"assets/assets/themes/spa-sand.png": "cc4980808f067fba48b587408f5199fe",
"assets/assets/themes/workshop-gold.png": "a4624c9ec704450485ea8c96aea7f1b6",
"assets/assets/themes/workshop-graphite.png": "b361ea4ffb79dcbbb95f3b964f16d813",
"assets/assets/themes/workshop-olive.png": "7084bec62ed5a0ccd4a75b305a2e46ee",
"assets/FontManifest.json": "c75f7af11fb9919e042ad2ee704db319",
"assets/fonts/MaterialIcons-Regular.otf": "246351aaec16be5fc13798381012697d",
"assets/NOTICES": "92fcac77a335d9f55e584bbcd3bab6cd",
"assets/packages/cupertino_icons/assets/CupertinoIcons.ttf": "33b7d9392238c04c131b6ce224e13711",
"assets/packages/font_awesome_flutter/lib/fonts/Font-Awesome-7-Brands-Regular-400.otf": "95174c6394f2501d26b698ede2ae13bf",
"assets/packages/font_awesome_flutter/lib/fonts/Font-Awesome-7-Free-Regular-400.otf": "46be639d952abe98effde36da35e7701",
"assets/packages/font_awesome_flutter/lib/fonts/Font-Awesome-7-Free-Solid-900.otf": "48b92e8451309fdcb73d294f0f6e9830",
"assets/shaders/ink_sparkle.frag": "ecc85a2e95f5e9f53123dcaf8cb9b6ce",
"assets/shaders/stretch_effect.frag": "40d68efbbf360632f614c731219e95f0",
"canvaskit/canvaskit.js": "8331fe38e66b3a898c4f37648aaf7ee2",
"canvaskit/canvaskit.js.symbols": "a3c9f77715b642d0437d9c275caba91e",
"canvaskit/canvaskit.wasm": "9b6a7830bf26959b200594729d73538e",
"canvaskit/chromium/canvaskit.js": "a80c765aaa8af8645c9fb1aae53f9abf",
"canvaskit/chromium/canvaskit.js.symbols": "e2d09f0e434bc118bf67dae526737d07",
"canvaskit/chromium/canvaskit.wasm": "a726e3f75a84fcdf495a15817c63a35d",
"canvaskit/skwasm.js": "8060d46e9a4901ca9991edd3a26be4f0",
"canvaskit/skwasm.js.symbols": "3a4aadf4e8141f284bd524976b1d6bdc",
"canvaskit/skwasm.wasm": "7e5f3afdd3b0747a1fd4517cea239898",
"canvaskit/skwasm_heavy.js": "740d43a6b8240ef9e23eed8c48840da4",
"canvaskit/skwasm_heavy.js.symbols": "0755b4fb399918388d71b59ad390b055",
"canvaskit/skwasm_heavy.wasm": "b0be7910760d205ea4e011458df6ee01",
"favicon.png": "22ebb12690dafbfe364b47696bc3398c",
"flutter.js": "24bc71911b75b5f8135c949e27a2984e",
"flutter_bootstrap.js": "70e937957517e6cfa6f290e634467ce8",
"icons/Icon-192.png": "c120ee44ede7e5bf793ec3d0c9d182f7",
"icons/Icon-512.png": "c05545d65a4b49ec0ff93ebe646547a1",
"icons/Icon-maskable-192.png": "ea9da4af18cc6be45d9c1ca7f328b4f2",
"icons/Icon-maskable-512.png": "16298fa704a5aca02ef6f2cb60cd9ca4",
"index.html": "c6ab63a4e7aabf39ec75a25c2219f769",
"/": "c6ab63a4e7aabf39ec75a25c2219f769",
"main.dart.js": "6058d497ffbfeaffdd4d76857ab5eaf6",
"manifest.json": "38b8f529446c74474781bf1a35bed402",
"version.json": "733742cc9df7eef9a33b63299ad286cd"};
// The application shell files that are downloaded before a service worker can
// start.
const CORE = ["main.dart.js",
"index.html",
"flutter_bootstrap.js",
"assets/AssetManifest.bin.json",
"assets/FontManifest.json"];

// During install, the TEMP cache is populated with the application shell files.
self.addEventListener("install", (event) => {
  self.skipWaiting();
  return event.waitUntil(
    caches.open(TEMP).then((cache) => {
      return cache.addAll(
        CORE.map((value) => new Request(value, {'cache': 'reload'})));
    })
  );
});
// During activate, the cache is populated with the temp files downloaded in
// install. If this service worker is upgrading from one with a saved
// MANIFEST, then use this to retain unchanged resource files.
self.addEventListener("activate", function(event) {
  return event.waitUntil(async function() {
    try {
      var contentCache = await caches.open(CACHE_NAME);
      var tempCache = await caches.open(TEMP);
      var manifestCache = await caches.open(MANIFEST);
      var manifest = await manifestCache.match('manifest');
      // When there is no prior manifest, clear the entire cache.
      if (!manifest) {
        await caches.delete(CACHE_NAME);
        contentCache = await caches.open(CACHE_NAME);
        for (var request of await tempCache.keys()) {
          var response = await tempCache.match(request);
          await contentCache.put(request, response);
        }
        await caches.delete(TEMP);
        // Save the manifest to make future upgrades efficient.
        await manifestCache.put('manifest', new Response(JSON.stringify(RESOURCES)));
        // Claim client to enable caching on first launch
        self.clients.claim();
        return;
      }
      var oldManifest = await manifest.json();
      var origin = self.location.origin;
      for (var request of await contentCache.keys()) {
        var key = request.url.substring(origin.length + 1);
        if (key == "") {
          key = "/";
        }
        // If a resource from the old manifest is not in the new cache, or if
        // the MD5 sum has changed, delete it. Otherwise the resource is left
        // in the cache and can be reused by the new service worker.
        if (!RESOURCES[key] || RESOURCES[key] != oldManifest[key]) {
          await contentCache.delete(request);
        }
      }
      // Populate the cache with the app shell TEMP files, potentially overwriting
      // cache files preserved above.
      for (var request of await tempCache.keys()) {
        var response = await tempCache.match(request);
        await contentCache.put(request, response);
      }
      await caches.delete(TEMP);
      // Save the manifest to make future upgrades efficient.
      await manifestCache.put('manifest', new Response(JSON.stringify(RESOURCES)));
      // Claim client to enable caching on first launch
      self.clients.claim();
      return;
    } catch (err) {
      // On an unhandled exception the state of the cache cannot be guaranteed.
      console.error('Failed to upgrade service worker: ' + err);
      await caches.delete(CACHE_NAME);
      await caches.delete(TEMP);
      await caches.delete(MANIFEST);
    }
  }());
});
// The fetch handler redirects requests for RESOURCE files to the service
// worker cache.
self.addEventListener("fetch", (event) => {
  if (event.request.method !== 'GET') {
    return;
  }
  var origin = self.location.origin;
  var key = event.request.url.substring(origin.length + 1);
  // Redirect URLs to the index.html
  if (key.indexOf('?v=') != -1) {
    key = key.split('?v=')[0];
  }
  if (event.request.url == origin || event.request.url.startsWith(origin + '/#') || key == '') {
    key = '/';
  }
  // If the URL is not the RESOURCE list then return to signal that the
  // browser should take over.
  if (!RESOURCES[key]) {
    return;
  }
  // If the URL is the index.html, perform an online-first request.
  if (key == '/') {
    return onlineFirst(event);
  }
  event.respondWith(caches.open(CACHE_NAME)
    .then((cache) =>  {
      return cache.match(event.request).then((response) => {
        // Either respond with the cached resource, or perform a fetch and
        // lazily populate the cache only if the resource was successfully fetched.
        return response || fetch(event.request).then((response) => {
          if (response && Boolean(response.ok)) {
            cache.put(event.request, response.clone());
          }
          return response;
        });
      })
    })
  );
});
self.addEventListener('message', (event) => {
  // SkipWaiting can be used to immediately activate a waiting service worker.
  // This will also require a page refresh triggered by the main worker.
  if (event.data === 'skipWaiting') {
    self.skipWaiting();
    return;
  }
  if (event.data === 'downloadOffline') {
    downloadOffline();
    return;
  }
});
// Download offline will check the RESOURCES for all files not in the cache
// and populate them.
async function downloadOffline() {
  var resources = [];
  var contentCache = await caches.open(CACHE_NAME);
  var currentContent = {};
  for (var request of await contentCache.keys()) {
    var key = request.url.substring(origin.length + 1);
    if (key == "") {
      key = "/";
    }
    currentContent[key] = true;
  }
  for (var resourceKey of Object.keys(RESOURCES)) {
    if (!currentContent[resourceKey]) {
      resources.push(resourceKey);
    }
  }
  return contentCache.addAll(resources);
}
// Attempt to download the resource online before falling back to
// the offline cache.
function onlineFirst(event) {
  return event.respondWith(
    fetch(event.request).then((response) => {
      return caches.open(CACHE_NAME).then((cache) => {
        cache.put(event.request, response.clone());
        return response;
      });
    }).catch((error) => {
      return caches.open(CACHE_NAME).then((cache) => {
        return cache.match(event.request).then((response) => {
          if (response != null) {
            return response;
          }
          throw error;
        });
      });
    })
  );
}
