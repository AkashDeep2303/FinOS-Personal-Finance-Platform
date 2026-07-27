<template>
<div class="space-y-6">
 <div><h1 class="text-2xl font-bold">Investments</h1><p class="text-sm text-gray-500">Track your portfolio, SIP schedules and EPF retirement savings.</p></div>
 <div v-if="store.error" class="p-3 rounded bg-red-50 text-red-700">{{store.error}}</div>
 <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
  <Stat label="Total Invested" :value="money(store.totalInvested)"/><Stat label="Current Value" :value="money(store.currentValue)"/>
  <Stat label="Returns" :value="money(store.totalReturns)"/><Stat label="Returns %" :value="store.returnsPercentage+'%'"/>
 </div>
 <div v-if="store.portfolioSummary" class="grid gap-4 lg:grid-cols-3">
  <div class="card lg:col-span-2">
   <h2 class="font-semibold">Asset Allocation</h2>
   <div v-if="store.portfolioSummary.assetAllocation?.length" class="mt-4 space-y-3">
    <div v-for="item in store.portfolioSummary.assetAllocation" :key="item.assetClassName">
     <div class="mb-1 flex justify-between text-sm"><span>{{item.assetClassName}}</span><b>{{item.allocationPct}}%</b></div>
     <div class="h-2 rounded-full bg-gray-100"><div class="h-2 rounded-full bg-primary-600" :style="{width:Math.min(100,item.allocationPct)+'%'}"></div></div>
    </div>
   </div>
   <p v-else class="mt-3 text-sm text-gray-500">Add holdings to see allocation.</p>
  </div>
  <div class="card">
   <h2 class="font-semibold">Concentration</h2>
   <p class="mt-4 text-3xl font-bold">{{store.portfolioSummary.largestHoldingPct||0}}%</p>
   <p class="text-sm text-gray-500">Largest holding weight</p>
   <p class="mt-4 text-sm" :class="store.portfolioSummary.concentratedHoldingCount?'text-amber-700':'text-green-700'">
    {{store.portfolioSummary.concentratedHoldingCount||0}} holding(s) at or above 20%
   </p>
  </div>
 </div>
 <nav class="flex gap-6 border-b">
  <button v-for="t in tabs" :key="t.id" @click="tab=t.id" class="py-2 border-b-2" :class="tab===t.id?'border-primary-600 text-primary-600':'border-transparent'">{{t.label}}</button>
 </nav>

 <section v-if="tab==='portfolio'" class="grid md:grid-cols-3 gap-4">
  <div v-for="x in store.investments" :key="x.id" class="card"><h3 class="font-semibold">{{x.name}}</h3><p class="text-sm text-gray-500">{{x.symbol}}</p><div class="mt-3 flex justify-between"><span>{{money(x.investedAmount)}}</span><b>{{money(x.currentValue)}}</b></div></div>
  <p v-if="!store.investments.length" class="text-gray-500">No portfolio holdings yet.</p>
 </section>

 <section v-if="tab==='performance'" class="space-y-4">
  <div v-if="store.performance" class="grid grid-cols-2 gap-4 md:grid-cols-4">
   <Stat label="Unrealized gain" :value="money(store.performance.unrealizedGain)"/>
   <Stat label="Realized gain" :value="money(store.performance.realizedGain)"/>
   <Stat label="Dividend income" :value="money(store.performance.dividendIncome)"/>
   <Stat label="Charges & taxes" :value="money(store.performance.charges)"/>
  </div>
  <div v-if="store.performance?.valueHistory?.length" class="card">
   <h2 class="font-semibold">Portfolio value history</h2>
   <div class="mt-4 h-72"><Line :data="valueHistoryChart" :options="chartOptions"/></div>
  </div>
  <div v-if="store.performance" class="card">
   <p v-if="!store.performance.realizedGainComplete" class="mb-3 rounded bg-amber-50 p-3 text-sm text-amber-800">Some historical sell transactions predate cost-basis tracking. Realized gain excludes those rows rather than estimating them.</p>
   <h2 class="font-semibold">Contribution history</h2>
   <table class="mt-3 w-full text-sm"><thead><tr><th>Month</th><th>Contributions</th><th>Withdrawals</th><th>Income</th></tr></thead><tbody><tr v-for="row in store.performance.contributionTrend" :key="row.yearMonth"><td>{{row.yearMonth}}</td><td>{{money(row.contributions)}}</td><td>{{money(row.withdrawals)}}</td><td>{{money(row.income)}}</td></tr></tbody></table>
   <p v-if="!store.performance.contributionTrend.length" class="mt-3 text-sm text-gray-500">No investment transactions in this period.</p>
  </div>
 </section>

 <section v-if="tab==='allocation'" class="space-y-4">
  <form class="card" @submit.prevent="analyzeAllocation">
   <h2 class="font-semibold">Target Allocation</h2>
   <p class="mt-1 text-sm text-gray-500">Saved targets are used for analysis only. FinOS will not execute transactions.</p>
   <div class="mt-4 grid grid-cols-2 gap-3 md:grid-cols-4">
    <label v-for="(_,name) in allocationTargets" :key="name" class="text-sm">{{name}} %
     <input v-model.number="allocationTargets[name]" type="number" min="0" max="100" step=".1" class="input mt-1 w-full"/>
    </label>
   </div>
   <div class="mt-4 flex items-center gap-4"><button class="btn">Save & analyze</button><span class="text-sm text-gray-500">Total: {{Object.values(allocationTargets).reduce((a,x)=>a+Number(x||0),0)}}%</span></div>
  </form>
  <div v-if="store.allocationAnalysis" class="card overflow-x-auto">
   <table class="w-full text-sm"><thead><tr><th>Asset class</th><th>Actual</th><th>Target</th><th>Deviation</th><th>Status</th></tr></thead>
    <tbody><tr v-for="item in store.allocationAnalysis.allocations" :key="item.assetClass"><td>{{item.assetClass}}</td><td>{{item.actualPct}}%</td><td>{{item.targetPct}}%</td><td>{{item.deviationPct>0?'+':''}}{{item.deviationPct}}%</td><td :class="item.status==='Balanced'?'text-green-700':'text-amber-700'">{{item.status}}</td></tr></tbody>
   </table>
   <p class="mt-4 text-sm" :class="store.allocationAnalysis.rebalancingSuggested?'text-amber-700':'text-green-700'">{{store.allocationAnalysis.rebalancingSuggested?'Allocation review suggested; no trades will be executed.':'Allocation is close to the supplied targets.'}}</p>
  </div>
 </section>

 <section v-if="tab==='sips'" class="space-y-4">
  <div class="flex justify-between items-center"><div><h2 class="font-semibold">SIP Tracker</h2><p class="text-sm text-gray-500">Monthly commitment: {{money(store.totalSIPMonthly)}}</p></div><button class="btn" @click="openSIP()">Add SIP</button></div>
  <form v-if="showSIP" @submit.prevent="saveSIP" class="card grid md:grid-cols-3 gap-3">
   <input v-model="sip.fundName" required placeholder="Fund name" class="input"/>
   <input v-model.number="sip.monthlyAmount" required min="1" type="number" placeholder="Monthly amount" class="input"/>
   <select v-model="sip.frequency" class="input"><option :value="2">Monthly</option><option :value="0">Weekly</option><option :value="1">Bi-weekly</option><option :value="3">Quarterly</option></select>
   <input v-model.number="sip.dayOfMonth" min="1" max="31" type="number" class="input"/>
   <input v-model="sip.startDate" required type="date" class="input"/><input v-model="sip.endDate" type="date" class="input"/>
   <select v-model.number="sip.sourceAccountId" required class="input"><option :value="0" disabled>Source account</option><option v-for="a in accounts.accounts" :key="a.id" :value="a.id">{{a.name}} ({{money(a.balance)}})</option></select>
   <div class="md:col-span-2 flex gap-2"><button class="btn">{{sip.id?'Update':'Create'}} SIP</button><button type="button" class="btn-secondary" @click="showSIP=false">Cancel</button></div>
  </form>
  <div class="card overflow-x-auto"><table class="w-full text-sm"><thead><tr><th>Fund</th><th>Amount</th><th>Invested</th><th>Current</th><th>Next date</th><th>Status</th><th></th></tr></thead>
   <tbody><tr v-for="x in store.sipList" :key="x.id"><td>{{x.fundName}}</td><td>{{money(x.monthlyAmount)}}</td><td>{{money(x.totalInvested)}}</td><td>{{money(x.currentValue)}}</td><td>{{date(x.nextExecutionDate)}}</td><td>{{x.isActive?'Active':'Paused'}}</td><td class="space-x-2"><button @click="openSIP(x)">Edit</button><button @click="store.setSIPStatus(x.id,!x.isActive)">{{x.isActive?'Pause':'Resume'}}</button><button class="text-red-600" @click="removeSIP(x.id)">Delete</button></td></tr></tbody>
  </table><p v-if="!store.sipList.length" class="p-4 text-gray-500">No SIP schedules. Add your first SIP.</p></div>
 </section>

 <section v-if="tab==='epf'" class="space-y-4">
  <form v-if="!store.epfTracker" @submit.prevent="createEPF" class="card grid md:grid-cols-3 gap-3">
   <h2 class="md:col-span-3 font-semibold">Set up EPF tracker</h2><input v-model="epf.employerName" placeholder="Employer name" class="input"/><input v-model="epf.uan" placeholder="UAN (stored securely)" class="input"/>
   <input v-model.number="epf.monthlySalary" required min="1" type="number" placeholder="Monthly basic salary" class="input"/><input v-model.number="epf.currentBalance" min="0" type="number" placeholder="Opening balance" class="input"/>
   <input v-model.number="epf.employeeContributionPct" type="number" step=".01" class="input"/><input v-model.number="epf.employerContributionPct" type="number" step=".01" class="input"/>
   <input v-model.number="epf.interestRate" type="number" step=".01" class="input"/><input v-model="epf.startDate" required type="date" class="input"/><button class="btn">Create EPF tracker</button>
  </form>
  <template v-else>
   <div class="flex justify-between"><div><h2 class="font-semibold">{{store.epfTracker.employerName||'EPF Account'}}</h2><p class="text-sm text-gray-500">{{store.epfTracker.maskedUAN}}</p></div></div>
   <div class="grid grid-cols-2 md:grid-cols-4 gap-4"><Stat label="Employee" :value="money(store.epfTracker.employeeContribution)"/><Stat label="Employer" :value="money(store.epfTracker.employerContribution)"/><Stat label="Interest" :value="money(store.epfTracker.interestEarned)"/><Stat label="EPF Balance" :value="money(store.epfTracker.currentBalance)"/></div>
   <form @submit.prevent="addContribution" class="card flex flex-wrap gap-3 items-end"><label>Month<input v-model="contribution.month" required type="month" class="input block"/></label><label>Basic salary<input v-model.number="contribution.monthlySalary" required min="1" type="number" class="input block"/></label><button class="btn">Add contribution</button></form>
   <form @submit.prevent="project" class="card flex flex-wrap gap-3 items-end"><label>Current age<input v-model.number="projection.currentAge" type="number" class="input block"/></label><label>Retirement age<input v-model.number="projection.retirementAge" type="number" class="input block"/></label><button class="btn">Calculate projection</button><b v-if="store.epfProjection">Projected corpus: {{money(store.epfProjection.projectedCorpus)}}</b></form>
   <div class="card overflow-x-auto"><table class="w-full text-sm"><thead><tr><th>Month</th><th>Employee</th><th>Employer</th><th>EPS</th><th>Interest</th><th>Closing</th></tr></thead><tbody><tr v-for="x in store.epfTracker.contributions" :key="x.id"><td>{{date(x.month)}}</td><td>{{money(x.employeeContribution)}}</td><td>{{money(x.employerContribution)}}</td><td>{{money(x.epsContribution)}}</td><td>{{money(x.interestEarned)}}</td><td>{{money(x.closingBalance)}}</td></tr></tbody></table></div>
  </template>
 </section>
