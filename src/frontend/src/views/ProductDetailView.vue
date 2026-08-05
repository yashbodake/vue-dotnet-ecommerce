<template>
  <div class="product-detail container">
    <nav class="breadcrumb" aria-label="Breadcrumb">
      <RouterLink to="/" class="crumb">Catalogue</RouterLink>
      <span class="sep" aria-hidden="true">/</span>
      <span class="crumb current">{{ detail?.product.name || 'Product' }}</span>
    </nav>

    <!-- Loading skeleton -->
    <div v-if="loading" class="layout" aria-live="polite">
      <div class="media skeleton" aria-hidden="true"></div>
      <div class="info" aria-hidden="true">
        <div class="skeleton line w-60 h-xl"></div>
        <div class="skeleton line w-30 h-lg"></div>
        <div class="skeleton line w-80"></div>
        <div class="skeleton line w-80"></div>
        <div class="skeleton line w-40 h-btn"></div>
      </div>
    </div>

    <!-- Not found -->
    <div v-else-if="notFound" class="status">
      <h1>Product not found</h1>
      <p class="status-copy">We couldn't find what you were looking for.</p>
      <RouterLink to="/" class="btn btn-primary">Back to catalogue</RouterLink>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="status error" aria-live="polite">
      <p class="status-copy">{{ error }}</p>
      <RouterLink to="/" class="btn btn-primary">Back to catalogue</RouterLink>
    </div>

    <!-- Detail -->
    <div v-else-if="detail" class="layout">
      <div class="media">
        <div v-if="displayImages.length > 0" class="gallery">
          <img
            v-for="(src, index) in displayImages"
            :key="`${src}-${index}`"
            :src="src"
            :alt="detail.product.name"
            class="gallery-image"
            @error="onDetailImageError($event)"
          />
        </div>
        <div v-else class="main-image">
          <img :src="fallbackSrc" :alt="detail.product.name" />
        </div>
      </div>

      <div class="info">
        <h1>{{ detail.product.name }}</h1>
        <p class="price tabular">{{ formattedPrice }}</p>
        <p class="stock">
          <span class="pill" :class="detail.product.stock > 0 ? 'pill-success' : 'pill-danger'">
            {{ detail.product.stock > 0 ? `In stock · ${detail.product.stock}` : 'Sold out' }}
          </span>
        </p>

        <div class="add-to-cart">
          <div v-if="detail.variants.length > 0" class="field variant-field">
            <label for="variant-select">Variant</label>
            <select
              id="variant-select"
              class="select"
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
                {{ variant.skuSuffix ? `(${variant.skuSuffix})` : '' }} —
                {{ formatPrice(variant.priceAdjustment >= 0 ? detail.product.price + variant.priceAdjustment : detail.product.price) }}
                ({{ variant.stock }} in stock)
              </option>
            </select>
          </div>

          <div class="field quantity-field">
            <label for="quantity-input">Quantity</label>
            <input
              id="quantity-input"
              class="input"
              type="number"
              min="1"
              :max="selectedStock"
              v-model.number="quantity"
              :disabled="adding"
            />
          </div>

          <button
            type="button"
            class="btn btn-primary add-button"
            :disabled="detail.product.stock <= 0 || adding || selectedStock <= 0"
            @click="addToCart"
          >
            {{ addButtonText }}
          </button>

          <p v-if="addError" class="pill pill-danger add-error" role="alert">{{ addError }}</p>
        </div>

        <p v-if="detail.product.description" class="description">
          {{ detail.product.description }}
        </p>
      </div>
    </div>

    <section v-if="detail && detail.variants.length > 0" class="variants" aria-label="Product variants">
      <h2>Variants</h2>
      <table class="table variants-table">
        <thead>
          <tr>
            <th scope="col">Name</th>
            <th scope="col">SKU</th>
            <th scope="col">Stock</th>
            <th scope="col">Price adj.</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="variant in detail.variants" :key="variant.productVariantId">
            <td>{{ variant.name }}</td>
            <td class="muted">{{ variant.skuSuffix ?? '—' }}</td>
            <td class="tabular">{{ variant.stock }}</td>
            <td class="tabular">{{ formatPrice(variant.priceAdjustment) }}</td>
          </tr>
        </tbody>
      </table>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watchEffect } from 'vue'
import { useRoute } from 'vue-router'
import { getProduct, type ProductDetail } from '../api/catalog'
import { useCartStore } from '../stores/cart'
import { useAuthStore } from '../stores/auth'
import type { ApiError } from '../api/client'
import { productImageFallback, productImageUrl } from '../utils/productImage'

const props = defineProps<{ id: string }>()
const route = useRoute()
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

