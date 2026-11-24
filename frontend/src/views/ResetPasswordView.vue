<script setup lang="ts">
import { ref } from 'vue'
import { useRoute } from 'vue-router'

import { api } from '../api/client'
import { messageOf } from '../api/errors'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import FormErrors from '../components/FormErrors.vue'
import FormSection from '../components/FormSection.vue'
import PasswordHints from '../components/PasswordHints.vue'
import TextField from '../components/TextField.vue'
import type { FormError } from '../components/formErrors'

const route = useRoute()

const token = typeof route.query.token === 'string' ? route.query.token : ''
const password = ref('')
const confirmation = ref('')
const error = ref('')
const done = ref(false)
const submitting = ref(false)
const formErrors = ref<FormError[]>([])

function validate(): FormError[] {
  const errors: FormError[] = []

  if (password.value.length < 8) {
    errors.push({ fieldId: 'reset-password', label: 'Пароль должен быть не короче 8 символов.' })
  }

  if (password.value.length > 0 && !/[^0-9]/.test(password.value)) {
    errors.push({ fieldId: 'reset-password', label: 'Пароль не должен состоять только из цифр.' })
  }

  if (password.value !== confirmation.value) {
    errors.push({ fieldId: 'reset-confirmation', label: 'Пароли должны совпадать.' })
  }

  return errors
}

async function submit(): Promise<void> {
  error.value = ''
  formErrors.value = validate()

  if (formErrors.value.length > 0 || done.value) {
    return
  }

  submitting.value = true

  try {
    await api.auth.resetPassword(token, password.value)
    done.value = true
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось изменить пароль.')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="page page--narrow">
    <h1 class="page__title">Сброс пароля</h1>

    <AppMessage v-if="token.length === 0" variant="error">
      В ссылке нет кода сброса. Запросите новую ссылку.
    </AppMessage>

    <template v-else-if="done">
      <AppMessage variant="success">
        Пароль изменён. Теперь можно войти с новым паролем.
      </AppMessage>
      <p class="text-muted">
        <RouterLink class="link" to="/login">Перейти ко входу</RouterLink>
      </p>
    </template>

    <form v-else class="stack" @submit.prevent="submit">
      <FormErrors :errors="formErrors" />

      <FormSection title="Новый пароль">
        <TextField
          v-model="password"
          label="Новый пароль"
          input-id="reset-password"
          type="password"
          autocomplete="new-password"
          required
        />
        <TextField
          v-model="confirmation"
          label="Повторите пароль"
          input-id="reset-confirmation"
          type="password"
          autocomplete="new-password"
          required
        />
        <PasswordHints :password="password" :confirmation="confirmation" />
      </FormSection>

      <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

      <AppButton variant="primary" type="submit" block :disabled="submitting">
        {{ submitting ? 'Сохраняем…' : 'Сохранить пароль' }}
      </AppButton>
    </form>

    <p class="text-muted">
      <RouterLink class="link" to="/forgot-password">Запросить новую ссылку</RouterLink>
    </p>
  </section>
</template>