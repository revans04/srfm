// Minimal type shims to avoid `any` while keeping DX reasonable
declare module 'papaparse' {
  export interface ParseError { message: string }
  export interface ParseResult<T = unknown> { data: T[]; errors: ParseError[] }
  export function parse<T = unknown>(input: string, config?: object): ParseResult<T>
  const Papa: { parse: typeof parse }
  export default Papa;
}

declare module 'file-saver' {
  export function saveAs(data: Blob | File | string, filename?: string, options?: Record<string, unknown>): void;
}

declare module 'lodash' {
  // Prefer importing specific functions like 'lodash/debounce'
  const lodash: unknown;
  export default lodash;
}

declare module 'jszip' {
  // JSZip default export is a class-like constructor
  export default class JSZip {
    file(name: string, data: Blob | Uint8Array | string): this
    generateAsync(options?: Record<string, unknown>): Promise<Blob>
  }
}

// Global augmentations
declare global {
  interface Window {
    // Google Identity Services (used on LoginPage)
    google: {
      accounts?: {
        id: {
          initialize(config: { client_id: string; callback: (response: { credential: string }) => void }): void;
          renderButton(parent: HTMLElement, options?: Record<string, unknown>): void;
          prompt?: () => void;
        };
      };
    };
  }
}

export {};
