<template>
  <div class="admin-product-edit">
    <h1>{{ isEdit ? 'Edit Product' : 'Create Product' }}</h1>

    <div v-if="!authStore.isAuthenticated" class="message error">
      Please <RouterLink to="/login">sign in</RouterLink> to access this page.
    </div>
    <div v-else-if="!isAdmin" class="message error">Access denied.</div>
    <div v-else-if="loading" class="message">Loading…</div>
    <form v-else class="edit-form" @submit.prevent="onSubmit">
      <label class="field">
        <span>Name *</span>
        <input v-model="form.name" type="text" required :disabled="submitting" />
      </label>

      <label class="field">
        <span>Description</span>
        <textarea v-model="form.description" rows="4" :disabled="submitting" />
      </label>

      <label class="field">
        <span>Price *</span>
        <input
          v-model.number="form.price"
          type="number"
          min="0"
          step="0.01"
          required
          :disabled="submitting"
        />
      </label>

      <label class="field">
        <span>Category *</span>
        <select v-model.number="form.categoryId" required :disabled="submitting">
          <option disabled value="">Select a category</option>
          <option v-for="cat in categories" :key="cat.categoryId" :value="cat.categoryId">
            {{ cat.name }}
          </option>
        </select>
      </label>

      <label class="field">
        <span>Thumbnail URL</span>
        <input v-model="form.thumbnailUrl" type="url" :disabled="submitting" />
      </label>

      <label class="field">
        <span>Stock *</span>
        <input
          v-model.number="form.stock"
          type="number"
          min="0"
          step="1"
          required
          :disabled="submitting"
        />
      </label>

      <label class="field checkbox">
        <input v-model="form.isActive" type="checkbox" :disabled="submitting" />
        <span>Active</span>
      </label>

      <p v-if="error" class="error" role="alert">{{ error }}</p>

      <div class="actions">
        <button type="submit" class="btn btn-primary" :disabled="submitting">
          {{ submitting ? 'Saving…' : 'Save' }}
        </button>
        <button type="button" class="btn" :disabled="submitting" @click="onCancel">
          Cancel
        </button>
      </div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import {
  getProduct,
  createProduct,
  updateProduct,
  getCategories,
  type AdminCategory,
  type CreateProductRequest,
} from '../../api/admin'

const props = defineProps<{ id?: string }>()

const router = useRouter()
const authStore = useAuthStore()
const isAdmin = computed(() => authStore.roles.includes('Admin'))

const isEdit = computed(() => Boolean(props.id))
const productId = computed(() => (props.id ? Number(props.id) : null))

const categories = ref<AdminCategory[]>([])
const loading = ref(false)
const submitting = ref(false)
const error = ref<string | null>(null)

const form = ref<CreateProductRequest>({
  categoryId: 0,
  name: '',
  description: '',
  price: 0,
  thumbnailUrl: '',
  stock: 0,
  isActive: true,
})

onMounted(() => {
  if (!authStore.isAuthenticated) {
    router.push('/login')
    return
  }
  loadData()
})

watch(() => authStore.isAuthenticated, (authenticated) => {
  if (!authenticated) {
    router.push('/login')
  }
})

async function loadData(): Promise<void> {
  loading.value = true
  error.value = null
  try {
    const [cats, product] = await Promise.all([
      getCategories(),
      isEdit.value && productId.value !== null
        ? getProduct(productId.value)
        : Promise.resolve(null),
    ])
    categories.value = cats
    if (product) {
      form.value = {
        categoryId: product.categoryId,
        name: product.name,
        description: product.description ?? '',
        price: product.price,
        thumbnailUrl: product.thumbnailUrl ?? '',
        stock: product.stock,
        isActive: product.isActive,
      }
    }
  } catch (e) {
    const message =
      e && typeof e === 'object' && 'message' in e
        ? String((e as { message?: unknown }).message)
        : 'Failed to load product data'
    error.value = message || 'Failed to load product data'
  } finally {
    loading.value = false
  }
}

function onCancel(): void {
  router.push('/admin/products')
}

async function onSubmit(): Promise<void> {
  error.value = null
  submitting.value = true

  const body: CreateProductRequest = {
    categoryId: form.value.categoryId,
    name: form.value.name.trim(),
    description: form.value.description?.trim() || undefined,
    price: form.value.price,
    thumbnailUrl: form.value.thumbnailUrl?.trim() || undefined,
    stock: form.value.stock,
    isActive: form.value.isActive,
  }

  try {
    if (isEdit.value && productId.value !== null) {
      await updateProduct(productId.value, body)
    } else {
      await createProduct(body)
    }
    router.push('/admin/products')
  } catch (e) {
    const message =
      e && typeof e === 'object' && 'message' in e
        ? String((e as { message?: unknown }).message)
        : 'Failed to save product'
    error.value = message || 'Failed to save product'
  } finally {
    submitting.value = false
  }
}
</script>

<style scoped>
.admin-product-edit {
  max-width: 600px;
  margin: 0 auto;
  padding: 1.5rem;
}

.admin-product-edit h1 {
  margin: 0 0 1.5rem;
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

.edit-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  font-size: 0.85rem;
  color: #374151;
}

.field.checkbox {
  flex-direction: row;
  align-items: center;
  gap: 0.5rem;
}

.field input,
.field select,
.field textarea {
  padding: 0.5rem 0.6rem;
  border: 1px solid #d1d5db;
  border-radius: 0.3rem;
  font: inherit;
  font-size: 0.9rem;
}

.field input:disabled,
.field select:disabled,
.field textarea:disabled {
  background: #f3f4f6;
}

.error {
  margin: 0;
  padding: 0.5rem 0.6rem;
  background: rgba(220, 38, 38, 0.1);
  border: 1px solid rgba(220, 38, 38, 0.4);
  border-radius: 0.3rem;
  color: #dc2626;
  font-size: 0.85rem;
}

.actions {
  display: flex;
  gap: 0.75rem;
  margin-top: 0.5rem;
}

.actions .btn {
  width: auto;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.5rem 1rem;
  border: 1px solid #d1d5db;
  border-radius: 0.3rem;
  background: #fff;
  color: #374151;
  font: inherit;
  cursor: pointer;
}

.actions .btn:hover:not(:disabled) {
  background: #f3f4f6;
}

.actions .btn-primary {
  background: var(--accent, #aa3bff);
  border-color: var(--accent, #aa3bff);
  color: #fff;
}

.actions .btn-primary:hover:not(:disabled) {
  background: #9333ea;
}

.actions .btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
</style>
