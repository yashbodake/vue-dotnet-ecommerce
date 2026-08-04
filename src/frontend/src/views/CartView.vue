<template>
  <div class="cart">
    <h1>Shopping Cart</h1>

    <div v-if="cartStore.loading" class="status" aria-live="polite">Loading cart...</div>

    <div v-else-if="cartStore.error" class="status error" aria-live="polite">
      {{ cartStore.error }}
    </div>

    <div v-else-if="cartStore.isEmpty" class="status empty">
      <p>Your cart is empty.</p>
      <RouterLink to="/" class="button-link">Continue shopping</RouterLink>
    </div>

    <div v-else class="cart-content">
      <ul class="items">
        <li v-for="item in cartStore.items" :key="item.cartItemId" class="item">
          <div class="item-main">
            <div class="item-info">
              <p class="name">{{ item.productName }}</p>
              <p v-if="item.variantName" class="variant">
                {{ item.variantName }}
                <span v-if="item.variantSkuSuffix" class="sku">({{ item.variantSkuSuffix }})</span>
              </p>
              <p class="unit-price">{{ formatPrice(item.productPrice + item.priceAdjustment) }} each</p>
            </div>

            <div class="quantity">
              <button
                type="button"
                :disabled="item.quantity <= 1 || cartStore.loading"
                @click="changeQuantity(item.cartItemId, item.quantity - 1)"
                aria-label="Decrease quantity"
              >
                −
              </button>
              <input
                type="number"
                min="1"
                :max="item.stock"
                v-model.number="localQuantities[item.cartItemId]"
                @change="onQuantityInput(item.cartItemId)"
                aria-label="Quantity"
              />
              <button
                type="button"
                :disabled="item.quantity >= item.stock || cartStore.loading"
                @click="changeQuantity(item.cartItemId, item.quantity + 1)"
                aria-label="Increase quantity"
              >
                +
              </button>
            </div>

            <div class="line-total">{{ formatPrice(item.lineTotal) }}</div>

            <button
              type="button"
              class="remove"
              :disabled="cartStore.loading"
              @click="remove(item.cartItemId)"
            >
              Remove
            </button>
          </div>
        </li>
      </ul>

      <div class="summary">
        <p class="subtotal">
          <span>Subtotal</span>
          <span>{{ formatPrice(cartStore.total) }}</span>
        </p>
        <div class="actions">
          <RouterLink to="/" class="button-link secondary">Continue shopping</RouterLink>
          <RouterLink to="/checkout" class="button-link checkout">Proceed to checkout</RouterLink>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useCartStore } from '../stores/cart'
import { useAuthStore } from '../stores/auth'

const cartStore = useCartStore()
const authStore = useAuthStore()

const localQuantities = reactive<Record<number, number>>({})

// Fetch the full cart when the page loads so a direct visit to /cart
// shows current contents, not just the badge count from App.vue.
onMounted(() => {
  cartStore.fetchCart(authStore.isAuthenticated)
})

watch(
  () => cartStore.items,
  (items) => {
    items.forEach((item) => {
      if (localQuantities[item.cartItemId] === undefined) {
        localQuantities[item.cartItemId] = item.quantity
      }
    })
  },
  { immediate: true },
)

function formatPrice(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

function changeQuantity(cartItemId: number, quantity: number): void {
  const item = cartStore.items.find((i) => i.cartItemId === cartItemId)
  if (!item) return

  const clamped = Math.max(1, Math.min(quantity, item.stock))
  if (clamped === item.quantity) return

  localQuantities[cartItemId] = clamped
  cartStore.updateQuantity(cartItemId, clamped, authStore.isAuthenticated)
}

function onQuantityInput(cartItemId: number): void {
  const item = cartStore.items.find((i) => i.cartItemId === cartItemId)
  if (!item) return

  let value = Number(localQuantities[cartItemId])
  if (Number.isNaN(value)) {
    value = item.quantity
  }
  const clamped = Math.max(1, Math.min(value, item.stock))
  localQuantities[cartItemId] = clamped

  if (clamped !== item.quantity) {
    cartStore.updateQuantity(cartItemId, clamped, authStore.isAuthenticated)
  }
}

function remove(cartItemId: number): void {
  cartStore.removeItem(cartItemId, authStore.isAuthenticated)
}
</script>

<style scoped>
.cart {
  max-width: 1000px;
  margin: 0 auto;
  padding: 1.5rem;
}

.cart h1 {
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
.status.empty p {
  margin: 0 0 1rem;
}

.items {
  list-style: none;
  margin: 0;
  padding: 0;
  border-top: 1px solid #e5e7eb;
}

.item {
  border-bottom: 1px solid #e5e7eb;
  padding: 1rem 0;
}

.item-main {
  display: grid;
  grid-template-columns: 1fr auto auto auto;
  gap: 1rem;
  align-items: center;
}
@media (max-width: 768px) {
  .item-main {
    grid-template-columns: 1fr;
    gap: 0.75rem;
  }
}

.item-info {
  min-width: 0;
}
.name {
  margin: 0;
  font-weight: 600;
  color: #111827;
}
.variant {
  margin: 0.25rem 0 0;
  font-size: 0.85rem;
  color: #4b5563;
}
.sku {
  color: #9ca3af;
  margin-left: 0.25rem;
}
.unit-price {
  margin: 0.25rem 0 0;
  font-size: 0.85rem;
  color: #6b7280;
}

.quantity {
  display: flex;
  align-items: center;
  gap: 0.35rem;
}
.quantity button {
  width: 1.8rem;
  height: 1.8rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  background: #fff;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}
.quantity button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.quantity input {
  width: 3rem;
  text-align: center;
  padding: 0.35rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font: inherit;
}

.line-total {
  font-weight: 700;
  color: #111827;
  min-width: 5rem;
  text-align: right;
}

.remove {
  padding: 0.4rem 0.75rem;
  border: 1px solid #fca5a5;
  border-radius: 6px;
  background: #fef2f2;
  color: #dc2626;
  cursor: pointer;
  font-size: 0.85rem;
}
.remove:hover:not(:disabled) {
  background: #fee2e2;
}
.remove:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.summary {
  margin-top: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  align-items: flex-end;
}
.subtotal {
  margin: 0;
  font-size: 1.25rem;
  font-weight: 700;
  color: #111827;
  display: flex;
  gap: 1rem;
}
.actions {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
}

.button-link {
  display: inline-flex;
  padding: 0.55rem 1rem;
  border: none;
  border-radius: 6px;
  background: var(--accent, var(--accent));
  color: #fff;
  text-decoration: none;
  font-size: 0.9rem;
  cursor: pointer;
}
.button-link.secondary {
  background: #fff;
  color: #374151;
  border: 1px solid #d1d5db;
}

.checkout {
  padding: 0.55rem 1rem;
  border: none;
  border-radius: 6px;
  background: #111827;
  color: #fff;
  font-size: 0.9rem;
  cursor: pointer;
}
.checkout:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
