# Design

## Context

См. `proposal.md` — мотивация и границы изменения.

Текущее состояние, важное для реализации:

- Интерфейс собирается Vite 8 с `build.manifest: true`; production-сборка отдаётся nginx из `frontend/dist` (`frontend/Dockerfile`, `deploy/nginx.conf`).
- nginx уже разделяет кеширование: `index.html` — `no-cache`, `/assets/` — `immutable` на год. API проксируется на `api:8080`, медиа отдаются внутренним путём `/media-internal/`.
- Интерфейс — SPA на Vue Router с `createWebHistory`; авторизация — серверная cookie, поэтому кеширование ответов API недопустимо.
- Production compose публикует только HTTP-порт 80; HTTPS-терминация остаётся внешней (см. «Вне рамок» в proposal).
- Адаптивность уже есть (точка перелома 768 px), но безопасные зоны экрана и режим `standalone` не учитываются.
- Есть тесты конфигурации (`frontend/src/__tests__/staticAssets.spec.ts`, `deployment.spec.ts`), которые читают файлы репозитория — этот приём переиспользуется для проверки PWA-конфигурации.

## Goals / Non-Goals

**Goals:**

- Установка приложения на домашний экран в поддерживающих браузерах по HTTPS.
- Офлайн-оболочка: после первой успешной загрузки приложение открывается без сети и показывает офлайн-страницу, не обращаясь к API.
- Управляемое обновление: новая версия не подменяет работающую сборку без действия пользователя.
- Аккуратный мобильный вид в `standalone`: безопасные зоны, цвет статус-бара, отсутствие горизонтальной прокрутки.
- Проверяемая конфигурация: сборка и nginx-заголовки покрыты автоматическими проверками.

**Non-Goals:**

- Кеширование объявлений, фотографий и других данных API.
- Push-уведомления, Background Sync и офлайн-подача объявлений.
- Настройка TLS, домена и внешнего прокси.
- Мобильная нижняя навигация и переработка информационной архитектуры.
- Service worker в режиме разработки Vite.

## Decisions

### 1. `vite-plugin-pwa` + Workbox

Используется `vite-plugin-pwa` ^2.0 (поддерживает Vite 8) со стратегией `generateSW` (Workbox 7). Плагин генерирует `sw.js`, `manifest.webmanifest` и `workbox-*.js` в `frontend/dist`.

Настройки:

- `registerType: 'prompt'`, `injectRegister: null` — регистрация вручную, чтобы управлять интерфейсом обновления.
- `devOptions.enabled: false` — dev-сервер и Vitest не получают service worker.
- `workbox.globPatterns: ['**/*.{js,css,html,svg,png,webmanifest}']`, `navigateFallback: '/index.html'`, `navigateFallbackDenylist: [/^\/api\//, /^\/media\//]`.
- `cleanupOutdatedCaches: true`; `skipWaiting` и `clientsClaim` не включаются глобально — активация новой версии только по действию пользователя.
- Runtime-маршруты для `/api/` и `/media/` не добавляются: запросы к API и медиа идут только в сеть.

Альтернативы: собственный service worker с ручным списком ассетов — сложнее поддерживать хешированные имена Vite; `vite-plugin-pwa` с `injectManifest` — избыточен, пока нет собственной логики кеширования.

### 2. Оболочка и офлайн-страница

- Оболочка (`index.html` и хешированные JS/CSS) попадает в precache. При потере сети навигация получает закешированный `index.html`, поэтому SPA запускается.
- В `main.ts` до создания Pinia и роутера проверяется `navigator.onLine`. Если сети нет, монтируется лёгкий `OfflineApp.vue` без роутера, Pinia и обращений к API: сообщение об отсутствии подключения и кнопка «Повторить» (`location.reload()`). Так офлайн-старт не запускает guard’ы роутера и `auth.load()`.
- Если приложение уже открыто и связь пропала, `DefaultLayout` показывает баннер «Нет подключения к интернету» на основе событий `online`/`offline`. Данные при этом не подменяются кешем.
- Отдельный статический `offline.html` не нужен: офлайн-страница — часть precache-оболочки.

Альтернативы: `navigateFallback: '/offline.html'` — проще, но не даёт запустить оболочку и показать офлайн-состояние в контексте приложения; кеширование данных API — противоречит требованию актуальности.

### 3. Обновление версии

- Регистрация через `virtual:pwa-register` в `src/pwa/registerServiceWorker.ts`; колбэки `onNeedRefresh`, `onOfflineReady`, `onRegisterError` пишут состояние в Pinia-хранилище `src/stores/pwa.ts`.
- `registerSW` вызывается только в production-сборке и только при `window.isSecureContext`.
- Дополнительно `registration.update()` вызывается при возврате фокуса на вкладку и раз в час, чтобы после деплоя предложение появлялось без полной перезагрузки.
- Компонент `PwaUpdatePrompt.vue` (на базе `AppMessage` и `AppButton`) показывает «Доступна новая версия» с действиями «Обновить» и «Позже». «Обновить» вызывает `updateServiceWorker(true)` (активация waiting-SW и перезагрузка). «Позже» скрывает сообщение до конца сеанса (`sessionStorage`), при следующем запуске предложение появляется снова.
- `sw.js` и `manifest.webmanifest` отдаются nginx с `Cache-Control: no-cache`, поэтому браузер видит новую версию service worker.

### 4. Манифест и иконки

