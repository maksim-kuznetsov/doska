import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { createApiClientMock } from '../../__tests__/helpers/apiClientMock'
import { mountWithRouter } from '../../__tests__/helpers/mount'

vi.mock('../../api/client', () =>
  createApiClientMock({
    auth: {
      login: vi.fn(),
      providers: () => Promise.resolve({ google: false }),
    },
  }),
)

import { useAuthStore } from '../../stores/auth'
import LoginView from '../LoginView.vue'

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

async function mountView(path = '/login', googleEnabled = false) {
  const pinia = createPinia()
  setActivePinia(pinia)

  const auth = useAuthStore()
  auth.googleEnabled = googleEnabled

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
    const { wrapper } = await mountView('/login?redirect=/ads?page=2', true)

    const link = wrapper.find('a[href^="/api/auth/google/start"]')

    expect(link.exists()).toBe(true)
    expect(link.attributes('href')).toBe(
      '/api/auth/google/start?redirect=%2Fads%3Fpage%3D2',
    )
    expect(link.text()).toContain('Войти через Google')
  })

  it('скрывает Google-вход при выключенном провайдере', async () => {
    const { wrapper } = await mountView('/login', false)

    expect(wrapper.find('a[href^="/api/auth/google/start"]').exists()).toBe(false)
  })

  it('показывает ошибку Google-входа из query', async () => {
    const { wrapper } = await mountView('/login?google=error&reason=blocked', true)

    expect(wrapper.text()).toContain('Аккаунт заблокирован')
  })
})