</div>
</template>
<script setup>
import{ref,reactive,onMounted,defineComponent,h,computed}from'vue'
import{Line}from'vue-chartjs'
import{Chart as ChartJS,CategoryScale,LinearScale,PointElement,LineElement,Tooltip,Legend}from'chart.js'
import{useInvestmentsStore}from'../stores/investments'
import{useAccountsStore}from'../stores/accounts'
const store=useInvestmentsStore(),accounts=useAccountsStore(),tab=ref('portfolio'),showSIP=ref(false)
ChartJS.register(CategoryScale,LinearScale,PointElement,LineElement,Tooltip,Legend)
const tabs=[{id:'portfolio',label:'Portfolio'},{id:'performance',label:'Performance'},{id:'allocation',label:'Target Allocation'},{id:'sips',label:'SIP Tracker'},{id:'epf',label:'EPF Tracker'}]
const today=()=>new Date().toISOString().slice(0,10)
const emptySIP=()=>({id:null,fundName:'',monthlyAmount:0,frequency:2,dayOfMonth:1,startDate:today(),endDate:null,sourceAccountId:0,holdingId:null})
const sip=reactive(emptySIP()),epf=reactive({employerName:'',uan:'',monthlySalary:0,currentBalance:0,employeeContributionPct:12,employerContributionPct:12,interestRate:8.25,startDate:today()})
const contribution=reactive({month:today().slice(0,7),monthlySalary:0}),projection=reactive({currentAge:30,retirementAge:60})
const allocationTargets=reactive({Equity:60,Debt:30,Gold:10,Hybrid:0})
const valueHistoryChart=computed(()=>({labels:(store.performance?.valueHistory||[]).map(x=>new Date(x.date).toLocaleDateString('en-IN')),datasets:[{label:'Current value',data:(store.performance?.valueHistory||[]).map(x=>x.currentValue),borderColor:'#4f46e5',tension:.3},{label:'Invested value',data:(store.performance?.valueHistory||[]).map(x=>x.investedValue),borderColor:'#10b981',tension:.3}]}))
const chartOptions={responsive:true,maintainAspectRatio:false,plugins:{legend:{position:'bottom'}}}
const Stat=defineComponent({props:['label','value'],setup:p=>()=>h('div',{class:'card'},[h('p',{class:'text-sm text-gray-500'},p.label),h('p',{class:'text-xl font-bold'},p.value)])})
const money=v=>new Intl.NumberFormat('en-IN',{style:'currency',currency:'INR',maximumFractionDigits:0}).format(Number(v||0))
const date=v=>v?new Date(v).toLocaleDateString('en-IN'):'-'
function openSIP(x){Object.assign(sip,emptySIP(),x||{});showSIP.value=true}
async function saveSIP(){const data={...sip,endDate:sip.endDate||null};sip.id?await store.updateSIP(sip.id,data):await store.createSIP(data);showSIP.value=false}
async function removeSIP(id){if(confirm('Delete this SIP?'))await store.deleteSIP(id)}
async function createEPF(){await store.createEPF({...epf});contribution.monthlySalary=epf.monthlySalary}
async function addContribution(){await store.addEPFContribution({month:contribution.month+'-01',monthlySalary:contribution.monthlySalary})}
async function project(){await store.fetchEPFProjection({...projection})}
async function analyzeAllocation(){await store.saveAllocationTargets(Object.entries(allocationTargets).map(([assetClass,targetPct])=>({assetClass,targetPct})))}
onMounted(async()=>{await Promise.allSettled([store.fetchInvestments(),store.fetchSIPs(),store.fetchEPF(),accounts.fetchAccounts()]);if(store.activePortfolioId){const [saved]=await Promise.all([store.fetchAllocationTargets(),store.fetchPerformance()]);for(const target of saved)allocationTargets[target.assetClass]=target.targetPct}if(store.epfTracker)contribution.monthlySalary=store.epfTracker.monthlySalary})
</script>
<style scoped>
.card{@apply bg-white rounded-xl border border-gray-200 p-5 shadow-sm}.input{@apply border border-gray-300 rounded-lg px-3 py-2 text-sm}.btn{@apply bg-primary-600 text-white rounded-lg px-4 py-2 text-sm}.btn-secondary{@apply border border-gray-300 rounded-lg px-4 py-2 text-sm}th,td{@apply px-4 py-3 text-left border-b border-gray-100}
</style>
