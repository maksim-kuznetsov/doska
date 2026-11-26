/** Адреса запуска Google-входа и перевод кодов ошибок сервера. */

/** Локальный адрес возврата по умолчанию. */
const homePath = '/'

/** Ссылка полноценной навигации для входа через Google. */
export function googleStartUrl(redirect: string | null | undefined): string {
  const target = typeof redirect === 'string' && redirect.length > 0 ? redirect : homePath
  return `/api/auth/google/start?redirect=${encodeURIComponent(target)}`
}

/** Ссылка полноценной навигации для привязки Google из профиля. */
export function googleLinkUrl(): string {
  return '/api/auth/google/link'
}

/** Русское сообщение для кода причины Google-ошибки из query. */
export function googleErrorMessage(reason: unknown): string {
  switch (typeof reason === 'string' ? reason : '') {
    case 'blocked':
      return 'Аккаунт заблокирован. Обратитесь к администрации.'
    case 'conflict':
      return 'Этот Google-аккаунт уже привязан к другому аккаунту.'
    case 'unverified':
      return 'Google не подтвердил адрес почты. Попробуйте другой аккаунт.'
    case 'session':
      return 'Сессия изменилась. Войдите заново и повторите привязку.'
    case 'denied':
      return 'Доступ к Google отменён.'
    case 'invalid':
    case 'failed':
    default:
      return 'Не удалось войти через Google. Попробуйте ещё раз.'
  }
}