<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { ApiError, api, type AdminUserView } from '../api/client'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import EmptyState from '../components/EmptyState.vue'
import SelectField from '../components/SelectField.vue'
import StatusBadge from '../components/StatusBadge.vue'

const users = ref<AdminUserView[]>([])
const roles = ref<Record<string, string>>({})
const error = ref('')
const notice = ref('')
const loading = ref(false)

const roleOptions = [
  { value: 'User', label: 'Пользователь' },
  { value: 'Moderator', label: 'Модератор' },
  { value: 'Admin', label: 'Администратор' },
]

function messageOf(cause: unknown, fallback: string): string {
  return cause instanceof ApiError ? cause.message : fallback
}

function rebuildRoles(): void {
  const next: Record<string, string> = {}

  for (const user of users.value) {
    next[user.id] = user.role
  }

  roles.value = next
}

async function load(): Promise<void> {
  loading.value = true

  try {
    users.value = await api.adminUsers.list()
    rebuildRoles()
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось загрузить пользователей.')
  } finally {
    loading.value = false
  }
}

async function run(action: () => Promise<unknown>, success: string): Promise<void> {
  error.value = ''
  notice.value = ''

  try {
    await action()
    notice.value = success
    await load()
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось выполнить действие.')
  }
}

function saveRole(user: AdminUserView): void {
  const role = roles.value[user.id] ?? user.role
  void run(() => api.adminUsers.setRole(user.id, role), 'Роль обновлена.')
}

function toggleBlock(user: AdminUserView): void {
  void run(
    () => (user.isBlocked ? api.users.unblock(user.id) : api.users.block(user.id)),
    user.isBlocked ? 'Пользователь разблокирован.' : 'Пользователь заблокирован.',
  )
}

function formatRole(role: string): string {
  return roleOptions.find((option) => option.value === role)?.label ?? role
}

onMounted(load)
</script>

<template>
  <section class="page">
    <div class="page__head">
      <h1 class="page__title">Пользователи</h1>
      <span class="text-muted">Управление доступно администратору</span>
    </div>

    <AppMessage v-if="notice" variant="success">{{ notice }}</AppMessage>
    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <p v-if="loading" class="text-muted">Загружаем…</p>

    <EmptyState
      v-if="!loading && users.length === 0"
      title="Пользователей пока нет"
      description="Зарегистрированные пользователи появятся здесь."
    />

    <div v-if="users.length > 0" class="ad-list">
      <article v-for="user in users" :key="user.id" class="panel stack" data-test="admin-user">
        <div class="split">
          <h2 class="ad-row__title">{{ user.email }}</h2>
          <div class="cluster">
            <StatusBadge :status="user.isBlocked ? 'Hidden' : 'Active'" />
            <StatusBadge :status="user.isEmailConfirmed ? 'Active' : 'OnModeration'" />
          </div>
        </div>

        <p class="text-muted">
          Текущая роль: {{ formatRole(user.role) }} ·
          {{ user.isEmailConfirmed ? 'email подтверждён' : 'email не подтверждён' }}
        </p>

        <SelectField
          v-model="roles[user.id]"
          label="Роль"
          :input-id="`user-role-${user.id}`"
        >
          <option v-for="option in roleOptions" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </SelectField>

        <div class="cluster">
          <AppButton variant="secondary" size="compact" @click="saveRole(user)">
            Сохранить роль
          </AppButton>
          <AppButton
            :variant="user.isBlocked ? 'secondary' : 'danger'"
            size="compact"
            @click="toggleBlock(user)"
          >
            {{ user.isBlocked ? 'Разблокировать' : 'Заблокировать' }}
          </AppButton>
        </div>
      </article>
    </div>
  </section>
</template>