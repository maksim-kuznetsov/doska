/** Проверки окружения, от которых зависит доступность функций PWA. */

export interface ServiceWorkerEnvironment {
  production: boolean
  secure: boolean
  supported: boolean
}

/** iOS и iPadOS: iPadOS 13+ представляется как macOS, поэтому учитываем touch-точки. */
export function isIosDevice(
  userAgent: string = navigator.userAgent,
  maxTouchPoints: number = navigator.maxTouchPoints ?? 0,
): boolean {
  if (/iphone|ipad|ipod/i.test(userAgent)) {
    return true
  }

  return /macintosh/i.test(userAgent) && maxTouchPoints > 1
}

/** Приложение открыто как установленное, а не во вкладке браузера. */
export function isStandaloneMode(
  matchesDisplayMode: boolean = typeof window !== 'undefined'
    && typeof window.matchMedia === 'function'
    && window.matchMedia('(display-mode: standalone)').matches,
  navigatorStandalone: boolean = typeof navigator !== 'undefined'
    && Boolean((navigator as Navigator & { standalone?: boolean }).standalone),
): boolean {
  return matchesDisplayMode || navigatorStandalone
}

export function isSecureContextAvailable(
  secure: boolean = typeof window !== 'undefined' && Boolean(window.isSecureContext),
  hostname: string = typeof window !== 'undefined' ? window.location.hostname : '',
): boolean {
  // localhost и петлевые адреса браузеры считают защищённым контекстом.
  return secure || hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '[::1]'
}

export function isServiceWorkerSupported(
  supported: boolean = typeof navigator !== 'undefined' && 'serviceWorker' in navigator,
): boolean {
  return supported
}

/**
 * Service worker регистрируется только в production-сборке, в защищённом
 * контексте и при поддержке браузером.
 */
export function shouldRegisterServiceWorker(environment: ServiceWorkerEnvironment): boolean {
  return environment.production && environment.secure && environment.supported
}