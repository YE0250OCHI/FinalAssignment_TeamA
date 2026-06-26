const CACHE_NAME = "app-v1";

const STATIC_FILES = [
    "/",
    "/css/site.css",
    "/js/site.js",
    "/icons/icon-192.png",
    "/icons/icon-512.png",
    "/manifest.json"
];

self.addEventListener("install", event => {
    console.log("Service Worker Installed");

    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(STATIC_FILES))
    );

    self.skipWaiting();
});

self.addEventListener("activate", event => {
    console.log("Service Worker Activated");

    event.waitUntil(
        self.clients.claim()
    );
});

self.addEventListener("fetch", event => {
});