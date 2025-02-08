import { fileURLToPath, URL } from 'node:url'
import { defineConfig, splitVendorChunkPlugin } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'
import move from './scripts/vite-plugin-move'
import fcm from './scripts/vite-plugin-fcm-sw'

export default defineConfig(({ mode }) => {
  const isDev = mode === 'development'
  const fileNamePattern = !isDev ? '[name]-[hash]' : '[name]'

  const config = {
    plugins: [
      vue(),
      move([
        {
          source: resolve(__dirname, '../wwwroot/index.html'),
          dest: resolve(__dirname, '../Views/App/Index.cshtml')
        }
      ]),
      splitVendorChunkPlugin(),
      fcm(isDev)
    ],
    server: {
      host: 'localhost',
      port: 17010,
      watch: { usePolling: false },
      https: {
        pfx: resolve(process.env.USERPROFILE!, '.aspnet/https/DraftPuck.pfx'),
        passphrase: 'b30b3f16-18c1-4a00-8f8f-0ce0fba61cb2'
      },
      proxy: {
        '/api': {
          target: 'https://localhost:17000',
          secure: false
        },
        '/hub': {
          target: 'https://localhost:17000',
          secure: false
        }
      }
    },
    resolve: {
      alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) }
    },
    build: {
      outDir: '../wwwroot',
      emptyOutdir: true,
      sourcemap: true,
      chunkSizeWarningLimit: 600,
      rollupOptions: {
        input: {
          'main': './index.html',
          'firebase-messaging-sw': './src/firebase-messaging-sw.js'
        },
        output: {
          entryFileNames: (chunkInfo: { name: string }) => {
            return chunkInfo.name === 'firebase-messaging-sw'
              ? '[name].js'
              : `${fileNamePattern}.js`
          },
          chunkFileNames: `${fileNamePattern}.js`,
          assetFileNames: `${fileNamePattern}[extname]`
        }
      }
    }
  }

  return config
})
