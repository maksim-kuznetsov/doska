<script setup lang="ts">
import { ref } from 'vue'

import type {
  AdCard as AdCardModel,
  AdSummary,
  ModerationQueueItem,
  SectionOption,
} from '../api/client'
import ActionLink from '../components/ActionLink.vue'
import AdCard from '../components/AdCard.vue'
import AdRow from '../components/AdRow.vue'
import AppButton from '../components/AppButton.vue'
import AppMessage from '../components/AppMessage.vue'
import Breadcrumbs from '../components/Breadcrumbs.vue'
import CategoryMenu from '../components/CategoryMenu.vue'
import CategoryStrip from '../components/CategoryStrip.vue'
import CategoryTiles from '../components/CategoryTiles.vue'
import CitySelect from '../components/CitySelect.vue'
import EmptyState from '../components/EmptyState.vue'
import FilterPanel from '../components/FilterPanel.vue'
import FormErrors from '../components/FormErrors.vue'
import FormGrid from '../components/FormGrid.vue'
import FormGridItem from '../components/FormGridItem.vue'
import FormLayout from '../components/FormLayout.vue'
import FormSection from '../components/FormSection.vue'
import ImageGallery from '../components/ImageGallery.vue'
import ModerationQueueList from '../components/ModerationQueueList.vue'
import Pagination from '../components/Pagination.vue'
import PasswordHints from '../components/PasswordHints.vue'
import PhotoUploader from '../components/PhotoUploader.vue'
import PriceRange from '../components/PriceRange.vue'
import PromoBanner from '../components/PromoBanner.vue'
import ResultsBar from '../components/ResultsBar.vue'
import SelectField from '../components/SelectField.vue'
import StatusBadge from '../components/StatusBadge.vue'
import SubsectionSelect from '../components/SubsectionSelect.vue'
import TextAreaField from '../components/TextAreaField.vue'
import TextField from '../components/TextField.vue'
import UserMenu from '../components/UserMenu.vue'

const palette = [
  { name: 'Основной', value: '#00aaff' },
  { name: 'Опасность', value: '#ff6163' },
  { name: 'Успех', value: '#97cf26' },
  { name: 'Акцент', value: '#a169f7' },
  { name: 'Текст', value: '#000000' },
  { name: 'Поверхность', value: '#ffffff' },
  { name: 'Фон страницы', value: '#f5f5f7' },
]

const sections: SectionOption[] = [
  {
    id: 's1',
    name: 'Транспорт',
    subsections: [
      { id: 'c1', name: 'Автомобили' },
      { id: 'c2', name: 'Мотоциклы' },
    ],
  },
  {
    id: 's2',
    name: 'Недвижимость',
    subsections: [{ id: 'c3', name: 'Квартиры' }],
  },
]

const cities = [
  { id: 'city-1', name: 'Москва' },
  { id: 'city-2', name: 'Санкт-Петербург' },
]

const adCard: AdCardModel = {
  id: 'ad-1',
  title: 'Велосипед городской',
  description: 'Аккуратный городской велосипед, почти не использовался.',
  price: 25000,
  cityName: 'Москва',
  categoryName: 'Автомобили',
  sectionName: 'Транспорт',
  publishedAt: '2026-11-18T10:00:00Z',
  thumbnailUrl: null,
}

const adSummary: AdSummary = {
  id: 'ad-1',
  title: 'Велосипед городской',
  description: 'Аккуратный городской велосипед.',
  price: 25000,
  cityId: 'city-1',
  categoryId: 'c1',
  contacts: '+7 900 000-00-00',
  status: 'Rejected',
  moderationReason: 'Фотография не соответствует товару.',
  createdAt: '2026-11-18T10:00:00Z',
  publishedAt: null,
  photos: [],
}

const queue: ModerationQueueItem[] = [
  {
    id: 'ad-2',
    title: 'Ноутбук рабочий',
    authorEmail: 'author@example.com',
    submittedAt: '2026-11-18T11:00:00Z',
  },
]

const guideFields = ref({ title: 'Название объявления', description: 'Описание', category: '', city: '', min: '1000', max: '50000' })
const guidePhotos = ref<File[]>([])
const guideSort = ref<'newest' | 'price_asc' | 'price_desc'>('newest')
const guidePassword = ref('secret123')

const guideErrors = [
  { fieldId: 'guide-title', label: 'Укажите заголовок.' },
  { fieldId: 'guide-price', label: 'Укажите цену.' },
]
</script>

