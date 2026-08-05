<template>
  <div class="home">
    <header class="page-header container">
      <span class="eyebrow">Catalogue</span>
      <h1>A considered edit of well-made objects.</h1>
      <p class="lede">{{ store.totalCount }} pieces, sourced and sorted for you.</p>
    </header>

    <section class="filters container" aria-label="Product filters">
      <div class="filters-top">
        <div class="search-wrap">
          <svg class="search-icon" viewBox="0 0 24 24" width="18" height="18" aria-hidden="true">
            <circle cx="11" cy="11" r="7" fill="none" stroke="currentColor" stroke-width="1.6" />
            <line x1="16.5" y1="16.5" x2="21" y2="21" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" />
          </svg>
          <input
            class="input search"
            type="search"
            placeholder="Search the catalogue…"
            :value="store.search"
            @input="onSearchInput"
            aria-label="Search products"
          />
        </div>
        <select class="select sort" :value="store.sortBy" @change="onSortChange" aria-label="Sort by">
          <option value="newest">Newest</option>
          <option value="name">Name</option>
          <option value="price_asc">Price · low to high</option>
          <option value="price_desc">Price · high to low</option>
        </select>
        <label class="check stock-toggle">
          <input type="checkbox" :checked="store.inStockOnly" @change="store.toggleInStockOnly()" />
          In stock only
        </label>
      </div>

      <div class="filters-row">
        <div class="price-group">
          <label class="price">
            <span class="price-label">Min</span>
            <input
              class="input"
              type="number"
              min="0"
              step="0.01"
              :value="store.minPrice ?? ''"
              @change="onMinChange"
              placeholder="0"
            />
          </label>
          <span class="price-sep">—</span>
          <label class="price">
            <span class="price-label">Max</span>
            <input
              class="input"
              type="number"
              min="0"
              step="0.01"
              :value="store.maxPrice ?? ''"
              @change="onMaxChange"
              placeholder="any"
            />
          </label>
        </div>
        <button
          v-if="hasActiveFilters"
          class="btn-link reset"
          type="button"
          @click="store.resetFilters()"
        >
          Clear all
        </button>
      </div>

      <div v-if="store.categories.length" class="categories" role="group" aria-label="Categories">
        <button
          v-for="cat in store.categories"
          :key="cat.categoryId"
          type="button"
          class="chip"
          :aria-pressed="store.categoryIds.includes(cat.categoryId)"
          :class="{ 'is-active': store.categoryIds.includes(cat.categoryId) }"
          @click="store.toggleCategory(cat.categoryId)"
        >
          {{ cat.name }}
        </button>
      </div>
    </section>

    <section v-if="store.loading" class="grid container" aria-label="Loading products" aria-live="polite">
      <div v-for="n in 12" :key="n" class="skeleton-card" aria-hidden="true">
        <div class="skeleton thumb"></div>
        <div class="skeleton line w-70"></div>
        <div class="skeleton line w-40"></div>
      </div>
    </section>

    <section v-else-if="store.error" class="container status error" aria-live="polite">
      <p>{{ store.error }}</p>
    </section>

    <section v-else class="grid container" aria-label="Product grid">
      <ProductCard v-for="p in store.products" :key="p.productId" :product="p" />
      <p v-if="store.products.length === 0" class="empty">
        No pieces match these filters. Try clearing them.
      </p>
    </section>

    <nav
      v-if="!store.loading && store.totalCount > 0"
      class="pagination container"
      aria-label="Pagination"
    >
      <button
        type="button"
        class="btn btn-ghost"
        :disabled="store.page <= 1"
        @click="store.setPage(store.page - 1)"
      >
        ← Previous
      </button>
      <span class="page-of tabular">Page {{ store.page }} of {{ store.totalPages }}</span>
      <button
        type="button"
        class="btn btn-ghost"
        :disabled="store.page >= store.totalPages"
        @click="store.setPage(store.page + 1)"
      >
        Next →
      </button>
    </nav>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useCatalogStore } from '../stores/catalog'
