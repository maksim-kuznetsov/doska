<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'

import { api, type AdminUserUpdatePayload } from '../api/client'
import { messageOf } from '../api/errors'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import Breadcrumbs from '../components/Breadcrumbs.vue'
import CheckboxField from '../components/CheckboxField.vue'
import FormLayout from '../components/FormLayout.vue'
import FormSection from '../components/FormSection.vue'
import SelectField from '../components/SelectField.vue'
import TextAreaField from '../components/TextAreaField.vue'
import TextField from '../components/TextField.vue'

const route = useRoute()
const userId = String(route.params.id)

const roleOptions = [
  { value: 'User', label: 'Пользователь' },
  { value: 'Moderator', label: 'Модератор' },
  { value: 'Admin', label: 'Администратор' },
]

const email = ref('')
const loading = ref(true)
const submitting = ref(false)
const error = ref('')
const notice = ref('')

const form = reactive({
  role: 'User',
  isTrusted: false,
  isBlocked: false,
  isEmailConfirmed: false,
  displayName: '',
  contacts: '',
})

function apply(update: {
  role: string
  isTrusted: boolean
  isBlocked: boolean
  isEmailConfirmed: boolean
  displayName: string | null
  contacts: string | null
}): void {
  form.role = update.role
  form.isTrusted = update.isTrusted
  form.isBlocked = update.isBlocked
  form.isEmailConfirmed = update.isEmailConfirmed
  form.displayName = update.displayName ?? ''
  form.contacts = update.contacts ?? ''
}

onMounted(async () => {
  try {
    const user = await api.adminUsers.get(userId)
    email.value = user.email
    apply(user)
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось загрузить пользователя.')
  } finally {
    loading.value = false
  }
})

async function submit(): Promise<void> {
  error.value = ''
  notice.value = ''
  submitting.value = true

  const payload: AdminUserUpdatePayload = {
    role: form.role,
    isTrusted: form.isTrusted,
    isBlocked: form.isBlocked,
    isEmailConfirmed: form.isEmailConfirmed,
    displayName: form.displayName.trim() === '' ? null : form.displayName,
    contacts: form.contacts.trim() === '' ? null : form.contacts,
  }

  try {
    const saved = await api.adminUsers.update(userId, payload)
    email.value = saved.email
    apply(saved)
    notice.value = 'Изменения сохранены.'
  } catch (cause) {
    error.value = messageOf(cause, 'Не удалось сохранить пользователя.')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="page">
    <Breadcrumbs
      :items="[
        { label: 'Главная', to: '/' },
        { label: 'Пользователи', to: '/admin/users' },
      ]"
    />

    <div class="page__head">
      <h1 class="page__title">Редактировать пользователя</h1>
      <RouterLink class="btn btn--secondary btn--compact" to="/admin/users">
        К списку
      </RouterLink>
    </div>

    <p v-if="loading" class="text-muted">Загружаем…</p>
    <AppMessage v-if="notice" variant="success">{{ notice }}</AppMessage>
    <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>

    <form v-if="!loading" @submit.prevent="submit">
      <FormLayout>
        <FormSection title="Учётная запись">
          <TextField
            :model-value="email"
            label="Email"
            input-id="admin-user-email"
            type="email"
            disabled
            hint="Email меняет только сам пользователь."
          />
          <SelectField v-model="form.role" label="Роль" input-id="admin-user-role">
            <option v-for="option in roleOptions" :key="option.value" :value="option.value">
              {{ option.label }}
            </option>
          </SelectField>
          <CheckboxField
            v-model="form.isEmailConfirmed"
            label="Email подтверждён"
            input-id="admin-user-email-confirmed"
          />
        </FormSection>

        <FormSection title="Профиль">
          <TextField
            v-model="form.displayName"
            label="Отображаемое имя"
            input-id="admin-user-display-name"
            :maxlength="100"
          />
          <TextAreaField
            v-model="form.contacts"
            label="Контакты"
            input-id="admin-user-contacts"
            :rows="4"
            :maxlength="500"
          />
        </FormSection>

        <FormSection title="Доступ">
          <CheckboxField
            v-model="form.isBlocked"
            label="Пользователь заблокирован"
            input-id="admin-user-blocked"
          />
          <CheckboxField
            v-model="form.isTrusted"
            label="Доверенный автор"
            input-id="admin-user-trusted"
          />
        </FormSection>

        <template #aside>
          <p class="form-layout__aside-text">
            Изменения вступают в силу сразу. Блокировка завершает активную сессию пользователя.
          </p>
          <AppMessage v-if="error" variant="error">{{ error }}</AppMessage>
          <div class="form-layout__actions">
            <AppButton variant="primary" type="submit" block :disabled="submitting">
              {{ submitting ? 'Сохраняем…' : 'Сохранить' }}
            </AppButton>
          </div>
        </template>
      </FormLayout>
    </form>
  </section>
</template>
