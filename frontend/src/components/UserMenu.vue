<script setup lang="ts">
import { RouterLink } from 'vue-router'

import DropdownMenu from './DropdownMenu.vue'

/** Меню авторизованного пользователя: разделы и выход. */
defineProps<{
  email: string
  canModerate: boolean
  isAdmin: boolean
}>()

const emit = defineEmits<{ (event: 'logout'): void }>()
</script>

<template>
  <DropdownMenu panel-id="user-menu-panel" align="end">
    <template #trigger="{ open, toggle }">
      <button
        type="button"
        class="dropdown__trigger"
        aria-controls="user-menu-panel"
        :aria-expanded="open"
        @click="toggle"
      >
        {{ email }}
      </button>
    </template>

    <template #default="{ close }">
      <div class="dropdown__group">
        <p class="dropdown__group-title">Объявления</p>
        <RouterLink class="dropdown__item" to="/ads/mine" @click="close">Мои объявления</RouterLink>
      </div>

      <template v-if="canModerate || isAdmin">
        <hr class="dropdown__divider" />
        <div class="dropdown__group">
          <p class="dropdown__group-title">Служебные разделы</p>
          <RouterLink v-if="canModerate" class="dropdown__item" to="/moderation" @click="close">
            Проверка
          </RouterLink>
          <RouterLink v-if="canModerate" class="dropdown__item" to="/complaints" @click="close">
            Жалобы
          </RouterLink>
          <RouterLink v-if="isAdmin" class="dropdown__item" to="/admin/categories" @click="close">
            Категории
          </RouterLink>
          <RouterLink v-if="isAdmin" class="dropdown__item" to="/admin/cities" @click="close">
            Города
          </RouterLink>
          <RouterLink v-if="isAdmin" class="dropdown__item" to="/admin/users" @click="close">
            Пользователи
          </RouterLink>
          <RouterLink v-if="isAdmin" class="dropdown__item" to="/admin/outbox" @click="close">
            Письма (outbox)
          </RouterLink>
        </div>
      </template>

      <hr class="dropdown__divider" />
      <div class="dropdown__group">
        <button type="button" class="dropdown__item" @click="emit('logout')">Выйти</button>
      </div>
    </template>
  </DropdownMenu>
</template>