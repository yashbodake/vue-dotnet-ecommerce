// Pinia cart store. Manages guest and authenticated cart state.

import { defineStore } from 'pinia'
import {
  getCart,
  getCartCount,
  addToCart,
  updateCartItem,
  removeCartItem,
  mergeCart,
  type Cart,
  type CartItem,
} from '../api/cart'

interface CartState {
  items: CartItem[]
  itemCount: number
  total: number
  loading: boolean
  error: string | null
}

export const useCartStore = defineStore('cart', {
  state: (): CartState => ({
    items: [],
    itemCount: 0,
    total: 0,
    loading: false,
    error: null,
  }),

  getters: {
    isEmpty: (state) => state.items.length === 0,
  },

  actions: {
    /** Replace store state from a Cart response. */
    privateSetCart(cart: Cart): void {
      this.items = cart.items ?? []
      this.itemCount = cart.itemCount ?? 0
      this.total = cart.total ?? 0
    },

    /** Load the full cart. */
    async fetchCart(isAuth: boolean): Promise<void> {
      this.loading = true
      this.error = null
      try {
        const cart = await getCart(isAuth)
        this.privateSetCart(cart)
      } catch (e) {
        this.error = errorMessage(e)
      } finally {
        this.loading = false
      }
    },

    /** Load only the count for the nav badge. */
    async fetchCount(isAuth: boolean): Promise<void> {
      try {
        const res = await getCartCount(isAuth)
        this.itemCount = res.count ?? 0
      } catch (e) {
        // Non-fatal: badge just won't update.
        this.error = errorMessage(e)
      }
    },

    /** Add an item to the cart and refresh state. */
    async addItem(
      productId: number,
      quantity: number,
      variantId?: number,
      isAuth = false,
    ): Promise<void> {
      this.loading = true
      this.error = null
      try {
        const cart = await addToCart(productId, quantity, variantId, isAuth)
        this.privateSetCart(cart)
      } catch (e) {
        this.error = errorMessage(e)
        throw e
      } finally {
        this.loading = false
      }
    },

    /** Update an item's quantity and refresh state. */
    async updateQuantity(cartItemId: number, quantity: number, isAuth = false): Promise<void> {
      this.loading = true
      this.error = null
      try {
        const cart = await updateCartItem(cartItemId, quantity, isAuth)
        this.privateSetCart(cart)
      } catch (e) {
        this.error = errorMessage(e)
        throw e
      } finally {
        this.loading = false
      }
    },

    /** Remove an item from the cart and refresh state. */
    async removeItem(cartItemId: number, isAuth = false): Promise<void> {
      this.loading = true
      this.error = null
      try {
        const cart = await removeCartItem(cartItemId, isAuth)
        this.privateSetCart(cart)
      } catch (e) {
        this.error = errorMessage(e)
        throw e
      } finally {
        this.loading = false
      }
    },

    /** Merge guest cart into the authenticated user cart, then reload. */
    async mergeOnLogin(): Promise<void> {
      this.loading = true
      this.error = null
      try {
        const cart = await mergeCart(true)
        this.privateSetCart(cart)
      } catch (e) {
        this.error = errorMessage(e)
        throw e
      } finally {
        this.loading = false
      }
    },

    /** Reset the cart to empty (e.g. on logout). */
    clear(): void {
      this.items = []
      this.itemCount = 0
      this.total = 0
      this.error = null
    },
  },
})

function errorMessage(e: unknown): string {
  if (e && typeof e === 'object' && 'message' in e) {
    return String((e as { message: unknown }).message)
  }
  return 'Cart request failed'
}
