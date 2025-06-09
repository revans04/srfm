<!-- src/pages/DashboardView.vue -->
<template>
  <q-page :class="isMobile ? 'pt-0 px-0' : ''">
    <!-- Loading Animation -->
    <q-row v-if="loading" justify="center" class="q-mt-lg">
      <q-spinner color="primary" size="50px" />
      <q-col v-if="showLoadingMessage" cols="12" class="text-center q-mt-sm">
        <span>Still loading budgets, please wait...</span>
      </q-col>
    </q-row>

    <!-- No Budgets Found -->
    <q-row v-else-if="!loading && budgets.length === 0" justify="center" class="q-mt-lg">
      <q-col cols="12" class="text-center">
        <q-banner dense class="bg-info text-white">
          <q-row align="center">
            <q-col class="grow">
              No budgets found for {{ selectedEntity?.name || 'selected entity' }}. Would you like to create a default budget for
              {{ formatMonth(currentMonth) }}? You can also import budget information from the Data Page.
            </q-col>
            <q-col class="shrink">
              <q-btn color="primary" label="Create Default Budget" @click="createDefaultBudget" />
            </q-col>
          </q-row>
        </q-banner>
      </q-col>
    </q-row>

    <!-- Main Content (hidden during loading) -->
    <div v-else>
      <q-row :class="isMobile ? 'q-pa-sm' : 'q-pr-xl'">
        <q-col cols="12">
          <!-- Entity Dropdown -->
          <q-select
            v-model="familyStore.selectedEntityId"
            :options="entityOptions"
            option-label="name"
            option-value="id"
            label="Select Entity"
            outlined
            dense
            clearable
            @update:model-value="loadBudgets"
            class="entity-select q-mb-sm"
          />
          <div class="row items-center">
            <q-menu v-model="menuOpen" :offset="[0, 8]">
              <div slot="activator" class="month-selector" :class="{ 'text-white': isMobile }" @click="menuOpen = !menuOpen">
                <h1 class="text-h4">
                  {{ formatMonth(currentMonth) }}
                  <q-icon name="mdi-chevron-down" size="sm" />
                </h1>
              </div>
              <q-card class="month-menu">
                <q-row no-gutters class="month-navigation items-center">
                  <q-col cols="auto">
                    <q-btn icon="mdi-chevron-left" flat dense @click.stop="shiftMonths(-6)" />
                  </q-col>
                  <q-col class="text-center">
                    <span>{{ displayYear }}</span>
                  </q-col>
                  <q-col cols="auto">
                    <q-btn icon="mdi-chevron-right" flat dense @click.stop="shiftMonths(6)" />
                  </q-col>
                </q-row>
                <q-row no-gutters>
                  <q-col v-for="month in displayedMonths" :key="month.value" cols="4" class="month-item">
                    <div
                      class="q-pa-xs rounded-borders cursor-pointer q-ma-xs text-center"
                      :class="month.value === currentMonth ? 'bg-primary text-white' : ''"
                      :style="{
                        borderWidth: '1px',
                        borderColor: 'var(--q-primary)',
                        borderStyle: monthExists(month.value) ? 'solid' : 'dashed',
                      }"
                      @click="selectMonth(month.value)"
                    >
                      {{ month.label }}
                    </div>
                  </q-col>
                </q-row>
              </q-card>
            </q-menu>
            <div class="q-ml-sm">
              <q-btn v-if="!isMobile && !isEditing" icon="mdi-pencil" flat dense color="primary" title="Edit Budget" @click="isEditing = true" />
              <q-btn v-if="isEditing" icon="mdi-close" flat dense title="Cancel" @click="isEditing = false" />
              <q-btn v-if="!isMobile && !isEditing" icon="mdi-trash-can-outline" flat dense color="negative" title="Delete Budget" />
            </div>
          </div>
          <div
            class="q-pr-sm q-py-none"
            :class="{
              'text-center': isMobile,
              'text-left': !isMobile,
              'text-white': isMobile,
              'text-negative': !isMobile && remainingToBudget < 0,
            }"
          >
            {{ formatCurrency(toDollars(toCents(Math.abs(remainingToBudget)))) }}
            {{ remainingToBudget >= 0 ? 'left to budget' : 'over budget' }}
          </div>
        </q-col>
        <q-col v-if="!isMobile || selectedCategory == null" cols="12" sm="6">
          <div class="q-my-sm bg-white rounded-borders q-pa-sm q-mb-lg">
            <q-input v-model="search" label="Search" dense outlined append-icon="mdi-magnify" />
          </div>
        </q-col>
        <q-btn v-if="isMobile && !selectedCategory" icon="mdi-plus" round color="white" class="fixed-top-right" @click="addTransaction" />
      </q-row>

      <q-row>
        <!-- Main Content -->
        <q-col :cols="selectedCategory ? (isMobile ? 0 : 8) : 12" :class="{ 'd-none': selectedCategory && isMobile }">
          <!-- Budget Editing Form -->
          <q-card v-if="isEditing">
            <q-card-section>
              <div class="text-h6">Edit Budget for {{ selectedEntity?.name || 'selected entity' }}</div>
            </q-card-section>
            <q-card-section>
              <q-form @submit="saveBudget">
                <!-- Merchants Section -->
                <q-row class="q-mt-md">
                  <q-col cols="12">
                    <h3>Merchants</h3>
                    <q-chip-group column>
                      <q-chip
                        v-for="(merchant, index) in budget.merchants"
                        :key="merchant.name"
                        :label="`${merchant.name} (${merchant.usageCount})`"
                        removable
                        @remove="removeMerchant(index)"
                        class="q-ma-xs"
                      />
                    </q-chip-group>
                    <q-input
                      v-model="newMerchantName"
                      label="Add Merchant"
                      dense
                      outlined
                      class="q-mt-sm"
                      @keyup.enter="addMerchant"
                      append-icon="mdi-plus"
                      @click:append="addMerchant"
                    />
                  </q-col>
                </q-row>

                <q-row class="q-mt-md">
                  <q-col cols="12">
                    <h3>Categories</h3>
                  </q-col>
                </q-row>
                <q-row v-for="(cat, index) in budget.categories" :key="index" class="items-center" no-gutters>
                  <q-col cols="12" sm="3" class="q-pa-sm">
                    <q-input v-model="cat.name" label="Category" outlined dense required />
                  </q-col>
                  <q-col cols="12" sm="2" class="q-pa-sm">
                    <q-input v-model="cat.group" label="Group (e.g., Utilities)" outlined dense />
                  </q-col>
                  <q-col cols="12" sm="2" class="q-pa-sm">
                    <Currency-Input v-model.number="cat.target" label="Target" class="text-right" dense required />
                  </q-col>
                  <q-col cols="12" sm="2" class="q-pa-sm">
                    <Currency-Input v-model="cat.carryover" label="Carryover" class="text-right" dense />
                  </q-col>
                  <q-col cols="12" sm="2" class="q-pa-sm">
                    <q-checkbox v-model="cat.isFund" label="Is Fund?" dense />
                  </q-col>
                  <q-col cols="12" sm="1" class="q-pa-sm">
                    <q-btn icon="mdi-close" color="negative" flat dense @click="removeCategory(index)" />
                  </q-col>
                </q-row>

                <q-btn color="primary" label="Add Category" class="q-mt-sm" @click="addCategory" />
                <q-btn color="positive" label="Add Income Category" class="q-mt-sm q-ml-sm" @click="addIncomeCategory" />
                <q-btn type="submit" color="positive" label="Save Budget" class="q-mt-sm q-ml-sm" :loading="saving" />
              </q-form>
            </q-card-section>
          </q-card>

          <!-- Income Section -->
          <q-card v-if="!isEditing && incomeItems">
            <q-card-section>
              <q-row>
                <q-col class="text-bold"> Income for {{ selectedEntity?.name || 'selected entity' }} </q-col>
                <q-col v-if="!isMobile" cols="auto">Planned</q-col>
                <q-col :cols="isMobile ? 'auto' : '2'" class="text-right">Received</q-col>
              </q-row>
              <div v-for="item in incomeItems" :key="item.name" style="border-bottom: 1px solid var(--q-light)" class="q-py-sm">
                <q-row @click="onIncomeRowClick(item)" class="cursor-pointer">
                  <q-col>{{ item.name }}</q-col>
                  <q-col v-if="!isMobile" cols="auto">
                    {{ formatCurrency(toDollars(toCents(item.planned))) }}
                  </q-col>
                  <q-col :cols="isMobile ? 'auto' : '2'" class="text-right">
                    <div :class="item.received > item.planned ? 'text-positive' : ''">
                      {{ formatCurrency(toDollars(toCents(item.received))) }}
                    </div>
                  </q-col>
                </q-row>
              </div>
              <q-row v-if="!isMobile" no-gutters>
                <q-col>
                  <q-space />
                </q-col>
                <q-col cols="auto" class="text-bold">
                  {{ formatCurrency(toDollars(toCents(plannedIncome))) }}
                </q-col>
                <q-col :cols="isMobile ? 'auto' : '2'">
                  <div class="text-bold text-right" :class="actualIncome > plannedIncome ? 'text-positive' : ''">
                    {{ formatCurrency(toDollars(toCents(actualIncome))) }}
                  </div>
                </q-col>
              </q-row>
            </q-card-section>
          </q-card>

          <!-- Category Tables -->
          <q-row v-if="!isEditing && catTransactions" class="q-mt-md">
            <q-col cols="12" v-for="(g, gIdx) in groups" :key="gIdx">
              <q-card>
                <q-card-section>
                  <q-row class="text-info">
                    <q-col>{{ g.group || 'Ungrouped' }}</q-col>
                    <q-space />
                    <q-col v-if="!isMobile" cols="2">Planned</q-col>
                    <q-col :cols="isMobile ? 'auto' : '2'">Remaining</q-col>
                  </q-row>
                  <div v-for="(item, idx) in catTransactions.filter((c) => c.group == g.group)" :key="idx">
                    <q-row @click="handleRowClick(item)" class="cursor-pointer">
                      <q-col
                        v-if="!(inlineEdit.item?.name === item.name && inlineEdit.field === 'name')"
                        @dblclick.stop="handleNameDblClick(item)"
                        @touchstart="startTouch(item, 'name')"
                        @touchend="endTouch"
                      >
                        <q-icon v-if="item.isFund" name="mdi-piggy-bank-outline" size="sm" color="primary" class="q-mr-xs" />
                        {{ item.name }}
                      </q-col>
                      <q-col v-else>
                        <q-input
                          v-model="inlineEdit.value"
                          dense
                          filled
                          autofocus
                          @keydown.enter="saveInlineEdit"
                          @keydown.esc="cancelInlineEdit"
                          @blur="saveInlineEdit"
                        />
                      </q-col>
                      <q-col
                        v-if="!isMobile && !(inlineEdit.item?.name === item.name && inlineEdit.field === 'target')"
                        cols="2"
                        @dblclick.stop="handleTargetDblClick(item)"
                        @touchstart="startTouch(item, 'target')"
                        @touchend="endTouch"
                      >
                        {{ formatCurrency(item.target) }}
                      </q-col>
                      <q-col v-else-if="!isMobile" cols="2">
                        <Currency-Input
                          v-model.number="inlineEdit.value"
                          dense
                          @keydown.enter="saveInlineEdit"
                          @keydown.esc="cancelInlineEdit"
                          @blur="saveInlineEdit"
                        />
                      </q-col>
                      <q-col :cols="isMobile ? 'auto' : '2'" :class="item.remaining && item.remaining < 0 ? 'text-negative' : ''">
                        {{ formatCurrency(item.remaining) }}
                      </q-col>
                    </q-row>
                    <q-row no-gutters>
                      <q-col cols="12">
                        <q-linear-progress :value="item.percentage / 100" color="primary" track-color="grey-3" class="progress-bar" />
                      </q-col>
                    </q-row>
                  </div>
                </q-card-section>
              </q-card>
            </q-col>
            <q-col cols="12" v-if="groups.length === 0">
              <q-banner dense class="bg-warning text-black"> No categories defined for this budget. </q-banner>
            </q-col>
          </q-row>
        </q-col>

        <!-- Transaction List Sidebar -->
        <q-col v-if="selectedCategory && !isEditing" :cols="isMobile ? 12 : 4" class="sidebar" :class="{ 'sidebar-mobile': isMobile }">
          <category-transactions
            :category="selectedCategory"
            :transactions="budget.transactions"
            :target="selectedCategory.target || 0"
            :budget-id="budgetId"
            :user-id="userId"
            :category-options="categoryOptions"
            @close="selectedCategory = null"
            @add-transaction="addTransactionForCategory(selectedCategory.name)"
            @update-transactions="updateTransactions"
          />
        </q-col>
      </q-row>

      <!-- Version Info -->
      <q-row>
        <q-space />
        <q-col cols="auto">
          <div class="text-caption text-center">
            {{ `Version: ${appVersion}` }}
          </div>
        </q-col>
      </q-row>

      <!-- Transaction Dialog -->
      <q-dialog v-model="showTransactionDialog" :maximized="isMobile" persistent>
        <q-card :style="{ width: isMobile ? '100%' : '600px', maxWidth: '600px' }">
          <q-card-section class="bg-primary text-white">
            <div class="text-h6">{{ isIncomeTransaction ? 'Add Income' : 'Add Transaction' }}</div>
          </q-card-section>
          <q-card-section>
            <transaction-form
              :initial-transaction="newTransaction"
              :show-cancel="true"
              :category-options="categoryOptions"
              :budget-id="budgetId"
              :user-id="userId"
              @save="onTransactionSaved"
              @cancel="showTransactionDialog = false"
              @update-transactions="updateTransactions"
            />
          </q-card-section>
        </q-card>
      </q-dialog>

      <!-- Snackbar -->
      <q-notification v-model="snackbar" :color="snackbarColor" position="top" :timeout="timeout">
        {{ snackbarText }}
        <template v-slot:actions>
          <q-btn v-if="showRetry && retryAction" flat label="Retry" @click="retryAction" />
          <q-btn flat label="Close" @click="snackbar = false" />
        </template>
      </q-notification>

      <!-- Duplicating Overlay -->
      <q-inner-loading :showing="duplicating">
        <q-spinner color="primary" size="50px" />
        <span class="q-ml-sm">Duplicating budget...</span>
      </q-inner-loading>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick, onUnmounted } from 'vue';
