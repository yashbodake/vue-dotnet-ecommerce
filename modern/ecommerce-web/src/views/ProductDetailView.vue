<template>
  <div class="product-detail">
    <div v-if="loading" class="status" aria-live="polite">Loading product...</div>

    <div v-else-if="notFound" class="status not-found">
      <p>Product not found.</p>
      <button type="button" class="back" @click="goBack">Back to catalog</button>
    </div>

    <div v-else-if="error" class="status error" aria-live="polite">
      <p>{{ error }}</p>
      <button type="button" class="back" @click="goBack">Back to catalog</button>
    </div>

    <div v-else-if="detail" class="detail">
      <button type="button" class="back" @click="goBack">Back to catalog</button>

      <div class="layout">
        <div class="media">
          <div v-if="displayImages.length > 0" class="gallery">
            <img
              v-for="img in displayImages"
              :key="img.productImageId"
              :src="img.url"
              :alt="detail.product.name"
              class="gallery-image"
            />
          </div>
          <div v-else-if="detail.product.thumbnailUrl" class="main-image">
            <img :src="detail.product.thumbnailUrl" :alt="detail.product.name" />
          </div>
          <div v-else class="placeholder" aria-hidden="true">No image available</div>
        </div>

        <div class="info">
          <h1>{{ detail.product.name }}</h1>
          <p class="price">{{ formattedPrice }}</p>
          <p class="stock" :class="{ out: detail.product.stock <= 0 }">
            {{ detail.product.stock > 0 ? `In stock (${detail.product.stock})` : 'Out of stock' }}
          </p>

          <div class="add-to-cart">
            <div v-if="detail.variants.length > 0" class="field">
              <label for="variant-select">Variant</label>
              <select
                id="variant-select"
                v-model.number="selectedVariantId"
                :disabled="adding"
              >
                <option :value="undefined">Select a variant</option>
                <option
                  v-for="variant in detail.variants"
                  :key="variant.productVariantId"
                  :value="variant.productVariantId"
                >
                  {{ variant.name }}
                  {{ variant.skuSuffix ? `(${variant.skuSuffix})` : '' }}
                  — {{ formatPrice(variant.priceAdjustment >= 0 ? detail.product.price + variant.priceAdjustment : detail.product.price) }}
                  ({{ variant.stock }} in stock)
                </option>
              </select>
            </div>

            <div class="field quantity-field">
              <label for="quantity-input">Quantity</label>
              <input
                id="quantity-input"
                type="number"
                min="1"
                :max="selectedStock"
                v-model.number="quantity"
                :disabled="adding"
              />
            </div>

            <button
              type="button"
              class="add-button"
              :disabled="detail.product.stock <= 0 || adding || selectedStock <= 0"
              @click="addToCart"
            >
              {{ addButtonText }}
            </button>

            <p v-if="addError" class="add-error" role="alert">{{ addError }}</p>
          </div>

          <p v-if="detail.product.description" class="description">
            {{ detail.product.description }}
          </p>
        </div>
      </div>

      <section v-if="detail.variants.length > 0" class="variants" aria-label="Product variants">
        <h2>Variants</h2>
        <table class="variants-table">
          <thead>
            <tr>
              <th scope="col">Name</th>
              <th scope="col">SKU</th>
              <th scope="col">Stock</th>
              <th scope="col">Price Adj.</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="variant in detail.variants" :key="variant.productVariantId">
              <td>{{ variant.name }}</td>
              <td>{{ variant.skuSuffix ?? '-' }}</td>
              <td>{{ variant.stock }}</td>
              <td>{{ formatPrice(variant.priceAdjustment) }}</td>
            </tr>
          </tbody>
        </table>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watchEffect } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { getProduct, type ProductDetail, type ProductImage } from '../api/catalog'
import { useCartStore } from '../stores/cart'
import { useAuthStore } from '../stores/auth'
import type { ApiError } from '../api/client'

const props = defineProps<{ id: string }>()
const route = useRoute()
const router = useRouter()
const cartStore = useCartStore()
const authStore = useAuthStore()

const loading = ref(true)
const detail = ref<ProductDetail | null>(null)
const error = ref<string | null>(null)
const notFound = ref(false)
const quantity = ref(1)
const selectedVariantId = ref<number | undefined>(undefined)
const adding = ref(false)
const added = ref(false)
const addError = ref<string | null>(null)

const resolvedId = computed(() => {
  const raw = props.id ?? route.params.id
  const parsed = Number(raw)
  return Number.isNaN(parsed) ? null : parsed
})

const displayImages = computed<ProductImage[]>(() => {
  if (!detail.value) return []
  const images = detail.value.images
  return images.length > 0 ? images : []
})

const formattedPrice = computed(() =>
  detail.value ? formatPrice(detail.value.product.price) : '',
)

