<template>
  <div class="login-page">
    <div class="login-card card">
      <span class="eyebrow">Account</span>
      <h1>Create your account</h1>
      <p class="lede">Join Maison to check out faster and track orders.</p>

      <form class="login-form" @submit.prevent="onSubmit">
        <div class="field">
          <label for="email">Email</label>
          <input
            id="email"
            class="input"
            v-model="email"
            type="email"
            name="email"
            autocomplete="email"
            required
            :disabled="submitting"
          />
        </div>

        <div class="field">
          <label for="password">Password</label>
          <input
            id="password"
            class="input"
            v-model="password"
            type="password"
            name="password"
            autocomplete="new-password"
            required
            :disabled="submitting"
          />
        </div>

        <div class="field">
          <label for="confirm">Confirm password</label>
          <input
            id="confirm"
            class="input"
            v-model="confirm"
            type="password"
            name="confirm"
            autocomplete="new-password"
            required
            :disabled="submitting"
          />
        </div>

        <p v-if="error" class="pill pill-danger error" role="alert">{{ error }}</p>

        <button type="submit" class="btn btn-primary block submit" :disabled="submitting">
          {{ submitting ? 'Creating account…' : 'Create account' }}
        </button>
      </form>

      <p class="hint">
        Already have an account?
        <RouterLink to="/login">Sign in</RouterLink>
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useCartStore } from '../stores/cart'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const cartStore = useCartStore()

const email = ref('')
const password = ref('')
const confirm = ref('')
const submitting = ref(false)
const error = ref<string | null>(null)

async function onSubmit(): Promise<void> {
  error.value = null

  if (password.value.length < 8) {
    error.value = 'Password must be at least 8 characters.'
    return
  }
  if (password.value !== confirm.value) {
    error.value = 'Passwords do not match.'
    return
  }

  submitting.value = true
  try {
    await authStore.register(email.value, password.value)
    await cartStore.mergeOnLogin()
    await cartStore.fetchCart(true)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    router.push(redirect.startsWith('/') ? redirect : '/')
  } catch (e) {
    const message =
      e && typeof e === 'object' && 'message' in e
        ? String((e as { message?: unknown }).message)
        : 'Registration failed'
    error.value = message || 'Registration failed'
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.login-page {
  display: flex;
  align-items: center;
  justify-content: center;
  padding-block: var(--sp-9);
  min-height: 60vh;
}

.login-card {
  width: 100%;
  max-width: 24rem;
  padding: var(--sp-7);
}
.login-card h1 {
  margin-top: var(--sp-3);
  font-size: var(--fs-xl);
}
.lede {
  margin-top: var(--sp-2);
  color: var(--muted);
  font-size: var(--fs-sm);
}

.login-form {
  margin-top: var(--sp-6);
  display: flex;
  flex-direction: column;
  gap: var(--sp-4);
}

.error {
  padding: 0.55rem 0.85rem;
}

.submit {
  margin-top: var(--sp-2);
}

.hint {
  margin-top: var(--sp-6);
  padding-top: var(--sp-4);
  border-top: 1px solid var(--line);
  font-size: var(--fs-xs);
  color: var(--muted);
  text-align: center;
}
</style>
