import { describe, expect, it } from 'vitest'

import {
  contactEmail,
  formatRussianDate,
  googleApiServicesUserDataPolicyUrl,
  googlePrivacyPolicyUrl,
  operatorName,
  privacyUpdatedAt,
  publicSiteUrl,
  termsUpdatedAt,
} from '../details'

const isoDatePattern = /^\d{4}-\d{2}-\d{2}$/

describe('Реквизиты правовых документов', () => {
  it('заполняет оператора, адрес сайта и контакт без заглушек', () => {
    expect(operatorName).toBe('Администрация сайта Doska')
    expect(publicSiteUrl).toBe('https://doska.xn--b1affljii0b4a.xn--p1ai:8443/')
    expect(contactEmail).toBe('maksim.kuznetsov@live.ru')

    const values = [
      operatorName,
      publicSiteUrl,
      contactEmail,
      privacyUpdatedAt,
      termsUpdatedAt,
      googlePrivacyPolicyUrl,
      googleApiServicesUserDataPolicyUrl,
    ].join(' ')

    expect(values).not.toContain('example.com')
    expect(values).not.toContain('TODO')
    expect(values).not.toContain('заполнить позже')
  })

  it('хранит валидные ISO-даты редакции', () => {
    for (const date of [privacyUpdatedAt, termsUpdatedAt]) {
      expect(date).toMatch(isoDatePattern)
      expect(Number.isNaN(Date.parse(date))).toBe(false)
    }
  })

  it('форматирует дату по-русски без сдвига часовым поясом', () => {
    expect(formatRussianDate('2026-11-26')).toBe('26 ноября 2026 г.')
  })

  it('ссылается на политику Google и Limited Use', () => {
    expect(googlePrivacyPolicyUrl).toBe('https://policies.google.com/privacy')
    expect(googleApiServicesUserDataPolicyUrl).toContain('api-services-user-data-policy')
  })

  it('отклоняет некорректную дату', () => {
    expect(() => formatRussianDate('2026-13-01')).toThrow('Некорректная ISO-дата')
  })
})