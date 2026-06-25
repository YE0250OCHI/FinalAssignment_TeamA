self.addEventListener("install", event => {
    console.log("Service Worker Installed");
});

self.addEventListener("fetch", event => {
});

const CACHE_NAME = "app-v1";

self.addEventListener("install", event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll([
                "/",
                "/css/site.css",
                "/js/site.js"
            ]);
        })
    );
});