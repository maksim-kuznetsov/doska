<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterLink, RouterView, useRouter } from 'vue-router'

import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

onMounted(() => {
  if (!auth.loaded) {
    void auth.load()
  }
})

async function logout(): Promise<void> {
  await auth.logout()
  await router.push('/')
}
</script>

<template>
  <div class="layout">
    <header class="layout__header">
      <RouterLink class="layout__brand" to="/">Doska</RouterLink>

      <nav class="layout__nav" aria-label="Основная навигация">
        <RouterLink to="/ads/new">Подать объявление</RouterLink>
        <RouterLink to="/ads/mine">Мои объявления</RouterLink>
        <RouterLink v-if="auth.canModerate" to="/moderation">Проверка</RouterLink>
        <RouterLink v-if="auth.canModerate" to="/complaints">Жалобы</RouterLink>
      </nav>

      <div class="layout__account">
        <template v-if="auth.user">
          <span class="muted">{{ auth.user.email }}</span>
          <button type="button" class="button" @click="logout">Выйти</button>
        </template>
        <template v-else>
          <RouterLink to="/login">Вход</RouterLink>
          <RouterLink to="/register">Регистрация</RouterLink>
        </template>
      </div>
    </header>

    <main class="layout__main">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.layout {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.layout__header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 1rem;
  padding: 1rem 1.5rem;
  border-bottom: 1px solid currentColor;
}

.layout__brand {
  font-weight: 600;
  text-decoration: none;
  color: inherit;
}

.layout__nav {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
}

.layout__nav a {
  color: inherit;
}

.layout__account {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-left: auto;
}

.layout__account a {
  color: inherit;
}

.layout__main {
  flex: 1;
  padding: 1.5rem;
}
</style>