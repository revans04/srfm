<template>
  <div class="fullscreen bg-grey-1 text-center q-pa-md flex flex-center">
    <div>
      <h1 class="not-found__code">404</h1>
      <p class="text-h4 text-grey-7 q-mt-md q-mb-lg">Nothing here.</p>
      <q-btn
        color="primary"
        unelevated
        to="/"
        label="Go Home"
        no-caps
      />
    </div>
  </div>
</template>

<style scoped>
.not-found__code {
  font-size: 30vh;
  font-weight: 700;
  line-height: 1;
  color: var(--color-text-primary);
  margin: 0;
}
</style>

<script setup lang="ts">
//
</script>
