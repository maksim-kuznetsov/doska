<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { api, type CityOption, type SectionOption } from '../api/client'
import { useAppStore } from '../stores/app'
import { useAuthStore } from '../stores/auth'
import ActionLink from './ActionLink.vue'
import CategoryMenu from './CategoryMenu.vue'
import CategoryStrip from './CategoryStrip.vue'
import CitySelect from './CitySelect.vue'
import SearchBar from './SearchBar.vue'
import UserMenu from './UserMenu.vue'

const app = useAppStore()
const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const sections = ref<SectionOption[]>([])
const cities = ref<CityOption[]>([])
const query = ref('')

const selectedCityId = computed(() => {
  const value = route.query.cityId
  return typeof value === 'string' ? value : ''
})

onMounted(async () => {
  if (!auth.loaded) {
    void auth.load()
  }

  try {
    sections.value = await api.catalog.sections()
  } catch {
    sections.value = []
  }

  try {
    cities.value = await api.catalog.cities()
  } catch {
    cities.value = []
  }
})

function search(): void {
  const text = query.value.trim()
  void router.push({ name: 'home', query: text.length > 0 ? { query: text } : {} })
}

function changeCity(cityId: string): void {
  void router.push({ name: 'home', query: cityId ? { cityId } : {} })
}

async function logout(): Promise<void> {
  await auth.logout()
  await router.push('/')
}
</script>

<template>
  <header class="site-header">
    <div class="site-header__inner">
      <div class="site-header__main">
        <div class="site-header__lead">
          <RouterLink class="site-header__brand" to="/">{{ app.appName }}</RouterLink>
          <div class="site-header__menu">
            <CategoryMenu :sections="sections" />
          </div>
        </div>

        <div class="site-header__city">
          <CitySelect :model-value="selectedCityId" :cities="cities" @update:model-value="changeCity" />
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

      <div class="site-header__strip">
        <CategoryStrip :sections="sections" />
      </div>
    </div>
  </header>
</template>
