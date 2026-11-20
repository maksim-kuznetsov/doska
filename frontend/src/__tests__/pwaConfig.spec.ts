import { existsSync, readFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../..')

function read(relativePath: string): string {
  return readFileSync(resolve(repoRoot, relativePath), 'utf8')
}

function pngSize(relativePath: string): { width: number; height: number } {
  const buffer = readFileSync(resolve(repoRoot, relativePath))

  return {
    width: buffer.readUInt32BE(16),
    height: buffer.readUInt32BE(20),
  }
}

describe('Конфигурация PWA', () => {
  it('описывает web-манифест с обязательными полями', () => {
    const config = read('frontend/vite.config.ts')

    expect(config).toContain("name: 'Doska — доска объявлений'")
    expect(config).toContain("short_name: 'Doska'")
    expect(config).toContain("lang: 'ru'")
    expect(config).toContain("start_url: '/'")
    expect(config).toContain("scope: '/'")
    expect(config).toContain("display: 'standalone'")
    expect(config).toContain("theme_color: '#00AAFF'")
    expect(config).toContain("background_color: '#F5F5F7'")
  })

  it('включает манифест и иконки в сборку', () => {
    expect(existsSync(resolve(repoRoot, 'frontend/public/pwa/logo.svg'))).toBe(true)

    const icons = [
      ['frontend/public/pwa/pwa-192x192.png', 192],
      ['frontend/public/pwa/pwa-512x512.png', 512],
      ['frontend/public/pwa/maskable-icon-512x512.png', 512],
      ['frontend/public/pwa/apple-touch-icon-180x180.png', 180],
    ] as const

    for (const [path, size] of icons) {
      expect(existsSync(resolve(repoRoot, path)), path).toBe(true)
      expect(pngSize(path), path).toEqual({ width: size, height: size })
    }
  })

  it('учитывает безопасные зоны экрана и высоту мобильного окна', () => {
    const tokens = read('frontend/src/assets/css/tokens.css')
    const layout = read('frontend/src/assets/css/layout.css')

    expect(tokens).toContain('--safe-area-top: env(safe-area-inset-top, 0px)')
    expect(tokens).toContain('--safe-area-right: env(safe-area-inset-right, 0px)')
    expect(tokens).toContain('--safe-area-bottom: env(safe-area-inset-bottom, 0px)')
    expect(tokens).toContain('--safe-area-left: env(safe-area-inset-left, 0px)')

    expect(layout).toContain('min-height: 100dvh')
    expect(layout).toContain('var(--safe-area-top)')
    expect(layout).toContain('var(--safe-area-right)')
    expect(layout).toContain('var(--safe-area-bottom)')
    expect(layout).toContain('var(--safe-area-left)')
  })

  it('отдаёт service worker и манифест без кеширования', () => {
    const nginx = read('deploy/nginx.conf')

    expect(nginx).toContain('location = /sw.js')
    expect(nginx).toContain('location = /manifest.webmanifest')
    expect(nginx).toContain('application/manifest+json')
    expect(nginx.match(/Cache-Control "no-cache"/g)?.length).toBeGreaterThanOrEqual(2)
  })

  it('подключает PWA-мета-теги в HTML-оболочке', () => {
    const html = read('frontend/index.html')

    expect(html).toContain('viewport-fit=cover')
    expect(html).toContain('name="theme-color" content="#00AAFF"')
    expect(html).toContain('name="apple-mobile-web-app-capable" content="yes"')
    expect(html).toContain('name="apple-mobile-web-app-title" content="Doska"')
    expect(html).toContain('rel="apple-touch-icon"')
    expect(html).toContain('/pwa/apple-touch-icon-180x180.png')
  })
})