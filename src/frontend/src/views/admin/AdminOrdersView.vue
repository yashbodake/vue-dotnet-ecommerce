<template>
  <div class="admin container">
    <header class="page-head">
      <div>
        <span class="eyebrow">Admin</span>
        <h1>Orders</h1>
      </div>
      <label class="status-filter field-inline">
        <span class="field-label">Status</span>
        <select class="select" v-model="selectedStatus" :disabled="loading" @change="onFilterChange">
          <option value="">All</option>
          <option v-for="status in statuses" :key="status" :value="status">{{ status }}</option>
        </select>
      </label>
    </header>

    <div v-if="!authStore.isAuthenticated" class="card message">
      Please <RouterLink to="/login">sign in</RouterLink> to access this page.
    </div>
    <div v-else-if="!isAdmin" class="card message danger">Access denied.</div>
    <div v-else-if="loading" class="card message">Loading orders…</div>
    <div v-else-if="error" class="card message danger" role="alert">{{ error }}</div>

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
              <div class="status-cell">
                <select
                  class="select status-select"
                  :value="pendingStatus[order.orderId] ?? order.status"
                  :disabled="savingId === order.orderId"
                  @change="onStatusChange(order.orderId, $event)"
                >
                  <option v-for="status in statuses" :key="status" :value="status">{{ status }}</option>
                </select>
                <button
                  type="button"
                  class="btn btn-sm btn-primary"
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
              </div>
            </td>
            <td class="numeric tabular">{{ order.itemCount }}</td>
            <td class="numeric tabular">{{ formatPrice(order.totalAmount) }}</td>
          </tr>
          <tr v-if="orders.length === 0">
            <td colspan="5" class="empty muted">No orders found.</td>
          </tr>
        </tbody>
      </table>
    </div>
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

watch(
  () => authStore.isAuthenticated,
  (authenticated) => {
    if (!authenticated) {
      router.push('/login')
    }
  },
)

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
.admin {
  max-width: var(--maxw);
  padding-block: var(--sp-8) var(--sp-9);
}

.page-head {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: var(--sp-5);
  margin-bottom: var(--sp-6);
  flex-wrap: wrap;
}
.page-head h1 {
  margin-top: var(--sp-2);
  font-size: var(--fs-xl);
}

.field-inline {
  display: flex;
  align-items: flex-end;
  gap: var(--sp-3);
}
.field-inline .select {
  min-width: 10rem;
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

.table-wrap {
  overflow: hidden;
  padding: 0;
}
.numeric {
  text-align: right;
}

.status-cell {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  flex-wrap: wrap;
}
.status-select {
  width: auto;
  min-width: 8rem;
  padding-block: 0.4rem;
}

.feedback {
  font-size: var(--fs-xs);
}
.feedback.success {
  color: var(--success);
}
.feedback.error {
  color: var(--danger);
}

.empty {
  text-align: center;
  padding: var(--sp-7);
}
</style>
