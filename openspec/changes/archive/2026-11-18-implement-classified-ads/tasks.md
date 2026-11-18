# Tasks

## 1. Домен

- [x] 1.1 Добавить в `Ad` методы `UpdateDetails`, `MarkSold`, `ReturnToSale`, `Archive`, `Renew` с проверкой допустимых переходов. Проверка: доменные тесты.
- [x] 1.2 Проверить, что редактирование непроверенным автором переводит активное объявление на проверку, а доверенным — сохраняет статус. Проверка: доменные тесты.

## 2. Фотографии и хранилище

- [x] 2.1 Добавить `IAdPhotoStorage`, `PhotoUpload`, `StoredPhoto` в Application и `LocalAdPhotoStorage` с ImageSharp в Infrastructure (оригинал + превью). Проверка: `dotnet build`.
- [x] 2.2 Реализовать проверки 1–5 фото, размера до 5 МБ и форматов JPEG/PNG/WebP в сценарии подачи. Проверка: API-тесты лимитов.
- [x] 2.3 Сохранять `AdPhoto` с оригиналом и превью, размерами и порядком; отдавать `thumbnailUrl` в выдаче и «Моих объявлениях». Проверка: API-тест.

## 3. Сценарии автора

- [x] 3.1 `AdService.UpdateAsync` и `DeleteAsync` с проверкой владения и правилом повторной модерации. Проверка: API-тесты 200/403.
- [x] 3.2 `MarkSoldAsync`, `ReturnToSaleAsync`, `RenewAsync` по правилам статусов и доверия. Проверка: API-тесты.
- [x] 3.3 `ArchiveExpiredAsync` и фоновый сервис автоархива через 7 дней. Проверка: тест автоархива.

## 4. API

- [x] 4.1 Перевести `POST /api/ads` на multipart с фотографиями; добавить `GET /api/ads/{id}` для автора, `PUT`, `DELETE`, `sold`, `return-to-sale`, `renew`. Проверка: `dotnet build`.
- [x] 4.2 Контроллер медиа с защитой от обхода пути и `X-Accel-Redirect` в `Production`. Проверка: API-тест отдачи файла.
- [x] 4.3 Обновить контракты `AdSummary`/`AdCard` фотографиями и `thumbnailUrl`. Проверка: API-тесты.

## 5. Интерфейс

- [x] 5.1 Форма подачи: выбор фото, превью, счётчик, лимиты, отправка `FormData`. Проверка: `pnpm build`.
- [x] 5.2 «Мои объявления»: редактирование, удаление, «Продано», возврат, продление. Проверка: `pnpm build`.
- [x] 5.3 Страница редактирования и карточки с фото/плейсхолдером. Проверка: `pnpm build`.
- [x] 5.4 Тест клиентских правил выбора фото (лимит и счётчик). Проверка: `pnpm test`.

## 6. Проверка

- [x] 6.1 `dotnet build backend/Doska.sln`, тесты бэкенда, `pnpm --dir frontend test`, `pnpm --dir frontend build`.
- [x] 6.2 `openspec validate implement-classified-ads`.