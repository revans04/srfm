<template>
  <q-page fluid :class="isMobile ? 'q-pt-none q-px-none' : ''">
    <!-- Loading Animation -->
    <div v-if="loading" class="row justify-center q-mt-lg">
      <q-spinner color="primary" size="50px" />
      <div v-if="showLoadingMessage" class="col-12 text-center q-mt-sm">
        <span>Still loading budgets, please wait...</span>
      </div>
    </div>

    <!-- No Budgets Found -->
    <div v-else-if="!loading && budgets.length === 0" class="row justify-center q-mt-lg">
      <div class="col-12 text-center">
        <q-card flat bordered class="q-pa-md">
          <q-card-section>
            <div class="row items-center">
              <div class="col">
                No budgets found for {{ selectedEntity?.name || 'selected entity' }}. Would you like to create a default budget for
                {{ formatMonth(currentMonth) }}? You can also import budget information from the Data Page.
              </div>
              <div class="col-auto">
                <q-btn color="primary" @click="createDefaultBudget">Create Default Budget</q-btn>
              </div>
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>

    <!-- Main Content -->
    <div v-else>
      <div class="row" :class="isMobile ? 'q-pa-sm' : 'q-pr-xl'" style="overflow: visible; min-height: 60px">
        <div class="col-12">
          <!-- Entity Dropdown -->
          <EntitySelector @change="loadBudgets" class="q-mb-sm" />
          <h4>
            {{ formatMonth(currentMonth) }}
            <span class="month-selector no-wrap" :class="{ 'text-white': isMobile }" @click="menuOpen = true">
              <q-icon size="xs" name="expand_more" />
              <q-menu v-model="menuOpen" :offset="[0, 4]" :close-on-content-click="false">
                <q-card class="month-menu">
                  <div class="row no-wrap items-center q-px-sm q-py-xs border-bottom">
                    <div class="col-auto">
                      <q-btn flat dense icon="chevron_left" @click.stop="shiftMonths(-6)" />
                    </div>
                    <div class="col text-center">
                      <span>{{ displayYear }}</span>
                    </div>
                    <div class="col-auto">
                      <q-btn flat dense icon="chevron_right" @click.stop="shiftMonths(6)" />
                    </div>
                  </div>
                  <div class="row">
                    <div v-for="month in displayedMonths" :key="month.value" class="col-4">
                      <div
                        class="q-pa-xs q-ma-xs text-center cursor-pointer border rounded"
                        :class="month.value === currentMonth ? 'bg-primary text-white' : 'text-primary'"
                        :style="monthExists(month.value) ? 'border-style: solid' : 'border-style: dashed'"
                        @click="selectMonth(month.value)"
                      >
                        {{ month.label }}
                      </div>
                    </div>
                  </div>
                </q-card>
              </q-menu>
            </span>
            <q-btn v-if="!isMobile && !isEditing" flat icon="edit" @click="isEditing = true" title="Edit Budget" />
            <q-btn v-if="isEditing" flat icon="close" @click="isEditing = false" title="Cancel" />
            <q-btn v-if="!isMobile && !isEditing" flat icon="delete" color="negative" title="Delete Budget" />
          </h4>
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
        </div>
        <!-- Dashboard Tiles -->
        <div class="col-12">
          <DashboardTiles :budget-id="budgetId" :family-id="familyId" @open-bills="onOpenBills" @create-goal="onCreateGoal" />
        </div>
        <div v-if="!isMobile || !selectedCategory" class="col-12 sm:col-6">
          <div class="q-my-sm bg-white q-pa-md rounded-borders">
            <q-input
              dense
              label="Search"
              v-model="search"
              clearable
              ref="searchInput"
              append-icon="search"
              @keyup.enter="blurSearchInput"
              @clear="clearSearch"
            />
          </div>
        </div>
        <q-fab v-if="isMobile && !selectedCategory" icon="add" color="primary" direction="up" class="absolute-top-right q-ma-sm" @click="addTransaction" />
      </div>

      <div class="row">
        <!-- Main Content -->
        <div :class="selectedCategory ? (isMobile ? 'col-0 d-none' : 'col-8') : 'col-12'">
          <!-- Budget Editing Form -->
          <q-card v-if="isEditing" flat bordered>
            <q-card-section>Edit Budget for {{ selectedEntity?.name || 'selected entity' }}</q-card-section>
            <q-card-section>
              <q-form @submit.prevent="saveBudget">
                <!-- Merchants Section -->
                <div class="row q-mt-lg">
                  <div class="col-12">
                    <h3 class="text-h6">Merchants</h3>
                    <q-chip v-for="(merchant, index) in budget.merchants" :key="merchant.name" removable @remove="removeMerchant(index)" class="q-ma-xs">
                      {{ merchant.name }} ({{ merchant.usageCount }})
                    </q-chip>
                    <q-input
                      v-model="newMerchantName"
                      label="Add Merchant"
                      dense
                      class="q-mt-sm"
                      @keyup.enter="addMerchant"
                      append-icon="add"
                      @click:append="addMerchant"
                    />
                  </div>
                </div>

                <!-- Categories Section -->
                <div class="row q-mt-lg">
                  <div class="col-12">
                    <h3 class="text-h6">Categories</h3>
                  </div>
                </div>
                <div v-for="(cat, index) in budget.categories" :key="index" class="row items-center no-wrap q-mb-sm">
                  <div class="col-12 sm:col-3 q-pa-xs">
                    <q-input v-model="cat.name" label="Category" required dense />
                  </div>
                  <div class="col-12 sm:col-2 q-pa-xs">
                    <q-input v-model="cat.group" label="Group (e.g., Utilities)" dense />
                  </div>
                  <div class="col-12 sm:col-2 q-pa-xs">
                    <CurrencyInput v-model.number="cat.target" label="Target" class="text-right" dense required />
                  </div>
                  <div class="col-12 sm:col-2 q-pa-xs">
                    <CurrencyInput v-model="cat.carryover" label="Carryover" class="text-right" dense />
                  </div>
                  <div class="col-12 sm:col-2 q-pa-xs">
                    <q-checkbox v-model="cat.isFund" label="Is Fund?" dense />
                  </div>
                  <div class="col-12 sm:col-1 q-pa-xs">
                    <q-btn flat icon="close" color="negative" @click="removeCategory(index)" />
                  </div>
                </div>

                <q-btn flat color="primary" @click="addCategory" class="q-mt-sm">Add Category</q-btn>
                <q-btn flat color="positive" @click="addIncomeCategory" class="q-mt-sm q-ml-sm">Add Income Category</q-btn>
                <q-btn type="submit" color="positive" class="q-mt-sm q-ml-sm" :loading="saving">Save Budget</q-btn>
              </q-form>
            </q-card-section>
          </q-card>

          <!-- Income Section -->
          <q-card v-if="!isEditing && incomeItems" flat bordered class="q-mt-md">
            <q-card-section>
              <div class="row text-bold">
                <div class="col">Income for {{ selectedEntity?.name || 'selected entity' }}</div>
                <div v-if="!isMobile" class="col-auto">Planned</div>
                <div :class="isMobile ? 'col-auto' : 'col-2'" class="text-right">Received</div>
              </div>
              <div v-for="item in incomeItems" :key="item.name" class="q-py-sm border-bottom">
                <div class="row cursor-pointer" @click="onIncomeRowClick(item)">
                  <div class="col">{{ item.name }}</div>
                  <div v-if="!isMobile" class="col-auto">{{ formatCurrency(toDollars(toCents(item.planned))) }}</div>
                  <div :class="isMobile ? 'col-auto' : 'col-2'" class="text-right">
                    <span :class="item.received > item.planned ? 'text-positive' : ''">
                      {{ formatCurrency(toDollars(toCents(item.received))) }}
                    </span>
                  </div>
                </div>
              </div>
              <div v-if="!isMobile" class="row q-mt-sm">
                <div class="col">
                  <q-space />
                </div>
                <div v-if="!isMobile" class="col-auto text-bold">{{ formatCurrency(toDollars(toCents(plannedIncome))) }}</div>
                <div :class="isMobile ? 'col-auto' : 'col-2'" class="text-right">
                  <span class="text-bold" :class="actualIncome > plannedIncome ? 'text-positive' : ''">
                    {{ formatCurrency(toDollars(toCents(actualIncome))) }}
                  </span>
                </div>
              </div>
            </q-card-section>
          </q-card>

          <!-- Category Tables -->
          <div v-if="!isEditing && catTransactions" class="row q-mt-lg">
            <div class="col-12" v-for="(g, gIdx) in groups" :key="gIdx">
              <q-card flat bordered>
                <q-card-section>
                  <div class="row text-primary">
                    <div class="col">{{ g.group || 'Ungrouped' }}</div>
                    <div class="col-auto"><q-space /></div>
                    <div v-if="!isMobile" class="col-2">Planned</div>
                    <div :class="isMobile ? 'col-auto' : 'col-2'">Remaining</div>
                  </div>
                  <div v-for="(item, idx) in catTransactions.filter((c) => c.group == g.group)" :key="idx">
                    <div class="row cursor-pointer" @click="handleRowClick(item)">
                      <div
                        v-if="!(inlineEdit.item?.name === item.name && inlineEdit.field === 'name')"
                        @dblclick.stop="handleNameDblClick(item)"
                        @touchstart="startTouch(item, 'name')"
                        @touchend="endTouch"
                        class="col"
                      >
                        <q-icon v-if="item.isFund" size="xs" class="q-mr-xs" color="primary" name="savings" />
                        {{ item.name }}
                      </div>
                      <div v-else class="col">
                        <q-input
                          v-model="inlineEdit.value"
                          dense
                          autofocus
                          @keydown.enter="saveInlineEdit"
                          @keydown.esc="cancelInlineEdit"
                          @blur="saveInlineEdit"
                        />
                      </div>
                      <div
                        v-if="!isMobile && !(inlineEdit.item?.name === item.name && inlineEdit.field === 'target')"
                        class="col-2"
                        @dblclick.stop="handleTargetDblClick(item)"
                        @touchstart="startTouch(item, 'target')"
                        @touchend="endTouch"
                      >
                        {{ formatCurrency(item.target) }}
                      </div>
                      <div v-else-if="!isMobile" class="col-2">
                        <CurrencyInput
                          v-model="inlineEditNumber"
                          dense
                          @keydown.enter="saveInlineEdit"
                          @keydown.esc="cancelInlineEdit"
                          @blur="saveInlineEdit"
                        />
                      </div>
                      <div :class="[isMobile ? 'col-auto' : 'col-2', { 'text-negative': item.remaining < 0 }]">
                        {{ formatCurrency(item.remaining) }}
                      </div>
                    </div>
                    <div class="row">
                      <div class="col-12">
                        <q-linear-progress :value="item.percentage / 100" color="primary" class="q-my-xs" />
                      </div>
                    </div>
                  </div>
                </q-card-section>
              </q-card>
            </div>
            <div class="col-12" v-if="groups.length === 0">
              <q-card flat bordered>
                <q-card-section>No categories defined for this budget.</q-card-section>
              </q-card>
            </div>
          </div>
        </div>

        <!-- Transaction List Sidebar -->
        <div v-if="selectedCategory && !isEditing" :class="isMobile ? 'col-12' : 'col-4'" class="sidebar">
          <CategoryTransactions
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
        </div>
      </div>

      <!-- Version Info -->
      <div class="row q-mt-md">
        <div class="col-auto">
          <div class="text-caption text-center">{{ `Version: ${appVersion}` }}</div>
        </div>
      </div>

      <!-- Transaction Dialog -->
      <q-dialog v-model="showTransactionDialog" max-width="600px">
        <q-card>
          <q-card-section>{{ isIncomeTransaction ? 'Add Income' : 'Add Transaction' }}</q-card-section>
          <q-card-section>
            <TransactionForm
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
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick, onUnmounted } from 'vue';
import { useQuasar, QSpinner } from 'quasar';
import { dataAccess } from '../dataAccess';
import CurrencyInput from '../components/CurrencyInput.vue';
import CategoryTransactions from '../components/CategoryTransactions.vue';
import TransactionForm from '../components/TransactionForm.vue';
import EntitySelector from '../components/EntitySelector.vue';
import DashboardTiles from '../components/DashboardTiles.vue';
import type { Transaction, Budget, IncomeTarget, BudgetCategoryTrx, BudgetCategory } from '../types';
import version from '../version';
import { toDollars, toCents, formatCurrency, adjustTransactionDate, todayISO, currentMonthISO } from '../utils/helpers';
import { useAuthStore } from '../store/auth';
import { useBudgetStore } from '../store/budget';
import { useMerchantStore } from '../store/merchants';
import { useFamilyStore } from '../store/family';
import debounce from 'lodash/debounce';
import { v4 as uuidv4 } from 'uuid';
import { DEFAULT_BUDGET_TEMPLATES } from '../constants/budgetTemplates';

