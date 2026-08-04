// Cart API functions and DTO shapes.
// Shapes mirror the backend Contracts with camelCase over the wire.

import { get, post, put, del } from './client'

export interface CartItem {
  cartItemId: number
  productId: number
  productName: string
  productPrice: number
  variantId?: number
  variantName?: string
  variantSkuSuffix?: string
  priceAdjustment: number
  quantity: number
  lineTotal: number
  stock: number
}

export interface Cart {
  items: CartItem[]
  itemCount: number
  total: number
}

export interface CartCount {
  count: number
}

export interface AddCartItemRequest {
  productId: number
  variantId?: number
  quantity: number
}

export interface UpdateCartItemRequest {
  quantity: number
}

/** GET /api/cart — load the current cart (guest via cookie, auth via JWT). */
export function getCart(auth = false): Promise<Cart> {
  return get<Cart>('/cart', auth)
}

/** GET /api/cart/count — lightweight count for the badge. */
export function getCartCount(auth = false): Promise<CartCount> {
  return get<CartCount>('/cart/count', auth)
}

/** POST /api/cart/items — add a product/variant to the cart. */
export function addToCart(
  productId: number,
  quantity: number,
  variantId?: number,
  auth = false,
): Promise<Cart> {
  const body: AddCartItemRequest = { productId, quantity }
  if (variantId !== undefined) {
    body.variantId = variantId
  }
  return post<Cart>('/cart/items', body, auth)
}

/** PUT /api/cart/items/{cartItemId} — update item quantity. */
export function updateCartItem(
  cartItemId: number,
  quantity: number,
  auth = false,
): Promise<Cart> {
  return put<Cart>(`/cart/items/${encodeURIComponent(cartItemId)}`, { quantity }, auth)
}

/** DELETE /api/cart/items/{cartItemId} — remove an item. */
export function removeCartItem(cartItemId: number, auth = false): Promise<Cart> {
  return del<Cart>(`/cart/items/${encodeURIComponent(cartItemId)}`, auth)
}

/** POST /api/cart/merge — merge guest cart into the authenticated user cart. */
export function mergeCart(auth = true): Promise<Cart> {
  return post<Cart>('/cart/merge', undefined, auth)
}
