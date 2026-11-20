# Tasks

## 1. Зависимости и базовая настройка

- [x] 1.1 Добавить `vite-plugin-pwa` и `@vite-pwa/assets-generator` в `frontend/package.json` (devDependencies) и обновить `pnpm-lock.yaml`; проверить командой `pnpm --dir frontend list vite-plugin-pwa @vite-pwa/assets-generator`
- [x] 1.2 Добавить `vite-plugin-pwa/client` в `types` файла `frontend/tsconfig.app.json` и скрипты `pwa:assets` и `check:pwa` в `frontend/package.json`; проверить, что `pnpm --dir frontend build` проходит без ошибок типов

## 2. Манифест, иконки и HTML-оболочка

- [x] 2.1 Создать брендовый исходник `frontend/public/pwa/logo.svg` (синий `#00AAFF`, белый знак «D», запас вокруг знака для maskable); проверить, что `pnpm --dir frontend pwa:assets` создаёт `pwa-192x192.png`, `pwa-512x512.png`, `maskable-icon-512x512.png`, `apple-touch-icon-180x180.png`
- [x] 2.2 Настроить `VitePWA` в `frontend/vite.config.ts`: манифест (`name`, `short_name`, `lang`, `start_url`, `scope`, `display: standalone`, цвета, иконки), `registerType: 'prompt'`, `injectRegister: null`, `devOptions.enabled: false`, precache и `navigateFallback`; проверить, что `pnpm --dir frontend build` создаёт `dist/manifest.webmanifest` с ожидаемыми полями и `dist/sw.js`
- [x] 2.3 Добавить в `frontend/index.html` `theme-color`, `apple-mobile-web-app-capable`, `apple-mobile-web-app-title`, `apple-touch-icon` и `viewport-fit=cover`; проверить, что `dist/index.html` после сборки ссылается на манифест и содержит эти мета-теги
- [x] 2.4 Добавить тест `frontend/src/__tests__/pwaConfig.spec.ts` на наличие манифеста, иконок и мета-тегов; проверить `pnpm --dir frontend test`

## 3. Service worker и офлайн-оболочка

- [x] 3.1 Реализовать состояние PWA в `frontend/src/stores/pwa.ts` (`needRefresh`, `offlineReady`, `installEvent`, `isInstalled`, `isIos`, `isOnline`, отказ от установки) и регистрацию в `frontend/src/pwa/registerServiceWorker.ts` через `virtual:pwa-register`; проверить, что регистрация выполняется только при `import.meta.env.PROD` и `window.isSecureContext`
- [x] 3.2 Добавить `frontend/src/pwa/OfflineApp.vue` с сообщением об отсутствии подключения и кнопкой «Повторить»; в `frontend/src/main.ts` монтировать его без Pinia и роутера при `navigator.onLine === false`; проверить, что в офлайн-режиме нет запросов к `/api` (DevTools Network)
- [x] 3.3 Показывать баннер «Нет подключения к интернету» в `DefaultLayout` при событиях `offline`/`online`; проверить отображение и скрытие баннера в юнит-тесте `frontend/src/pwa/__tests__/pwa.spec.ts`
- [x] 3.4 Покрыть тестами определение iOS/standalone/secure context, онлайн-состояние и обработку отказа от установки; проверить `pnpm --dir frontend test`

## 4. Установка и обновление версии

- [x] 4.1 Создать `frontend/src/components/PwaInstallPrompt.vue` на базе `AppMessage`/`AppButton`: показ при `beforeinstallprompt`, скрытие после установки, подсказка для iOS, закрытие до конца сеанса; проверить сценарии юнит-тестом
- [x] 4.2 Создать `frontend/src/components/PwaUpdatePrompt.vue`: сообщение о новой версии, «Обновить» через `updateServiceWorker(true)`, «Позже» до конца сеанса; проверить вызов обновления и скрытие юнит-тестом
- [x] 4.3 Подключить оба компонента в `frontend/src/layouts/DefaultLayout.vue`; проверить, что `pnpm --dir frontend test` проходит и компоненты не ломают монтирование страниц

## 5. Безопасные зоны и standalone

- [x] 5.1 Добавить в `frontend/src/assets/css/tokens.css` переменные `env(safe-area-inset-*)`, а в `frontend/src/assets/css/layout.css` — отступы шапки, основной области и подвала с этими переменными и `min-height: 100dvh`; проверить отсутствие горизонтальной прокрутки в мобильной эмуляции
- [x] 5.2 Проверить вид в `display-mode: standalone`: шапка ниже выреза, подвал выше домашнего индикатора, цвет статус-бара совпадает с `theme-color`; зафиксировать проверку в `frontend/src/__tests__/pwaConfig.spec.ts` (наличие `safe-area-inset` и `100dvh` в стилях)

## 6. nginx, проверка сборки и документация

- [x] 6.1 Добавить в `deploy/nginx.conf` точные locations для `sw.js` и `manifest.webmanifest` с `Cache-Control: no-cache` и `default_type application/manifest+json`; проверить `docker compose -f docker-compose.prod.yml config` и тест `pwaConfig.spec.ts`
- [x] 6.2 Создать `frontend/scripts/check-pwa-build.mjs`, проверяющий `dist/sw.js`, обязательные поля `dist/manifest.webmanifest`, наличие иконок и ссылку на манифест в `dist/index.html`; проверить `pnpm --dir frontend build`, затем `pnpm --dir frontend check:pwa`
- [x] 6.3 Добавить шаг `pnpm check:pwa` в job `frontend` файла `.github/workflows/ci.yml` после сборки; проверить, что локальный запуск `pnpm --dir frontend check:pwa` повторяет проверку CI
- [x] 6.4 Описать в `README.md` установку PWA, офлайн-режим, требование HTTPS в production, локальную проверку через `pnpm preview` и порядок отката; проверить, что команды из раздела выполняются как написаны

## 7. Интеграционная проверка

- [x] 7.1 Прогнать `pnpm --dir frontend test`, `pnpm --dir frontend build` и `pnpm --dir frontend check:pwa`; все команды должны завершиться успешно
- [x] 7.2 Запустить `pnpm --dir frontend preview`, проверить в DevTools Application манифест и service worker, офлайн-старт и предложение обновления после повторной сборки
- [x] 7.3 Собрать production-образ `docker compose -f docker-compose.prod.yml build web` и проверить `curl.exe -I` для `sw.js` и `manifest.webmanifest` (ожидается `Cache-Control: no-cache`)
- [x] 7.4 Прогнать `dotnet test backend/Doska.sln` (бэкенд не менялся) и сообщить итог вместе с итогами интерфейсных проверок

## Workflow follow-up

- Архивировать change после проверки артефактов и результатов тестов.
- Предложить коммит на русском в стиле «Архивировать change add-pwa».