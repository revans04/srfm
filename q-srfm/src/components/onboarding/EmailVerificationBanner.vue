<!--
  Renders on Budget + Dashboard whenever the signed-in Firebase user has
  not yet verified their email. Provides a single CTA to resend the
  verification email, with a 60-second cooldown to avoid Brevo rate-limits.

  Per-session dismissible (sessionStorage) so the user isn't nagged across
  every navigation in the same session, but the banner reappears next time
  they open the app — until they actually verify, at which point the
  Firebase auth listener flips `emailVerified` and this banner self-hides.

  Calm, factual copy per the design system rule (no exclamation marks,
  no emoji).
-->
<template>
  <q-banner
    v-if="visible"
    rounded
    class="email-verify-banner bg-amber-1 text-grey-9 q-mb-md"
  >
    <template #avatar>
      <q-icon name="mark_email_unread" color="warning" size="28px" />
    </template>
    <div class="text-body2 text-weight-medium">Verify your email address</div>
    <div class="text-caption text-grey-8 q-mt-xs">
      We sent a verification link to <span class="text-weight-medium">{{ emailDisplay }}</span>.
      Click the link in that email to confirm your address.
    </div>
    <template #action>
      <q-btn
        flat
        no-caps
        color="primary"
        :label="resendLabel"
        :disable="resendDisabled"
        :loading="resending"
        @click="onResend"
      />
      <q-btn
        flat
        dense
        no-caps
        color="grey-7"
        label="Hide for now"
        @click="dismissForSession"
      >
        <q-tooltip>Dismiss until your next session.</q-tooltip>
      </q-btn>
    </template>
  </q-banner>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';
import { useQuasar } from 'quasar';
import { useAuthStore } from '../../store/auth';
import { dataAccess } from '../../dataAccess';

const SESSION_DISMISSED_KEY = 'srfm-email-verify-banner-dismissed';
const RESEND_COOLDOWN_MS = 60_000;

const $q = useQuasar();
const authStore = useAuthStore();
const dismissedThisSession = ref(false);
const resending = ref(false);
const cooldownUntil = ref(0);
const now = ref(Date.now());
let cooldownTimer: ReturnType<typeof setInterval> | null = null;

// `emailVerified` is mirrored from the Firebase user object via the auth
// store's onAuthStateChanged listener. When the user clicks the verification
// link in their email and their token refreshes, this flips true and the
// banner self-removes — no manual reload needed.
const visible = computed(() => {
  if (dismissedThisSession.value) return false;
  const u = authStore.user;
  if (!u) return false;
  if (u.emailVerified) return false;
  return true;
});

const emailDisplay = computed(() => authStore.user?.email ?? 'your inbox');

const resendDisabled = computed(() => resending.value || now.value < cooldownUntil.value);

const resendLabel = computed(() => {
  if (resending.value) return 'Sending…';
  const remainingMs = cooldownUntil.value - now.value;
  if (remainingMs > 0) {
    const seconds = Math.ceil(remainingMs / 1000);
    return `Resend (${seconds}s)`;
  }
  return 'Resend verification email';
});

async function onResend() {
  if (resendDisabled.value) return;
  resending.value = true;
  try {
    await dataAccess.resendVerificationEmail();
    cooldownUntil.value = Date.now() + RESEND_COOLDOWN_MS;
    $q.notify({
      type: 'positive',
      message: `Verification email sent to ${emailDisplay.value}.`,
      position: 'bottom',
      timeout: 4000,
    });
  } catch (err) {
    const msg = err instanceof Error ? err.message : 'Could not send verification email';
    // The backend short-circuits with "Email is already verified" when the
    // Firebase record is verified but the token hasn't refreshed yet —
    // surface that case as a positive notice + nudge to reload.
    if (/already verified/i.test(msg)) {
      $q.notify({
        type: 'positive',
        message: 'Your email is already verified. Reload the page to update.',
        position: 'bottom',
        timeout: 5000,
      });
    } else {
      $q.notify({
        type: 'negative',
        message: msg,
        position: 'bottom',
        timeout: 5000,
      });
    }
  } finally {
    resending.value = false;
  }
}

function dismissForSession() {
  dismissedThisSession.value = true;
  try {
    sessionStorage.setItem(SESSION_DISMISSED_KEY, '1');
  } catch {
    // sessionStorage unavailable — banner just won't be sticky for the session.
  }
}

onMounted(() => {
  try {
    dismissedThisSession.value = sessionStorage.getItem(SESSION_DISMISSED_KEY) === '1';
  } catch {
    dismissedThisSession.value = false;
  }
  // If a cooldown is active, tick `now` once a second so the countdown
  // label stays current.
  cooldownTimer = setInterval(() => {
    now.value = Date.now();
  }, 1000);

  // Best-effort: refresh the Firebase user record so `emailVerified` is
  // up-to-date. Useful when the user clicked the link in another tab and
  // came back — the in-memory token is stale until reload.
  void authStore.user?.reload().catch(() => {
    /* token may be stale; ignore */
  });
});

onBeforeUnmount(() => {
  if (cooldownTimer) {
    clearInterval(cooldownTimer);
    cooldownTimer = null;
  }
});
</script>

<style scoped>
.email-verify-banner {
  border: 1px solid var(--q-amber-3, #ffe082);
}
</style>
