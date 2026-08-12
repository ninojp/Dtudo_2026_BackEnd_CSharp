import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath } from 'node:url'

const noIndexHeaders = {
  'X-Robots-Tag': 'noindex, nofollow, noarchive',
}

const gatewayProxy = {
  target: 'https://localhost:51376',
  secure: false,
}

// https://vite.dev/config/
export default defineConfig({
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
        replacement: fileURLToPath(new URL('./src/app/FullApp.jsx', import.meta.url)),
      },
      {
        find: '@dtudo-anime-content',
        replacement: fileURLToPath(new URL('./src/utils/animeContentUtils.js', import.meta.url)),
      },
    ],
  },
  server: {
    headers: noIndexHeaders,
    proxy: {
      '/api/catalog': { ...gatewayProxy },
      '/api/external': { ...gatewayProxy },
      '/bff': { ...gatewayProxy },
      '/identity': { ...gatewayProxy },
      '/signin-oidc': { ...gatewayProxy },
      '/signout-callback-oidc': { ...gatewayProxy },
    },
  },
  preview: {
    headers: noIndexHeaders,
  },
})
