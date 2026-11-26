import { describe, expect, it } from 'vitest'
import type { RouteRecordRaw } from 'vue-router'

import { mountWithRouter } from '../../__tests__/helpers/mount'
import TermsOfServiceView from '../TermsOfServiceView.vue'

const routes: RouteRecordRaw[] = [
  { path: '/terms', name: 'terms', component: TermsOfServiceView },
  { path: '/privacy', name: 'privacy', component: { template: '<div />' } },
]

async function mountView() {
  return mountWithRouter({
    component: TermsOfServiceView,
    routes,
    path: '/terms',
  })
}

describe('TermsOfServiceView', () => {
  it('показывает заголовок и дату редакции', async () => {
    const wrapper = await mountView()

    expect(wrapper.get('.legal__title').text()).toBe('Условия использования')
    expect(wrapper.get('.legal__updated').text()).toContain('26 ноября 2026')
  })

  it('показывает кириллический адрес сайта, а ссылка ведёт на ASCII-домен', async () => {
    const wrapper = await mountView()
    const text = wrapper.text()

    expect(text).toContain('https://doska.мкузнецов.рф:8443/')
    expect(text).not.toContain('xn--')
    expect(wrapper.get('a[href="https://doska.xn--b1affljii0b4a.xn--p1ai:8443/"]')).toBeTruthy()
  })

  it('сообщает о принятии условий и ссылается на политику', async () => {
    const wrapper = await mountView()
    const text = wrapper.text()

    expect(text).toContain('означают, что пользователь принял Условия')
    expect(wrapper.get('a[href="/privacy"]').text()).toBe('Политику конфиденциальности')
  })

  it('описывает аккаунт, подтверждение email и запрет передачи аккаунта', async () => {
    const text = (await mountView()).text()

    expect(text).toContain('подтверждение email обязательно')
    expect(text).toContain('передавать аккаунт третьим лицам запрещено')
    expect(text).toContain('созданный через Google')
  })

  it('указывает правила объявлений и суточный лимит в пять объявлений', async () => {
    const text = (await mountView()).text()

    expect(text).toContain('не более пяти объявлений в сутки')
    expect(text).toContain('на 7 дней')
    expect(text).toContain('достоверными')
    expect(text).toContain('права на фотографии')
  })

  it('перечисляет запрещённый контент', async () => {
    const text = (await mountView()).text()

    expect(text).toContain('оружие')
    expect(text).toContain('наркотики')
    expect(text).toContain('порнографию')
    expect(text).toContain('разжигание ненависти')
    expect(text).toContain('спам')
    expect(text).toContain('персональные данные')
    expect(text).toContain('авторских прав')
  })

  it('объясняет модерацию, блокировку и права на контент', async () => {
    const text = (await mountView()).text()

    expect(text).toContain('Модерация, жалобы и блокировка')
    expect(text).toContain('блокирует аккаунты')
    expect(text).toContain('Права на контент')
    expect(text).toContain('неисключительное право')
    expect(text).toContain('без компенсации')
  })

  it('ограничивает ответственность и описывает изменения условий', async () => {
    const text = (await mountView()).text()

    expect(text).toContain('Ответственность')
    expect(text).toContain('«как есть»')
    expect(text).toContain('Изменения условий')
    expect(text).toContain('Новая редакция публикуется')
    expect(text).toContain('maksim.kuznetsov@live.ru')
  })

  it('не содержит заглушек', async () => {
    const text = (await mountView()).text()

    expect(text).not.toContain('TODO')
    expect(text).not.toContain('example.com')
    expect(text).not.toContain('заполнить позже')
    expect(text).not.toContain('черновик')
  })
})