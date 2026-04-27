import type { PiniaPluginContext } from 'pinia';
import { onAuthStateChanged } from 'firebase/auth';
import { auth } from '../../firebase/init';

/**
 * Stores that intentionally carry across user sessions:
 *   - `auth` — Firebase manages its own session; the persisted shape only
 *     includes UI hints (avatarSrc) that are harmless to share.
 *   - `uiState` — UI prefs (drawer state, theme, etc.) are device-local, not
 *     user-scoped.
 * Everything else is treated as user-scoped: storage keys are namespaced by
 * the current UID, and on UID change the in-memory state is reset to its
 * initial snapshot before re-hydrating from the new user's namespace.
 */
const USER_AGNOSTIC_STORES = new Set<string>(['auth', 'uiState']);

function replacer(_key: string, value: unknown) {
  if (value instanceof Map) {
    return {
      __type: 'Map',
      value: Array.from(value.entries()),
    };
  }
  return value;
}

function reviver(_key: string, value: unknown) {
  if (value && typeof value === 'object' && '__type' in (value as Record<string, unknown>)) {
    const v = value as { __type: string; value: unknown };
    if (v.__type === 'Map' && Array.isArray(v.value)) {
      return new Map(v.value as [string, unknown][]);
    }
  }
  return value;
}

/**
 * Pinia persistence plugin.
 *
 * **User isolation invariant:** user-scoped store data must never leak
 * across accounts on the same browser. Two layers:
 *
 *   1. **Namespaced keys.** Storage keys are `pinia-<storeId>-<uid>` for
 *      user-scoped stores. Without a UID, user-scoped stores are NOT
 *      written (no anonymous bucket that could later be claimed).
 *
 *   2. **Auth-change reset.** When Firebase reports an auth-state change
 *      (login, logout, account switch), the in-memory state of every
 *      user-scoped store is wiped back to its initial snapshot, then
 *      re-hydrated from the incoming user's namespace.
 *
 * The combination means: if user A signs out and user B signs in on the
 * same tab, B never observes A's family/merchants/tour state — both in
 * memory and in any subsequent localStorage read.
 */
export default function persistedState({ store }: PiniaPluginContext) {
  // The budgets store contains all budgets and their transactions.
  // Persisting this large dataset regularly exceeds the localStorage quota
  // and throws QuotaExceededError during imports. Skip it entirely.
  if (store.$id === 'budgets') {
    return;
  }

  const storeId = store.$id;
  const isUserScoped = !USER_AGNOSTIC_STORES.has(storeId);

  // Capture the store's pristine state so we can reset on UID change
  // without requiring each setup-style store to expose a $reset method.
  // JSON-clone is fine here: every persisted store's state is JSON-safe by
  // construction (we serialize to localStorage in the same shape).
  const initialSnapshot = JSON.parse(JSON.stringify(store.$state));

  function storageKeyFor(uid: string | null): string | null {
    if (isUserScoped) {
      if (!uid) return null;
      return `pinia-${storeId}-${uid}`;
    }
    return `pinia-${storeId}`;
  }

  function loadFromStorage(uid: string | null) {
    const key = storageKeyFor(uid);
    if (!key) return;
    const raw = localStorage.getItem(key);
    if (!raw) return;
    try {
      store.$patch(JSON.parse(raw, reviver));
    } catch (err) {
      console.error('Failed to parse state from localStorage for', storeId, err);
    }
  }

  function resetToInitialSnapshot() {
    // Pinia setup stores don't expose $reset; we replicate the behaviour
    // by re-applying our captured initial snapshot. JSON-clone the
    // snapshot every time so the store can mutate the result freely
    // without polluting our captured pristine copy.
    const fresh = JSON.parse(JSON.stringify(initialSnapshot)) as Record<string, unknown>;
    store.$patch((state) => {
      const target = state as Record<string, unknown>;
      // Reset existing keys to their initial values; keys absent from the
      // snapshot get cleared to undefined (rare but possible if a store
      // adds a ref dynamically).
      for (const key of Object.keys(target)) {
        target[key] = fresh[key];
      }
      // Cover any keys the snapshot has that the current state has
      // dropped (also rare; defensive).
      for (const key of Object.keys(fresh)) {
        if (!(key in target)) target[key] = fresh[key];
      }
    });
  }

  // Initial hydration. For user-agnostic stores this runs immediately. For
  // user-scoped stores this is a no-op when auth hasn't resolved yet (no
  // UID → no key → no read); the onAuthStateChanged listener below will
  // handle the deferred hydration.
  loadFromStorage(auth.currentUser?.uid ?? null);

  // Track the last UID we've seen so we can detect changes on every
  // auth-state event Firebase emits (some events fire without a UID
  // change — e.g. token refresh).
  let lastUid: string | null = auth.currentUser?.uid ?? null;
  onAuthStateChanged(auth, (firebaseUser) => {
    const newUid = firebaseUser?.uid ?? null;
    if (newUid === lastUid) return;
    const previousUid = lastUid;
    lastUid = newUid;

    if (!isUserScoped) return;

    // On logout (newUid === null) wipe the previous user's persisted
    // snapshot off disk. Without this, signing out leaves
    // `pinia-<storeId>-<previousUid>` sitting in localStorage indefinitely
    // — inaccessible to the next user that signs in (their UID generates a
    // different key) but a privacy concern on shared devices.
    if (newUid === null && previousUid) {
      try {
        localStorage.removeItem(`pinia-${storeId}-${previousUid}`);
      } catch (err) {
        console.warn('Failed to clear persisted state on logout for', storeId, err);
      }
    }

    resetToInitialSnapshot();
    loadFromStorage(newUid);
  });

  store.$subscribe((_, state) => {
    const uid = auth.currentUser?.uid ?? null;
    const key = storageKeyFor(uid);
    // No key → no write. For user-scoped stores this means we skip writes
    // until the user's UID is known, preventing an "anonymous" cache that
    // could later be associated with a freshly-authenticated user.
    if (!key) return;
    try {
      localStorage.setItem(key, JSON.stringify(state, replacer));
    } catch (err) {
      console.error('Failed to save state to localStorage for', storeId, err);
    }
  });
}
