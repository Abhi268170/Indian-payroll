import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [tailwindcss(), react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    host: true,
    port: 5173,
    watch: {
      // polling required for Docker volume mounts on WSL2
      usePolling: true,
    },
    proxy: {
      '/connect': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        headers: { Host: 'demo.localhost' },
      },
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        headers: { Host: 'demo.localhost' },
      },
    },
  },
})
