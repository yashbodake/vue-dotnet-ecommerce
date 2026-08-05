<template>
  <div class="checkout container">
    <header class="page-head">
      <span class="eyebrow">Checkout</span>
      <h1>Almost yours.</h1>
    </header>

    <div v-if="cartStore.loading" class="status" aria-live="polite">Loading your cart…</div>

    <div v-else-if="cartStore.itemCount === 0" class="empty">
      <h2>Your cart is empty.</h2>
      <p class="status-copy">Add a piece to your bag before checking out.</p>
      <RouterLink to="/" class="btn btn-primary">Browse the catalogue</RouterLink>
    </div>

    <template v-else>
      <ol class="step-indicator" aria-label="Checkout steps">
        <li class="step" :class="{ active: step === 1, done: step > 1 }">
          <span class="step-number">{{ step > 1 ? '✓' : '1' }}</span>
          <span class="step-label">Address</span>
        </li>
        <li class="step" :class="{ active: step === 2, done: step > 2 }">
          <span class="step-number">{{ step > 2 ? '✓' : '2' }}</span>
          <span class="step-label">Shipping</span>
        </li>
        <li class="step" :class="{ active: step === 3 }">
          <span class="step-number">3</span>
          <span class="step-label">Payment</span>
        </li>
      </ol>

      <div v-if="error" class="pill pill-danger error-banner" role="alert">{{ error }}</div>

      <!-- Step 1: Address -->
      <form v-if="step === 1" class="step-form" @submit.prevent="goToShipping">
        <h2>Shipping address</h2>
        <div class="field">
          <label for="fullName">Full name <span class="req">*</span></label>
          <input id="fullName" class="input" v-model="address.fullName" type="text" required :disabled="submitting" />
        </div>
        <div class="field">
          <label for="line1">Address line 1 <span class="req">*</span></label>
          <input id="line1" class="input" v-model="address.line1" type="text" required :disabled="submitting" />
        </div>
        <div class="field">
          <label for="line2">Address line 2</label>
          <input id="line2" class="input" v-model="address.line2" type="text" :disabled="submitting" />
        </div>
        <div class="field-row">
          <div class="field">
            <label for="city">City <span class="req">*</span></label>
            <input id="city" class="input" v-model="address.city" type="text" required :disabled="submitting" />
          </div>
          <div class="field">
            <label for="state">State / region <span class="req">*</span></label>
            <input id="state" class="input" v-model="address.state" type="text" required :disabled="submitting" />
          </div>
        </div>
        <div class="field-row">
          <div class="field">
            <label for="postal">Postal code <span class="req">*</span></label>
            <input id="postal" class="input" v-model="address.postalCode" type="text" required :disabled="submitting" />
          </div>
          <div class="field">
            <label for="country">Country <span class="req">*</span></label>
            <input id="country" class="input" v-model="address.country" type="text" required :disabled="submitting" />
          </div>
        </div>
        <div class="actions">
          <RouterLink to="/cart" class="btn btn-ghost">← Back to cart</RouterLink>
          <button type="submit" class="btn btn-primary" :disabled="submitting">Continue to shipping</button>
        </div>
      </form>

      <!-- Step 2: Shipping -->
      <form v-if="step === 2" class="step-form" @submit.prevent="goToPayment">
        <h2>Shipping method</h2>
        <div v-if="shippingLoading" class="status">Loading shipping options…</div>
        <fieldset v-else class="shipping-options">
          <legend class="visually-hidden">Choose a shipping method</legend>
          <label
            v-for="option in shippingOptions"
            :key="option.code"
            class="shipping-option"
            :class="{ selected: selectedShipping === option.code }"
          >
            <input
              v-model="selectedShipping"
              type="radio"
              name="shipping"
              :value="option.code"
              required
              :disabled="submitting"
            />
            <div class="shipping-info">
              <p class="shipping-name">{{ option.name }}</p>
              <p class="shipping-desc">{{ option.description }}</p>
              <p class="shipping-est">Estimated {{ option.estimatedDays }} business days</p>
            </div>
          </label>
        </fieldset>
        <div class="actions">
          <button type="button" class="btn btn-ghost" @click="step = 1" :disabled="submitting || shippingLoading">
            ← Back
          </button>
          <button type="submit" class="btn btn-primary" :disabled="submitting || shippingLoading || !selectedShipping">
            Continue to payment
          </button>
        </div>
      </form>

      <!-- Step 3: Payment -->
      <form v-if="step === 3" class="step-form" @submit.prevent="submitOrder">
        <h2>Payment</h2>
        <div class="field">
          <label for="cardName">Name on card <span class="req">*</span></label>
          <input id="cardName" class="input" v-model="payment.cardName" type="text" required :disabled="submitting" />
        </div>
        <div class="field">
          <label for="cardNumber">Card number <span class="req">*</span></label>
          <input
            id="cardNumber"
            class="input tabular"
            v-model="payment.cardNumber"
            type="text"
            inputmode="numeric"
            maxlength="19"
            placeholder="1234 5678 9012 3456"
            required
            :disabled="submitting"
          />
        </div>
        <div class="field-row">
          <div class="field">
            <label for="expiry">Expiry (MM/YY) <span class="req">*</span></label>
            <input
              id="expiry"
              class="input tabular"
              v-model="payment.cardExpiry"
              type="text"
              maxlength="5"
              placeholder="MM/YY"
              required
              :disabled="submitting"
            />
          </div>
          <div class="field">
            <label for="cvv">CVV <span class="req">*</span></label>
            <input
              id="cvv"
              class="input tabular"
              v-model="payment.cardCvv"
              type="text"
              inputmode="numeric"
              maxlength="4"
              placeholder="123"
              required
              :disabled="submitting"
            />
          </div>
        </div>

        <h3>Order summary</h3>
        <div class="card summary-box">
          <ul class="item-list">
            <li v-for="item in cartStore.items" :key="item.cartItemId" class="summary-item">
              <div class="summary-item-main">
                <span class="item-name">{{ item.productName }}</span>
                <span v-if="item.variantName" class="item-variant">{{ item.variantName }}</span>
              </div>
              <span class="item-qty muted">×{{ item.quantity }}</span>
              <span class="item-total tabular">{{ formatPrice(item.lineTotal) }}</span>
            </li>
          </ul>
          <div class="summary-total">
            <span>Total</span>
            <span class="tabular">{{ formatPrice(cartStore.total) }}</span>
          </div>
        </div>

        <h3>Shipping details</h3>
        <div class="card summary-box address-summary">
          <p>{{ formattedAddress }}</p>
          <p class="method">Method: {{ selectedShipping }}</p>
        </div>

        <div class="actions">
          <button type="button" class="btn btn-ghost" @click="step = 2" :disabled="submitting">← Back</button>
          <button type="submit" class="btn btn-primary" :disabled="submitting">
            {{ submitting ? 'Placing order…' : 'Place order' }}
          </button>
        </div>
      </form>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useCartStore } from '../stores/cart'