import { auth } from '../firebase';
import { dataAccess } from '../dataAccess';
import CurrencyInput from '../components/CurrencyInput.vue';
import CategoryTransactions from '../components/CategoryTransactions.vue';
import TransactionForm from '../components/TransactionForm.vue';
import type { Transaction, Budget, IncomeTarget, BudgetCategoryTrx, BudgetCategory } from '../types';
import version from '../version';
import { toDollars, toCents, formatCurrency, adjustTransactionDate, todayISO, currentMonthISO } from '../utils/helpers';
import { useBudgetStore } from '../store/budget';
import { useMerchantStore } from '../store/merchants';
import { useFamilyStore } from '../store/family';
import { debounce } from 'lodash';
import { v4 as uuidv4 } from 'uuid';
import { DEFAULT_BUDGET_TEMPLATES } from '../constants/budgetTemplates';
import { useQuasar } from 'quasar';
import type { QMenu } from 'quasar';

const $q = useQuasar();
const budgetStore = useBudgetStore();
const merchantStore = useMerchantStore();
const familyStore = useFamilyStore();
const budgets = ref<Budget[]>([]);

const appVersion = version;

const currentMonth = ref(currentMonthISO());
const isInitialLoad = ref(true);
const availableBudgets = ref<Budget[]>([]);
const budget = ref<Budget>({
  familyId: '',
  month: currentMonthISO(),
  incomeTarget: 0,
  categories: [],
  transactions: [],
  label: '',
  merchants: [],
});
const categoryOptions = ref<string[]>(['Income']);
const saving = ref(false);
const isEditing = ref(false);
const showTransactionDialog = ref(false);
const isIncomeTransaction = ref(false);
const loading = ref(true);
const showLoadingMessage = ref(false);
let loadingTimeout: ReturnType<typeof setTimeout> | null = null;
const newTransaction = ref<Transaction>({
  id: '',
  date: todayISO(),
  budgetMonth: currentMonthISO(),
  merchant: '',
  categories: [{ category: '', amount: 0 }],
  amount: 0,
  notes: '',
  recurring: false,
  recurringInterval: 'Monthly',
  userId: '',
  isIncome: false,
  taxMetadata: [],
});
const snackbar = ref(false);
const snackbarText = ref('');
const snackbarColor = ref('positive');
const showRetry = ref(false);
const retryAction = ref<((evt: Event) => void) | null>(null);
const timeout = ref(-1);
const ownerUid = ref<string | null>(null);
const budgetId = computed(() => {
  if (!ownerUid.value || !familyStore.selectedEntityId) return '';
  return `${ownerUid.value}_${familyStore.selectedEntityId}_${currentMonth.value}`;
});
const userId = computed(() => auth.currentUser?.uid || '');
const selectedCategory = ref<BudgetCategory | null>(null);
const newMerchantName = ref('');
const search = ref('');
const debouncedSearch = ref('');
const monthOffset = ref(0);
const duplicating = ref(false);
const menuOpen = ref(false);

