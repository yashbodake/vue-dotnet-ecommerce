// Account API functions for the current authenticated user.
// Shapes mirror the API contracts in Ecommerce.Api/Contracts/AccountDtos.cs.

import { get } from './client'

export interface OrderSummary {
  orderId: number
  orderDate: string
  status: string
  totalAmount: number
  itemCount: number
}

/** GET /api/account/orders — the current user's order history. */
export function getMyOrders(): Promise<OrderSummary[]> {
  return get<OrderSummary[]>('/account/orders', true)
}
