import { readFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../..')

function read(relativePath: string): string {
  return readFileSync(resolve(repoRoot, relativePath), 'utf8')
}

describe('Версионирование и кеширование статики', () => {
  it('отдаёт HTML-оболочку без кеширования', () => {
    const nginx = read('deploy/nginx.conf')

    expect(nginx).toContain('location = /index.html')
    expect(nginx).toContain('Cache-Control "no-cache"')
  })

  it('кеширует хешированные ассеты надолго', () => {
    const nginx = read('deploy/nginx.conf')

    expect(nginx).toContain('location /assets/')
    expect(nginx).toContain('max-age=31536000')
    expect(nginx).toContain('immutable')
  })

  it('включает манифест сборки с хешированными именами', () => {
    const viteConfig = read('frontend/vite.config.ts')

    expect(viteConfig).toContain('manifest: true')
  })

  it('в режиме разработки подключает исходный модуль без сборки', () => {
    const html = read('frontend/index.html')

    expect(html).toContain('/src/main.ts')
    expect(html).not.toContain('/assets/')
  })
})