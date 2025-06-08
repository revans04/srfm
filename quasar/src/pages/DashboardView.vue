<!-- src/pages/DashboardPage.vue -->
<template>
  <q-page :class="isMobile ? 'pt-0 px-0' : ''">
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
          <q-card-section class="row items-center">
            <div class="col grow">
              No budgets found for {{ selectedEntity?.name || 'selected entity' }}. Would you like
              to create a default budget for {{ formatMonth(currentMonth) }}? You can also import
              budget information from the Data Page.
            </div>
            <div class="col shrink">
              <q-btn color="primary" label="Create Default Budget" @click="createDefaultBudget" />
            </div>
          </q-card-section>
        </q-card>
      </div>
    </div>

    <!-- Main Content (hidden during loading) -->
    <div v-else>
      <div class="row" :class="isMobile ? 'q-pa-sm' : 'q-pr-xl'">
        <div class="col-12">
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
              <template v-slot:activator="{ toggle }">
                <div
                  class="month-selector no-wrap"
                  :class="{ 'text-white': isMobile }"
                  @click="toggle"
                >
                  <h1>
                    {{ formatMonth(currentMonth) }}
                    <q-icon name="mdi-chevron-down" size="sm" />
                  </h1>
                </div>
              </template>
              <q-card class="month-menu">
                <div class="row no-wrap items-center q-pa-sm month-navigation">
                  <div class="col-auto">
                    <q-btn icon="mdi-chevron-left" flat dense @click.stop="shiftMonths(-6)" />
                  </div>
                  <div class="col text-center">
                    <span>{{ displayYear }}</span>
                  </div>
                  <div class="col-auto">
                    <q-btn icon="mdi-chevron-right" flat dense @click.stop="shiftMonths(6)" />
                  </div>
                </div>
                <div class="row no-wrap">
                  <div v-for="month in displayedMonths" :key="month.value" class="col-4 month-item">
                    <div
                      class="q-pa-xs rounded-5 cursor-pointer q-ma-xs text-center"
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
                  </div>
                </div>
              </q-card>
            </q-menu>
            <div class="q-ml-sm">
              <q-btn
                v-if="!isMobile && !isEditing"
                icon="mdi-pencil"
                flat
                round
                color="primary"
                title="Edit Budget"
                @click="isEditing = true"
                class="q-mr-xs"
              />
              <q-btn
                v-if="isEditing"
                icon="mdi-close"
                flat
                round
                title="Cancel"
                @click="isEditing = false"
                class="q-mr-xs"
              />
              <q-btn
                v-if="!isMobile && !isEditing"
                icon="mdi-trash-can-outline"
                flat
                round
                color="negative"
                title="Delete Budget"
              />
            </div>
          </div>
          <div
            class="q-pr-sm q-py-0"
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
        <div v-if="!isMobile || selectedCategory == null" class="col-12 col-sm-6">
          <div class="q-my-sm bg-white rounded-10 q-pa-sm q-mb-lg">
            <q-input
              v-model="search"
              append-icon="mdi-magnify"
              dense
              label="Search"
              standout
              single-line
            />
          </div>
        </div>
        <q-btn
          v-if="isMobile && !selectedCategory"
          icon="mdi-plus"
          round
          color="white"
          class="fixed-top-right q-ma-sm"
          @click="addTransaction"
        />
      </div>

      <div class="row">
        <!-- Main Content -->
        <div
          :class="{
            'col-12': !selectedCategory,
            'col-8': selectedCategory && !isMobile,
            'd-none': selectedCategory && isMobile,
          }"
        >
          <!-- Budget Editing Form -->
          <q-card v-if="isEditing" flat bordered>
            <q-card-section>
              <div class="text-h6">
                Edit Budget for {{ selectedEntity?.name || 'selected entity' }}
              </div>
            </q-card-section>
            <q-card-section>
              <q-form @submit="saveBudget">
                <!-- Merchants Section -->
                <div class="row q-mt-md">
                  <div class="col-12">
                    <h3>Merchants</h3>
                    <q-chip
                      v-for="(merchant, index) in budget.merchants"
                      :key="merchant.name"
                      removable
                      @remove="removeMerchant(index)"
                      class="q-ma-xs"
                    >
                      {{ merchant.name }} ({{ merchant.usageCount }})
                    </q-chip>
                    <q-input
                      v-model="newMerchantName"
                      label="Add Merchant"
                      dense
                      class="q-mt-sm"
                      @keyup.enter="addMerchant"
                      append-icon="mdi-plus"
                      @click:append="addMerchant"
                    />
                  </div>
                </div>

                <div class="row q-mt-md">
                  <div class="col-12">
                    <h3>Categories</h3>
                  </div>
                </div>
                <div
                  v-for="(cat, index) in budget.categories"
                  :key="index"
                  class="row items-center no-wrap"
                >
                  <div class="col-12 col-sm-3 q-pa-sm">
                    <q-input v-model="cat.name" label="Category" required dense />
                  </div>
                  <div class="col-12 col-sm-2 q-pa-sm">
                    <q-input v-model="cat.group" label="Group (e.g., Utilities)" dense />
                  </div>
                  <div class="col-12 col-sm-2 q-pa-sm">
                    <currency-input
                      v-model.number="cat.target"
                      label="Target"
                      class="text-right"
                      dense
                      required
                    />
                  </div>
                  <div class="col-12 col-sm-2 q-pa-sm">
                    <currency-input
                      v-model="cat.carryover"
                      label="Carryover"
                      class="text-right"
                      dense
                    />
                  </div>
                  <div class="col-12 col-sm-2 q-pa-sm">
                    <q-checkbox v-model="cat.isFund" label="Is Fund?" dense />
                  </div>
                  <div class="col-12 col-sm-1 q-pa-sm">
                    <q-btn
                      icon="mdi-close"
                      color="negative"
                      flat
                      round
                      @click="removeCategory(index)"
                    />
                  </div>
                </div>

                <q-btn color="primary" label="Add Category" @click="addCategory" class="q-mt-sm" />
                <q-btn
                  color="positive"
                  label="Add Income Category"
                  @click="addIncomeCategory"
                  class="q-mt-sm q-ml-sm"
                />
                <q-btn
                  type="submit"
                  color="positive"
                  label="Save Budget"
                  :loading="saving"
                  class="q-mt-sm q-ml-sm"
                />
              </q-form>
            </q-card-section>
          </q-card>

          <!-- Income Section -->
          <q-card v-if="!isEditing && incomeItems" flat bordered class="q-mt-md">
            <q-card-section>
              <div class="row">
                <div class="col font-weight-bold">
                  Income for {{ selectedEntity?.name || 'selected entity' }}
                </div>
                <div v-if="!isMobile" class="col-auto">Planned</div>
                <div :class="isMobile ? 'col-auto' : 'col-2'" class="text-right">Received</div>
              </div>
              <div
                v-for="item in incomeItems"
                :key="item.name"
                style="border-bottom: solid 1px var(--q-light)"
                class="q-py-sm"
              >
                <div class="row cursor-pointer" @click="onIncomeRowClick(item)">
                  <div class="col">{{ item.name }}</div>
                  <div v-if="!isMobile" class="col-auto">
                    {{ formatCurrency(toDollars(toCents(item.planned))) }}
                  </div>
                  <div :class="isMobile ? 'col-auto' : 'col-2'" class="text-right">
                    <div :class="item.received > item.planned ? 'text-positive' : ''">
                      {{ formatCurrency(toDollars(toCents(item.received))) }}
                    </div>
                  </div>
                </div>
              </div>
              <div v-if="!isMobile" class="row dense">
                <div class="col">
                  <q-space />
                </div>
                <div class="col-auto font-weight-bold">
                  {{ formatCurrency(toDollars(toCents(plannedIncome))) }}
                </div>
                <div :class="isMobile ? 'col-auto' : 'col-2'">
                  <div
                    class="font-weight-bold text-right"
                    :class="actualIncome > plannedIncome ? 'text-positive' : ''"
                  >
                    {{ formatCurrency(toDollars(toCents(actualIncome))) }}
                  </div>
                </div>
              </div>
            </q-card-section>
          </q-card>

          <!-- Category Tables -->
          <div v-if="!isEditing && catTransactions" class="row q-mt-md">
            <div v-for="(g, gIdx) in groups" :key="gIdx" class="col-12">
              <q-card flat bordered>
                <q-card-section>
                  <div class="row text-info">
                    <div class="col">{{ g.group || 'Ungrouped' }}</div>
                    <q-space />
                    <div v-if="!isMobile" class="col-2">Planned</div>
                    <div :class="isMobile ? 'col-auto' : 'col-2'">Remaining</div>
                  </div>
                  <div
                    v-for="(item, idx) in catTransactions.filter((c) => c.group == g.group)"
                    :key="idx"
                  >
                    <div class="row cursor-pointer">
                      <div
                        v-if="!(inlineEdit.item?.name === item.name && inlineEdit.field === 'name')"
                        class="col"
                        @dblclick.stop="handleNameDblClick(item)"
                        @touchstart="startTouch(item, 'name')"
                        @touchend="endTouch"
                      >
                        <q-icon
                          v-if="item.isFund"
                          name="mdi-piggy-bank-outline"
                          size="sm"
                          color="primary"
                          class="q-mr-xs"
                        />
                        {{ item.name }}
                      </div>
                      <div v-else class="col">
                        <q-input
                          v-model="inlineEdit.value"
                          dense
                          hide-details
                          standout
                          autofocus
                          @keydown.enter="saveInlineEdit"
                          @keydown.esc="cancelInlineEdit"
                          @blur="saveInlineEdit"
                        />
                      </div>
                      <div
                        v-if="
                          !isMobile &&
                          !(inlineEdit.item?.name === item.name && inlineEdit.field === 'target')
                        "
                        class="col-2"
                        @dblclick.stop="handleTargetDblClick(item)"
                        @touchstart="startTouch(item, 'target')"
                        @touchend="endTouch"
                      >
                        {{ formatCurrency(item.target) }}
                      </div>
                      <div v-else-if="!isMobile" class="col-2">
                        <currency-input
                          v-model.number="inlineEdit.value"
                          dense
                          hide-details
                          @keydown.enter="saveInlineEdit"
                          @keydown.esc="cancelInlineEdit"
                          @blur="saveInlineEdit"
                        />
                      </div>
                      <div
                        :class="isMobile ? 'col-auto' : 'col-2'"
                        :class="item.remaining && item.remaining < 0 ? 'text-negative' : ''"
                      >
                        {{ formatCurrency(item.remaining) }}
                      </div>
                    </div>
                    <div class="row dense">
                      <div class="col-12">
                        <q-linear-progress
                          :value="item.percentage / 100"
                          color="primary"
                          track-color="grey-4"
                          class="progress-bar"
                        />
                      </div>
                    </div>
                  </div>
                </q-card-section>
              </q-card>
            </div>
            <div v-if="groups.length === 0" class="col-12">
              <q-card flat bordered class="q-pa-md">
                <q-card-section class="text-warning"
                  >No categories defined for this budget.</q-card-section
                >
              </q-card>
            </div>
          </div>
        </div>

        <!-- Transaction List Sidebar -->
        <div
          v-if="selectedCategory && !isEditing"
          :class="isMobile ? 'col-12 sidebar-mobile' : 'col-4 sidebar'"
        >
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
        </div>
      </div>

      <!-- Version Info -->
      <div class="row">
        <q-space />
        <div class="col-auto">
          <span class="text-caption text-center">{{ `Version: ${appVersion}` }}</span>
        </div>
      </div>

      <!-- Transaction Dialog -->
      <q-dialog v-model="showTransactionDialog" max-width="600px">
        <q-card>
          <q-card-section class="text-h6">
            {{ isIncomeTransaction ? 'Add Income' : 'Add Transaction' }}
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
      <q-toast v-model="snackbar" :color="snackbarColor" :timeout="timeout" position="bottom">
        {{ snackbarText }}
        <template v-slot:action>
          <q-btn v-if="showRetry" flat label="Retry" @click="retryAction" />
          <q-btn flat label="Close" @click="snackbar = false" />
        </template>
      </q-toast>

      <!-- Duplicating Overlay -->
      <q-dialog v-model="duplicating" persistent>
        <q-card class="q-pa-md">
          <q-spinner color="primary" size="50px" />
          <span class="q-ml-sm">Duplicating budget...</span>
        </q-card>
      </q-dialog>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick, onUnmounted } from 'vue';
