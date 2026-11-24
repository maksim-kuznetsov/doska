# Интерфейс доски объявлений (Vue 3 + TypeScript + Vite)

Одностраничное приложение: Vue 3 (Composition API), TypeScript, Vite и Pinia.
Общее описание проекта, требования к окружению и запуск API с PostgreSQL — в
[корневом README](../README.md).

## Запуск

Dev-сервер с горячей заменой модулей. Запросы к `/api` проксируются на http://localhost:5000,
поэтому API и PostgreSQL должны быть запущены из корня проекта.

```bash
pnpm dev
```

Адрес — http://localhost:5173.

## Сборка и предпросмотр

```bash
pnpm build
pnpm preview
```

`pnpm build` проверяет типы (`vue-tsc -b`) и собирает приложение Vite: файлы с хешем в имени и
манифест попадают в `dist/`. `pnpm preview` поднимает собранную версию на http://localhost:4173.

## Тесты

```bash
pnpm test
```

`pnpm test` запускает Vitest (`vitest run`). Тесты лежат рядом с проверяемым кодом в каталогах
`__tests__` (`src/**/__tests__/*.spec.ts`) и проверяют компоненты и клиентские правила.

Сквозной смоук в браузере (Playwright; нужны запущенные API, PostgreSQL и демо-данные — см.
корневой README):

```bash
pnpm e2e
```

## PWA

Приложение собирается как устанавливаемое: web-манифест, иконки, service worker и офлайн-оболочка.
Service worker включается только в production-сборке и только в защищённом контексте — по HTTPS
или на `localhost`; dev-сервер PWA не включает.

- Иконки пересобираются из `public/pwa/logo.svg`: `pnpm pwa:assets`.
- Проверка PWA-сборки (манифест и service worker): `pnpm check:pwa`.

Подробности о PWA, развёртывании и обновлении — в корневом README и [`deploy/DEPLOY.md`](../deploy/DEPLOY.md).
