// Pinia catalog store. Holds filter state and the current paged product list.
// Filter changes call fetchProducts() to refetch from the API.

import { defineStore } from 'pinia'
import {
  getProducts,
  getCategories,
  type Product,
  type Category,
  type SortBy,
} from '../api/catalog'

interface CatalogState {
  products: Product[]
  categories: Category[]
  totalCount: number
  page: number
  pageSize: number
  search: string
  sortBy: SortBy
  categoryIds: number[]
  minPrice: number | null
  maxPrice: number | null
  inStockOnly: boolean
  loading: boolean
  error: string | null
}

export const useCatalogStore = defineStore('catalog', {
  state: (): CatalogState => ({
    products: [],
    categories: [],
    totalCount: 0,
    page: 1,
    pageSize: 12,
    search: '',
    sortBy: 'newest',
    categoryIds: [],
    minPrice: null,
    maxPrice: null,
    inStockOnly: false,
    loading: false,
    error: null,
  }),
  getters: {
    totalPages: (state) => Math.max(1, Math.ceil(state.totalCount / state.pageSize)),
  },
  actions: {
    /** Fetch the product list using the current filter state. */
    async fetchProducts(): Promise<void> {
      this.loading = true
      this.error = null
      try {
        const res = await getProducts({
          page: this.page,
          pageSize: this.pageSize,
          search: this.search || undefined,
          sortBy: this.sortBy,
          categoryIds: this.categoryIds,
          minPrice: this.minPrice,
          maxPrice: this.maxPrice,
          inStockOnly: this.inStockOnly,
        })
        this.products = res.items
        this.totalCount = res.totalCount
        // Keep page in valid range if API returned fewer pages.
        if (this.page > res.totalPages && res.totalPages > 0) {
          this.page = res.totalPages
        }
      } catch (e) {
        this.error = errorMessage(e)
        this.products = []
      } finally {
        this.loading = false
      }
    },

    /** Fetch the category list for filter controls. */
    async fetchCategories(): Promise<void> {
      try {
        this.categories = await getCategories()
      } catch (e) {
        // Non-fatal: filters just won't show categories.
        this.categories = []
      }
    },

    setPage(page: number): void {
      const target = Math.min(Math.max(1, page), this.totalPages)
      if (target !== this.page) {
        this.page = target
        this.fetchProducts()
      }
    },

    setSearch(value: string): void {
      this.search = value
      this.page = 1
      this.fetchProducts()
    },

    setSortBy(value: SortBy): void {
      this.sortBy = value
      this.page = 1
      this.fetchProducts()
    },

    toggleCategory(categoryId: number): void {
      const idx = this.categoryIds.indexOf(categoryId)
      if (idx >= 0) {
        this.categoryIds.splice(idx, 1)
      } else {
        this.categoryIds.push(categoryId)
      }
      this.page = 1
      this.fetchProducts()
    },

    setPriceRange(min: number | null, max: number | null): void {
      this.minPrice = min
      this.maxPrice = max
      this.page = 1
      this.fetchProducts()
    },

    toggleInStockOnly(): void {
      this.inStockOnly = !this.inStockOnly
      this.page = 1
      this.fetchProducts()
    },

    resetFilters(): void {
      this.search = ''
      this.sortBy = 'newest'
      this.categoryIds = []
      this.minPrice = null
      this.maxPrice = null
      this.inStockOnly = false
      this.page = 1
      this.fetchProducts()
    },
  },
})

function errorMessage(e: unknown): string {
  if (e && typeof e === 'object' && 'message' in e) {
    return String((e as { message: unknown }).message)
  }
  return 'Failed to load products'
}