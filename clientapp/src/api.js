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
  trace: (code) => req(`/trace/${encodeURIComponent(code)}`),
  verify: (b) => req('/verify', { method: 'POST', body: b }),
  verifications: (q) => req(`/verifications${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  ctes: (q) => req(`/ctes${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  saveCte: (b) => req('/ctes', { method: 'POST', body: b }),
  deleteCte: (id) => req(`/ctes/${id}`, { method: 'DELETE' }),
  kdes: (q) => req(`/kdes${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  saveKde: (b) => req('/kdes', { method: 'POST', body: b }),
  deleteKde: (id) => req(`/kdes/${id}`, { method: 'DELETE' }),
  dataTypes: (q) => req(`/data-types${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  saveDataType: (b) => req('/data-types', { method: 'POST', body: b }),
  deleteDataType: (id) => req(`/data-types/${id}`, { method: 'DELETE' }),
  cteKdes: (cteCode) => req(`/cte-kdes${cteCode ? `?cteCode=${encodeURIComponent(cteCode)}` : ''}`),
  saveCteKdes: (b) => req('/cte-kdes', { method: 'POST', body: b }),
  glns: (q) => req(`/glns${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  saveGln: (b) => req('/glns', { method: 'POST', body: b }),
  deleteGln: (id) => req(`/glns/${id}`, { method: 'DELETE' }),
  farms: (q) => req(`/farms${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  saveFarm: (b) => req('/farms', { method: 'POST', body: b }),
  deleteFarm: (id) => req(`/farms/${id}`, { method: 'DELETE' }),
  orgGlns: (q) => req(`/org-glns${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  saveOrgGln: (b) => req('/org-glns', { method: 'POST', body: b }),
  deleteOrgGln: (id) => req(`/org-glns/${id}`, { method: 'DELETE' }),
  templates: (q) => req(`/templates${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  template: (id) => req(`/templates/${id}`),
  saveTemplate: (b) => req('/templates', { method: 'POST', body: b }),
  approveTemplate: (id) => req(`/templates/${id}/approve`, { method: 'POST' }),
  deleteTemplate: (id) => req(`/templates/${id}`, { method: 'DELETE' }),
  tplViewEvents: (q) => req(`/tpl-view-events${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  saveTplViewEvent: (b) => req('/tpl-view-events', { method: 'POST', body: b }),
  deleteTplViewEvent: (id) => req(`/tpl-view-events/${id}`, { method: 'DELETE' }),
  records: (q) => req(`/records${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  record: (id) => req(`/records/${id}`),
  saveRecord: (b) => req('/records', { method: 'POST', body: b }),
  deleteRecord: (id) => req(`/records/${id}`, { method: 'DELETE' }),
  stampBatches: (q) => req(`/stamp-batches${q ? `?q=${encodeURIComponent(q)}` : ''}`),
  stampBatch: (id) => req(`/stamp-batches/${id}`),
  generateStamps: (b) => req('/stamp-batches', { method: 'POST', body: b }),
  deleteStampBatch: (id) => req(`/stamp-batches/${id}`, { method: 'DELETE' }),
  stamps: (batchId, q) => req(`/stamps?${batchId ? `batchId=${batchId}&` : ''}${q ? `q=${encodeURIComponent(q)}` : ''}`)
}
export const fmtDate = (s) => s ? new Date(s).toLocaleDateString('vi-VN') : '—'
export const fmtDateTime = (s) => s ? new Date(s).toLocaleString('vi-VN') : '—'
export const STAGES = ['Sản xuất', 'Kiểm định', 'Đóng gói', 'Nhập kho', 'Vận chuyển', 'Đại lý nhận', 'Bày bán', 'Đã bán']
