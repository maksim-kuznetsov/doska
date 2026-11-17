# Tasks

- [x] 1.1 В `web/src/assets/css/layout.css` заменить `flex: 1 1 320px` на `flex: 1 1 auto` в правиле `.ds-search`; проверить `Select-String -Path web\src\assets\css\layout.css -Pattern "ds-search"` и убедиться, что база больше не задана в пикселях
- [x] 1.2 Проверить, что `web/src/assets/css/components.css` задаёт строке поиска ограничение ширины только на настольной ширине; правок не требуется, но `Select-String -Path web\src\assets\css\components.css -Pattern "ds-search"` не должен показывать жёстких пиксельных ширин
- [x] 1.3 В мобильном блоке `@media (max-width: 768px)` файла `layout.css` добавить строке поиска `flex: 0 1 auto` и `max-width: none`; проверить, что `flex: 1 1 320px` в файле не осталось (`Select-String -Path web\src\assets\css\layout.css -Pattern "flex: 1 1 320px"`)
- [x] 1.4 Добавить в `web/src/assets/__tests__/styles.spec.ts` (Vitest) статическую проверку правила: нет `flex: 1 1 320px`, есть `flex: 1 1 auto`, в мобильном блоке есть `flex: 0 1 auto`
- [x] 1.5 Прогнать `pnpm test` в каталоге `web` и убедиться, что проверка проходит
- [x] 2.1 Собрать и поднять окружение (`docker compose up -d --build`) и проверить в браузере при ширине 480px и 768px: строка поиска не выше строки бренда, шапка не разъезжается, поиск не перекрывает бургер и меню
- [x] 2.2 Проверить настольную ширину: `.ds-search` по-прежнему занимает свободное место, вид шапки не изменился

## Workflow follow-up

- Архивировать change после проверки.
- Предложить коммит по-русски в стиле «Починить высоту строки поиска в шапке на мобильной ширине».
