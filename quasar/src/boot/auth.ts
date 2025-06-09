import { boot } from 'quasar/wrappers';
import { onAuthStateChanged } from 'firebase/auth';
import { auth } from 'src/firebase/init';
import { useAuthStore } from 'src/store/auth';

export default boot(() => {
  const authStore = useAuthStore();
  return new Promise<void>((resolve) => {
    onAuthStateChanged(auth, (user) => {
      authStore.user = user;
      resolve();
    });
  });
});
