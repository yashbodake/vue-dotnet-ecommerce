<template>
  <div class="not-found container">
    <span class="eyebrow">Error 404</span>
    <h1>This page has wandered off.</h1>
    <p class="lede">
      The page you were looking for doesn't exist, or may have moved. Let's get you back to the
      catalogue.
    </p>
    <div class="actions">
      <RouterLink to="/" class="btn btn-primary">Back to the catalogue</RouterLink>
      <RouterLink to="/cart" class="btn btn-ghost">View cart</RouterLink>
    </div>
  </div>
</template>

<script setup lang="ts"></script>

<style scoped>
.not-found {
  text-align: center;
  padding-block: var(--sp-9);
  max-width: 40rem;
  margin-inline: auto;
}
.not-found h1 {
  margin-top: var(--sp-4);
  font-size: var(--fs-3xl);
  max-width: 16ch;
  margin-inline: auto;
}
.lede {
  margin-top: var(--sp-4);
  color: var(--body);
  font-size: var(--fs-md);
  line-height: 1.7;
}
.actions {
  margin-top: var(--sp-7);
  display: flex;
  gap: var(--sp-3);
  justify-content: center;
  flex-wrap: wrap;
}
</style>
