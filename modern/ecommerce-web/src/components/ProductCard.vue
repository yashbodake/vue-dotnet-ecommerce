<template>
  <article class="product-card" @click="goToDetail">
    <div class="thumb">
      <img
        v-if="product.thumbnailUrl"
        :src="product.thumbnailUrl"
        :alt="product.name"
        loading="lazy"
      />
      <div v-else class="placeholder" aria-hidden="true">No image</div>
    </div>
    <div class="info">
      <h3 class="name">{{ product.name }}</h3>
      <p class="price">{{ formattedPrice }}</p>
      <p class="stock" :class="{ out: product.stock <= 0 }">
        {{ product.stock > 0 ? `In stock (${product.stock})` : 'Out of stock' }}
      </p>
      <button
        type="button"
        class="add-button"
        :disabled="product.stock <= 0 || adding"
        @click.stop="addToCart"
      >
        {{ feedbackText }}
      </button>
    </div>
  </article>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useCartStore } from '../stores/cart'
import { useAuthStore } from '../stores/auth'
import type { Product } from '../api/catalog'

const props = defineProps<{ product: Product }>()
const router = useRouter()
const cartStore = useCartStore()
const authStore = useAuthStore()

const adding = ref(false)
const added = ref(false)

const formattedPrice = computed(() =>
  new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(
    props.product.price,
  ),
)

const feedbackText = computed(() => {
  if (props.product.stock <= 0) return 'Out of stock'
  if (added.value) return 'Added!'
  return 'Add to Cart'
})

async function addToCart(): Promise<void> {
  if (props.product.stock <= 0 || adding.value) return
  adding.value = true
  try {
    await cartStore.addItem(props.product.productId, 1, undefined, authStore.isAuthenticated)
    added.value = true
    setTimeout(() => {
      added.value = false
    }, 1200)
  } finally {
    adding.value = false
  }
}

function goToDetail(): void {
  router.push(`/products/${props.product.productId}`)
}
</script>

<style scoped>
.product-card {
  display: flex;
  flex-direction: column;
  background: #fff;
  border: 1px solid #e5e7eb;
  border-radius: 8px;
  overflow: hidden;
  cursor: pointer;
  transition: box-shadow 0.15s ease, transform 0.15s ease;
}
.product-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  transform: translateY(-2px);
}
.thumb {
  width: 100%;
  aspect-ratio: 1 / 1;
  background: #f9fafb;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}
.thumb img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.placeholder {
  color: #9ca3af;
  font-size: 0.85rem;
}
.info {
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  flex: 1;
  gap: 0.25rem;
}
.name {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 600;
  color: #111827;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  text-overflow: ellipsis;
  min-height: 2.6em;
}
.price {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  color: #111827;
}
.stock {
  margin: 0;
  font-size: 0.8rem;
  color: #059669;
}
.stock.out {
  color: #dc2626;
}

.add-button {
  margin-top: auto;
  padding: 0.45rem 0.75rem;
  border: none;
  border-radius: 6px;
  background: #111827;
  color: #fff;
  font-size: 0.85rem;
  cursor: pointer;
  transition: background 0.15s ease;
}
.add-button:hover:not(:disabled) {
  background: #374151;
}
.add-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>