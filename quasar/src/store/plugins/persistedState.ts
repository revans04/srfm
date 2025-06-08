import { PiniaPluginContext } from 'pinia';

function replacer(_key: string, value: any) {
  if (value instanceof Map) {
    return {
      __type: 'Map',
      value: Array.from(value.entries()),
    };
  }
  return value;
}

function reviver(_key: string, value: any) {
  if (value && value.__type === 'Map') {
    return new Map(value.value);
  }
  return value;
}

export default function persistedState({ store }: PiniaPluginContext) {
  const storageKey = `pinia-${store.$id}`;
  const fromStorage = localStorage.getItem(storageKey);
  if (fromStorage) {
    try {
      store.$patch(JSON.parse(fromStorage, reviver));
    } catch (err) {
      console.error('Failed to parse state from localStorage for', store.$id, err);
    }
  }

  store.$subscribe((_, state) => {
    try {
      localStorage.setItem(storageKey, JSON.stringify(state, replacer));
    } catch (err) {
      console.error('Failed to save state to localStorage for', store.$id, err);
    }
  });
}