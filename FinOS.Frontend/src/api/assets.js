import api from'./axios';export const assetsApi={list:()=>api.get('/api/corefinance/assets'),add:x=>api.post('/api/corefinance/assets',x),remove:id=>api.delete(`/api/corefinance/assets/${id}`)}