let clickTimeout: ReturnType<typeof setTimeout> | null = null;
let touchTimeout: ReturnType<typeof setTimeout> | null = null;

const inlineEdit = ref<{
  item: BudgetCategoryTrx | null;
  field: 'name' | 'target' | null;
  value: string | number;
}>({
  item: null,
  field: null,
  value: '',
});

const isMobile = computed(() => $q.screen.lt.md);

const entityOptions = computed(() => {
  const options = (familyStore.family?.entities || []).map((entity) => ({
    id: entity.id,
    name: entity.name,
  }));
  return [{ id: '', name: 'All Entities' }, ...options];
});

const selectedEntity = computed(() => {
  return familyStore.family?.entities?.find((e) => e.id === familyStore.selectedEntityId);
});

const budgetedExpenses = computed(() => {
  const totalPlanned = budget.value.categories
    .filter((cat) => cat.name !== 'Income' && cat.group !== 'Income')
    .reduce((sum, cat) => sum + (cat.target || 0), 0);
  return totalPlanned;
});

const remainingToBudget = computed(() => {
  return actualIncome.value - budgetedExpenses.value;
});

const formatMonth = (month: string) => {
  const [year, monthNum] = month.split('-');
  const date = new Date(parseInt(year), parseInt(monthNum) - 1);
  return date.toLocaleString('en-US', { month: 'long', year: 'numeric' });
};

