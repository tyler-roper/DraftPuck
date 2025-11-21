import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv, splitVendorChunkPlugin } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'
import move from './scripts/vite-plugin-move'
import fcm from './scripts/vite-plugin-fcm-sw'
import fs from 'fs'
import os from 'os'

export default defineConfig(({ mode }) => {
  const isDev = mode === 'development'
  const fileNamePattern = !isDev ? '[name]-[hash]' : '[name]'
  const httpsConfig = configureHttps(mode)

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
      https: httpsConfig || true,
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
      sourcemap: false,
      minify: 'terser' as const,
      terserOptions: {
        compress: {
            drop_console: true,
            drop_debugger: true
        },
        mangle: true
      },
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

function configureHttps(mode: string) {
  loadEnv(mode, process.cwd(), '') // Load environment variables
  const certificateName = "DraftPuck.pfx"
  const certificatePassword = "f9aaa3fd-2580-4aa6-8e32-ad0208c40666" 

  // Determine the correct paths based on the OS
  const homeDir = os.userInfo().homedir;
  const appDataPath = os.platform() === 'win32' 
      ? resolve(homeDir, 'AppData/Roaming/ASP.NET/Https', certificateName)
      : resolve(homeDir, '.aspnet/https', certificateName);

  const certPath = [
    appDataPath,
    resolve(homeDir, '.aspnet/https', certificateName)
  ].find(p => fs.existsSync(p));

  if (!certPath) {
    console.warn(`
      🚨 WARNING: Local HTTPS certificate not found at expected ASP.NET locations:
      ${appDataPath}
      ${resolve(homeDir, '.aspnet/https', certificateName)}
      Please ensure you run 'dotnet dev-certs https -ep <path> -p <password>' to export the certificate.
    `);
  }

  return certPath 
    ? {
        pfx: fs.readFileSync(certPath), // Read the binary PFX file content
        passphrase: certificatePassword
      }
    : null;
}
