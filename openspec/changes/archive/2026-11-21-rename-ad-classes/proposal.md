# Proposal

## Why

Блокировщики рекламы скрывают содержимое сайта: в универсальном списке EasyList (`easylist_general_hide.txt`) есть общие правила `##.ad-card`, `##.ad-grid`, `##.ad-row`, `##.ad-detail` без привязки к домену, поэтому элементы с этими классами прячутся на любом сайте. У нас этими классами свёрстаны сетка ленты, списки и страница объявления — у пользователя с включённым блокировщиком они пропадают.

## What Changes

- Переименовать семейство классов `ad-*` в `item-*` в `frontend/src/assets/css/components.css` и во всех шаблонах, которые их используют: `ad-grid` → `item-grid`, `ad-list` → `item-list`, `ad-card` (и `__media`, `__image`, `__placeholder`, `__body`, `__price`, `__title`, `__meta`, `__description`, `__date`) → `item-card`, `ad-row` (и `__media`, `__image`, `__placeholder`, `__body`, `__head`, `__title`, `__price`, `__meta`, `__reason`, `__actions`) → `item-row`, `ad-detail` (и `__main`, `__aside`) → `item-detail`.
- Переименовать компоненты, которые несут эти классы: `AdCard.vue` → `ItemCard.vue`, `AdRow.vue` → `ItemRow.vue` — вместе с импортами, регистрацией и тестами.
- Переименовать имя маршрута `ad-detail` → `item-detail`.
- Переименовать тестовые атрибуты, привязанные к этим блокам: `data-test="ad-card"` → `item-card`, `data-test="my-ad"` → `my-item`.
- Закрепить в спецификации дизайн-системы правило: имена классов блоков интерфейса не должны совпадать с токенами, которые скрывают универсальные косметические фильтры блокировщиков.
- Обновить `frontend/src/assets/__tests__/styles.spec.ts` и тесты компонентов под новые классы и имена.

## Вне рамок

- Имена представлений (`AdFormView.vue`, `AdEditView.vue`, `AdDetailView.vue`, `MyAdsView.vue`), типы TS API (`Ad`, `AdCard`, `AdDetail`), URL (`/ads`), имена JSON-полей и бэкенд — не меняются: блокировщик видит только HTML и CSS, а не имена файлов, типов и маршрутов.
- Класс `.promo-banner` не трогаем: в универсальном списке EasyList точных правил для `.banner`/`.promo` нет, а переименование вышло бы за рамки задачи.
- Тестовые атрибуты, не связанные с переименованными классами, остаются как есть.

## Capabilities

### New Capabilities

Нет.

### Modified Capabilities

- `design-system`: добавляется требование об устойчивости имён классов к универсальным косметическим фильтрам блокировщиков рекламы; существующие требования о карточке, сетке и списках объявлений не меняют поведение — меняются только имена классов.

## Impact

- Интерфейс: `frontend/src/assets/css/components.css`, `frontend/src/components/AdCard.vue` → `ItemCard.vue`, `frontend/src/components/AdRow.vue` → `ItemRow.vue`, `frontend/src/views/` (`HomeView`, `MyAdsView`, `AdDetailView`, `DesignSystemGuide`, `AdminCategoriesView`, `AdminCitiesView`, `AdminUsersView`, `AdminOutboxView`, `ComplaintsQueueView`), `frontend/src/router/index.ts`.
- Тесты: `frontend/src/assets/__tests__/styles.spec.ts`, `frontend/src/components/__tests__/AdCard.spec.ts` → `ItemCard.spec.ts`, `frontend/src/router/__tests__/router.spec.ts`, `frontend/src/views/__tests__/AdDetailView.spec.ts`.
- Бэкенд, API и зависимости не затрагиваются.
