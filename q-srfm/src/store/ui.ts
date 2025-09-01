import { defineStore } from "pinia";
import { ref } from "vue";
import type { LedgerFilters } from "../composables/useTransactions";

export const useUIStore = defineStore("uiState", () => {
  const selectedAccount = ref<string | null>(null);
  const search = ref("");
  const filterMerchant = ref("");
  const filterMatched = ref(false);
  const filterAmount = ref("");
  const filterImportedMerchant = ref("");
  const filterStartDate = ref("");
  const filterEndDate = ref("");

  // Budget Transactions filters
  const entriesSearch = ref("");
  const entriesFilterMerchant = ref("");
  const entriesFilterAmount = ref("");
  const entriesFilterNote = ref("");
  const entriesFilterStatus = ref("");
  const entriesFilterDate = ref("");
  const entriesFilterAccount = ref("");
  const entriesFilterDuplicates = ref(false);
  const entriesIncludeDeleted = ref(false);
  const selectedBudgetIds = ref<string[]>([]);

  const defaultFilters: LedgerFilters = {
    search: "",
    importedMerchant: "",
    cleared: false,
    uncleared: false,
    reconciled: false,
    duplicatesOnly: false,
    minAmt: null,
    maxAmt: null,
    start: null,
    end: null,
    accountId: null,
  };

  const budgetFilters = ref<LedgerFilters>({ ...defaultFilters });
  const registerFilters = ref<LedgerFilters>({ ...defaultFilters });

  return {
    selectedAccount,
    search,
    filterMerchant,
    filterMatched,
    filterAmount,
    filterImportedMerchant,
    filterStartDate,
    filterEndDate,
    entriesSearch,
    entriesFilterMerchant,
    entriesFilterAmount,
    entriesFilterNote,
    entriesFilterStatus,
    entriesFilterDate,
    entriesFilterAccount,
    entriesFilterDuplicates,
    entriesIncludeDeleted,
    selectedBudgetIds,
    budgetFilters,
    registerFilters,
  };
});
