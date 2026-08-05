<template>
  <div class="admin container">
    <header class="page-head">
      <div>
        <span class="eyebrow">Admin</span>
        <h1>Products</h1>
      </div>
      <RouterLink to="/admin/products/create" class="btn btn-primary">New product</RouterLink>
    </header>

    <div v-if="!authStore.isAuthenticated" class="card message">
      Please <RouterLink to="/login">sign in</RouterLink> to access this page.
    </div>
    <div v-else-if="!isAdmin" class="card message danger">Access denied.</div>
    <div v-else-if="loading" class="card message">Loading products…</div>
    <div v-else-if="error" class="card message danger" role="alert">{{ error }}</div>

    <div v-else class="card table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Category</th>
            <th class="numeric">Price</th>
            <th class="numeric">Stock</th>
            <th>Active</th>
            <th class="col-actions">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="product in products" :key="product.productId">
            <td class="cell-name">{{ product.name }}</td>
            <td class="muted">{{ product.categoryName }}</td>
            <td class="numeric tabular">{{ formatPrice(product.price) }}</td>
            <td class="numeric tabular">{{ product.stock }}</td>
            <td>
              <span class="pill" :class="product.isActive ? 'pill-success' : 'pill-muted'">
                {{ product.isActive ? 'Active' : 'Hidden' }}
              </span>
            </td>
            <td class="col-actions">
              <RouterLink :to="`/admin/products/${product.productId}/edit`" class="btn btn-sm">
                Edit
              </RouterLink>
              <button
                type="button"
                class="btn btn-sm btn-danger"
                :disabled="deletingId === product.productId"
                @click="onDelete(product)"
              >
                {{ deletingId === product.productId ? 'Deleting…' : 'Delete' }}
              </button>
            </td>
          </tr>
          <tr v-if="products.length === 0">
            <td colspan="6" class="empty muted">No products found.</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getProducts, deleteProduct, type AdminProduct } from '../../api/admin'

const router = useRouter()
const authStore = useAuthStore()
const isAdmin = computed(() => authStore.roles.includes('Admin'))

const products = ref<AdminProduct[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const deletingId = ref<number | null>(null)

onMounted(() => {
  if (!authStore.isAuthenticated) {
    router.push('/login')
    return
  }
  loadProducts()
})

watch(
  () => authStore.isAuthenticated,
  (authenticated) => {
    if (!authenticated) {
      router.push('/login')
    }
  },
)

async function loadProducts(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    products.value = await getProducts()
  } catch (e) {
    const message =
      e && typeof e === 'object' && 'message' in e
        ? String((e as { message?: unknown }).message)
        : 'Failed to load products'
    error.value = message || 'Failed to load products'
  } finally {
    loading.value = false
  }
}

function formatPrice(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

async function onDelete(product: AdminProduct): Promise<void> {
  if (!confirm(`Delete “${product.name}”? This will deactivate the product.`)) {
    return
  }
  deletingId.value = product.productId
  try {
    await deleteProduct(product.productId)
    await loadProducts()
  } catch (e) {
    const message =
      e && typeof e === 'object' && 'message' in e
        ? String((e as { message?: unknown }).message)
        : 'Failed to delete product'
    alert(message || 'Failed to delete product')
  } finally {
    deletingId.value = null
  }
}
</script>

<style scoped>
.admin {
  max-width: var(--maxw);
  padding-block: var(--sp-8) var(--sp-9);
}

.page-head {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: var(--sp-4);
  margin-bottom: var(--sp-6);
  flex-wrap: wrap;
}
.page-head h1 {
  margin-top: var(--sp-2);
  font-size: var(--fs-xl);
}

.message {
  padding: var(--sp-5);
  font-size: var(--fs-sm);
}
.message.danger {
  color: var(--danger);
  background: var(--danger-soft);
  border-color: var(--danger-border);
}

.table-wrap {
  overflow: hidden;
  padding: 0;
}
.cell-name {
  font-weight: 500;
  color: var(--ink);
}
.col-actions {
  white-space: nowrap;
  width: 1%;
}
.col-actions .btn + .btn {
  margin-left: var(--sp-2);
}
.numeric {
  text-align: right;
}
.empty {
  text-align: center;
  padding: var(--sp-7);
}
</style>
