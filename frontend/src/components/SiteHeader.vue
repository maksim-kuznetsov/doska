<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { RouterLink, useRouter } from 'vue-router'

import { api, type SectionOption } from '../api/client'
import { useAppStore } from '../stores/app'
import { useAuthStore } from '../stores/auth'
import ActionLink from './ActionLink.vue'
import CategoryMenu from './CategoryMenu.vue'
import SearchBar from './SearchBar.vue'
import UserMenu from './UserMenu.vue'

const app = useAppStore()
const auth = useAuthStore()
const router = useRouter()

const sections = ref<SectionOption[]>([])
const query = ref('')

onMounted(async () => {
  if (!auth.loaded) {
    void auth.load()
  }

  try {
    sections.value = await api.catalog.sections()
  } catch {
    sections.value = []
  }
})

function search(): void {
  const text = query.value.trim()
  void router.push({ name: 'home', query: text.length > 0 ? { query: text } : {} })
}

async function logout(): Promise<void> {
  await auth.logout()
  await router.push('/')
}
</script>

<template>
  <header class="site-header">
    <div class="site-header__inner">
      <div class="site-header__lead">
        <RouterLink class="site-header__brand" to="/">{{ app.appName }}</RouterLink>
        <CategoryMenu :sections="sections" />
      </div>

      <div class="site-header__search">
        <SearchBar v-model="query" @submit="search" />
      </div>

      <nav class="site-header__nav" aria-label="Основная навигация">
        <ActionLink v-if="auth.isAuthenticated" to="/ads/new">Подать объявление</ActionLink>
      </nav>

      <div class="site-header__account">
        <template v-if="auth.user">
          <UserMenu
            :email="auth.user.email"
            :can-moderate="auth.canModerate"
            :is-admin="auth.isAdmin"
            @logout="logout"
          />
        </template>
        <template v-else>
          <RouterLink class="site-header__auth-link" to="/login">Войти</RouterLink>
          <RouterLink class="site-header__auth-link" to="/register">Регистрация</RouterLink>
        </template>
      </div>
    </div>
  </header>
</template>