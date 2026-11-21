# Proposal

## Why

На главной странице блок «Разделы» плитками (`CategoryTiles`) стоит между промо-баннером и панелью фильтров и перекрывает основную форму: пользователь не сразу видит форму поиска, а те же разделы уже доступны в постоянной полосе шапки. Блок дублирует навигацию и мешает главному действию страницы.

## What Changes

- Убрать блок разделов плитками с главной страницы: удалить компонент `CategoryTiles.vue` и его использование в `HomeView.vue`.
- Убрать пример этого блока со страницы-гайда дизайн-системы (`DesignSystemGuide.vue`).
- Удалить стили `.category-tiles*` и `.category-tile*` из `frontend/src/assets/css/components.css`.
- Удалить тест компонента `CategoryTiles.spec.ts`.
- Убрать требование «Блок разделов на главной» из спецификации `design-system`.

## Вне рамок

- Постоянная горизонтальная полоса разделов в шапке (`CategoryStrip`) и меню категорий через бургер остаются: это основная навигация по разделам.
- Промо-баннер на главной, панель фильтров, строка результатов, лента объявлений и пагинация не меняются.
- Загрузка списка разделов в `HomeView` сохраняется: она нужна панели фильтров.
- Бэкенд, API и зависимости не затрагиваются.

## Capabilities

### New Capabilities

Нет.

### Modified Capabilities

- `design-system`: удаляется требование «Блок разделов на главной» — главная страница больше не показывает блок разделов и подразделов плитками.

## Impact

- Интерфейс: `frontend/src/views/HomeView.vue`, `frontend/src/views/DesignSystemGuide.vue`, `frontend/src/components/CategoryTiles.vue` (удаляется), `frontend/src/assets/css/components.css`.
- Тесты: `frontend/src/components/__tests__/CategoryTiles.spec.ts` (удаляется).
- Спецификация: `openspec/specs/design-system/spec.md`.
- Бэкенд, API и зависимости не затрагиваются.
