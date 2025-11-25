declare module 'vue-smart-suggest' {
  import type { DefineComponent } from 'vue';
  export const SmartSuggest: DefineComponent<any, any, any>;
  export type Trigger = { char: string; items: {value: string }[] }
}