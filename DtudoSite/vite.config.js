import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath } from 'node:url'

// https://vite.dev/config/
export default defineConfig(({ mode }) => ({
  plugins: [
    react({
      babel: {
        plugins: [['babel-plugin-react-compiler']],
      },
    }),
  ],
  resolve: {
    alias: [
      {
        find: '@dtudo-app',
        replacement: fileURLToPath(new URL(
          mode === 'homologation' ? './src/app/CatalogOnlyApp.jsx' : './src/app/FullApp.jsx',
          import.meta.url
        )),
      },
      {
        find: '@dtudo-anime-content',
        replacement: fileURLToPath(new URL(
          mode === 'homologation' ? './src/utils/animeContentUtils.catalog.js' : './src/utils/animeContentUtils.js',
          import.meta.url
        )),
      },
    ],
  },
  server: {
    proxy: {
      '/api/catalog': {
        target: 'https://localhost:51376',
        secure: false,
      },
    },
  },
}))
