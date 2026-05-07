import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

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
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5200',
        changeOrigin: true,
        headers: {
          'Accept': 'application/json',
        },
        followRedirects: true,
        configure: (proxy, options) => {
          proxy.on('proxyRes', (proxyRes, req, res) => {
            if (proxyRes.headers['www-authenticate']) {
              res.setHeader('WWW-Authenticate', proxyRes.headers['www-authenticate']);
            }
            if (proxyRes.statusCode === 401) {
              res.statusCode = 401;
            }
          });
        }
      }
    }
  }
})