<!--
  Single-page onboarding seed form. Replaces the multi-step
  SetupWizardPage.vue. Designed for sub-90-second completion at 375px:
  required fields are pre-filled and visible; tax forms and starter
  accounts live in collapsed-by-default expanders.

  Two modes:
    - 'seed' (default, fresh user): creates family + entity + first budget +
      optional accounts. Family-name input is shown.
    - 'add-entity' (existing user from Settings): hides the family-name
      input. Backend short-circuits with 409 + existing IDs and the parent
      page just navigates back to /budget.

  All persistence flows through `dataAccess.seedOnboarding(...)`, which
  hits the transactional /api/onboarding/seed endpoint (PR 2). Partial
  states are impossible — either everything lands or nothing does.
-->
<template>
  <q-form ref="formRef" class="seed-form" @submit.prevent="onSubmit">
    <q-card-section class="q-pb-none">
      <h1 class="seed-form__headline">{{ headline }}</h1>
      <p class="seed-form__subhead text-grey-8">{{ subhead }}</p>
    </q-card-section>

    <!-- Family name -->
    <q-card-section v-if="mode === 'seed'" class="seed-form__section">
      <div class="seed-form__section-title">Your household</div>
      <q-input
        v-model="familyName"
        label="Family or household name"
        outlined
        :rules="[(v: string) => !!v?.trim() || 'Required']"
        hide-bottom-space
        autocomplete="off"
      />
    </q-card-section>

    <!-- Entity -->
    <q-card-section class="seed-form__section">
      <div class="seed-form__section-title">
        {{ mode === 'add-entity' ? 'Add an entity' : 'Your first entity' }}
      </div>
      <div class="row q-col-gutter-md">
        <div class="col-12 col-sm-6">
          <q-select
            v-model="entityType"
            :options="entityTypeOptions"
            label="Type"
            option-label="title"
            option-value="value"
            emit-value
            map-options
            outlined
            :rules="[(v: string) => !!v || 'Required']"
            hide-bottom-space
          />
        </div>
        <div class="col-12 col-sm-6">
          <q-input
            v-model="entityName"
            label="Name"
            outlined
            :rules="[(v: string) => !!v?.trim() || 'Required']"
            hide-bottom-space
            autocomplete="off"
          />
        </div>
      </div>
    </q-card-section>

    <!-- Starter budget -->
    <q-card-section class="seed-form__section">
      <div class="seed-form__section-title">Starter budget</div>
      <q-checkbox
        v-model="useTemplate"
        :label="`Use the ${entityTypeLabel} template — ${templateCategoryCount} starter categories`"
      />
      <div class="text-caption text-grey-7 q-mt-xs">
        {{ useTemplate
          ? "We'll seed common categories you can edit later."
          : 'Empty budget — you\'ll add categories yourself.' }}
      </div>
    </q-card-section>

    <!-- Optional accounts expander -->
    <q-card-section class="seed-form__section q-pt-none">
      <q-expansion-item
        icon="account_balance"
        label="Add an account (optional)"
        header-class="seed-form__expander-header"
      >
        <div class="q-pa-sm">
          <p class="text-caption text-grey-7 q-mb-md">
            You can add accounts later on the Accounts page; this is just for
            convenience while you're here.
          </p>
          <div
            v-for="(acc, idx) in accounts"
            :key="idx"
            class="row q-col-gutter-sm q-mb-sm items-end"
          >
            <div class="col-12 col-sm-3">
              <q-input v-model="acc.name" label="Name" outlined dense hide-bottom-space />
            </div>
            <div class="col-6 col-sm-3">
              <q-select
                v-model="acc.type"
                :options="accountTypeOptions"
                label="Type"
                outlined
                dense
                hide-bottom-space
              />
            </div>
            <div class="col-6 col-sm-3">
              <q-input v-model="acc.institution" label="Institution" outlined dense hide-bottom-space />
            </div>
            <div class="col-8 col-sm-2">
              <CurrencyInput v-model="acc.balance" label="Balance" outlined dense />
            </div>
            <div class="col-4 col-sm-1 text-right">
              <q-btn
                flat round dense color="negative" icon="close"
                @click="removeAccount(idx)"
                :aria-label="`Remove account ${idx + 1}`"
              />
            </div>
          </div>
          <q-btn
            flat
            color="primary"
            icon="add"
            label="Add another account"
            @click="addAccount"
            class="q-mt-sm"
          />
        </div>
      </q-expansion-item>
    </q-card-section>

    <!-- Optional tax forms expander -->
    <q-card-section v-if="availableTaxForms.length > 0" class="seed-form__section q-pt-none">
      <q-expansion-item
        icon="description"
        label="Tax forms (optional)"
        header-class="seed-form__expander-header"
      >
        <div class="q-pa-sm">
          <p class="text-caption text-grey-7 q-mb-sm">
            Select forms this entity files. You can change this later in Entity settings.
          </p>
          <q-option-group
            v-model="taxFormIds"
            :options="availableTaxForms.map((f) => ({ label: `${f.name} — ${f.description}`, value: f.id }))"
            type="checkbox"
            color="primary"
          />
        </div>
      </q-expansion-item>
    </q-card-section>

    <q-card-actions class="seed-form__actions q-px-md q-pt-none q-pb-md">
      <q-btn
        type="submit"
        color="primary"
        unelevated
        no-caps
        :label="submitLabel"
        :loading="saving"
        class="seed-form__submit full-width"
      />
      <q-btn
        v-if="mode === 'add-entity'"
        flat
        no-caps
        label="Cancel"
        color="grey-7"
        @click="$emit('cancel')"
        class="full-width q-mt-sm"
      />
    </q-card-actions>
  </q-form>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useQuasar, QForm } from 'quasar';
