import { defineStore } from 'pinia';
import { shallowRef, computed } from 'vue';
export const useTourStore = defineStore('tour', () => {
    const dismissedTips = shallowRef([]);
    const completedChecklist = shallowRef([]);
    const initialized = shallowRef(false);
    const checklistItems = [
        { id: 'create-budget', label: 'Create your first budget' },
        { id: 'enter-transaction', label: 'Enter a transaction' },
        { id: 'import-transactions', label: 'Import bank transactions' },
        { id: 'setup-goal', label: 'Set up a savings goal' },
        { id: 'reconcile-account', label: 'Reconcile an account' },
    ];
    const checklist = computed(() => {
        const completed = completedChecklist.value;
        return checklistItems.map((item) => ({
            ...item,
            completed: completed.indexOf(item.id) >= 0,
        }));
    });
    const completedCount = computed(() => completedChecklist.value.length);
    const totalCount = checklistItems.length;
    const allComplete = computed(() => completedCount.value >= totalCount);
    function isTipDismissed(tipId) {
        return dismissedTips.value.indexOf(tipId) >= 0;
    }
    function dismissTip(tipId) {
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
    function completeChecklistItem(itemId) {
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
        }
        catch {
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
                }
                else {
                    // Clear stale data from previous Set-based version
                    localStorage.removeItem('srfm-tour-state');
                }
                if (Array.isArray(state.completedChecklist)) {
                    completedChecklist.value = state.completedChecklist;
                }
            }
        }
        catch {
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
