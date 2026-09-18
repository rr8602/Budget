// Blazor Server는 SignalR 연결이 필요하므로 fetch를 캐싱하지 않음.
// fetch 핸들러 등록은 PWA 설치 조건 충족용 (respondWith 미호출 → 브라우저 기본 동작 유지).

self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', event => event.waitUntil(clients.claim()));
self.addEventListener('fetch', () => {});
