import { describe, expect, it } from 'vitest'

import { read } from './helpers/repo'

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