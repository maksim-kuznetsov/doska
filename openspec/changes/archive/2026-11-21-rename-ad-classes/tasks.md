# Tasks

## 1. Классы оформления и шаблоны

- [x] 1.1 Переименовать селекторы семейства `ad-*` в `item-*` в `frontend/src/assets/css/components.css`: `ad-grid` → `item-grid`, `ad-list` → `item-list`, `ad-card` → `item-card`, `ad-row` → `item-row`, `ad-detail` → `item-detail` вместе со всеми BEM-потомками (`__media`, `__image`, `__placeholder`, `__body`, `__head`, `__price`, `__title`, `__meta`, `__description`, `__date`, `__reason`, `__actions`, `__main`, `__aside`) и состояниями (`:hover`, `:focus-visible`). Проверка: поиск `ad-(card|grid|list|row|detail)` в `frontend/src/assets/css/components.css` не находит совпадений.
- [x] 1.2 Обновить классы в шаблонах, использующих переименованные блоки: `components/AdCard.vue`, `components/AdRow.vue`, `views/AdDetailView.vue`, `views/HomeView.vue`, `views/MyAdsView.vue`, `views/DesignSystemGuide.vue`, `views/AdminCategoriesView.vue`, `views/AdminCitiesView.vue`, `views/AdminUsersView.vue`, `views/AdminOutboxView.vue`, `views/ComplaintsQueueView.vue`. Проверка: поиск `ad-(card|grid|list|row|detail)` по `frontend/src` не находит совпадений в разметке.

## 2. Компоненты, маршрут и тестовые атрибуты

- [x] 2.1 Переименовать `frontend/src/components/AdCard.vue` → `ItemCard.vue` и `frontend/src/components/AdRow.vue` → `ItemRow.vue`, обновить импорты и использование в `HomeView.vue`, `MyAdsView.vue`, `DesignSystemGuide.vue`. Проверка: `pnpm test` в `frontend/` не падает из-за ненайденных компонентов; импорты указывают на `../components/ItemCard.vue` и `../components/ItemRow.vue`.
- [x] 2.2 Переименовать имя маршрута `ad-detail` → `item-detail` в `frontend/src/router/index.ts` и во всех ссылках `{ name: 'ad-detail' }`. Проверка: поиск `'ad-detail'` не находит совпадений.
- [x] 2.3 Обновить тестовые атрибуты, привязанные к блокам объявлений: `data-test="ad-card"` → `item-card` в `ItemCard.vue`, `data-test="my-ad"` → `my-item` в `ItemRow.vue`. Проверка: поиск `data-test="(ad-card|my-ad)"` не находит совпадений.

## 3. Тесты

- [x] 3.1 Обновить `frontend/src/assets/__tests__/styles.spec.ts`: селекторы `.ad-card`, `.ad-card__media`, `.ad-card:hover` → `.item-card`, `.item-card__media`, `.item-card:hover`. Проверка: `pnpm test` в `frontend/` проходит.
- [x] 3.2 Переименовать `frontend/src/components/__tests__/AdCard.spec.ts` → `ItemCard.spec.ts`, обновить импорт компонента и описание. Проверка: тест-файл запускается и проходит.
- [x] 3.3 Обновить ссылки на имя маршрута в `frontend/src/router/__tests__/router.spec.ts` и `frontend/src/views/__tests__/AdDetailView.spec.ts` (`ad-detail` → `item-detail`). Проверка: `pnpm test` в `frontend/` проходит.

## 4. Сквозная проверка

- [x] 4.1 Убедиться, что в `frontend/src` не осталось имён классов `ad-(card|grid|list|row|detail)`. Проверка: `Select-String -Path frontend/src -Pattern 'ad-(card|grid|list|row|detail)' -Recurse` возвращает пусто.
- [x] 4.2 Проверить, что тест ловит регрессию: временно вернуть селектор `.ad-card` в `components.css`, увидеть падение `pnpm test` в `frontend/`, вернуть `item-card`. Проверка: тест падает с `ad-card` и проходит с `item-card`.
- [x] 4.3 Визуальная приёмка на стенде (`docker compose`) с включённым блокировщиком рекламы: сетка ленты, «Мои объявления» и страница объявления отображаются, а не скрыты. Проверка: элементы видны в браузере с блокировщиком.

## Workflow follow-up

- Перед завершением запустить `dotnet test` из корня решения (бэкенд не затронут) и сообщить итог.
- Архивировать изменение после проверки; стиль коммита: «Архивировать change rename-ad-classes».
