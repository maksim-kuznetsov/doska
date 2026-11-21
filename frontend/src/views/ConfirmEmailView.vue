<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

import { ApiError, api } from '../api/client'
import AppMessage from '../components/AppMessage.vue'

const route = useRoute()
const state = ref<'pending' | 'done' | 'failed'>('pending')
const message = ref('Подтверждаем email…')

onMounted(async () => {
  const token = typeof route.query.token === 'string' ? route.query.token : ''

  if (token.length === 0) {
    state.value = 'failed'
    message.value = 'В ссылке нет кода подтверждения.'
    return
  }

  try {
    await api.auth.confirmEmail(token)
    state.value = 'done'
    message.value = 'Email подтверждён. Теперь можно подавать объявления и жалобы.'
  } catch (cause) {
    state.value = 'failed'
    message.value = cause instanceof ApiError ? cause.message : 'Не удалось подтвердить email.'
  }
})
</script>

<template>
  <section class="page page--narrow">
    <h1 class="page__title">Подтверждение email</h1>

    <AppMessage v-if="state === 'failed'" variant="error">{{ message }}</AppMessage>
    <AppMessage v-else-if="state === 'done'" variant="success">{{ message }}</AppMessage>
    <p v-else class="text-muted">{{ message }}</p>

    <p v-if="state === 'failed'">
      <RouterLink class="link" to="/resend-confirmation">Запросить новое письмо</RouterLink>
    </p>

    <p>
      <RouterLink class="link" to="/ads/mine">Перейти к объявлениям</RouterLink>
    </p>
  </section>
</template>