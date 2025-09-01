<template>
  <q-card>
    <q-card-section>
      <p>Let’s get started by naming your Family, Group, or Organization. This name will be used when sharing access with other people.</p>
      <br />
      <q-form @submit.prevent="createFamily">
        <q-input v-model="groupName" label="Family/Group/Org Name" required :rules="[(v: string) => !!v || 'Name is required']" autofocus></q-input>
        <q-btn type="submit" color="primary" :loading="creating" block>Save</q-btn>
      </q-form>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { dataAccess } from '../dataAccess';
import { useFamilyStore } from '../store/family';
import { useAuthStore } from '../store/auth';

const familyStore = useFamilyStore();
const auth = useAuthStore();
const emit = defineEmits(['family-created']);
const groupName = ref('');
const creating = ref(false);

onMounted(async () => {
  const user = auth.user;
  const family = await familyStore.getFamily();
  if (family && family.name) groupName.value = family.name;
  if (user && user.displayName && groupName.value == '') groupName.value = user?.displayName + ' Family';
});

async function createFamily() {
  const user = auth.user;
  if (!user || !groupName.value) return;
  let family = await familyStore.getFamily();

  creating.value = true;
  try {
    if (family) {
      family.name = groupName.value;
      await dataAccess.renameFamily(family.id, family.name);
    } else {
      await dataAccess.createFamily(user.uid, groupName.value, user.email ?? '');
      family = await familyStore.loadFamily();
    }
    emit('family-created', family?.id);
  } catch (error: unknown) {
    console.error('Error creating family:', error);
  } finally {
    creating.value = false;
  }
}
</script>