const displayImages = computed<string[]>(() => {
  if (!detail.value) return []
  const fromGallery = detail.value.images
    .map((img) => productImageUrl(img.url))
    .filter((url): url is string => Boolean(url))
  if (fromGallery.length > 0) return fromGallery
  const thumb = productImageUrl(detail.value.product.thumbnailUrl)
  return thumb ? [thumb] : []
})

const fallbackSrc = computed(() =>
  productImageFallback(detail.value?.product.name ?? 'Product'),
)

function onDetailImageError(event: Event): void {
  const img = event.target as HTMLImageElement
  img.src = fallbackSrc.value
}

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
  if (detail.value && detail.value.product.stock <= 0) return 'Sold out'
  if (selectedStock.value <= 0) return 'Sold out'
  if (added.value) return 'Added'
  return 'Add to cart'
})

const currencyFormatter = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

function formatPrice(value: number): string {
  return currencyFormatter.format(value)
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
  padding-block: var(--sp-5) var(--sp-9);
}

/* Breadcrumb ----------------------------------------------------------- */
.breadcrumb {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  margin-bottom: var(--sp-6);
  font-size: var(--fs-sm);
}
.crumb {
  color: var(--muted);
  text-decoration: none;
  transition: color var(--dur) var(--ease);
}
.crumb:hover {
  color: var(--ink);
}
.crumb.current {
  color: var(--ink);
  max-width: 22rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.sep {
  color: var(--line-strong);
}

/* Layout --------------------------------------------------------------- */
.layout {
  display: grid;
  grid-template-columns: 1.05fr 1fr;
  gap: var(--sp-8);
  align-items: start;
}

.media {
  background: var(--paper-soft);
  border: 1px solid var(--line);
  border-radius: var(--r-md);
  min-height: 360px;
  overflow: hidden;
  position: sticky;
  top: calc(var(--sp-7) + var(--sp-4));
}
.main-image,
.gallery-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.gallery {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: var(--sp-2);
  padding: var(--sp-2);
}

/* Info ----------------------------------------------------------------- */
.info {
  display: flex;
  flex-direction: column;
  gap: var(--sp-4);
  padding-top: var(--sp-2);
}
.info h1 {
  font-size: var(--fs-2xl);
  line-height: 1.15;
}
.price {
  font-family: var(--display);
  font-size: var(--fs-xl);
  font-weight: 500;
  color: var(--ink);
  letter-spacing: -0.01em;
  margin-top: -var(--sp-1);
}
.stock {
  margin-top: -var(--sp-2);
}

.add-to-cart {
  display: flex;
  flex-wrap: wrap;
  gap: var(--sp-3);
  align-items: flex-end;
  padding-block: var(--sp-4);
  border-block: 1px solid var(--line);
}
.variant-field {
  flex: 1 1 100%;
}
.variant-field .select {
  min-width: 14rem;
}
.quantity-field input {
  width: 5rem;
}
.add-button {
  flex: 1 1 auto;
  min-width: 10rem;
}
.add-error {
  flex-basis: 100%;
  padding: 0.5rem 0.85rem;
  width: fit-content;
}

.description {
  line-height: 1.7;
  color: var(--body);
  white-space: pre-line;
  max-width: 60ch;
}

/* Variants table ------------------------------------------------------- */
.variants {
  margin-top: var(--sp-9);
}
.variants h2 {
  font-size: var(--fs-lg);
  margin-bottom: var(--sp-4);
}
.variants-table {
  background: var(--surface);
  border: 1px solid var(--line);
  border-radius: var(--r-md);
  overflow: hidden;
}

/* Status --------------------------------------------------------------- */
.status {
  text-align: center;
  padding: var(--sp-9) var(--sp-4);
}
.status h1 {
  font-size: var(--fs-xl);
}
.status-copy {
  color: var(--muted);
  margin-block: var(--sp-3) var(--sp-5);
}
.status.error .status-copy {
  color: var(--danger);
}

/* Skeleton ------------------------------------------------------------- */
.line {
  height: 1rem;
  border-radius: var(--r-sm);
}
.h-xl {
  height: 2rem;
}
.h-lg {
  height: 1.5rem;
}
.h-btn {
  height: 2.6rem;
}
.w-30 {
  width: 30%;
}
.w-40 {
  width: 40%;
}
.w-60 {
  width: 60%;
}
.w-80 {
  width: 80%;
}

@media (max-width: 900px) {
  .layout {
    grid-template-columns: 1fr;
    gap: var(--sp-6);
  }
  .media {
    position: static;
    min-height: 280px;
  }
  .info h1 {
    font-size: var(--fs-xl);
  }
}
</style>
