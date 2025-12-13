import type { VNode } from 'vue';

declare module 'quasar' {
  interface QMenuSlots {
    activator: (scope: { props: Record<string, unknown> }) => VNode[];
  }
}