import { getShippingOptions, placeOrder, type ShippingOption, type OrderConfirmation } from '../api/checkout'

const router = useRouter()
const authStore = useAuthStore()
const cartStore = useCartStore()

const step = ref(1)
const shippingOptions = ref<ShippingOption[]>([])
const shippingLoading = ref(false)
const selectedShipping = ref('')
const submitting = ref(false)
const error = ref<string | null>(null)

const address = reactive({
  fullName: '',
  line1: '',
  line2: '',
  city: '',
  state: '',
  postalCode: '',
  country: '',
})

const payment = reactive({
  cardName: '',
  cardNumber: '',
  cardExpiry: '',
  cardCvv: '',
})

const formattedAddress = computed(() => {
  const parts = [
    address.fullName,
    [address.line1, address.line2].filter(Boolean).join(', '),
    address.city,
    `${address.state} ${address.postalCode}`,
    address.country,
  ].filter(Boolean)
  return `${parts.join(', ')} (${selectedShipping.value})`
})

onMounted(() => {
  if (!authStore.isAuthenticated) {
    router.replace('/login?redirect=/checkout')
    return
  }
  cartStore.fetchCart(true)
})

function goToShipping(): void {
  error.value = null
  if (!address.fullName || !address.line1 || !address.city || !address.state || !address.postalCode || !address.country) {
    error.value = 'Please fill in all required address fields.'
    return
  }
  step.value = 2
  loadShippingOptions()
}

async function loadShippingOptions(): Promise<void> {
  if (shippingOptions.value.length) return
  shippingLoading.value = true
  try {
    shippingOptions.value = await getShippingOptions()
  } catch (e) {
    error.value =
      e && typeof e === 'object' && 'message' in e
        ? String((e as { message?: unknown }).message)
        : 'Failed to load shipping options.'
  } finally {
    shippingLoading.value = false
  }
}

function goToPayment(): void {
  error.value = null
  if (!selectedShipping.value) {
    error.value = 'Please select a shipping method.'
    return
  }
  step.value = 3
}

