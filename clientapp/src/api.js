const base = '/api/v1'
async function req(path, opts = {}) {
  const res = await fetch(base + path, {
    headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin',
    ...opts, body: opts.body ? JSON.stringify(opts.body) : undefined
  })
  const text = await res.text(); const data = text ? JSON.parse(text) : null
  if (!res.ok) throw new Error(data?.error || `Lỗi ${res.status}`)
  return { data, cache: res.headers.get('X-Cache') }
}
export const api = {
  dashboard: () => req('/dashboard'),
  products: () => req('/products'),
  createProduct: (b) => req('/products', { method: 'POST', body: b }),
  importPim: () => req('/products/import-pim', { method: 'POST' }),
  units: (q) => req(`/units${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  unit: (id) => req(`/units/${id}`),
  createUnit: (b) => req('/units', { method: 'POST', body: b }),
  addEvent: (id, b) => req(`/units/${id}/events`, { method: 'POST', body: b }),
  trace: (code) => req(`/trace/${encodeURIComponent(code)}`)
}
export const fmtDate = (s) => s ? new Date(s).toLocaleDateString('vi-VN') : '—'
export const fmtDateTime = (s) => s ? new Date(s).toLocaleString('vi-VN') : '—'
export const STAGES = ['Sản xuất', 'Kiểm định', 'Đóng gói', 'Nhập kho', 'Vận chuyển', 'Đại lý nhận', 'Bày bán', 'Đã bán']
