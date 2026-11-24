# Tasks

## 1. Инвариантный разбор числовых полей запроса

- [x] 1.1 В `backend/src/Doska.Api/Program.cs` задать `builder.Services.Configure<RequestLocalizationOptions>` с `DefaultRequestCulture` = `CultureInfo.InvariantCulture` и `SupportedCultures`/`SupportedUICultures` только `CultureInfo.InvariantCulture`, затем добавить `app.UseRequestLocalization()` рано в конвейере (до `UseAuthentication`). Проверка: `dotnet build backend/Doska.sln --nologo` завершается с 0 ошибок.
- [x] 1.2 Расширить `backend/tests/Doska.Tests/ClassifiedAdsApiTests.cs`: подача с ценой `9999999999.99` (запись с точкой) даёт `201 Created` со значением `AdService.MaxPrice`, а цена `10000000000.99` — `400` с телом, содержащим «9 999 999 999» (сохранить существующий `Create_rejects_price_above_database_range`). Проверка: запустить собранный тест-раннер `backend/tests/Doska.Tests/bin/Debug/net10.0/Doska.Tests.exe -class Doska.Tests.ClassifiedAdsApiTests -noLogo` при поднятой БД — 0 failed.
- [x] 1.3 Добавить тест-страж, не зависящий от локали машины и CI: проверить, что разрешённая культура разбора числовых полей — инвариантная (через `IOptions<RequestLocalizationOptions>` и/или запрос с заголовком `Accept-Language: ru-RU`, после которого значение с точкой всё равно разбирается). Проверка: прогон класса с новым тестом — 0 failed; при откате 1.1 тест падает.
- [x] 1.4 Убедиться, что заголовок `Accept-Language` не меняет разбор и что тексты ответов остались прежними (жёстко заданные русские сообщения). Проверка: `Select-String -Path backend/src/Doska.Api/Program.cs -Pattern 'RequestLocalization'` и прогон класса `ClassifiedAdsApiTests` на хосте с локалью `ru-RU`.

## 2. Итоговая проверка

- [x] 2.1 Собрать решение: `dotnet build backend/Doska.sln --nologo` — 0 ошибок.
- [x] 2.2 При поднятой БД (`docker compose up -d postgres`, порт 5433) выполнить `dotnet build` и затем `backend/tests/Doska.Tests/bin/Debug/net10.0/Doska.Tests.exe -noLogo` с `ConnectionStrings__Default="Host=localhost;Port=5433;Database=doska;Username=doska;Password=doska"` — 0 failed, 0 skipped, 199+ тестов.
- [x] 2.3 Выполнить `pnpm test` в `frontend/` — 0 failed (подтверждает клиентский тест из группы 3).
- [x] 2.4 Выполнить `openspec validate fix-culture-independent-number-binding --strict` — без предупреждений.

## 3. Инвариантная передача чисел на клиенте

- [x] 3.1 Добавить в `frontend/src/api/__tests__/client.spec.ts` тест: вызов `api.ads.search` с дробными `minPrice`/`maxPrice` (например `100.5` и `9999999999.99`) формирует URL с точкой как разделителем. Проверка: `pnpm test -- src/api/__tests__/client.spec.ts` в `frontend/` проходит.
- [x] 3.2 Подтвердить отсутствие зависимости от локали браузера без правок кода: поле цены в создании и редактировании — `input[type="number"]`, отправка идёт как есть. Проверка: `Select-String -Path frontend/src/views/AdFormView.vue,frontend/src/views/AdEditView.vue -Pattern 'type="number"'` находит оба поля.

## Workflow follow-up

- Заархивировать изменение после проверки на хосте с локалью `ru-RU` и документально подтвердить, что контракт API не изменился (коды и тексты сообщений прежние).
