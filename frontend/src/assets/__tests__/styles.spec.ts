import { readFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const css = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), '../css/components.css'),
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

describe('Сетка карточек ленты', () => {
  it('карточка ограничивает минимальную ширину grid-элемента', () => {
    expect(ruleBody(css, '.ad-card')).toContain('min-width: 0')
  })

  it('медиа-блок карточки удерживает ширину и обрезает фото', () => {
    const media = ruleBody(css, '.ad-card__media')

    expect(media).toContain('width: 100%')
    expect(media).toContain('min-width: 0')
    expect(media).toContain('overflow: hidden')
    expect(media).toContain('aspect-ratio: var(--card-media-aspect)')
  })
})
