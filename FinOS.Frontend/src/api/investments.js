import api from './axios'
export const investmentsApi={
 getAll:userId=>api.get(`/api/investment/portfolios/user/${userId}`),
 getSummary:id=>api.get(`/api/investment/portfolios/${id}/summary`),
 createPortfolio:data=>api.post('/api/investment/portfolios',data),
 create:data=>api.post('/api/investment/holdings',data),
 update:(id,data)=>api.put(`/api/investment/holdings/${id}`,data),
 delete:id=>api.delete(`/api/investment/holdings/${id}`),
 getSIPs:()=>api.get('/api/investment/sips/me'),
 createSIP:data=>api.post('/api/investment/sips',data),
 updateSIP:(id,data)=>api.put(`/api/investment/sips/${id}`,data),
 setSIPStatus:(id,isActive)=>api.patch(`/api/investment/sips/${id}/status`,{isActive}),
 deleteSIP:id=>api.delete(`/api/investment/sips/${id}`),
 getEPF:()=>api.get('/api/investment/epf/me'),
 createEPF:data=>api.post('/api/investment/epf',data),
 addEPFContribution:(id,data)=>api.post(`/api/investment/epf/${id}/contributions`,data),
 getEPFProjection:(id,params)=>api.get(`/api/investment/epf/${id}/projection`,{params})
}
