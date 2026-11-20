import { readFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../..')

function read(relativePath: string): string {
  return readFileSync(resolve(repoRoot, relativePath), 'utf8')
}

describe('Конфигурация развёртывания', () => {
  it('описывает три сервиса production-окружения', () => {
    const compose = read('docker-compose.prod.yml')

    expect(compose).toContain('postgres:')
    expect(compose).toContain('api:')
    expect(compose).toContain('web:')
    expect(compose).toContain('postgres-data:')
    expect(compose).toContain('media-data:')
    expect(compose).toContain('ConnectionStrings__Default')
  })

  it('описывает HTTPS через Caddy и не публикует порт веб-сервера', () => {
    const compose = read('docker-compose.prod.yml')
    const webBlock = / {2}web:\n([\s\S]*?)\n {2}caddy:/.exec(compose)?.[1] ?? ''

    expect(compose).toContain('caddy:')
    expect(compose).toContain('caddy:2-alpine')
    expect(compose).toContain('${DOMAIN:?')
    expect(compose).toContain('${ACME_EMAIL:?')
    expect(compose).toContain('./deploy/Caddyfile')
    expect(webBlock).toContain('expose:')
    expect(webBlock).not.toMatch(/^ {4}ports:/m)
    expect(read('deploy/Caddyfile')).toContain('reverse_proxy web:80')
  })

  it('веб-сервер проксирует API и отдаёт медиа внутренним путём', () => {
    const nginx = read('deploy/nginx.conf')

    expect(nginx).toContain('try_files $uri $uri/ /index.html')
    expect(nginx).toContain('proxy_pass http://api:8080')
    expect(nginx).toContain('location /media-internal/')
    expect(nginx).toContain('internal;')
  })

  it('не хранит секреты в репозитории', () => {
    const compose = read('docker-compose.prod.yml')
    const example = read('.env.example')

    expect(compose).not.toContain('POSTGRES_PASSWORD: doska')
    expect(compose).not.toContain('Password=doska')
    expect(example).toContain('POSTGRES_PASSWORD=change-me')
  })

  it('описывает откат релиза в документации', () => {
    expect(read('deploy/DEPLOY.md')).toContain('Откат релиза')
    expect(read('README.md')).toContain('deploy/DEPLOY.md')
  })
})