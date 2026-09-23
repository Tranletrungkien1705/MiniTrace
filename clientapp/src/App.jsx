import React, { useEffect, useState } from 'react'
import { Routes, Route, NavLink, Outlet } from 'react-router-dom'
import { api, fmtDate, fmtDateTime, STAGES } from './api'

function Badge({ text, css }) { return <span className={`badge ${css || 'secondary'}`}>{text}</span> }
function Flash({ msg }) { return msg ? <div className={`flash ${msg.ok ? 'ok' : 'err'}`}>{msg.text}</div> : null }
function Modal({ title, onClose, wide, children }) {
  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="modal" style={wide ? { maxWidth: 720 } : undefined} onClick={e => e.stopPropagation()}>
        <div className="row" style={{ marginBottom: 12 }}><h2 style={{ flex: 1, margin: 0 }}>{title}</h2>
          <button className="btn gray sm" style={{ flex: 'none' }} onClick={onClose}>Đóng</button></div>{children}
      </div>
    </div>
  )
}
function Field({ label, children }) { return <div style={{ flex: 1 }}><label>{label}</label>{children}</div> }

function Layout() {
  return (
    <>
      <nav className="nav"><span className="brand">🔗 MiniTrace</span>
        <NavLink to="/" end>Tổng quan</NavLink><NavLink to="/units">Đơn vị truy xuất</NavLink>
        <NavLink to="/products">Sản phẩm</NavLink><NavLink to="/trace">Tra cứu</NavLink>
        <NavLink to="/verify">Chống hàng giả</NavLink><NavLink to="/ctes">Sự kiện (CTE)</NavLink></nav>
      <div className="wrap"><Outlet /></div>
    </>
  )
}

function Dashboard() {
  const [d, setD] = useState(null); const [cache, setCache] = useState('')
  useEffect(() => { api.dashboard().then(r => { setD(r.data); setCache(r.cache) }) }, [])
  if (!d) return <p className="muted">Đang tải…</p>
  const max = Math.max(1, ...d.byStage.map(s => s.count))
  return (
    <>
      <h1>Tổng quan truy xuất {cache && <span className="pill">cache: {cache}</span>}</h1>
      <div className="grid kpis" style={{ marginBottom: 18 }}>
        <div className="kpi"><div className="v">{d.products}</div><div className="l">Sản phẩm</div></div>
        <div className="kpi"><div className="v">{d.units}</div><div className="l">Đơn vị truy xuất</div></div>
        <div className="kpi"><div className="v">{d.events}</div><div className="l">Sự kiện</div></div>
        <div className="kpi"><div className="v" style={{ color: 'var(--success)' }}>{d.completed}</div><div className="l">Đã đến tay NTD</div></div>
      </div>
      <div className="card funnel"><h2>Đơn vị theo giai đoạn hiện tại</h2>
        {d.byStage.map((s, i) => (<div className="bar" key={i}><div className="lbl">{s.stageText}</div>
          <div className="track"><div className="fill" style={{ width: `${(s.count / max) * 100}%` }} /></div><div className="n">{s.count}</div></div>))}
      </div>
    </>
  )
}

function Units() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [open, setOpen] = useState(null); const [show, setShow] = useState(false)
  const load = () => api.units(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Đơn vị truy xuất</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setShow(true)}>+ Tạo đơn vị</button></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã</th><th>Sản phẩm</th><th>Lô</th><th className="right">Sự kiện</th><th>Giai đoạn</th><th>Ngày tạo</th></tr></thead>
          <tbody>{rows.map(u => (
            <tr key={u.id} style={{ cursor: 'pointer' }} onClick={() => setOpen(u.id)}>
              <td style={{ fontFamily: 'monospace' }}>{u.code}</td><td>{u.product}</td><td>{u.lotNo || '—'}</td>
              <td className="right">{u.events}</td><td>{u.lastStage ? <Badge text={u.lastStage} css={u.lastStage === 'Đã bán' ? 'success' : 'info'} /> : '—'}</td><td>{fmtDate(u.createdAt)}</td></tr>))}
            {rows.length === 0 && <tr><td colSpan={6} className="muted" style={{ padding: 20 }}>Chưa có đơn vị.</td></tr>}</tbody></table>
      </div>
      {open && <UnitDetail id={open} onClose={() => setOpen(null)} onChanged={load} />}
      {show && <UnitForm onClose={() => setShow(false)} onSaved={() => { setShow(false); load() }} />}
    </>
  )
}

