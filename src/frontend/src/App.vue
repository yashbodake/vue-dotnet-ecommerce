<template>
  <div class="app">
    <a href="#main" class="skip-link">Skip to content</a>
    <div class="grain" aria-hidden="true"></div>

    <header class="site-header">
      <div class="container header-inner">
        <RouterLink to="/" class="wordmark">Maison</RouterLink>

        <nav class="primary-nav" aria-label="Primary">
          <RouterLink to="/" class="nav-link">Catalogue</RouterLink>
          <RouterLink to="/cart" class="nav-link cart-link">
            <span>Cart</span>
            <span v-if="cartStore.itemCount > 0" class="badge">{{ cartStore.itemCount }}</span>
          </RouterLink>
          <template v-if="authStore.isAuthenticated">
            <RouterLink to="/orders" class="nav-link">Orders</RouterLink>
            <template v-if="authStore.roles.includes('Admin')">
              <span class="nav-divider" aria-hidden="true"></span>
              <RouterLink to="/admin/products" class="nav-link">Products</RouterLink>
              <RouterLink to="/admin/orders" class="nav-link">Admin orders</RouterLink>
            </template>
            <span class="user-name" :title="authStore.email ?? undefined">{{ authStore.email }}</span>
            <button type="button" class="btn btn-sm btn-ghost" @click="onLogout">Sign out</button>
          </template>
          <RouterLink v-else to="/login" class="btn btn-sm btn-primary">Sign in</RouterLink>
        </nav>
      </div>
    </header>

    <main id="main" class="content">
      <RouterView />
    </main>

    <footer class="site-footer">
      <div class="container footer-inner">
        <div class="footer-brand">
          <span class="wordmark small">Maison</span>
          <p class="footer-tag">A considered catalogue of well-made objects.</p>
        </div>
        <nav class="footer-nav" aria-label="Footer">
          <RouterLink to="/" class="footer-link">Catalogue</RouterLink>
          <RouterLink to="/cart" class="footer-link">Cart</RouterLink>
          <RouterLink to="/login" class="footer-link">Account</RouterLink>
          <a href="#" class="footer-link">Privacy</a>
          <a href="#" class="footer-link">Terms</a>
        </nav>
        <p class="footer-legal">© {{ year }} Maison. All rights reserved.</p>
      </div>
    </footer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'
import { useCartStore } from './stores/cart'

const router = useRouter()
const authStore = useAuthStore()
const cartStore = useCartStore()

const year = computed(() => new Date().getFullYear())

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
  flex: 1;
  display: flex;
  flex-direction: column;
}

/* Header ---------------------------------------------------------------- */
.site-header {
  position: sticky;
  top: 0;
  z-index: var(--z-header);
  background: color-mix(in srgb, var(--paper) 82%, transparent);
  backdrop-filter: blur(12px) saturate(1.1);
  border-bottom: 1px solid var(--line);
}

.header-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--sp-5);
  padding-block: var(--sp-4);
}

.wordmark {
  font-family: var(--display);
  font-size: var(--fs-xl);
  font-weight: 500;
  letter-spacing: -0.03em;
  color: var(--ink);
  text-decoration: none;
  line-height: 1;
}
.wordmark:hover {
  color: var(--ink);
}
.wordmark.small {
  font-size: var(--fs-md);
}

.primary-nav {
  display: flex;
  align-items: center;
  gap: var(--sp-4);
  flex-wrap: wrap;
  justify-content: flex-end;
}

.nav-link {
  position: relative;
  font-size: var(--fs-sm);
  font-weight: 500;
  letter-spacing: 0.01em;
  color: var(--ink-soft);
  text-decoration: none;
  padding-block: var(--sp-1);
  transition: color var(--dur) var(--ease);
}
.nav-link::after {
  content: '';
  position: absolute;
  left: 0;
  right: 0;
  bottom: -2px;
  height: 1px;
  background: currentColor;
  transform: scaleX(0);
  transform-origin: left;
  transition: transform var(--dur) var(--ease);
}
.nav-link:hover {
  color: var(--ink);
}
.nav-link.router-link-active {
  color: var(--ink);
}
.nav-link.router-link-active::after {
  transform: scaleX(1);
}

.cart-link {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
}
.cart-link .badge {
  background: var(--ink);
  color: var(--surface);
  min-width: 1.1rem;
  height: 1.1rem;
  font-size: 0.65rem;
}

.nav-divider {
  width: 1px;
  align-self: stretch;
  margin-block: 0.2rem;
  background: var(--line);
}

.user-name {
  font-size: var(--fs-sm);
  color: var(--muted);
  max-width: 14rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.content {
  flex: 1;
}

/* Footer ---------------------------------------------------------------- */
.site-footer {
  margin-top: var(--sp-9);
  border-top: 1px solid var(--line);
  background: var(--paper-soft);
}

.footer-inner {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: var(--sp-5);
  padding-block: var(--sp-7);
}

.footer-brand {
  display: flex;
  flex-direction: column;
  gap: var(--sp-1);
}

.footer-tag {
  font-size: var(--fs-sm);
  color: var(--muted);
  max-width: 26ch;
}

.footer-nav {
  display: flex;
  flex-wrap: wrap;
  gap: var(--sp-5);
}

.footer-link {
  font-size: var(--fs-sm);
  color: var(--ink-soft);
  text-decoration: none;
  transition: color var(--dur) var(--ease);
}
.footer-link:hover {
  color: var(--accent);
}

.footer-legal {
  width: 100%;
  font-size: var(--fs-xs);
  color: var(--muted);
  letter-spacing: 0.04em;
  padding-top: var(--sp-4);
  border-top: 1px solid var(--line);
}

@media (max-width: 640px) {
  .header-inner {
    gap: var(--sp-3);
  }
  .wordmark {
    font-size: var(--fs-lg);
  }
  .primary-nav {
    gap: var(--sp-3);
  }
  .user-name {
    display: none;
  }
  .footer-inner {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
