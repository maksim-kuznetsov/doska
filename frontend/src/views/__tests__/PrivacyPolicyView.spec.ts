import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'

import PrivacyPolicyView from '../PrivacyPolicyView.vue'

describe('PrivacyPolicyView', () => {
  const wrapper = mount(PrivacyPolicyView)

  it('показывает заголовок и дату редакции', () => {
    expect(wrapper.get('.legal__title').text()).toBe('Политика конфиденциальности')
    expect(wrapper.get('.legal__updated').text()).toContain('27 ноября 2026')
  })

  it('называет оператора, адрес сайта и контакт', () => {
    const text = wrapper.text()

    expect(text).toContain('Администрация сайта Doska')
    expect(text).toContain('https://doska.мкузнецов.рф:8443/')
    expect(text).not.toContain('xn--')
    expect(wrapper.get('a[href="https://doska.xn--b1affljii0b4a.xn--p1ai:8443/"]')).toBeTruthy()
    expect(text).toContain('maksim.kuznetsov@live.ru')
    expect(wrapper.get('a[href="mailto:maksim.kuznetsov@live.ru"]')).toBeTruthy()
  })

  it('описывает данные аккаунта, Google, контент, cookie и технические сведения', () => {
    const text = wrapper.text()

    expect(text).toContain('хеш пароля')
    expect(text).toContain('идентификатор пользователя Google')
    expect(text).toContain('Яндекс ID')
    expect(text).toContain('подтверждения email')
    expect(text).toContain('cookie-сессия')
    expect(text).toContain('объявления')
    expect(text).toContain('фотографии')
    expect(text).toContain('жалобы')
    expect(text).toContain('IP-адрес')
  })

  it('объясняет цели, основания и хранение данных', () => {
    const text = wrapper.text()

    expect(text).toContain('Цели и основания обработки')
    expect(text).toContain('Хранение и защита')
    expect(text).toContain('30 дней')
    expect(text).toContain('7 дней')
    expect(text).toContain('90 дней')
  })

  it('раскрывает Limited Use и ссылается на политику Google', () => {
    const text = wrapper.text()

    expect(text).toContain('Google API Services User Data Policy')
    expect(text).toContain('Limited Use')
    expect(wrapper.get('a[href*="developers.google.com/terms/api-services-user-data-policy"]')).toBeTruthy()
    expect(wrapper.get('a[href="https://policies.google.com/privacy"]')).toBeTruthy()
  })

  it('описывает права и удаление по обращению', () => {
    const text = wrapper.text()

    expect(text).toContain('Права пользователя')
    expect(text).toContain('удалить данные или аккаунт')
    expect(text).toContain('Отвязать Google')
    expect(text).toContain('уполномоченный орган')
  })

  it('не содержит заглушек и пустых разделов', () => {
    const text = wrapper.text()

    expect(text).not.toContain('TODO')
    expect(text).not.toContain('example.com')
    expect(text).not.toContain('заполнить позже')
    expect(text).not.toContain('черновик')
  })
})