<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { ApiError } from '../api/client'
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
    <h1>Вход</h1>

    <form class="form" @submit.prevent="submit">
      <label class="field">
        <span class="field__label">Email</span>
        <input v-model="email" class="field__input" type="email" autocomplete="email" required />
      </label>

      <label class="field">
        <span class="field__label">Пароль</span>
        <input
          v-model="password"
          class="field__input"
          type="password"
          autocomplete="current-password"
          required
        />
      </label>

      <p v-if="error" class="error">{{ error }}</p>

      <button class="button button--primary" type="submit" :disabled="submitting">
        {{ submitting ? 'Входим…' : 'Войти' }}
      </button>
    </form>

    <p class="muted">
      Нет аккаунта?
      <RouterLink to="/register">Зарегистрироваться</RouterLink>
    </p>
  </section>
</template>