const displayedMonths = computed(() => {
  const months = [];
  const today = new Date();
  const startDate = new Date(today.getFullYear(), today.getMonth() + monthOffset.value, 1);

  for (let i = -4; i <= 7; i++) {
    const date = new Date(startDate.getFullYear(), startDate.getMonth() + i, 1);
    const year = date.getFullYear();
    const monthNum = date.getMonth();
    const label = date.toLocaleString('en-US', {
      month: 'short',
      year: 'numeric',
    });
    const value = `${year}-${(monthNum + 1).toString().padStart(2, '0')}`;
    months.push({ label, value });
  }
  return months;
});

const displayYear = computed(() => {
  const today = new Date();
  const startDate = new Date(today.getFullYear(), today.getMonth() + monthOffset.value, 1);
  return startDate.getFullYear();
});

const catTransactions = computed(() => {
  const catTransactions: BudgetCategoryTrx[] = [];
  if (budget.value && budget.value.categories) {
    budget.value.categories.forEach((c) => {
      if (c.group !== 'Income') {
        catTransactions.push({
          ...c,
          spent: 0,
          remaining: (c.carryover ?? 0) + c.target,
          percentage: 0,
        });
      }
    });
  }

  for (let i = 0; i < catTransactions.length; i++) {
    const carryover = catTransactions[i].carryover || 0;
    const target = catTransactions[i].target || 0;
    const totalTarget = target + (catTransactions[i].isFund ? carryover : 0);
    budget.value.transactions.forEach((t) => {
      if (!t.deleted) {
        t.categories.forEach((tc) => {
          if (tc.category == catTransactions[i].name) {
            if (!catTransactions[i].transactions) catTransactions[i].transactions = [];

            catTransactions[i].transactions?.push({
              id: t.id,
              date: t.date,
              merchant: t.merchant,
              category: tc.category,
              isSplit: t.categories.length > 1,
              amount: tc.amount,
              isIncome: t.isIncome,
            });
            if (t.isIncome) {
              catTransactions[i].spent -= tc.amount;
              catTransactions[i].remaining += tc.amount;
            } else {
              catTransactions[i].spent += tc.amount;
              catTransactions[i].remaining -= tc.amount;
            }
          }
        });
      }
    });
    const rawPercentage = totalTarget > 0 ? (catTransactions[i].spent / totalTarget) * 100 : 0;
    catTransactions[i].percentage = Math.min(Math.max(rawPercentage, 0), 100);
  }

  if (debouncedSearch.value !== '') {
    const srch = debouncedSearch.value.toLowerCase();
    return catTransactions.filter((t) => t.group.toLowerCase().includes(srch) || t.name.toLowerCase().includes(srch));
  }
  return catTransactions;
});

const groups = computed(() => {
  const g: GroupCategory[] = [];
  catTransactions.value.forEach((c) => {
    if (g.length == 0 || g.filter((f) => f.group == c.group).length == 0) {
      g.push({ group: c.group, cat: [] });
    }

    for (let j = 0; j < g.length; j++) {
      if (g[j].group == c.group) {
        g[j].cat.push(c.name);
      }
    }
  });
  return g;
});

