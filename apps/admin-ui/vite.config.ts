import path from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const repoRoot = path.resolve(__dirname, '../..')

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, 'src'),
      '@repoDocs': path.resolve(repoRoot, 'docs'),
    },
  },
  server: {
    fs: {
      allow: [repoRoot],
    },
    proxy: {
      '/api': { target: 'http://127.0.0.1:5050', changeOrigin: true },
      '/health': { target: 'http://127.0.0.1:5050', changeOrigin: true },
    },
  },
})