import { auth } from '@/firebase';
import { dataAccess } from '@/dataAccess';
import CurrencyInput from '@/components/CurrencyInput.vue';
import CategoryTransactions from '@/components/CategoryTransactions.vue';
import TransactionForm from '@/components/TransactionForm.vue';
import {
  Transaction,
  Budget,
  IncomeTarget,
  BudgetCategoryTrx,
  BudgetCategory,
  Entity,
} from '@/types';
import version from '@/version';
import {
  toDollars,
  toCents,
  formatCurrency,
  adjustTransactionDate,
  todayISO,
  currentMonthISO,
} from '@/utils/helpers';
import { useBudgetStore } from '@/store/budget';
import { useMerchantStore } from '@/store/merchants';
import { useFamilyStore } from '@/store/family';
import { debounce } from 'lodash';
import { v4 as uuidv4 } from 'uuid';
import { DEFAULT_BUDGET_TEMPLATES } from '@/constants/budgetTemplates';

const budgetStore = useBudgetStore();
const merchantStore = useMerchantStore();
const familyStore = useFamilyStore();
const budgets = ref<Budget[]>([]);

const appVersion = version;

const currentMonth = ref(currentMonthISO());
const initialMonth = ref(currentMonth.value);
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
});
const snackbar = ref(false);
const snackbarText = ref('');
const snackbarColor = ref('success');
const showRetry = ref(false);
const retryAction = ref<(() => void) | null>(null);
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

