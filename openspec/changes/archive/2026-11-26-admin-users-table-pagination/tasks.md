# Tasks

## 1. Постраничный список (бэкенд)

- [x] 1.1 В `Doska.Application/Accounts/UserService.cs` заменить `GetListAsync` на постраничную: принимать `page` и `pageSize`, нормализовать их (`DefaultPageSize = 20`, `MaxPageSize = 100`, нижние границы) и возвращать `PagedResult<AdminUserView>` с `TotalCount`. Проверка: проект собирается (`dotnet build`), логика покрыта тестами из 1.3.
- [x] 1.2 В `Doska.Api/Controllers/AdminUsersController.cs` принимать `[FromQuery] int? page, int? pageSize` и возвращать `PagedResultResponse<AdminUserResponse>` (тип уже есть в `AdSearchContracts.cs`). Проверка: `dotnet build`; ответ эндпоинта содержит `items`, `totalCount`, `page`, `pageSize`.
- [x] 1.3 Обновить `tests/Doska.Tests/UserAccountApiTests.cs`: читать страницу вместо массива (обёртка `AdminUserPage` с `Items`, `TotalCount`, `Page`, `PageSize`) в `FindUserAsync` и в проверках списка; добавить тест, что `pageSize` сверх максимума ограничивается, а `page` за пределами диапазона даёт пустой `items` с тем же `totalCount`. Проверка: `dotnet test` из `backend` — тесты бэкенда проходят.

## 2. Оформление таблицы (дизайн-система)

- [x] 2.1 Добавить в `frontend/src/assets/css/components.css` блок стилей таблицы данных (`.data-table` и обёртка с `overflow-x: auto`): заголовок, разделители строк, выравнивание ячеек, отступы. Проверка: `pnpm build` в `frontend/` собирается без ошибок.
- [x] 2.2 Добавить пример таблицы на страницу-гайд `frontend/src/views/DesignSystemGuide.vue` и обновить `DesignSystemGuide.spec.ts` (проверка наличия таблицы). Проверка: `pnpm test` в `frontend/`.

## 3. Таблица и пагинация (интерфейс)

- [x] 3.1 В `frontend/src/api/client.ts` заменить `adminUsers.list` на приём `page`/`pageSize` и возврат `PagedResult<AdminUserView>`. Проверка: `pnpm build` собирается; тип соответствует ответу API.
- [x] 3.2 Переписать `frontend/src/views/AdminUsersView.vue`: таблица со столбцами (email, имя, роль, доверие, подтверждение email, статус, контакты, «Редактировать»), загрузка страницы, вывод `Pagination` при нескольких страницах, `EmptyState` при пустой выдаче. Обновить `AdminUsersView.spec.ts` (таблица, ссылки, переход на страницу 2). Проверка: `pnpm test` в `frontend/`.

## 4. Интеграционная проверка

- [x] 4.1 Прогнать `dotnet test` (из `backend`) и `pnpm test` (из `frontend/`) — оба набора зелёные. Проверка: команды завершаются успешно, без падающих тестов.
- [x] 4.2 Проверить на живом стенде (`docker compose`): таблица показывает свойства и ссылку, пагинация листает страницы и показывает общее число. Проверка: описанные действия дают ожидаемый результат.
