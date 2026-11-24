<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'

import { messageOf } from '../api/errors'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import PasswordHints from '../components/PasswordHints.vue'
import TextField from '../components/TextField.vue'
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
    error.value = messageOf(cause, 'Не удалось зарегистрироваться.')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="page page--narrow">
    <h1 class="page__title">Регистрация</h1>

    <form class="stack" @submit.prevent="submit">
      <TextField
        v-model="email"
        label="Email"
        input-id="register-email"
        type="email"
        autocomplete="email"
        required
      />

      <TextField
        v-model="password"
        label="Пароль"
        input-id="register-password"
        type="password"
        autocomplete="new-password"
        required
      />

      <TextField
        v-model="confirmation"
        label="Повторите пароль"
        input-id="register-confirmation"
        type="password"
        autocomplete="new-password"
        required
      />

      <PasswordHints :password="password" :confirmation="confirmation" />

      <p class="text-muted">
        Минимум 8 символов и не только цифры. После регистрации подтвердите email — до этого
        подача объявлений и жалобы недоступны.
      </p>

      <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

      <AppButton variant="primary" type="submit" block :disabled="submitting">
        {{ submitting ? 'Создаём…' : 'Зарегистрироваться' }}
      </AppButton>
    </form>

    <p class="text-muted">
      Уже есть аккаунт?
      <RouterLink class="link" to="/login">Войти</RouterLink>
    </p>
  </section>
</template>