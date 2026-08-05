<template>
  <div class="orders-page container">
    <header class="page-head">
      <span class="eyebrow">Account</span>
      <h1>Your orders</h1>
    </header>

    <div v-if="loading" class="card message">Loading your orders…</div>
    <div v-else-if="error" class="card message danger" role="alert">{{ error }}</div>

    <div v-else-if="orders.length === 0" class="card message empty-state">
      <p>You haven't placed any orders yet.</p>
      <RouterLink to="/" class="btn btn-primary">Browse the catalogue</RouterLink>
    </div>

    <div v-else class="card table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>Order</th>
            <th>Date</th>
            <th>Status</th>
            <th class="numeric">Items</th>
            <th class="numeric">Total</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="order in orders" :key="order.orderId">
            <td class="tabular">#{{ order.orderId }}</td>
            <td class="muted">{{ formatDate(order.orderDate) }}</td>
            <td>
              <span class="status-pill" :class="statusClass(order.status)">{{ order.status }}</span>
            </td>
            <td class="numeric tabular">{{ order.itemCount }}</td>
            <td class="numeric tabular">{{ formatPrice(order.totalAmount) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { getMyOrders, type OrderSummary } from '../api/account'

const router = useRouter()
const authStore = useAuthStore()

const orders = ref<OrderSummary[]>([])
const loading = ref(false)
const error = ref<string | null>(null)

onMounted(async () => {
  if (!authStore.isAuthenticated) {
    router.push({ path: '/login', query: { redirect: '/orders' } })
    return
  }
  loading.value = true
  error.value = null
  try {
    orders.value = await getMyOrders()
  } catch (e) {
    const message =
      e && typeof e === 'object' && 'message' in e
        ? String((e as { message?: unknown }).message)
        : 'Failed to load orders'
    error.value = message || 'Failed to load orders'
  } finally {
    loading.value = false
  }
})

function formatDate(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  }).format(date)
}

function formatPrice(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

function statusClass(status: string): string {
  const s = status.toLowerCase()
  if (s === 'delivered') return 'good'
  if (s === 'shipped' || s === 'processing') return 'info'
  if (s === 'cancelled') return 'bad'
  return 'pending'
}
</script>

<style scoped>
.orders-page {
  max-width: var(--maxw);
  padding-block: var(--sp-8) var(--sp-9);
}

.page-head {
  margin-bottom: var(--sp-6);
}
.page-head h1 {
  margin-top: var(--sp-2);
  font-size: var(--fs-xl);
}

.message {
  padding: var(--sp-5);
  font-size: var(--fs-sm);
}
.message.danger {
  color: var(--danger);
  background: var(--danger-soft);
  border-color: var(--danger-border);
}
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--sp-4);
}

.table-wrap {
  overflow: hidden;
  padding: 0;
}
.numeric {
  text-align: right;
}

.status-pill {
  display: inline-block;
  font-size: var(--fs-xs);
  padding: 0.2rem 0.55rem;
  border-radius: var(--r-pill);
  border: 1px solid var(--line);
  color: var(--ink-soft);
  background: var(--paper-soft);
}
.status-pill.good {
  color: var(--success);
  border-color: var(--success);
}
.status-pill.info {
  color: var(--accent);
  border-color: var(--accent);
}
.status-pill.bad {
  color: var(--danger);
  border-color: var(--danger);
}
</style>
