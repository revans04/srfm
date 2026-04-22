<template>
  <q-page class="bg-grey-1 q-pa-lg" :class="['budget-page', { 'budget-page--mobile': isMobile }]">
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
        <q-card  class="q-pa-md">
          <q-card-section>
            <div class="row items-center">
              <div class="col">
                No budgets found for {{ selectedEntity?.name || 'selected entity' }}. Would you like to create a default budget for
                {{ formatLongMonth(currentMonth) }}? You can also import budget information from the Data Page.
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
      <h1 class="page-title q-mb-sm">Budget</h1>
      <GuidedTip tip-id="budget-page">
        This is your monthly budget. Set planned amounts for each category and track actual spending.
      </GuidedTip>
      <section class="budget-header">
        <!-- Row 1: Title + context + actions -->
        <div class="row items-center justify-between q-mb-sm">
          <div class="row items-center q-gutter-sm">
            <EntitySelector @change="loadBudgets" />
            <div class="text-body1 text-primary text-weight-medium">
              {{ budgetLabel }}
              <MonthSelector v-model="currentMonth" :entity-id="familyStore.selectedEntityId" :existing-months="selectedEntityMonthSet" @select="selectMonth" />
            </div>
          </div>
          <div class="row items-center q-gutter-xs">
            <q-btn
              v-if="!isEditing"
              outline
              dense
              no-caps
              color="primary"
              icon="edit"
              label="Edit"
              @click="isEditing = true"
            />
            <q-btn
              v-else
              outline
              dense
              no-caps
              color="primary"
              icon="close"
              label="Close"
              @click="isEditing = false"
            />
            <q-btn
              v-if="!isEditing"
              outline
              dense
              no-caps
              color="negative"
              icon="delete_outline"
              label="Delete"
              @click="confirmDeleteBudget"
            />
          </div>
        </div>

        <!-- Non-current month banner -->
        <div v-if="!isViewingCurrentMonth" class="non-current-banner q-mb-sm">
          <q-icon name="schedule" size="16px" />
          <span class="non-current-banner__text">
            Viewing <strong>{{ budgetLabel }}</strong> · {{ monthOffsetLabel }}
          </span>
          <a class="non-current-banner__link" @click="returnToCurrentMonth">Return to current month</a>
        </div>

        <!-- Row 2: Summary stats -->
        <div v-if="!isMobile && !isEditing" class="row q-col-gutter-md q-mb-md budget-stats-row">
          <div class="col">
            <div class="budget-stat-card">
              <div class="budget-stat-card__label">Left to Budget</div>
              <div class="budget-stat-card__value" :class="remainingToBudget >= 0 ? 'text-positive' : 'text-warning'">
                {{ formatCurrency(Math.abs(remainingToBudget)) }}
              </div>
            </div>
          </div>
          <div class="col">
            <div class="budget-stat-card">
              <div class="budget-stat-card__label">Income Received</div>
              <div class="budget-stat-card__value">{{ formatCurrency(actualIncome) }}</div>
              <div class="budget-stat-card__sub">Planned {{ formatCurrency(plannedIncome) }}</div>
            </div>
          </div>
          <div class="col">
            <div class="budget-stat-card">
              <div class="budget-stat-card__label">Savings Goals</div>
              <div class="budget-stat-card__value">{{ formatCurrency(savingsTotal) }}</div>
              <div class="budget-stat-card__sub">Monthly total</div>
            </div>
          </div>
        </div>

        <!-- Row 3: Search -->
        <q-input
          v-model="search"
          dense
          outlined
          clearable
          ref="searchInput"
          placeholder="Search categories or groups"
          hide-bottom-space
          class="budget-search"
          @keyup.enter="blurSearchInput"
          @clear="clearSearch"
        >
          <template #prepend>
            <q-icon name="search" color="grey-5" />
          </template>
        </q-input>
      </section>

      <q-page-sticky v-if="!isMobile" position="bottom-right" :offset="[24, 24]" class="budget-fab">
        <q-btn round color="primary" icon="add" @click="addTransaction">
          <q-tooltip>Add transaction</q-tooltip>
        </q-btn>
      </q-page-sticky>

      <div class="row budget-content-row">
        <!-- Main Content -->
        <div :class="isMobile ? 'col-12' : 'col-12 col-lg-8'">
          <!-- Budget Editing Form -->
          <q-card v-if="isEditing" >
            <q-card-section>Edit Budget for {{ selectedEntity?.name || 'selected entity' }}</q-card-section>
            <q-card-section>
              <q-form @submit.prevent="saveBudget">
                <!-- Merchants Section -->
                <div class="row q-mt-lg">
                  <div class="col-12">
                    <h3 class="text-h5">Merchants</h3>
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
                    <h3 class="text-h5">Categories</h3>
                  </div>
                </div>
                <div v-for="(cat, index) in budget.categories" :key="index" class="row items-center q-col-gutter-sm q-mb-sm">
                  <div class="col-12 col-sm-3 q-pa-xs">
                    <q-input v-model="cat.name" label="Category" required dense />
                  </div>
                  <div class="col-12 col-sm-3 q-pa-xs">
                    <q-select
                      :model-value="cat.groupName ?? ''"
                      :options="groupNameOptions"
                      label="Group"
                      dense
                      use-input
                      hide-selected
                      fill-input
                      input-debounce="0"
                      new-value-mode="add-unique"
                      @update:model-value="(val) => onCategoryGroupChange(cat, val)"
                    />
                  </div>
                  <div class="col-6 col-sm-2 q-pa-xs">
                    <CurrencyInput v-model.number="cat.target" label="Target" class="text-right" dense required />
                  </div>
                  <div class="col-6 col-sm-2 q-pa-xs">
                    <CurrencyInput v-model="cat.carryover" label="Carryover" class="text-right" dense />
                  </div>
                  <div class="col-6 col-sm-1 q-pa-xs">
                    <q-checkbox v-model="cat.isFund" label="Is Fund?" dense />
                  </div>
                  <div class="col-6 col-sm-1 q-pa-xs">
                    <q-btn flat icon="close" color="negative" @click="removeCategory(index)">
                      <q-tooltip>Remove category</q-tooltip>
                    </q-btn>
                  </div>
                  <div class="col-12 col-sm-6 q-pa-xs">
                    <q-select
                      :model-value="cat.fundingSourceCategory || null"
                      :options="fundingSourceOptionsFor(cat)"
                      label="Funded from (optional)"
                      dense
                      outlined
                      stack-label
                      clearable
                      hint="If set, new expenses in this category default to a transfer from the source."
                      @update:model-value="(v) => (cat.fundingSourceCategory = v || undefined)"
                    />
                  </div>
                </div>

                <q-btn flat color="primary" @click="addCategory" class="q-mt-sm">Add Category</q-btn>
                <q-btn flat color="primary" @click="addIncomeCategory" class="q-mt-sm q-ml-sm">Add Income Category</q-btn>
                <q-btn type="submit" color="primary" class="q-mt-sm q-ml-sm" :loading="saving">Save Budget</q-btn>
              </q-form>
            </q-card-section>
          </q-card>

          <!-- Income Section -->
          <q-card v-if="!isEditing && incomeItems" id="income-section" class="income-card">
            <q-card-section class="q-pa-lg">
              <div class="income-header">
                <span class="income-title">Income for {{ selectedEntity?.name || 'selected entity' }}</span>
                <span v-if="!isMobile" class="col-header income-col-planned">Planned</span>
                <span class="col-header income-col-received">Received</span>
              </div>
              <div class="income-divider" />
              <div v-for="item in incomeItems" :key="item.name" class="income-row cursor-pointer" @click="onIncomeRowClick(item)">
                <span class="income-row__name">{{ item.name }}</span>
                <span v-if="!isMobile" class="income-row__planned">{{ formatCurrency(toDollars(toCents(item.planned))) }}</span>
                <span class="income-row__received" :class="item.received > item.planned ? 'text-positive' : ''">
                  {{ formatCurrency(toDollars(toCents(item.received))) }}
                </span>
              </div>
              <div class="income-divider" />
              <div class="income-total">
                <span class="income-total__label">Total Income</span>
                <span v-if="!isMobile" class="income-total__planned">{{ formatCurrency(toDollars(toCents(plannedIncome))) }}</span>
                <span class="income-total__received" :class="actualIncome > plannedIncome ? 'text-positive' : ''">
                  {{ formatCurrency(toDollars(toCents(actualIncome))) }}
                </span>
              </div>
            </q-card-section>
          </q-card>

          <!-- Favorites Section -->
          <q-card v-if="!isEditing && favoriteItems.length" id="favorites-section" class="q-mt-md">
            <q-card-section class="q-pa-lg">
              <!-- Column headers -->
              <div class="cat-table-header">
                <span class="col-header cat-col-name">Category</span>
                <span v-if="!isMobile" class="col-header cat-col-progress">Progress</span>
                <span v-if="!isMobile" class="col-header cat-col-planned">Planned</span>
                <span class="col-header cat-col-remaining">Remaining</span>
              </div>
              <div class="cat-divider" />
              <div class="cat-group-header">Favorites</div>
              <div v-for="(item, idx) in favoriteItems" :key="idx">
                <div class="cat-row cursor-pointer" @click="handleRowClick(item)">
                  <span class="cat-col-name row items-center no-wrap">
                    <q-icon
                      :name="item.favorite ? 'star' : 'star_border'"
                      size="16px"
                      class="q-mr-xs cursor-pointer"
                      :color="item.favorite ? 'amber' : 'grey'"
                      @click.stop="toggleFavorite(item)"
                    >
                      <q-tooltip>Toggle Favorite</q-tooltip>
                    </q-icon>
                    <q-icon v-if="item.isFund" size="16px" class="q-mr-xs" color="primary" name="savings" />
                    <span class="cat-name-text">{{ item.name }}</span>
                  </span>
                  <span v-if="!isMobile" class="cat-col-progress">
                    <div class="cat-progress-track">
                      <div
                        class="cat-progress-fill"
                        :class="item.percentage > 100 ? 'cat-progress--over' : item.percentage >= 100 ? 'cat-progress--full' : 'cat-progress--partial'"
                        :style="{ width: Math.min(item.percentage, 100) + '%' }"
                      />
                    </div>
                  </span>
                  <span v-if="!isMobile" class="cat-col-planned cat-amount">{{ formatCurrency(item.target) }}</span>
                  <span class="cat-col-remaining cat-amount" :class="{ 'text-warning': item.remaining < 0 }">
                    {{ formatCurrency(item.remaining) }}
                  </span>
                </div>
              </div>
            </q-card-section>
          </q-card>

          <div id="goals-section" class="q-mt-md">
            <GoalsGroupCard :entity-id="familyStore.selectedEntityId || ''" @add="onAddGoal" @contribute="onContribute" @view="onViewGoal" />
          </div>

          <!-- Category Tables -->
          <div v-if="!isEditing && catTransactions" id="groups-section" class="q-mt-md">
            <template v-for="(g, gIdx) in groups" :key="g.id">
              <q-card
                class="cat-table-card q-mb-md"
                :class="{ 'group-drag-over': dragOverIndex === gIdx }"
                draggable="true"
                @dragstart="onGroupDragStart(gIdx, $event)"
                @dragover.prevent="onGroupDragOver(gIdx)"
                @dragleave="onGroupDragLeave"
                @drop.prevent="onGroupDrop(gIdx)"
                @dragend="onGroupDragEnd"
              >
                <q-card-section class="q-pa-lg">
                  <div class="cat-table-header">
                    <span class="col-header cat-col-name row items-center no-wrap">
                      <q-icon name="drag_indicator" size="18px" class="group-drag-handle q-mr-xs" />
                      <q-input
                        v-if="groupRename.id === g.id"
                        v-model="groupRename.value"
                        dense
                        autofocus
                        @keydown.enter="saveGroupRename"
                        @keydown.esc="cancelGroupRename"
                        @blur="saveGroupRename"
                        class="group-rename-input"
                      />
                      <span
                        v-else
                        class="group-name-text"
                        @dblclick.stop="startGroupRename(g)"
                      >
                        {{ g.name || '(Ungrouped)' }}
                        <q-tooltip>Double-click to rename. Renames apply to every month for this entity.</q-tooltip>
                      </span>
                    </span>
                    <span v-if="!isMobile" class="col-header cat-col-progress">Progress</span>
                    <span v-if="!isMobile" class="col-header cat-col-planned">Planned</span>
                    <span class="col-header cat-col-remaining">Remaining</span>
                  </div>
                  <div class="cat-divider" />
                  <div
                    v-for="(item, idx) in catTransactions
                      .filter((c) => c.groupId === g.id)
                      .slice()
                      .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || a.name.toLowerCase().localeCompare(b.name.toLowerCase()))"
                    :key="idx"
                    :class="{ 'cat-drag-over': catDragOver?.groupId === g.id && catDragOver?.idx === idx }"
                    draggable="true"
                    @dragstart.stop="onCategoryDragStart(g.id, idx, item, $event)"
                    @dragover.prevent.stop="onCategoryDragOver(g.id, idx)"
                    @dragleave.stop="onCategoryDragLeave"
                    @drop.prevent.stop="onCategoryDrop(g.id, idx)"
                    @dragend.stop="onCategoryDragEnd"
                  >
                    <div class="cat-row cursor-pointer" @click="handleRowClick(item)">
                      <span
                        v-if="!(inlineEdit.item?.name === item.name && inlineEdit.field === 'name')"
                        @dblclick.stop="handleNameDblClick(item)"
                        @touchstart="startTouch(item, 'name')"
                        @touchend="endTouch"
                        class="cat-col-name row items-center no-wrap"
                      >
                        <q-icon
                          :name="item.favorite ? 'star' : 'star_border'"
                          size="16px"
                          class="q-mr-xs cursor-pointer"
                          :color="item.favorite ? 'amber' : 'grey'"
                          @click.stop="toggleFavorite(item)"
                        />
                        <q-icon v-if="item.isFund" size="16px" class="q-mr-xs" color="primary" name="savings" />
                        <span class="cat-name-text">{{ item.name }}</span>
                        <q-icon
                          v-if="item.isFund"
                          name="change_circle"
                          size="16px"
                          class="q-ml-xs cursor-pointer"
                          color="accent"
                          @click.stop="onConvertLegacy(item)"
                        >
                          <q-tooltip>Convert to Savings Goal</q-tooltip>
                        </q-icon>
                      </span>
                      <span v-else class="cat-col-name">
                        <q-input
                          v-model="inlineEdit.value"
                          dense
                          autofocus
                          @keydown.enter="saveInlineEdit"
                          @keydown.esc="cancelInlineEdit"
                          @blur="saveInlineEdit"
                        />
                      </span>
                      <span v-if="!isMobile" class="cat-col-progress">
                        <template v-if="!(inlineEdit.item?.name === item.name && inlineEdit.field === 'target')">
                          <div class="cat-progress-track">
                            <div
                              class="cat-progress-fill"
                              :class="item.percentage > 100 ? 'cat-progress--over' : item.percentage >= 100 ? 'cat-progress--full' : 'cat-progress--partial'"
                              :style="{ width: Math.min(item.percentage, 100) + '%' }"
                            />
                          </div>
                        </template>
                      </span>
                      <span
                        v-if="!isMobile && !(inlineEdit.item?.name === item.name && inlineEdit.field === 'target')"
                        class="cat-col-planned cat-amount"
                        @dblclick.stop="handleTargetDblClick(item)"
                        @touchstart="startTouch(item, 'target')"
                        @touchend="endTouch"
                      >
                        {{ formatCurrency(item.target) }}
                      </span>
                      <span v-else-if="!isMobile" class="cat-col-planned">
                        <CurrencyInput
                          v-model="inlineEditNumber"
                          dense
                          @keydown.enter="saveInlineEdit"
                          @keydown.esc="cancelInlineEdit"
                          @blur="saveInlineEdit"
                        />
                      </span>
                      <span class="cat-col-remaining cat-amount" :class="{ 'text-warning': item.remaining < 0 }">
                        {{ formatCurrency(item.remaining) }}
                      </span>
                    </div>
                  </div>
                </q-card-section>
              </q-card>
            </template>
            <div v-if="groups.length === 0">
              <q-card>
                <q-card-section class="text-body2 text-grey-7">
                  No categories defined for this budget.
                  <q-btn flat dense no-caps color="primary" label="Add categories →" class="q-ml-xs" @click="isEditing = true" />
                </q-card-section>
              </q-card>
            </div>
          </div>
        </div>

        <!-- Mobile Detail Panels (full-screen dialogs) -->
        <q-dialog v-model="showMobileCategoryDialog" maximized transition-show="slide-up" transition-hide="slide-down" no-route-dismiss>
          <q-card class="column full-height">
            <CategoryTransactions
              v-if="selectedCategory"
              class="fit"
              :category="selectedCategory"
              :transactions="budget.transactions"
              :target="selectedCategory.target || 0"
              :budget-id="budgetId"
              :user-id="userId"
              :category-options="categoryOptions"
              @close="closeMobileCategoryDialog"
              @add-transaction="addTransactionForCategory(selectedCategory.name)"
              @update-transactions="updateTransactions"
            />
          </q-card>
        </q-dialog>

        <q-dialog v-model="showMobileGoalDialog" maximized transition-show="slide-up" transition-hide="slide-down" no-route-dismiss>
          <q-card class="column full-height">
            <GoalDetailsPanel v-if="selectedGoal" :goal="selectedGoal" @close="closeMobileGoalDialog" />
          </q-card>
        </q-dialog>

        <!-- Mobile Transactions Panel (stacked below main content) -->
        <div v-if="isMobile && !selectedCategory && !selectedGoal && !isEditing" class="col-12 q-mt-md">
          <q-card class="column q-pa-none budget-transactions-card" >
            <div class="row items-center justify-between q-pt-md q-px-md q-pb-sm border-bottom">
              <div class="row items-center q-gutter-xs">
                <q-icon name="receipt_long" size="20px" color="primary" />
                <div>
                  <div class="text-body2 text-weight-medium">Transactions</div>
                  <div class="text-caption text-muted">{{ formatLongMonth(currentMonth) }}</div>
                </div>
              </div>
              <div class="text-right">
                <div class="text-h6">{{ filteredMonthTransactions.length }}</div>
                <div class="text-caption text-muted">{{ filteredMonthTransactions.length === 1 ? 'item' : 'items' }}</div>
              </div>
            </div>

            <q-card-section class="q-pt-sm q-px-md q-pb-sm">
              <div class="budget-tx-toggle">
                <q-btn-toggle
                  v-model="transactionFilter"
                  unelevated
                  spread
                  no-caps
                  rounded
                  toggle-color="primary"
                  color="grey-3"
                  text-color="dark"
                  :options="transactionFilterOptions"
                />
              </div>
            </q-card-section>

            <q-card-section class="q-pt-none q-px-md q-pb-xs">
              <q-input
                v-model="transactionSearch"
                dense
                rounded
                outlined
                clearable
                label="Search transactions"
                prepend-icon="search"
                @clear="transactionSearch = ''"
              />
            </q-card-section>

            <q-separator />

            <q-card-section class="q-pt-none q-px-md q-pb-md">
              <q-list separator class="q-pa-none">
                <BudgetTransactionItem
                  v-for="transaction in filteredMonthTransactions"
                  :key="transaction.id"
                  :transaction="transaction"
                  :goal="goalMap[transaction.fundedByGoalId]"
                  removable
                  @select="editTransaction"
                  @delete="confirmDeleteTransaction"
                />
              </q-list>
              <div v-if="!filteredMonthTransactions.length" class="text-center text-grey-6 q-pt-md">
                {{ transactionEmptyLabel }}
              </div>
            </q-card-section>
          </q-card>
        </div>

        <!-- Desktop Sidebar -->
        <div v-if="!isMobile" class="col-12 col-lg-4 desktop-sidebar">
          <CategoryTransactions
            v-if="selectedCategory && !isEditing"
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
          <GoalDetailsPanel v-else-if="selectedGoal && !isEditing" :goal="selectedGoal" @close="selectedGoal = null" />
          <template v-else>
            <q-card class="column q-pa-none budget-transactions-card full-height" >
              <div class="row items-center justify-between q-pt-md q-px-md q-pb-sm border-bottom">
                <div class="row items-center q-gutter-xs">
                  <q-icon name="receipt_long" size="20px" color="primary" />
                  <div>
                    <div class="text-body2 text-weight-medium">Transactions</div>
                    <div class="text-caption text-muted">{{ formatLongMonth(currentMonth) }}</div>
                  </div>
                </div>
                <div class="text-right">
                  <div class="text-h6">{{ filteredMonthTransactions.length }}</div>
                  <div class="text-caption text-muted">{{ filteredMonthTransactions.length === 1 ? 'item' : 'items' }}</div>
                </div>
              </div>

              <q-card-section class="q-pt-sm q-px-md q-pb-sm">
                <div class="budget-tx-toggle">
                  <q-btn-toggle
                    v-model="transactionFilter"
                    unelevated
                    spread
                    no-caps
                    toggle-color="primary"
                    color="grey-3"
                    text-color="dark"
                    :options="transactionFilterOptions"
                  />
                </div>
              </q-card-section>

              <q-card-section class="q-pt-none q-px-md q-pb-xs">
                <q-input
                  v-model="transactionSearch"
                  dense
                  rounded
                  outlined
                  clearable
                  label="Search transactions"
                  prepend-icon="search"
                  @clear="transactionSearch = ''"
                />
              </q-card-section>

              <q-separator />

              <q-card-section class="q-pt-none q-px-md q-pb-md transactions-panel">
                <q-scroll-area class="transactions-scroll">
                    <q-list separator class="q-pa-none">
                    <BudgetTransactionItem
                      v-for="transaction in filteredMonthTransactions"
                      :key="transaction.id"
                      :transaction="transaction"
                      :goal="goalMap[transaction.fundedByGoalId]"
                      removable
                      @select="editTransaction"
                      @delete="confirmDeleteTransaction"
                    />
                  </q-list>
                </q-scroll-area>
                <div v-if="!filteredMonthTransactions.length" class="text-center text-grey-6 q-pt-md">
                  {{ transactionEmptyLabel }}
                </div>
              </q-card-section>
            </q-card>
          </template>
        </div>
      </div>

      <!-- Version Info -->
      <div class="row q-mt-md">
        <div class="col-auto">
          <div class="text-caption text-center">{{ `Version: ${appVersion}` }}</div>
        </div>
      </div>

      <TransactionDialog
        :key="transactionDialogKey"
        :show-dialog="showTransactionDialog"
        :initial-transaction="newTransaction"
        :category-options="categoryOptions"
        :budget-id="budgetId"
        :user-id="userId"
        :title="isIncomeTransaction ? 'Add Income' : 'Add Transaction'"
        @update:showDialog="showTransactionDialog = $event"
        @save="onTransactionSaved"
        @cancel="showTransactionDialog = false"
        @update-transactions="updateTransactions"
      />
      <q-dialog v-model="showDeleteTransactionDialog" max-width="400">
        <q-card>
          <q-card-section class="text-h6">Delete transaction</q-card-section>
          <q-card-section>
            Are you sure you want to delete the transaction for
            <strong>{{ transactionToDelete?.merchant || 'this merchant' }}</strong>?
          </q-card-section>
          <q-card-actions align="right">
            <q-space />
            <q-btn color="grey" flat label="Cancel" @click="showDeleteTransactionDialog = false" />
            <q-btn color="negative" flat label="Move to Trash" @click="executeTransactionDelete" />
          </q-card-actions>
        </q-card>
      </q-dialog>
      <GoalDialog v-model="goalDialog" :goal="selectedGoal || undefined" @save="saveGoal" />
      <ContributeDialog
        v-model="contributeDialog"
        :goal="selectedGoal || undefined"
        :category-options="categoryOptions"
        @save="saveContribution"
      />
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick, onUnmounted } from 'vue';
import { useQuasar, QSpinner, Loading } from 'quasar';
import { dataAccess } from '../dataAccess';
import CurrencyInput from '../components/CurrencyInput.vue';
import CategoryTransactions from '../components/CategoryTransactions.vue';
import TransactionDialog from '../components/TransactionDialog.vue';
import EntitySelector from '../components/EntitySelector.vue';
import GoalsGroupCard from '../components/goals/GoalsGroupCard.vue';
import GoalDialog from '../components/goals/GoalDialog.vue';
import ContributeDialog from '../components/goals/ContributeDialog.vue';
import GoalDetailsPanel from '../components/goals/GoalDetailsPanel.vue';
import MonthSelector from '../components/MonthSelector.vue';
import BudgetTransactionItem from '../components/BudgetTransactionItem.vue';
import GuidedTip from '../components/GuidedTip.vue';
import type { Transaction, Budget, IncomeTarget, BudgetCategoryTrx, BudgetCategory, Goal, BudgetGroupKind } from '../types';
import { EntityType } from '../types';
import { isIncomeCategory, categoryGroupName } from '../utils/groups';
import version from '../version';
import { toDollars, toCents, formatCurrency, todayISO, currentMonthISO } from '../utils/helpers';
import { useAuthStore } from '../store/auth';
import { useBudgetStore } from '../store/budget';
import { useMerchantStore } from '../store/merchants';
import { useFamilyStore } from '../store/family';
import debounce from 'lodash/debounce';
import { v4 as uuidv4 } from 'uuid';
import { createBudgetForMonth } from '../utils/budget';
import { useGoals } from '../composables/useGoals';

