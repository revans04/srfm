import { initializeApp } from "firebase/app";
import { getAuth } from "firebase/auth"; // Import auth separately
import { getFirestore } from "firebase/firestore";

const firebaseConfig = {
  // Your Firebase config object from Firebase Console
  apiKey: "AIzaSyAiTx36M_FElpCZT0kwGS_w63GWMo9Fxlg",
  authDomain: "budget-buddy-a6b6c.firebaseapp.com",
  projectId: "budget-buddy-a6b6c",
  storageBucket: "budget-buddy-a6b6c.firebasestorage.app",
  messagingSenderId: "583821970715",
  appId: "1:583821970715:web:5d797fe8e71a9048ac17be",
  measurementId: "G-Z1TPM1S847",
};

const app = initializeApp(firebaseConfig);
export const auth = getAuth(app); // Export the auth instance
export const db = getFirestore(app); // Export Firestore instance
