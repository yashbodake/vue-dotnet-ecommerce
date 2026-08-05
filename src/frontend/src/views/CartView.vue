<template>
  <div class="cart container">
    <header class="page-head">
      <span class="eyebrow">Cart</span>
      <h1>Your bag</h1>
    </header>

    <div v-if="cartStore.loading" class="items-skeleton" aria-live="polite">
      <div v-for="n in 3" :key="n" class="skeleton-row">
        <div class="skeleton thumb-s"></div>
        <div class="skeleton-lines">
          <div class="skeleton line w-60"></div>
          <div class="skeleton line w-30"></div>
        </div>
      </div>
    </div>

    <div v-else-if="cartStore.error" class="status error" aria-live="polite">
      {{ cartStore.error }}
    </div>

    <div v-else-if="cartStore.isEmpty" class="empty">
      <h2>Your bag is empty.</h2>
      <p class="status-copy">When you find something you love, it will gather here.</p>
      <RouterLink to="/" class="btn btn-primary">Browse the catalogue</RouterLink>
    </div>

    <div v-else class="cart-content">
      <ul class="items">
        <li v-for="item in cartStore.items" :key="item.cartItemId" class="item">
          <div class="item-thumb">
            <img :src="thumbFor(item)" :alt="item.productName" @error="onThumbError($event, item.productName)" />
          </div>

          <div class="item-info">
            <p class="name">{{ item.productName }}</p>
            <p v-if="item.variantName" class="variant">
              {{ item.variantName }}
              <span v-if="item.variantSkuSuffix" class="sku">({{ item.variantSkuSuffix }})</span>
            </p>
            <p class="unit-price tabular">{{ formatPrice(item.productPrice + item.priceAdjustment) }} each</p>
          </div>

          <div class="quantity">
            <button
              type="button"
              class="qty-btn"
              :disabled="item.quantity <= 1 || cartStore.loading"
              @click="changeQuantity(item.cartItemId, item.quantity - 1)"
              aria-label="Decrease quantity"
            >
              −
            </button>
            <input
              type="number"
              class="input qty-input tabular"
              min="1"
              :max="item.stock"
              v-model.number="localQuantities[item.cartItemId]"
              @change="onQuantityInput(item.cartItemId)"
              aria-label="Quantity"
            />
            <button
              type="button"
              class="qty-btn"
              :disabled="item.quantity >= item.stock || cartStore.loading"
              @click="changeQuantity(item.cartItemId, item.quantity + 1)"
              aria-label="Increase quantity"
            >
              +
            </button>
          </div>

          <div class="line-total tabular">{{ formatPrice(item.lineTotal) }}</div>

          <button
            type="button"
            class="btn-link remove"
            :disabled="cartStore.loading"
            @click="remove(item.cartItemId)"
          >
            Remove
          </button>
        </li>
      </ul>

      <aside class="summary">
        <h2 class="summary-title">Order summary</h2>
        <div class="summary-row">
          <span class="muted">Subtotal</span>
          <span class="tabular">{{ formatPrice(cartStore.total) }}</span>
        </div>
        <div class="summary-row">
          <span class="muted">Shipping</span>
          <span class="muted">Calculated at checkout</span>
        </div>
        <hr class="divider" />
        <div class="summary-row total">
          <span>Total</span>
          <span class="tabular">{{ formatPrice(cartStore.total) }}</span>
        </div>
        <RouterLink to="/checkout" class="btn btn-primary block">Proceed to checkout</RouterLink>
        <RouterLink to="/" class="btn-link continue">Continue shopping</RouterLink>
      </aside>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, watch, onMounted } from 'vue'
import { useCartStore } from '../stores/cart'
import { useAuthStore } from '../stores/auth'
import { productImageFallback } from '../utils/productImage'
import type { CartItem } from '../api/cart'

const cartStore = useCartStore()
const authStore = useAuthStore()

const localQuantities = reactive<Record<number, number>>({})

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

function thumbFor(item: CartItem): string {
  return productImageFallback(item.productName)
}

function onThumbError(event: Event, name: string): void {
  const img = event.target as HTMLImageElement
  img.src = productImageFallback(name)
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
  padding-block: var(--sp-8) var(--sp-9);
  max-width: var(--maxw);
}

.page-head {
  margin-bottom: var(--sp-7);
}
.page-head h1 {
  margin-top: var(--sp-2);
}

