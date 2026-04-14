import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

const proxyTarget = process.env.VITE_PROXY_TARGET || 'http://localhost:7071'

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': {
        target: proxyTarget,
        changeOrigin: true,
      },
      '/hub': {
        target: proxyTarget,
        changeOrigin: true,
        ws: true,
      }
    }
  }
})