- Манифест задаётся в `vite.config.ts` (`VitePWA.manifest`): `name: 'Doska — доска объявлений'`, `short_name: 'Doska'`, `lang: 'ru'`, `start_url: '/'`, `scope: '/'`, `display: 'standalone'`, `theme_color: '#00AAFF'`, `background_color: '#F5F5F7'`, `icons` для 192, 512 и maskable 512.
- Исходник иконки — `frontend/public/pwa/logo.svg`: брендовый знак на синем `#00AAFF` с белым символом «D», значимая часть внутри safe-зоны maskable.
- PNG-размеры генерируются `@vite-pwa/assets-generator` (devDependency) командой `pnpm pwa:assets` и коммитятся: `pwa-192x192.png`, `pwa-512x512.png`, `maskable-icon-512x512.png`, `apple-touch-icon-180x180.png`.
- В `index.html` добавляются `theme-color`, `apple-mobile-web-app-capable`, `apple-mobile-web-app-title`, `apple-touch-icon` и `viewport-fit=cover`.

Альтернативы: сторонний сервис иконок — лишняя внешняя зависимость; одна SVG-иконка — не выполняет условия установки в Chromium и хуже поддерживается Android/iOS.

### 5. Установка из интерфейса

- `beforeinstallprompt` перехватывается при инициализации `src/stores/pwa.ts` и сохраняется в состоянии; компонент `PwaInstallPrompt.vue` показывает «Установить приложение» только когда событие доступно и приложение не установлено.
- Нажатие вызывает `prompt()`; после `appinstalled` или принятия предложения действие скрывается. Отказ скрывает предложение до конца сеанса (`sessionStorage`), не блокируя сайт.
- Для iOS (включая iPadOS, который представляется как macOS) событие недоступно: показывается текстовая подсказка «Поделиться → На экран „Домой“».
- Проверка `display-mode: standalone` и `navigator.standalone` скрывает установку в уже установленном приложении.

### 6. Безопасные зоны и standalone

- В `tokens.css` добавляются `--safe-area-top/right/bottom/left: env(safe-area-inset-*, 0px)`.
- В `layout.css` отступы шапки, основной области и подвала складываются с безопасными зонами; на мобильной ширине используются те же переменные. Горизонтальные отступы не дают контенту уйти под скругления.
- `min-height` оболочки получает `100dvh` с fallback `100vh`, чтобы мобильная адресная строка и системные панели не создавали лишнюю прокрутку.
- Цвет статус-бара задаётся `theme-color`; в standalone он совпадает с синим брендом.

### 7. nginx

В `deploy/nginx.conf` добавляются точные locations:

- `location = /sw.js` — `Cache-Control: no-cache`.
- `location = /manifest.webmanifest` — `Cache-Control: no-cache` и явный `default_type application/manifest+json` (не полагаемся на версию mime.types в образе).
- Существующие `/assets/` и `index.html` не меняются; SPA-fallback не перехватывает отсутствующий `sw.js`, потому что точный location возвращает 404.

### 8. Проверка

- Модульные тесты (`src/pwa/__tests__/pwa.spec.ts`): определение iOS/standalone/secure context, обработка `beforeinstallprompt` и отказа, состояние онлайн/офлайн, показ обновления.
- Тест конфигурации (`src/__tests__/pwaConfig.spec.ts`): наличие манифеста и иконок, `viewport-fit=cover`, `theme-color`, `no-cache` для `sw.js` и манифеста в nginx.
- Скрипт `frontend/scripts/check-pwa-build.mjs` (`pnpm check:pwa`) проверяет после сборки: `dist/sw.js`, `dist/manifest.webmanifest` с обязательными полями, наличие всех иконок, ссылку на манифест в `dist/index.html`. Шаг добавляется в CI после `pnpm build`.
- Ручная проверка: `pnpm build`, `pnpm preview`, DevTools → Application (Manifest, Service Workers), offline-режим, установка; на production — после настройки HTTPS.

## Risks / Trade-offs

- **Пользователи по HTTP не получат PWA.** → Функции регистрируются только при `isSecureContext`; сайт работает как обычно. В README фиксируется требование HTTPS для production.
- **Устаревшая оболочка после деплоя.** → `no-cache` для `sw.js` и манифеста, `cleanupOutdatedCaches`, явное предложение обновления; новая версия не активируется без действия пользователя.
- **iOS ограничивает установку и service worker.** → Подсказка через «Поделиться», стандартные meta-теги, проверка на реальном устройстве.
- **Откат версии с уже установленным service worker.** → SW не кеширует API и данные; откат интерфейса безопасен. Для полного удаления PWA при необходимости выпускается сборка с самоудалением service worker — описывается в README.
- **Офлайн-старт обходит роутер и Pinia.** → Отдельный `OfflineApp` намеренно минимален; после «Повторить» страница перезагружается обычным путём.
- **Ошибка в manifest/иконках ломает установку незаметно.** → `check:pwa` в CI и тест конфигурации.
- **Дополнительный вес precache.** → Кешируются только JS/CSS/HTML и иконки; медиа и данные не попадают.

## Migration Plan

1. Добавить зависимости и конфигурацию PWA, иконки и офлайн-компоненты; собрать `frontend/dist`.
2. Обновить nginx и проверить заголовки для `sw.js` и манифеста.
3. Задеплоить обычным `docker compose -f docker-compose.prod.yml up -d --build`; HTTPS обеспечивает внешний прокси.
4. Первый визит после деплоя устанавливает service worker; последующие деплои показывают предложение обновления.
5. Откат: вернуть предыдущую ревизию и пересобрать. Установленный SW не мешает онлайн-работе; при необходимости удалить PWA — выпустить сборку с самоудалением service worker.

## Open Questions

Нет.