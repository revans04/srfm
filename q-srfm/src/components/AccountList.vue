<!-- src/components/AccountList.vue -->
<template>
  <q-card>
    <q-card-section>
      <div class="row items-center q-pa-sm">
        <div class="col">{{ type }} Accounts</div>
        <div class="col-auto text-right q-mr-md">{{ formatCurrency(accountValue) }}</div>
        <div class="col-auto">
          <q-btn color="primary" flat dense @click="$emit('add')" icon="add">Add Account</q-btn>
        </div>
      </div>
    </q-card-section>
    <q-card-section>
      <!-- Mobile card view -->
      <q-list v-if="isMobileView && accounts.length > 0" separator>
        <q-expansion-item
          v-for="account in accounts"
          :key="account.id"
          group="accounts"
          class="account-card-item"
        >
          <template #header>
            <q-item-section>
              <q-item-label class="text-weight-medium">{{ account.name }}</q-item-label>
              <q-item-label caption>{{ account.institution || type }}</q-item-label>
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
                  flat
                  dense
                  color="primary"
                  icon="edit"
                  label="Edit"
                  @click="$emit('edit', account)"
                  :disabled="account.userId && account.userId !== userId"
                  style="min-width: 44px; min-height: 44px;"
                />
                <q-btn
                  flat
                  dense
                  color="negative"
                  icon="delete"
                  label="Delete"
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
      <q-table v-else-if="accounts.length > 0" :columns="columns" :rows="accounts" class="elevation-1" :pagination="{ rowsPerPage: 100 }" hide-bottom>
        <template v-slot:body-cell-owner="props">
          <q-td :props="props">
            {{ props.row.userId ? 'Personal' : 'Shared' }}
          </q-td>
        </template>
        <template v-slot:body-cell-balance="props">
          <q-td :props="props">
            {{ formatCurrency(props.row.balance || 0) }}
          </q-td>
        </template>
        <template v-slot:body-cell-actions="props">
          <q-td :props="props">
            <q-btn flat dense color="primary" icon="edit" @click="$emit('edit', props.row)" :disabled="props.row.userId && props.row.userId !== userId" />
            <q-btn
              flat
              dense
              color="negative"
              icon="delete"
              @click="$emit('delete', props.row.id)"
              :disabled="props.row.userId && props.row.userId !== userId"
            />
          </q-td>
        </template>
      </q-table>
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
  { name: 'name', label: 'Name', field: 'name' },
  ...(props.type !== 'Property' ? [{ name: 'institution', label: 'Institution', field: 'institution' }] : []),
  { name: 'balance', label: 'Balance', field: 'balance' },
  ...(props.type === 'Bank' || props.type === 'CreditCard' ? [{ name: 'accountNumber', label: 'Account Number', field: 'accountNumber' }] : []),
  ...(props.type === 'Property' ? [{ name: 'address', label: 'Address', field: (row: Account) => row.details?.address }] : []),
  ...(props.type === 'Loan' || props.type === 'CreditCard'
    ? [{ name: 'interestRate', label: 'Interest Rate', field: (row: Account) => row.details?.interestRate }]
    : []),
  { name: 'owner', label: 'Owner', field: 'userId' },
  { name: 'actions', label: 'Actions', field: 'actions' },
]);
</script>

<style scoped>
.account-card-item {
  min-height: 48px;
}
</style>
