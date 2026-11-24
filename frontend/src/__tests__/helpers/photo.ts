import type { AdPhotoSummary } from '../../api/client'

/** Создаёт фотографию объявления с указанным порядком сортировки. */
export function photo(order: number): AdPhotoSummary {
  return {
    id: `photo-${order}`,
    url: `/api/media/ad-1/photo-${order}.png`,
    thumbnailUrl: `/api/media/ad-1/photo-${order}_thumb.jpg`,
    contentType: 'image/png',
    byteSize: 128,
    width: 800,
    height: 600,
    sortOrder: order,
  }
}