import ProductCard from '../components/ProductCard.vue'
import type { SortBy } from '../api/catalog'

const store = useCatalogStore()

const hasActiveFilters = computed(
  () =>
    !!store.search ||
    !!store.minPrice ||
    !!store.maxPrice ||
    store.inStockOnly ||
    store.categoryIds.length > 0 ||
    store.sortBy !== 'newest',
)

onMounted(() => {
  void Promise.all([store.fetchCategories(), store.fetchProducts()])
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
  padding-block: var(--sp-8) var(--sp-9);
}

/* Page header ---------------------------------------------------------- */
.page-header {
  margin-bottom: var(--sp-7);
}
.page-header h1 {
  margin-top: var(--sp-3);
  max-width: 18ch;
}
.lede {
  margin-top: var(--sp-3);
  color: var(--muted);
  font-size: var(--fs-md);
}

/* Filters -------------------------------------------------------------- */
.filters {
  display: flex;
  flex-direction: column;
  gap: var(--sp-4);
  padding: var(--sp-5);
  background: var(--surface);
  border: 1px solid var(--line);
  border-radius: var(--r-md);
  margin-bottom: var(--sp-7);
}
.filters-top {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--sp-3);
}
.search-wrap {
  position: relative;
  flex: 1 1 280px;
}
.search-icon {
  position: absolute;
  left: 0.85rem;
  top: 50%;
  transform: translateY(-50%);
  color: var(--muted);
  pointer-events: none;
}
.search {
  padding-left: 2.5rem;
}
.sort {
  width: auto;
  min-width: 12rem;
}
.stock-toggle {
  margin-left: auto;
}

.filters-row {
  display: flex;
  align-items: flex-end;
  gap: var(--sp-5);
  flex-wrap: wrap;
}
.price-group {
  display: flex;
  align-items: flex-end;
  gap: var(--sp-3);
}
.price {
  display: flex;
  flex-direction: column;
  gap: var(--sp-1);
}
.price input {
  width: 7rem;
}
.price-label {
  font-size: var(--fs-xs);
  font-weight: 500;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--muted);
}
.price-sep {
  color: var(--muted);
  padding-bottom: 0.7rem;
}
.reset {
  font-size: var(--fs-sm);
  margin-left: auto;
}

.categories {
  display: flex;
  flex-wrap: wrap;
  gap: var(--sp-2);
  padding-top: var(--sp-4);
  border-top: 1px solid var(--line);
}

/* Grid + skeleton ------------------------------------------------------ */
.grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(230px, 1fr));
  gap: var(--sp-6) var(--sp-5);
}
.empty {
  grid-column: 1 / -1;
  text-align: center;
  color: var(--muted);
  padding: var(--sp-9) var(--sp-4);
  font-size: var(--fs-md);
}

.skeleton-card {
  display: flex;
  flex-direction: column;
  gap: var(--sp-3);
}
.skeleton-card .thumb {
  aspect-ratio: 1 / 1;
  border-radius: 0;
}
.line {
  height: 0.85rem;
}
.w-70 {
  width: 70%;
}
.w-40 {
  width: 40%;
}

/* Pagination ----------------------------------------------------------- */
.pagination {
  margin-top: var(--sp-8);
  display: flex;
  justify-content: center;
  align-items: center;
  gap: var(--sp-5);
}
.page-of {
  font-size: var(--fs-sm);
  color: var(--muted);
  letter-spacing: 0.04em;
}

.status {
  padding: var(--sp-9) var(--sp-4);
  text-align: center;
}
.status.error {
  color: var(--danger);
}

@media (max-width: 640px) {
  .stock-toggle {
    margin-left: 0;
  }
  .page-header h1 {
    font-size: var(--fs-xl);
  }
}
</style>
