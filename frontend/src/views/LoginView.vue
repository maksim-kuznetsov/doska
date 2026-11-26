<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { messageOf } from '../api/errors'
import { googleErrorMessage, googleStartUrl } from '../api/googleAuth'
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
const googleHref = computed(() => googleStartUrl(redirect.value))
const googleError = computed(() =>
  route.query.google === 'error' ? googleErrorMessage(route.query.reason) : '',
)

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

    <AppMessage v-if="googleError" variant="error">{{ googleError }}</AppMessage>

    <template v-if="auth.googleEnabled">
      <p class="text-muted">Или войдите без пароля:</p>
      <a class="btn btn--secondary btn--block" :href="googleHref">Войти через Google</a>
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