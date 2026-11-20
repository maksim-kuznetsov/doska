import { existsSync, readFileSync } from 'node:fs'
import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'

const dist = resolve(dirname(fileURLToPath(import.meta.url)), '../dist')

function fail(message) {
  throw new Error(message)
}

function assert(condition, message) {
  if (!condition) {
    fail(message)
  }
}

function read(path) {
  assert(existsSync(resolve(dist, path)), `не найден файл dist/${path}`)
  return readFileSync(resolve(dist, path), 'utf8')
}

function pngSize(path) {
  const buffer = readFileSync(resolve(dist, path))

  assert(buffer.subarray(1, 4).toString() === 'PNG', `${path} не является PNG`)

  return {
    width: buffer.readUInt32BE(16),
    height: buffer.readUInt32BE(20),
  }
}

function main() {
  assert(existsSync(resolve(dist, 'sw.js')), 'не найден service worker dist/sw.js')

  const manifest = JSON.parse(read('manifest.webmanifest'))

  for (const field of [
    'name',
    'short_name',
    'start_url',
    'scope',
    'display',
    'theme_color',
    'background_color',
    'lang',
    'icons',
  ]) {
    assert(manifest[field], `в манифесте отсутствует поле ${field}`)
  }

  assert(manifest.display === 'standalone', 'манифест должен открывать приложение в standalone')
  assert(manifest.lang === 'ru', 'язык манифеста должен быть ru')
  assert(
    manifest.start_url === '/' || manifest.start_url.startsWith('/'),
    'стартовый адрес должен быть абсолютным путём внутри области действия',
  )

  assert(Array.isArray(manifest.icons) && manifest.icons.length > 0, 'в манифесте нет иконок')

  const sizes = new Set()
  let hasMaskable = false

  for (const icon of manifest.icons) {
    assert(icon.src, 'у иконки нет src')

    const path = icon.src.replace(/^\//, '')
    assert(existsSync(resolve(dist, path)), `не найдена иконка dist/${path}`)

    const actual = pngSize(path)
    const declared = String(icon.sizes ?? '')

    assert(
      declared === `${actual.width}x${actual.height}`,
      `размер иконки ${path} не совпадает с манифестом`,
    )

    sizes.add(declared)
    hasMaskable ||= icon.purpose === 'maskable'
  }

  assert(sizes.has('192x192'), 'нет иконки 192x192')
  assert(sizes.has('512x512'), 'нет иконки 512x512')
  assert(hasMaskable, 'нет maskable-иконки')

  const html = read('index.html')

  assert(html.includes('rel="manifest" href="/manifest.webmanifest"'), 'HTML не ссылается на манифест')
  assert(html.includes('name="theme-color"'), 'HTML не содержит theme-color')
  assert(html.includes('viewport-fit=cover'), 'HTML не содержит viewport-fit=cover')

  const serviceWorker = read('sw.js')
  assert(serviceWorker.includes('index.html'), 'service worker не кеширует оболочку index.html')

  console.log(
    `PWA-сборка корректна: service worker, манифест, ${manifest.icons.length} иконок, оболочка в precache.`,
  )
}

try {
  main()
} catch (error) {
  console.error(`Проверка PWA-сборки не пройдена: ${error.message}`)
  process.exitCode = 1
}