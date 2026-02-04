import { boot } from 'quasar/wrappers';
import { onAuthStateChanged } from 'firebase/auth';
import { auth } from 'src/firebase/init';
import { useAuthStore } from 'src/store/auth';
import { useFamilyStore } from 'src/store/family';

export default boot(() => {
  const authStore = useAuthStore();
  const familyStore = useFamilyStore();
  return new Promise<void>((resolve) => {
    onAuthStateChanged(auth, (user) => {
      authStore.user = user;
      if (user) {
        void familyStore.loadFamily(user.uid, { force: true });
      }
      resolve();
    });
  });
});
