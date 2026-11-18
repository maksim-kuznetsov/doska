<script setup lang="ts">
/** Раскладка формы: поля и боковая колонка с действием. */
</script>

<template>
  <div class="form-layout">
    <div class="form-layout__fields">
      <slot />
    </div>
    <aside class="form-layout__aside">
      <div class="form-layout__aside-inner">
        <slot name="aside" />
      </div>
    </aside>
  </div>
</template>