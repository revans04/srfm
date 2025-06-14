// src/store/auth.ts
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { auth, signInWithGoogle } from '../firebase/init';
import type { User } from 'firebase/auth';

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null);
  const avatarSrc = ref('https://via.placeholder.com/36');

  async function login(): Promise<void> {
    user.value = await signInWithGoogle();
  }

  function initializeAuth() {
    auth.onAuthStateChanged((firebaseUser: User | null) => {
      user.value = firebaseUser;
      if (firebaseUser) {
        avatarSrc.value = firebaseUser.photoURL || 'https://via.placeholder.com/36';
      } else {
        avatarSrc.value = 'https://via.placeholder.com/36';
      }
    });
  }

  async function logout() {
    await auth.signOut();
  }

  async function getIdToken(): Promise<string | null> {
    const user = auth.currentUser;
    return user ? await user.getIdToken(true) : null; // Force refresh
  }

  return { user, avatarSrc, initializeAuth, logout, login, getIdToken, auth };
});
