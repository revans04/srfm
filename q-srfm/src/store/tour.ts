import { defineStore } from 'pinia';
import { shallowRef, computed } from 'vue';
import { useBudgetStore } from './budget';
import { useFamilyStore } from './family';
import { useAuthStore } from './auth';
import { computeChecklistCompletion } from '../utils/onboardingChecklist';

export interface ChecklistItem {
  id: string;
  label: string;
  completed: boolean;
}

export const useTourStore = defineStore('tour', () => {
  const dismissedTips = shallowRef<string[]>([]);
  const completedChecklist = shallowRef<string[]>([]);
  const initialized = shallowRef(false);

  // NB: stable IDs are persisted in localStorage via `completedChecklist`.
  // Renaming an `id` would orphan a user's existing progress. Adding new
  // items at the bottom is safe; the IDs `verify-email` and `invite-partner`
  // were added in PR 4 of the SetupWizard rewrite (extends the bare 5-item
  // checklist to 7 once the new banners + invite flow start ticking them).
  const checklistItems: { id: string; label: string }[] = [
    { id: 'create-budget', label: 'Create your first budget' },
    { id: 'enter-transaction', label: 'Enter a transaction' },
    { id: 'import-transactions', label: 'Link or import bank transactions' },
    { id: 'setup-goal', label: 'Set up a savings goal' },
    { id: 'reconcile-account', label: 'Reconcile an account' },
    { id: 'verify-email', label: 'Verify your email address' },
    { id: 'invite-partner', label: 'Invite someone to your family' },
  ];

  // Derive completion from actual data in the budget/family/auth stores.
  // Each check is reactive — as stores hydrate, items tick on automatically.
  // Sticky: once a check goes true, it stays true via completedChecklist
  // (so a user who clears state still sees historical progress).
  // The pure derivation lives in `utils/onboardingChecklist` so the rules
  // can be unit-tested without a Pinia harness.
  const derivedCompleted = computed<Record<string, boolean>>(() => {
    const budgetStore = useBudgetStore();
    const familyStore = useFamilyStore();
    const authStore = useAuthStore();
    return computeChecklistCompletion({
      budgets: Array.from(budgetStore.budgets.values()),
      family: familyStore.family,
      authUser: authStore.user,
    });
  });

  const checklist = computed<ChecklistItem[]>(() => {
    const sticky = completedChecklist.value;
    const derived = derivedCompleted.value;
    return checklistItems.map((item) => ({
      ...item,
      completed: Boolean(derived[item.id]) || sticky.indexOf(item.id) >= 0,
    }));
  });

  const completedCount = computed(() => checklist.value.filter((i) => i.completed).length);
  const totalCount = checklistItems.length;
  const allComplete = computed(() => completedCount.value >= totalCount);

  function isTipDismissed(tipId: string): boolean {
    return dismissedTips.value.indexOf(tipId) >= 0;
  }

  function dismissTip(tipId: string) {
    if (dismissedTips.value.indexOf(tipId) < 0) {
      dismissedTips.value = [...dismissedTips.value, tipId];
      saveTourState();
    }
  }

  function dismissAllTips() {
    dismissedTips.value = [
      'budget-page',
      'budget-register',
      'account-register',
      'match-transactions',
      'accounts-page',
      'reports-page',
    ];
    saveTourState();
  }

  function completeChecklistItem(itemId: string) {
    if (completedChecklist.value.indexOf(itemId) < 0) {
      completedChecklist.value = [...completedChecklist.value, itemId];
      saveTourState();
    }
  }

  function saveTourState() {
    try {
      const state = {
        dismissedTips: [...dismissedTips.value],
        completedChecklist: [...completedChecklist.value],
      };
      localStorage.setItem('srfm-tour-state', JSON.stringify(state));
    } catch {
      // localStorage may be unavailable
    }
  }

  function loadTourState() {
    try {
      const raw = localStorage.getItem('srfm-tour-state');
      if (raw) {
        const state = JSON.parse(raw);
        if (Array.isArray(state.dismissedTips)) {
          dismissedTips.value = state.dismissedTips;
        } else {
          // Clear stale data from previous Set-based version
          localStorage.removeItem('srfm-tour-state');
        }
        if (Array.isArray(state.completedChecklist)) {
          completedChecklist.value = state.completedChecklist;
        }
      }
    } catch {
      localStorage.removeItem('srfm-tour-state');
    }
    initialized.value = true;
  }

  return {
    dismissedTips,
    completedChecklist,
    initialized,
    checklist,
    completedCount,
    totalCount,
    allComplete,
    isTipDismissed,
    dismissTip,
    dismissAllTips,
    completeChecklistItem,
    loadTourState,
  };
});
