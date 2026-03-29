import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
export const useMerchantStore = defineStore('merchants', () => {
    // State: List of merchants sorted by usage
    const merchants = ref([]);
    // Action: Update merchants with a new list of transactions
    function updateMerchants(transactions) {
        // Count usage of each merchant
        const merchantCounts = {};
        transactions.forEach((transaction) => {
            if (!transaction.deleted && transaction.merchant) {
                merchantCounts[transaction.merchant] = (merchantCounts[transaction.merchant] || 0) + 1;
            }
        });
        // Convert to array and sort by usage count (descending)
        merchants.value = Object.entries(merchantCounts)
            .map(([name, usageCount]) => ({
            name,
            usageCount,
        }))
            .sort((a, b) => b.usageCount - a.usageCount);
    }
    // Action: Update merchants from a precomputed counts object
    function updateMerchantsFromCounts(merchantCounts) {
        merchants.value = Object.entries(merchantCounts)
            .map(([name, usageCount]) => ({
            name,
            usageCount,
        }))
            .sort((a, b) => b.usageCount - a.usageCount);
    }
    // Getter: Get the list of merchant names for autocomplete
    const merchantNames = computed(() => merchants.value.map((m) => m.name));
    return {
        merchants,
        updateMerchants,
        updateMerchantsFromCounts,
        merchantNames,
    };
});
