function replacer(_key, value) {
    if (value instanceof Map) {
        return {
            __type: 'Map',
            value: Array.from(value.entries()),
        };
    }
    return value;
}
function reviver(_key, value) {
    if (value && typeof value === 'object' && '__type' in value) {
        const v = value;
        if (v.__type === 'Map' && Array.isArray(v.value)) {
            return new Map(v.value);
        }
    }
    return value;
}
export default function persistedState({ store }) {
    // The budgets store contains all budgets and their transactions. Persisting
    // this large dataset regularly exceeds the localStorage quota and throws
    // QuotaExceededError during imports.  Avoid persisting the budgets store to
    // keep the console clean and the application responsive.
    if (store.$id === 'budgets') {
        return;
    }
    const storageKey = `pinia-${store.$id}`;
    const fromStorage = localStorage.getItem(storageKey);
    if (fromStorage) {
        try {
            store.$patch(JSON.parse(fromStorage, reviver));
        }
        catch (err) {
            console.error('Failed to parse state from localStorage for', store.$id, err);
        }
    }
    store.$subscribe((_, state) => {
        try {
            localStorage.setItem(storageKey, JSON.stringify(state, replacer));
        }
        catch (err) {
            console.error('Failed to save state to localStorage for', store.$id, err);
        }
    });
}
