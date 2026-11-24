# Tasks

## 1. Токены и мёртвый код

- [x] 1.1 Добавить в `tokens.css` семантические токены цветов, равные текущим литералам (`components.css:598,603,618,640,652,658,1258`); проверить `pnpm --dir frontend test` (`styles.spec.ts`).
- [x] 1.2 Заменить литералы цветов и фокус-ринг (`components.css:258`) на токены; `pnpm --dir frontend test`.
- [x] 1.3 Удалить неиспользуемые `--opacity-muted`, `--breakpoint-mobile`, `--breakpoint-tablet` и мёртвый класс `.divider`; `pnpm --dir frontend test`.

## 2. Размеры и компоненты

- [x] 2.1 Заменить `rem`-литералы на spacing-токены там, где значения совпадают; `pnpm --dir frontend test`.
- [x] 2.2 Перенести `.outbox-content` из scoped-`<style>` `AdminOutboxView.vue` в `components.css`; `pnpm --dir frontend test` (`AdminOutboxView.spec.ts`).
- [x] 2.3 Перевести поле в `ModerationCard.vue` на компонент `TextAreaField`; `pnpm --dir frontend test`.

## 3. Проверка

- [x] 3.1 Собрать интерфейс (`pnpm --dir frontend build`) и вручную сверить затронутые страницы на отсутствие визуальных изменений.

## Workflow follow-up

- Архивировать изменение после проверок.
