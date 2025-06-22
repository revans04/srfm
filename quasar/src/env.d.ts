/// <reference types="vite/client" />

declare namespace NodeJS {
  interface ProcessEnv {
    NODE_ENV: string;
    VUE_ROUTER_MODE: 'hash' | 'history' | 'abstract' | undefined;
    VUE_ROUTER_BASE: string | undefined;
  }
}

declare module "papaparse";
declare module "file-saver";

// Extend Quasar QTable slot typings so that dynamic item and header
// slots (e.g. `v-slot:item.foo`) are recognised by TypeScript.
// This mirrors how slots are used throughout the project.
declare module 'quasar' {
  interface QTableSlots {
    [name: `item.${string}`]: (scope: any) => any
    [name: `header.${string}`]?: (scope: any) => any
  }
}
