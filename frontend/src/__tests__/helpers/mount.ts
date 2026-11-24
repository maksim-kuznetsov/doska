import { mount } from '@vue/test-utils'
import type { ComponentMountingOptions, GlobalMountOptions } from '@vue/test-utils'
import type { Component, Plugin } from 'vue'
import { createMemoryHistory, createRouter } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'

export interface MountWithRouterOptions {
  /** Компонент, который монтируется. */
  component: Component
  /** Маршруты приложения. */
  routes: RouteRecordRaw[]
  /** Путь, на который переходит роутер перед монтированием. */
  path?: string
  /** Свойства монтируемого компонента. */
  props?: Record<string, unknown>
  /** Дополнительные плагины (например, Pinia). */
  plugins?: Plugin[]
  /** Дополнительные настройки `mount`. */
  global?: GlobalMountOptions
}

/**
 * Собирает memory history, таблицу маршрутов, переходит на `path` и ждёт
 * `isReady`, после чего монтирует компонент с подключённым роутером.
 */
export async function mountWithRouter({
  component,
  routes,
  path = '/',
  props,
  plugins = [],
  global = {},
}: MountWithRouterOptions) {
  const router = createRouter({ history: createMemoryHistory(), routes })

  await router.push(path)
  await router.isReady()

  const options = {
    props,
    global: {
      ...global,
      plugins: [...(global.plugins ?? []), router, ...plugins],
    },
  } as ComponentMountingOptions<Component>

  return mount(component, options)
}
