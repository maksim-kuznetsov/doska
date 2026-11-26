# Tasks

## 1. Реквизиты и общая раскладка документа

- [x] 1.1 Создать `frontend/src/legal/details.ts` с оператором, публичным адресом сайта, контактом, ISO-датами редакции, ссылками на политику Google и функцией форматирования даты; проверить тестом `frontend/src/legal/__tests__/details.spec.ts`, что значения заполнены, даты валидны и нет заглушек `example.com`/`TODO`.
- [x] 1.2 Создать `frontend/src/components/LegalDocument.vue` (заголовок, дата редакции, слот содержимого) и стили `.legal` в `frontend/src/assets/css/layout.css`; проверить тестом `frontend/src/components/__tests__/LegalDocument.spec.ts`, что заголовок, дата и слот отображаются.

## 2. Политика конфиденциальности

- [x] 2.1 Создать `frontend/src/views/PrivacyPolicyView.vue` с разделами об операторе и контакте, данных аккаунта и Google, объявлениях и фотографиях, cookie и технических данных, целях и основаниях, Limited Use, хранении, правах и удалении; проверить тестом `frontend/src/views/__tests__/PrivacyPolicyView.spec.ts` наличие обязательных разделов, контакта и заявления о Google API Services User Data Policy.
- [x] 2.2 Добавить маршрут `/privacy` с именем `privacy` в `frontend/src/router/index.ts` без требования входа; проверить в `frontend/src/router/__tests__/router.spec.ts`, что маршрут резолвится и не имеет `requiresAuth`.

## 3. Условия использования

- [x] 3.1 Создать `frontend/src/views/TermsOfServiceView.vue` с разделами о принятии условий, аккаунте, правилах объявлений, запрещённом контенте, модерации и блокировке, правах на контент, ответственности и изменениях; проверить тестом `frontend/src/views/__tests__/TermsOfServiceView.spec.ts` наличие обязательных разделов, лимита в пять объявлений и контакта.
- [x] 3.2 Добавить маршрут `/terms` с именем `terms` в `frontend/src/router/index.ts` без требования входа; проверить в `frontend/src/router/__tests__/router.spec.ts`, что маршрут резолвится и не имеет `requiresAuth`.

## 4. Ссылки из интерфейса

- [x] 4.1 Добавить в `frontend/src/components/SiteFooter.vue` постоянные ссылки на `/privacy` и `/terms`; проверить тестом `frontend/src/components/__tests__/SiteFooter.spec.ts`, что обе ссылки отображаются и ведут на нужные адреса.
- [x] 4.2 Добавить на `frontend/src/views/LoginView.vue` и `frontend/src/views/RegisterView.vue` текст о принятии условий со ссылками на документы; проверить в `frontend/src/views/__tests__/LoginView.spec.ts` и существующем тесте регистрации, что ссылки присутствуют и не зависят от сессии.

## 5. Документация и настройка Google

- [x] 5.1 Описать в `README.md` и `deploy/DEPLOY.md` публичные URL `/privacy` и `/terms` и шаг указания их в Google Cloud Console; проверить поиском по файлам, что оба адреса и порядок действий присутствуют.
- [x] 5.2 Проверить, что тексты не содержат заглушек и незаполненных разделов, поиском по `frontend/src/views/PrivacyPolicyView.vue`, `TermsOfServiceView.vue`, `frontend/src/legal/details.ts` строк `TODO`, `example.com`, «заполнить позже».

## 6. Интеграционная проверка

- [x] 6.1 Запустить `pnpm test` в `frontend/` и убедиться, что все тесты, включая новые, проходят.
- [x] 6.2 Запустить `pnpm build` в `frontend/` и убедиться, что проверка типов `vue-tsc` и сборка Vite завершаются без ошибок.
- [x] 6.3 Запустить `pnpm preview` после сборки и открыть `/privacy` и `/terms` без входа, в том числе после перезагрузки; убедиться, что документы отображаются с датой редакции и ссылками, а прямые адреса не дают 404.

## Workflow follow-up

- Архивировать change после проверки и коммита.
- После публикации указать в Google Cloud Console политику `https://doska.xn--b1affljii0b4a.xn--p1ai:8443/privacy` и условия `https://doska.xn--b1affljii0b4a.xn--p1ai:8443/terms`, затем отправить OAuth-приложение на проверку.