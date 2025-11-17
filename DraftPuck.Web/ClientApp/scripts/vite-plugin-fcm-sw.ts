import fs from 'fs'
import path from 'path'
import { fileURLToPath } from 'url'
import { loadEnv } from 'vite'

export default function fcmSwEnvPlugin(isDev: boolean) {
  if (!isDev)
    return {
      name: 'rollup-plugin-fcm-sw-env'
    }

  const __filename = fileURLToPath(import.meta.url)
  const __dirname = path.dirname(__filename)

  const srcDir = path.resolve(__dirname, '../src')
  const fcmSwCode = fs.readFileSync(`${srcDir}/firebase-messaging-sw.js`, 'utf8')

  const env = loadEnv('development', process.cwd())

  const transformedCode = fcmSwCode.replace(new RegExp(`import.meta.env.(\\w+)`, 'g'), (_, varName) => `"${env[varName]}"`)
  const finalCode =
    '// IMPORTANT: This file only exists for dev mode purposes. Do not modify this file. Any changes should be made in `src/firebase-messaging-sw.js`.\n\n' +
    transformedCode

  const outputPath = path.resolve('public', './firebase-messaging-sw.js')
  fs.writeFileSync(outputPath, finalCode)

  return {
    name: 'rollup-plugin-fcm-sw-env'
  }
}
