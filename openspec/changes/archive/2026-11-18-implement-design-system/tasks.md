# Tasks

## 1. Токены и стили

- [x] 1.1 Создать `frontend/src/assets/css/tokens.css` с палитрой, типографикой, отступами, радиусами, тенями, состояниями и точками перелома. Проверка: `pnpm --dir frontend build`.
- [x] 1.2 Добавить `base.css`, `components.css`, `layout.css`; подключить четыре слоя в `main.ts` и удалить `style.css`. Проверка: в проекте нет `scoped`-стилей и классов вне дизайн-системы.
- [x] 1.3 Реализовать классы кнопок, полей, форм, карточек, бейджей, сообщений, навигации, фильтров, меню и оболочки. Проверка: страница-гайд и `pnpm --dir frontend build`.

## 2. Компоненты

- [x] 2.1 Добавить `AppButton`, `ActionLink`, `AppMessage`, `StatusBadge`, `EmptyState`, `Breadcrumbs`, `Pagination`, `CharCounter`, `FormErrors`. Проверка: тесты компонентов.
- [x] 2.2 Добавить поля формы: `AppField`, `TextField`, `TextAreaField`, `SelectField`, `SubsectionSelect`, `FormSection`, `FormGrid`, `FormGridItem`, `FormLayout`. Проверка: тесты и `pnpm --dir frontend build`.
- [x] 2.3 Добавить `PhotoUploader` с превью, счётчиком, удалением и подсказкой о лимитах. Проверка: тест компонента.
- [x] 2.4 Добавить `AdCard`, `AdRow`, `ImageGallery`, `ModerationCard`. Проверка: тесты компонентов.
- [x] 2.5 Добавить `SearchBar`, `PriceRange`, `FilterPanel`, `ResultsBar`. Проверка: тест диапазона цены и строки результатов.
- [x] 2.6 Добавить `DropdownMenu`, перевести `CategoryMenu` и создать `UserMenu`. Проверка: тест меню (наведение, нажатие, `aria-expanded`, `Escape`).

## 3. Оболочка и страницы

- [x] 3.1 Создать `SiteHeader`, `SiteFooter` и перевести `DefaultLayout`: бургер категорий, поиск, «Подать объявление», меню пользователя, подвал. Проверка: `pnpm --dir frontend build`.
- [x] 3.2 Перевести `HomeView` на панель фильтров, строку результатов, карточки, пагинацию и пустое состояние. Проверка: `pnpm --dir frontend test`.
- [x] 3.3 Перевести `MyAdsView`, `AdFormView`, `AdEditView`, `ModerationQueueView`, `ComplaintsQueueView`, `AdminCategoriesView`, `LoginView`, `RegisterView`, `ConfirmEmailView` на компоненты без собственных стилей. Проверка: `pnpm --dir frontend build`.
- [x] 3.4 Создать `DesignSystemGuide.vue` и маршрут `/design-system` только в `Development`. Проверка: тест маршрута и `pnpm --dir frontend build`.

## 4. Проверка

- [x] 4.1 `dotnet build backend/Doska.sln`, тесты бэкенда, `pnpm --dir frontend test`, `pnpm --dir frontend build`.
- [x] 4.2 Убедиться, что представления не содержат `<style>` и не используют классы оформления вне дизайн-системы.
- [x] 4.3 `openspec validate implement-design-system`.