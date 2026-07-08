import path from 'node:path';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, 'src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5018',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://localhost:5018',
        changeOrigin: true,
        ws: true,
      },
      '/health': {
        target: 'http://localhost:5018',
        changeOrigin: true,
      },
    },
  },
});
