/**
 * Клиент Web API: ходит по относительному адресу `/api` (в разработке — через прокси Vite),
 * передаёт cookie-сессию и antiforgery-токен для изменяющих запросов.
 */

export interface CurrentUser {
  id: string
  email: string
  role: 'User' | 'Moderator' | 'Admin'
  isTrusted: boolean
  isEmailConfirmed: boolean
}

export interface AdSummary {
  id: string
  title: string
  description: string
  price: number
  cityId: string
  categoryId: string
  contacts: string
  status: string
  moderationReason: string | null
  createdAt: string
  publishedAt: string | null
}

export interface CreateAdPayload {
  title: string
  description: string
  price: number
  cityId: string
  categoryId: string
  contacts: string
}

export interface CityOption {
  id: string
  name: string
}

export interface CategoryOption {
  id: string
  name: string
  sectionId: string
  sectionName: string
}

export interface ModerationQueueItem {
  id: string
  title: string
  authorEmail: string
  submittedAt: string
}

export interface ModerationDecision {
  adId: string
  status: string
  trustGranted: boolean
}

export interface ComplaintQueueItem {
  id: string
  adId: string
  adTitle: string
  authorEmail: string
  reason: string
  createdAt: string
}

export interface SubsectionOption {
  id: string
  name: string
}

export interface SectionOption {
  id: string
  name: string
  subsections: SubsectionOption[]
}

export interface AdCard {
  id: string
  title: string
  description: string
  price: number
  cityName: string
  categoryName: string
  sectionName: string
  publishedAt: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export type AdSort = 'newest' | 'price_asc' | 'price_desc'

export interface AdSearchParams {
  query?: string
  categoryId?: string
  cityId?: string
  minPrice?: number
  maxPrice?: number
  sort?: AdSort
  page?: number
  pageSize?: number
}

/** Ошибка API с HTTP-кодом и сообщением из `ProblemDetails`. */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'DELETE'
  body?: unknown
}

const apiBaseUrl = '/api'

function toQueryString(params: AdSearchParams): string {
  const search = new URLSearchParams()

  if (params.query) search.set('query', params.query)
  if (params.categoryId) search.set('categoryId', params.categoryId)
  if (params.cityId) search.set('cityId', params.cityId)
  if (params.minPrice !== undefined) search.set('minPrice', String(params.minPrice))
  if (params.maxPrice !== undefined) search.set('maxPrice', String(params.maxPrice))
  if (params.sort) search.set('sort', params.sort)
  if (params.page !== undefined) search.set('page', String(params.page))
  if (params.pageSize !== undefined) search.set('pageSize', String(params.pageSize))

  const query = search.toString()
  return query.length > 0 ? `?${query}` : ''
}

/** Кэш не используется: токен привязан к текущей сессии и запрашивается перед изменяющим запросом. */
async function fetchCsrfToken(): Promise<string> {
  const response = await fetch(`${apiBaseUrl}/auth/csrf`, { credentials: 'include' })

  if (!response.ok) {
    throw new ApiError(response.status, 'Не удалось получить защитный токен.')
  }

  const payload = (await response.json()) as { token: string }
  return payload.token
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const method = options.method ?? 'GET'
  const headers: Record<string, string> = { Accept: 'application/json' }
  const isMutating = method !== 'GET'

  if (isMutating) {
    headers['X-CSRF-TOKEN'] = await fetchCsrfToken()
  }

  let body: string | undefined
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json'
    body = JSON.stringify(options.body)
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method,
    headers,
    body,
    credentials: 'include',
  })

  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()
  const payload: unknown = text ? JSON.parse(text) : null

  if (!response.ok) {
    const problem = payload as { title?: string; detail?: string } | null
    const message = problem?.title ?? problem?.detail ?? `Ошибка запроса (${response.status}).`
    throw new ApiError(response.status, message)
  }

  return payload as T
}

export const api = {
  auth: {
    me: () => request<CurrentUser>('/auth/me'),
    login: (email: string, password: string) =>
      request<CurrentUser>('/auth/login', { method: 'POST', body: { email, password } }),
    register: (email: string, password: string) =>
      request<CurrentUser>('/auth/register', { method: 'POST', body: { email, password } }),
    confirmEmail: (token: string) =>
      request<void>('/auth/confirm-email', { method: 'POST', body: { token } }),
    logout: () => request<void>('/auth/logout', { method: 'POST' }),
  },
  catalog: {
    cities: () => request<CityOption[]>('/catalog/cities'),
    categories: () => request<CategoryOption[]>('/catalog/categories'),
    sections: () => request<SectionOption[]>('/catalog/sections'),
  },
  ads: {
    search: (params: AdSearchParams) => request<PagedResult<AdCard>>(`/ads${toQueryString(params)}`),
    create: (payload: CreateAdPayload) => request<AdSummary>('/ads', { method: 'POST', body: payload }),
    mine: () => request<AdSummary[]>('/ads/mine'),
  },
  moderation: {
    queue: () => request<ModerationQueueItem[]>('/moderation/queue'),
    approve: (id: string) => request<ModerationDecision>(`/moderation/ads/${id}/approve`, { method: 'POST' }),
    reject: (id: string, reason: string) =>
      request<ModerationDecision>(`/moderation/ads/${id}/reject`, { method: 'POST', body: { reason } }),
    hide: (id: string, reason: string) =>
      request<ModerationDecision>(`/moderation/ads/${id}/hide`, { method: 'POST', body: { reason } }),
  },
  complaints: {
    file: (adId: string, reason: string) =>
      request<void>('/complaints', { method: 'POST', body: { adId, reason } }),
    queue: () => request<ComplaintQueueItem[]>('/complaints/queue'),
    resolve: (id: string, hideAd: boolean, hideReason: string | null) =>
      request<void>(`/complaints/${id}/resolve`, { method: 'POST', body: { hideAd, hideReason } }),
  },
  users: {
    grantTrust: (id: string) => request<CurrentUser>(`/users/${id}/trust`, { method: 'POST' }),
    revokeTrust: (id: string) => request<CurrentUser>(`/users/${id}/trust`, { method: 'DELETE' }),
  },
}