const inlineEdit = ref({
  item: null as BudgetCategoryTrx | null,
  field: null as 'name' | 'target' | null,
  value: '' as string | number,
});

const isMobile = computed(() => window.innerWidth < 960);

// Entity-related computed properties
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
    return catTransactions.filter(
      (t) => t.group.toLowerCase().includes(srch) || t.name.toLowerCase().includes(srch),
    );
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
  return (
    availableBudgets.value.filter(
      (b) => b.month == month && b.entityId === familyStore.selectedEntityId,
    ).length > 0
  );
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

watch(selectedCategory, (newVal) => {
  if (newVal && isMobile.value) {
    nextTick(() => {
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

  const defaultBudget = budgets.value.find(
    (b) => b.month === currentMonth.value && b.entityId === familyStore.selectedEntityId,
  );
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
    showSnackbar('Please log in to view the dashboard', 'error');
    loading.value = false;
    return;
  }

  try {
    loadingTimeout = setTimeout(() => {
      showLoadingMessage.value = true;
      console.log('Loading timeout triggered');
    }, 5000);

    await familyStore.loadFamily(user.uid); // Load family to get entities
    await loadBudgets();
  } catch (error: any) {
    showSnackbar(`Error loading data: ${error.message}`, 'error');
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
  } catch (error: any) {
    showSnackbar(`Error loading budgets: ${error.message}`, 'error');
  } finally {
    loading.value = false;
  }
}

async function createBudgetForMonth(
  month: string,
  familyId: string,
  ownerUid: string,
  entityId: string,
): Promise<Budget> {
  const budgetId = `${ownerUid}_${entityId}_${month}`;
  const existingBudget = await dataAccess.getBudget(budgetId);
  if (existingBudget) {
    return existingBudget;
  }

  // Check entity template budget
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

  // Use predefined template based on entity.type
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

  // Fallback: Copy most recent previous budget or earliest future budget
  const availableBudgets = Array.from(budgetStore.budgets.values()).sort((a, b) =>
    a.month.localeCompare(b.month),
  );
  let sourceBudget: Budget | undefined;

  const previousBudgets = availableBudgets.filter(
    (b) => b.month < month && b.entityId === entityId,
  );
  if (previousBudgets.length > 0) {
    sourceBudget = previousBudgets[previousBudgets.length - 1]; // Most recent previous
  } else {
    const futureBudgets = availableBudgets.filter(
      (b) => b.month > month && b.entityId === entityId,
    );
    if (futureBudgets.length > 0) {
      sourceBudget = futureBudgets[0]; // Earliest future
    }
  }

  if (sourceBudget) {
    const [newYear, newMonthNum] = month.split('-').map(Number);
    const [sourceYear, sourceMonthNum] = sourceBudget.month.split('-').map(Number);
    const isFutureMonth =
      newYear > sourceYear || (newYear === sourceYear && newMonthNum > sourceMonthNum);

    let newCarryover: Record<string, number> = {};
    if (isFutureMonth) {
      newCarryover = await dataAccess.calculateCarryOver(sourceBudget);
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

    // Copy recurring transactions
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
        const firstInstance = group.sort(
          (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime(),
        )[0];
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

  // Default budget as last resort
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

    const budget = await createBudgetForMonth(
      currentMonth.value,
      family.id,
      family.ownerUid,
      familyStore.selectedEntityId,
    );
    await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);
    showSnackbar('Budget created successfully', 'success');
  } catch (error: any) {
    console.error('Error creating budget:', error);
    showSnackbar(`Failed to create budget: ${error.message}`, 'error');
  } finally {
    loading.value = false;
  }
}

async function refreshBudget() {
  try {
    const freshBudget = await dataAccess.getBudget(budgetId.value);
    if (freshBudget) {
      budget.value = { ...freshBudget, budgetId: budgetId.value };
      budgetStore.updateBudget(budgetId.value, freshBudget);
      categoryOptions.value = freshBudget.categories.map((cat) => cat.name);
      if (!categoryOptions.value.includes('Income')) {
        categoryOptions.value.push('Income');
      }
    }
  } catch (error: any) {
    showSnackbar(`Error refreshing budget: ${error.message}`, 'error');
  }
}

function updateMerchants() {
  merchantStore.updateMerchants(budget.value.transactions);
}

async function onTransactionSaved(transaction: Transaction) {
  showTransactionDialog.value = false;
  try {
    budget.value.transactions = budget.value.transactions
      ? budget.value.transactions.filter((tx) => tx.id !== transaction.id)
      : [];
    budget.value.transactions.push(transaction);
    budgetStore.updateBudget(budgetId.value, { ...budget.value });

    updateMerchants();
    showSnackbar(
      isIncomeTransaction.value ? 'Income added successfully' : 'Transaction added successfully',
    );
  } catch (error: any) {
    showSnackbar(`Error updating transaction: ${error.message}`, 'error');
  }
}

async function updateTransactions(newTransactions: Transaction[]) {
  try {
    budget.value.transactions = newTransactions;
    budgetStore.updateBudget(budgetId.value, { ...budget.value });
    updateMerchants();
  } catch (error: any) {
    showSnackbar(`Error updating transactions: ${error.message}`, 'error');
  }
}

function shiftMonths(offset: number) {
  monthOffset.value += offset;
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
      const b = await createBudgetForMonth(
        month,
        family!.id,
        ownerId,
        familyStore.selectedEntityId,
      );
      budget.value = { ...b, budgetId: newBudgetId };
      budgetStore.updateBudget(newBudgetId, b);
      categoryOptions.value = b.categories.map((cat) => cat.name);
      if (!categoryOptions.value.includes('Income')) {
        categoryOptions.value.push('Income');
      }
    }
  } catch (error: any) {
    showSnackbar(`Error loading budget: ${error.message}`, 'error');
  } finally {
    loading.value = false;
  }
}

async function saveBudget() {
  const user = auth.currentUser;
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
  } catch (error: any) {
    showSnackbar(`Error saving budget: ${error.message}`, 'error', async () => {
      await saveBudget();
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

  const existingMerchant = budget.value.merchants.find(
    (m) => m.name.toLowerCase() === merchantName.toLowerCase(),
  );

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
    };
    isIncomeTransaction.value = false;
    showTransactionDialog.value = true;
  }
}

function addIncome() {
  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before adding income', 'error');
    return;
  }

  newTransaction.value = {
    id: uuidv4(),
    date: todayISO(),
    budgetMonth: currentMonth.value,
    merchant: '',
    categories: [{ category: 'Income', amount: 0 }],
    amount: 0,
    notes: '',
    recurring: false,
    recurringInterval: 'Monthly',
    userId: userId.value,
    isIncome: true,
    entityId: familyStore.selectedEntityId,
  };
  isIncomeTransaction.value = true;
  showTransactionDialog.value = true;
}

async function duplicateCurrentMonth(month: string) {
  const user = auth.currentUser;
  if (!user) {
    showSnackbar('Please log in to duplicate a budget', 'error');
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before duplicating a budget', 'error');
    return;
  }

  duplicating.value = true;
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

    const newBudget = await createBudgetForMonth(
      month,
      family.id,
      family.ownerUid,
      familyStore.selectedEntityId,
    );
    await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);

    showSnackbar("Created new month's budget");
  } catch (error: any) {
    console.error('Error duplicating budget:', error);
    showSnackbar(`Failed to duplicate budget: ${error.message}`, 'error');
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
    const amount =
      typeof inlineEdit.value.value === 'number'
        ? inlineEdit.value.value
        : parseFloat(String(inlineEdit.value.value));
    budget.value.categories[idx].target = isNaN(amount) ? 0 : amount;
    item.target = isNaN(amount) ? 0 : amount;
  }

  try {
    budget.value.budgetId = budgetId.value;
    await dataAccess.saveBudget(budgetId.value, budget.value);
    budgetStore.updateBudget(budgetId.value, { ...budget.value });
    showSnackbar('Budget updated');
  } catch (error: any) {
    showSnackbar(`Error saving budget: ${error.message}`, 'error');
  }

  cancelInlineEdit();
}

function cancelInlineEdit() {
  inlineEdit.value.item = null;
  inlineEdit.value.field = null;
}

function showSnackbar(text: string, color = 'success', retry?: () => void) {
  snackbarText.value = text;
  snackbarColor.value = color;
  showRetry.value = !!retry;
  retryAction.value = retry || null;
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
.search-container {
  max-width: 400px;
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
.month-item .q-btn {
  text-transform: none;
  font-size: 0.875rem;
}
.today-row {
  padding: 8px 0;
  border-top: 1px solid #e0e0e0;
}
.today-label {
  font-size: 0.875rem;
  color: #757575;
}
.rounded-10 {
  border-radius: 10px;
}
</style>