// Structured logger for this page
const DBG = '[Budget]';
function log(...args: unknown[]) {
  console.log(DBG, ...args);
}

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
const selectedEntityMonthSet = computed(() => new Set(availableBudgets.value.filter(matchesSelectedEntity).map((b) => b.month)));
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
const futureCategories = ref<BudgetCategory[]>([]);
const saving = ref(false);
const isEditing = ref(false);
const showTransactionDialog = ref(false);
const transactionDialogKey = ref(0);
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
const { monthlySavingsTotal, createGoal, listGoals, loadGoals, loadGoalDetails } = useGoals();
const savingsTotal = ref(0);
const goals = ref<Goal[]>([]);
const goalMap = computed(() => Object.fromEntries(goals.value.map((g) => [g.id, g])));
const goalDialog = ref(false);
const contributeDialog = ref(false);
const selectedGoal = ref<Goal | null>(null);
const convertingCategory = ref<BudgetCategory | null>(null);
const ownerUid = ref<string | null>(null);
// Prefer the actual budget's id if present; otherwise fall back to derived id for new budgets
const budgetId = computed(() => {
  if (budget.value?.budgetId) return budget.value.budgetId;
  if (!ownerUid.value || !familyStore.selectedEntityId) return '';
  return `${ownerUid.value}_${familyStore.selectedEntityId}_${currentMonth.value}`;
});
const userId = computed(() => auth.user?.uid || '');
const selectedCategory = ref<BudgetCategory | null>(null);
const showMobileCategoryDialog = ref(false);
const showMobileGoalDialog = ref(false);
const newMerchantName = ref('');
const searchInput = ref(null);
const search = ref('');
const debouncedSearch = ref('');
const transactionSearch = ref('');
type TransactionFilterKey = 'matched' | 'unmatched' | 'deleted';

