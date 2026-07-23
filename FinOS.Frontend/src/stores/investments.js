import{defineStore}from'pinia'
import{investmentsApi}from'../api/investments'
import{useAuthStore}from'./auth'
export const useInvestmentsStore=defineStore('investments',{
 state:()=>({investments:[],sipList:[],epfTracker:null,epfProjection:null,activePortfolioId:null,loading:false,error:null}),
 getters:{
  totalInvested:s=>s.investments.reduce((a,x)=>a+Number(x.investedAmount||0),0),
  currentValue:s=>s.investments.reduce((a,x)=>a+Number(x.currentValue||0),0),
  totalReturns(){return this.currentValue-this.totalInvested},
  returnsPercentage(){return this.totalInvested?((this.totalReturns/this.totalInvested)*100).toFixed(2):0},
  totalSIPMonthly:s=>s.sipList.filter(x=>x.isActive).reduce((a,x)=>a+Number(x.monthlyAmount||0),0)
 },
 actions:{
  async run(fn,message){this.loading=true;this.error=null;try{return await fn()}catch(e){this.error=e.response?.data?.message||e.message||message;throw e}finally{this.loading=false}},
  async fetchInvestments(){return this.run(async()=>{const userId=useAuthStore().user?.id??useAuthStore().user?.userId;if(!userId)return;let r=await investmentsApi.getAll(userId);let p=Array.isArray(r.data?.data)?r.data.data:[];if(!p.length){r=await investmentsApi.createPortfolio({userId,name:'My Portfolio',currency:'INR',isDefault:true});p=[r.data?.data]}this.activePortfolioId=(p.find(x=>x?.isDefault)||p[0])?.id;if(this.activePortfolioId){r=await investmentsApi.getSummary(this.activePortfolioId);this.investments=r.data?.data?.topHoldings||[]}},'Failed to load investments')},
  async fetchSIPs(){return this.run(async()=>{const r=await investmentsApi.getSIPs();this.sipList=r.data?.data||[]},'Failed to load SIPs')},
  async createSIP(data){return this.run(async()=>{await investmentsApi.createSIP(data);await this.fetchSIPs()},'Failed to create SIP')},
  async updateSIP(id,data){return this.run(async()=>{await investmentsApi.updateSIP(id,data);await this.fetchSIPs()},'Failed to update SIP')},
  async setSIPStatus(id,value){return this.run(async()=>{await investmentsApi.setSIPStatus(id,value);await this.fetchSIPs()},'Failed to update SIP')},
  async deleteSIP(id){return this.run(async()=>{await investmentsApi.deleteSIP(id);await this.fetchSIPs()},'Failed to delete SIP')},
  async fetchEPF(){return this.run(async()=>{const r=await investmentsApi.getEPF();this.epfTracker=r.data?.data||null},'Failed to load EPF')},
  async createEPF(data){return this.run(async()=>{await investmentsApi.createEPF(data);await this.fetchEPF()},'Failed to create EPF')},
  async addEPFContribution(data){return this.run(async()=>{await investmentsApi.addEPFContribution(this.epfTracker.id,data);await this.fetchEPF()},'Failed to add contribution')},
  async fetchEPFProjection(params){return this.run(async()=>{const r=await investmentsApi.getEPFProjection(this.epfTracker.id,params);this.epfProjection=r.data?.data},'Failed to calculate projection')}
 }
})