const $q = useQuasar();
const budgetStore = useBudgetStore();
const merchantStore = useMerchantStore();
const familyStore = useFamilyStore();
const auth = useAuthStore();
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
const ownerUid = ref<string | null>(null);
const budgetId = computed(() => {
  if (!ownerUid.value || !familyStore.selectedEntityId) return '';
  return `${ownerUid.value}_${familyStore.selectedEntityId}_${currentMonth.value}`;
});
const userId = computed(() => auth.user?.uid || '');
const familyId = computed(() => familyStore.family?.id || '');
const selectedCategory = ref<BudgetCategory | null>(null);
const newMerchantName = ref('');
const searchInput = ref(null);
const search = ref('');
const debouncedSearch = ref('');
const monthOffset = ref(0);
const menuOpen = ref(false);

let clickTimeout: ReturnType<typeof setTimeout> | null = null;
let touchTimeout: ReturnType<typeof setTimeout> | null = null;

const inlineEdit = ref({
  item: null as BudgetCategoryTrx | null,
  field: null as 'name' | 'target' | null,
  value: '' as string | number,
});

const inlineEditNumber = computed<number>({
  get() {
    const v = inlineEdit.value.value;
    return typeof v === 'number' ? v : parseFloat(String(v)) || 0;
  },
  set(val: number) {
    inlineEdit.value.value = val;
  }
});

