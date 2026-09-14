import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'node:path';

export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: 'tours-api/wwwroot/dist/travelgroups',
    emptyOutDir: true,
    rollupOptions: {
      input: resolve(import.meta.dirname, 'src/travelgroups/index.js'),
      output: {
        entryFileNames: 'index.js',
        chunkFileNames: 'chunks/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash][extname]'
      }
    }
  }
});

