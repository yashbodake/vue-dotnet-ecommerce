<template>
  <div class="login">
    <h1>Sign in</h1>
    <p class="subtitle">Ecommerce Modern</p>

    <form class="login-form" @submit.prevent="onSubmit">
      <label class="field">
        <span>Email</span>
        <input
          v-model="email"
          type="email"
          name="email"
          autocomplete="username"
          required
          :disabled="submitting"
        />
      </label>

      <label class="field">
        <span>Password</span>
        <input
          v-model="password"
          type="password"
          name="password"
          autocomplete="current-password"
          required
          :disabled="submitting"
        />
      </label>

      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <button type="submit" class="submit" :disabled="submitting">
        {{ submitting ? 'Signing in…' : 'Sign in' }}
      </button>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useCartStore } from '../stores/cart'

const router = useRouter()
const authStore = useAuthStore()
const cartStore = useCartStore()

// Demo convenience pre-fill.
const email = ref('admin@legacy.local')
const password = ref('Admin123!')
const submitting = ref(false)
const error = ref<string | null>(null)

async function onSubmit(): Promise<void> {
  error.value = null
  submitting.value = true
  try {
    await authStore.login(email.value, password.value)
    await cartStore.mergeOnLogin()
    await cartStore.fetchCart(true)
    router.push('/')
  } catch (e) {
    const message = e && typeof e === 'object' && 'message' in e
      ? String((e as { message?: unknown }).message)
      : 'Login failed'
    error.value = message || 'Login failed'
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.login {
  max-width: 22rem;
  margin: 3rem auto;
  padding: 2rem;
  border: 1px solid var(--border, #e5e4e7);
  border-radius: 0.5rem;
  background: var(--bg, #fff);
}

.login h1 {
  margin: 0 0 0.25rem;
  font-size: 1.5rem;
  color: var(--text-h, #08060d);
}

.subtitle {
  margin: 0 0 1.5rem;
  color: var(--text, #6b6375);
  font-size: 0.9rem;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  font-size: 0.85rem;
  color: var(--text, #6b6375);
}

.field input {
  padding: 0.5rem 0.6rem;
  border: 1px solid var(--border, #e5e4e7);
  border-radius: 0.3rem;
  font: inherit;
}

.submit {
  margin-top: 0.5rem;
  padding: 0.6rem 0.8rem;
  border: none;
  border-radius: 0.3rem;
  background: var(--accent, #aa3bff);
  color: #fff;
  font: inherit;
  cursor: pointer;
}

.submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.error {
  margin: 0;
  padding: 0.5rem 0.6rem;
  background: rgba(220, 38, 38, 0.1);
  border: 1px solid rgba(220, 38, 38, 0.4);
  border-radius: 0.3rem;
  color: #dc2626;
  font-size: 0.85rem;
}
</style>