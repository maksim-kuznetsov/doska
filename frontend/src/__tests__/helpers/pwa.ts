import { vi } from 'vitest'

import type { BeforeInstallPromptEvent } from '../../stores/pwa'

/** Создаёт событие `beforeinstallprompt` с заданным исходом выбора. */
export function createInstallPromptEvent(
  outcome: 'accepted' | 'dismissed' = 'accepted',
): BeforeInstallPromptEvent {
  const prompt = vi.fn().mockResolvedValue(undefined)
  const event = new Event('beforeinstallprompt')

  Object.defineProperties(event, {
    prompt: { value: prompt },
    userChoice: {
      value: Promise.resolve({ outcome, platform: 'web' }),
    },
    platforms: { value: ['web'] },
  })

  return event as unknown as BeforeInstallPromptEvent
}
