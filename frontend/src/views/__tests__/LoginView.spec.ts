import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'
import { mountWithRouter } from '../../__tests__/helpers/mount'

vi.mock('../../api/client', () =>
  createApiClientMock({
    auth: {
      login: vi.fn(),
      providers: () => Promise.resolve({ providers: [] }),
    },
  }),
)

import type { AuthProviderInfo } from '../../api/client'
import { useAuthStore } from '../../stores/auth'
import LoginView from '../LoginView.vue'

const googleProvider: AuthProviderInfo = { id: 'google', name: 'Google', enabled: true }
const yandexProvider: AuthProviderInfo = { id: 'yandex', name: 'Яндекс', enabled: true }

const routes: RouteRecordRaw[] = [
  { path: '/', name: 'home', component: { template: '<div />' } },
  { path: '/login', name: 'login', component: LoginView },
  { path: '/register', name: 'register', component: { template: '<div />' } },
  { path: '/forgot-password', name: 'forgot-password', component: { template: '<div />' } },
  {
    path: '/resend-confirmation',
    name: 'resend-confirmation',
    component: { template: '<div />' },
  },
  { path: '/terms', name: 'terms', component: { template: '<div />' } },
  { path: '/privacy', name: 'privacy', component: { template: '<div />' } },
]

async function mountView(path = '/login', providers: AuthProviderInfo[] = []) {
  const pinia = createPinia()
  setActivePinia(pinia)

  const auth = useAuthStore()
  auth.providers = providers

  const wrapper = await mountWithRouter({
    component: LoginView,
    routes,
    path,
    plugins: [pinia],
  })

  return { wrapper, auth }
}

describe('LoginView', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('сообщает о принятии условий и ссылается на документы', async () => {
    const { wrapper } = await mountView()

    expect(wrapper.text()).toContain('Продолжая, вы принимаете')
    expect(wrapper.get('a[href="/terms"]').text()).toBe('условия использования')
    expect(wrapper.get('a[href="/privacy"]').text()).toBe('политику конфиденциальности')
  })

  it('показывает Google-вход с текущим адресом возврата', async () => {
    const { wrapper } = await mountView('/login?redirect=/ads?page=2', [googleProvider])

    const link = wrapper.find('a[href^="/api/auth/google/start"]')

    expect(link.exists()).toBe(true)
    expect(link.attributes('href')).toBe(
      '/api/auth/google/start?redirect=%2Fads%3Fpage%3D2',
    )
    expect(link.text()).toContain('Войти через Google')
  })

  it('показывает вход через Яндекс', async () => {
    const { wrapper } = await mountView('/login', [yandexProvider])

    const link = wrapper.get('a[href^="/api/auth/yandex/start"]')

    expect(link.attributes('href')).toBe('/api/auth/yandex/start?redirect=%2F')
    expect(link.text()).toContain('Войти через Яндекс')
  })

  it('скрывает внешний вход при выключенных провайдерах', async () => {
    const { wrapper } = await mountView('/login', [])

    expect(wrapper.find('a[href^="/api/auth/google/start"]').exists()).toBe(false)
    expect(wrapper.find('a[href^="/api/auth/yandex/start"]').exists()).toBe(false)
  })

  it('показывает ошибку входа из query', async () => {
    const { wrapper } = await mountView('/login?google=error&reason=blocked', [googleProvider])

    expect(wrapper.text()).toContain('Аккаунт заблокирован')
  })
})
