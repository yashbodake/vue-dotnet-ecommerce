<template>
  <div class="app">
    <header class="nav">
      <RouterLink to="/" class="brand">Shop</RouterLink>

      <div class="nav-right">
        <RouterLink to="/cart" class="nav-chip cart-link">
          Cart
          <span v-if="cartStore.itemCount > 0" class="badge">{{ cartStore.itemCount }}</span>
        </RouterLink>
        <template v-if="authStore.isAuthenticated">
          <RouterLink v-if="authStore.roles.includes('Admin')" to="/admin/products" class="nav-chip">
            Products
          </RouterLink>
          <RouterLink v-if="authStore.roles.includes('Admin')" to="/admin/orders" class="nav-chip">
            Orders
          </RouterLink>
          <span class="user">{{ authStore.email }}</span>
          <button type="button" class="nav-chip logout" @click="onLogout">Sign out</button>
        </template>
        <template v-else>
          <RouterLink to="/login" class="nav-chip solid">Sign in</RouterLink>
        </template>
      </div>
    </header>

    <main class="content">
      <RouterView />
    </main>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'
import { useCartStore } from './stores/cart'

const router = useRouter()
const authStore = useAuthStore()
const cartStore = useCartStore()

onMounted(() => {
  cartStore.fetchCount(authStore.isAuthenticated)
})

function onLogout(): void {
  authStore.logout()
  cartStore.clear()
  router.push('/login')
}
</script>

<style scoped>
.app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.nav {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.9rem 1.5rem;
  border-bottom: 1px solid var(--border);
  background: color-mix(in srgb, var(--bg-elevated) 88%, transparent);
  backdrop-filter: blur(10px);
  position: sticky;
  top: 0;
  z-index: 20;
}

.brand {
  font-size: 1.45rem;
  font-weight: 700;
  color: var(--text-h);
  text-decoration: none;
}

.nav-right {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.55rem;
  justify-content: flex-end;
}

.user {
  color: var(--text-muted);
  font-size: 0.88rem;
}

.nav-chip {
  border: 1px solid var(--border);
  border-radius: 999px;
  padding: 0.4rem 0.85rem;
  background: var(--bg-elevated);
  color: var(--text-h);
  cursor: pointer;
  text-decoration: none;
  transition: border-color 0.15s ease, background 0.15s ease, color 0.15s ease;
}

.nav-chip:hover {
  border-color: var(--accent);
  color: var(--accent);
}

.nav-chip.solid {
  background: var(--accent);
  border-color: var(--accent);
  color: var(--accent-ink);
}

.nav-chip.solid:hover {
  background: var(--accent-hover);
  border-color: var(--accent-hover);
  color: var(--accent-ink);
}

.cart-link {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
}

.badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 1.25rem;
  height: 1.25rem;
  padding: 0 0.35rem;
  border-radius: 999px;
  background: var(--accent);
  color: var(--accent-ink);
  font-size: 0.75rem;
  font-weight: 700;
}

.content {
  flex: 1;
}
</style>