function UnitDetail({ id, onClose, onChanged }) {
  const [u, setU] = useState(null); const [msg, setMsg] = useState(null); const [ev, setEv] = useState({ location: '', actor: '' })
  const load = () => api.unit(id).then(r => setU(r.data))
  useEffect(() => { load() }, [id])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  if (!u) return <Modal title="…" onClose={onClose}><p className="muted">Đang tải…</p></Modal>
  const nextStage = (u.lastStage ?? -1) + 1
  const addEvent = async () => {
    try { const r = await api.addEvent(id, { type: nextStage, location: ev.location, actor: ev.actor }); flash(true, r.data.msg); setEv({ location: '', actor: '' }); load(); onChanged() }
    catch (e) { flash(false, e.message) }
  }
  return (
    <Modal title={`Đơn vị ${u.code}`} onClose={onClose} wide>
      <Flash msg={msg} />
      <dl className="dl"><dt>Sản phẩm</dt><dd>{u.product}</dd><dt>Lô</dt><dd>{u.lotNo || '—'}</dd></dl>
      <div className="section-t">Hành trình truy xuất</div>
      <div style={{ borderLeft: '2px solid var(--line)', paddingLeft: 14, marginLeft: 6 }}>
        {u.events.map((e, i) => (
          <div key={i} style={{ marginBottom: 12, position: 'relative' }}>
            <div style={{ position: 'absolute', left: -21, top: 3, width: 12, height: 12, borderRadius: 6, background: 'var(--brand)' }} />
            <b>{e.stageText}</b> <span className="muted" style={{ fontSize: 12 }}>{fmtDateTime(e.occurredAt)}</span><br />
            <span className="muted">{e.actor} · {e.location}</span>{e.note ? <span className="muted"> — {e.note}</span> : ''}
          </div>))}
      </div>
      {nextStage < 8 ? (
        <div className="card" style={{ background: '#f8fafc', marginTop: 10 }}>
          <div className="section-t">Ghi sự kiện tiếp theo: {STAGES[nextStage]}</div>
          <div className="row"><Field label="Đơn vị thực hiện"><input value={ev.actor} onChange={e => setEv({ ...ev, actor: e.target.value })} /></Field>
            <Field label="Địa điểm"><input value={ev.location} onChange={e => setEv({ ...ev, location: e.target.value })} /></Field>
            <div style={{ flex: 'none', alignSelf: 'flex-end' }}><button className="btn sm" onClick={addEvent}>Ghi</button></div></div>
        </div>
      ) : <div className="flash ok" style={{ marginTop: 10 }}>Đã hoàn tất chuỗi truy xuất (đã bán).</div>}
    </Modal>
  )
}

function UnitForm({ onClose, onSaved }) {
  const [prods, setProds] = useState([]); const [f, setF] = useState({ productId: '', lotNo: '' }); const [err, setErr] = useState('')
  useEffect(() => { api.products().then(r => { setProds(r.data); if (r.data[0]) setF(s => ({ ...s, productId: r.data[0].id })) }) }, [])
  const save = async () => { try { if (!f.productId) { setErr('Chọn SP'); return } await api.createUnit({ productId: Number(f.productId), lotNo: f.lotNo }); onSaved() } catch (e) { setErr(e.message) } }
  return (
    <Modal title="Tạo đơn vị truy xuất" onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <Field label="Sản phẩm"><select value={f.productId} onChange={e => setF({ ...f, productId: e.target.value })}>{prods.map(p => <option key={p.id} value={p.id}>{p.code} · {p.name}</option>)}</select></Field>
      <Field label="Số lô"><input value={f.lotNo} onChange={e => setF({ ...f, lotNo: e.target.value })} /></Field>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Tạo (sự kiện Sản xuất)</button></div>
    </Modal>
  )
}