const transactionFilter = ref<TransactionFilterKey>('unmatched');
const transactionToDelete = ref<Transaction | null>(null);
const showDeleteTransactionDialog = ref(false);

watch(
  [() => familyStore.selectedEntityId, currentMonth],
  () => {
    if (familyStore.selectedEntityId) {
      goals.value = listGoals(familyStore.selectedEntityId);
      savingsTotal.value = monthlySavingsTotal(familyStore.selectedEntityId, currentMonth.value);
      // Hydrate the entity's group taxonomy for templates/drag-reorder/etc.
      void familyStore.loadGroups(familyStore.selectedEntityId);
    } else {
      savingsTotal.value = 0;
      goals.value = [];
    }
  },
  { immediate: true },
);

watch(goalDialog, (v) => {
  if (!v) convertingCategory.value = null;
});

let clickTimeout: ReturnType<typeof setTimeout> | null = null;
let touchTimeout: ReturnType<typeof setTimeout> | null = null;

function onAddGoal() {
  goalDialog.value = true;
  selectedGoal.value = null;
}

function onContribute(goal: Goal) {
  selectedGoal.value = goal;
  contributeDialog.value = true;
}

// --- Group drag-and-drop reordering ---
// Order is persisted on `budget_groups.sort_order` per entity. Reordering
// here therefore propagates to every month for the entity automatically.
const dragFromIndex = ref<number | null>(null);
const dragOverIndex = ref<number | null>(null);

