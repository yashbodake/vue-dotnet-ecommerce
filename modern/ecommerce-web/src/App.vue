<template>
  <div class="app">
    <header class="nav">
      <RouterLink to="/" class="brand">Ecommerce Modern</RouterLink>

      <div class="nav-right">
        <RouterLink to="/cart" class="cart-link">
          Cart
          <span v-if="cartStore.itemCount > 0" class="badge">{{ cartStore.itemCount }}</span>
        </RouterLink>
        <template v-if="authStore.isAuthenticated">
          <RouterLink v-if="authStore.roles.includes('Admin')" to="/admin/products" class="admin-link">
            Admin: Products
          </RouterLink>
          <RouterLink v-if="authStore.roles.includes('Admin')" to="/admin/orders" class="admin-link">
            Admin: Orders
          </RouterLink>
          <span class="user">{{ authStore.email }}</span>
          <button type="button" class="logout" @click="onLogout">Sign out</button>
        </template>
        <template v-else>
          <RouterLink to="/login" class="login-link">Sign in</RouterLink>
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
  padding: 0.75rem 1.5rem;
  border-bottom: 1px solid var(--border, #e5e4e7);
  background: var(--bg, #fff);
}

.brand {
  font-weight: 600;
  color: var(--text-h, #08060d);
  text-decoration: none;
}

.nav-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.user {
  color: var(--text, #6b6375);
  font-size: 0.9rem;
}

.logout,
.login-link,
.cart-link,
.admin-link {
  border: 1px solid var(--border, #e5e4e7);
  border-radius: 0.3rem;
  padding: 0.35rem 0.75rem;
  background: transparent;
  color: var(--text-h, #08060d);
  font: inherit;
  cursor: pointer;
  text-decoration: none;
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
  background: var(--accent, #aa3bff);
  color: #fff;
  font-size: 0.75rem;
  font-weight: 600;
}

.content {
  flex: 1;
}
</style>