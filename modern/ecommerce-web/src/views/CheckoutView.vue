<template>
  <div class="checkout">
    <h1>Checkout</h1>

    <div v-if="cartStore.loading" class="status" aria-live="polite">Loading cart...</div>

    <div v-else-if="cartStore.itemCount === 0" class="status empty">
      <p>Your cart is empty.</p>
      <RouterLink to="/" class="button-link">Continue shopping</RouterLink>
    </div>

    <template v-else>
      <div class="step-indicator" aria-label="Checkout steps">
        <div class="step" :class="{ active: step === 1, completed: step > 1 }">
          <span class="step-number">1</span>
          <span class="step-label">Address</span>
        </div>
        <div class="step" :class="{ active: step === 2, completed: step > 2 }">
          <span class="step-number">2</span>
          <span class="step-label">Shipping</span>
        </div>
        <div class="step" :class="{ active: step === 3, completed: step > 3 }">
          <span class="step-number">3</span>
          <span class="step-label">Payment</span>
        </div>
      </div>

      <div v-if="error" class="error-banner" role="alert">{{ error }}</div>

      <form v-if="step === 1" class="step-form" @submit.prevent="goToShipping">
        <h2>Shipping Address</h2>
        <label class="field">
          <span>Full Name <span class="required">*</span></span>
          <input v-model="address.fullName" type="text" required :disabled="submitting" />
        </label>
        <label class="field">
          <span>Address Line 1 <span class="required">*</span></span>
          <input v-model="address.line1" type="text" required :disabled="submitting" />
        </label>
        <label class="field">
          <span>Address Line 2</span>
          <input v-model="address.line2" type="text" :disabled="submitting" />
        </label>
        <div class="field-row">
          <label class="field">
            <span>City <span class="required">*</span></span>
            <input v-model="address.city" type="text" required :disabled="submitting" />
          </label>
          <label class="field">
            <span>State / Region <span class="required">*</span></span>
            <input v-model="address.state" type="text" required :disabled="submitting" />
          </label>
        </div>
        <div class="field-row">
          <label class="field">
            <span>Postal Code <span class="required">*</span></span>
            <input v-model="address.postalCode" type="text" required :disabled="submitting" />
          </label>
          <label class="field">
            <span>Country <span class="required">*</span></span>
            <input v-model="address.country" type="text" required :disabled="submitting" />
          </label>
        </div>
        <div class="actions">
          <RouterLink to="/cart" class="button-link secondary">Back to cart</RouterLink>
          <button type="submit" class="button-link" :disabled="submitting">
            Continue to Shipping
          </button>
        </div>
      </form>

      <form v-if="step === 2" class="step-form" @submit.prevent="goToPayment">
        <h2>Shipping Method</h2>
        <div v-if="shippingLoading" class="status">Loading shipping options...</div>
        <fieldset v-else class="shipping-options">
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
              <p class="shipping-est">Estimated: {{ option.estimatedDays }} business days</p>
            </div>
          </label>
        </fieldset>
        <div class="actions">
          <button type="button" class="button-link secondary" @click="step = 1" :disabled="submitting || shippingLoading">
            Back
          </button>
          <button type="submit" class="button-link" :disabled="submitting || shippingLoading || !selectedShipping">
            Continue to Payment
          </button>
        </div>
      </form>

      <form v-if="step === 3" class="step-form" @submit.prevent="submitOrder">
        <h2>Payment</h2>
        <label class="field">
          <span>Card Name <span class="required">*</span></span>
          <input v-model="payment.cardName" type="text" required :disabled="submitting" />
        </label>
        <label class="field">
          <span>Card Number <span class="required">*</span></span>
          <input
            v-model="payment.cardNumber"
            type="text"
            inputmode="numeric"
            maxlength="19"
            placeholder="1234 5678 9012 3456"
            required
            :disabled="submitting"
          />
        </label>
        <div class="field-row">
          <label class="field">
            <span>Expiry (MM/YY) <span class="required">*</span></span>
            <input
              v-model="payment.cardExpiry"
              type="text"
              maxlength="5"
              placeholder="MM/YY"
              required
              :disabled="submitting"
            />
          </label>
          <label class="field">
            <span>CVV <span class="required">*</span></span>
            <input
              v-model="payment.cardCvv"
              type="text"
              inputmode="numeric"
              maxlength="4"
              placeholder="123"
              required
              :disabled="submitting"
            />
          </label>
        </div>

        <h3>Order Summary</h3>
        <div class="summary-box">
          <ul class="item-list">
            <li v-for="item in cartStore.items" :key="item.cartItemId" class="item">
              <span class="item-name">{{ item.productName }}</span>
              <span v-if="item.variantName" class="item-variant">{{ item.variantName }}</span>
              <span class="item-qty">x{{ item.quantity }}</span>
              <span class="item-total">{{ formatPrice(item.lineTotal) }}</span>
            </li>
          </ul>
          <div class="summary-total">
            <span>Total</span>
            <span>{{ formatPrice(cartStore.total) }}</span>
          </div>
        </div>

        <h3>Shipping Details</h3>
        <div class="summary-box address-summary">
          <p>{{ formattedAddress }}</p>
          <p class="method">Method: {{ selectedShipping }}</p>
        </div>

        <div class="actions">
          <button type="button" class="button-link secondary" @click="step = 2" :disabled="submitting">
            Back
          </button>
          <button type="submit" class="button-link" :disabled="submitting">
            {{ submitting ? 'Placing order…' : 'Place Order' }}
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
    error.value = e && typeof e === 'object' && 'message' in e
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
    error.value = e && typeof e === 'object' && 'message' in e
      ? String((e as { message?: unknown }).message)
      : 'Order failed. Please try again.'
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.checkout {
  max-width: 800px;
  margin: 0 auto;
  padding: 1.5rem;
}

