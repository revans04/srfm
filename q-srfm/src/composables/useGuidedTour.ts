import { computed, onMounted } from 'vue';
import { useTourStore } from '../store/tour';

export function useGuidedTour(tipId: string) {
  const tourStore = useTourStore();

  onMounted(() => {
    if (!tourStore.initialized) {
      tourStore.loadTourState();
    }
  });

  const showTip = computed(() => tourStore.initialized && !tourStore.isTipDismissed(tipId));

  function dismiss() {
    tourStore.dismissTip(tipId);
  }

  return {
    showTip,
    dismiss,
  };
}
