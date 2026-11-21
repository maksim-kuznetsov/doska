import { readFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const css = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), '../css/components.css'),
  'utf8',
)

const layoutCss = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), '../css/layout.css'),
  'utf8',
)

/** Возвращает тело CSS-правила по селектору (`selector { ... }`). */
function ruleBody(source: string, selector: string): string {
  const index = source.indexOf(`${selector} {`)
  if (index === -1) {
    throw new Error(`Правило ${selector} не найдено`)
  }

  const start = source.indexOf('{', index)
  const end = source.indexOf('}', start)

  return source.slice(start + 1, end)
}

/** Возвращает тело медиазапроса с учётом вложенных фигурных скобок. */
function mediaBody(source: string, query: string): string {
  const index = source.indexOf(`${query} {`)
  if (index === -1) {
    throw new Error(`Медиазапрос ${query} не найден`)
  }

  const start = source.indexOf('{', index)
  let depth = 0

  for (let position = start; position < source.length; position += 1) {
    if (source[position] === '{') {
      depth += 1
    } else if (source[position] === '}') {
      depth -= 1
      if (depth === 0) {
        return source.slice(start + 1, position)
      }
    }
  }

  throw new Error(`Не найден конец медиазапроса ${query}`)
}

/** Делит значение CSS-шортката на части по пробелам вне скобок. */
function shorthandValues(value: string): string[] {
  const parts: string[] = []
  let current = ''
  let depth = 0

  for (const char of value) {
    if (char === '(') {
      depth += 1
    } else if (char === ')') {
      depth -= 1
    }

    if (/\s/.test(char) && depth === 0) {
      if (current.length > 0) {
        parts.push(current)
        current = ''
      }
      continue
    }

    current += char
  }

  if (current.length > 0) {
    parts.push(current)
  }

  return parts
}

/** Возвращает значения шортката padding правила в порядке top/right/bottom/left. */
function paddingValues(rule: string): string[] {
  const match = rule.match(/padding:\s*([\s\S]*?);/)
  if (!match) {
    throw new Error('Не найдено свойство padding')
  }

  return shorthandValues(match[1])
}


describe('Сетка карточек ленты', () => {
  it('карточка ограничивает минимальную ширину grid-элемента', () => {
    expect(ruleBody(css, '.item-card')).toContain('min-width: 0')
  })

  it('медиа-блок карточки удерживает ширину и обрезает фото', () => {
    const media = ruleBody(css, '.item-card__media')

    expect(media).toContain('width: 100%')
    expect(media).toContain('min-width: 0')
    expect(media).toContain('overflow: hidden')
    expect(media).toContain('aspect-ratio: var(--card-media-aspect)')
  })

  it('карточка не подчёркивает текст при наведении', () => {
    expect(ruleBody(css, '.item-card:hover')).toContain('text-decoration: none')
  })
})

describe('Полоса разделов в шапке', () => {
  it('скрывает системную полосу прокрутки у списка разделов', () => {
    expect(ruleBody(css, '.category-strip__list')).toContain('scrollbar-width: none')
  })

  it('скрывает полосу прокрутки WebKit у списка разделов', () => {
    expect(ruleBody(css, '.category-strip__list::-webkit-scrollbar')).toContain('display: none')
  })

  it('гасит край списка разделов при непросмотренных разделах', () => {
    const fadeEnd = ruleBody(css, '.category-strip__list--fade-end')

    expect(fadeEnd).toContain('-webkit-mask-image')
    expect(fadeEnd).toContain('mask-image')
  })
})

describe('Окно просмотра фотографий', () => {
  it('вписывает полноразмерное изображение в окно', () => {
    const image = ruleBody(css, '.photo-viewer__image')

    expect(image).toContain('max-width: 100%')
    expect(image).toContain('max-height: 70vh')
    expect(image).toContain('object-fit: contain')
  })

  it('затемняет фон за окном', () => {
    expect(ruleBody(css, '.photo-viewer::backdrop')).toContain('background: var(--color-overlay)')
  })
})

describe('Шапка на мобильной ширине', () => {
  it('на настольной ширине низ шапки не задаёт отступ: его даёт полоса разделов', () => {
    expect(paddingValues(ruleBody(layoutCss, '.site-header__inner'))[2]).toBe('0')
  })

  it('на мобильной ширине отделяет последнюю строку шапки от нижней границы', () => {
    const mobile = mediaBody(layoutCss, '@media (max-width: 768px)')
    const padding = paddingValues(ruleBody(mobile, '.site-header__inner'))

    expect(padding[2]).not.toBe('0')
    expect(padding[2]).toBe('var(--space-3)')
  })
})

