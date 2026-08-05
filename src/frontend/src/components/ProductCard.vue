<template>
  <article class="product-card" @click="goToDetail">
    <div class="thumb">
      <img :src="imageSrc" :alt="product.name" loading="lazy" @error="onImageError" />
      <span v-if="product.stock <= 0" class="thumb-flag">Sold out</span>
    </div>
    <div class="info">
      <h3 class="name">{{ product.name }}</h3>
      <p class="price tabular">{{ formattedPrice }}</p>
      <button
        type="button"
        class="btn btn-sm add-btn"
        :class="added ? 'btn-accent' : 'btn-ghost'"
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
import { productImageFallback, productImageUrl } from '../utils/productImage'

const props = defineProps<{ product: Product }>()
const router = useRouter()
const cartStore = useCartStore()
const authStore = useAuthStore()

const adding = ref(false)
const added = ref(false)
const imageBroken = ref(false)

const imageSrc = computed(() => {
  if (imageBroken.value) return productImageFallback(props.product.name)
  return productImageUrl(props.product.thumbnailUrl) ?? productImageFallback(props.product.name)
})

function onImageError(): void {
  imageBroken.value = true
}

const currencyFormatter = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

const formattedPrice = computed(() =>
  currencyFormatter.format(props.product.price),
)

const feedbackText = computed(() => {
  if (props.product.stock <= 0) return 'Sold out'
  if (added.value) return 'Added'
  return 'Add to cart'
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
  background: transparent;
  cursor: pointer;
  transition: transform var(--dur) var(--ease);
}
.product-card:hover {
  transform: translateY(-2px);
}

.thumb {
  position: relative;
  width: 100%;
  aspect-ratio: 1 / 1;
  background: var(--paper-soft);
  overflow: hidden;
}
.thumb img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform var(--dur-slow) var(--ease);
}
.product-card:hover .thumb img {
  transform: scale(1.04);
}

.thumb-flag {
  position: absolute;
  top: var(--sp-3);
  left: var(--sp-3);
  background: var(--ink);
  color: var(--surface);
  font-size: var(--fs-xs);
  font-weight: 500;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  padding: 0.25rem 0.6rem;
  border-radius: var(--r-sharp);
}

.info {
  padding-top: var(--sp-4);
  display: flex;
  flex-direction: column;
  gap: var(--sp-2);
  flex: 1;
}

.name {
  font-family: var(--sans);
  font-size: var(--fs-sm);
  font-weight: 500;
  color: var(--ink);
  letter-spacing: 0;
  line-height: 1.4;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  text-overflow: ellipsis;
  min-height: 2.8em;
}

.price {
  font-family: var(--display);
  font-size: var(--fs-md);
  font-weight: 500;
  color: var(--ink);
  letter-spacing: -0.01em;
}

.add-btn {
  margin-top: auto;
  width: 100%;
  border-color: var(--line-strong);
}
.add-btn:hover:not(:disabled) {
  border-color: var(--ink);
}
.add-btn:disabled {
  opacity: 0.4;
}
</style>
