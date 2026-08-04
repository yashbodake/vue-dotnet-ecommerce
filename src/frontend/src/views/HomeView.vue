<template>
  <div class="home">
    <header class="header">
      <h1>Shop</h1>
      <p class="subtitle">{{ store.totalCount }} products available</p>
    </header>

    <section class="filters" aria-label="Product filters">
      <div class="row">
        <input
          class="search"
          type="search"
          placeholder="Search products..."
          :value="store.search"
          @input="onSearchInput"
        />

        <select
          class="sort"
          :value="store.sortBy"
          @change="onSortChange"
          aria-label="Sort by"
        >
          <option value="newest">Newest</option>
          <option value="name">Name</option>
          <option value="price_asc">Price: Low to High</option>
          <option value="price_desc">Price: High to Low</option>
        </select>

        <label class="checkbox">
          <input
            type="checkbox"
            :checked="store.inStockOnly"
            @change="store.toggleInStockOnly()"
          />
          In stock only
        </label>

        <button class="reset" type="button" @click="store.resetFilters()">Reset</button>
      </div>

      <div class="row">
        <label class="price">
          Min
          <input
            type="number"
            min="0"
            step="0.01"
            :value="store.minPrice ?? ''"
            @change="onMinChange"
            placeholder="0"
          />
        </label>
        <label class="price">
          Max
          <input
            type="number"
            min="0"
            step="0.01"
            :value="store.maxPrice ?? ''"
            @change="onMaxChange"
            placeholder="any"
          />
        </label>
      </div>

      <fieldset v-if="store.categories.length" class="categories">
        <legend>Categories</legend>
        <label v-for="cat in store.categories" :key="cat.categoryId" class="checkbox">
          <input
            type="checkbox"
            :value="cat.categoryId"
            :checked="store.categoryIds.includes(cat.categoryId)"
            @change="store.toggleCategory(cat.categoryId)"
          />
          {{ cat.name }}
        </label>
      </fieldset>
    </section>

    <section class="status" v-if="store.loading" aria-live="polite">
      Loading products...
    </section>
    <section class="status error" v-else-if="store.error" aria-live="polite">
      {{ store.error }}
    </section>

    <section v-else class="grid" aria-label="Product grid">
      <ProductCard v-for="p in store.products" :key="p.productId" :product="p" />
      <p v-if="store.products.length === 0" class="empty">No products match your filters.</p>
    </section>

    <nav class="pagination" v-if="!store.loading && store.totalCount > 0" aria-label="Pagination">
      <button type="button" :disabled="store.page <= 1" @click="store.setPage(store.page - 1)">
        Previous
      </button>
      <span>Page {{ store.page }} of {{ store.totalPages }}</span>
      <button
        type="button"
        :disabled="store.page >= store.totalPages"
        @click="store.setPage(store.page + 1)"
      >
        Next
      </button>
    </nav>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useCatalogStore } from '../stores/catalog'
import ProductCard from '../components/ProductCard.vue'
import type { SortBy } from '../api/catalog'

const store = useCatalogStore()

onMounted(() => {
  store.fetchCategories()
  store.fetchProducts()
})

let searchTimer: ReturnType<typeof setTimeout> | null = null
function onSearchInput(e: Event): void {
  const value = (e.target as HTMLInputElement).value
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => store.setSearch(value), 300)
}

function onSortChange(e: Event): void {
  store.setSortBy((e.target as HTMLSelectElement).value as SortBy)
}

function onMinChange(e: Event): void {
  const raw = (e.target as HTMLInputElement).value
  store.setPriceRange(raw === '' ? null : Number(raw), store.maxPrice)
}

function onMaxChange(e: Event): void {
  const raw = (e.target as HTMLInputElement).value
  store.setPriceRange(store.minPrice, raw === '' ? null : Number(raw))
}
</script>

<style scoped>
.home {
  max-width: 1200px;
  margin: 0 auto;
  padding: 1.5rem;
}
.header {
  margin-bottom: 1rem;
}
.header h1 {
  margin: 0;
  font-size: 1.9rem;
  color: var(--text-h);
}
.subtitle {
  margin: 0.25rem 0 0;
  color: var(--text-muted);
  font-size: 0.9rem;
}

.filters {
  background: var(--bg-elevated);
  border: 1px solid var(--border);
  border-radius: var(--radius);
  padding: 1rem;
  margin-bottom: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  box-shadow: var(--shadow);
}
.row {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  align-items: center;
}
.search {
  flex: 1 1 240px;
  padding: 0.5rem 0.75rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: var(--bg);
  font-size: 0.9rem;
}
.sort {
  padding: 0.5rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: var(--bg);
  font-size: 0.9rem;
}
.checkbox {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
  color: var(--text);
}
.reset {
  padding: 0.45rem 0.75rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: var(--bg-elevated);
  cursor: pointer;
  font-size: 0.85rem;
  color: var(--text-h);
}
.reset:hover {
  border-color: var(--accent);
  color: var(--accent);
}
.price {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
  color: var(--text);
}
.price input {
  width: 90px;
  padding: 0.4rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: var(--bg);
  font-size: 0.85rem;
}
.categories {
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  padding: 0.5rem 0.75rem;
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem 1rem;
  align-items: center;
  background: var(--bg-soft);
}
.categories legend {
  font-size: 0.8rem;
  color: var(--text-muted);
  padding: 0 0.25rem;
}

.status {
  padding: 2rem;
  text-align: center;
  color: var(--text-muted);
}
.status.error {
  color: var(--danger);
}

.grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 1rem;
}
.empty {
  grid-column: 1 / -1;
  text-align: center;
  color: var(--text-muted);
  padding: 2rem;
}

.pagination {
  margin-top: 1.5rem;
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
}
.pagination button {
  padding: 0.5rem 1rem;
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  background: var(--bg-elevated);
  color: var(--text-h);
  cursor: pointer;
}
.pagination button:hover:not(:disabled) {
  border-color: var(--accent);
  color: var(--accent);
}
.pagination button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.pagination span {
  font-size: 0.9rem;
  color: var(--text);
}
</style>