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
import type { ApiError } from '../api/client'

const props = defineProps<{ id: string }>()
const route = useRoute()
const router = useRouter()

const loading = ref(true)
const detail = ref<ProductDetail | null>(null)
const error = ref<string | null>(null)
const notFound = ref(false)

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

function formatPrice(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

function goBack(): void {
  router.push('/')
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
