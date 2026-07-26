import api from'./axios';export const reportsApi={fyReview:startYear=>api.get('/api/analytics/reports/financial-year-review',{params:{startYear}})}
