# Tasks

## 1. Домен: правило заполнения имени

- [x] 1.1 Добавить в `User` константу `MaxDisplayNameLength = 100` и метод `ApplyGoogleDisplayName(string? displayName, DateTimeOffset now)`: нормализует значение, игнорирует пустое и длиннее предела, меняет `DisplayName` только если он `null`, при изменении вызывает `MarkUpdated`; проверить сборкой `dotnet build`.
- [x] 1.2 Переиспользовать `User.MaxDisplayNameLength` в `ProfileService` вместо собственной константы; проверить, что существующие тесты профиля проходят (`dotnet test --filter FullyQualifiedName~UserProfile`).
- [x] 1.3 Добавить юнит-тесты в `UserGoogleDomainTests`: `ApplyGoogleDisplayName` заполняет пустое имя, не перезаписывает заданное, игнорирует пустое и слишком длинное, обновляет отметку времени только при изменении; проверить `dotnet test --filter FullyQualifiedName~UserGoogleDomainTests`.

## 2. Извлечение имени из Google

- [x] 2.1 Добавить пользовательский claim `google_name` и читать поле `name` из userinfo в `GoogleAuthentication.OnCreatingTicket`; в `OnTicketReceived` читать claim и передавать значение в сервис; проверить сборкой `dotnet build`.
- [x] 2.2 Проверить, что отсутствие `name` (claim не задан) не прерывает вход: имя передаётся как `null`; проверить в тестах сервиса из группы 3.

## 3. Сервис разрешения Google-идентичности

- [x] 3.1 Добавить параметр `string? displayName` в `GoogleAuthService.SignInAsync` и `LinkAsync`; применять `ApplyGoogleDisplayName` при создании аккаунта, автопривязке по email и явной привязке, не трогая имя при входе по уже привязанной идентичности; обновить вызовы в `GoogleAuthentication`; проверить сборкой `dotnet build`.
- [x] 3.2 Обновить существующие вызовы `SignInAsync`/`LinkAsync` в `GoogleAuthServiceTests` под новую сигнатуру; проверить `dotnet test --filter FullyQualifiedName~GoogleAuthServiceTests`.
- [x] 3.3 Добавить тесты: создание аккаунта сохраняет имя Google, автопривязка заполняет пустое имя существующего аккаунта, явная привязка заполняет пустое имя, повторный вход не перезаписывает заданное имя, отсутствие имени не мешает входу; проверить `dotnet test --filter FullyQualifiedName~GoogleAuthServiceTests`.

## 4. Итоговая проверка

- [x] 4.1 Запустить полный прогон `dotnet test` из корня решения и убедиться, что все тесты проходят; убедиться, что сценарии спеки `user-accounts` о заполнении имени покрыты.

## Workflow follow-up

- Заархивировать change после проверки и ревью.
- Предложить коммит на русском в стиле «Заполнять отображаемое имя из Google».