<template>
  <section class="page guide">
    <div>
      <h1 class="page__title">Дизайн-система</h1>
      <p class="page__subtitle">
        Страница доступна только в окружении Development и показывает токены и компоненты.
      </p>
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Токены палитры</h2>
      <div class="guide__swatches">
        <div v-for="token in palette" :key="token.name" class="guide__swatch">
          <span class="guide__swatch-color" :style="{ background: token.value }"></span>
          <strong>{{ token.name }}</strong>
          <span>{{ token.value }}</span>
        </div>
      </div>
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Компонентные токены</h2>
      <div class="guide__row">
        <span class="guide__note">--card-media-aspect: 4 / 3</span>
        <span class="guide__note">--grid-column-min: 15rem</span>
        <span class="guide__note">--grid-gap: var(--space-5)</span>
        <span class="guide__note">--price-font-size: 1.25rem</span>
        <span class="guide__note">--category-strip-height: 2.75rem</span>
        <span class="guide__note">--control-height: 2.75rem</span>
      </div>
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Шапка и разделы</h2>
      <PromoBanner />
      <div class="guide__row">
        <CitySelect :model-value="guideFields.city" :cities="cities" />
      </div>
      <CategoryStrip :sections="sections" />
      <CategoryTiles :sections="sections" />
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Кнопки</h2>
      <div class="guide__row">
        <AppButton variant="primary">Основная</AppButton>
        <AppButton variant="secondary">Второстепенная</AppButton>
        <AppButton variant="ghost">Тихая</AppButton>
        <AppButton variant="danger">Опасная</AppButton>
        <AppButton variant="primary" size="compact">Компактная</AppButton>
        <AppButton variant="primary" disabled>Недоступная</AppButton>
      </div>
      <div class="guide__row">
        <ActionLink to="/ads/new">Подать объявление</ActionLink>
      </div>
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Поля и формы</h2>
      <FormErrors :errors="guideErrors" />
      <FormGrid>
        <FormGridItem>
          <TextField
            v-model="guideFields.title"
            label="Заголовок"
            input-id="guide-title"
            :maxlength="200"
          />
        </FormGridItem>
        <FormGridItem>
          <SelectField v-model="guideFields.city" label="Город" input-id="guide-city">
            <option value="">Все города</option>
            <option v-for="city in cities" :key="city.id" :value="city.id">{{ city.name }}</option>
          </SelectField>
        </FormGridItem>
        <FormGridItem wide>
          <TextAreaField
            v-model="guideFields.description"
            label="Описание"
            input-id="guide-description"
            :maxlength="500"
          />
        </FormGridItem>
        <FormGridItem wide>
          <SubsectionSelect v-model="guideFields.category" :sections="sections" input-id="guide-category" />
        </FormGridItem>
      </FormGrid>
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Раскладка формы и зона загрузки</h2>
      <FormLayout>
        <FormSection title="Фотографии">
          <PhotoUploader v-model:files="guidePhotos" input-id="guide-photos" />
        </FormSection>
        <template #aside>
          <p class="form-layout__aside-text">Основное действие остаётся в боковой колонке.</p>
          <div class="form-layout__actions">
            <AppButton variant="primary" block>Отправить</AppButton>
          </div>
        </template>
      </FormLayout>
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Карточки, строки и галерея</h2>
      <div class="ad-grid">
        <AdCard :ad="adCard" />
      </div>
      <AdRow :ad="adSummary">
        <template #actions>
          <AppButton variant="secondary" size="compact">Редактировать</AppButton>
          <AppButton variant="danger" size="compact">Удалить</AppButton>
        </template>
      </AdRow>
      <ImageGallery :photos="[]" />
      <div class="guide__row">
        <StatusBadge status="OnModeration" />
        <StatusBadge status="Active" />
        <StatusBadge status="Rejected" />
        <StatusBadge status="Hidden" />
        <StatusBadge status="Sold" />
        <StatusBadge status="Archived" />
      </div>
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Сообщения</h2>
      <AppMessage variant="success">Действие выполнено.</AppMessage>
      <AppMessage variant="error">Не удалось выполнить действие.</AppMessage>
      <AppMessage variant="info">Информационное сообщение.</AppMessage>
      <AppMessage variant="warning">Предупреждение.</AppMessage>
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Навигация</h2>
      <Breadcrumbs
        :items="[
          { label: 'Главная', to: '/' },
          { label: 'Транспорт', to: '/?categoryId=s1' },
          { label: 'Автомобили' },
        ]"
      />
      <Pagination :page="2" :total-pages="5" :total-count="57" />
      <EmptyState title="Ничего не найдено" description="Попробуйте изменить условия поиска." />
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Фильтры и результаты</h2>
      <FilterPanel
        v-model:query="guideFields.title"
        :category-id="guideFields.category"
        :city-id="guideFields.city"
        :sort="guideSort"
        :sections="sections"
        :cities="cities"
        :min-price="guideFields.min"
        :max-price="guideFields.max"
        @update:category-id="guideFields.category = $event"
        @update:city-id="guideFields.city = $event"
        @update:min-price="guideFields.min = $event"
        @update:max-price="guideFields.max = $event"
      />
      <PriceRange v-model:min="guideFields.min" v-model:max="guideFields.max" />
      <ResultsBar :total="57" v-model:sort="guideSort" />
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Модерация</h2>
      <ModerationQueueList :items="queue" />
    </div>

    <div class="guide__section">
      <h2 class="guide__section-title">Подсказки к паролю и меню</h2>
      <PasswordHints :password="guidePassword" :confirmation="guidePassword" />
      <div class="guide__row">
        <CategoryMenu :sections="sections" />
        <UserMenu email="admin@example.com" :can-moderate="true" :is-admin="true" />
      </div>
    </div>
  </section>
</template>