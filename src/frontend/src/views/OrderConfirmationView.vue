<template>
  <div class="confirmation container">
    <div v-if="loading" class="status" aria-live="polite">Loading your order…</div>

    <div v-else-if="error" class="status error">
      <h1>Order not found</h1>
      <p class="status-copy">{{ error }}</p>
      <RouterLink to="/" class="btn btn-primary">Continue shopping</RouterLink>
    </div>

    <div v-else-if="order" class="order-wrap">
      <header class="order-intro">
        <span class="eyebrow">Thank you</span>
        <h1>Your order is confirmed.</h1>
        <p class="lede">A receipt is on its way. Below is a summary of your purchase.</p>
      </header>

      <div class="card order-card">
        <div class="order-header">
          <div>
            <p class="label">Order number</p>
            <p class="order-number tabular">#{{ order.orderId }}</p>
          </div>
          <div class="order-meta">
            <div>
              <p class="label">Order date</p>
              <p>{{ formatDate(order.orderDate) }}</p>
            </div>
            <div>
              <p class="label">Status</p>
              <span class="pill pill-success">{{ order.status }}</span>
            </div>
          </div>
        </div>

        <div class="section">
          <h2>Shipping</h2>
          <p class="address">{{ order.shippingAddress }}</p>
          <p class="method"><span class="muted">Method:</span> {{ order.shippingMethod }}</p>
        </div>

        <div class="section">
          <h2>Items</h2>
          <table class="table items-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>Variant</th>
                <th class="numeric">Qty</th>
                <th class="numeric">Unit</th>
                <th class="numeric">Total</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in order.items" :key="item.productId">
                <td>{{ item.productName }}</td>
                <td class="muted">{{ item.variantName ?? '—' }}</td>
                <td class="numeric tabular">{{ item.quantity }}</td>
                <td class="numeric tabular">{{ formatPrice(item.unitPrice) }}</td>
                <td class="numeric tabular">{{ formatPrice(item.lineTotal) }}</td>
              </tr>
            </tbody>
            <tfoot>
              <tr>
                <td colspan="4" class="total-label">Order total</td>
                <td class="numeric total-value tabular">{{ formatPrice(order.totalAmount) }}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>

      <div class="actions">
        <RouterLink to="/" class="btn btn-primary">Continue shopping</RouterLink>
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

watch(
  () => props.orderId,
  () => {
    if (authStore.isAuthenticated) {
      loadOrder()
    }
  },
)

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
    const message =
      e && typeof e === 'object' && 'message' in e
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
  max-width: 52rem;
  padding-block: var(--sp-8) var(--sp-9);
}

.status,
.error {
  text-align: center;
  padding: var(--sp-9) var(--sp-4);
}
.status.error {
  color: var(--danger);
}
.error h1 {
  font-size: var(--fs-xl);
}
.status-copy {
  color: var(--muted);
  margin-block: var(--sp-3) var(--sp-5);
}

/* Intro ---------------------------------------------------------------- */
.order-intro {
  margin-bottom: var(--sp-7);
}
.order-intro h1 {
  margin-top: var(--sp-3);
  font-size: var(--fs-2xl);
}
.lede {
  margin-top: var(--sp-3);
  color: var(--muted);
  font-size: var(--fs-md);
}

/* Card ----------------------------------------------------------------- */
.order-card {
  padding: var(--sp-6);
}

.order-header {
  display: flex;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: var(--sp-5);
  padding-bottom: var(--sp-5);
  border-bottom: 1px solid var(--line);
  margin-bottom: var(--sp-5);
}

.order-number {
  font-family: var(--display);
  font-size: var(--fs-xl);
  font-weight: 500;
  color: var(--ink);
  margin-top: var(--sp-1);
}

.order-meta {
  display: flex;
  gap: var(--sp-6);
  flex-wrap: wrap;
}

.label {
  font-size: var(--fs-xs);
  font-weight: 500;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: var(--muted);
}

.section {
  margin-bottom: var(--sp-6);
}
.section:last-of-type {
  margin-bottom: 0;
}
.section h2 {
  font-family: var(--sans);
  font-size: var(--fs-xs);
  font-weight: 500;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--muted);
  margin-bottom: var(--sp-3);
}
.address {
  color: var(--body);
  line-height: 1.6;
  margin-bottom: var(--sp-1);
}
.method {
  color: var(--ink);
  font-size: var(--fs-sm);
}

/* Items table ---------------------------------------------------------- */
.items-table tfoot td {
  border-top: 1px solid var(--line-strong);
  background: var(--paper-soft);
}
.numeric {
  text-align: right;
}
.total-label {
  text-align: right;
  font-weight: 500;
  color: var(--ink);
}
.total-value {
  font-weight: 500;
  color: var(--ink);
  font-size: var(--fs-md);
}

.actions {
  margin-top: var(--sp-6);
  text-align: center;
}

@media (max-width: 600px) {
  .order-header {
    flex-direction: column;
    gap: var(--sp-4);
  }
  .order-meta {
    gap: var(--sp-5);
  }
}
</style>
