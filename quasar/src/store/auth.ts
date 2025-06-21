// src/store/auth.ts
import { defineStore } from 'pinia';
import { ref } from 'vue';
import { auth, signInWithGoogle } from '../firebase/init';
import { signInWithCustomToken } from 'firebase/auth';
import type { User } from 'firebase/auth';

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null);
  const avatarSrc = ref('https://via.placeholder.com/36');
  const authError = ref<string | null>(null);

  function initializeAuth() {
    return new Promise((resolve, reject) => {
      console.log('Initializing Firebase auth');
      const unsubscribe = auth.onAuthStateChanged(
        (firebaseUser) => {
          console.log('Firebase auth state changed:', firebaseUser ? firebaseUser.uid : null);
          user.value = firebaseUser;
          if (firebaseUser) {
            avatarSrc.value = firebaseUser.photoURL || 'https://via.placeholder.com/36';
          } else {
            avatarSrc.value = 'https://via.placeholder.com/36';
          }
          resolve(firebaseUser);
        },
        (error) => {
          console.error('Auth state error:', error);
          authError.value = error.message;
          unsubscribe();
          reject(error);
        }
      );
    });
  }

  // Auto-initialize auth
  initializeAuth().catch((error) => {
    console.error('Failed to initialize auth:', error);
  });

  async function loginWithGoogle(): Promise<void> {
    try {
      authError.value = null;
      user.value = await signInWithGoogle();
    } catch (error: any) {
      console.error('Google login error:', error);
      authError.value = error.message;
      throw error;
    }
  }

  async function loginWithCustomToken(token: string): Promise<void> {
    try {
      authError.value = null;
      const userCredential = await signInWithCustomToken(auth, token);
      user.value = userCredential.user;
    } catch (error: any) {
      console.error('Custom token login error:', error);
      authError.value = error.message;
      throw error;
    }
  }

  async function logout() {
    try {
      await auth.signOut();
      user.value = null;
      authError.value = null;
    } catch (error: any) {
      console.error('Logout error:', error);
      authError.value = error.message;
      throw error;
    }
  }

  async function getIdToken(): Promise<string | null> {
    try {
      const currentUser = user.value;
      return currentUser ? await currentUser.getIdToken(true) : null;
    } catch (error: any) {
      console.error('Get ID token error:', error);
      authError.value = error.message;
      return null;
    }
  }

  return { user, avatarSrc, authError, initializeAuth, loginWithGoogle, loginWithCustomToken, logout, getIdToken };
});
