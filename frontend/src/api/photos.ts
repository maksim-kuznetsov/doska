/** Правила выбора фотографий совпадают с серверными: 1–5 файлов, до 5 МБ, JPEG/PNG/WebP. */

export const MAX_PHOTOS = 5
export const MAX_PHOTO_BYTES = 5 * 1024 * 1024
export const ACCEPTED_PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp']

export interface PhotoSelection {
  accepted: File[]
  rejected: string[]
}

/** Добавляет допустимые файлы к уже выбранным и объясняет отказы. */
export function selectPhotos(existing: File[], incoming: File[]): PhotoSelection {
  const accepted = [...existing]
  const rejected: string[] = []

  for (const file of incoming) {
    if (accepted.length >= MAX_PHOTOS) {
      rejected.push(`${file.name}: не более ${MAX_PHOTOS} фотографий`)
      continue
    }

    if (!ACCEPTED_PHOTO_TYPES.includes(file.type)) {
      rejected.push(`${file.name}: допустимы JPEG, PNG и WebP`)
      continue
    }

    if (file.size > MAX_PHOTO_BYTES) {
      rejected.push(`${file.name}: больше 5 МБ`)
      continue
    }

    accepted.push(file)
  }

  return { accepted, rejected }
}