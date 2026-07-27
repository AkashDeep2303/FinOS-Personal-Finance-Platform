<template>
  <div class="space-y-6">
    <header>
      <h1 class="text-2xl font-bold text-gray-900">Data Center</h1>
      <p class="mt-1 text-sm text-gray-500">Understand the sources, imports, documents, and quality of the data FinOS uses.</p>
    </header>

    <LoadingState v-if="store.loading && !store.overview" message="Reviewing your financial data..." />
    <ErrorState v-else-if="store.error && !store.overview" :message="store.error" @retry="store.load" />

    <template v-else>
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard title="Data quality score" :value="`${overview.dataQualityScore}%`" />
        <StatCard title="Open issues" :value="String(overview.openIssueCount)" />
        <StatCard title="Rows imported" :value="formatNumber(overview.importedRowCount)" />
        <StatCard title="Failed rows" :value="formatNumber(overview.failedRowCount)" />
      </div>

      <div class="flex gap-2 overflow-x-auto border-b border-gray-200" role="tablist" aria-label="Data Center sections">
        <button
          v-for="tab in tabs"
          :key="tab"
          type="button"
          class="whitespace-nowrap border-b-2 px-4 py-3 text-sm font-medium"
          :class="activeTab === tab ? 'border-indigo-600 text-indigo-700' : 'border-transparent text-gray-500 hover:text-gray-800'"
          :aria-selected="activeTab === tab"
          role="tab"
          @click="activeTab = tab"
        >
          {{ tab }}
        </button>
      </div>

      <section v-if="activeTab === 'Connections'">
        <div class="mb-4 rounded-xl border border-blue-200 bg-blue-50 p-4 text-sm text-blue-900">
          Sources are manual-import profiles only. FinOS stores no banking credentials and does not claim live synchronization.
        </div>
        <form class="mb-5 grid gap-3 rounded-xl border border-gray-200 bg-white p-5 md:grid-cols-2" @submit.prevent="addSource">
          <select v-model="sourceForm.sourceType" required class="rounded-lg border border-gray-300 px-3 py-2">
            <option v-for="type in sourceTypes" :key="type.value" :value="type.value">{{ type.label }}</option>
          </select>
          <input v-model.trim="sourceForm.displayName" required maxlength="150" placeholder="Display name" class="rounded-lg border border-gray-300 px-3 py-2">
          <input v-model.trim="sourceForm.institutionName" maxlength="150" placeholder="Institution (optional)" class="rounded-lg border border-gray-300 px-3 py-2 md:col-span-2">
          <button :disabled="savingSource" class="rounded-lg bg-indigo-600 px-4 py-2 font-medium text-white disabled:opacity-50 md:col-span-2">
            {{ savingSource ? 'Adding...' : 'Add manual source' }}
          </button>
          <p v-if="sourceError" class="text-sm text-red-600 md:col-span-2">{{ sourceError }}</p>
        </form>
        <EmptyState v-if="!store.sources.length" title="No data sources" description="Add a manual source to organize future imports without connecting an external account." />
        <div v-else class="grid gap-4 md:grid-cols-2">
        <article v-for="source in store.sources" :key="source.id" class="rounded-xl border border-gray-200 bg-white p-5">
          <div class="flex items-start justify-between gap-4">
            <div>
              <h2 class="font-semibold text-gray-900">{{ source.displayName }}</h2>
              <p class="mt-1 text-sm text-gray-500">{{ sourceTypeLabel(source.sourceType) }}<span v-if="source.institutionName"> · {{ source.institutionName }}</span></p>
            </div>
            <FinancialStatusBadge status="neutral" :label="source.connectionMode" />
          </div>
          <div class="mt-4 flex items-center justify-between">
            <span class="text-xs text-gray-500">Last import: {{ source.lastImportedAt ? formatIndianDate(source.lastImportedAt) : 'Never' }}</span>
            <button class="text-sm font-medium text-red-600" @click="removeSource(source.id)">Remove</button>
          </div>
        </article>
        </div>
      </section>

      <section v-else-if="activeTab === 'Imports'">
        <div class="mb-5 rounded-xl border border-gray-200 bg-white p-5">
          <h2 class="font-semibold text-gray-900">Preview bank CSV</h2>
          <p class="mt-1 text-sm text-gray-500">Validate columns before importing. Preview does not save the file or create transactions.</p>
          <div class="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center">
            <input ref="csvInput" type="file" accept=".csv,text/csv" class="block text-sm text-gray-600" @change="selectCsv">
            <button :disabled="!selectedCsv || previewingCsv" class="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-50" @click="previewCsv">
              {{ previewingCsv ? 'Validating...' : 'Preview CSV' }}
            </button>
          </div>
          <p class="mt-2 text-xs text-gray-500">UTF-8 CSV only. Maximum 2 MB, 5,000 data rows, 50 columns, and 500 characters per cell.</p>
          <p v-if="csvError" class="mt-2 text-sm text-red-600">{{ csvError }}</p>
          <div v-if="store.csvPreview" class="mt-5 overflow-x-auto">
            <p class="mb-2 text-sm text-gray-600">{{ formatNumber(store.csvPreview.dataRowCount) }} rows detected. Showing up to 5.</p>
            <table class="min-w-full border-collapse text-sm">
              <thead><tr><th v-for="header in store.csvPreview.headers" :key="header" class="border bg-gray-50 p-2 text-left">{{ header }}</th></tr></thead>
              <tbody><tr v-for="(row, rowIndex) in store.csvPreview.sampleRows" :key="rowIndex"><td v-for="(cell, cellIndex) in row" :key="cellIndex" class="max-w-xs truncate border p-2">{{ cell }}</td></tr></tbody>
            </table>
            <div class="mt-5 rounded-lg border border-gray-200 p-4">
              <h3 class="font-semibold text-gray-900">Column mapping</h3>
              <p class="mt-1 text-xs text-gray-500">Review suggested mappings. Date, description, and either amount or both debit and credit are required.</p>
              <div class="mt-4 grid gap-3 md:grid-cols-2">
                <label v-for="field in mappingFields" :key="field.key" class="text-sm">
                  <span class="mb-1 block font-medium text-gray-700">{{ field.label }}<span v-if="field.required" class="text-red-600"> *</span></span>
                  <select v-model="store.csvMapping[field.key]" class="w-full rounded-lg border border-gray-300 px-3 py-2" @change="store.csvMappingValidation = null">
                    <option :value="undefined">Not mapped</option>
                    <option v-for="header in store.csvPreview.headers" :key="header" :value="header">{{ header }}</option>
                  </select>
                  <span v-for="error in mappingErrors(field.key)" :key="error" class="mt-1 block text-xs text-red-600">{{ error }}</span>
                </label>
              </div>
              <button :disabled="validatingMapping" class="mt-4 rounded-lg border border-indigo-200 px-4 py-2 text-sm font-medium text-indigo-700 disabled:opacity-50" @click="validateMapping">
                {{ validatingMapping ? 'Validating...' : 'Validate mapping' }}
              </button>
              <FinancialStatusBadge v-if="store.csvMappingValidation?.isValid" class="ml-3" status="positive" label="Mapping ready" />
              <p v-else-if="store.csvMappingValidation" class="mt-2 text-sm text-red-600">Resolve the mapping issues before importing.</p>
            </div>
            <div v-if="store.csvMappingValidation?.isValid" class="mt-4 rounded-lg border border-gray-200 p-4">
              <h3 class="font-semibold text-gray-900">Validate transaction rows</h3>
              <label class="mt-3 block max-w-sm text-sm">
                <span class="mb-1 block text-gray-600">For a single signed Amount column, positive values mean</span>
                <select v-model="positiveAmountType" class="w-full rounded-lg border border-gray-300 px-3 py-2">
                  <option value="Income">Income / credit</option>
                  <option value="Expense">Expense / debit</option>
                </select>
              </label>
              <button :disabled="validatingTransactions" class="mt-3 rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-50" @click="validateTransactions">
                {{ validatingTransactions ? 'Checking rows...' : 'Validate transaction rows' }}
              </button>
              <div v-if="store.csvTransactionValidation" class="mt-4">
                <div class="grid gap-3 sm:grid-cols-3">
                  <StatCard title="Total rows" :value="formatNumber(store.csvTransactionValidation.totalRows)" />
                  <StatCard title="Valid rows" :value="formatNumber(store.csvTransactionValidation.validRows)" />
                  <StatCard title="Invalid rows" :value="formatNumber(store.csvTransactionValidation.invalidRows)" />
                </div>
                <div v-if="store.csvTransactionValidation.sampleTransactions.length" class="mt-4 overflow-x-auto">
                  <table class="min-w-full text-sm"><thead><tr><th class="p-2 text-left">Date</th><th class="p-2 text-left">Description</th><th class="p-2 text-left">Type</th><th class="p-2 text-right">Amount</th></tr></thead>
                    <tbody><tr v-for="item in store.csvTransactionValidation.sampleTransactions" :key="item.rowNumber" class="border-t"><td class="p-2">{{ item.transactionDate }}</td><td class="p-2">{{ item.description }}</td><td class="p-2">{{ item.type }}</td><td class="p-2 text-right">{{ item.amount }}</td></tr></tbody>
                  </table>
                </div>
                <div v-if="store.csvTransactionValidation.errors.length" class="mt-4 rounded-lg bg-red-50 p-3">
                  <p class="font-medium text-red-900">Rows requiring correction</p>
                  <p v-for="error in store.csvTransactionValidation.errors" :key="error.rowNumber" class="mt-1 text-sm text-red-700">Row {{ error.rowNumber }}: {{ error.errors.join(' ') }}</p>
                </div>
                <div v-if="store.csvTransactionValidation.invalidRows === 0" class="mt-4 rounded-lg border border-gray-200 p-4">
                  <h4 class="font-medium text-gray-900">Duplicate check</h4>
                  <select v-model.number="selectedAccountId" class="mt-2 w-full max-w-sm rounded-lg border border-gray-300 px-3 py-2">
                    <option :value="null">Select destination account</option>
                    <option v-for="account in accountsStore.accounts" :key="account.id" :value="account.id">{{ account.name }} · {{ account.bank || 'FinOS account' }}</option>
                  </select>
                  <button :disabled="!selectedAccountId || checkingDuplicates" class="ml-0 mt-2 rounded-lg border border-indigo-200 px-4 py-2 text-sm font-medium text-indigo-700 disabled:opacity-50 sm:ml-2" @click="checkDuplicates">
                    {{ checkingDuplicates ? 'Checking...' : 'Check existing transactions' }}
                  </button>
                  <div v-if="store.csvDuplicateAnalysis" class="mt-3">
                    <FinancialStatusBadge
                      :status="store.csvDuplicateAnalysis.duplicateRows ? 'warning' : 'positive'"
                      :label="store.csvDuplicateAnalysis.duplicateRows ? `${store.csvDuplicateAnalysis.duplicateRows} possible duplicates` : 'No duplicates found'"
                    />
                    <p v-for="match in store.csvDuplicateAnalysis.matches" :key="match.rowNumber" class="mt-1 text-sm text-amber-700">
                      CSV row {{ match.rowNumber }}
                      <template v-if="match.existingTransactionId">matches transaction #{{ match.existingTransactionId }}</template>
                      <template v-else>repeats CSV row {{ match.matchingRowNumber }}</template>
                      by {{ match.matchReason }}.
                    </p>
                    <select v-model="duplicatePolicy" class="mt-3 rounded-lg border border-gray-300 px-3 py-2 text-sm"><option value="Skip">Skip possible duplicates</option><option value="Include">Include duplicates explicitly</option></select>
                    <button :disabled="importingCsv" class="ml-2 rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white disabled:opacity-50" @click="confirmImport">{{ importingCsv?'Importing...':'Confirm import' }}</button>
                    <p v-if="store.csvImportResult" class="mt-3 text-sm font-medium text-green-700">Imported {{store.csvImportResult.importedRows}} rows. Balance change: {{store.csvImportResult.balanceDelta}}.</p>
                  </div>
                </div>
              </div>
            </div>
            <p class="mt-3 text-sm text-amber-700">Preview, mapping, and validation are not persisted. Transactions are created only after you confirm the import.</p>
          </div>
        </div>
        <EmptyState
          v-if="!overview.recentImports.length"
          title="No import history"
          description="Import processing is supported by the backend schema, but no batches have been recorded for your account."
        />
        <div v-else class="overflow-x-auto rounded-xl border border-gray-200 bg-white">
          <table class="w-full min-w-[760px]">
            <thead class="bg-gray-50 text-left text-xs uppercase text-gray-500">
              <tr><th class="p-4">File</th><th class="p-4">Source</th><th class="p-4">Status</th><th class="p-4">Successful</th><th class="p-4">Failed</th><th class="p-4">Imported</th></tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              <tr v-for="batch in overview.recentImports" :key="batch.id">
                <td class="p-4 font-medium text-gray-900">{{ batch.fileName }}</td>
                <td class="p-4 text-gray-600">{{ batch.source }}</td>
                <td class="p-4"><FinancialStatusBadge :status="importStatus(batch.status)" :label="batch.status" /></td>
                <td class="p-4">{{ formatNumber(batch.successRows) }}</td>
                <td class="p-4">{{ formatNumber(batch.failedRows) }}</td>
                <td class="p-4 text-gray-600">{{ formatIndianDate(batch.createdAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="mt-6">
          <h2 class="text-lg font-semibold text-gray-900">Import reconciliation</h2>
          <p class="mt-1 text-sm text-gray-500">Review processing failures without exposing the original imported row content.</p>
          <EmptyState
            v-if="!store.reconciliationIssues.length"
            title="No unresolved import errors"
            description="There are no import rows waiting for reconciliation."
          />
          <div v-else class="mt-4 space-y-3">
            <article v-for="issue in store.reconciliationIssues" :key="issue.id" class="rounded-xl border border-gray-200 bg-white p-5">
              <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
                <div>
                  <h3 class="font-semibold text-gray-900">{{ issue.fileName }} · row {{ issue.rowNumber }}</h3>
                  <p class="mt-1 text-xs text-gray-500">{{ issue.source }} · {{ formatIndianDate(issue.createdAt) }}</p>
                  <p class="mt-3 text-sm text-gray-700">{{ issue.errorReason }}</p>
                </div>
                <button
                  :disabled="resolvingIssueId === issue.id"
                  class="whitespace-nowrap rounded-lg border border-indigo-200 px-3 py-2 text-sm font-medium text-indigo-700 disabled:opacity-50"
                  @click="resolveIssue(issue.id)"
                >
                  {{ resolvingIssueId === issue.id ? 'Resolving...' : 'Mark resolved' }}
                </button>
              </div>
            </article>
          </div>
          <p v-if="reconciliationError" class="mt-3 text-sm text-red-600">{{ reconciliationError }}</p>
        </div>
      </section>

      <section v-else-if="activeTab === 'Documents'">
        <div class="mb-4 rounded-xl border border-indigo-200 bg-indigo-50 p-4 text-sm text-indigo-900">
          Files are stored privately and retrieved only through your authenticated account. Allowed formats: PDF, JPG, PNG, CSV, and XLSX, up to 10 MB.
        </div>
        <form class="mb-5 grid gap-3 rounded-xl border border-gray-200 bg-white p-5 md:grid-cols-2" @submit.prevent="addDocument">
          <select v-model="documentForm.documentType" required class="rounded-lg border border-gray-300 px-3 py-2">
            <option v-for="type in documentTypes" :key="type.value" :value="type.value">{{ type.label }}</option>
          </select>
          <input v-model.trim="documentForm.title" required maxlength="200" placeholder="Document title" class="rounded-lg border border-gray-300 px-3 py-2">
          <input v-model.trim="documentForm.issuer" maxlength="150" placeholder="Issuer (optional)" class="rounded-lg border border-gray-300 px-3 py-2">
          <input v-model.trim="documentForm.financialYear" pattern="\d{4}-\d{2}" placeholder="Financial year, e.g. 2025-26" class="rounded-lg border border-gray-300 px-3 py-2">
          <input v-model="documentForm.documentDate" type="date" :max="today" class="rounded-lg border border-gray-300 px-3 py-2">
          <input v-model.trim="documentForm.notes" maxlength="500" placeholder="Notes (optional)" class="rounded-lg border border-gray-300 px-3 py-2">
          <input ref="documentInput" type="file" accept=".pdf,.jpg,.jpeg,.png,.csv,.xlsx" class="rounded-lg border border-gray-300 px-3 py-2 md:col-span-2" @change="selectDocumentFile">
          <button :disabled="savingDocument" class="rounded-lg bg-indigo-600 px-4 py-2 font-medium text-white disabled:opacity-50 md:col-span-2">
            {{ savingDocument ? 'Saving...' : 'Save document' }}
          </button>
          <p v-if="documentError" class="text-sm text-red-600 md:col-span-2">{{ documentError }}</p>
        </form>
        <EmptyState v-if="!store.documents.length" title="No documents recorded" description="Record Form 16, statements, policies, and other financial-document metadata here." />
        <div v-else class="grid gap-4 lg:grid-cols-2">
          <article v-for="document in store.documents" :key="document.id" class="rounded-xl border border-gray-200 bg-white p-5">
            <div class="flex items-start justify-between gap-3">
              <div><h2 class="font-semibold text-gray-900">{{ document.title }}</h2><p class="mt-1 text-xs text-gray-500">{{ documentTypeLabel(document.documentType) }}</p></div>
              <div class="flex gap-3">
                <button v-if="document.hasFile" class="text-sm font-medium text-indigo-600" @click="downloadDocument(document)">Download</button>
                <button class="text-sm font-medium text-red-600" @click="removeDocument(document.id)">Remove</button>
              </div>
            </div>
            <dl class="mt-4 grid grid-cols-2 gap-3 text-sm">
              <div><dt class="text-gray-500">Issuer</dt><dd>{{ document.issuer || 'Not recorded' }}</dd></div>
              <div><dt class="text-gray-500">Financial year</dt><dd>{{ document.financialYear || 'Not recorded' }}</dd></div>
              <div><dt class="text-gray-500">Document date</dt><dd>{{ document.documentDate ? formatIndianDate(document.documentDate) : 'Not recorded' }}</dd></div>
              <div><dt class="text-gray-500">Status</dt><dd>{{ document.status }}</dd></div>
              <div><dt class="text-gray-500">File</dt><dd>{{ document.hasFile ? `${document.originalFileName} (${formatFileSize(document.fileSizeBytes)})` : 'Metadata only' }}</dd></div>
            </dl>
          </article>
        </div>
      </section>

      <section v-else class="space-y-4">
        <div class="rounded-xl border border-indigo-100 bg-indigo-50 p-5">
          <h2 class="font-semibold text-indigo-950">How the score is calculated</h2>
          <p class="mt-1 text-sm text-indigo-800">{{ overview.scoreCalculation }}</p>
          <p v-if="overview.unresolvedImportErrorCount" class="mt-2 text-sm text-indigo-800">
            {{ overview.unresolvedImportErrorCount }} unresolved import errors are included. Raw imported rows are never exposed here.
          </p>
        </div>
        <EmptyState
          v-if="!overview.issues.length"
          title="No data-quality issues detected"
          description="FinOS did not find an issue in the current deterministic quality checks."
        />
        <div v-else class="grid gap-4 lg:grid-cols-2">
          <article v-for="issue in overview.issues" :key="`${issue.issueType}-${issue.entityId}`" class="rounded-xl border border-gray-200 bg-white p-5">
            <div class="flex items-start justify-between gap-3">
              <div>
                <h2 class="font-semibold text-gray-900">{{ issueTitle(issue.issueType) }}</h2>
                <p class="mt-1 text-xs text-gray-500">{{ issue.entityType }} · {{ formatIndianDate(issue.issueDetectedAt) }}</p>
              </div>
              <FinancialStatusBadge status="warning" label="Review" />
            </div>
            <p class="mt-3 text-sm text-gray-700">{{ issue.issueDescription }}</p>
          </article>
        </div>
      </section>
    </template>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { useDataCenterStore } from '../stores/dataCenter'
import { useAccountsStore } from '../stores/accounts'
import { formatIndianDate } from '../utils/formatters'
import EmptyState from '../components/EmptyState.vue'
import ErrorState from '../components/ErrorState.vue'
import FinancialStatusBadge from '../components/FinancialStatusBadge.vue'
import LoadingState from '../components/LoadingState.vue'
import StatCard from '../components/StatCard.vue'

const store = useDataCenterStore()
const accountsStore = useAccountsStore()
const activeTab = ref('Data Quality')
const savingDocument = ref(false)
const documentInput = ref(null)
const selectedDocumentFile = ref(null)
const savingSource = ref(false)
const documentError = ref(null)
const sourceError = ref(null)
const reconciliationError = ref(null)
const resolvingIssueId = ref(null)
const csvInput = ref(null)
const selectedCsv = ref(null)
const previewingCsv = ref(false)
const csvError = ref(null)
const validatingMapping = ref(false)
const validatingTransactions = ref(false)
const positiveAmountType = ref('Income')
const selectedAccountId = ref(null)
const checkingDuplicates = ref(false)
const duplicatePolicy = ref('Skip')
const importingCsv = ref(false)
const today = new Date().toISOString().slice(0, 10)
const tabs = ['Connections', 'Imports', 'Documents', 'Data Quality']
const overview = computed(() => store.overview ?? {
  dataQualityScore: 100,
  openIssueCount: 0,
  unresolvedImportErrorCount: 0,
  importedRowCount: 0,
  failedRowCount: 0,
  recentImports: [],
  issues: [],
  scoreCalculation: ''
})
const sourceTypes = [
  ['Bank', 'Bank'], ['Broker', 'Broker'], ['MutualFund', 'Mutual fund'],
  ['Salary', 'Salary'], ['Tax', 'Tax'], ['Loan', 'Loan'], ['EPF', 'EPF'], ['Other', 'Other']
].map(([value, label]) => ({ value, label }))
const sourceForm = reactive({ sourceType: 'Bank', displayName: '', institutionName: '' })
const documentTypes = [
  ['BankStatement', 'Bank statement'], ['BrokerStatement', 'Broker statement'],
  ['MutualFundStatement', 'Mutual fund statement'], ['SalarySlip', 'Salary slip'],
  ['Form16', 'Form 16'], ['LoanStatement', 'Loan statement'], ['EPF', 'EPF'],
  ['Insurance', 'Insurance'], ['Tax', 'Tax'], ['Other', 'Other']
].map(([value, label]) => ({ value, label }))
const documentForm = reactive({
  documentType: 'BankStatement', title: '', issuer: '', financialYear: '', documentDate: '', notes: ''
})
const mappingFields = [
  { key: 'transactionDate', label: 'Transaction date', required: true },
  { key: 'description', label: 'Description', required: true },
  { key: 'amount', label: 'Amount', required: false },
  { key: 'debit', label: 'Debit / withdrawal', required: false },
  { key: 'credit', label: 'Credit / deposit', required: false },
  { key: 'referenceNumber', label: 'Reference number', required: false },
  { key: 'type', label: 'Transaction type / Dr-Cr', required: false }
]

function formatNumber(value) {
  return new Intl.NumberFormat('en-IN').format(value ?? 0)
}

function formatFileSize(bytes) {
  if (!bytes) return '0 bytes'
  if (bytes < 1024 * 1024) return `${Math.ceil(bytes / 1024)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function selectDocumentFile(event) {
  selectedDocumentFile.value = event.target.files?.[0] ?? null
}

function importStatus(status) {
  const normalized = String(status || '').toLowerCase()
  if (['completed', 'success', 'processed'].includes(normalized)) return 'positive'
  if (['failed', 'error'].includes(normalized)) return 'negative'
  if (['processing', 'pending'].includes(normalized)) return 'warning'
  return 'neutral'
}

function issueTitle(value) {
  return String(value || 'Data issue').replace(/([a-z])([A-Z])/g, '$1 $2')
}

function documentTypeLabel(value) {
  return documentTypes.find(type => type.value === value)?.label ?? value
}

function sourceTypeLabel(value) {
  return sourceTypes.find(type => type.value === value)?.label ?? value
}

async function addSource() {
  savingSource.value = true
  sourceError.value = null
  try {
    await store.addSource({
      ...sourceForm,
      institutionName: sourceForm.institutionName || null
    })
    Object.assign(sourceForm, { sourceType: 'Bank', displayName: '', institutionName: '' })
  } catch (error) {
    sourceError.value = error.response?.data?.message || 'Could not add the data source.'
  } finally {
    savingSource.value = false
  }
}

async function removeSource(id) {
  try {
    await store.deleteSource(id)
  } catch (error) {
    sourceError.value = error.response?.data?.message || 'Could not remove the data source.'
  }
}

async function addDocument() {
  savingDocument.value = true
  documentError.value = null
  try {
    await store.addDocument({
      ...documentForm,
      issuer: documentForm.issuer || null,
      financialYear: documentForm.financialYear || null,
      documentDate: documentForm.documentDate || null,
      notes: documentForm.notes || null
    }, selectedDocumentFile.value)
    Object.assign(documentForm, { documentType: 'BankStatement', title: '', issuer: '', financialYear: '', documentDate: '', notes: '' })
    selectedDocumentFile.value = null
    if (documentInput.value) documentInput.value.value = ''
  } catch (error) {
    documentError.value = error.response?.data?.message || 'Could not record the document.'
  } finally {
    savingDocument.value = false
  }
}

async function downloadDocument(document) {
  documentError.value = null
  try {
    const response = await store.downloadDocument(document.id)
    const url = URL.createObjectURL(response.data)
    const link = window.document.createElement('a')
    link.href = url
    link.download = document.originalFileName || 'financial-document'
    link.click()
    URL.revokeObjectURL(url)
  } catch (error) {
    documentError.value = error.response?.data?.message || 'Could not download the document.'
  }
}

async function removeDocument(id) {
  try {
    await store.deleteDocument(id)
  } catch (error) {
    documentError.value = error.response?.data?.message || 'Could not remove the document.'
  }
}

async function resolveIssue(id) {
  resolvingIssueId.value = id
  reconciliationError.value = null
  try {
    await store.resolveIssue(id)
  } catch (error) {
    reconciliationError.value = error.response?.data?.message || 'Could not resolve the import issue.'
  } finally {
    resolvingIssueId.value = null
  }
}

function selectCsv(event) {
  selectedCsv.value = event.target.files?.[0] ?? null
  store.csvPreview = null
  store.csvMapping = {}
  store.csvMappingValidation = null
  store.csvTransactionValidation = null
  store.csvDuplicateAnalysis = null
  csvError.value = null
}

function mappingErrors(field) {
  return store.csvMappingValidation?.errors?.[field] ?? []
}

async function validateMapping() {
  validatingMapping.value = true
  csvError.value = null
  try {
    await store.validateCsvMapping()
  } catch (error) {
    csvError.value = error.response?.data?.message || 'Could not validate the column mapping.'
  } finally {
    validatingMapping.value = false
  }
}

async function validateTransactions() {
  if (!selectedCsv.value) return
  validatingTransactions.value = true
  csvError.value = null
  try {
    await store.validateCsvTransactions(selectedCsv.value, positiveAmountType.value)
  } catch (error) {
    csvError.value = error.response?.data?.errors?.file?.[0] || error.response?.data?.message || 'Could not validate transaction rows.'
    store.csvTransactionValidation = null
  } finally {
    validatingTransactions.value = false
  }
}

async function previewCsv() {
  if (!selectedCsv.value) return
  previewingCsv.value = true
  csvError.value = null
  try {
    await store.previewCsv(selectedCsv.value)
  } catch (error) {
    csvError.value = error.response?.data?.errors?.file?.[0] || error.response?.data?.message || 'Could not preview the CSV file.'
    store.csvPreview = null
  } finally {
    previewingCsv.value = false
  }
}

async function checkDuplicates() {
  if (!selectedCsv.value || !selectedAccountId.value) return
  checkingDuplicates.value = true
  csvError.value = null
  try {
    await store.checkCsvDuplicates(selectedCsv.value, positiveAmountType.value, selectedAccountId.value)
  } catch (error) {
    csvError.value = error.response?.data?.message || 'Could not check for duplicate transactions.'
    store.csvDuplicateAnalysis = null
  } finally {
    checkingDuplicates.value = false
  }
}

async function confirmImport(){
  importingCsv.value=true;csvError.value=null
  try{await store.confirmCsvImport(selectedCsv.value,positiveAmountType.value,selectedAccountId.value,duplicatePolicy.value);await accountsStore.fetchAccounts()}
  catch(error){csvError.value=error.response?.data?.errors?.file?.[0]||error.response?.data?.message||'Could not import transactions.'}
  finally{importingCsv.value=false}
}

onMounted(() => Promise.all([store.load(), accountsStore.fetchAccounts()]))
</script>
