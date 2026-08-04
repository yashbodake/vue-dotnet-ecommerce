import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      // Dev-only proxy: forward /api requests to the modern API.
      '/api': {
        target: 'http://127.0.0.1:5100',
        changeOrigin: true,
      },
    },
  },
})