function Products() {
  const [rows, setRows] = useState([]); const [show, setShow] = useState(false); const [busy, setBusy] = useState(false); const [msg, setMsg] = useState(null)
  const load = () => api.products().then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const syncPim = async () => { setBusy(true); setMsg(null); try { const r = await api.importPim(); setMsg(r.data.msg); load() } catch (e) { setMsg('❌ ' + e.message) } finally { setBusy(false) } }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 1 }}>Sản phẩm</h1>
        <button className="btn gray sm" style={{ flex: 'none' }} disabled={busy} onClick={syncPim}>{busy ? 'Đang đồng bộ…' : '⭳ Đồng bộ từ PIM'}</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setShow(true)}>+ Thêm</button></div>
      {msg && <div className="card" style={{ padding: '10px 14px', marginBottom: 10, fontSize: 13 }}>{msg} <span className="muted">— danh mục chuẩn từ MiniPIM</span></div>}
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>GTIN</th><th>Tên</th><th>Xuất xứ</th><th>Nhà sản xuất</th></tr></thead>
          <tbody>{rows.map(p => <tr key={p.id}><td>{p.code}</td><td>{p.name}</td><td>{p.origin || '—'}</td><td>{p.manufacturer || '—'}</td></tr>)}</tbody></table>
      </div>
      {show && <ProductForm onClose={() => setShow(false)} onSaved={() => { setShow(false); load() }} />}
    </>
  )
}

