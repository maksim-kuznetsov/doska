# Tasks

## 1. Расширение библиотеки компонентов

- [x] 1.1 Добавить в `web/src/assets/css/components.css` компоненты `ds-filters`, `ds-gallery` (+ `--sm`), `ds-list`/`ds-list-item`, `ds-password-hints`, `ds-actions`, `ds-divider`, `ds-row-actions`, перенеся значения отступов и размеров из частных классов страниц; проверить в браузере, что блоки отображаются как прежде.
- [x] 1.2 Добавить утилиты `ds-muted` и `ds-price` в `web/src/assets/css/components.css`; проверить на ленте и карточке, что цвет и размер совпадают с прежними `muted`/`price`.
- [x] 1.3 Добавить в `web/src/views/DesignSystemGuide.vue` секции с примерами `ds-filters`, `ds-gallery`, `ds-list`, `ds-password-hints`; проверить открытие маршрута `/design-system` в окружении Development.

## 2. Лента и страницы объявлений

- [x] 2.1 Перевести `web/src/views/search/FeedView.vue` на `ds-filters`, `ds-grid`, `ds-card`, `ds-badge`/`ds-pagination`/`ds-empty`, `ds-muted`, `ds-price`; проверить прохождение `pnpm test` в каталоге web (спецификация `web/src/views/search/FeedView.spec.ts`).
- [x] 2.2 Перевести `web/src/views/ads/AdDetailView.vue` на `ds-breadcrumbs`, `ds-panel`, `ds-badge`, `ds-gallery`, `ds-divider`, `ds-btn--danger`; проверить прохождение `pnpm test` в каталоге web (спецификация `web/src/views/ads/AdDetailView.spec.ts`).
- [x] 2.3 Перевести `web/src/views/ads/AdFormView.vue` на `ds-form`/`ds-panel`, `ds-field` (+ `is-invalid`), `ds-error`, `ds-help`, `ds-gallery--sm`, `ds-btn`; проверить прохождение `pnpm test` в каталоге web (спецификация `web/src/views/ads/AdFormView.spec.ts`).
- [x] 2.4 Перевести `web/src/views/ads/MyAdsView.vue` на `ds-list`/`ds-list-item`, `ds-badge`, `ds-btn`/`ds-btn--ghost`, `ds-empty`, `ds-muted`; проверить прохождение `pnpm test` в каталоге web (спецификация `web/src/views/ads/MyAdsView.spec.ts`).

## 3. Страницы аккаунтов

- [x] 3.1 Перевести `web/src/views/accounts/LoginView.vue` и `web/src/views/accounts/RegisterView.vue` на `ds-form`/`ds-panel--narrow`, `ds-field`, `ds-error`, `ds-help`, `ds-password-hints`, `ds-btn`; сохранить контракты компонента подсказок (`#password-hints`, `[data-password-rule]`, `is-ok`/`is-bad`); проверить `pnpm test` в каталоге web (спецификация `web/src/views/accounts/RegisterView.spec.ts`).
- [x] 3.2 Перевести `web/src/views/accounts/PasswordResetFormView.vue`, `PasswordResetConfirmView.vue`, `PasswordResetDoneView.vue`, `PasswordResetCompleteView.vue`, `VerifyEmailView.vue` на `ds-form`/`ds-panel`, `ds-alert`, `ds-btn`; проверить `pnpm test` в каталоге web (спецификация `web/src/views/accounts/PasswordResetFormView.spec.ts`).
- [x] 3.3 Проверить в браузере регистрацию: подсказки к паролю меняют состояние по мере ввода и блокируются при несовпадении.

## 4. Страницы модерации

- [x] 4.1 Перевести `web/src/views/moderation/ModerationQueueView.vue` на `ds-list`/`ds-panel`, `ds-gallery--sm`, `ds-actions`, `ds-btn` (+ `--success`/`--danger`), `ds-empty`; проверить `pnpm test` в каталоге web (спецификация `web/src/views/moderation/ModerationQueueView.spec.ts`).
- [x] 4.2 Перевести `web/src/views/moderation/ComplaintsView.vue` на `ds-list`/`ds-panel`, `ds-badge`, `ds-actions`, `ds-btn`; проверить `pnpm test` в каталоге web (спецификация `web/src/views/moderation/ComplaintsView.spec.ts`).
- [x] 4.3 Перевести `web/src/views/moderation/ModerationUsersView.vue` на `ds-filters`, `ds-table-wrap`/`ds-table`, `ds-row-actions`, `ds-btn--ghost`; проверить `pnpm test` в каталоге web (спецификация `web/src/views/moderation/ModerationUsersView.spec.ts`).

## 5. Удаление частных классов и алиасов

- [x] 5.1 Убедиться, что поиск старых классов (`button`, `link-button`, `filters`, `stacked-form`, `form-row`, `ad-card`, `ad-grid`, `status`, `message`, `breadcrumbs`, `pagination`, `empty`, `users-table`, `my-ad`, `moderation`, `gallery`, `password-hints`, `ad-detail`, `decision-`, `row-actions`, `muted`, `price`) по `web/src/views/` и `web/src/components/` не находит совпадений.
- [x] 5.2 Удалить частные классы страниц из `web/src/assets/css/`; убрать `.muted` и `.price` из `web/src/assets/css/base.css`; проверить `pnpm build` в каталоге web без ошибок.
- [x] 5.3 Удалить алиасы старых имён из `web/src/assets/css/tokens.css` (убедившись, что `var(--bg)`, `var(--surface)`, `var(--text)`, `var(--muted)`, `var(--border)`, `var(--accent)`, `var(--danger)`, `var(--success)`, `var(--radius)` не используются); проверить, что страницы открываются.
- [x] 5.4 Обновить `README.md`: убрать упоминание частных классов страниц, описать порядок подключения стилей без слоя совместимости; проверить, что описанные шаги открывают страницы и гайд.

## 6. Интеграционная проверка

- [x] 6.1 Прогнать полный набор `pnpm test` в каталоге web и `dotnet test` и убедиться, что регрессий нет.
- [x] 6.2 Вручную проверить ключевые экраны (лента, страница объявления, форма подачи, мои объявления, вход/регистрация, сброс пароля, очередь модерации, жалобы, пользователи) на настольной и мобильной ширине; зафиксировать результат.

## Workflow follow-up

- Заархивировать изменение после приёмки и убедиться, что основная спецификация `design-system` обновлена (требование обратной совместимости удалено, добавлены требования единого оформления и компонентов фильтров/списков/медиа).