import CurrencyInput from '../CurrencyInput.vue';
import { dataAccess } from '../../dataAccess';
import { auth } from '../../firebase/init';
import { useFamilyStore } from '../../store/family';
import { EntityType } from '../../types';
import { DEFAULT_BUDGET_TEMPLATES } from '../../constants/budgetTemplates';
import { DEFAULT_TAX_FORMS } from '../../constants/taxForms';
import { currentMonthISO, formatEntityType } from '../../utils/helpers';

type Mode = 'seed' | 'add-entity';

const props = withDefaults(defineProps<{
  mode?: Mode;
  /** Pre-fill the family name when the user already has a family. */
  defaultFamilyName?: string;
}>(), {
  mode: 'seed',
  defaultFamilyName: '',
});

const emit = defineEmits<{
  /** Fires after a successful seed (or the 409 short-circuit). Carries the
   * new IDs so the page can navigate. */
  (e: 'seeded', payload: { familyId: string; entityId: string; budgetId?: string; created: boolean }): void;
  /** Fires when the user cancels (only meaningful in `add-entity` mode). */
  (e: 'cancel'): void;
}>();

const $q = useQuasar();
const familyStore = useFamilyStore();
const formRef = ref<InstanceType<typeof QForm> | null>(null);

// Form state
const familyName = ref('');
const entityName = ref('Household');
const entityType = ref<EntityType>(EntityType.Family);
const useTemplate = ref(true);
const taxFormIds = ref<string[]>([]);
const accounts = ref<Array<{
  name: string;
  type: 'Bank' | 'CreditCard' | 'Investment' | 'Property' | 'Loan';
  institution: string;
  balance: number;
}>>([]);

const saving = ref(false);

// Computed
const headline = computed(() =>
  props.mode === 'add-entity'
    ? 'Add another entity'
    : "Let's set up your first budget",
);

const subhead = computed(() =>
  props.mode === 'add-entity'
    ? 'Each entity has its own budget, categories, and accounts. Pick a type and a name to get started.'
    : 'About a minute. You can edit anything later.',
);

const submitLabel = computed(() =>
  saving.value
    ? 'Setting things up…'
    : props.mode === 'add-entity'
      ? 'Add entity'
      : 'Get started',
);

const entityTypeOptions = Object.values(EntityType).map((value) => ({
  title: formatEntityType(value),
  value,
}));

