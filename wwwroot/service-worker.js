// Blazor Server는 SignalR 연결이 필요하므로 fetch를 인터셉트하지 않음.
// PWA 설치 가능(installable) 조건 충족용 최소 서비스 워커.

self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', event => event.waitUntil(clients.claim()));
