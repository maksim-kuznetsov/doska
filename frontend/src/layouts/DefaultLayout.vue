<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
  <div class="layout">
    <header class="layout__header">
      <RouterLink class="layout__brand" to="/">Doska</RouterLink>
    </header>
    <main class="layout__main">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.layout {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.layout__header {
  padding: 1rem 1.5rem;
  border-bottom: 1px solid currentColor;
}

.layout__brand {
  font-weight: 600;
  text-decoration: none;
  color: inherit;
}

.layout__main {
  flex: 1;
  padding: 1.5rem;
}
</style>
