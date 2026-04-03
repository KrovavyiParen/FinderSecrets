import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5200', 
        changeOrigin: true,
        configure: (proxy, options) => {
          proxy.on('proxyRes', (proxyRes, req, res) => {
            // Проксируем статус 401 и заголовки
            if (proxyRes.statusCode === 401) {
              res.statusCode = 401
              if (proxyRes.headers['www-authenticate']) {
                res.setHeader('WWW-Authenticate', proxyRes.headers['www-authenticate'])
              }
            }
          })
        }
      }
    }
  }
})
