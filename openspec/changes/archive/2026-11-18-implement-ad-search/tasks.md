# Tasks

## 1. Сценарий поиска

- [x] 1.1 Добавить в `Doska.Application/Ads` модель `AdSearchQuery`, `AdSort`, `AdCard` и `PagedResult<T>`. Проверка: `dotnet build backend/Doska.sln`.
- [x] 1.2 Реализовать `AdSearchService.SearchAsync`: только активные объявления незаблокированных авторов, текст по заголовку и описанию без учёта регистра. Проверка: API-тест поиска.
- [x] 1.3 Добавить фильтры по разделу/подразделу, городу и диапазону цены, их комбинацию и проверку `minPrice > maxPrice`. Проверка: API-тест фильтров.
- [x] 1.4 Добавить сортировку `newest`/`price_asc`/`price_desc` и пагинацию с `totalCount`. Проверка: API-тест сортировки и страниц.

## 2. API

- [x] 2.1 Добавить анонимный `GET /api/ads` с query-параметрами и контрактами ответа (`AdCardResponse`, `PagedResultResponse`). Проверка: анонимный запрос возвращает 200.
- [x] 2.2 Зарегистрировать `AdSearchService` в DI и проверить коды ошибок (`400` при неверном диапазоне). Проверка: API-тест 400.

## 3. Справочник разделов

- [x] 3.1 Добавить `CatalogService.GetSectionsAsync` и `GET /api/catalog/sections` (разделы с активными подразделами). Проверка: API-тест справочника.

## 4. Интерфейс ленты

- [x] 4.1 Добавить в api-клиент типы `AdCard`, `PagedResult`, `SectionOption` и методы `ads.search`, `catalog.sections`. Проверка: `pnpm --dir frontend build`.
- [x] 4.2 Вынести правила фильтров в `frontend/src/api/search.ts` (`readFilters`, `toRouteQuery`, `toSearchParams`). Проверка: `pnpm --dir frontend test`.
- [x] 4.3 Переделать `HomeView.vue` в публичную ленту: поиск, раздел/подраздел, город, цена, сортировка, карточки, пустой результат, пагинация с сохранением фильтров. Проверка: `pnpm --dir frontend build`.

## 5. Тесты

- [x] 5.1 API-тесты: видимость только активных, поиск в заголовке/описании без учёта регистра, фильтр раздела и подраздела, город и цена, сортировка, пагинация. Проверка: `dotnet test` (или прямой запуск `Doska.Tests.exe`).
- [x] 5.2 Тесты клиентских правил фильтров и query-параметров. Проверка: `pnpm --dir frontend test`.

## 6. Итоговая проверка

- [x] 6.1 `dotnet build backend/Doska.sln`, тесты бэкенда, `pnpm --dir frontend test`, `pnpm --dir frontend build` — без ошибок и падений.
- [x] 6.2 `openspec validate implement-ad-search` — change корректен.