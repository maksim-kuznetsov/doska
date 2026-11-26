<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { api, type AdminUserView } from '../api/client'
import { messageOf } from '../api/errors'
import AppMessage from '../components/AppMessage.vue'
import Breadcrumbs from '../components/Breadcrumbs.vue'
import EmptyState from '../components/EmptyState.vue'
import StatusBadge from '../components/StatusBadge.vue'

const users = ref<AdminUserView[]>([])
const error = ref('')
const loading = ref(false)

const roleLabels: Record<string, string> = {
  User: 'Пользователь',
  Moderator: 'Модератор',
  Admin: 'Администратор',
}

function formatRole(role: string): string {
  return roleLabels[role] ?? role
}

async function load(): Promise<void> {
  loading.value = true
  error.value = ''

  try {
    users.value = await api.adminUsers.list()
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось загрузить пользователей.')
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="page">
    <Breadcrumbs :items="[{ label: 'Главная', to: '/' }]" />

    <div class="page__head">
      <h1 class="page__title">Пользователи</h1>
      <span class="text-muted">Управление доступно администратору</span>
    </div>

    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <p v-if="loading" class="text-muted">Загружаем…</p>

    <EmptyState
      v-if="!loading && users.length === 0"
      title="Пользователей пока нет"
      description="Зарегистрированные пользователи появятся здесь."
    />

    <div v-if="users.length > 0" class="item-list">
      <article v-for="user in users" :key="user.id" class="panel stack" data-test="admin-user">
        <div class="split">
          <h2 class="item-row__title">{{ user.email }}</h2>
          <div class="cluster">
            <StatusBadge :status="user.isBlocked ? 'Hidden' : 'Active'" />
          </div>
        </div>

        <p class="text-muted">
          Роль: {{ formatRole(user.role) }} · {{ user.isTrusted ? 'доверенный' : 'без доверия' }} ·
          {{ user.isEmailConfirmed ? 'email подтверждён' : 'email не подтверждён' }}
        </p>

        <p class="text-muted">
          Имя: {{ user.displayName || 'не задано' }} · Контакты: {{ user.contacts || 'не заданы' }}
        </p>

        <div class="cluster">
          <RouterLink class="btn btn--secondary btn--compact" :to="`/admin/users/${user.id}`">
            Редактировать
          </RouterLink>
        </div>
      </article>
    </div>
  </section>
</template>
