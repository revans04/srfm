// src/store/auth.ts
import { defineStore } from "pinia";
import { auth } from "../firebase";
import { ref } from "vue";
import type { User } from "firebase/auth";

export const useAuthStore = defineStore("auth", () => {
  let user: User | null = null;
  const avatarSrc = ref("https://via.placeholder.com/36");

  function initializeAuth() {
    auth.onAuthStateChanged((firebaseUser) => {
      user = firebaseUser;
      if (firebaseUser) {
        avatarSrc.value = firebaseUser.photoURL || "https://via.placeholder.com/36";
      } else {
        avatarSrc.value = "https://via.placeholder.com/36";
      }
    });
  }

  async function signOut() {
    await auth.signOut();
  }

  return { user, avatarSrc, initializeAuth, signOut };
});

