# Tasks

## 1. Убрать блок с главной страницы

- [x] 1.1 Убрать импорт и использование `CategoryTiles` из `frontend/src/views/HomeView.vue`; проверить, что в шаблоне нет `<CategoryTiles>` и загрузка `sections` для панели фильтров сохранена.
- [x] 1.2 Удалить компонент `frontend/src/components/CategoryTiles.vue` и его тест `frontend/src/components/__tests__/CategoryTiles.spec.ts`; проверить, что файлы удалены и ссылок на `CategoryTiles` в `frontend/src` не осталось.

## 2. Убрать пример из гайда и стили

- [x] 2.1 Убрать импорт и пример `<CategoryTiles>` из `frontend/src/views/DesignSystemGuide.vue`; проверить, что `DesignSystemGuide.spec.ts` проходит (`pnpm test` в `frontend`).
- [x] 2.2 Удалить стили `.category-tiles*` и `.category-tile*` из `frontend/src/assets/css/components.css`; проверить, что правила удалены и `frontend/src/assets/__tests__/styles.spec.ts` проходит.

## 3. Проверка результата

- [x] 3.1 Запустить `pnpm test` в `frontend` и убедиться, что все тесты проходят.
- [x] 3.2 Проверить главную страницу через `docker compose`: блок «Разделы» плитками отсутствует, промо-баннер и форма фильтров отображаются, полоса разделов в шапке работает.

## Workflow follow-up

- Архивировать изменение после проверки и ревью.
