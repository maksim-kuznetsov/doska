<script setup lang="ts">
import { ref } from 'vue'

import { api } from '../api/client'
import { messageOf } from '../api/errors'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import TextField from '../components/TextField.vue'

const email = ref('')
const error = ref('')
const sent = ref(false)
const submitting = ref(false)

async function submit(): Promise<void> {
  error.value = ''
  sent.value = false
  submitting.value = true

  try {
    await api.auth.forgotPassword(email.value)
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
    <h1 class="page__title">Забыли пароль?</h1>

    <p class="text-muted">
      Укажите email — если адрес зарегистрирован, мы отправим ссылку для сброса пароля.
    </p>

    <form class="stack" @submit.prevent="submit">
      <TextField
        v-model="email"
        label="Email"
        input-id="forgot-email"
        type="email"
        autocomplete="email"
        required
      />

      <AppMessage v-if="sent" variant="success">
        Если адрес зарегистрирован, письмо со ссылкой отправлено. Проверьте почту.
      </AppMessage>
      <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

      <AppButton variant="primary" type="submit" block :disabled="submitting">
        {{ submitting ? 'Отправляем…' : 'Отправить ссылку' }}
      </AppButton>
    </form>

    <p class="text-muted">
      <RouterLink class="link" to="/login">Вернуться ко входу</RouterLink>
    </p>
  </section>
</template>