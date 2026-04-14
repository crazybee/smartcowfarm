<template>
  <div class="vax-view">
    <div class="page-header">
      <h1>💉 Vaccination Management</h1>
      <button class="add-btn" @click="showForm = !showForm">
        {{ showForm ? 'Hide Form' : '+ Add Record' }}
      </button>
    </div>
    <div v-if="showForm" class="add-form">
      <h3>Record Vaccination</h3>
      <form @submit.prevent="submitVax">
        <div class="form-row">
          <label>Cow ID <input v-model="form.cow_id" required /></label>
          <label>Vaccine Name <input v-model="form.vaccine_name" required /></label>
          <label>Date Administered <input type="date" v-model="form.last_administered" required /></label>
          <label>Next Due <input type="date" v-model="form.next_due" /></label>
          <button type="submit" class="btn-primary">Save</button>
        </div>
      </form>
    </div>
    <div class="filters">
      <input v-model="filterCow" placeholder="Filter by Cow ID" class="filter-input" />
      <input v-model="filterVaccine" placeholder="Filter by Vaccine" class="filter-input" />
      <input type="date" v-model="filterFrom" class="filter-input" title="From date" />
      <input type="date" v-model="filterTo" class="filter-input" title="To date" />
      <button @click="clearFilters" class="clear-btn">Clear</button>
    </div>
    <div class="table-wrap">
      <table class="table">
        <thead>
          <tr>
            <th>Cow ID</th>
            <th>Vaccine Name</th>
            <th>Last Administered</th>
            <th>Next Due</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in filteredRecords" :key="row.id">
            <td>{{ row.cow_id }}</td>
            <td>{{ row.vaccine_name }}</td>
            <td>{{ row.last_administered ? new Date(row.last_administered).toLocaleDateString() : 'N/A' }}</td>
            <td>{{ row.next_due ? new Date(row.next_due).toLocaleDateString() : 'N/A' }}</td>
            <td>
              <span :class="['status-badge', statusClass(row.next_due)]">{{ vaxStatus(row.next_due) }}</span>
            </td>
          </tr>
          <tr v-if="filteredRecords.length === 0">
            <td colspan="5" class="empty">No records match your filters.</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useCowStore } from '../stores/cowStore'
import apiService from '../services/apiService'
import { getCowId } from '../services/modelTransforms'

const store = useCowStore()
const records = ref([])
const showForm = ref(false)
const form = ref({ cow_id: '', vaccine_name: '', last_administered: '', next_due: '' })
const filterCow = ref('')
const filterVaccine = ref('')
const filterFrom = ref('')
const filterTo = ref('')

onMounted(async () => {
  if (!store.cows.length) await store.fetchCows()
  await loadAll()
})

async function loadAll() {
  try {
    const all = await Promise.all(
      store.cows.map(c => apiService.getVaccinations(getCowId(c)).catch(() => []))
    )
    records.value = all.flat()
  } catch (e) {
    records.value = []
  }
}

async function submitVax() {
  try {
    await apiService.addVaccination(form.value.cow_id, form.value)
    await loadAll()
    showForm.value = false
    form.value = { cow_id: '', vaccine_name: '', last_administered: '', next_due: '' }
  } catch (e) {
    console.error('submitVax error', e)
  }
}

function clearFilters() {
  filterCow.value = ''
  filterVaccine.value = ''
  filterFrom.value = ''
  filterTo.value = ''
}

function vaxStatus(nextDue) {
  if (!nextDue) return 'OK'
  const today = new Date()
  const due = new Date(nextDue)
  if (due < today) return 'Overdue'
  if (due <= new Date(today.getTime() + 7 * 24 * 60 * 60 * 1000)) return 'Due Soon'
  return 'OK'
}

function statusClass(nextDue) {
  const s = vaxStatus(nextDue)
  return { Overdue: 'status-overdue', 'Due Soon': 'status-soon', OK: 'status-ok' }[s]
}

const filteredRecords = computed(() => {
  return records.value.filter(r => {
    if (filterCow.value && !String(r.cow_id).includes(filterCow.value)) return false
    if (filterVaccine.value && !r.vaccine_name?.toLowerCase().includes(filterVaccine.value.toLowerCase())) return false
    if (filterFrom.value && r.last_administered && r.last_administered < filterFrom.value) return false
    if (filterTo.value && r.last_administered && r.last_administered > filterTo.value) return false
    return true
  })
})
</script>

<style scoped>
.vax-view { padding: 20px; max-width: 1200px; margin: 0 auto; }
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
h1 { margin: 0; }
.add-btn { background: #3b82f6; color: white; border: none; padding: 8px 18px; border-radius: 8px; cursor: pointer; }
.add-form { background: white; border-radius: 10px; padding: 16px; margin-bottom: 16px; box-shadow: 0 1px 4px rgba(0,0,0,0.1); }
.add-form h3 { margin: 0 0 12px; }
.form-row { display: flex; gap: 12px; align-items: flex-end; flex-wrap: wrap; }
.form-row label { display: flex; flex-direction: column; gap: 4px; font-size: 0.9rem; }
.form-row input { padding: 6px 10px; border: 1px solid #d1d5db; border-radius: 6px; }
.btn-primary { background: #3b82f6; color: white; border: none; padding: 6px 16px; border-radius: 6px; cursor: pointer; height: fit-content; }
.filters { display: flex; gap: 10px; margin-bottom: 16px; flex-wrap: wrap; }
.filter-input { padding: 6px 10px; border: 1px solid #d1d5db; border-radius: 6px; font-size: 0.9rem; }
.clear-btn { background: #6b7280; color: white; border: none; padding: 6px 14px; border-radius: 6px; cursor: pointer; }
.table-wrap { background: white; border-radius: 10px; box-shadow: 0 1px 4px rgba(0,0,0,0.1); overflow: hidden; }
.table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
.table th, .table td { padding: 10px 14px; border-bottom: 1px solid #e5e7eb; text-align: left; }
.table th { background: #f9fafb; font-weight: 600; }
.table tr:hover { background: #f9fafb; }
.status-badge { padding: 2px 8px; border-radius: 12px; font-size: 0.8rem; font-weight: 600; }
.status-overdue { background: #fee2e2; color: #b91c1c; }
.status-soon { background: #fef9c3; color: #854d0e; }
.status-ok { background: #dcfce7; color: #166534; }
.empty { text-align: center; color: #9ca3af; padding: 24px; }
</style>
