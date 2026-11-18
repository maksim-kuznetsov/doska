import type { AdSearchParams, AdSort } from './client'

/** Состояние фильтров ленты, хранимое в query-параметрах маршрута. */
export interface FeedFilters {
  query: string
  categoryId: string
  cityId: string
  minPrice: string
  maxPrice: string
  sort: AdSort
  page: number
}

export const defaultFeedFilters: FeedFilters = {
  query: '',
  categoryId: '',
  cityId: '',
  minPrice: '',
  maxPrice: '',
  sort: 'newest',
  page: 1,
}

type QuerySource = Record<string, unknown>

function readString(source: QuerySource, key: string): string {
  const value = source[key]

  if (Array.isArray(value)) {
    return typeof value[0] === 'string' ? value[0] : ''
  }

  return typeof value === 'string' ? value : ''
}

/** Читает фильтры из query-параметров, подставляя значения по умолчанию. */
export function readFilters(source: QuerySource): FeedFilters {
  const sortValue = readString(source, 'sort')
  const sort: AdSort =
    sortValue === 'price_asc' || sortValue === 'price_desc' ? sortValue : 'newest'

  const pageValue = Number.parseInt(readString(source, 'page'), 10)

  return {
    query: readString(source, 'query'),
    categoryId: readString(source, 'categoryId'),
    cityId: readString(source, 'cityId'),
    minPrice: readString(source, 'minPrice'),
    maxPrice: readString(source, 'maxPrice'),
    sort,
    page: Number.isFinite(pageValue) && pageValue > 0 ? pageValue : 1,
  }
}

/** Формирует query-параметры маршрута, опуская значения по умолчанию. */
export function toRouteQuery(filters: FeedFilters): Record<string, string> {
  const query: Record<string, string> = {}
  const text = filters.query.trim()
  const minPrice = filters.minPrice.trim()
  const maxPrice = filters.maxPrice.trim()

  if (text.length > 0) query.query = text
  if (filters.categoryId) query.categoryId = filters.categoryId
  if (filters.cityId) query.cityId = filters.cityId
  if (minPrice.length > 0) query.minPrice = minPrice
  if (maxPrice.length > 0) query.maxPrice = maxPrice
  if (filters.sort !== 'newest') query.sort = filters.sort
  if (filters.page > 1) query.page = String(filters.page)

  return query
}

/** Преобразует фильтры в параметры запроса к API. */
export function toSearchParams(filters: FeedFilters): AdSearchParams {
  const minPrice = Number.parseFloat(filters.minPrice)
  const maxPrice = Number.parseFloat(filters.maxPrice)
  const text = filters.query.trim()

  return {
    query: text.length > 0 ? text : undefined,
    categoryId: filters.categoryId || undefined,
    cityId: filters.cityId || undefined,
    minPrice: Number.isFinite(minPrice) ? minPrice : undefined,
    maxPrice: Number.isFinite(maxPrice) ? maxPrice : undefined,
    sort: filters.sort,
    page: filters.page,
  }
}