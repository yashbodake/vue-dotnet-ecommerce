<template>
  <div class="confirmation">
    <h1>Order Confirmation</h1>

    <div v-if="loading" class="status" aria-live="polite">Loading order...</div>

    <div v-else-if="error" class="status error">
      <p>{{ error }}</p>
      <RouterLink to="/" class="button-link">Continue shopping</RouterLink>
    </div>

    <div v-else-if="order" class="order-card">
      <div class="order-header">
        <div>
          <p class="label">Order Number</p>
          <p class="order-number">#{{ order.orderId }}</p>
        </div>
        <div class="order-meta">
          <div>
            <p class="label">Order Date</p>
            <p>{{ formatDate(order.orderDate) }}</p>
          </div>
          <div>
            <p class="label">Status</p>
            <p class="status-badge">{{ order.status }}</p>
          </div>
        </div>
      </div>

      <div class="section">
        <h2>Shipping Information</h2>
        <p class="address">{{ order.shippingAddress }}</p>
        <p class="method"><strong>Method:</strong> {{ order.shippingMethod }}</p>
      </div>

      <div class="section">
        <h2>Order Items</h2>
        <table class="items-table">
          <thead>
            <tr>
              <th>Product</th>
              <th>Variant</th>
              <th class="numeric">Qty</th>
              <th class="numeric">Unit Price</th>
              <th class="numeric">Total</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in order.items" :key="item.productId">
              <td>{{ item.productName }}</td>
              <td>{{ item.variantName ?? '-' }}</td>
              <td class="numeric">{{ item.quantity }}</td>
              <td class="numeric">{{ formatPrice(item.unitPrice) }}</td>
              <td class="numeric">{{ formatPrice(item.lineTotal) }}</td>
            </tr>
          </tbody>
          <tfoot>
            <tr>
              <td colspan="4" class="total-label">Order Total</td>
              <td class="numeric total-value">{{ formatPrice(order.totalAmount) }}</td>
            </tr>
          </tfoot>
        </table>
      </div>

      <div class="actions">
        <RouterLink to="/" class="button-link">Continue Shopping</RouterLink>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { getOrder, type OrderConfirmation } from '../api/checkout'

const props = defineProps<{
  orderId: string | number
}>()

const router = useRouter()
const authStore = useAuthStore()

const order = ref<OrderConfirmation | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)

onMounted(() => {
  if (!authStore.isAuthenticated) {
    router.replace('/login?redirect=/checkout/confirmation/' + props.orderId)
    return
  }
  loadOrder()
})

watch(() => props.orderId, () => {
  if (authStore.isAuthenticated) {
    loadOrder()
  }
})

async function loadOrder(): Promise<void> {
  const id = typeof props.orderId === 'number' ? props.orderId : parseInt(String(props.orderId), 10)
  if (Number.isNaN(id)) {
    error.value = 'Invalid order number.'
    loading.value = false
    return
  }

  loading.value = true
  error.value = null
  try {
    order.value = await getOrder(id)
  } catch (e) {
    const status = e && typeof e === 'object' && 'status' in e ? (e as { status?: number }).status : undefined
    const message = e && typeof e === 'object' && 'message' in e
      ? String((e as { message?: unknown }).message)
      : 'Failed to load order.'
    error.value = status === 404 ? 'Order not found.' : message
  } finally {
    loading.value = false
  }
}

function formatPrice(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

function formatDate(value: string): string {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString()
}
</script>

<style scoped>
.confirmation {
  max-width: 900px;
  margin: 0 auto;
  padding: 1.5rem;
}

.confirmation h1 {
  margin: 0 0 1.25rem;
  font-size: 1.75rem;
  color: #111827;
}

.status {
  padding: 2rem;
  text-align: center;
  color: #6b7280;
}

.status.error {
  color: #dc2626;
}

.order-card {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 1.5rem;
  background: #fff;
}

.order-header {
  display: flex;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 1rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid #e5e7eb;
  margin-bottom: 1rem;
}

.order-number {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
}

.order-meta {
  display: flex;
  gap: 1.5rem;
  flex-wrap: wrap;
}

.label {
  margin: 0 0 0.2rem;
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  color: #6b7280;
}

.status-badge {
  display: inline-block;
  padding: 0.2rem 0.6rem;
  background: #f3f4f6;
  border-radius: 9999px;
  font-size: 0.85rem;
  font-weight: 600;
  color: #111827;
}

.section {
  margin-bottom: 1.5rem;
}

.section h2 {
  margin: 0 0 0.75rem;
  font-size: 1.1rem;
  color: #374151;
}

.address {
  margin: 0 0 0.4rem;
  color: #374151;
  line-height: 1.5;
}

.method {
  margin: 0;
  color: #111827;
}

.items-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
}

.items-table th,
.items-table td {
  padding: 0.6rem 0.5rem;
  border-bottom: 1px solid #e5e7eb;
  text-align: left;
}

.items-table th {
  font-weight: 600;
  color: #374151;
}

.items-table td {
  color: #111827;
}

.numeric {
  text-align: right;
}

.total-label {
  text-align: right;
  font-weight: 700;
}

.total-value {
  font-weight: 700;
  color: #111827;
}

.actions {
  margin-top: 1.5rem;
}

.button-link {
  display: inline-flex;
  padding: 0.55rem 1rem;
  border: none;
  border-radius: 6px;
  background: var(--accent, #aa3bff);
  color: #fff;
  text-decoration: none;
  font-size: 0.9rem;
  cursor: pointer;
}

@media (max-width: 600px) {
  .order-header {
    flex-direction: column;
    gap: 1rem;
  }

  .order-meta {
    gap: 1rem;
  }

  .items-table {
    font-size: 0.8rem;
  }
}
</style>
