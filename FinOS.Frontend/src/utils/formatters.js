const inr = new Intl.NumberFormat('en-IN', {
  style: 'currency',
  currency: 'INR',
  minimumFractionDigits: 0,
  maximumFractionDigits: 2
})

const compactInr = new Intl.NumberFormat('en-IN', {
  style: 'currency',
  currency: 'INR',
  notation: 'compact',
  compactDisplay: 'short',
  maximumFractionDigits: 1
})

export function formatMoney(value, options = {}) {
  const amount = Number(value)
  if (!Number.isFinite(amount)) return options.fallback ?? '—'
  return (options.compact ? compactInr : inr).format(amount)
}

export function formatPercentage(value, options = {}) {
  const amount = Number(value)
  if (!Number.isFinite(amount)) return options.fallback ?? '—'
  const normalized = options.fraction ? amount : amount / 100
  return new Intl.NumberFormat('en-IN', {
    style: 'percent',
    minimumFractionDigits: options.minimumFractionDigits ?? 0,
    maximumFractionDigits: options.maximumFractionDigits ?? 1
  }).format(normalized)
}

export function formatIndianDate(value, options = {}) {
  if (!value) return options.fallback ?? '—'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return options.fallback ?? '—'
  return new Intl.DateTimeFormat('en-IN', {
    day: '2-digit',
    month: options.long ? 'long' : 'short',
    year: 'numeric'
  }).format(date)
}
