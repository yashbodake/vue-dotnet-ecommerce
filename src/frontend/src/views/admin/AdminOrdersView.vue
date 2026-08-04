<template>
  <div class="admin-orders">
    <header class="page-header">
      <h1>Admin: Orders</h1>

      <label class="status-filter">
        <span>Status</span>
        <select v-model="selectedStatus" :disabled="loading" @change="onFilterChange">
          <option value="">All</option>
          <option v-for="status in statuses" :key="status" :value="status">
            {{ status }}
          </option>
        </select>
      </label>
    </header>

    <div v-if="!authStore.isAuthenticated" class="message error">
      Please <RouterLink to="/login">sign in</RouterLink> to access this page.
    </div>
    <div v-else-if="!isAdmin" class="message error">Access denied.</div>
    <div v-else-if="loading" class="message">Loading orders…</div>
    <div v-else-if="error" class="message error" role="alert">{{ error }}</div>
    <template v-else>
      <table class="data-table">
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
            <td>#{{ order.orderId }}</td>
            <td>{{ formatDate(order.orderDate) }}</td>
            <td class="status-cell">
              <select
                :value="pendingStatus[order.orderId] ?? order.status"
                :disabled="savingId === order.orderId"
                @change="onStatusChange(order.orderId, $event)"
              >
                <option v-for="status in statuses" :key="status" :value="status">
                  {{ status }}
                </option>
              </select>
              <button
                type="button"
                class="btn btn-small btn-primary save-btn"
                :disabled="
                  savingId === order.orderId ||
                  (pendingStatus[order.orderId] ?? order.status) === order.status
                "
                @click="saveStatus(order.orderId)"
              >
                {{ savingId === order.orderId ? 'Saving…' : 'Save' }}
              </button>
              <span
                v-if="feedback[order.orderId]"
                class="feedback"
                :class="feedback[order.orderId].type"
              >
                {{ feedback[order.orderId].message }}
              </span>
            </td>
            <td class="numeric">{{ order.itemCount }}</td>
            <td class="numeric">{{ formatPrice(order.totalAmount) }}</td>
          </tr>
          <tr v-if="orders.length === 0">
            <td colspan="5" class="empty">No orders found.</td>
          </tr>
        </tbody>
      </table>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getOrders, updateOrderStatus, type AdminOrder } from '../../api/admin'

const statuses = ['Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'] as const

const router = useRouter()
const authStore = useAuthStore()
const isAdmin = computed(() => authStore.roles.includes('Admin'))

const orders = ref<AdminOrder[]>([])
const selectedStatus = ref('')
const loading = ref(false)
const error = ref<string | null>(null)
const savingId = ref<number | null>(null)
const pendingStatus = ref<Record<number, string>>({})
const feedback = ref<Record<number, { message: string; type: 'success' | 'error' }>>({})

onMounted(() => {
  if (!authStore.isAuthenticated) {
    router.push('/login')
    return
  }
  loadOrders()
})

watch(() => authStore.isAuthenticated, (authenticated) => {
  if (!authenticated) {
    router.push('/login')
  }
})

async function loadOrders(): Promise<void> {
  loading.value = true
  error.value = null
  feedback.value = {}
  try {
    orders.value = await getOrders(selectedStatus.value || undefined)
  } catch (e) {
    const message =
      e && typeof e === 'object' && 'message' in e
        ? String((e as { message?: unknown }).message)
        : 'Failed to load orders'
    error.value = message || 'Failed to load orders'
  } finally {
    loading.value = false
  }
}

function onFilterChange(): void {
  loadOrders()
}

function onStatusChange(orderId: number, event: Event): void {
  const value = (event.target as HTMLSelectElement).value
  pendingStatus.value = { ...pendingStatus.value, [orderId]: value }
  // clear previous feedback for this order so the user can try again
  const next = { ...feedback.value }
  delete next[orderId]
  feedback.value = next
}

async function saveStatus(orderId: number): Promise<void> {
  const newStatus = pendingStatus.value[orderId]
  if (!newStatus) return

  savingId.value = orderId
  try {
    await updateOrderStatus(orderId, newStatus)
    feedback.value = {
      ...feedback.value,
      [orderId]: { message: 'Saved', type: 'success' },
    }
    // refresh the underlying order status so Save stays disabled until changed again
    orders.value = orders.value.map((o) =>
      o.orderId === orderId ? { ...o, status: newStatus } : o
    )
    pendingStatus.value = { ...pendingStatus.value }
  } catch (e) {
    const message =
      e && typeof e === 'object' && 'message' in e
        ? String((e as { message?: unknown }).message)
        : 'Failed to update status'
    feedback.value = {
      ...feedback.value,
      [orderId]: { message: message || 'Failed to update status', type: 'error' },
    }
  } finally {
    savingId.value = null
  }
}

function formatDate(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value
  return new Intl.DateTimeFormat('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}

function formatPrice(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}
</script>

<style scoped>
.admin-orders {
  max-width: 1200px;
  margin: 0 auto;
  padding: 1.5rem;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1.5rem;
}

.page-header h1 {
  margin: 0;
  font-size: 1.5rem;
  color: var(--text-h, #08060d);
}

.status-filter {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  color: #374151;
}

.status-filter select {
  padding: 0.4rem 0.5rem;
  border: 1px solid #d1d5db;
  border-radius: 0.3rem;
  font: inherit;
  font-size: 0.9rem;
}

.message {
  padding: 1rem;
  border-radius: 0.3rem;
  background: #f9fafb;
  color: #374151;
}

.message.error {
  background: rgba(220, 38, 38, 0.08);
  color: #dc2626;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
}

.data-table th,
.data-table td {
  padding: 0.6rem 0.75rem;
  border-bottom: 1px solid #e5e7eb;
  text-align: left;
}

.data-table th {
  font-weight: 600;
  color: #374151;
  background: #f9fafb;
}

.numeric {
  text-align: right;
}

.status-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.status-cell select {
  padding: 0.35rem 0.5rem;
  border: 1px solid #d1d5db;
  border-radius: 0.3rem;
  font: inherit;
  font-size: 0.85rem;
}

.save-btn {
  flex-shrink: 0;
}

.feedback {
  font-size: 0.8rem;
}

.feedback.success {
  color: #16a34a;
}

.feedback.error {
  color: #dc2626;
}

.empty {
  text-align: center;
  color: #6b7280;
  padding: 2rem;
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.45rem 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 0.3rem;
  background: #fff;
  color: #374151;
  font: inherit;
  font-size: 0.85rem;
  cursor: pointer;
}

.btn:hover:not(:disabled) {
  background: #f3f4f6;
}

.btn-primary {
  background: var(--accent, var(--accent));
  border-color: var(--accent, var(--accent));
  color: #fff;
}

.btn-primary:hover:not(:disabled) {
  background: var(--accent-hover);
}

.btn-small {
  padding: 0.25rem 0.5rem;
  font-size: 0.8rem;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
