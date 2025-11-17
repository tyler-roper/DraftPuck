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
    css: {
      preprocessorOptions: {
        scss: {
          api: 'modern-compiler',
          silenceDeprecations: ['legacy-js-api', 'import', 'global-builtin', 'color-functions']
        }
      }
    },
    server: {
      host: 'localhost',
      port: 17010,
      watch: { usePolling: false },
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
