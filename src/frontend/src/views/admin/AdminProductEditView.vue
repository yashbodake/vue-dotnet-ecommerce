<template>
  <div class="admin container">
    <nav class="breadcrumb" aria-label="Breadcrumb">
      <RouterLink to="/admin/products" class="crumb">Products</RouterLink>
      <span class="sep" aria-hidden="true">/</span>
      <span class="crumb current">{{ isEdit ? 'Edit' : 'New' }}</span>
    </nav>

    <header class="page-head">
      <span class="eyebrow">Admin</span>
      <h1>{{ isEdit ? 'Edit product' : 'New product' }}</h1>
    </header>

    <div v-if="!authStore.isAuthenticated" class="card message">
      Please <RouterLink to="/login">sign in</RouterLink> to access this page.
    </div>
    <div v-else-if="!isAdmin" class="card message danger">Access denied.</div>
    <div v-else-if="loading" class="card message">Loading…</div>

    <form v-else class="card edit-form" @submit.prevent="onSubmit">
      <div class="field">
        <label for="name">Name <span class="req">*</span></label>
        <input id="name" class="input" v-model="form.name" type="text" required :disabled="submitting" />
      </div>

      <div class="field">
        <label for="description">Description</label>
        <textarea
          id="description"
          class="textarea"
          v-model="form.description"
          rows="4"
          :disabled="submitting"
        ></textarea>
      </div>

      <div class="field-row">
        <div class="field">
          <label for="price">Price <span class="req">*</span></label>
          <input
            id="price"
            class="input tabular"
            v-model.number="form.price"
            type="number"
            min="0"
            step="0.01"
            required
            :disabled="submitting"
          />
        </div>
        <div class="field">
          <label for="stock">Stock <span class="req">*</span></label>
          <input
            id="stock"
            class="input tabular"
            v-model.number="form.stock"
            type="number"
            min="0"
            step="1"
            required
            :disabled="submitting"
          />
        </div>
      </div>

      <div class="field">
        <label for="category">Category <span class="req">*</span></label>
        <select id="category" class="select" v-model.number="form.categoryId" required :disabled="submitting">
          <option disabled value="">Select a category</option>
          <option v-for="cat in categories" :key="cat.categoryId" :value="cat.categoryId">
            {{ cat.name }}
          </option>
        </select>
      </div>

      <div class="field">
        <label for="thumb">Thumbnail URL</label>
        <input id="thumb" class="input" v-model="form.thumbnailUrl" type="url" :disabled="submitting" />
      </div>

      <label class="check toggle">
        <input v-model="form.isActive" type="checkbox" :disabled="submitting" />
        Active (visible in catalogue)
      </label>

      <p v-if="error" class="pill pill-danger error" role="alert">{{ error }}</p>

      <div class="actions">
        <button type="submit" class="btn btn-primary" :disabled="submitting">
          {{ submitting ? 'Saving…' : 'Save product' }}
        </button>
        <button type="button" class="btn btn-ghost" :disabled="submitting" @click="onCancel">
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

watch(
  () => authStore.isAuthenticated,
  (authenticated) => {
    if (!authenticated) {
      router.push('/login')
    }
  },
)

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
.admin {
  max-width: 42rem;
  padding-block: var(--sp-7) var(--sp-9);
}

.breadcrumb {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  margin-bottom: var(--sp-5);
  font-size: var(--fs-sm);
}
.crumb {
  color: var(--muted);
  text-decoration: none;
}
.crumb:hover {
  color: var(--ink);
}
.crumb.current {
  color: var(--ink);
}
.sep {
  color: var(--line-strong);
}

.page-head {
  margin-bottom: var(--sp-6);
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

.edit-form {
  padding: var(--sp-6);
  display: flex;
  flex-direction: column;
  gap: var(--sp-5);
}

.req {
  color: var(--danger);
}

.toggle {
  font-size: var(--fs-sm);
}

.error {
  padding: 0.55rem 0.85rem;
}

.actions {
  display: flex;
  gap: var(--sp-3);
  margin-top: var(--sp-2);
}
</style>
