// Catalog API functions. Shapes mirror the API contracts in
// modern/Ecommerce.Api/Contracts (camelCase over the wire).

import { get } from './client'

export interface Product {
  productId: number
  categoryId: number
  name: string
  description: string
  price: number
  thumbnailUrl: string | null
  stock: number
  isActive: boolean
  createdDate: string
}

export interface Category {
  categoryId: number
  name: string
  parentCategoryId: number | null
  displayOrder: number
}

export interface ProductImage {
  productImageId: number
  productId: number
  url: string
  displayOrder: number
}

export interface ProductVariant {
  productVariantId: number
  productId: number
  name: string
  skuSuffix: string | null
  stock: number
  priceAdjustment: number
}

export interface ProductDetail {
  product: Product
  images: ProductImage[]
  variants: ProductVariant[]
  selectedVariantId: number | null
}

export interface PagedResult {
  items: Product[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export type SortBy = 'name' | 'price_asc' | 'price_desc' | 'newest'

export interface ProductQueryParams {
  page?: number
  pageSize?: number
  search?: string
  sortBy?: SortBy
  categoryIds?: number[]
  minPrice?: number | null
  maxPrice?: number | null
  inStockOnly?: boolean
}

function buildQuery(params: ProductQueryParams): string {
  const parts: string[] = []
  if (params.page != null) parts.push(`page=${encodeURIComponent(params.page)}`)
  if (params.pageSize != null) parts.push(`pageSize=${encodeURIComponent(params.pageSize)}`)
  if (params.search) parts.push(`search=${encodeURIComponent(params.search)}`)
  if (params.sortBy) parts.push(`sortBy=${encodeURIComponent(params.sortBy)}`)
  if (params.categoryIds && params.categoryIds.length > 0) {
    parts.push(`categoryIds=${params.categoryIds.map((c) => encodeURIComponent(c)).join(',')}`)
  }
  if (params.minPrice != null) parts.push(`minPrice=${encodeURIComponent(params.minPrice)}`)
  if (params.maxPrice != null) parts.push(`maxPrice=${encodeURIComponent(params.maxPrice)}`)
  if (params.inStockOnly) parts.push(`inStockOnly=true`)
  return parts.length > 0 ? `?${parts.join('&')}` : ''
}

/** GET /api/products — paged product list with optional filters. */
export function getProducts(params: ProductQueryParams = {}): Promise<PagedResult> {
  return get<PagedResult>(`/products${buildQuery(params)}`)
}

/** GET /api/categories — full category list. */
export function getCategories(): Promise<Category[]> {
  return get<Category[]>('/categories')
}

/** GET /api/products/{id} — single product detail. */
export function getProduct(id: number): Promise<ProductDetail> {
  return get<ProductDetail>(`/products/${encodeURIComponent(id)}`)
}