const incomeItems = computed(() => {
  const incTrx: IncomeTarget[] = [];
  if (budget.value && budget.value.categories) {
    budget.value.categories.forEach((c) => {
      if (c.group.toLowerCase() == 'income') {
        incTrx.push({
          name: c.name,
          group: c.group,
          planned: c.target,
          received: 0,
        });
      }
    });
  }

  for (let i = 0; i < incTrx.length; i++) {
    budget.value.transactions.forEach((t) => {
      if (!t.deleted && t.categories && t.categories.length > 0) {
        t.categories.forEach((c) => {
          if (incTrx[i].name == c.category) {
            incTrx[i].received += c.amount;
          }
        });
      }
    });
  }
  return incTrx;
});

const actualIncome = computed(() => {
  return incomeItems.value.reduce((sum, t) => sum + (t.received || 0), 0);
});

const plannedIncome = computed(() => {
  return incomeItems.value.reduce((sum, t) => sum + (t.planned || 0), 0);
});

function monthExists(month: string) {
  return availableBudgets.value.filter((b) => b.month == month && b.entityId === familyStore.selectedEntityId).length > 0;
}

function onIncomeRowClick(item: IncomeTarget) {
  const t = getCategoryInfo(item.name);
  selectedCategory.value = t;
}

function onCategoryRowClick(item: BudgetCategoryTrx) {
  selectedCategory.value = getCategoryInfo(item.name);
}

function getCategoryInfo(name: string): BudgetCategory {
  return budget.value.categories.filter((c) => c.name == name)[0];
}

const updateSearch = debounce((value: string) => {
  debouncedSearch.value = value;
}, 300);

watch(search, (newValue) => {
  updateSearch(newValue);
});

watch(selectedCategory, async (newVal) => {
  if (newVal && isMobile.value) {
    await nextTick(() => {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    });
  }
});

watch(
  () => familyStore.selectedEntityId,
  async () => {
    await loadBudgets();
  },
);

const updateBudgetForMonth = debounce(async () => {
  if (!familyStore.selectedEntityId) {
    budget.value = {
      familyId: '',
      entityId: '',
      month: currentMonth.value,
      incomeTarget: 0,
      categories: [],
      transactions: [],
      label: '',
      merchants: [],
    };
    return;
  }

  const defaultBudget = budgets.value.find((b) => b.month === currentMonth.value && b.entityId === familyStore.selectedEntityId);
  if (defaultBudget) {
    const family = await familyStore.getFamily();
    if (family) {
      ownerUid.value = family.ownerUid;
    } else {
      console.error('No family found for user');
      ownerUid.value = userId.value;
    }

    budget.value = { ...defaultBudget, budgetId: budgetId.value };
    categoryOptions.value = defaultBudget.categories.map((cat) => cat.name);
    if (!categoryOptions.value.includes('Income')) {
      categoryOptions.value.push('Income');
    }
  } else if (isInitialLoad.value && budgets.value.length > 0) {
    const sortedBudgets = budgets.value
      .filter((b) => b.entityId === familyStore.selectedEntityId)
      .sort((a, b) => {
        const dateA = new Date(a.month);
        const dateB = new Date(b.month);
        return dateB.getTime() - dateA.getTime();
      });
    const mostRecentBudget = sortedBudgets[0];
    if (mostRecentBudget) {
      currentMonth.value = mostRecentBudget.month;
      budget.value = { ...mostRecentBudget, budgetId: budgetId.value };

      const family = await familyStore.getFamily();
      if (family) {
        ownerUid.value = family.ownerUid;
      } else {
        console.error('No family found for user');
        ownerUid.value = userId.value;
      }

      categoryOptions.value = mostRecentBudget.categories.map((cat) => cat.name);
      if (!categoryOptions.value.includes('Income')) {
        categoryOptions.value.push('Income');
      }
    }
  }
}, 300);

watch(
  () => budgetStore.budgets,
  (newBudgets) => {
    budgets.value = Array.from(newBudgets.values());
    availableBudgets.value = budgets.value;
    if (budgets.value.length > 0) {
      updateBudgetForMonth();
    }
  },
  { deep: true },
);

watch(currentMonth, () => {
  updateBudgetForMonth();
});

onMounted(async () => {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar('Please log in to view the dashboard', 'negative');
    loading.value = false;
    return;
  }

  try {
    loadingTimeout = setTimeout(() => {
      showLoadingMessage.value = true;
      console.log('Loading timeout triggered');
    }, 5000);

    await familyStore.loadFamily(user.uid);
    await loadBudgets();
  } catch (error: unknown) {
    showSnackbar(`Error loading data: ${(error as Error).message}`, 'negative');
  } finally {
    if (loadingTimeout) clearTimeout(loadingTimeout);
    showLoadingMessage.value = false;
    loading.value = false;
    isInitialLoad.value = false;
  }
});

onUnmounted(() => {
  budgetStore.unsubscribeAll();
  if (loadingTimeout) clearTimeout(loadingTimeout);
});

async function loadBudgets() {
  const user = auth.currentUser;
  if (!user) return;

  loading.value = true;
  try {
    await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);
  } catch (error: unknown) {
    showSnackbar(`Error loading budgets: ${(error as Error).message}`, 'negative');
  } finally {
    loading.value = false;
  }
}

