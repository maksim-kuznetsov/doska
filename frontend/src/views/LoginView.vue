<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { messageOf } from '../api/errors'
import { externalErrorMessage, externalStartUrl } from '../api/externalAuth'
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

const redirect = computed(() =>
  typeof route.query.redirect === 'string' ? route.query.redirect : null,
)
const enabledProviders = computed(() => auth.providers.filter((provider) => provider.enabled))
const providerError = computed(() => {
  const failed = auth.providers.find((provider) => route.query[provider.id] === 'error')
  return failed ? externalErrorMessage(failed.id, route.query.reason) : ''
})

async function submit(): Promise<void> {
  error.value = ''
  submitting.value = true

  try {
    await auth.login(email.value, password.value)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    await router.push(redirect)
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось войти.')
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

    <AppMessage v-if="providerError" variant="error">{{ providerError }}</AppMessage>

    <template v-if="enabledProviders.length > 0">
      <p class="text-muted">Или войдите без пароля:</p>
      <a
        v-for="provider in enabledProviders"
        :key="provider.id"
        class="btn btn--secondary btn--block"
        :href="externalStartUrl(provider.id, redirect)"
      >
        Войти через {{ provider.name }}
      </a>
    </template>

    <p class="text-muted">
      Продолжая, вы принимаете
      <RouterLink class="link" to="/terms">условия использования</RouterLink>
      и
      <RouterLink class="link" to="/privacy">политику конфиденциальности</RouterLink>.
    </p>

    <p class="text-muted">
      <RouterLink class="link" to="/forgot-password">Забыли пароль?</RouterLink>
    </p>

    <p class="text-muted">
      <RouterLink class="link" to="/resend-confirmation">Не пришло письмо подтверждения?</RouterLink>
    </p>

    <p class="text-muted">
      Нет аккаунта?
      <RouterLink class="link" to="/register">Зарегистрироваться</RouterLink>
    </p>
  </section>
</template>