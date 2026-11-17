# Tasks

## 1. Компонент «ссылка-действие» в дизайн-системе

- [x] 1.1 Добавить в `web/src/assets/css/components.css` компонент `.ds-action-link` (inline-flex, `gap: var(--ds-space-2)`, `min-height: var(--ds-control-height)`, горизонтальные отступы `var(--ds-space-2)`, `border-radius: var(--ds-radius-sm)`, вес medium, `white-space: nowrap`, `color: var(--ds-color-brand-strong)`) и иконку `.ds-action-link__icon`: 16×16, `flex: none`, `position: relative`, псевдоэлементы `::before`/`::after` рисуют «+» — полосы 2px, `border-radius: var(--ds-radius-pill)`, `background: currentColor`.
  Проверка: `Select-String -Path web\src\assets\css\components.css -Pattern "ds-action-link"` находит правило компонента, правило иконки и оба псевдоэлемента.
- [x] 1.2 Добавить в `web/src/views/DesignSystemGuide.vue` пример ссылки-действия с иконкой (подпись «Подать объявление») рядом с блоком кнопок.
  Проверка: в `web/src/views/__tests__/DesignSystemGuide.spec.ts` добавить утверждение о наличии `ds-action-link` в разметке гайда и прогнать `pnpm test` в каталоге `web`.
- [x] 1.3 Проверить компонент глазами на странице гайда `/design-system` при `ASPNETCORE_ENVIRONMENT=Development` (`docker compose up -d --build` из корня проекта): «+» стоит по центру подписи и не «плавает» по вертикали, при наведении подпись подчёркивается, при фокусе с клавиатуры виден контур.
  Проверка: ручной осмотр гайда на широкой ширине — без него нельзя утверждать, что самодельная иконка выровнена.

## 2. Действие «Подать объявление» в шапке

- [x] 2.1 Заменить в `web/src/layouts/AppLayout.vue` разметку действия на ссылку-действие: `<RouterLink class="ds-action-link" to="/ads/create"><span class="ds-action-link__icon" aria-hidden="true"></span>Подать объявление</RouterLink>`.
  Проверка: обновить в `web/src/components/__tests__/AppHeader.spec.ts` проверку шапки (сейчас она пинует прежний `<a class="ds-btn" …>`) под класс `ds-action-link`, подпись и иконку, при необходимости переименовать её под фактическое поведение; `pnpm test` в каталоге `web`.
- [x] 2.2 В `web/src/assets/css/layout.css` удалить `.ds-nav .ds-btn` и `.ds-nav .ds-btn:hover` и добавить `.ds-nav .ds-action-link { color: var(--ds-color-brand-strong); }` с комментарием, что это переопределение цвета ссылок навигации.
  Проверка: `Select-String -Path web\src\assets\css\layout.css -Pattern "ds-nav \.ds"` показывает только правило ссылки-действия; в `web/src/assets/__tests__/styles.spec.ts` добавить через существующий разбор CSS проверку, что правило `.ds-nav .ds-action-link` есть, а `.ds-nav .ds-btn` в файле не осталось.
- [x] 2.3 Убедиться, что другие места с действием «Подать объявление» не изменились: `web/src/views/MyAdsView.vue` и `web/src/views/VerifyEmailView.vue` сохраняют залитую кнопку `ds-btn`.
  Проверка: `Select-String -Path web\src\views\MyAdsView.vue,web\src\views\VerifyEmailView.vue -Pattern "Подать объявление"` показывает класс `ds-btn`, а `dotnet test` и `pnpm test` в каталоге `web` остаются зелёными.

## 3. Интеграционная проверка

- [x] 3.1 Прогнать полный набор тестов и убедиться, что шапка, гайд и статика зелёные: `dotnet test` и `pnpm test` в каталоге `web`.
- [x] 3.2 Проверить шапку в браузере на широкой и мобильной ширине после `docker compose up -d --build` из корня проекта: «Найти» и «Подать объявление» больше не спорят друг с другом, ссылка-действие не перекрашивается в цвет обычной ссылки при наведении, шапка не разъехалась.

## Workflow follow-up

- Архивировать change после проверки соответствия спеке `design-system`.
- Предложить коммит на русском в стиле «Показать подачу объявления ссылкой с иконкой».
