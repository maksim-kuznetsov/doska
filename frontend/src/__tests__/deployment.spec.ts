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
    expect(read('README.md')).toContain('Откат релиза')
  })
})