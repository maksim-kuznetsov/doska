/**
 * Реквизиты правовых документов сайта.
 *
 * Единственный источник оператора, публичного адреса, контакта и дат редакции:
 * политика конфиденциальности и условия использования берут значения отсюда,
 * поэтому при смене контакта или даты правка делается в одном месте.
 */
export const operatorName = 'Администрация сайта Doska'

/** Публичный адрес сайта в ASCII (punycode) — используется в ссылках `href`. */
export const publicSiteUrl = 'https://doska.xn--b1affljii0b4a.xn--p1ai:8443/'

/** Кириллическое отображение публичного адреса для текста документов. */
export const publicSiteDisplayUrl = 'https://doska.мкузнецов.рф:8443/'

export const contactEmail = 'maksim.kuznetsov@live.ru'

export const privacyUpdatedAt = '2026-11-27'

export const termsUpdatedAt = '2026-11-27'

export const googlePrivacyPolicyUrl = 'https://policies.google.com/privacy'

export const googleApiServicesUserDataPolicyUrl =
  'https://developers.google.com/terms/api-services-user-data-policy'

const russianDateFormatter = new Intl.DateTimeFormat('ru-RU', {
  day: 'numeric',
  month: 'long',
  year: 'numeric',
  timeZone: 'UTC',
})

/**
 * Форматирует ISO-дату (`YYYY-MM-DD`) для подписи «Дата редакции».
 * Дата разбирается как календарная, без сдвига часовым поясом.
 */
export function formatRussianDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number)

  if (
    !Number.isInteger(year) ||
    !Number.isInteger(month) ||
    !Number.isInteger(day) ||
    month < 1 ||
    month > 12 ||
    day < 1 ||
    day > 31
  ) {
    throw new Error(`Некорректная ISO-дата: ${isoDate}`)
  }

  return russianDateFormatter.format(new Date(Date.UTC(year, month - 1, day)))
}