async function createBudgetForMonth(month: string, familyId: string, ownerUid: string, entityId: string): Promise<Budget> {
  const budgetId = `${ownerUid}_${entityId}_${month}`;
  const existingBudget = await dataAccess.getBudget(budgetId);
  if (existingBudget) {
    return existingBudget;
  }

  const entity = familyStore.family?.entities?.find((e) => e.id === entityId);
  const templateBudget = entity?.templateBudget;

  if (templateBudget && templateBudget.categories.length > 0) {
    const newBudget: Budget = {
      familyId: familyId,
      entityId: entityId,
      month: month,
      incomeTarget: 0,
      categories: templateBudget.categories.map((cat) => ({
        ...cat,
        carryover: cat.isFund ? 0 : 0,
      })),
      transactions: [],
      label: `Template Budget for ${month}`,
      merchants: [],
      budgetId: budgetId,
    };

    await dataAccess.saveBudget(budgetId, newBudget);
    budgetStore.updateBudget(budgetId, newBudget);
    budgets.value.push(newBudget);
    return newBudget;
  }

  if (entity && DEFAULT_BUDGET_TEMPLATES[entity.type]) {
    const predefinedTemplate = DEFAULT_BUDGET_TEMPLATES[entity.type];
    const newBudget: Budget = {
      familyId: familyId,
      entityId: entityId,
      month: month,
      incomeTarget: 0,
      categories: predefinedTemplate.categories.map((cat) => ({
        ...cat,
        carryover: cat.isFund ? 0 : 0,
      })),
      transactions: [],
      label: `Default ${entity.type} Budget for ${month}`,
      merchants: [],
      budgetId: budgetId,
    };

    await dataAccess.saveBudget(budgetId, newBudget);
    budgetStore.updateBudget(budgetId, newBudget);
    budgets.value.push(newBudget);
    return newBudget;
  }

  const availableBudgets = Array.from(budgetStore.budgets.values()).sort((a, b) => a.month.localeCompare(b.month));
  let sourceBudget: Budget | undefined;

  const previousBudgets = availableBudgets.filter((b) => b.month < month && b.entityId === entityId);
  if (previousBudgets.length > 0) {
    sourceBudget = previousBudgets[previousBudgets.length - 1];
  } else {
    const futureBudgets = availableBudgets.filter((b) => b.month > month && b.entityId === entityId);
    if (futureBudgets.length > 0) {
      sourceBudget = futureBudgets[0];
    }
  }

  if (sourceBudget) {
    const [newYear, newMonthNum] = month.split('-').map(Number);
    const [sourceYear, sourceMonthNum] = sourceBudget.month.split('-').map(Number);
    const isFutureMonth = newYear > sourceYear || (newYear === sourceYear && newMonthNum > sourceMonthNum);

    let newCarryover: Record<string, number> = {};
    if (isFutureMonth) {
      newCarryover = dataAccess.calculateCarryOver(sourceBudget);
    }

    const newBudget: Budget = {
      familyId: familyId,
      entityId: entityId,
      month: month,
      incomeTarget: sourceBudget.incomeTarget,
      categories: sourceBudget.categories.map((cat) => ({
        ...cat,
        carryover: cat.isFund ? newCarryover[cat.name] || 0 : 0,
      })),
      label: sourceBudget.label || `Budget for ${month}`,
      merchants: sourceBudget.merchants || [],
      transactions: [],
      budgetId: budgetId,
    };

    const recurringTransactions: Transaction[] = [];
    if (sourceBudget.transactions) {
      const recurringGroups = sourceBudget.transactions.reduce(
        (groups, trx) => {
          if (!trx.deleted && trx.recurring) {
            const key = `${trx.merchant}-${trx.amount}-${trx.recurringInterval}-${trx.userId}-${trx.isIncome}`;
            if (!groups[key]) {
              groups[key] = [];
            }
            groups[key].push(trx);
          }
          return groups;
        },
        {} as Record<string, Transaction[]>,
      );

      Object.values(recurringGroups).forEach((group) => {
        const firstInstance = group.sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())[0];
        if (firstInstance.recurringInterval === 'Monthly') {
          const newDate = adjustTransactionDate(firstInstance.date, month, 'Monthly');
          recurringTransactions.push({
            ...firstInstance,
            id: uuidv4(),
            date: newDate,
            budgetMonth: month,
            entityId: entityId,
          });
        }
      });
    }

    newBudget.transactions = recurringTransactions;
    await dataAccess.saveBudget(budgetId, newBudget);
    budgetStore.updateBudget(budgetId, newBudget);
    budgets.value.push(newBudget);
    return newBudget;
  }

  const defaultBudget: Budget = {
    familyId: familyId,
    entityId: entityId,
    month: month,
    incomeTarget: 0,
    categories: [
      { name: 'Income', target: 0, isFund: false, group: 'Income' },
      { name: 'Miscellaneous', target: 0, isFund: false, group: 'Expenses' },
    ],
    transactions: [],
    label: `Default Budget for ${month}`,
    merchants: [],
    budgetId: budgetId,
  };
  await dataAccess.saveBudget(budgetId, defaultBudget);
  budgetStore.updateBudget(budgetId, defaultBudget);
  return defaultBudget;
}

async function createDefaultBudget() {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar('Please log in to create a budget', 'negative');
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before creating a budget', 'negative');
    return;
  }

  loading.value = true;
  try {
    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar('No family found for user', 'negative');
      return;
    }

    await createBudgetForMonth(currentMonth.value, family.id, family.ownerUid, familyStore.selectedEntityId);
    await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);
    showSnackbar('Budget created successfully', 'positive');
  } catch (error: unknown) {
    console.error('Error creating budget:', error);
    showSnackbar(`Failed to create budget: ${(error as Error).message}`, 'negative');
  } finally {
    loading.value = false;
  }
}

