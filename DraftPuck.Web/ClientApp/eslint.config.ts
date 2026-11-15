import js from '@eslint/js'
import { globalIgnores } from 'eslint/config'
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript'
import pluginVue from 'eslint-plugin-vue'
import skipFormatting from '@vue/eslint-config-prettier/skip-formatting'
import globals from 'globals'

export default defineConfigWithVueTs(
  {
    name: 'app/files-to-lint',
    files: ['**/*.{ts,mts,tsx,vue,js,mjs,cjs}']
  },

  // public/ holds static assets and files the dev server generates (env.js, env-sw.js, the dev service worker).
  globalIgnores(['dist/**', 'coverage/**', 'public/**']),

  js.configs.recommended,
  pluginVue.configs['flat/essential'],
  vueTsConfigs.recommended,
  skipFormatting,

  {
    name: 'app/rules',
    rules: {
      '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_' }]
    }
  },

  {
    name: 'app/service-worker',
    files: ['src/firebase-messaging-sw.js'],
    languageOptions: {
      // firebase comes from the SDK the worker loads with importScripts.
      globals: { ...globals.serviceworker, firebase: 'readonly' }
    }
  },

  {
    name: 'app/node',
    files: ['*.config.ts', 'scripts/**'],
    languageOptions: { globals: globals.node }
  }
)