function formatPrice(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

async function submitOrder(): Promise<void> {
  error.value = null
  if (!payment.cardName || !payment.cardNumber || !payment.cardExpiry || !payment.cardCvv) {
    error.value = 'Please fill in all card details.'
    return
  }

  submitting.value = true
  try {
    const confirmation: OrderConfirmation = await placeOrder({
      shippingAddress: formattedAddress.value,
      shippingMethod: selectedShipping.value,
      cardName: payment.cardName,
      cardNumber: payment.cardNumber,
      cardExpiry: payment.cardExpiry,
      cardCvv: payment.cardCvv,
    })
    router.push(`/checkout/confirmation/${confirmation.orderId}`)
  } catch (e) {
    error.value =
      e && typeof e === 'object' && 'message' in e
        ? String((e as { message?: unknown }).message)
        : 'Order failed. Please try again.'
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.checkout {
  max-width: 48rem;
  padding-block: var(--sp-8) var(--sp-9);
}

.page-head {
  margin-bottom: var(--sp-6);
}
.page-head h1 {
  margin-top: var(--sp-2);
}

.status,
.empty {
  text-align: center;
  padding: var(--sp-9) var(--sp-4);
}
.empty h2 {
  font-size: var(--fs-xl);
}
.status-copy {
  color: var(--muted);
  margin-block: var(--sp-3) var(--sp-5);
}

.error-banner {
  margin-bottom: var(--sp-5);
  padding: 0.65rem 1rem;
}

/* Step indicator ------------------------------------------------------- */
.step-indicator {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
  list-style: none;
  margin: 0 0 var(--sp-7);
  padding: 0 0 var(--sp-5);
  border-bottom: 1px solid var(--line);
}
.step {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  color: var(--muted);
  font-size: var(--fs-sm);
  position: relative;
}
.step::after {
  content: '';
  display: inline-block;
  width: 2rem;
  height: 1px;
  background: var(--line);
  margin-left: var(--sp-2);
}
.step:last-child::after {
  display: none;
}
.step.active {
  color: var(--ink);
  font-weight: 500;
}
.step.done {
  color: var(--success);
}
.step-number {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.7rem;
  height: 1.7rem;
  border-radius: 50%;
  border: 1px solid var(--line-strong);
  background: var(--surface);
  color: var(--muted);
  font-size: var(--fs-xs);
  font-weight: 500;
}
.step.active .step-number {
  background: var(--ink);
  border-color: var(--ink);
  color: var(--surface);
}
.step.done .step-number {
  background: var(--success);
  border-color: var(--success);
  color: var(--surface);
}

/* Forms ---------------------------------------------------------------- */
.step-form h2 {
  font-size: var(--fs-lg);
  margin-bottom: var(--sp-5);
}
.step-form h3 {
  font-family: var(--sans);
  font-size: var(--fs-xs);
  font-weight: 500;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: var(--muted);
  margin: var(--sp-6) 0 var(--sp-3);
}
.step-form .field {
  margin-bottom: var(--sp-4);
}
.req {
  color: var(--danger);
}

.actions {
  display: flex;
  gap: var(--sp-3);
  margin-top: var(--sp-6);
  flex-wrap: wrap;
  align-items: center;
}

/* Shipping options ----------------------------------------------------- */
.shipping-options {
  border: 1px solid var(--line);
  border-radius: var(--r-md);
  padding: 0;
  margin: 0 0 var(--sp-4);
  overflow: hidden;
}
.shipping-option {
  display: flex;
  align-items: flex-start;
  gap: var(--sp-3);
  padding: var(--sp-4);
  border-bottom: 1px solid var(--line);
  cursor: pointer;
  transition: background var(--dur) var(--ease);
}
.shipping-option:last-child {
  border-bottom: none;
}
.shipping-option:hover,
.shipping-option.selected {
  background: var(--paper-soft);
}
.shipping-option input {
  margin-top: 0.25rem;
  accent-color: var(--accent);
}
.shipping-info {
  display: flex;
  flex-direction: column;
  gap: var(--sp-1);
}
.shipping-name {
  font-weight: 500;
  color: var(--ink);
  font-size: var(--fs-sm);
}
.shipping-desc,
.shipping-est {
  font-size: var(--fs-xs);
  color: var(--muted);
}

/* Summary box ---------------------------------------------------------- */
.summary-box {
  padding: var(--sp-4);
  background: var(--paper-soft);
}
.item-list {
  list-style: none;
  margin: 0;
  padding: 0;
}
.summary-item {
  display: grid;
  grid-template-columns: 1fr auto auto;
  gap: var(--sp-3);
  padding-block: var(--sp-2);
  border-bottom: 1px solid var(--line);
  align-items: baseline;
}
.summary-item:last-child {
  border-bottom: none;
}
.summary-item-main {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}
.item-name {
  font-weight: 500;
  color: var(--ink);
  font-size: var(--fs-sm);
}
.item-variant {
  font-size: var(--fs-xs);
  color: var(--muted);
}
.item-total {
  font-weight: 500;
  color: var(--ink);
  font-size: var(--fs-sm);
}
.summary-total {
  display: flex;
  justify-content: space-between;
  margin-top: var(--sp-3);
  padding-top: var(--sp-3);
  border-top: 1px solid var(--line-strong);
  font-size: var(--fs-md);
  font-weight: 500;
  color: var(--ink);
}
.summary-total .tabular {
  font-family: var(--display);
}
.address-summary p {
  margin: 0 0 var(--sp-1);
  color: var(--body);
  line-height: 1.6;
  font-size: var(--fs-sm);
}
.address-summary .method {
  margin: 0;
  font-weight: 500;
  color: var(--ink);
}

.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  overflow: hidden;
  clip: rect(0 0 0 0);
}

@media (max-width: 640px) {
  .step::after {
    width: 1rem;
  }
  .step-label {
    display: none;
  }
  .actions {
    flex-direction: column-reverse;
    align-items: stretch;
  }
  .actions .btn {
    width: 100%;
  }
}
</style>