function updateMerchants() {
  merchantStore.updateMerchants(budget.value.transactions);
}

async function onTransactionSaved(transaction: Transaction) {
  showTransactionDialog.value = false;
  try {
    budget.value.transactions = budget.value.transactions ? budget.value.transactions.filter((tx) => tx.id !== transaction.id) : [];
    budget.value.transactions.push(transaction);
    budgetStore.updateBudget(budgetId.value, { ...budget.value });

    updateMerchants();
    showSnackbar(isIncomeTransaction.value ? 'Income added successfully' : 'Transaction added successfully');
  } catch (error: unknown) {
    showSnackbar(`Error updating transaction: ${(error as Error).message}`, 'negative');
  }
}

async function updateTransactions(newTransactions: Transaction[]) {
  try {
    budget.value.transactions = newTransactions;
    budgetStore.updateBudget(budgetId.value, { ...budget.value });
    updateMerchants();
  } catch (error: unknown) {
    showSnackbar(`Error updating transactions: ${(error as Error).message}`, 'negative');
  }
}

function shiftMonths(offset: number) {
  monthOffset.value += offset;
}

async function selectMonth(month: string) {
  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before selecting a month', 'negative');
    return;
  }

  if (!monthExists(month)) {
    await duplicateCurrentMonth(month);
  }
  selectedCategory.value = null;
  currentMonth.value = month;
  monthOffset.value = 0;
  menuOpen.value = false;

  loading.value = true;
  try {
    const family = await familyStore.getFamily();
    const ownerId = family ? family.ownerUid : userId.value;
    const newBudgetId = `${ownerId}_${familyStore.selectedEntityId}_${month}`;
    const freshBudget = await dataAccess.getBudget(newBudgetId);
    if (freshBudget) {
      budget.value = { ...freshBudget, budgetId: newBudgetId };
      budgetStore.updateBudget(newBudgetId, freshBudget);
      categoryOptions.value = freshBudget.categories.map((cat) => cat.name);
      if (!categoryOptions.value.includes('Income')) {
        categoryOptions.value.push('Income');
      }
    } else {
      const b = await createBudgetForMonth(month, family!.id, ownerId, familyStore.selectedEntityId);
      budget.value = { ...b, budgetId: newBudgetId };
      budgetStore.updateBudget(newBudgetId, b);
      categoryOptions.value = b.categories.map((cat) => cat.name);
      if (!categoryOptions.value.includes('Income')) {
        categoryOptions.value.push('Income');
      }
    }
  } catch (error: unknown) {
    showSnackbar(`Error loading budget: ${(error as Error).message}`, 'negative');
  } finally {
    loading.value = false;
  }
}

async function saveBudget() {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar('You don’t have permission to save budgets', 'negative');
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before saving the budget', 'negative');
    return;
  }

  saving.value = true;
  try {
    budget.value.entityId = familyStore.selectedEntityId;
    budget.value.budgetId = budgetId.value;
    await dataAccess.saveBudget(budgetId.value, budget.value);
    showSnackbar('Budget saved successfully');
    isEditing.value = false;
  } catch (error: unknown) {
    showSnackbar(`Error saving budget: ${(error as Error).message}`, 'negative', () => {
      saveBudget();
    });
  } finally {
    saving.value = false;
  }
}

function addCategory() {
  budget.value.categories.push({
    name: '',
    target: 0,
    isFund: false,
    group: '',
  });
}

function addIncomeCategory() {
  budget.value.categories.push({
    name: 'Income',
    target: 0,
    isFund: false,
    group: 'Income',
  });
  showSnackbar('Added new income category');
}

function removeCategory(index: number) {
  budget.value.categories.splice(index, 1);
}

function addMerchant() {
  const merchantName = newMerchantName.value.trim();
  if (merchantName === '') return;

  const existingMerchant = budget.value.merchants.find((m) => m.name.toLowerCase() === merchantName.toLowerCase());

  if (existingMerchant) {
    showSnackbar('Merchant already exists', 'warning');
  } else {
    budget.value.merchants.push({
      name: merchantName,
      usageCount: 0,
    });
    showSnackbar(`Added merchant: ${merchantName}`);
  }

  newMerchantName.value = '';
}

function removeMerchant(index: number) {
  const merchantName = budget.value.merchants[index].name;
  budget.value.merchants.splice(index, 1);
  showSnackbar(`Removed merchant: ${merchantName}`);
}

function addTransaction() {
  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before adding a transaction', 'negative');
    return;
  }

  if (!showTransactionDialog.value) {
    newTransaction.value = {
      id: uuidv4(),
      date: todayISO(),
      budgetMonth: currentMonth.value,
      merchant: '',
      categories: [{ category: '', amount: 0 }],
      amount: 0,
      notes: '',
      recurring: false,
      recurringInterval: 'Monthly',
      userId: userId.value,
      isIncome: false,
      taxMetadata: [],
      entityId: familyStore.selectedEntityId,
    };
    isIncomeTransaction.value = false;
    showTransactionDialog.value = true;
  }
}

function addTransactionForCategory(category: string) {
  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before adding a transaction', 'negative');
    return;
  }

  if (!showTransactionDialog.value) {
    newTransaction.value = {
      id: uuidv4(),
      date: todayISO(),
      budgetMonth: currentMonth.value,
      merchant: '',
      categories: [{ category: category, amount: 0 }],
      amount: 0,
      notes: '',
      recurring: false,
      recurringInterval: 'Monthly',
      userId: userId.value,
      isIncome: false,
      taxMetadata: [],
      entityId: familyStore.selectedEntityId,
    };
    isIncomeTransaction.value = false;
    showTransactionDialog.value = true;
  }
}

