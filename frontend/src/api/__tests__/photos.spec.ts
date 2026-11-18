import { describe, expect, it } from 'vitest'

import { MAX_PHOTOS, MAX_PHOTO_BYTES, selectPhotos } from '../photos'

function photo(name: string, type = 'image/png', size = 128): File {
  return new File([new Uint8Array(size)], name, { type })
}

describe('selectPhotos', () => {
  it('принимает допустимые фотографии', () => {
    const files = Array.from({ length: MAX_PHOTOS }, (_, index) => photo(`${index}.png`))

    const result = selectPhotos([], files)

    expect(result.accepted).toHaveLength(MAX_PHOTOS)
    expect(result.rejected).toHaveLength(0)
  })

  it('не принимает шестую фотографию', () => {
    const existing = Array.from({ length: MAX_PHOTOS }, (_, index) => photo(`${index}.png`))

    const result = selectPhotos(existing, [photo('six.png')])

    expect(result.accepted).toHaveLength(MAX_PHOTOS)
    expect(result.rejected[0]).toContain('не более 5')
  })

  it('отклоняет неподдерживаемый формат и слишком большой файл', () => {
    const result = selectPhotos([], [
      photo('note.txt', 'text/plain'),
      photo('huge.png', 'image/png', MAX_PHOTO_BYTES + 1),
      photo('ok.webp', 'image/webp'),
    ])

    expect(result.accepted.map((file) => file.name)).toEqual(['ok.webp'])
    expect(result.rejected).toHaveLength(2)
    expect(result.rejected.join(' ')).toContain('JPEG')
    expect(result.rejected.join(' ')).toContain('5 МБ')
  })

  it('сохраняет уже выбранные фотографии', () => {
    const existing = [photo('one.png')]

    const result = selectPhotos(existing, [photo('two.png')])

    expect(result.accepted.map((file) => file.name)).toEqual(['one.png', 'two.png'])
  })
})