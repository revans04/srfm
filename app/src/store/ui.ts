import { defineStore } from "pinia";
import { ref } from "vue";

export const useUIStore = defineStore("uiState", () => {
  const selectedAccount = ref<string | null>(null);
  const search = ref("");
  const filterMerchant = ref("");
  const filterMatched = ref(false);
  const filterAmount = ref("");
  const filterImportedMerchant = ref("");
  const filterStartDate = ref("");
  const filterEndDate = ref("");

  // Budget Entries filters
  const entriesSearch = ref("");
  const entriesFilterMerchant = ref("");
  const entriesFilterAmount = ref("");
  const entriesFilterNote = ref("");
  const entriesFilterStatus = ref("");
  const entriesFilterDate = ref("");
  const entriesFilterAccount = ref("");
  const entriesFilterDuplicates = ref(false);
  const selectedBudgetIds = ref<string[]>([]);

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
    selectedBudgetIds,
  };
});