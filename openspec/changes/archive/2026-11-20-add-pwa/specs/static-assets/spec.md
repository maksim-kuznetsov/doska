# Spec Delta

## ADDED Requirements

### Requirement: Служебные файлы PWA не кешируются

Веб-сервер nginx SHALL отдавать web-манифест и service worker с заголовком `Cache-Control: no-cache`, чтобы браузер получал актуальные версии после развёртывания. Service worker SHALL быть доступен из корня сайта, чтобы управлять всеми страницами приложения.

#### Scenario: Запрос service worker

- **WHEN** браузер запрашивает файл service worker
- **THEN** ответ содержит `Cache-Control: no-cache`, а файл доступен из корня сайта

#### Scenario: Запрос web-манифеста

- **WHEN** браузер запрашивает web-манифест
- **THEN** ответ содержит `Cache-Control: no-cache` и тип содержимого web-манифеста

#### Scenario: Развёртывание новой версии

- **WHEN** после развёртывания браузер повторно запрашивает service worker
- **THEN** он получает новую версию файла, а не закешированную копию