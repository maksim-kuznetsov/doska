# Tasks

## 1. Скрыть системную полосу прокрутки полосы разделов

- [x] 1.1 В `frontend/src/assets/css/components.css` у `.category-strip__list` добавить `scrollbar-width: none` и правило `.category-strip__list::-webkit-scrollbar { display: none }`, не меняя `overflow-x: auto`, `flex-wrap: nowrap` и `gap`. Проверка: в блоке `.category-strip__list` есть `scrollbar-width: none`, есть отдельное правило `::-webkit-scrollbar` с `display: none`.
- [x] 1.2 Дополнить `frontend/src/assets/__tests__/styles.spec.ts` проверками: тело `.category-strip__list` содержит `scrollbar-width: none`, а `ruleBody(css, '.category-strip__list::-webkit-scrollbar')` содержит `display: none`. Проверка: `pnpm test` в `frontend/` проходит.

## 2. Градиентный индикатор скрытых разделов

- [x] 2.1 Добавить `frontend/src/components/categoryStripFades.ts` с чистой функцией `computeStripFades({ scrollLeft, clientWidth, scrollWidth })`, возвращающей `{ fadeStart, fadeEnd }` (порог 1px: переполнение — `scrollWidth - clientWidth > 1`; `fadeStart` — есть прокрутка вправо от начала, `fadeEnd` — остались разделы правее). Проверка: новый `frontend/src/components/__tests__/categoryStripFades.spec.ts` покрывает случаи «помещается целиком», «переполнено у обоих краёв», «прокручено до конца», `pnpm test` в `frontend/` проходит.
- [x] 2.2 В `frontend/src/assets/css/components.css` задать маску краёв через классы `.category-strip__list--fade-start`, `.category-strip__list--fade-end` и их комбинацию: `mask-image`/`-webkit-mask-image` с прозрачным краем шириной `var(--space-4)` и непрозрачной серединой; маска применяется только при наличии соответствующего класса, поэтому без переполнения затухания нет. Проверка: правило `.category-strip__list--fade-end` содержит `mask-image` и `-webkit-mask-image`, добавлена проверка в `styles.spec.ts`, `pnpm test` в `frontend/` проходит.
- [x] 2.3 В `frontend/src/components/CategoryStrip.vue` получить ссылку на `<ul>` (`ref`), хранить флаги из `computeStripFades`, навесить классы `category-strip__list--fade-start`/`--fade-end`, пересчитывать флаги на событие `scroll`, через `ResizeObserver` на списке и после изменения пропа `sections` (`nextTick`); наблюдатель отключать в `onBeforeUnmount`. Проверка: тест в `frontend/src/components/__tests__/CategoryStrip.spec.ts` с заглушкой `ResizeObserver` и заданными через `Object.defineProperty` значениями `clientWidth`/`scrollWidth` на элементе списка показывает, что после `scroll` появляется нужный класс-затухание, а при отсутствии переполнения классы не выставляются; `pnpm test` в `frontend/` проходит.
- [x] 2.4 Убедиться, что страница-гайд (`frontend/src/views/DesignSystemGuide.vue`) продолжает показывать полосу разделов без правок разметки. Проверка: `pnpm test` в `frontend/` проходит, типовая проверка `vue-tsc` не ругается (`pnpm build` или `pnpm test`).

## 3. Интеграционная проверка

- [x] 3.1 Прогнать тесты интерфейса: `pnpm test` в `frontend/`; убедиться, что проходят `styles.spec.ts`, `CategoryStrip.spec.ts` и новый `categoryStripFades.spec.ts`. Проверка: команда завершается без падений.
- [x] 3.2 На поднятом стенде (`pnpm dev` в `frontend/` или `docker compose up -d` из корня) на настольной ширине проверить полосу разделов с большим числом разделов: системная полоса прокрутки не видна, у правого края есть затухание, после прокрутки до конца затухание у правого края исчезает и появляется у левого, а при достаточной ширине затухания нет ни у одного края. Проверка: ручной осмотр; страница `/design-system` выглядит так же.
- [x] 3.3 Проверить доступность: пройти по ссылкам полосы клавишей Tab и убедиться, что браузер подкручивает полосу к активной ссылке, а порядок и переходы по разделам не изменились. Проверка: ручной осмотр.

## Workflow follow-up

- Архивировать изменение после проверки (стиль коммита: «Архивировать change fix-category-strip-scrollbar»).