.checkout h1 {
  margin: 0 0 1.25rem;
  font-size: 1.75rem;
  color: #111827;
}

.status {
  padding: 2rem;
  text-align: center;
  color: #6b7280;
}

.status.empty p {
  margin: 0 0 1rem;
}

.error-banner {
  margin-bottom: 1rem;
  padding: 0.75rem 1rem;
  background: #fef2f2;
  border: 1px solid #fca5a5;
  border-radius: 6px;
  color: #dc2626;
  font-size: 0.9rem;
}

.step-indicator {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid #e5e7eb;
}

.step {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  color: #9ca3af;
  font-size: 0.9rem;
}

.step.active {
  color: #111827;
  font-weight: 600;
}

.step.completed {
  color: #10b981;
}

.step-number {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.6rem;
  height: 1.6rem;
  border-radius: 50%;
  background: #e5e7eb;
  color: #6b7280;
  font-size: 0.8rem;
  font-weight: 600;
}

.step.active .step-number {
  background: #aa3bff;
  color: #fff;
}

.step.completed .step-number {
  background: #10b981;
  color: #fff;
}

.step-form h2 {
  margin: 0 0 1rem;
  font-size: 1.25rem;
  color: #111827;
}

.step-form h3 {
  margin: 1.25rem 0 0.75rem;
  font-size: 1rem;
  color: #374151;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  margin-bottom: 0.9rem;
  font-size: 0.85rem;
  color: #374151;
}

.field input {
  padding: 0.5rem 0.6rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font: inherit;
}

.field-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
}

@media (max-width: 480px) {
  .field-row {
    grid-template-columns: 1fr;
    gap: 0;
  }
}

.required {
  color: #dc2626;
}

.actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 1rem;
  flex-wrap: wrap;
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
  align-items: center;
}

.button-link.secondary {
  background: #fff;
  color: #374151;
  border: 1px solid #d1d5db;
}

.button-link:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.shipping-options {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 0;
  margin: 0 0 1rem;
}

.shipping-option {
  display: flex;
  align-items: flex-start;
  gap: 0.75rem;
  padding: 0.9rem 1rem;
  border-bottom: 1px solid #e5e7eb;
  cursor: pointer;
}

.shipping-option:last-child {
  border-bottom: none;
}

.shipping-option.selected {
  background: #f9fafb;
}

.shipping-option input {
  margin-top: 0.25rem;
}

.shipping-info {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.shipping-name {
  margin: 0;
  font-weight: 600;
  color: #111827;
}

.shipping-desc,
.shipping-est {
  margin: 0;
  font-size: 0.85rem;
  color: #6b7280;
}

.summary-box {
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  padding: 1rem;
  background: #f9fafb;
}

.item-list {
  list-style: none;
  margin: 0;
  padding: 0;
}

.item {
  display: grid;
  grid-template-columns: 1fr auto auto;
  gap: 0.5rem;
  padding: 0.5rem 0;
  border-bottom: 1px solid #e5e7eb;
  align-items: baseline;
}

.item:last-child {
  border-bottom: none;
}

.item-name {
  font-weight: 500;
  color: #111827;
}

.item-variant {
  grid-column: 1 / 2;
  font-size: 0.8rem;
  color: #6b7280;
}

.item-qty {
  color: #6b7280;
  font-size: 0.9rem;
}

.item-total {
  font-weight: 600;
  color: #111827;
}

.summary-total {
  display: flex;
  justify-content: space-between;
  margin-top: 0.75rem;
  padding-top: 0.75rem;
  border-top: 2px solid #d1d5db;
  font-size: 1.1rem;
  font-weight: 700;
  color: #111827;
}

.address-summary p {
  margin: 0 0 0.4rem;
  color: #374151;
  line-height: 1.4;
}

.address-summary .method {
  margin: 0;
  font-weight: 500;
  color: #111827;
}
</style>
