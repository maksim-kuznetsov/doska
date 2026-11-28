/** Адреса запуска внешнего входа и перевод кодов ошибок сервера. */

/** Локальный адрес возврата по умолчанию. */
const homePath = '/'

/** Название провайдера для текстов сообщений. */
function providerName(providerId: string): string {
  switch (providerId) {
    case 'google':
      return 'Google'
    case 'yandex':
      return 'Яндекс'
    default:
      return providerId
  }
}

/** Ссылка полноценной навигации для входа через провайдера. */
export function externalStartUrl(providerId: string, redirect: string | null | undefined): string {
  const target = typeof redirect === 'string' && redirect.length > 0 ? redirect : homePath
  return `/api/auth/${encodeURIComponent(providerId)}/start?redirect=${encodeURIComponent(target)}`
}

/** Ссылка полноценной навигации для привязки провайдера из профиля. */
export function externalLinkUrl(providerId: string): string {
  return `/api/auth/${encodeURIComponent(providerId)}/link`
}

/** Русское сообщение для кода причины ошибки входа через провайдера из query. */
export function externalErrorMessage(providerId: string, reason: unknown): string {
  const name = providerName(providerId)

  switch (typeof reason === 'string' ? reason : '') {
    case 'blocked':
      return 'Аккаунт заблокирован. Обратитесь к администрации.'
    case 'conflict':
      return `Этот аккаунт ${name} уже привязан к другому аккаунту.`
    case 'unverified':
      return `${name} не подтвердил адрес почты. Попробуйте другой аккаунт.`
    case 'session':
      return 'Сессия изменилась. Войдите заново и повторите привязку.'
    case 'denied':
      return `Доступ к ${name} отменён.`
    case 'invalid':
    case 'failed':
    default:
      return `Не удалось войти через ${name}. Попробуйте ещё раз.`
  }
}
