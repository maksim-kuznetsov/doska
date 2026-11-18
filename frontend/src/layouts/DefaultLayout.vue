<script setup lang="ts">
import { RouterView } from 'vue-router'

import SiteFooter from '../components/SiteFooter.vue'
import SiteHeader from '../components/SiteHeader.vue'
</script>

<template>
  <div class="app-shell">
    <SiteHeader />
    <main class="site-main">
      <RouterView />
    </main>
    <SiteFooter />
  </div>
</template>