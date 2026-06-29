const CACHE_NAME = "app-v1";

const STATIC_FILES = [
    "/picking-orders",
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
            .then(async (cache) => {
                for (const file of STATIC_FILES) {
                    try {
                        await cache.add(file);
                        console.log(`キャッシュ成功: ${file}`);
                    } catch (error) {
                        console.error(`キャッシュ失敗: ${file}`, error);
                    }
                }

                await self.skipWaiting();
            })
    );

});

self.addEventListener("activate", event => {
    console.log("Service Worker Activated");

    event.waitUntil(
        self.clients.claim()
    );
});

self.addEventListener("fetch", event => {
});