const entityTypeLabel = computed(() => formatEntityType(entityType.value));

const templateCategoryCount = computed(() => {
  const tpl = DEFAULT_BUDGET_TEMPLATES[entityType.value];
  return tpl?.categories?.length ?? 0;
});

const availableTaxForms = computed(() =>
  DEFAULT_TAX_FORMS.filter((f) => f.applicableEntityTypes.includes(entityType.value)),
);

// Account handlers
const accountTypeOptions = ['Bank', 'CreditCard', 'Investment', 'Property', 'Loan'] as const;
function addAccount() {
  accounts.value.push({ name: '', type: 'Bank', institution: '', balance: 0 });
}
function removeAccount(idx: number) {
  accounts.value.splice(idx, 1);
}

// Submit
async function onSubmit() {
  const valid = await formRef.value?.validate();
  if (!valid) return;

  saving.value = true;
  try {
    const month = currentMonthISO();
    const templateCategories = useTemplate.value
      ? (DEFAULT_BUDGET_TEMPLATES[entityType.value]?.categories ?? []).map((c) => ({
          name: c.name,
          groupName: c.groupName ?? '',
          target: c.target ?? 0,
          isFund: c.isFund ?? false,
        }))
      : undefined;

    const cleanedAccounts = accounts.value
      .filter((a) => a.name.trim())
      .map((a) => ({
        name: a.name.trim(),
        type: a.type,
        institution: a.institution.trim() || undefined,
        balance: typeof a.balance === 'number' ? a.balance : Number(a.balance) || 0,
      }));

    const payload = {
      familyName: (familyName.value || props.defaultFamilyName || '').trim(),
      entityName: entityName.value.trim(),
      entityType: entityType.value,
      useTemplate: useTemplate.value,
      month,
      templateCategories,
      taxFormIds: taxFormIds.value.length > 0 ? taxFormIds.value : undefined,
      accounts: cleanedAccounts.length > 0 ? cleanedAccounts : undefined,
    };

    const result = await dataAccess.seedOnboarding(payload);

    // Force-reload the family store so downstream pages (BudgetPage,
    // Dashboard) see the freshly-seeded family/entity instead of stale
    // null state.
    if (auth.currentUser?.uid) {
      await familyStore.loadFamily(auth.currentUser.uid, { force: true });
    }

    emit('seeded', {
      familyId: result.familyId,
      entityId: result.entityId,
      budgetId: result.budgetId,
      created: result.created,
    });
  } catch (err) {
    const msg = err instanceof Error ? err.message : 'Unknown error';
    console.error('Onboarding seed failed:', err);
    $q.notify({
      type: 'negative',
      message: `We couldn't save your setup — ${msg}. Try again.`,
      position: 'bottom',
      timeout: 6000,
    });
  } finally {
    saving.value = false;
  }
}

onMounted(() => {
  // Pre-fill the family name from the user's display name (sensible default
  // for the dominant Family entity type).
  if (props.mode === 'seed' && !familyName.value) {
    const display = auth.currentUser?.displayName?.trim() ?? '';
    familyName.value = display ? `${display.split(' ')[0]} Family` : 'My Family';
  }
  if (props.mode === 'add-entity' && props.defaultFamilyName) {
    familyName.value = props.defaultFamilyName;
  }
});
</script>

<style scoped>
.seed-form__headline {
  font-size: 1.5rem;
  font-weight: 600;
  margin: 0 0 4px 0;
  color: var(--q-grey-9, #0f172a);
}

.seed-form__subhead {
  margin: 0 0 8px 0;
  font-size: 0.95rem;
}

.seed-form__section {
  padding-top: 16px;
  padding-bottom: 16px;
}

.seed-form__section-title {
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--q-grey-7, #475569);
  margin-bottom: 12px;
}

.seed-form__expander-header {
  font-weight: 500;
  color: var(--q-primary);
}

.seed-form__submit {
  /* Guarantee 44px tap target on mobile per the design-system rule. */
  min-height: 44px;
  padding: 12px;
  font-weight: 600;
}
</style>
