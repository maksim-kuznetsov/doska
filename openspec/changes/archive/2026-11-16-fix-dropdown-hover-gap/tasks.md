# Tasks

## 1. Контейнер раскрытия в стилях

- [x] 1.1 В `web/src/assets/css/components.css` перенести позиционирование с `.ds-menu__panel` на новый `.ds-menu__dropdown`: `position: absolute; top: 100%; left: 0; z-index: 20; display: none; padding-top: var(--ds-space-2)`. Панель оставить блоком без `position/top/left/z-index`. Показ по `.ds-menu.is-open .ds-menu__dropdown { display: block; }`, у варианта пользователя — `.ds-menu--user .ds-menu__dropdown { right: 0; left: auto; }`. Проверка: в `web/src/assets/css/components.css` нет правил позиционирования панели (`top: calc(100% ...)`, `z-index`), а зазор задаётся только `padding-top` контейнера.
- [x] 1.2 Убедиться, что оформление панели не изменилось: рамка, радиус, фон, тень, `min-width`, `max-height`, `overflow-y` остаются на `.ds-menu__panel` и используют только токены. Проверка: в правилах панели нет «сырых» цветов и размеров, видны только `var(--ds-*)`.

## 2. Разметка меню в компонентах

- [x] 2.1 В `web/src/components/CategoryBurger.vue` обернуть `<nav class="ds-menu__panel" :id="menuId">` в `<div class="ds-menu__dropdown">`; `id` и `aria-label` оставить на `<nav>`, `aria-controls` кнопки не менять. Проверка: разметка компонента содержит `ds-menu__dropdown`, а `id` панели по-прежнему на `nav`.
- [x] 2.2 Добавить такую же обёртку в меню пользователя в `web/src/components/AppHeader.vue` и в примере меню в `web/src/views/DesignSystemGuide.vue` (перед вложением проверить поиском `ds-menu__panel` по репозиторию, что других мест нет). Проверка: поиском по `web/src` все вхождения `ds-menu__panel` находятся внутри `ds-menu__dropdown`.
- [x] 2.3 Обновить `web/src/components/__tests__/AppHeader.spec.ts`: добавить проверку, что меню категорий и меню пользователя содержат контейнер раскрытия, что `id` панели и `aria-controls` сохранены и что `data-ds-menu` остался на корне; в `web/src/views/__tests__/DesignSystemGuide.spec.ts` — проверку контейнера в примере гайда. Проверка: `pnpm test -- AppHeader DesignSystemGuide` в каталоге `web` проходит.

## 3. Интеграционная проверка

- [x] 3.1 Вручную в браузере в окружении `Development` (маршрут `/design-system` и шапка): для бургера категорий и для меню пользователя навести и медленно вести курсор вниз и по диагонали к панели — меню остаётся раскрытым на всём пути; уход курсора в сторону, клик вне и Escape закрывают меню; нажатие по-прежнему переключает и закрепляет меню. Проверка: перечисленные наблюдения подтверждены для обоих меню.
- [x] 3.2 Прогнать полный набор тестов: `dotnet test` и `pnpm test` в каталоге `web` — все тесты проходят.

## Workflow follow-up

- Архивировать change после проверки результата (по правилам проекта — сразу после архивации предложить коммит).
