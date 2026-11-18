<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { ApiError } from '../api/client'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import TextField from '../components/TextField.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const error = ref('')
const submitting = ref(false)

async function submit(): Promise<void> {
  error.value = ''
  submitting.value = true

  try {
    await auth.login(email.value, password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    await router.push(redirect)
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось войти.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="page page--narrow">
    <h1 class="page__title">Вход</h1>

    <form class="stack" @submit.prevent="submit">
      <TextField
        v-model="email"
        label="Email"
        input-id="login-email"
        type="email"
        autocomplete="email"
        required
      />

      <TextField
        v-model="password"
        label="Пароль"
        input-id="login-password"
        type="password"
        autocomplete="current-password"
        required
      />

      <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

      <AppButton variant="primary" type="submit" block :disabled="submitting">
        {{ submitting ? 'Входим…' : 'Войти' }}
      </AppButton>
    </form>

    <p class="text-muted">
      <RouterLink class="link" to="/forgot-password">Забыли пароль?</RouterLink>
    </p>

    <p class="text-muted">
      Нет аккаунта?
      <RouterLink class="link" to="/register">Зарегистрироваться</RouterLink>
    </p>
  </section>
</template>