/* Status / empty ------------------------------------------------------- */
.status,
.empty {
  text-align: center;
  padding: var(--sp-9) var(--sp-4);
}
.status.error {
  color: var(--danger);
}
.empty h2 {
  font-size: var(--fs-xl);
}
.status-copy {
  color: var(--muted);
  margin-block: var(--sp-3) var(--sp-5);
}

/* Skeleton ------------------------------------------------------------- */
.items-skeleton {
  display: flex;
  flex-direction: column;
  gap: var(--sp-5);
}
.skeleton-row {
  display: flex;
  gap: var(--sp-4);
  align-items: center;
}
.thumb-s {
  width: 4.5rem;
  height: 4.5rem;
  border-radius: var(--r-sm);
  flex-shrink: 0;
}
.skeleton-lines {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--sp-2);
}
.line {
  height: 0.9rem;
}
.w-30 {
  width: 30%;
}
.w-60 {
  width: 60%;
}

/* Content -------------------------------------------------------------- */
.cart-content {
  display: grid;
  grid-template-columns: 1fr 22rem;
  gap: var(--sp-7);
  align-items: start;
}

.items {
  list-style: none;
  margin: 0;
  padding: 0;
  border-top: 1px solid var(--line);
}

.item {
  display: grid;
  grid-template-columns: 4.5rem 1fr auto auto auto;
  gap: var(--sp-4);
  align-items: center;
  padding-block: var(--sp-4);
  border-bottom: 1px solid var(--line);
}

.item-thumb {
  width: 4.5rem;
  height: 4.5rem;
  background: var(--paper-soft);
  border-radius: var(--r-sm);
  overflow: hidden;
  flex-shrink: 0;
}
.item-thumb img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.item-info {
  min-width: 0;
}
.name {
  font-family: var(--sans);
  font-weight: 500;
  color: var(--ink);
  font-size: var(--fs-sm);
  line-height: 1.4;
}
.variant {
  margin-top: var(--sp-1);
  font-size: var(--fs-xs);
  color: var(--muted);
}
.sku {
  margin-left: 0.2rem;
}
.unit-price {
  margin-top: var(--sp-1);
  font-size: var(--fs-xs);
  color: var(--muted);
}

.quantity {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
}
.qty-btn {
  width: 2rem;
  height: 2rem;
  border: 1px solid var(--line-strong);
  border-radius: var(--r-sm);
  background: var(--surface);
  color: var(--ink);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: var(--fs-md);
  transition: background var(--dur) var(--ease), border-color var(--dur) var(--ease);
}
.qty-btn:hover:not(:disabled) {
  background: var(--paper-soft);
  border-color: var(--ink);
}
.qty-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
.qty-input {
  width: 3rem;
  text-align: center;
  padding-inline: 0;
}

.line-total {
  font-family: var(--display);
  font-weight: 500;
  color: var(--ink);
  min-width: 5.5rem;
  text-align: right;
  font-size: var(--fs-md);
}

.remove {
  font-size: var(--fs-xs);
  justify-self: end;
}

/* Summary -------------------------------------------------------------- */
.summary {
  background: var(--surface);
  border: 1px solid var(--line);
  border-radius: var(--r-md);
  padding: var(--sp-5);
  display: flex;
  flex-direction: column;
  gap: var(--sp-3);
  position: sticky;
  top: calc(var(--sp-7) + var(--sp-4));
}
.summary-title {
  font-size: var(--fs-md);
  margin-bottom: var(--sp-2);
}
.summary-row {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  font-size: var(--fs-sm);
}
.summary-row.total {
  font-size: var(--fs-md);
  font-weight: 500;
  color: var(--ink);
}
.summary-row.total .tabular {
  font-family: var(--display);
}
.divider {
  margin-block: var(--sp-1);
}
.block {
  margin-top: var(--sp-3);
  width: 100%;
}
.continue {
  text-align: center;
  font-size: var(--fs-sm);
}

@media (max-width: 900px) {
  .cart-content {
    grid-template-columns: 1fr;
  }
  .summary {
    position: static;
    order: -1;
  }
  .item {
    grid-template-columns: 4.5rem 1fr auto;
    grid-template-areas:
      'thumb info total'
      'thumb quantity remove';
    row-gap: var(--sp-3);
  }
  .item-thumb {
    grid-area: thumb;
  }
  .item-info {
    grid-area: info;
  }
  .quantity {
    grid-area: quantity;
  }
  .line-total {
    grid-area: total;
  }
  .remove {
    grid-area: remove;
    justify-self: end;
  }
}
</style>