const selectedStock = computed(() => {
  if (!detail.value) return 0
  if (selectedVariantId.value === undefined) return detail.value.product.stock
  const variant = detail.value.variants.find((v) => v.productVariantId === selectedVariantId.value)
  return variant ? variant.stock : detail.value.product.stock
})

const addButtonText = computed(() => {
  if (detail.value && detail.value.product.stock <= 0) return 'Out of stock'
  if (selectedStock.value <= 0) return 'Out of stock'
  if (added.value) return 'Added!'
  return 'Add to Cart'
})

function formatPrice(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

function goBack(): void {
  router.push('/')
}

async function addToCart(): Promise<void> {
  if (!detail.value || adding.value) return
  const productId = detail.value.product.productId
  const effectiveStock = selectedStock.value
  if (effectiveStock <= 0) return

  const clampedQuantity = Math.max(1, Math.min(quantity.value, effectiveStock))
  quantity.value = clampedQuantity

  adding.value = true
  addError.value = null
  try {
    await cartStore.addItem(
      productId,
      clampedQuantity,
      selectedVariantId.value,
      authStore.isAuthenticated,
    )
    added.value = true
    setTimeout(() => {
      added.value = false
    }, 1200)
  } catch (e) {
    addError.value = errorMessage(e)
  } finally {
    adding.value = false
  }
}

function errorMessage(e: unknown): string {
  if (e && typeof e === 'object' && 'message' in e) {
    return String((e as { message: unknown }).message)
  }
  return 'Failed to add item to cart'
}

watchEffect(async () => {
  const id = resolvedId.value
  if (id == null) {
    loading.value = false
    notFound.value = true
    error.value = null
    detail.value = null
    return
  }

  loading.value = true
  error.value = null
  notFound.value = false

  try {
    detail.value = await getProduct(id)
  } catch (err) {
    detail.value = null
    const apiError = err as ApiError
    if (apiError.status === 404) {
      notFound.value = true
      error.value = null
    } else {
      notFound.value = false
      error.value = apiError.message || 'Failed to load product.'
    }
  } finally {
    loading.value = false
  }
})
</script>

<style scoped>
.product-detail {
  max-width: 1200px;
  margin: 0 auto;
  padding: 1.5rem;
}

.status {
  padding: 2rem;
  text-align: center;
  color: #6b7280;
}
.status.error {
  color: #dc2626;
}
.status.not-found {
  color: #374151;
}

.back {
  display: inline-flex;
  padding: 0.5rem 1rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  background: #fff;
  cursor: pointer;
  font-size: 0.9rem;
  margin-bottom: 1rem;
}
.back:hover {
  background: #f3f4f6;
}

.layout {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2rem;
}
@media (max-width: 768px) {
  .layout {
    grid-template-columns: 1fr;
  }
}

.media {
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  min-height: 300px;
  overflow: hidden;
}
.main-image img,
.gallery-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.gallery {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
  gap: 0.5rem;
  width: 100%;
  padding: 0.5rem;
}
.placeholder {
  color: #9ca3af;
  font-size: 0.9rem;
}

.info h1 {
  margin: 0 0 0.5rem;
  font-size: 1.75rem;
  color: #111827;
}
.price {
  font-size: 1.5rem;
  font-weight: 700;
  color: #111827;
  margin: 0 0 0.5rem;
}
.stock {
  font-size: 0.9rem;
  color: #059669;
  margin: 0 0 1rem;
}
.stock.out {
  color: #dc2626;
}
.description {
  line-height: 1.5;
  color: #4b5563;
  white-space: pre-line;
}

.add-to-cart {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  align-items: flex-end;
  margin-bottom: 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  font-size: 0.85rem;
  color: #374151;
}

.quantity-field input {
  width: 4rem;
  padding: 0.4rem 0.5rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font: inherit;
}

.field select {
  min-width: 12rem;
  padding: 0.4rem 0.5rem;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font: inherit;
  background: #fff;
}

.add-button {
  padding: 0.55rem 1rem;
  border: none;
  border-radius: 6px;
  background: #111827;
  color: #fff;
  font-size: 0.9rem;
  cursor: pointer;
}
.add-button:hover:not(:disabled) {
  background: #374151;
}
.add-button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.add-error {
  width: 100%;
  margin: 0;
  padding: 0.5rem 0.75rem;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 6px;
  color: #dc2626;
  font-size: 0.85rem;
}

.variants {
  margin-top: 2rem;
}
.variants h2 {
  font-size: 1.25rem;
  color: #111827;
  margin-bottom: 0.75rem;
}
.variants-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
}
.variants-table th,
.variants-table td {
  border: 1px solid #e5e7eb;
  padding: 0.6rem 0.75rem;
  text-align: left;
}
.variants-table th {
  background: #f9fafb;
  color: #374151;
  font-weight: 600;
}
.variants-table td {
  color: #4b5563;
}
</style>
