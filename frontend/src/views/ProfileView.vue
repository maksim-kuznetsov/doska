<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'

import { api } from '../api/client'
import { messageOf } from '../api/errors'
import { externalErrorMessage, externalLinkUrl } from '../api/externalAuth'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import FormSection from '../components/FormSection.vue'
import PasswordHints from '../components/PasswordHints.vue'
import TextAreaField from '../components/TextAreaField.vue'
import TextField from '../components/TextField.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()

const providerMessage = ref('')
const providerError = ref('')
const needsPassword = computed(() => auth.user?.hasPassword === false)

const linkableProviders = computed(() =>
  auth.providers.filter(
    (provider) => provider.enabled || (auth.user?.linkedProviders.includes(provider.id) ?? false),
  ),
)

onMounted(() => {
  const linked = auth.providers.find((provider) => route.query[provider.id] === 'linked')
  const failed = auth.providers.find((provider) => route.query[provider.id] === 'error')

  if (linked) {
    providerMessage.value = `${linked.name} привязан к аккаунту.`
  } else if (failed) {
    providerError.value = externalErrorMessage(failed.id, route.query.reason)
  } else {
    return
  }

  void router.replace({ name: 'profile' })
})

const profile = reactive({
  displayName: auth.user?.displayName ?? '',
  contacts: auth.user?.contacts ?? '',
})
const profileError = ref('')
const profileSaved = ref(false)
const savingProfile = ref(false)

const password = reactive({
  current: '',
  next: '',
  confirmation: '',
})
const passwordError = ref('')
const passwordSaved = ref('')
const savingPassword = ref(false)

/** Пустые значения уходят на сервер как `null`, чтобы очистить поле профиля. */
function normalized(value: string): string | null {
  const trimmed = value.trim()
  return trimmed.length === 0 ? null : trimmed
}

async function saveProfile(): Promise<void> {
  profileError.value = ''
  profileSaved.value = false
  savingProfile.value = true

  try {
    await auth.updateProfile(normalized(profile.displayName), normalized(profile.contacts))
    profileSaved.value = true
  } catch (cause) {
    profileError.value = messageOf(cause, 'Не удалось сохранить профиль.')
  } finally {
    savingProfile.value = false
  }
}

async function changePassword(): Promise<void> {
  passwordError.value = ''
  passwordSaved.value = ''
  savingPassword.value = true
  const firstPassword = needsPassword.value

  try {
    await api.account.changePassword(password.current, password.next)

    if (firstPassword) {
      await auth.load()
    }

    passwordSaved.value = firstPassword ? 'Пароль задан.' : 'Пароль изменён.'
    password.current = ''
    password.next = ''
    password.confirmation = ''
  } catch (cause) {
    passwordError.value = messageOf(cause, 'Не удалось сменить пароль.')
  } finally {
    savingPassword.value = false
  }
}
</script>

<template>
  <section class="page page--narrow">
    <h1 class="page__title">Профиль</h1>

    <form class="stack" @submit.prevent="saveProfile">
      <FormSection title="Контакты">
        <TextField
          v-model="profile.displayName"
          label="Отображаемое имя"
          input-id="profile-display-name"
          :maxlength="100"
        />
        <TextAreaField
          v-model="profile.contacts"
          label="Контакты"
          input-id="profile-contacts"
          :rows="3"
          :maxlength="500"
        />
        <p class="text-muted">
          Имя и контакты подставляются в форму нового объявления; их можно изменить для
          конкретного объявления.
        </p>
      </FormSection>

      <AppMessage v-if="profileError" variant="error">{{ profileError }}</AppMessage>
      <AppMessage v-else-if="profileSaved" variant="success">Профиль сохранён.</AppMessage>

      <AppButton variant="primary" type="submit" block :disabled="savingProfile">
        {{ savingProfile ? 'Сохраняем…' : 'Сохранить профиль' }}
      </AppButton>
    </form>

    <FormSection v-if="linkableProviders.length > 0" title="Вход без пароля">
      <template v-for="provider in linkableProviders" :key="provider.id">
        <p v-if="auth.user?.linkedProviders.includes(provider.id)" class="text-muted">
          {{ provider.name }} привязан к аккаунту: можно входить без пароля.
        </p>
        <template v-else>
          <p class="text-muted">
            Привяжите {{ provider.name }}, чтобы входить в аккаунт без пароля.
          </p>
          <a class="btn btn--secondary" :href="externalLinkUrl(provider.id)">
            Привязать {{ provider.name }}
          </a>
        </template>
      </template>

      <AppMessage v-if="providerMessage" variant="success">{{ providerMessage }}</AppMessage>
      <AppMessage v-if="providerError" variant="error">{{ providerError }}</AppMessage>
    </FormSection>

    <form class="stack" @submit.prevent="changePassword">
      <FormSection :title="needsPassword ? 'Задать пароль' : 'Смена пароля'">
        <TextField
          v-if="!needsPassword"
          v-model="password.current"
          label="Текущий пароль"
          input-id="profile-current-password"
          type="password"
          autocomplete="current-password"
          required
        />
        <TextField
          v-model="password.next"
          label="Новый пароль"
          input-id="profile-new-password"
          type="password"
          autocomplete="new-password"
          required
        />
        <TextField
          v-model="password.confirmation"
          label="Повторите новый пароль"
          input-id="profile-confirm-password"
          type="password"
          autocomplete="new-password"
          required
        />
        <PasswordHints :password="password.next" :confirmation="password.confirmation" />
      </FormSection>

      <AppMessage v-if="passwordError" variant="error">{{ passwordError }}</AppMessage>
      <AppMessage v-else-if="passwordSaved" variant="success">{{ passwordSaved }}</AppMessage>

      <AppButton variant="primary" type="submit" block :disabled="savingPassword">
        {{
          savingPassword
            ? 'Сохраняем…'
            : needsPassword
              ? 'Задать пароль'
              : 'Сменить пароль'
        }}
      </AppButton>
    </form>
  </section>
</template>
