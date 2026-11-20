import vue from '@vitejs/plugin-vue'
import { VitePWA } from 'vite-plugin-pwa'
import { defineConfig } from 'vitest/config'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    VitePWA({
      // Регистрация выполняется вручную (src/pwa/registerServiceWorker.ts),
      // чтобы управлять предложением обновления.
      registerType: 'prompt',
      injectRegister: null,
      devOptions: {
        // В режиме разработки service worker не нужен: PWA проверяется на сборке.
        enabled: false,
      },
      manifest: {
        id: '/',
        name: 'Doska — доска объявлений',
        short_name: 'Doska',
        description: 'Доска объявлений: подача, поиск и модерация.',
        lang: 'ru',
        start_url: '/',
        scope: '/',
        display: 'standalone',
        theme_color: '#00AAFF',
        background_color: '#F5F5F7',
        icons: [
          {
            src: '/pwa/pwa-64x64.png',
            sizes: '64x64',
            type: 'image/png',
          },
          {
            src: '/pwa/pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png',
          },
          {
            src: '/pwa/pwa-512x512.png',
            sizes: '512x512',
            type: 'image/png',
          },
          {
            src: '/pwa/maskable-icon-512x512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'maskable',
          },
        ],
      },
      workbox: {
        // Кешируем только оболочку и статические файлы сборки.
        globPatterns: ['**/*.{js,css,html,svg,png,ico,webmanifest}'],
        navigateFallback: '/index.html',
        // API и медиа всегда идут в сеть и не попадают в кеш.
        navigateFallbackDenylist: [/^\/api\//, /^\/media\//],
        cleanupOutdatedCaches: true,
      },
    }),
  ],
  build: {
    // Манифест сборки: по нему определяются хешированные имена ассетов.
    manifest: true,
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
  test: {
    environment: 'jsdom',
    include: ['src/**/__tests__/*.spec.ts'],
  },
})