async function duplicateCurrentMonth(month: string) {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar('Please log in to duplicate a budget', 'negative');
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before duplicating a budget', 'negative');
    return;
  }

  duplicating.value = true;
  try {
    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar('No family found for user', 'negative');
      return;
    }

    const newBudgetId = `${family.ownerUid}_${familyStore.selectedEntityId}_${month}`;
    const existingBudget = await dataAccess.getBudget(newBudgetId);
    if (existingBudget) {
      showSnackbar('A budget already exists for this month', 'warning');
      return;
    }

    await createBudgetForMonth(month, family.id, family.ownerUid, familyStore.selectedEntityId);
    await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);

    showSnackbar("Created new month's budget");
  } catch (error: unknown) {
    console.error('Error duplicating budget:', error);
    showSnackbar(`Failed to duplicate budget: ${(error as Error).message}`, 'negative');
  } finally {
    duplicating.value = false;
  }
}

function handleRowClick(item: BudgetCategoryTrx) {
  if (clickTimeout) clearTimeout(clickTimeout);
  clickTimeout = setTimeout(() => {
    onCategoryRowClick(item);
    clickTimeout = null;
  }, 250);
}

function handleNameDblClick(item: BudgetCategoryTrx) {
  if (clickTimeout) {
    clearTimeout(clickTimeout);
    clickTimeout = null;
  }
  startInlineEdit(item, 'name');
}

function handleTargetDblClick(item: BudgetCategoryTrx) {
  if (clickTimeout) {
    clearTimeout(clickTimeout);
    clickTimeout = null;
  }
  startInlineEdit(item, 'target');
}

function startTouch(item: BudgetCategoryTrx, field: 'name' | 'target') {
  touchTimeout = setTimeout(() => {
    if (clickTimeout) {
      clearTimeout(clickTimeout);
      clickTimeout = null;
    }
    startInlineEdit(item, field);
  }, 500);
}

function endTouch() {
  if (touchTimeout) {
    clearTimeout(touchTimeout);
    touchTimeout = null;
  }
}

function startInlineEdit(item: BudgetCategoryTrx, field: 'name' | 'target') {
  inlineEdit.value.item = item;
  inlineEdit.value.field = field;
  inlineEdit.value.value = field === 'name' ? item.name : item.target;
}

async function saveInlineEdit() {
  if (!inlineEdit.value.item || !inlineEdit.value.field) {
    cancelInlineEdit();
    return;
  }

  const item = inlineEdit.value.item;
  const field = inlineEdit.value.field;
  const idx = budget.value.categories.findIndex((c) => c.name === item.name);
  if (idx === -1) {
    cancelInlineEdit();
    return;
  }

  if (field === 'name') {
    const oldName = budget.value.categories[idx].name;
    const newName = String(inlineEdit.value.value).trim();
    if (newName === '' || newName === oldName) {
      cancelInlineEdit();
      return;
    }
    budget.value.categories[idx].name = newName;
    item.name = newName;
    budget.value.transactions.forEach((t) => {
      t.categories.forEach((c) => {
        if (c.category === oldName) c.category = newName;
      });
    });
    const optIdx = categoryOptions.value.indexOf(oldName);
    if (optIdx !== -1) categoryOptions.value.splice(optIdx, 1, newName);
    if (selectedCategory.value && selectedCategory.value.name === oldName) {
      selectedCategory.value.name = newName;
    }
  } else {
    const amount = typeof inlineEdit.value.value === 'number' ? inlineEdit.value.value : parseFloat(String(inlineEdit.value.value));
    budget.value.categories[idx].target = isNaN(amount) ? 0 : amount;
    item.target = isNaN(amount) ? 0 : amount;
  }

  try {
    budget.value.budgetId = budgetId.value;
    await dataAccess.saveBudget(budgetId.value, budget.value);
    budgetStore.updateBudget(budgetId.value, { ...budget.value });
    showSnackbar('Budget updated');
  } catch (error: unknown) {
    showSnackbar(`Error saving budget: ${(error as Error).message}`, 'negative');
  }

  cancelInlineEdit();
}

function cancelInlineEdit() {
  inlineEdit.value.item = null;
  inlineEdit.value.field = null;
}

function showSnackbar(text: string, color = 'positive', retry?: () => void) {
  snackbarText.value = text;
  snackbarColor.value = color;
  showRetry.value = !!retry;
  retryAction.value = retry ? (evt: Event) => retry() : null;
  timeout.value = retry ? -1 : 3000;
  snackbar.value = true;
}

interface GroupCategory {
  group: string;
  cat: string[];
}
</script>

<style scoped>
.sidebar {
  background-color: #f5f5f5;
  height: calc(100vh - 64px);
  position: sticky;
  top: 64px;
  overflow-y: auto;
}

@media (max-width: 600px) {
  .q-card {
    margin-bottom: 8px;
  }
}

h1 {
  font-size: 1.3em;
  white-space: nowrap;
}

.entity-select .q-field__control {
  font-size: 1.6rem;
  font-weight: bold;
  color: var(--q-primary);
}

.month-selector {
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  font-size: 1.5rem;
  font-weight: bold;
}

.month-menu {
  width: 300px;
  padding: 8px;
}

.month-navigation {
  padding: 8px 0;
  border-bottom: 1px solid #e0e0e0;
}

.month-item {
  padding: 4px;
}

.q-btn {
  text-transform: none;
  font-size: 0.875rem;
}
</style>