function ProductForm({ onClose, onSaved }) {
  const [f, setF] = useState({ name: '', code: '', origin: '', manufacturer: '' }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => { try { await api.createProduct(f); onSaved() } catch (e) { setErr(e.message) } }
  return (
    <Modal title="Thêm sản phẩm" onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Tên *"><input value={f.name} onChange={e => up('name', e.target.value)} /></Field>
        <Field label="GTIN"><input value={f.code} onChange={e => up('code', e.target.value)} /></Field></div>
      <div className="row"><Field label="Xuất xứ"><input value={f.origin} onChange={e => up('origin', e.target.value)} /></Field>
        <Field label="Nhà sản xuất"><input value={f.manufacturer} onChange={e => up('manufacturer', e.target.value)} /></Field></div>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

function Trace() {
  const [code, setCode] = useState(''); const [res, setRes] = useState(null); const [err, setErr] = useState(null)
  const doTrace = async () => { try { const r = await api.trace(code.trim()); setRes(r.data); setErr(null) } catch (e) { setErr(e.message); setRes(null) } }
  return (
    <>
      <h1>Tra cứu nguồn gốc</h1>
      <div className="card"><div className="row">
        <Field label="Mã truy xuất trên sản phẩm"><input value={code} onChange={e => setCode(e.target.value)} onKeyDown={e => e.key === 'Enter' && doTrace()} /></Field>
        <div style={{ flex: 'none', alignSelf: 'flex-end' }}><button className="btn" onClick={doTrace}>Tra cứu</button></div>
      </div></div>
      {err && <Flash msg={{ ok: false, text: err }} />}
      {res && (
        <div className="card" style={{ borderLeft: '5px solid var(--success)' }}>
          <h2>{res.product}</h2>
          <dl className="dl"><dt>GTIN</dt><dd>{res.gtin}</dd><dt>Xuất xứ</dt><dd>{res.origin || '—'}</dd>
            <dt>Nhà sản xuất</dt><dd>{res.manufacturer || '—'}</dd><dt>Lô</dt><dd>{res.lotNo}</dd></dl>
          <div className="section-t">Hành trình ({res.journey.length} chặng)</div>
          <div style={{ borderLeft: '2px solid var(--line)', paddingLeft: 14, marginLeft: 6 }}>
            {res.journey.map((e, i) => (
              <div key={i} style={{ marginBottom: 12, position: 'relative' }}>
                <div style={{ position: 'absolute', left: -21, top: 3, width: 12, height: 12, borderRadius: 6, background: 'var(--success)' }} />
                <b>{e.stage}</b> <span className="muted" style={{ fontSize: 12 }}>{fmtDateTime(e.occurredAt)}</span><br />
                <span className="muted">{e.actor} · {e.location}</span></div>))}
          </div>
        </div>
      )}
    </>
  )
}

const VERIFY_CSS = { 0: 'success', 1: 'warning', 2: 'danger' }

function Verify() {
  const [code, setCode] = useState(''); const [res, setRes] = useState(null); const [err, setErr] = useState(null)
  const [rows, setRows] = useState([]); const [q, setQ] = useState('')
  const load = () => api.verifications(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const doVerify = async () => {
    try { const r = await api.verify({ code: code.trim() }); setRes(r.data); setErr(null); load() }
    catch (e) { setErr(e.message); setRes(null) }
  }
  return (
    <>
      <h1>Chống hàng giả</h1>
      <div className="card"><div className="row">
        <Field label="Mã truy xuất trên sản phẩm"><input value={code} onChange={e => setCode(e.target.value)} onKeyDown={e => e.key === 'Enter' && doVerify()} /></Field>
        <div style={{ flex: 'none', alignSelf: 'flex-end' }}><button className="btn" onClick={doVerify}>Xác thực</button></div>
      </div></div>
      {err && <Flash msg={{ ok: false, text: err }} />}
      {res && (
        <div className="card" style={{ borderLeft: `5px solid var(--${VERIFY_CSS[res.status] || 'line'})` }}>
          <h2>{res.product} <Badge text={res.statusText} css={VERIFY_CSS[res.status]} /></h2>
          <p style={{ marginTop: 4 }}>{res.message}</p>
          <dl className="dl"><dt>Mã</dt><dd style={{ fontFamily: 'monospace' }}>{res.code}</dd>
            <dt>Xuất xứ</dt><dd>{res.origin || '—'}</dd><dt>Nhà sản xuất</dt><dd>{res.manufacturer || '—'}</dd>
            <dt>Lô</dt><dd>{res.lotNo}</dd><dt>Số lần quét</dt><dd>{res.verifyCount}</dd></dl>
        </div>
      )}
      <div className="toolbar" style={{ marginTop: 18 }}><h2 style={{ margin: 0, flex: 'none' }}>Lịch sử quét</h2><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã</th><th>Sản phẩm</th><th className="right">Lần quét</th><th>Trạng thái</th><th>Vị trí</th><th>IP</th><th>Thời điểm</th></tr></thead>
          <tbody>{rows.map(v => (
            <tr key={v.id}><td style={{ fontFamily: 'monospace' }}>{v.code}</td><td>{v.product}</td>
              <td className="right">{v.verifyCount}</td><td><Badge text={v.statusText} css={v.css} /></td>
              <td>{v.location || '—'}</td><td className="muted">{v.ipAddress || '—'}</td><td>{fmtDateTime(v.scannedAt)}</td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Chưa có lần quét nào.</td></tr>}</tbody></table>
      </div>
    </>
  )
}

const CTE_NET = ['Manufacturer', 'Warehouse', 'Distributor', 'Dealer', 'Consumer']

function Ctes() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.ctes(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (c) => {
    if (!window.confirm(`Xóa sự kiện ${c.code}?`)) return
    try { const r = await api.deleteCte(c.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Sự kiện truy xuất (CTE)</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / diễn giải…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, code: '', description: '', networkType: '', apiLink: '', active: true })}>+ Thêm sự kiện</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Danh mục sự kiện trọng yếu (GS1 Critical Tracking Event) — "từ điển" các loại sự kiện dùng để ghi hành trình truy xuất.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã (CTECode)</th><th>Diễn giải</th><th>Loại mạng</th><th>API đích</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>{rows.map(c => (
            <tr key={c.id}><td style={{ fontFamily: 'monospace' }}>{c.code}</td><td>{c.description}</td>
              <td>{c.networkType || '—'}</td><td className="muted">{c.apiLink || '—'}</td>
              <td><Badge text={c.active ? 'Đang dùng' : 'Ngưng'} css={c.active ? 'success' : 'secondary'} /></td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(c)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(c)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={6} className="muted" style={{ padding: 20 }}>Chưa có sự kiện.</td></tr>}</tbody></table>
      </div>
      {edit && <CteForm cte={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function CteForm({ cte, onClose, onSaved }) {
  const [f, setF] = useState({ ...cte }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveCte({ id: f.id, code: f.code, description: f.description, networkType: f.networkType, apiLink: f.apiLink, active: f.active }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa sự kiện ${f.code}` : 'Thêm sự kiện truy xuất'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã sự kiện (CTECode) *"><input value={f.code} onChange={e => up('code', e.target.value)} placeholder="vd: PRODUCTION_IN" /></Field>
        <Field label="Loại mạng"><select value={f.networkType || ''} onChange={e => up('networkType', e.target.value)}>
          <option value="">—</option>{CTE_NET.map(n => <option key={n} value={n}>{n}</option>)}</select></Field></div>
      <Field label="Diễn giải *"><input value={f.description} onChange={e => up('description', e.target.value)} /></Field>
      <Field label="API đích (APIsLink)"><input value={f.apiLink || ''} onChange={e => up('apiLink', e.target.value)} placeholder="https://…" /></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.active} onChange={e => up('active', e.target.checked)} /> Đang sử dụng</label>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="units" element={<Units />} />
        <Route path="products" element={<Products />} />
        <Route path="trace" element={<Trace />} />
        <Route path="verify" element={<Verify />} />
        <Route path="ctes" element={<Ctes />} />
      </Route>
    </Routes>
  )
}
