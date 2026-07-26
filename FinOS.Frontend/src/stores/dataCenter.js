import { defineStore } from 'pinia'
import { dataCenterApi } from '../api/dataCenter'

export const useDataCenterStore = defineStore('dataCenter', {
  state: () => ({
    overview: null,
    loading: false,
    documents: [],
    reconciliationIssues: [],
    sources: [],
    csvPreview: null,
    csvMapping: {},
    csvMappingValidation: null,
    csvTransactionValidation: null,
    csvDuplicateAnalysis: null,
    csvImportResult: null,
    error: null
  }),
  actions: {
    async load() {
      this.loading = true
      this.error = null
      try {
        const [overviewResponse, documentResponse, issueResponse, sourceResponse] = await Promise.all([
          dataCenterApi.overview(),
          dataCenterApi.documents(),
          dataCenterApi.reconciliationIssues(),
          dataCenterApi.sources()
        ])
        this.overview = overviewResponse.data?.data ?? null
        this.documents = documentResponse.data?.data ?? []
        this.reconciliationIssues = issueResponse.data?.data ?? []
        this.sources = sourceResponse.data?.data ?? []
      } catch (error) {
        this.error = error.response?.data?.message || 'Failed to load your data overview.'
      } finally {
        this.loading = false
      }
    },
    async addDocument(document, file = null) {
      const response = await dataCenterApi.addDocument(document)
      let created = response.data?.data
      if (file) {
        const uploadResponse = await dataCenterApi.uploadDocumentFile(created.id, file)
        created = uploadResponse.data?.data
      }
      this.documents.unshift(created)
    },
    async downloadDocument(id) {
      return dataCenterApi.downloadDocumentFile(id)
    },
    async deleteDocument(id) {
      await dataCenterApi.deleteDocument(id)
      this.documents = this.documents.filter(document => document.id !== id)
    },
    async resolveIssue(id, transactionId = null) {
      await dataCenterApi.resolveIssue(id, transactionId)
      this.reconciliationIssues = this.reconciliationIssues.filter(issue => issue.id !== id)
      if (this.overview) {
        this.overview.unresolvedImportErrorCount = Math.max(0, this.overview.unresolvedImportErrorCount - 1)
      }
    },
    async addSource(source) {
      const response = await dataCenterApi.addSource(source)
      this.sources.push(response.data?.data)
    },
    async deleteSource(id) {
      await dataCenterApi.deleteSource(id)
      this.sources = this.sources.filter(source => source.id !== id)
    },
    async previewCsv(file) {
      const response = await dataCenterApi.previewCsv(file)
      this.csvPreview = response.data?.data ?? null
      this.csvMapping = { ...(this.csvPreview?.suggestedMappings ?? {}) }
      this.csvMappingValidation = null
      this.csvTransactionValidation = null
      this.csvDuplicateAnalysis = null
    },
    async validateCsvMapping() {
      const response = await dataCenterApi.validateCsvMapping(this.csvPreview?.headers ?? [], this.csvMapping)
      this.csvMappingValidation = response.data?.data ?? null
    },
    async validateCsvTransactions(file, positiveAmountType) {
      const response = await dataCenterApi.validateCsvTransactions(file, this.csvMapping, positiveAmountType)
      this.csvTransactionValidation = response.data?.data ?? null
      this.csvDuplicateAnalysis = null
    },
    async checkCsvDuplicates(file, positiveAmountType, accountId) {
      const response = await dataCenterApi.checkCsvDuplicates(file, this.csvMapping, positiveAmountType, accountId)
      this.csvDuplicateAnalysis = response.data?.data ?? null
    },
    async confirmCsvImport(file, positiveAmountType, accountId, duplicatePolicy) {
      const response = await dataCenterApi.confirmCsvImport(file, this.csvMapping, positiveAmountType, accountId, duplicatePolicy)
      this.csvImportResult = response.data?.data ?? null
      await this.load()
    }
  }
})