function onGroupDragStart(idx: number, e: DragEvent) {
  dragFromIndex.value = idx;
  if (e.dataTransfer) {
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', String(idx));
  }
}

function onGroupDragOver(idx: number) {
  if (dragFromIndex.value !== null && dragFromIndex.value !== idx) {
    dragOverIndex.value = idx;
  }
}

function onGroupDragLeave() {
  dragOverIndex.value = null;
}

async function onGroupDrop(toIdx: number) {
  const fromIdx = dragFromIndex.value;
  dragOverIndex.value = null;
  dragFromIndex.value = null;
  if (fromIdx === null || fromIdx === toIdx) return;

  const entityId = familyStore.selectedEntityId;
  if (!entityId) return;

  // Build the new id ordering by reordering the visible (non-income) groups
  // shown on this page, then appending the rest of the entity's groups (income,
  // archived) in their existing order so the full taxonomy stays sorted.
  const visibleIds = groups.value.map((g) => g.id);
  const [movedId] = visibleIds.splice(fromIdx, 1);
  visibleIds.splice(toIdx, 0, movedId);

  const allOrdered = [
    ...visibleIds,
    ...familyStore.currentGroups
      .filter((g) => !visibleIds.includes(g.id))
      .map((g) => g.id),
  ];

  try {
    await familyStore.reorderGroups(entityId, allOrdered);
  } catch (err) {
    console.error('Failed to save group order', err);
  }
}

function onGroupDragEnd() {
  dragFromIndex.value = null;
  dragOverIndex.value = null;
}

// --- Inline group rename ---
// Double-click a group header in the BudgetPage to rename. Persists via
// familyStore.renameGroup, which writes the new name to budget_groups for
// the entity — so the rename takes effect across every month immediately
// (no per-month edit needed).
const groupRename = ref<{ id: string; value: string }>({ id: '', value: '' });

function startGroupRename(g: { id: string; name: string }) {
  groupRename.value = { id: g.id, value: g.name };
}

function cancelGroupRename() {
  groupRename.value = { id: '', value: '' };
}

async function saveGroupRename() {
  const { id, value } = groupRename.value;
  if (!id) return;
  const trimmed = (value ?? '').trim();
  const entityId = familyStore.selectedEntityId;
  if (!entityId) {
    cancelGroupRename();
    return;
  }
  const original = familyStore.getGroup(entityId, id);
  // No-op if blank or unchanged.
  if (!trimmed || (original && trimmed === original.name)) {
    cancelGroupRename();
    return;
  }
  // Reject collisions with another existing group on the same entity.
  const collision = familyStore
    .currentGroups
    .find((g) => g.id !== id && g.name.toLowerCase() === trimmed.toLowerCase());
  if (collision) {
    showSnackbar(`A group named "${collision.name}" already exists on this entity.`, 'negative');
    cancelGroupRename();
    return;
  }
  try {
    await familyStore.renameGroup(entityId, id, trimmed);
    showSnackbar('Group renamed.', 'success');
  } catch (err) {
    console.error('Failed to rename group', err);
    showSnackbar('Failed to rename group.', 'negative');
  } finally {
    cancelGroupRename();
  }
}

// --- Per-category drag-and-drop reordering ---
// Within a group, plus cross-group drag (drop on a different group's category
// row reassigns the dragged category to that group). Persisted via the new
// PUT /api/budget/{budgetId}/categories/order endpoint.
const catDragFrom = ref<{ groupId: string; idx: number; item: BudgetCategoryTrx } | null>(null);
const catDragOver = ref<{ groupId: string; idx: number } | null>(null);

function onCategoryDragStart(groupId: string, idx: number, item: BudgetCategoryTrx, e: DragEvent) {
  catDragFrom.value = { groupId, idx, item };
  if (e.dataTransfer) {
    e.dataTransfer.effectAllowed = 'move';
    e.dataTransfer.setData('text/plain', `cat::${item.name}`);
  }
}

function onCategoryDragOver(groupId: string, idx: number) {
  if (!catDragFrom.value) return;
  // Don't show drop target on yourself.
  if (catDragFrom.value.groupId === groupId && catDragFrom.value.idx === idx) return;
  catDragOver.value = { groupId, idx };
}

function onCategoryDragLeave() {
  catDragOver.value = null;
}

function onCategoryDragEnd() {
  catDragFrom.value = null;
  catDragOver.value = null;
}

async function onCategoryDrop(toGroupId: string, toIdx: number) {
  const from = catDragFrom.value;
  catDragOver.value = null;
  catDragFrom.value = null;
  if (!from) return;
  if (from.groupId === toGroupId && from.idx === toIdx) return;
  if (!budgetId.value) return;

  // Build the new ordering for the affected groups, then send a single
  // payload with every (id, group_id, sort_order) we want to apply. The
  // backend ReorderCategories executes them transactionally.
  type Row = { id: number; groupId: string; sortOrder: number };
  const rowsByGroup = new Map<string, BudgetCategoryTrx[]>();
  for (const c of catTransactions.value) {
    const gid = c.groupId ?? '';
    let bucket = rowsByGroup.get(gid);
    if (!bucket) {
      bucket = [];
      rowsByGroup.set(gid, bucket);
    }
    bucket.push(c);
  }
  // Sort each bucket by current order so we splice in a stable way.
  for (const list of rowsByGroup.values()) {
    list.sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0) || a.name.localeCompare(b.name));
  }

  const fromList = rowsByGroup.get(from.groupId) || [];
  const fromIndexInList = fromList.findIndex((c) => c.name === from.item.name);
  if (fromIndexInList === -1) return;
  const [moved] = fromList.splice(fromIndexInList, 1);

  let toList = rowsByGroup.get(toGroupId);
  if (!toList) {
    toList = [];
    rowsByGroup.set(toGroupId, toList);
  }
  // Clamp toIdx for cross-group inserts (the original drop index assumed the
  // to-group still had the dragged row; for cross-group it doesn't).
  const insertIdx = Math.min(Math.max(toIdx, 0), toList.length);
  toList.splice(insertIdx, 0, { ...moved, groupId: toGroupId });

  // Build the payload: only categories whose group or order changed need
  // to be updated, but to keep the math simple we recompute every row in
  // the two affected groups.
  const payload: Row[] = [];
  const affected = new Set([from.groupId, toGroupId]);
  for (const gid of affected) {
    const list = rowsByGroup.get(gid) || [];
    list.forEach((c, i) => {
      if (typeof c.id !== 'number') return; // skip in-flight new rows without a PK
      payload.push({ id: c.id, groupId: gid, sortOrder: i });
    });
  }
  if (payload.length === 0) return;

  // Optimistically reflect the change in the in-memory budget so the table
  // reorders immediately, then persist.
  budget.value.categories = budget.value.categories.map((cat) => {
    if (typeof cat.id !== 'number') return cat;
    const updated = payload.find((p) => p.id === cat.id);
    if (!updated) return cat;
    return { ...cat, groupId: updated.groupId, sortOrder: updated.sortOrder };
  });
  if (budgetId.value) budgetStore.updateBudget(budgetId.value, { ...budget.value });

  try {
    await dataAccess.reorderCategories(budgetId.value, payload);
  } catch (err) {
    console.error('Failed to save category order', err);
    showSnackbar('Failed to save category order', 'negative');
  }
}

async function toggleFavorite(item: BudgetCategoryTrx) {
  const idx = budget.value.categories.findIndex((c) => c.name === item.name);
  if (idx === -1) return;
  const updated = { ...budget.value.categories[idx], favorite: !budget.value.categories[idx].favorite };
  budget.value.categories.splice(idx, 1, updated);
  try {
    if (budgetId.value) {
      budget.value.budgetId = budgetId.value;
      await dataAccess.saveBudget(budgetId.value, budget.value);
      // Keep local and store in sync so favorites re-render consistently
      budgetStore.updateBudget(budgetId.value, { ...budget.value });
      const b = await dataAccess.getBudget(budgetId.value);
      if (b) {
        budget.value = b;
        budgetStore.updateBudget(budgetId.value, b);
      }
    }
  } catch (err) {
    console.error('Failed to save favorite toggle', err);
  }
}

async function onViewGoal(goal: Goal) {
  console.log('onViewGoal clicked', goal);
  selectedGoal.value = goal;
  selectedCategory.value = null;
  if (isMobile.value) {
    showMobileGoalDialog.value = true;
  }
  try {
    await loadGoalDetails(goal.id);
    console.log('Loaded goal details for', goal.id);
  } catch (err) {
    // Failing to load details shouldn't block the panel from opening
    console.error('Failed to load goal details', err);
  }
}

function onConvertLegacy(cat: BudgetCategory) {
  convertingCategory.value = cat;
  selectedGoal.value = { name: cat.name, monthlyTarget: cat.target } as Goal;
  goalDialog.value = true;
}

