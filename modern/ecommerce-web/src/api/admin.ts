// Admin API functions. All endpoints require JWT + Admin role.

import { get, post, put, del } from './client'

export interface AdminProduct {
  productId: number
  categoryId: number
  categoryName: string
  name: string
  description?: string
  price: number
  thumbnailUrl?: string
  stock: number
  isActive: boolean
}

export interface CreateProductRequest {
  categoryId: number
  name: string
  description?: string
  price: number
  thumbnailUrl?: string
  stock: number
  isActive: boolean
}

export type UpdateProductRequest = CreateProductRequest

export interface AdminCategory {
  categoryId: number
  name: string
}

export interface AdminOrder {
  orderId: number
  userId: number
  orderDate: string
  status: string
  shippingAddress: string
  totalAmount: number
  itemCount: number
}

export interface UpdateOrderStatusRequest {
  status: string
}

/** GET /api/admin/products */
export function getProducts(): Promise<AdminProduct[]> {
  return get<AdminProduct[]>('/admin/products', true)
}

/** GET /api/admin/products/{id} */
export function getProduct(id: number): Promise<AdminProduct> {
  return get<AdminProduct>(`/admin/products/${id}`, true)
}

/** POST /api/admin/products */
export function createProduct(req: CreateProductRequest): Promise<AdminProduct> {
  return post<AdminProduct>('/admin/products', req, true)
}

/** PUT /api/admin/products/{id} */
export function updateProduct(id: number, req: UpdateProductRequest): Promise<AdminProduct> {
  return put<AdminProduct>(`/admin/products/${id}`, req, true)
}

/** DELETE /api/admin/products/{id} */
export function deleteProduct(id: number): Promise<void> {
  return del<void>(`/admin/products/${id}`, true)
}

/** GET /api/admin/categories */
export function getCategories(): Promise<AdminCategory[]> {
  return get<AdminCategory[]>('/admin/categories', true)
}

/** GET /api/admin/orders */
export function getOrders(status?: string): Promise<AdminOrder[]> {
  const query = status ? `?status=${encodeURIComponent(status)}` : ''
  return get<AdminOrder[]>(`/admin/orders${query}`, true)
}

/** PUT /api/admin/orders/{id}/status */
export function updateOrderStatus(id: number, status: string): Promise<AdminOrder> {
  const body: UpdateOrderStatusRequest = { status }
  return put<AdminOrder>(`/admin/orders/${id}/status`, body, true)
}
