<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'

import { api, type AdminUserView } from '../api/client'
import { messageOf } from '../api/errors'
import AppMessage from '../components/AppMessage.vue'
import Breadcrumbs from '../components/Breadcrumbs.vue'
import EmptyState from '../components/EmptyState.vue'
import Pagination from '../components/Pagination.vue'
import StatusBadge from '../components/StatusBadge.vue'

const users = ref<AdminUserView[]>([])
const page = ref(1)
const pageSize = ref(20)
const totalCount = ref(0)
const error = ref('')
const loading = ref(false)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize.value)))

const roleLabels: Record<string, string> = {
  User: 'Пользователь',
  Moderator: 'Модератор',
  Admin: 'Администратор',
}

function formatRole(role: string): string {
  return roleLabels[role] ?? role
}

async function load(targetPage = 1): Promise<void> {
  loading.value = true
  error.value = ''

  try {
    const result = await api.adminUsers.list(targetPage)
    users.value = result.items
    page.value = result.page
    pageSize.value = result.pageSize
    totalCount.value = result.totalCount
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось загрузить пользователей.')
  } finally {
    loading.value = false
  }
}

onMounted(() => load(1))
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

    <template v-if="!loading && users.length > 0">
      <div class="data-table-wrap">
        <table class="data-table" data-test="admin-users-table">
          <caption class="visually-hidden">Пользователи</caption>
          <thead>
            <tr>
              <th scope="col">Email</th>
              <th scope="col">Имя</th>
              <th scope="col">Роль</th>
              <th scope="col">Доверие</th>
              <th scope="col">Email подтверждён</th>
              <th scope="col">Статус</th>
              <th scope="col">Контакты</th>
              <th scope="col"><span class="visually-hidden">Действия</span></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="user in users" :key="user.id" data-test="admin-user">
              <td>{{ user.email }}</td>
              <td>{{ user.displayName || '—' }}</td>
              <td>{{ formatRole(user.role) }}</td>
              <td>{{ user.isTrusted ? 'Да' : 'Нет' }}</td>
              <td>{{ user.isEmailConfirmed ? 'Да' : 'Нет' }}</td>
              <td><StatusBadge :status="user.isBlocked ? 'Hidden' : 'Active'" /></td>
              <td>{{ user.contacts || '—' }}</td>
              <td>
                <RouterLink class="btn btn--secondary btn--compact" :to="`/admin/users/${user.id}`">
                  Редактировать
                </RouterLink>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <Pagination
        v-if="totalPages > 1"
        :page="page"
        :total-pages="totalPages"
        :total-count="totalCount"
        @change="load"
      />
    </template>
  </section>
</template>
