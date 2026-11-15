import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'
import move from './scripts/vite-plugin-move.ts'
import fcm from './scripts/vite-plugin-fcm-sw.ts'
import fs from 'fs'
import os from 'os'

export default defineConfig(({ mode }) => {
  const isDev = mode === 'development'
  const fileNamePattern = !isDev ? '[name]-[hash]' : '[name]'
  const env = loadEnv(mode, process.cwd(), '')

  return {
    plugins: [
      vue(),
      move([
        {
          source: resolve(import.meta.dirname, '../wwwroot/index.html'),
          dest: resolve(import.meta.dirname, '../Views/App/Index.cshtml')
        }
      ]),
      fcm(isDev)
    ],
    css: {
      preprocessorOptions: {
        scss: {
          silenceDeprecations: ['import', 'global-builtin', 'color-functions']
        }
      }
    },
    server: {
      host: 'localhost',
      port: 17010,
      watch: { usePolling: false },
      https: isDev ? configureHttps(env) : undefined,
      proxy: {
        '/api': {
          target: 'https://localhost:17000',
          secure: false
        },
        '/hub': {
          target: 'https://localhost:17000',
          secure: false,
          ws: true
        }
      }
    },
    resolve: {
      alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) }
    },
    build: {
      outDir: '../wwwroot',
      sourcemap: false,
      chunkSizeWarningLimit: 600,
      rolldownOptions: {
        input: {
          'main': './index.html',
          'firebase-messaging-sw': './src/firebase-messaging-sw.js'
        },
        output: {
          entryFileNames: (chunkInfo) => {
            return chunkInfo.name === 'firebase-messaging-sw'
              ? '[name].js'
              : `${fileNamePattern}.js`
          },
          chunkFileNames: `${fileNamePattern}.js`,
          assetFileNames: `${fileNamePattern}[extname]`,
          minify: { compress: { dropConsole: true, dropDebugger: true } },
          // Libraries the app loads at startup get their own chunk, which stays cached across deploys. Libraries only
          // a lazy-loaded view uses (SignalR in the lobby, for one) stay with that view.
          codeSplitting: {
            groups: [{ name: 'vendor', test: /[\\/]node_modules[\\/]/, tags: ['$initial'] }]
          }
        }
      }
    }
  }
})

function configureHttps(env: Record<string, string>) {
  const certificateName = "DraftPuck.pfx"
  const passwordKey = 'VITE_AUTH_PFX_PASSWORD';

  const certificatePassword = env[passwordKey];

  if (!certificatePassword) {
      console.error(`
        ❌ ERROR: Certificate password not found.
        Please ensure '${passwordKey}' is defined in your .env.development file.
        Serving over HTTP instead.
      `);
      return undefined;
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
        Run CreateCertificate.ps1 to create it. Serving over HTTP instead.
      `);
    return undefined;
  }

  return {
    pfx: fs.readFileSync(certPath),
    passphrase: certificatePassword
  };
}