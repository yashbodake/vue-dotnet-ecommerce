<template>
  <div class="admin-products">
    <header class="page-header">
      <h1>Admin: Products</h1>
      <RouterLink to="/admin/products/create" class="btn btn-primary">
        Create New Product
      </RouterLink>
    </header>

    <div v-if="!authStore.isAuthenticated" class="message error">
      Please <RouterLink to="/login">sign in</RouterLink> to access this page.
    </div>
    <div v-else-if="!isAdmin" class="message error">Access denied.</div>
    <div v-else-if="loading" class="message">Loading products…</div>
    <div v-else-if="error" class="message error" role="alert">{{ error }}</div>
    <template v-else>
      <table class="data-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Category</th>
            <th class="numeric">Price</th>
            <th class="numeric">Stock</th>
            <th>Active</th>
            <th class="actions">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="product in products" :key="product.productId">
            <td>{{ product.name }}</td>
            <td>{{ product.categoryName }}</td>
            <td class="numeric">{{ formatPrice(product.price) }}</td>
            <td class="numeric">{{ product.stock }}</td>
            <td>{{ product.isActive ? 'Yes' : 'No' }}</td>
            <td class="actions">
              <RouterLink
                :to="`/admin/products/${product.productId}/edit`"
                class="btn btn-small"
              >
                Edit
              </RouterLink>
              <button
                type="button"
                class="btn btn-small btn-danger"
                :disabled="deletingId === product.productId"
                @click="onDelete(product)"
              >
                {{ deletingId === product.productId ? 'Deleting…' : 'Delete' }}
              </button>
            </td>
          </tr>
          <tr v-if="products.length === 0">
            <td colspan="6" class="empty">No products found.</td>
          </tr>
        </tbody>
      </table>
    </template>
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

watch(() => authStore.isAuthenticated, (authenticated) => {
  if (!authenticated) {
    router.push('/login')
  }
})

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
.admin-products {
  max-width: 1200px;
  margin: 0 auto;
  padding: 1.5rem;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1.5rem;
}

.page-header h1 {
  margin: 0;
  font-size: 1.5rem;
  color: var(--text-h, #08060d);
}

.message {
  padding: 1rem;
  border-radius: 0.3rem;
  background: #f9fafb;
  color: #374151;
}

.message.error {
  background: rgba(220, 38, 38, 0.08);
  color: #dc2626;
}

.data-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
}

.data-table th,
.data-table td {
  padding: 0.6rem 0.75rem;
  border-bottom: 1px solid #e5e7eb;
  text-align: left;
}

.data-table th {
  font-weight: 600;
  color: #374151;
  background: #f9fafb;
}

.numeric {
  text-align: right;
}

.actions {
  white-space: nowrap;
  width: 1%;
}

.actions .btn {
  margin-right: 0.5rem;
}

.empty {
  text-align: center;
  color: #6b7280;
  padding: 2rem;
}

.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.45rem 0.75rem;
  border: 1px solid #d1d5db;
  border-radius: 0.3rem;
  background: #fff;
  color: #374151;
  font: inherit;
  font-size: 0.85rem;
  text-decoration: none;
  cursor: pointer;
}

.btn:hover {
  background: #f3f4f6;
}

.btn-primary {
  background: var(--accent, var(--accent));
  border-color: var(--accent, var(--accent));
  color: #fff;
}

.btn-primary:hover {
  background: var(--accent-hover);
}

.btn-small {
  padding: 0.25rem 0.5rem;
  font-size: 0.8rem;
}

.btn-danger {
  color: #dc2626;
  border-color: #fca5a5;
  background: #fef2f2;
}

.btn-danger:hover {
  background: #fee2e2;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
