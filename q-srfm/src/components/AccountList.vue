<!-- src/components/AccountList.vue -->
<template>
  <q-card flat bordered class="account-list-card">
    <q-card-section class="row items-center q-py-sm">
      <div class="col">
        <span class="text-body1 text-weight-medium">{{ typeLabel }}</span>
      </div>
      <div class="col-auto text-weight-bold q-mr-md">{{ formatCurrency(accountValue) }}</div>
      <div class="col-auto">
        <q-btn color="primary" flat dense icon="add" label="Add Account" @click="$emit('add')" />
      </div>
    </q-card-section>

    <q-separator />

    <q-card-section class="q-pa-none" v-if="accounts.length > 0">
      <!-- Mobile card view -->
      <q-list v-if="isMobileView" separator>
        <q-expansion-item
          v-for="account in accounts"
          :key="account.id"
          group="accounts"
          class="account-card-item"
        >
          <template #header>
            <q-item-section>
              <q-item-label class="text-weight-medium">{{ account.name }}</q-item-label>
              <q-item-label caption>{{ account.institution || typeLabel }}</q-item-label>
            </q-item-section>
            <q-item-section side>
              <q-item-label class="text-weight-bold">{{ formatCurrency(account.balance || 0) }}</q-item-label>
            </q-item-section>
          </template>
          <q-card flat>
            <q-card-section class="q-pt-none">
              <div v-if="account.accountNumber" class="q-mb-xs">
                <span class="text-caption text-muted">Account #:</span> {{ account.accountNumber }}
              </div>
              <div class="q-mb-xs">
                <span class="text-caption text-muted">Owner:</span> {{ account.userId ? 'Personal' : 'Shared' }}
              </div>
              <div class="row q-gutter-sm q-mt-sm">
                <q-btn
                  flat dense color="primary" icon="edit" label="Edit"
                  @click="$emit('edit', account)"
                  :disabled="account.userId && account.userId !== userId"
                  style="min-width: 44px; min-height: 44px;"
                />
                <q-btn
                  flat dense color="negative" icon="delete_outline" label="Delete"
                  @click="$emit('delete', account.id)"
                  :disabled="account.userId && account.userId !== userId"
                  style="min-width: 44px; min-height: 44px;"
                />
              </div>
            </q-card-section>
          </q-card>
        </q-expansion-item>
      </q-list>

      <!-- Desktop table view -->
      <q-table
        v-else
        :columns="columns"
        :rows="accounts"
        flat
        :pagination="{ rowsPerPage: 100 }"
        hide-bottom
        class="account-table"
      >
        <template v-slot:body-cell-name="props">
          <q-td :props="props" class="text-weight-medium">
            {{ props.row.name }}
          </q-td>
        </template>
        <template v-slot:body-cell-owner="props">
          <q-td :props="props">
            <q-badge :color="props.row.userId ? 'grey-4' : 'primary'" :text-color="props.row.userId ? 'grey-8' : 'white'" :label="props.row.userId ? 'Personal' : 'Shared'" />
          </q-td>
        </template>
        <template v-slot:body-cell-balance="props">
          <q-td :props="props" class="text-weight-bold">
            {{ formatCurrency(props.row.balance || 0) }}
          </q-td>
        </template>
        <template v-slot:body-cell-actions="props">
          <q-td :props="props">
            <q-btn flat round dense color="primary" icon="edit" size="sm" @click="$emit('edit', props.row)" :disabled="props.row.userId && props.row.userId !== userId" />
            <q-btn flat round dense color="negative" icon="delete_outline" size="sm" @click="$emit('delete', props.row.id)" :disabled="props.row.userId && props.row.userId !== userId" />
          </q-td>
        </template>
      </q-table>
    </q-card-section>

    <q-card-section v-else class="text-center q-py-lg text-muted">
      No {{ typeLabel.toLowerCase() }} yet. Click "Add Account" to get started.
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { computed, defineProps } from 'vue';
import { useQuasar } from 'quasar';
import type { Account, ImportedTransaction } from '../types';
import { formatCurrency } from '../utils/helpers';
import { auth } from '../firebase/init';

const $q = useQuasar();
const isMobileView = computed(() => $q.screen.lt.md);

const props = defineProps<{
  accounts: Account[];
  importedTransactions?: ImportedTransaction[];
  type: 'Bank' | 'CreditCard' | 'Investment' | 'Property' | 'Loan';
}>();

const userId = computed(() => auth.currentUser?.uid || '');

const typeLabel = computed(() => {
  const labels: Record<string, string> = {
    Bank: 'Bank Accounts',
    CreditCard: 'Credit Cards',
    Investment: 'Investments',
    Property: 'Properties',
    Loan: 'Loans',
  };
  return labels[props.type] || props.type;
});

const accountValue = computed(() => {
  let val = 0;
  props.accounts.forEach((a) => {
    val += a.balance ?? 0;
  });
  if (props.type == 'CreditCard' || props.type == 'Loan') {
    val = val * -1;
  }
  return val;
});

const columns = computed(() => [
  { name: 'name', label: 'Name', field: 'name', align: 'left' as const },
  ...(props.type !== 'Property' ? [{ name: 'institution', label: 'Institution', field: 'institution', align: 'left' as const }] : []),
  { name: 'balance', label: 'Balance', field: 'balance', align: 'right' as const },
  ...(props.type === 'Bank' || props.type === 'CreditCard' ? [{ name: 'accountNumber', label: 'Acct #', field: 'accountNumber', align: 'left' as const }] : []),
  ...(props.type === 'Property' ? [{ name: 'address', label: 'Address', field: (row: Account) => row.details?.address, align: 'left' as const }] : []),
  ...(props.type === 'Loan' || props.type === 'CreditCard'
    ? [{ name: 'interestRate', label: 'Rate', field: (row: Account) => row.details?.interestRate, align: 'right' as const }]
    : []),
  { name: 'owner', label: 'Owner', field: 'userId', align: 'center' as const },
  { name: 'actions', label: '', field: 'actions', align: 'right' as const },
]);
</script>

<style scoped>
.account-list-card {
  border-radius: var(--radius-md);
}

.account-table {
  border: none;
}

.account-table :deep(thead th) {
  font-weight: 600;
  font-size: 0.8rem;
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.03em;
}

.account-card-item {
  min-height: 48px;
}
</style>
