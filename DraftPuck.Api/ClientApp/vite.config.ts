import { fileURLToPath, URL } from 'node:url'
import { defineConfig, splitVendorChunkPlugin } from 'vite'
import vue from '@vitejs/plugin-vue'
import path, { resolve } from 'path'
import move from './scripts/vite-plugin-move'

export default defineConfig(({ mode }) => {
  const isDev = mode === 'debug'
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
      splitVendorChunkPlugin()
    ],
    server: {
      host: 'localhost',
      port: 17010,
      watch: { usePolling: false },
      // https: {
      //   pfx: path.resolve(process.env.USERPROFILE!, '.aspnet/https/Casknotes.pfx'),
      //   passphrase: 'e20c46ff-c901-483a-9d65-3185c16eb8b1'
      // },
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
        output: {
          entryFileNames: `${fileNamePattern}.js`,
          chunkFileNames: `${fileNamePattern}.js`,
          assetFileNames: `${fileNamePattern}[extname]`
        }
      }
    }
  }

  return config
})
