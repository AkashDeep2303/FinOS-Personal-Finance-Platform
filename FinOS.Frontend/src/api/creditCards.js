import api from './axios';export const creditCardsApi={list:()=>api.get('/api/corefinance/credit-cards'),save:(id,x)=>api.put(`/api/corefinance/credit-cards/${id}`,x)}
