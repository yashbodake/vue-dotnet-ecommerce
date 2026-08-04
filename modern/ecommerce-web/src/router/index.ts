import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/LoginView.vue'),
    },
    {
      path: '/products/:id',
      name: 'product-detail',
      component: () => import('../views/ProductDetailView.vue'),
      props: true,
    },
    {
      path: '/cart',
      name: 'cart',
      component: () => import('../views/CartView.vue'),
    },
    {
      path: '/checkout',
      name: 'checkout',
      component: () => import('../views/CheckoutView.vue'),
    },
    {
      path: '/checkout/confirmation/:orderId',
      name: 'order-confirmation',
      component: () => import('../views/OrderConfirmationView.vue'),
      props: true,
    },
    {
      path: '/admin/products',
      name: 'admin-products',
      component: () => import('../views/admin/AdminProductsView.vue'),
    },
    {
      path: '/admin/products/create',
      name: 'admin-product-create',
      component: () => import('../views/admin/AdminProductEditView.vue'),
    },
    {
      path: '/admin/products/:id/edit',
      name: 'admin-product-edit',
      component: () => import('../views/admin/AdminProductEditView.vue'),
      props: true,
    },
    {
      path: '/admin/orders',
      name: 'admin-orders',
      component: () => import('../views/admin/AdminOrdersView.vue'),
    },
    // Legacy ASP.NET MVC redirects
    { path: '/Cart', redirect: '/cart' },
    { path: '/Checkout', redirect: '/checkout' },
    { path: '/Account/Login', redirect: '/login' },
    { path: '/Account/Register', redirect: '/register' },
    { path: '/Account/Orders', redirect: '/orders' },
    { path: '/Admin/Products', redirect: '/admin/products' },
    { path: '/Admin/Orders', redirect: '/admin/orders' },
    { path: '/Product/:id', redirect: to => `/products/${to.params.id}` },
    { path: '/Product/Detail/:id', redirect: to => `/products/${to.params.id}` },
    { path: '/Home/Index', redirect: '/' },
  ],
})

export default router