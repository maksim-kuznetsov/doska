<script setup lang="ts">
import { ref } from 'vue'

import { api } from '../api/client'
import { messageOf } from '../api/errors'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import TextField from '../components/TextField.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()

const email = ref(auth.user?.email ?? '')
const error = ref('')
const sent = ref(false)
const submitting = ref(false)

async function submit(): Promise<void> {
  error.value = ''
  sent.value = false
  submitting.value = true

  try {
    await api.auth.resendConfirmation(email.value)
    sent.value = true
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось отправить письмо.')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="page page--narrow">
    <h1 class="page__title">Повторное подтверждение email</h1>

    <p class="text-muted">
      Укажите email — если адрес зарегистрирован и ещё не подтверждён, мы отправим новое письмо со
      ссылкой. Предыдущие ссылки подтверждения перестанут действовать.
    </p>

    <form class="stack" @submit.prevent="submit">
      <TextField
        v-model="email"
        label="Email"
        input-id="resend-confirmation-email"
        type="email"
        autocomplete="email"
        required
      />

      <AppMessage v-if="sent" variant="success">
        Если адрес зарегистрирован и ещё не подтверждён, письмо отправлено. Проверьте почту.
      </AppMessage>
      <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

      <AppButton variant="primary" type="submit" block :disabled="submitting">
        {{ submitting ? 'Отправляем…' : 'Отправить письмо' }}
      </AppButton>
    </form>

    <p class="text-muted">
      <RouterLink class="link" to="/login">Вернуться ко входу</RouterLink>
    </p>
  </section>
</template>
