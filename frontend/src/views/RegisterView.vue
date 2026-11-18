<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

import { ApiError } from '../api/client'
import PasswordHints from '../components/PasswordHints.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const confirmation = ref('')
const error = ref('')
const submitting = ref(false)

async function submit(): Promise<void> {
  error.value = ''
  submitting.value = true

  try {
    await auth.register(email.value, password.value)
    await router.push('/ads/mine')
  } catch (cause) {
    error.value = cause instanceof ApiError ? cause.message : 'Не удалось зарегистрироваться.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="page page--narrow">
    <h1>Регистрация</h1>

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
          autocomplete="new-password"
          required
        />
      </label>

      <label class="field">
        <span class="field__label">Повторите пароль</span>
        <input
          v-model="confirmation"
          class="field__input"
          type="password"
          autocomplete="new-password"
          required
        />
      </label>

      <PasswordHints :password="password" :confirmation="confirmation" />

      <p class="muted">
        Минимум 8 символов и не только цифры. После регистрации подтвердите email — до этого
        подача объявлений и жалобы недоступны.
      </p>

      <p v-if="error" class="error">{{ error }}</p>

      <button class="button button--primary" type="submit" :disabled="submitting">
        {{ submitting ? 'Создаём…' : 'Зарегистрироваться' }}
      </button>
    </form>

    <p class="muted">
      Уже есть аккаунт?
      <RouterLink to="/login">Войти</RouterLink>
    </p>
  </section>
</template>