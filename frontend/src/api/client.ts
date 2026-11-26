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
  displayName: string | null
  contacts: string | null
}

export interface AdPhotoSummary {
  id: string
  url: string
  thumbnailUrl: string
  contentType: string
  byteSize: number
  width: number
  height: number
  sortOrder: number
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
  photos: AdPhotoSummary[]
  isOwn: boolean
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
  description: string
  price: number
  cityName: string
  categoryName: string
  contacts: string
  authorEmail: string
  authorIsTrusted: boolean
  authorApprovedAdsCount: number
  submittedAt: string
  photos: AdPhotoSummary[]
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
  adDescription: string
  adPrice: number
  adCityName: string
  adCategoryName: string
  adContacts: string
  adStatus: string
  adAuthorEmail: string
  adPhotos: AdPhotoSummary[]
  authorEmail: string
  reason: string
  openComplaintsCount: number
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
  thumbnailUrl: string | null
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

export interface CategoryItemView {
  id: string
  name: string
  sortOrder: number
  isActive: boolean
}

export interface CategorySectionView {
  id: string
  name: string
  sortOrder: number
  isActive: boolean
  subsections: CategoryItemView[]
}

export interface CityAdminView {
  id: string
  name: string
  isActive: boolean
}

export interface AdminUserView {
  id: string
  email: string
  role: 'User' | 'Moderator' | 'Admin'
  isTrusted: boolean
  isBlocked: boolean
  isEmailConfirmed: boolean
}

export interface OutboxMessageView {
  fileName: string
  size: number
  modifiedAt: string
  recipient: string
  subject: string
  sentAt: string | null
}

export interface OutboxMessageContent {
  content: string
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
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
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

async function parseResponse<T>(response: Response): Promise<T> {
  if (response.status === 204) {
    return undefined as T
  }

  const text = await response.text()
  let payload: unknown = null

  if (text) {
    try {
      payload = JSON.parse(text)
    } catch {
      // Прокси может вернуть HTML или пустое тело вместо ProblemDetails.
      payload = null
    }
  }

  if (!response.ok) {
    const problem = payload as { title?: string; detail?: string } | null
    const message = problem?.title ?? problem?.detail ?? fallbackErrorMessage(response.status)
    throw new ApiError(response.status, message)
  }

  return payload as T
}

function fallbackErrorMessage(status: number): string {
  if (status === 413) {
    return 'Фотографии слишком большие. Уменьшите файлы: допустимо не более 5 МБ на фотографию.'
  }

  return `Ошибка запроса (${status}).`
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

  return parseResponse<T>(response)
}

async function requestForm<T>(path: string, formData: FormData): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'X-CSRF-TOKEN': await fetchCsrfToken(),
    },
    body: formData,
    credentials: 'include',
  })

  return parseResponse<T>(response)
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
    forgotPassword: (email: string) =>
      request<void>('/auth/forgot-password', { method: 'POST', body: { email } }),
    resendConfirmation: (email: string) =>
      request<void>('/auth/resend-confirmation', { method: 'POST', body: { email } }),
    resetPassword: (token: string, password: string) =>
      request<void>('/auth/reset-password', { method: 'POST', body: { token, password } }),
    logout: () => request<void>('/auth/logout', { method: 'POST' }),
  },
  account: {
    updateProfile: (displayName: string | null, contacts: string | null) =>
      request<CurrentUser>('/account/profile', { method: 'PUT', body: { displayName, contacts } }),
    changePassword: (currentPassword: string, newPassword: string) =>
      request<void>('/account/password', {
        method: 'POST',
        body: { currentPassword, newPassword },
      }),
  },
  catalog: {
    cities: () => request<CityOption[]>('/catalog/cities'),
    categories: () => request<CategoryOption[]>('/catalog/categories'),
    sections: () => request<SectionOption[]>('/catalog/sections'),
  },
  ads: {
    search: (params: AdSearchParams) => request<PagedResult<AdCard>>(`/ads${toQueryString(params)}`),
    create: (formData: FormData) => requestForm<AdSummary>('/ads', formData),
    get: (id: string) => request<AdSummary>(`/ads/${id}`),
    update: (id: string, payload: CreateAdPayload) =>
      request<AdSummary>(`/ads/${id}`, { method: 'PUT', body: payload }),
    remove: (id: string) => request<void>(`/ads/${id}`, { method: 'DELETE' }),
    markSold: (id: string) => request<AdSummary>(`/ads/${id}/sold`, { method: 'POST' }),
    returnToSale: (id: string) =>
      request<AdSummary>(`/ads/${id}/return-to-sale`, { method: 'POST' }),
    renew: (id: string) => request<AdSummary>(`/ads/${id}/renew`, { method: 'POST' }),
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
    block: (id: string) => request<CurrentUser>(`/users/${id}/block`, { method: 'POST' }),
    unblock: (id: string) => request<CurrentUser>(`/users/${id}/unblock`, { method: 'POST' }),
  },
  adminUsers: {
    list: () => request<AdminUserView[]>('/admin/users'),
    setRole: (id: string, role: string) =>
      request<AdminUserView>(`/admin/users/${id}/role`, { method: 'PUT', body: { role } }),
  },
  adminCategories: {
    tree: () => request<CategorySectionView[]>('/admin/categories'),
    createSection: (name: string, sortOrder: number) =>
      request<CategorySectionView>('/admin/categories/sections', {
        method: 'POST',
        body: { name, sortOrder },
      }),
    createSubsection: (sectionId: string, name: string, sortOrder: number) =>
      request<CategoryItemView>(`/admin/categories/${sectionId}/subsections`, {
        method: 'POST',
        body: { name, sortOrder },
      }),
    update: (id: string, name: string, sortOrder: number) =>
      request<CategoryItemView>(`/admin/categories/${id}`, {
        method: 'PUT',
        body: { name, sortOrder },
      }),
    setActive: (id: string, isActive: boolean) =>
      request<CategoryItemView>(
        `/admin/categories/${id}/${isActive ? 'activate' : 'deactivate'}`,
        { method: 'POST' },
      ),
    remove: (id: string) => request<void>(`/admin/categories/${id}`, { method: 'DELETE' }),
  },
  adminCities: {
    list: () => request<CityAdminView[]>('/admin/cities'),
    create: (name: string) =>
      request<CityAdminView>('/admin/cities', { method: 'POST', body: { name } }),
    rename: (id: string, name: string) =>
      request<CityAdminView>(`/admin/cities/${id}`, { method: 'PUT', body: { name } }),
    setActive: (id: string, isActive: boolean) =>
      request<CityAdminView>(`/admin/cities/${id}/${isActive ? 'activate' : 'deactivate'}`, {
        method: 'POST',
      }),
    remove: (id: string) => request<void>(`/admin/cities/${id}`, { method: 'DELETE' }),
  },
  adminOutbox: {
    list: () => request<OutboxMessageView[]>('/admin/outbox'),
    read: (fileName: string) =>
      request<OutboxMessageContent>(`/admin/outbox/${encodeURIComponent(fileName)}`),
  },
}