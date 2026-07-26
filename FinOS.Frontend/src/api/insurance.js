import api from './axios'
export const insuranceApi={list:()=>api.get('/api/corefinance/insurance'),add:x=>api.post('/api/corefinance/insurance',x),remove:id=>api.delete(`/api/corefinance/insurance/${id}`)}
