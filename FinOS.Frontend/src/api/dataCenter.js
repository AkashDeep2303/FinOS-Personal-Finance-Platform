import api from './axios'

export const dataCenterApi = {
  overview: (params = {}) => api.get('/api/corefinance/data-center/overview', { params }),
  documents: () => api.get('/api/corefinance/data-center/documents'),
  addDocument: document => api.post('/api/corefinance/data-center/documents', document),
  uploadDocumentFile: (id, file) => {
    const form = new FormData()
    form.append('file', file)
    return api.post(`/api/corefinance/data-center/documents/${id}/file`, form)
  },
  downloadDocumentFile: id => api.get(`/api/corefinance/data-center/documents/${id}/file`, { responseType: 'blob' }),
  deleteDocument: id => api.delete(`/api/corefinance/data-center/documents/${id}`),
  reconciliationIssues: (includeResolved = false) => api.get('/api/corefinance/data-center/reconciliation-issues', { params: { includeResolved } }),
  resolveIssue: (id, transactionId = null) => api.post(`/api/corefinance/data-center/reconciliation-issues/${id}/resolve`, { transactionId }),
  sources: () => api.get('/api/corefinance/data-center/sources'),
  addSource: source => api.post('/api/corefinance/data-center/sources', source),
  deleteSource: id => api.delete(`/api/corefinance/data-center/sources/${id}`),
  previewCsv: file => {
    const form = new FormData()
    form.append('file', file)
    return api.post('/api/corefinance/data-center/imports/csv/preview', form)
  },
  validateCsvMapping: (headers, mappings) => api.post('/api/corefinance/data-center/imports/csv/mapping/validate', { headers, mappings }),
  validateCsvTransactions: (file, mappings, positiveAmountType) => {
    const form = new FormData()
    form.append('file', file)
    form.append('mappings', JSON.stringify(mappings))
    form.append('positiveAmountType', positiveAmountType)
    return api.post('/api/corefinance/data-center/imports/csv/transactions/validate', form)
  },
  checkCsvDuplicates: (file, mappings, positiveAmountType, accountId) => {
    const form = new FormData()
    form.append('file', file)
    form.append('mappings', JSON.stringify(mappings))
    form.append('positiveAmountType', positiveAmountType)
    form.append('accountId', accountId)
    return api.post('/api/corefinance/data-center/imports/csv/duplicates/check', form)
  },
  confirmCsvImport: (file, mappings, positiveAmountType, accountId, duplicatePolicy) => {
    const form = new FormData()
    form.append('file', file); form.append('mappings', JSON.stringify(mappings))
    form.append('positiveAmountType', positiveAmountType); form.append('accountId', accountId)
    form.append('duplicatePolicy', duplicatePolicy)
    return api.post('/api/corefinance/data-center/imports/csv/confirm', form)
  }
}