async function saveGoal(data: Partial<Goal>) {
  if (convertingCategory.value) {
    await convertLegacyCategory(convertingCategory.value, data);
    convertingCategory.value = null;
    // Reload budgets so category lists reflect the conversion without a manual refresh
    await loadBudgets();
  } else {
    await createGoal({ ...data, entityId: familyStore.selectedEntityId || '' });
  }
  goalDialog.value = false;
  if (familyStore.selectedEntityId) {
    await loadGoals(familyStore.selectedEntityId);
    goals.value = listGoals(familyStore.selectedEntityId);
    savingsTotal.value = monthlySavingsTotal(familyStore.selectedEntityId, currentMonth.value);
  }
}

async function saveContribution(payload: { amount: number; note?: string; sourceCategory: string }) {
  const goal = selectedGoal.value;
  if (!goal) return;
  const { amount, note, sourceCategory } = payload;
  if (!amount || amount <= 0) {
    showSnackbar('Contribution amount must be greater than 0', 'negative');
    return;
  }

  try {
    // Make sure a budget exists for the current month/entity so the transfer has a home
    let targetBudget: Budget | null = null;
    if (familyStore.family && familyStore.selectedEntityId) {
      targetBudget = await createBudgetForMonth(
        currentMonth.value,
        familyStore.family.id,
        userId.value,
        familyStore.selectedEntityId,
      );
    } else if (budget.value.budgetId) {
      targetBudget = budget.value;
    }

    if (!targetBudget || !targetBudget.budgetId) {
      showSnackbar('Unable to save contribution: no budget for this month.', 'negative');
      return;
    }

    const tx: Transaction = {
      id: uuidv4(),
      budgetId: targetBudget.budgetId,
      date: todayISO(),
      budgetMonth: currentMonth.value,
      merchant: note && note.trim().length > 0 ? note.trim() : 'Contribution',
      categories: [
        { category: sourceCategory, amount: -amount },
        { category: goal.name, amount: amount },
      ],
      amount,
      notes: note || '',
      recurring: false,
      recurringInterval: 'Monthly',
      userId: userId.value,
      familyId: familyStore.family?.id,
      isIncome: false,
      taxMetadata: [],
      entityId: familyStore.selectedEntityId || undefined,
      transactionType: 'transfer',
    };

    const saved = await dataAccess.saveTransaction(targetBudget, tx);
    // Keep the in-memory budget in sync so category math updates immediately
    const existingIdx = targetBudget.transactions.findIndex((t) => t.id === saved.id);
    if (existingIdx >= 0) {
      targetBudget.transactions[existingIdx] = saved;
    } else {
      targetBudget.transactions.push(saved);
    }
    if (targetBudget.budgetId) {
      budgetStore.updateBudget(targetBudget.budgetId, { ...targetBudget });
    }

    contributeDialog.value = false;
    showSnackbar('Contribution saved', 'success');

    if (familyStore.selectedEntityId) {
      // Pull fresh savedToDate/spentToDate for all goals (backend derives from transactions)
      await loadGoals(familyStore.selectedEntityId);
      goals.value = listGoals(familyStore.selectedEntityId);
      savingsTotal.value = monthlySavingsTotal(familyStore.selectedEntityId, currentMonth.value);
      // Refresh the details panel cache for this goal so the contribution list updates
      try {
        await loadGoalDetails(goal.id);
      } catch (err) {
        console.warn('Failed to refresh goal details after contribution', err);
      }
    }
  } catch (err) {
    console.error('Failed to save contribution', err);
    showSnackbar('Failed to save contribution', 'negative');
  }
}

async function convertLegacyCategory(cat: BudgetCategory, data: Partial<Goal>) {
  document.body.style.cursor = 'progress';
  Loading.show({ message: 'Converting savings category to goal…' });
  try {
    // The backend links any existing budget_categories sharing the goal's name
    // via goals_budget_categories, so historical transactions automatically
    // roll up into the goal's savedToDate/spentToDate — no client-side
    // backfill required.
    await createGoal({
      ...data,
      name: data.name || cat.name,
      monthlyTarget: data.monthlyTarget ?? cat.target,
      entityId: familyStore.selectedEntityId || '',
    });

    if (budget.value.categories) {
      categoryOptions.value = budget.value.categories.map((c) => c.name);
      if (!categoryOptions.value.includes('Income')) {
        categoryOptions.value.push('Income');
      }
    }
  } finally {
    Loading.hide();
    document.body.style.cursor = '';
  }
}

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
  },
});

const isMobile = computed(() => $q.screen.lt.md);

const selectedEntity = computed(() => {
  return familyStore.family?.entities?.find((e) => e.id === familyStore.selectedEntityId);
});

function matchesSelectedEntity(b: Budget) {
  // If no specific entity is selected, allow any
  if (!familyStore.selectedEntityId) return true;
  if (b.entityId) return b.entityId === familyStore.selectedEntityId;
  // Some legacy family budgets may have no entityId; treat them as the Family entity
  return selectedEntity.value?.type === EntityType.Family;
}

const budgetedExpenses = computed(() => {
  const groupList = familyStore.currentGroups;
  const totalPlanned = budget.value.categories
    .filter((cat) => !isIncomeCategory(cat, groupList))
    .reduce((sum, cat) => sum + (cat.target || 0), 0);
  return totalPlanned;
});

const remainingToBudget = computed(() => {
  return actualIncome.value - budgetedExpenses.value - savingsTotal.value;
});

const formatLongMonth = (month: string) => {
  const [year, monthNum] = month.split('-');
  const date = new Date(parseInt(year), parseInt(monthNum) - 1);
  return date.toLocaleString('en-US', { month: 'long', year: 'numeric' });
};

const budgetLabel = computed(() => formatLongMonth(currentMonth.value));

const isViewingCurrentMonth = computed(() => currentMonth.value === currentMonthISO());

const monthOffsetLabel = computed(() => {
  if (isViewingCurrentMonth.value) return '';
  const now = currentMonthISO();
  const [nowY, nowM] = now.split('-').map(Number);
  const [selY, selM] = currentMonth.value.split('-').map(Number);
  const diff = (selY - nowY) * 12 + (selM - nowM);
  const abs = Math.abs(diff);
  if (diff < 0) return `${abs} month${abs > 1 ? 's' : ''} ago`;
  return `${abs} month${abs > 1 ? 's' : ''} from now`;
});

async function returnToCurrentMonth() {
  await selectMonth(currentMonthISO());
}

const catTransactions = computed(() => {
  const catTransactions: BudgetCategoryTrx[] = [];
  const groupList = familyStore.currentGroups;
  if (budget.value && budget.value.categories) {
    budget.value.categories.forEach((c) => {
      if (!isIncomeCategory(c, groupList)) {
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
              transactionType: t.transactionType,
              categories: t.categories,
            });
            if (t.transactionType === 'transfer') {
              // Transfers store signed splits: positive = inflow to this
              // category (reduces spent / adds to available), negative = outflow.
              catTransactions[i].spent -= tc.amount;
              catTransactions[i].remaining += tc.amount;
            } else if (t.isIncome) {
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
    const groupList = familyStore.currentGroups;
    return catTransactions.filter((t) => {
      const gName = categoryGroupName(t, groupList).toLowerCase();
      return gName.includes(srch) || t.name.toLowerCase().includes(srch);
    });
  }
  return catTransactions;
});

// Favorite categories (non-income), sorted A→Z by name
const favoriteItems = computed(() =>
  catTransactions.value
    .filter((t) => t.favorite)
    .slice()
    .sort((a, b) => a.name.toLowerCase().localeCompare(b.name.toLowerCase())),
);


// Visible (non-income) groups for this budget, sourced from the entity's
// canonical taxonomy (`familyStore.currentGroups`) and filtered to those that
// have at least one category present in the current budget. Order comes from
// the entity-scoped `sortOrder` on each BudgetGroup.
const groups = computed<GroupCategory[]>(() => {
  const groupList = familyStore.currentGroups;
  if (!groupList.length) return [];
  const presentGroupIds = new Set<string>();
  for (const c of catTransactions.value) {
    if (c.groupId) presentGroupIds.add(c.groupId);
  }
  return groupList
    .filter((g) => g.kind !== 'income' && !g.archived && presentGroupIds.has(g.id))
    .slice()
    .sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name))
    .map((g) => ({
      id: g.id,
      name: g.name,
      kind: g.kind,
      cat: catTransactions.value.filter((c) => c.groupId === g.id).map((c) => c.name),
    }));
});

// Names of every non-income group available on the entity. Used by the inline
// category-row group select so users can pick existing groups or type a new
// one (which the backend upserts on save).
const groupNameOptions = computed<string[]>(() =>
  familyStore.currentGroups
    .filter((g) => !g.archived)
    .map((g) => g.name)
    .sort((a, b) => a.localeCompare(b)),
);

function onCategoryGroupChange(cat: BudgetCategory, value: string | null) {
  const trimmed = (value ?? '').trim();
  cat.groupName = trimmed;
  // Clear groupId so SaveBudget routes through EnsureGroupAsync(name) — that
  // handles both "switch to existing group by name" and "create new group".
  // SaveBudget upserts and writes the resolved group_id.
  const existing = familyStore.getGroupByName(familyStore.selectedEntityId, trimmed);
  cat.groupId = existing?.id;
}

