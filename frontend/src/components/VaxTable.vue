<template>
  <div class="vax-table">
    <div class="table-header">
      <h3>💉 Vaccination Records</h3>
      <div class="controls">
        <select v-model="filterStatus" class="filter-select">
          <option value="">All Statuses</option>
          <option value="Overdue">Overdue</option>
          <option value="Due Soon">Due Soon</option>
          <option value="OK">OK</option>
        </select>
        <button class="add-btn" @click="$emit('record-vaccination')">+ Record Vaccination</button>
      </div>
    </div>
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
            <span :class="['status-badge', statusClass(row.next_due)]">
              {{ vaxStatus(row.next_due) }}
            </span>
          </td>
        </tr>
        <tr v-if="filteredRecords.length === 0">
          <td colspan="5" class="empty">No records found.</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  records: { type: Array, default: () => [] },
})

defineEmits(['record-vaccination'])

const filterStatus = ref('')

function vaxStatus(nextDue) {
  if (!nextDue) return 'OK'
  const today = new Date()
  const due = new Date(nextDue)
  if (due < today) return 'Overdue'
  const soon = new Date(today.getTime() + 7 * 24 * 60 * 60 * 1000)
  if (due <= soon) return 'Due Soon'
  return 'OK'
}

function statusClass(nextDue) {
  const s = vaxStatus(nextDue)
  return { Overdue: 'status-overdue', 'Due Soon': 'status-soon', OK: 'status-ok' }[s]
}

const filteredRecords = computed(() => {
  if (!filterStatus.value) return props.records
  return props.records.filter(r => vaxStatus(r.next_due) === filterStatus.value)
})
</script>

<style scoped>
.vax-table { background: white; border-radius: 8px; padding: 16px; box-shadow: 0 1px 4px rgba(0,0,0,0.1); }
.table-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
h3 { margin: 0; }
.controls { display: flex; gap: 8px; align-items: center; }
.filter-select { padding: 4px 8px; border-radius: 4px; border: 1px solid #d1d5db; }
.add-btn { background: #3b82f6; color: white; border: none; padding: 6px 14px; border-radius: 6px; cursor: pointer; }
.add-btn:hover { background: #2563eb; }
.table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
.table th, .table td { padding: 8px 12px; border-bottom: 1px solid #e5e7eb; text-align: left; }
.table th { background: #f9fafb; font-weight: 600; }
.table tr:hover { background: #f9fafb; }
.status-badge { padding: 2px 8px; border-radius: 12px; font-size: 0.8rem; font-weight: 600; }
.status-overdue { background: #fee2e2; color: #b91c1c; }
.status-soon { background: #fef9c3; color: #854d0e; }
.status-ok { background: #dcfce7; color: #166534; }
.empty { text-align: center; color: #9ca3af; padding: 20px; }
</style>
