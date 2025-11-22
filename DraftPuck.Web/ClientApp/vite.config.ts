import { fileURLToPath, URL } from 'node:url'
import { defineConfig, splitVendorChunkPlugin, loadEnv } from 'vite' 
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'
import move from './scripts/vite-plugin-move'
import fcm from './scripts/vite-plugin-fcm-sw'
import fs from 'fs'
import os from 'os'

export default defineConfig(({ mode }) => {
  const isDev = mode === 'development'
  const fileNamePattern = !isDev ? '[name]-[hash]' : '[name]'
  const env = loadEnv(mode, process.cwd(), '') 

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
      https: isDev ? configureHttps(env) : false, 
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

function configureHttps(env: Record<string, string>) {
  const certificateName = "DraftPuck.pfx"
  const passwordKey = 'VITE_AUTH_PFX_PASSWORD';

  const certificatePassword = env[passwordKey];

  if (!certificatePassword) {
      console.error(`
        ❌ ERROR: Certificate password not found.
        Please ensure '${passwordKey}' is defined in your .env.development file.
        Falling back to a standard self-signed certificate.
      `);
      return true;
  }

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
        Run CreateCertificate.ps1 to create it.
      `);
    return true;
  }

  return {
    pfx: fs.readFileSync(certPath),
    passphrase: certificatePassword
  };
}