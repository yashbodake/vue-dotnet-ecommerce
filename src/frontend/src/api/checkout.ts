// Checkout API functions and DTO shapes.

import { get, post } from './client'

export interface ShippingOption {
  code: string
  name: string
  description: string
  estimatedDays: string
}

export interface OrderItem {
  productId: number
  productName: string
  variantName: string | null
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface OrderConfirmation {
  orderId: number
  orderDate: string
  status: string
  shippingAddress: string
  shippingMethod: string
  totalAmount: number
  items: OrderItem[]
}

export interface PlaceOrderRequest {
  shippingAddress: string
  shippingMethod: string
  cardName: string
  cardNumber: string
  cardExpiry: string
  cardCvv: string
}

/** GET /api/checkout/shipping-options — load available shipping options. */
export function getShippingOptions(): Promise<ShippingOption[]> {
  return get<ShippingOption[]>('/checkout/shipping-options')
}

/** POST /api/checkout/place-order — submit an authenticated order. */
export function placeOrder(request: PlaceOrderRequest): Promise<OrderConfirmation> {
  return post<OrderConfirmation>('/checkout/place-order', request, true)
}

/** GET /api/checkout/orders/{orderId} — load an authenticated order confirmation. */
export function getOrder(orderId: number): Promise<OrderConfirmation> {
  return get<OrderConfirmation>(`/checkout/orders/${encodeURIComponent(orderId)}`, true)
}
