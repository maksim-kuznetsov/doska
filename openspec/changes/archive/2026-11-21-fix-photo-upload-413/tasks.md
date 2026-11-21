# Tasks

## 1. Лимит загрузки на веб-сервере и в API

- [x] 1.1 Добавить `client_max_body_size 32m` в `location /api/` файла `deploy/nginx.conf` и комментарий с расчётом (5 фотографий по 5 МБ плюс multipart-обвязка). Проверка: `docker run --rm --add-host api:127.0.0.1 -v "${PWD}/deploy/nginx.conf:/etc/nginx/conf.d/default.conf:ro" nginx:1.27-alpine nginx -t` завершается успешно, а `Select-String -Path deploy/nginx.conf -Pattern 'client_max_body_size\s+32m'` находит директиву.
- [x] 1.2 Задать Kestrel `MaxRequestBodySize = 32 МиБ` в `backend/src/Doska.Api/Program.cs` рядом с настройкой `FormOptions`, чтобы сервер приложений не отдавал `413` раньше прикладной проверки. Проверка: `dotnet build backend/Doska.sln --configuration Debug` проходит без ошибок.
- [x] 1.3 Добавить в `backend/tests/Doska.Tests/ClassifiedAdsApiTests.cs` тест подачи объявления с фотографией 3,4 МБ, ожидающий `201 Created`. Проверка: `dotnet test backend/Doska.sln --filter "FullyQualifiedName~ClassifiedAdsApiTests"` проходит, новый тест виден в результате.

## 2. Понятная ошибка в клиенте API

- [x] 2.1 Переписать `parseResponse` в `frontend/src/api/client.ts`: читать тело как текст, разбирать JSON в `try/catch`, при не-JSON ответе формировать сообщение по статусу, а для `413` показывать текст о слишком больших фотографиях и допустимом размере 5 МБ на файл. Проверка: `pnpm build` в `frontend/` проходит, `ApiError` для JSON-ответов сохраняет прежнее сообщение.
- [x] 2.2 Добавить в `frontend/src/api/__tests__/client.spec.ts` тесты: `413` с HTML-телом даёт `ApiError` со статусом `413` и понятным сообщением; не-JSON ответ `502` даёт `ApiError` с сообщением по статусу, а не ошибку разбора JSON. Проверка: `pnpm test -- src/api/__tests__/client.spec.ts` в `frontend/` проходит.

## 3. Проверка конфигурации в CI

- [x] 3.1 Дополнить job `deployment-scripts` в `.github/workflows/ci.yml`: проверять наличие `client_max_body_size` не меньше 32 МБ и запускать `nginx -t` в контейнере `nginx:1.27-alpine` с `--add-host api:127.0.0.1`. Проверка: команды шага успешно выполняются локально в Docker, а при удалении директивы проверка завершается ошибкой.
- [x] 3.2 Убедиться, что шаг CI использует тот же файл `deploy/nginx.conf`, который копируется в образ `frontend/Dockerfile`, и что сообщение об ошибке называет ожидаемую директиву. Проверка: `Select-String -Path .github/workflows/ci.yml,frontend/Dockerfile -Pattern 'deploy/nginx.conf|client_max_body_size'` показывает согласованные пути.

## 4. Документация для внешнего прокси

- [x] 4.1 Добавить в `deploy/DEPLOY.md` раздел о загрузке фотографий: директива `client_max_body_size 32m` для внешнего nginx, проверка `nginx -t`, перезагрузка, проверка CDN/WAF и ограничения Caddy; включить проверку загрузки фото 3–5 МБ. Проверка: `Select-String -Path deploy/DEPLOY.md -Pattern 'client_max_body_size 32m','413'` находит оба фрагмента, инструкция согласована с `deploy/nginx.conf`.
- [x] 4.2 Добавить в таблицу диагностики `deploy/DEPLOY.md` строку для `413` при загрузке фотографий с указанием, как определить слой (заголовок `Server`, журнал nginx `client intended to send too large body`, CDN/WAF). Проверка: строка присутствует в таблице и содержит команды или признаки для каждого слоя.

## 5. Итоговая проверка

- [x] 5.1 Выполнить `dotnet test backend/Doska.sln` из корня проекта и `pnpm test` в `frontend/`; оба набора тестов проходят.
- [x] 5.2 Выполнить `pnpm build` в `frontend/`; сборка интерфейса завершается успешно.
- [x] 5.3 Выполнить `openspec validate fix-photo-upload-413 --strict`; проверка проходит без предупреждений.
- [x] 5.4 Сделать проверку `frontend/src/__tests__/deployment.spec.ts` независимой от переводов строк (CRLF и LF). Проверка: `pnpm test -- src/__tests__/deployment.spec.ts` в `frontend/` проходит в рабочей копии с CRLF.

## Workflow follow-up

- После выпуска задать `client_max_body_size 32m` на внешнем прокси/nginx, выполнить `nginx -t` и перезагрузить прокси.
- Проверить на production подачу объявления с фотографией 3,4 МБ и около 5 МБ; ожидается `201 Created`, в журнале nginx нет `client intended to send too large body`.
- Если `413` сохраняется, определить следующий слой по заголовку `Server` (CDN/WAF) и скорректировать его лимит.
- Заархивировать изменение после успешной проверки на production.