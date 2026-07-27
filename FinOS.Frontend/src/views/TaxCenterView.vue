<template>
  <div class="space-y-6">
    <header><h1 class="text-2xl font-bold">Tax Center</h1><p class="text-sm text-gray-500">Financial-year-specific tax preparation for India.</p></header>
    <div class="flex gap-3"><label class="text-sm">Financial Year<input v-model="fy" class="ml-2 rounded-lg border-gray-300" placeholder="2026-27" @change="load"></label><span class="rounded-full px-3 py-1 text-sm" :class="store.rules.length?'bg-green-100 text-green-800':'bg-amber-100 text-amber-800'">{{ store.rules.length ? 'Rules configured' : 'Rules not configured' }}</span></div>
    <ErrorState v-if="store.error" :message="store.error" @retry="load" />
    <nav class="flex gap-2 overflow-x-auto border-b"><button v-for="tab in tabs" :key="tab" class="px-3 py-2 text-sm" :class="active===tab?'border-b-2 border-indigo-600 font-semibold':''" @click="active=tab">{{ tab }}</button></nav>
    <form class="rounded-xl border bg-white p-6" @submit.prevent="save">
      <div v-if="active==='Overview'" class="grid gap-4 sm:grid-cols-2">
        <label class="text-sm">Preferred regime<select v-model="regime" class="mt-1 w-full rounded-lg border-gray-300"><option value="">Not selected</option><option>Old</option><option>New</option></select></label>
        <div class="rounded-lg bg-gray-50 p-4"><p class="text-sm text-gray-500">Estimated tax</p><b>{{ store.rules.length ? 'Save, then calculate from published rules' : 'Unavailable until a rule version is published' }}</b></div>
      </div>
      <div v-else-if="active==='Income'" class="grid gap-4 sm:grid-cols-2"><label v-for="field in incomeFields" :key="field.key" class="text-sm">{{ field.label }}<input v-model.number="input[field.key]" min="0" type="number" class="mt-1 w-full rounded-lg border-gray-300"></label></div>
      <div v-else-if="active==='Deductions'" class="grid gap-4 sm:grid-cols-2"><label class="text-sm">Recorded deductions<input v-model.number="input.deductions" min="0" type="number" class="mt-1 w-full rounded-lg border-gray-300"><span class="mt-1 block text-xs text-gray-500">The published rule decides whether and how much is eligible.</span></label></div>
      <div v-else-if="active==='Capital Gains'" class="grid gap-4 sm:grid-cols-2"><label class="text-sm">Capital gains<input v-model.number="input.capitalGains" min="0" type="number" class="mt-1 w-full rounded-lg border-gray-300"><span class="mt-1 block text-xs text-gray-500">Excluded with a warning unless the rule explicitly configures its treatment.</span></label></div>
      <div v-else-if="active==='TDS'" class="grid gap-4 sm:grid-cols-2"><label class="text-sm">TDS paid<input v-model.number="input.tdsPaid" min="0" type="number" class="mt-1 w-full rounded-lg border-gray-300"></label><label class="text-sm">Advance / self-assessment tax<input v-model.number="input.otherTaxPaid" min="0" type="number" class="mt-1 w-full rounded-lg border-gray-300"></label></div>
      <p v-else class="text-sm text-gray-500">{{ active }} inputs are stored in the versioned profile architecture; structured workflows will be added without hard-coded rules.</p>
      <button class="mt-5 rounded-lg bg-indigo-600 px-5 py-2 text-white">Save profile</button>
      <button type="button" class="ml-2 mt-5 rounded-lg border px-5 py-2" :disabled="store.loading || !store.profile" @click="store.calculate(fy)">Calculate comparison</button>
    </form>
    <section v-if="store.comparison" class="space-y-3 rounded-xl border bg-white p-6">
      <div class="grid gap-4 md:grid-cols-2">
        <article v-for="result in [store.comparison.old,store.comparison.new]" :key="result.regime" class="rounded-lg border p-4">
          <h2 class="font-semibold">{{ result.regime }} regime <span class="text-xs text-gray-500">{{ result.ruleVersion }}</span></h2>
          <p v-if="result.available" class="mt-2 text-2xl font-bold">{{ money(result.estimatedTax) }}</p>
          <p v-else class="mt-2 text-amber-700">Unavailable</p>
          <dl v-if="result.available" class="mt-3 grid grid-cols-2 gap-2 text-sm"><dt>Gross income</dt><dd>{{ money(result.grossIncome) }}</dd><dt>Taxable income</dt><dd>{{ money(result.taxableIncome) }}</dd><dt>Taxes paid</dt><dd>{{ money(result.taxesPaid) }}</dd><dt>Payable / refund</dt><dd>{{ money(result.estimatedPayableOrRefund) }}</dd></dl>
          <p v-for="warning in result.warnings" :key="warning" class="mt-2 text-xs text-amber-700">{{ warning }}</p>
        </article>
      </div>
      <p class="text-sm text-gray-600">{{ store.comparison.explanation }}</p>
    </section>
  </div>
</template>
<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useTaxStore } from '../stores/tax'
import ErrorState from '../components/ErrorState.vue'
import { formatMoney as money } from '../utils/formatters'
const store=useTaxStore(), fy=ref('2026-27'), active=ref('Overview'), regime=ref('')
const tabs=['Overview','Income','Deductions','Capital Gains','TDS','Tax Regime','Documents']
const input=reactive({salary:0,interest:0,dividend:0,rentalIncome:0,capitalGains:0,otherIncome:0,deductions:0,tdsPaid:0,otherTaxPaid:0})
const incomeFields=[['salary','Salary'],['interest','Interest'],['dividend','Dividend'],['rentalIncome','Rental income'],['capitalGains','Capital gains'],['otherIncome','Other income']].map(([key,label])=>({key,label}))
const load=async()=>{await store.load(fy.value); if(store.profile){regime.value=store.profile.preferredRegime||''; Object.assign(input,JSON.parse(store.profile.inputJson||'{}'))}}
const save=()=>store.save(fy.value,regime.value||null,input)
onMounted(load)
</script>
