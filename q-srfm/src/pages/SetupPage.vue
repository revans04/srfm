<!--
  Thin page shell for /setup. The actual form lives in
  components/onboarding/SetupSeedForm.vue. This page is responsible for:
    - Detecting `mode=add-entity` query (from Settings) vs. fresh user.
    - Centring the form card and capping its width on desktop.
    - Pre-filling family name when the user already has a family.
    - Routing the user to /budget after a successful seed.
-->
<template>
  <q-page class="setup-page bg-grey-1">
    <div class="setup-page__container">
      <q-card class="setup-page__card" flat bordered>
        <SetupSeedForm
          :mode="mode"
          :default-family-name="existingFamilyName"
          @seeded="onSeeded"
          @cancel="onCancel"
        />
      </q-card>
    </div>
  </q-page>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useFamilyStore } from '../store/family';
import { auth } from '../firebase/init';
import SetupSeedForm from '../components/onboarding/SetupSeedForm.vue';

const router = useRouter();
const route = useRoute();
const familyStore = useFamilyStore();

// Mode resolution: explicit ?mode=add-entity from Settings, otherwise infer
// from whether the user already has a family. Note that when the backend
// short-circuits with 409, we still navigate cleanly.
const mode = computed<'seed' | 'add-entity'>(() => {
  const q = route.query.mode;
  if (q === 'add-entity') return 'add-entity';
  return familyStore.family ? 'add-entity' : 'seed';
});

const existingFamilyName = computed(() => familyStore.family?.name ?? '');

function onSeeded(payload: { familyId: string; entityId: string; budgetId?: string; created: boolean }) {
  // Always land on /budget. The query param triggers the OnboardingWelcomeBanner
  // (only visible on a fresh seed; the banner itself self-suppresses on
  // re-renders via localStorage).
  void router.replace({
    path: '/budget',
    query: payload.created ? { onboarded: '1' } : undefined,
  });
}

function onCancel() {
  // Add-entity mode only — go back to where the user came from, falling
  // back to settings.
  if (window.history.length > 1) {
    router.back();
  } else {
    void router.replace('/settings');
  }
}

onMounted(async () => {
  // Make sure we have the family loaded so the mode computed is accurate
  // and the family name is pre-filled in add-entity mode.
  if (auth.currentUser?.uid && !familyStore.family) {
    try {
      await familyStore.loadFamily(auth.currentUser.uid);
    } catch (err) {
      // Non-fatal — the form still works. The family will be created if
      // missing, or 409'd if present.
      console.warn('SetupPage: failed to load family on mount', err);
    }
  }
});
</script>

<style scoped>
.setup-page {
  /* Vertical center on desktop, top-align on mobile so the keyboard doesn't
   * push the submit button off-screen. */
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 24px 16px;
  min-height: calc(100vh - 50px); /* account for top toolbar */
}

@media (min-width: 600px) {
  .setup-page {
    align-items: center;
    padding: 48px 24px;
  }
}

.setup-page__container {
  width: 100%;
  max-width: 560px;
}

.setup-page__card {
  border-radius: var(--radius-md, 12px);
  background: var(--color-surface-card, #ffffff);
}
</style>
