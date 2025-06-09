<template>
  <q-card>
    <q-card-section>
      <p>Let’s get started by naming your Family, Group, or Organization. This name will be used when sharing access with other people.</p>
      <br>
      <q-form @submit.prevent="createFamily">
        <q-text-field
          v-model="groupName"
          label="Family/Group/Org Name"
          required
          :rules="[v => !!v || 'Name is required']"
          autofocus
        ></q-text-field>
        <q-btn type="submit" color="primary" :loading="creating" block>Save</q-btn>
      </q-form>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { auth } from "../firebase/index";
import { dataAccess } from "../dataAccess";
import { useRouter } from "vue-router";
import { useFamilyStore } from "../store/family";

const router = useRouter();
const familyStore = useFamilyStore();
const emit = defineEmits(["family-created"]);
const groupName = ref("");
const creating = ref(false);
const showNextSteps = ref(false);

onMounted(async () => {
  const user = auth.currentUser;
  const family = await familyStore.getFamily();
  if (family && family.name) groupName.value = family.name;
  if (user && user.displayName && groupName.value == "")
    groupName.value = user?.displayName + " Family";
});

async function createFamily() {
  const user = auth.currentUser;
  if (!user || !groupName.value) return;
  let family = await familyStore.getFamily();

  creating.value = true;
  try {
    if (family) {
      family.name == groupName.value
      await dataAccess.renameFamily(family.id, family.name);
    } else {
      const response = await dataAccess.createFamily(user.uid, groupName.value, user.email ?? "");
      family = await familyStore.loadFamily();
    }
    emit("family-created", family?.id);
  } catch (error: any) {
    console.error("Error creating family:", error);
  } finally {
    creating.value = false;
  }
}

</script>