const incomeItems = computed(() => {
  const incTrx: IncomeTarget[] = [];
  const groupList = familyStore.currentGroups;
  if (budget.value && budget.value.categories) {
    budget.value.categories.forEach((c) => {
      if (isIncomeCategory(c, groupList)) {
        incTrx.push({
          name: c.name,
          group: categoryGroupName(c, groupList),
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

const monthlyTransactions = computed(() => {
  const transactions = budget.value?.transactions ?? [];
  const monthPrefix = `${currentMonth.value}-`;
  return transactions
    .filter((tx) => !tx.date || tx.date.startsWith(monthPrefix))
    .slice()
    .sort((a, b) => {
      const dateCompare = (b.date || '').localeCompare(a.date || '');
      if (dateCompare !== 0) {
        return dateCompare;
      }
      const merchantA = a.merchant || '';
      const merchantB = b.merchant || '';
      return merchantA.localeCompare(merchantB);
    });
});

const transactionCounts = computed<Record<TransactionFilterKey, number>>(() => ({
  unmatched: monthlyTransactions.value.filter((tx) => !tx.deleted && tx.status === 'U').length,
  matched: monthlyTransactions.value.filter((tx) => !tx.deleted && (tx.status === 'C' || tx.status === 'R')).length,
  deleted: monthlyTransactions.value.filter((tx) => !!tx.deleted).length,
}));

const transactionFilterOptions = computed(() => [
  { label: `Unmatched (${transactionCounts.value.unmatched})`, value: 'unmatched' },
  { label: `Matched (${transactionCounts.value.matched})`, value: 'matched' },
  { label: `Deleted (${transactionCounts.value.deleted})`, value: 'deleted' },
]);

watch(
  transactionCounts,
  (counts) => {
    if (counts[transactionFilter.value] > 0) {
      return;
    }

    const prioritizedFilters: TransactionFilterKey[] = ['unmatched', 'matched', 'deleted'];
    const nextFilter = prioritizedFilters.find((key) => counts[key] > 0);

    if (nextFilter) {
      transactionFilter.value = nextFilter;
    }
  },
  { immediate: true },
);

const filteredMonthTransactions = computed(() => {
  const searchTerm = transactionSearch.value.trim().toLowerCase();
  return monthlyTransactions.value.filter((tx) => {
    if (transactionFilter.value === 'unmatched' && (tx.status !== 'U' || tx.deleted)) {
      return false;
    }
    if (transactionFilter.value === 'matched' && (tx.deleted || (tx.status !== 'C' && tx.status !== 'R'))) {
      return false;
    }
    if (transactionFilter.value === 'deleted' && !tx.deleted) {
      return false;
    }
    if (!searchTerm) {
      return true;
    }
    const merchant = tx.merchant?.toLowerCase() ?? '';
    const notes = tx.notes?.toLowerCase() ?? '';
    const categories = tx.categories?.map((c) => c.category.toLowerCase()).join(' ') ?? '';
    return merchant.includes(searchTerm) || notes.includes(searchTerm) || categories.includes(searchTerm);
  });
});

const transactionEmptyLabel = computed(() => {
  if (transactionFilter.value === 'unmatched') {
    return 'No unmatched transactions this month.';
  }
  if (transactionFilter.value === 'deleted') {
    return 'No deleted transactions this month.';
  }
  return 'No matched transactions for this month yet.';
});

function monthExists(month: string) {
  return selectedEntityMonthSet.value.has(month);
}

function onIncomeRowClick(item: IncomeTarget) {
  const t = getCategoryInfo(item.name);
  selectedCategory.value = t;
  selectedGoal.value = null;
  if (isMobile.value) {
    showMobileCategoryDialog.value = true;
  }
}

function onCategoryRowClick(item: BudgetCategoryTrx) {
  selectedCategory.value = getCategoryInfo(item.name);
  selectedGoal.value = null;
  if (isMobile.value) {
    showMobileCategoryDialog.value = true;
  }
}

function closeMobileCategoryDialog() {
  showMobileCategoryDialog.value = false;
  selectedCategory.value = null;
}

function closeMobileGoalDialog() {
  showMobileGoalDialog.value = false;
  selectedGoal.value = null;
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
  async (val, oldVal) => {
    log('selectedEntityId changed', { from: oldVal, to: val });
    await loadBudgets();
  },
);

const updateBudgetForMonth = debounce(async () => {
  log('updateBudgetForMonth start', {
    selectedEntityId: familyStore.selectedEntityId,
    currentMonth: currentMonth.value,
    budgetsCount: budgets.value.length,
  });
  if (!familyStore.selectedEntityId) {
    log('No selected entity; resetting empty budget model');
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

  const defaultBudget = budgets.value.find((b) => b.month === currentMonth.value && matchesSelectedEntity(b));
  if (defaultBudget) {
    log('Found budget for current month/entity', { month: currentMonth.value, entityId: familyStore.selectedEntityId });
    const family = await familyStore.getFamily();
    if (family) {
      ownerUid.value = family.ownerUid;
    } else {
      console.error('No family found for user');
      ownerUid.value = userId.value;
    }

    // Always load the full budget so transactions are available
    const key = defaultBudget.budgetId || budgetId.value;
    const fullBudget = await dataAccess.getBudget(key);
    if (fullBudget) {
      const normalized = {
        ...fullBudget,
        budgetId: key,
        transactions: fullBudget.transactions || [],
        categories: fullBudget.categories && fullBudget.categories.length > 0 ? fullBudget.categories : [],
      };
      budget.value = normalized;
      budgetStore.updateBudget(key, normalized);
    } else {
      // Fallback to accessible budget if full fetch fails
      budget.value = {
        ...defaultBudget,
        budgetId: key,
        transactions: defaultBudget.transactions || [],
        categories: defaultBudget.categories && defaultBudget.categories.length > 0 ? defaultBudget.categories : [],
      };
    }

    categoryOptions.value = (budget.value.categories || []).map((cat) => cat.name);
    if (!categoryOptions.value.includes('Income')) {
      categoryOptions.value.push('Income');
    }
  } else if (isInitialLoad.value && budgets.value.length > 0) {
    log('No current-month budget; picking most recent for entity');
    const sortedBudgets = budgets.value
      .filter((b) => b.entityId === familyStore.selectedEntityId)
      .sort((a, b) => {
        const dateA = new Date(a.month);
        const dateB = new Date(b.month);
        return dateB.getTime() - dateA.getTime();
      });
    const mostRecentBudget = sortedBudgets[0];
    if (mostRecentBudget) {
      log('Switching to most recent budget', mostRecentBudget.month);
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
    } else {
      log('No budgets exist for selected entity');
    }
  }
}, 300);

watch(
  () => budgetStore.budgets,
  (newBudgets) => {
    log('Budget store changed', { size: (newBudgets as Map<string, Budget>).size });
    budgets.value = Array.from(newBudgets.values());
    availableBudgets.value = budgets.value;
  },
  { deep: true },
);

watch(currentMonth, () => {
  log('currentMonth changed', currentMonth.value);
  updateBudgetForMonth();
});

watch(
  () => budgetId.value,
  (val, oldVal) => {
    log('budgetId changed', {
      from: oldVal,
      to: val,
      ownerUid: ownerUid.value,
      selectedEntityId: familyStore.selectedEntityId,
      currentMonth: currentMonth.value,
    });
  },
);

onMounted(async () => {
  log('Mounted: Checking auth state', { uid: auth.user?.uid, email: auth.user?.email });
  loading.value = true;

  try {
    // User should be guaranteed by route guard
    if (!auth.user) {
      console.error('No user found despite route guard');
      showSnackbar('Authentication error: No user found', 'negative');
      loading.value = false;
      return;
    }

    if (auth.authError) {
      console.error('Auth error:', auth.authError);
      showSnackbar(`Authentication error: ${auth.authError}`, 'negative');
      loading.value = false;
      return;
    }

    loadingTimeout = setTimeout(() => {
      showLoadingMessage.value = true;
      log('Loading timeout triggered');
    }, 5000);

    log('Loading family for user', auth.user.uid);
    await familyStore.loadFamily(auth.user.uid);
    log('Family loaded', {
      familyId: familyStore.family?.id,
      entities: familyStore.family?.entities?.length || 0,
      selectedEntityId: familyStore.selectedEntityId,
    });
    if (familyStore.selectedEntityId) {
      await familyStore.loadGroups(familyStore.selectedEntityId);
    }
    log('Loading budgets');
    await loadBudgets();
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Initialization error:', err);
    showSnackbar(`Error loading data: ${err.message}`, 'negative');
  } finally {
    if (loadingTimeout) clearTimeout(loadingTimeout);
    showLoadingMessage.value = false;
    loading.value = false;
    isInitialLoad.value = false;
  }
});

onUnmounted(() => {
  log('Unmount: cleaning up subscriptions and timers');
  budgetStore.unsubscribeAll();
  if (loadingTimeout) clearTimeout(loadingTimeout);
});

async function loadBudgets() {
  const user = auth.user;
  if (!user) {
    log('No user for loading budgets');
    return;
  }

  loading.value = true;
  try {
    log('Loading budgets for user', { uid: user.uid, entityId: familyStore.selectedEntityId });
    await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);
    log('Budgets loaded', {
      total: budgetStore.budgets.size,
      months: Array.from(budgetStore.budgets.values()).map((b) => b.month),
    });
    await nextTick();
    updateBudgetForMonth();
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error loading budgets:', err);
    showSnackbar(`Error loading budgets: ${err.message}`, 'negative');
  } finally {
    loading.value = false;
  }
}

async function createDefaultBudget() {
  const user = auth.user;
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
    showSnackbar('Budget created successfully', 'success');
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Failed to create budget: ${err.message}`, 'negative');
  } finally {
    loading.value = false;
  }
}

function confirmDeleteBudget() {
  const id = budgetId.value;
  const month = currentMonth.value;
  if (!id) {
    showSnackbar('No budget selected to delete', 'negative');
    return;
  }

  $q.dialog({
    title: 'Delete Budget',
    message: `Are you sure you want to delete the budget for ${formatLongMonth(month)}? This cannot be undone.`,
    cancel: { label: 'Cancel' },
    persistent: true,
    ok: { label: 'Delete', color: 'negative' },
  }).onOk(() => {
    void (async () => {
      try {
        await dataAccess.deleteBudget(id);
        budgetStore.removeBudget(id);

        // Determine nearest available month with a budget for the selected entity
        const months = Array.from(budgetStore.budgets.values())
          .filter((b) => matchesSelectedEntity(b))
          .map((b) => b.month);

        const target = findNearestMonth(month, months);
        if (target) {
          await selectMonth(target);
        } else {
          // No budgets left; clear current view
          budget.value = {
            familyId: familyStore.family?.id || '',
            month: currentMonthISO(),
            incomeTarget: 0,
            categories: [],
            transactions: [],
            label: '',
            merchants: [],
          } as Budget;
        }
        showSnackbar('Budget deleted', 'positive');
      } catch (err) {
        const e = err as Error;
        console.error('Failed to delete budget', e);
        showSnackbar(`Failed to delete budget: ${e.message}`, 'negative');
      }
    })();
  });
}

function findNearestMonth(target: string, months: string[]): string | null {
  if (!months || months.length === 0) return null;
  const t = new Date(`${target}-01`).getTime();
  let best: string = months[0];
  let bestDiff = Math.abs(new Date(`${best}-01`).getTime() - t);
  for (let i = 1; i < months.length; i++) {
    const m = months[i];
    const diff = Math.abs(new Date(`${m}-01`).getTime() - t);
    if (diff < bestDiff) {
      best = m;
      bestDiff = diff;
    }
  }
  return best;
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

    // If the transaction touches any goal's fund category, refresh goal
    // roll-ups so savedToDate/spentToDate reflect the change immediately.
    const touchesGoal = (transaction.categories || []).some((c) =>
      goals.value.some((g) => g.name === c.category),
    );
    if (touchesGoal && familyStore.selectedEntityId) {
      void loadGoals(familyStore.selectedEntityId, true).then(() => {
        if (familyStore.selectedEntityId) {
          goals.value = listGoals(familyStore.selectedEntityId);
          savingsTotal.value = monthlySavingsTotal(familyStore.selectedEntityId, currentMonth.value);
        }
      });
    }
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error updating transaction: ${err.message}`, 'negative');
  }
}

function updateTransactions(newTransactions: Transaction[]) {
  try {
    budget.value.transactions = newTransactions;
    budgetStore.updateBudget(budgetId.value, { ...budget.value });
    updateMerchants();
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error updating transactions: ${err.message}`, 'negative');
  }
}

async function selectMonth(month: string) {
  // If no entity is selected, but a budget exists for this month for some entity,
  // temporarily switch to that entity so the user can view it.
  if (!familyStore.selectedEntityId) {
    const existing = availableBudgets.value.find((b) => b.month === month);
    if (existing?.entityId) {
      familyStore.selectEntity(existing.entityId);
    } else {
      showSnackbar('Select an entity to create a budget for this month', 'warning');
      return;
    }
  }

  if (!monthExists(month)) {
    await duplicateCurrentMonth(month);
  }
  selectedCategory.value = null;
  currentMonth.value = month;

  loading.value = true;
  try {
    const family = await familyStore.getFamily();
    const ownerId = family ? family.ownerUid : userId.value;

    // Look up an accessible budget for the selected month/entity and use its actual id
    const existing = availableBudgets.value.find((b) => b.month === month && matchesSelectedEntity(b));
    if (existing?.budgetId) {
      const freshBudget = await dataAccess.getBudget(existing.budgetId);
      if (freshBudget) {
        budget.value = { ...freshBudget, budgetId: existing.budgetId };
        budgetStore.updateBudget(existing.budgetId, freshBudget);
        categoryOptions.value = freshBudget.categories.map((cat) => cat.name);
        if (!categoryOptions.value.includes('Income')) {
          categoryOptions.value.push('Income');
        }
        return;
      }
    }

    // If not found, create a new budget for this month
    if (!family) throw new Error('Family not found');
    const b = await createBudgetForMonth(month, family.id, ownerId, familyStore.selectedEntityId);
    budget.value = { ...b };
    if (b.budgetId) budgetStore.updateBudget(b.budgetId, b);
    categoryOptions.value = b.categories.map((cat) => cat.name);
    if (!categoryOptions.value.includes('Income')) {
      categoryOptions.value.push('Income');
    }
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error loading budget: ${err.message}`, 'negative');
  } finally {
    loading.value = false;
  }
}

async function saveBudget() {
  const user = auth.user;
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
    if (futureCategories.value.length > 0) {
      await applyFutureCategories();
    }
    showSnackbar('Budget saved successfully');
    isEditing.value = false;
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Error saving budget: ${err.message}`, 'negative', () => {
      void saveBudget();
    });
  } finally {
    saving.value = false;
  }
}

// Options for the "Funded from" dropdown on the category edit row. Excludes
// the category itself and any Income-group categories (income isn't a real
// fund to draw from in the transfer model — it's just where positive cash
// flow is recorded).
function fundingSourceOptionsFor(cat: BudgetCategory): string[] {
  const groupList = familyStore.currentGroups;
  return budget.value.categories
    .filter((c) => c !== cat && c.name && !isIncomeCategory(c, groupList))
    .map((c) => c.name)
    .sort((a, b) => a.localeCompare(b));
}

async function addCategory() {
  const newCat: BudgetCategory = {
    name: '',
    target: 0,
    isFund: false,
    groupName: '',
  };
  budget.value.categories.push(newCat);
  const include = await new Promise<boolean>((resolve) => {
    $q.dialog({
      title: 'Include Category',
      message: 'Include this category in future budgets?',
      cancel: true,
      persistent: true,
    })
      .onOk(() => resolve(true))
      .onCancel(() => resolve(false))
      .onDismiss(() => resolve(false));
  });
  if (include) {
    futureCategories.value.push(newCat);
  }
}

function addIncomeCategory() {
  // Resolve the entity's existing income group; the migration guarantees one
  // per entity so this should always be present.
  const incomeGroup = familyStore.currentGroups.find((g) => g.kind === 'income');
  budget.value.categories.push({
    name: 'Income',
    target: 0,
    isFund: false,
    groupId: incomeGroup?.id,
    groupName: incomeGroup?.name ?? 'Income',
  });
  showSnackbar('Added new income category');
}

function removeCategory(index: number) {
  const removed = budget.value.categories.splice(index, 1)[0];
  futureCategories.value = futureCategories.value.filter((c) => c !== removed);
}

async function applyFutureCategories() {
  const entityId = familyStore.selectedEntityId;
  const family = await familyStore.getFamily();
  if (!entityId || !family) {
    futureCategories.value = [];
    return;
  }

  const entity = family.entities?.find((e) => e.id === entityId);
  if (entity) {
    const templateCats = entity.templateBudget?.categories || [];
    for (const cat of futureCategories.value) {
      if (!templateCats.some((c) => c.name === cat.name)) {
        // Persist the resolved group name on the template; the backend's
        // SaveBudget will upsert a budget_groups row per entity for it.
        const groupList = familyStore.currentGroups;
        templateCats.push({
          name: cat.name,
          target: cat.target,
          isFund: cat.isFund,
          groupId: cat.groupId,
          groupName: cat.groupName ?? categoryGroupName(cat, groupList),
        });
      }
    }
    entity.templateBudget = { categories: templateCats };
    await familyStore.updateEntity(family.id, entity);
  }

  const futureBudgets = budgets.value.filter((b) => b.entityId === entityId && b.month > budget.value.month);
  for (const fb of futureBudgets) {
    for (const cat of futureCategories.value) {
      if (!fb.categories.some((c) => c.name === cat.name)) {
        fb.categories.push({ ...cat });
      }
    }
    if (fb.budgetId) {
      await dataAccess.saveBudget(fb.budgetId, fb);
      budgetStore.updateBudget(fb.budgetId, fb);
    }
  }

  futureCategories.value = [];
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

  if (selectedCategory.value) {
    addTransactionForCategory(selectedCategory.value.name);
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
    transactionDialogKey.value += 1;
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
      entityId: familyStore.selectedEntityId,
      taxMetadata: [],
    };
    isIncomeTransaction.value = false;
    transactionDialogKey.value += 1;
    showTransactionDialog.value = true;
  }
}

function editTransaction(transaction: Transaction) {
  if (!transaction) {
    return;
  }

  const categories = transaction.categories?.map((cat) => ({ ...cat })) ?? [];
  if (categories.length === 0) {
    categories.push({ category: '', amount: transaction.amount ?? 0 });
  }

  const taxMetadata = transaction.taxMetadata ? transaction.taxMetadata.map((meta) => ({ ...meta, tags: meta.tags ? [...meta.tags] : [] })) : [];

  const dialogTransaction: Transaction = {
    ...transaction,
    categories,
    taxMetadata,
    budgetMonth: transaction.budgetMonth || currentMonth.value,
    userId: transaction.userId || userId.value,
  };

  if (!dialogTransaction.entityId && familyStore.selectedEntityId) {
    dialogTransaction.entityId = familyStore.selectedEntityId;
  }

  if (!dialogTransaction.budgetId && budgetId.value) {
    dialogTransaction.budgetId = budgetId.value;
  }

  newTransaction.value = dialogTransaction;
  isIncomeTransaction.value = !!transaction.isIncome;
  transactionDialogKey.value += 1;
  showTransactionDialog.value = true;
}

function confirmDeleteTransaction(transaction: Transaction) {
  transactionToDelete.value = transaction;
  showDeleteTransactionDialog.value = true;
}

async function executeTransactionDelete() {
  if (!transactionToDelete.value?.id) {
    showDeleteTransactionDialog.value = false;
    return;
  }

  $q.loading.show({
    message: 'Deleting transaction...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm flex items-center justify-center',
  });

  try {
    const targetBudget = budgetStore.getBudget(budgetId.value);
    if (targetBudget) {
      await dataAccess.deleteTransaction(targetBudget, transactionToDelete.value.id);
    }
    const updatedTransactions = budgetStore.getBudget(budgetId.value)?.transactions;
    if (updatedTransactions) {
      updateTransactions(updatedTransactions);
    }
    showSnackbar('Transaction deleted', 'positive');
  } catch (error: unknown) {
    const err = error as Error;
    console.error('Error deleting transaction', err);
    showSnackbar(`Failed to delete transaction: ${err.message}`, 'negative');
  } finally {
    $q.loading.hide();
    showDeleteTransactionDialog.value = false;
    transactionToDelete.value = null;
  }
}

async function duplicateCurrentMonth(month: string) {
  const user = auth.user;
  if (!user) {
    showSnackbar('Please log in to duplicate a budget', 'negative');
    return;
  }

  if (!familyStore.selectedEntityId) {
    showSnackbar('Please select an entity before duplicating a budget', 'negative');
    return;
  }

  $q.loading.show({
    message: 'Duplicating budget...',
    spinner: QSpinner,
    spinnerColor: 'primary',
    spinnerSize: 50,
    customClass: 'q-ml-sm items-center justify-center',
  });

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

    const newBudget = await createBudgetForMonth(month, family.id, family.ownerUid, familyStore.selectedEntityId);
    newBudget.merchants = budget.value.merchants ? [...budget.value.merchants] : [];
    await dataAccess.saveBudget(newBudget.budgetId, newBudget);
    await budgetStore.loadBudgets(user.uid, familyStore.selectedEntityId);

    showSnackbar("Created new month's budget");
  } catch (error: unknown) {
    const err = error as Error;
    showSnackbar(`Failed to duplicate budget: ${err.message}`, 'negative');
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
    showSnackbar(`Error saving budget: ${err.message}`, 'negative');
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
  id: string;
  name: string;
  kind: BudgetGroupKind;
  cat: string[];
}
</script>

<style scoped>
.non-current-banner {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: var(--color-warning-soft);
  border-radius: var(--radius-sm);
  font-size: 13px;
  color: var(--color-warning-strong-text);
}

.non-current-banner__text {
  flex: 1;
}

.non-current-banner__link {
  font-weight: 600;
  color: var(--color-warning-strong-text);
  cursor: pointer;
  text-decoration: underline;
  text-underline-offset: 2px;
  white-space: nowrap;
}

.non-current-banner__link:hover {
  opacity: 0.8;
}

.budget-page {
  min-height: 100%;
}

.budget-fab {
  z-index: 1000;
}

.budget-page--mobile {
  padding: 12px 8px 32px !important;
}

.budget-page--mobile :deep(.q-card-section.q-pa-lg) {
  padding: 16px 12px;
}

.budget-page--mobile .cat-name-text,
.budget-page--mobile .cat-amount,
.budget-page--mobile .income-row__name,
.budget-page--mobile .income-row__received,
.budget-page--mobile .income-row__planned {
  font-size: 15px;
}

.budget-page--mobile .cat-group-header {
  font-size: 15px;
}

.budget-page--mobile .cat-row {
  padding: 9px 0;
}

.budget-page--mobile .income-row {
  padding: 8px 0;
}

.budget-content-row {
  display: flex;
  flex-wrap: wrap;
}

.budget-content-row > * {
  margin-bottom: 16px;
  padding: 0 16px;
}

.budget-header {
  padding: 0 0 0px;
  margin-bottom: 0;
}

.budget-stat-card {
  background: var(--color-surface-card);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-subtle);
  padding: 16px;
  min-height: 100px;
}

.budget-stat-card__label {
  font-size: 12px;
  font-weight: 500;
  color: var(--color-text-muted);
}

.budget-stat-card__value {
  font-size: 28px;
  font-weight: 700;
  line-height: 1.3;
  margin-top: 4px;
  color: var(--color-text-primary);
}

.budget-stat-card__sub {
  font-size: 11px;
  font-weight: 400;
  color: var(--color-text-muted);
  margin-top: 4px;
}

.budget-search :deep(.q-field__control) {
  height: 40px;
  border-radius: var(--radius-md);
  border-color: var(--color-text-subtle);
}

.income-card {
  margin-top: 16px;
}

.income-header {
  display: flex;
  align-items: center;
}

.income-title {
  flex: 1;
  font-size: 15px;
  font-weight: 600;
  color: var(--color-text-primary);
}

.income-col-planned {
  width: 120px;
  text-align: right;
}

.income-col-received {
  width: 120px;
  text-align: right;
}

.income-divider {
  height: 1px;
  background: var(--color-divider, #e2e8f0);
  margin: 12px 0;
}

.income-row {
  display: flex;
  align-items: center;
  padding: 6px 0;
}

.income-row__name {
  flex: 1;
  font-size: 14px;
  font-weight: 400;
  color: var(--color-text-primary);
}

.income-row__planned {
  width: 120px;
  text-align: right;
  font-size: 14px;
  font-weight: 500;
  color: var(--color-text-primary);
}

.income-row__received {
  width: 120px;
  text-align: right;
  font-size: 14px;
  font-weight: 500;
  color: var(--color-text-primary);
}

.income-total {
  display: flex;
  align-items: center;
  padding: 4px 0;
}

.income-total__label {
  flex: 1;
  font-size: 14px;
  font-weight: 600;
  color: var(--color-text-primary);
}

.income-total__planned {
  width: 120px;
  text-align: right;
  font-size: 14px;
  font-weight: 600;
  color: var(--color-text-primary);
}

.income-total__received {
  width: 120px;
  text-align: right;
  font-size: 14px;
  font-weight: 600;
  color: var(--color-text-primary);
}

/* Category table layout */
.cat-table-header {
  display: flex;
  align-items: center;
}

.group-drag-handle {
  cursor: grab;
  opacity: 0.3;
  transition: opacity 0.15s;
}

.cat-table-header:hover .group-drag-handle {
  opacity: 0.7;
}

.group-drag-handle:active {
  cursor: grabbing;
}

.cat-table-card[draggable="true"] {
  transition: transform 0.15s, box-shadow 0.15s;
}

.group-drag-over {
  box-shadow: 0 0 0 2px var(--q-primary) !important;
  border-radius: var(--radius-md);
}

/* Per-category drop indicator: a top border so it's clear where the row will land. */
.cat-drag-over {
  border-top: 2px solid var(--q-primary);
}

.group-name-text {
  cursor: text;
  user-select: text;
}
.group-name-text:hover {
  text-decoration: underline dotted;
}
.group-rename-input {
  min-width: 140px;
  max-width: 280px;
}

.cat-col-name {
  flex: 1;
  min-width: 0;
}

.cat-col-progress {
  width: 180px;
  flex-shrink: 0;
  padding: 0 16px;
}

.cat-col-planned {
  width: 100px;
  flex-shrink: 0;
  text-align: right;
}

.cat-col-remaining {
  width: 100px;
  flex-shrink: 0;
  text-align: right;
}

.cat-divider {
  height: 1px;
  background: var(--color-divider, #e2e8f0);
  margin: 10px 0;
}

.cat-group-header {
  font-size: 14px;
  font-weight: 600;
  color: var(--q-primary);
  padding: 12px 0 8px;
}

.cat-row {
  display: flex;
  align-items: center;
  padding: 7px 0;
}

.cat-name-text {
  font-size: 14px;
  font-weight: 400;
  color: var(--color-text-primary);
}

.cat-amount {
  font-size: 14px;
  font-weight: 500;
  color: var(--color-text-primary);
}

.cat-progress-track {
  width: 100%;
  height: var(--progress-height, 8px);
  background: var(--color-divider);
  border-radius: var(--progress-radius, 4px);
  overflow: hidden;
}

.cat-progress-fill {
  height: 100%;
  border-radius: var(--progress-radius, 4px);
  transition: width 0.3s ease;
}

.cat-progress--partial {
  background: var(--q-primary);
}

.cat-progress--full {
  background: var(--q-positive);
}

.cat-progress--over {
  background: var(--q-warning);
}

.budget-tx-toggle :deep(.q-btn-group) {
  border-radius: var(--radius-sm) !important;
}

.budget-tx-toggle :deep(.q-btn) {
  border-radius: 0 !important;
  padding: 4px 12px !important;
  font-weight: 500;
  font-size: 0.8rem;
}

.budget-tx-toggle :deep(.q-btn:first-child) {
  border-radius: var(--radius-sm) 0 0 var(--radius-sm) !important;
}

.budget-tx-toggle :deep(.q-btn:last-child) {
  border-radius: 0 var(--radius-sm) var(--radius-sm) 0 !important;
}

.desktop-sidebar {
  position: sticky;
  top: 72px;
  display: flex;
  flex-direction: column;
  max-height: calc(100vh - 120px);
  overflow-y: auto;
  padding-bottom: 16px;
  gap: 16px;
}

.transactions-scroll {
  min-height: 100px;
  flex: 1;
}

.budget-transactions-card {
  min-height: 320px;
  display: flex;
  flex-direction: column;
}

.transactions-panel {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}
</style>
