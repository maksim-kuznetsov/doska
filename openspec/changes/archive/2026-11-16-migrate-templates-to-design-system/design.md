# Design

## Context

См. `proposal.md` — Why. Сейчас в проекте два слоя стилей: компоненты дизайн-системы (`web/src/assets/css/components.css`, `web/src/assets/css/layout.css`) и частные классы страниц, которые повторяют те же блоки. Представления вне `web/src/App.vue` используют именно частные классы. Токены `--ds-*` уже применяются везде, а алиасы старых имён (`--bg`, `--surface`, `--text`, `--muted`, `--border`, `--accent`, `--danger`, `--success`, `--radius`) в `web/src/assets/css/tokens.css` больше нигде не используются — это проверено поиском по `web/src/`.

Ограничения:
- Внешний вид страниц должен сохраниться: это перевод разметки, а не редизайн.
- `web/src/components/DsPasswordHints.vue` зависит от `#password-hints`, `[data-password-rule]` и классов `is-ok` / `is-bad`; эти контракты нельзя ломать.
- Существующие тесты (`web/src/**/*.spec.ts` и `tests/Doska.Tests`) проверяют тексты и ключевые классы страниц, поэтому тексты менять не нужно.

## Goals / Non-Goals

**Goals:**
- Все представления в `web/src/views/` собираются из компонентов дизайн-системы.
- Библиотека компонентов покрывает блоки, которые раньше описывались частными классами страниц.
- Частных (не `ds-`) классов в стилях не осталось, а `web/src/App.vue` подключает только `tokens`, `base`, `components`, `layout`.

**Non-Goals:**
- Изменение визуального дизайна, палитры, типографики или раскладки.
- Изменение поведения страниц, представлений, форм и маршрутов.
- Изменение конфигурации сборки Vite или добавление зависимостей.

## Decisions

### Решение 1. Расширять `components.css`, а не переносить блоки в представления как inline-стили
Повторяющиеся блоки получают новые `ds-` компоненты рядом с существующими (`ds-card`, `ds-table`, `ds-badge`). Альтернатива — оставить уникальные стили в частных классах страниц — отклонена, потому что цель изменения именно убрать второй слой.

Новые компоненты:
- `ds-filters` — панель фильтров (обёртка формы), внутри — существующие `ds-input`, `ds-select`, `ds-btn`.
- `ds-gallery` — галерея изображений, модификатор `ds-gallery--sm` для превью в формах и модерации.
- `ds-list` и `ds-list-item` — список записей (мои объявления, очередь модерации, жалобы): фото, основная часть, блок действий.
- `ds-password-hints` — список требований к паролю; состояния `is-ok` / `is-bad` переиспользуются компонентом `DsPasswordHints.vue` без изменений.

### Решение 2. Маппинг старых классов на компоненты системы
| Старый класс | Новый компонент |
| --- | --- |
| `button` | `ds-btn` |
| `link-button`, `link-button danger` | `ds-btn ds-btn--ghost` (+ `ds-btn--danger`/`ds-btn--ghost` в таблице) |
| `filters` | `ds-filters` |
| `stacked-form`, `stacked-form narrow` | `ds-form` + `ds-panel` (`ds-panel--narrow`) |
| `form-row`, `has-errors`, `error`, `help` | `ds-field`, `ds-field.is-invalid`, `ds-error`, `ds-help` |
| `ad-grid`, `ad-card`, `ad-card-link`, `ad-card-image`, `ad-card-body` | `ds-grid`, `ds-card`, `ds-card-link`, `ds-card-media` (`--placeholder`), `ds-card-body` |
| `my-ads`, `my-ad`, `my-ad-thumb`, `my-ad-main`, `my-ad-actions` | `ds-list`, `ds-list-item` (+ вложенные `ds-card-*`) |
| `status`, `status-*`, `moderation-reason` | `ds-badge`, `ds-badge--*`, текст цвета danger |
| `message`, `message-*` | `ds-alert`, `ds-alert--*` |
| `breadcrumbs` | `ds-breadcrumbs` |
| `pagination` | `ds-pagination` |
| `empty` | `ds-empty` |
| `users-table`, `row-actions` | `ds-table-wrap` + `ds-table`, `ds-row-actions` |
| `moderation-list`, `moderation-card` | `ds-list`, `ds-panel` |
| `decision-actions`, `decision-form` | `ds-actions`, `ds-actions` (форма как flex-блок) |
| `ad-detail`, `contacts`, `complaint`, `description`, `gallery` | `ds-panel`, `ds-divider`, `ds-gallery` |
| `password-hints` | `ds-password-hints` |
| `price`, `muted` | `ds-price`, `ds-muted` (перенос утилит в `components.css`) |

### Решение 3. Утилиты `price` и `muted` переезжают в `components.css`
`base.css` уже содержит `.muted` и `.price`. Чтобы не держать «полу-старые» имена, утилиты переименовываются в `ds-muted` и `ds-price` и переносятся в `components.css`; `base.css` оставляет только сброс, типографику, ссылки, контейнер и фокус. Альтернатива — оставить их в `base.css` — отклонена, потому что цель — единый источник в компонентном слое.

### Решение 4. Удаление алиасов токенов
Блок алиасов в `tokens.css` удаляется после перевода представлений на `ds-` классы: старые имена не используются нигде. Проверка перед удалением — поиск `var(--bg)`, `var(--surface)`, `var(--text)`, `var(--muted)`, `var(--border)`, `var(--accent)`, `var(--danger)`, `var(--success)`, `var(--radius)` по `web/src/`.

### Решение 5. Обновление страницы-гайда
`web/src/views/DesignSystemGuide.vue` дополняется секциями новых компонентов (`ds-filters`, `ds-gallery`, `ds-list`, `ds-password-hints`), чтобы гайд оставался полным каталогом системы.

## Risks / Trade-offs

- [Пропущенный частный класс в представлении → блок теряет оформление] → Перед удалением правил прогнать поиск старых классов по `web/src/`; после перевода убедиться, что список пуст, и только затем удалять правила.
- [Рассинхрон с собранными ассетами] → После правок выполнить `pnpm build` в каталоге web и проверить, что удалённые правила не подхватываются из сборки.
- [Регрессия внешнего вида] → Сохранять существующие размеры/отступы в новых компонентах (значения брать из частных классов), вручную проверить ключевые экраны на настольной и мобильной ширине.
- [Тесты на классы] → Проверить `web/src/**/*.spec.ts` и `tests/Doska.Tests`; при необходимости обновить ожидания, не меняя проверяемое поведение.

## Migration Plan

1. Добавить новые компоненты в `components.css` и раскладочные классы в `layout.css`.
2. Перевести представления постранично: `web/src/views/search/FeedView.vue`, `web/src/views/ads/`, `web/src/views/accounts/`, `web/src/views/moderation/`.
3. Перенести утилиты `muted`/`price` и убрать их из `base.css`.
4. Дополнить страницу-гайд.
5. Удалить частные классы страниц и алиасы из `tokens.css`.
6. Прогнать `pnpm test` в каталоге web и `pnpm build`.

Откат: изменение локально в репозитории; при проблемах достаточно вернуть правила стилей и алиасы токенов.

## Open Questions

Нет.