const isMobile = computed(() => $q.screen.lt.md);

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

function clearSearch() {
  search.value = '';
}

function blurSearchInput() {
  if (isMobile.value && searchInput.value) {
    const el = searchInput.value.$el?.querySelector('input');
    el?.blur();
  }
}

watch(selectedCategory, (newVal) => {
  if (newVal && isMobile.value) {
    void nextTick(() => {
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
  console.log('Mounted: Checking auth state');
  loading.value = true;

  try {
    // User should be guaranteed by route guard
    if (!auth.user) {
      console.error('No user found despite route guard');
      showSnackbar('Authentication error: No user found', 'error');
      loading.value = false;
      return;
    }

    if (auth.authError) {
      console.error('Auth error:', auth.authError);
      showSnackbar(`Authentication error: ${auth.authError}`, 'error');
      loading.value = false;
      return;
    }

    loadingTimeout = setTimeout(() => {
      showLoadingMessage.value = true;
      console.log('Loading timeout triggered');
    }, 5000);

    console.log('Loading family for user:', auth.user.uid);
    await familyStore.loadFamily(auth.user.uid);
    console.log('Loading budgets');
    await loadBudgets();
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Initialization error:', err);
    showSnackbar(`Error loading data: ${err.message}`, 'error');
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

function onOpenBills() {
  // Placeholder: navigate to transactions/register or statements page in future
}

function onCreateGoal() {
  // Placeholder: open future goals modal/page
}

async function loadBudgets() {
  const user = auth.user;
  if (!user) {
    console.log('No user for loading budgets');
    return;
  }

  loading.value = true;
  try {
    console.log('Loading budgets for user:', user.uid, 'entity:', familyStore.selectedEntityId);
    await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error loading budgets:', err);
    showSnackbar(`Error loading budgets: ${err.message}`, 'error');
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
  let sourceBudget;

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
      const recurringGroups: Record<string, Transaction[]> = sourceBudget.transactions.reduce(
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
  const user = auth.user;
  if (!user) {
    showSnackbar('Please log in to create a budget', 'error');
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before creating a budget', 'error');
    return;
  }

  loading.value = true;
  try {
    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar('No family found for user', 'error');
      return;
    }

    await createBudgetForMonth(currentMonth.value, family.id, family.ownerUid, familyStore.selectedEntityId);
    await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);
    showSnackbar('Budget created successfully', 'success');
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Failed to create budget: ${err.message}`, 'error');
  } finally {
    loading.value = false;
  }
}

function updateMerchants() {
  merchantStore.updateMerchants(budget.value.transactions);
}

function onTransactionSaved(transaction: Transaction) {
  showTransactionDialog.value = false;
  try {
    budget.value.transactions = budget.value.transactions ? budget.value.transactions.filter((tx) => tx.id !== transaction.id) : [];
    budget.value.transactions.push(transaction);
    budgetStore.updateBudget(budgetId.value, { ...budget.value });

    updateMerchants();
    showSnackbar(isIncomeTransaction.value ? 'Income added successfully' : 'Transaction added successfully');
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error updating transaction: ${err.message}`, 'error');
  }
}

function updateTransactions(newTransactions: Transaction[]) {
  try {
    budget.value.transactions = newTransactions;
    budgetStore.updateBudget(budgetId.value, { ...budget.value });
    updateMerchants();
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error updating transactions: ${err.message}`, 'error');
  }
}

function shiftMonths(offset: number) {
  monthOffset.value = offset;
}

async function selectMonth(month: string) {
  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before selecting a month', 'error');
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
      if (!family) {
        throw new Error('Family not found');
      }
      const b = await createBudgetForMonth(month, family.id, ownerId, familyStore.selectedEntityId);
      budget.value = { ...b, budgetId: newBudgetId };
      budgetStore.updateBudget(newBudgetId, b);
      categoryOptions.value = b.categories.map((cat) => cat.name);
      if (!categoryOptions.value.includes('Income')) {
        categoryOptions.value.push('Income');
      }
    }
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error loading budget: ${err.message}`, 'error');
  } finally {
    loading.value = false;
  }
}

async function saveBudget() {
  const user = auth.user;
  if (!user) {
    showSnackbar('You don’t have permission to save budgets', 'error');
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before saving the budget', 'error');
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
    const err = error as Error;
    showSnackbar(`Error saving budget: ${err.message}`, 'error', () => {
      void saveBudget();
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
    showSnackbar('Please select an entity before adding a transaction', 'error');
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
      entityId: familyStore.selectedEntityId,
      taxMetadata: [],
    };
    isIncomeTransaction.value = false;
    showTransactionDialog.value = true;
  }
}

function addTransactionForCategory(category: string) {
  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before adding a transaction', 'error');
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
      entityId: familyStore.selectedEntityId,
      taxMetadata: [],
    };
    isIncomeTransaction.value = false;
    showTransactionDialog.value = true;
  }
}

async function duplicateCurrentMonth(month: string) {
  const user = auth.user;
  if (!user) {
    showSnackbar('Please log in to duplicate a budget', 'error');
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before duplicating a budget', 'error');
    return;
  }

  $q.loading.show({
    message: 'Duplicating budget...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });

  try {
    const family = await familyStore.getFamily();
    if (!family) {
      showSnackbar('No family found for user', 'error');
      return;
    }

    const newBudgetId = `${family.ownerUid}_${familyStore.selectedEntityId}_${month}`;
    const existingBudget = await dataAccess.getBudget(newBudgetId);
    if (existingBudget) {
      showSnackbar('A budget already exists for this month', 'warning');
      return;
    }

    const newBudget = await createBudgetForMonth(month, family.id, family.ownerUid, familyStore.selectedEntityId);
    newBudget.merchants = budget.value.merchants ? [...budget.value.merchants] : [];
    await dataAccess.saveBudget(newBudget.budgetId, newBudget);
    await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);

    showSnackbar("Created new month's budget");
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Failed to duplicate budget: ${err.message}`, 'error');
  } finally {
    $q.loading.hide();
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
  console.log(inlineEdit.value);
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
    const err = error as Error;
    showSnackbar(`Error saving budget: ${err.message}`, 'error');
  }

  cancelInlineEdit();
}

function cancelInlineEdit() {
  inlineEdit.value.item = null;
  inlineEdit.value.field = null;
}

function showSnackbar(text: string, color = 'success', retry?: () => void) {
  $q.notify({
    message: text,
    color: color,
    position: 'bottom',
    timeout: retry ? 0 : 3000,
    actions: [...(retry ? [{ label: 'Retry', color: 'white', handler: retry }] : []), { label: 'Close', color: 'white', handler: () => {} }],
  });
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

.q-card {
  margin-bottom: 16px;
}

.q-btn {
  text-transform: none;
}

h1 {
  font-size: 1.3rem;
  white-space: nowrap;
}

.entity-selector {
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  font-size: 1.5rem;
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
  align-items: center;
}

.month-item {
  padding: 4px;
}

.border {
  border: 1px solid var(--q-primary);
}

.border-bottom {
  border-bottom: 1px solid #e0e0e0;
}

.progress-bar {
  height: 6px;
  border-radius: 3px;
}
</style>
