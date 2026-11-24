# Tasks

## 1. Резолвер пути

- [x] 1.1 Добавить `PathResolver.Resolve(configured, contentRoot)` в `Doska.Infrastructure` и покрыть тестом на абсолютный и относительный путь: `dotnet test backend/Doska.sln`.
- [x] 1.2 Заменить пять копий резолвера в `MediaMaintenanceService`, `LocalAdPhotoStorage`, `MediaController`, `FileEmailSender`, `FileEmailOutbox`; `dotnet test backend/Doska.sln`.

## 2. Ошибки и сессия

- [x] 2.1 Заменить магические числа в `ServiceExceptionHandler` на сопоставление `ServiceErrorKind` → HTTP-код; проверить тестами кодов ответов: `dotnet test backend/Doska.sln`.
- [x] 2.2 Свести построение claim'ов и принципала в `ClaimsPrincipalExtensions` и использовать в `AuthController` и `Program`; `dotnet test backend/Doska.sln` — тесты аутентификации зелёные.
- [x] 2.3 Свести проверку пользователя в `OnValidatePrincipal` с `AuthService.GetAsync`, не увеличивая число запросов к базе; `dotnet test backend/Doska.sln`.

## 3. Проверка

- [x] 3.1 Сборка и полный прогон: `dotnet build backend/Doska.sln` и `dotnet test backend/Doska.sln` завершаются успешно.

## Workflow follow-up

- Архивировать изменение после проверок.
