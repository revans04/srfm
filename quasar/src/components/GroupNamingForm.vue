<template>
  <q-card>
    <q-card-section>
      <p>
        Let’s get started by naming your Family, Group, or Organization. This name will be used when
        sharing access with other people.
      </p>
      <br />
      <q-form @submit="createFamily">
        <q-input
          v-model="groupName"
          label="Family/Group/Org Name"
          :rules="[(v) => !!v || 'Name is required']"
          autofocus
          standout
          dense
        />
        <q-btn type="submit" color="primary" :loading="creating" label="Save" class="full-width" />
      </q-form>
    </q-card-section>
  </q-card>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { auth } from '../firebase/index';
import { dataAccess } from '../dataAccess';
import { useFamilyStore } from '@/store/family';

const familyStore = useFamilyStore();
const emit = defineEmits(['family-created']);
const groupName = ref('');
const creating = ref(false);

onMounted(async () => {
  const user = auth.currentUser;
  const family = await familyStore.getFamily();
  if (family && family.name) groupName.value = family.name;
  if (user && user.displayName && groupName.value == '')
    groupName.value = user?.displayName + ' Family';
});

async function createFamily() {
  const user = auth.currentUser;
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
