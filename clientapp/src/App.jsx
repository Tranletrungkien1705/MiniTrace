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
        <NavLink to="/verify">Chống hàng giả</NavLink><NavLink to="/ctes">Sự kiện (CTE)</NavLink>
        <NavLink to="/kdes">Thành phần (KDE)</NavLink><NavLink to="/glns">Địa điểm (GLN)</NavLink>
        <NavLink to="/templates">Mẫu loại tổ chức</NavLink></nav>
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

const KDE_TYPES = ['Text', 'Number', 'Date', 'List']

function Kdes() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const [mapFor, setMapFor] = useState(null)
  const load = () => api.kdes(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (k) => {
    if (!window.confirm(`Xóa thành phần ${k.code}?`)) return
    try { const r = await api.deleteKde(k.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Thành phần dữ liệu (KDE)</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / mô tả…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, code: '', description: '', dataType: 'Text', refNoList: '', networkType: '', flagList: false, flagQuery: false, active: true })}>+ Thêm thành phần</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Danh mục thành phần dữ liệu trọng yếu (GS1 Key Data Element) — "từ điển" các trường dữ liệu phải thu thập tại mỗi sự kiện truy xuất.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã (KDECode)</th><th>Mô tả</th><th>Kiểu</th><th>Loại mạng</th><th>Cờ</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>{rows.map(k => (
            <tr key={k.id}><td style={{ fontFamily: 'monospace' }}>{k.code}</td><td>{k.description}</td>
              <td>{k.dataType || '—'}</td><td>{k.networkType || '—'}</td>
              <td>{k.flagList ? <Badge text="Danh sách" css="info" /> : null}{k.flagQuery ? <Badge text="Truy vấn" css="secondary" /> : null}</td>
              <td><Badge text={k.active ? 'Đang dùng' : 'Ngưng'} css={k.active ? 'success' : 'secondary'} /></td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setMapFor(k)}>Ánh xạ</button>{' '}
                <button className="btn ghost sm" onClick={() => setEdit(k)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(k)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Chưa có thành phần.</td></tr>}</tbody></table>
      </div>
      {edit && <KdeForm kde={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
      {mapFor && <CteKdeMap kde={mapFor} onClose={() => setMapFor(null)} />}
    </>
  )
}

function KdeForm({ kde, onClose, onSaved }) {
  const [f, setF] = useState({ ...kde }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveKde({ id: f.id, code: f.code, description: f.description, dataType: f.dataType, refNoList: f.refNoList, networkType: f.networkType, flagList: f.flagList, flagQuery: f.flagQuery, active: f.active }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa thành phần ${f.code}` : 'Thêm thành phần dữ liệu'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã thành phần (KDECode) *"><input value={f.code} onChange={e => up('code', e.target.value)} placeholder="vd: LOT_NO" /></Field>
        <Field label="Kiểu dữ liệu"><select value={f.dataType || ''} onChange={e => up('dataType', e.target.value)}>
          <option value="">—</option>{KDE_TYPES.map(t => <option key={t} value={t}>{t}</option>)}</select></Field></div>
      <Field label="Mô tả *"><input value={f.description} onChange={e => up('description', e.target.value)} /></Field>
      <div className="row"><Field label="Loại mạng"><select value={f.networkType || ''} onChange={e => up('networkType', e.target.value)}>
          <option value="">—</option>{CTE_NET.map(n => <option key={n} value={n}>{n}</option>)}</select></Field>
        <Field label="Danh sách giá trị (RefNoList)"><input value={f.refNoList || ''} onChange={e => up('refNoList', e.target.value)} placeholder="Đạt;Không đạt" /></Field></div>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.flagList} onChange={e => up('flagList', e.target.checked)} /> Loại danh sách (chọn 1 giá trị)</label>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 6 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.flagQuery} onChange={e => up('flagQuery', e.target.checked)} /> Dùng để truy vấn</label>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 6 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.active} onChange={e => up('active', e.target.checked)} /> Đang sử dụng</label>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

function CteKdeMap({ kde, onClose }) {
  const [ctes, setCtes] = useState([]); const [sel, setSel] = useState(''); const [items, setItems] = useState([]); const [msg, setMsg] = useState(null)
  useEffect(() => { api.ctes().then(r => { setCtes(r.data); if (r.data[0]) setSel(r.data[0].code) }) }, [])
  const loadMap = (code) => api.cteKdes(code).then(r => setItems(r.data.map(m => ({ kdeCode: m.kdeCode, flagKey: m.flagKey, flagOsOrgView: m.flagOsOrgView }))))
  useEffect(() => { if (sel) loadMap(sel) }, [sel])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const has = items.some(i => i.kdeCode === kde.code)
  const toggle = () => setItems(has ? items.filter(i => i.kdeCode !== kde.code) : [...items, { kdeCode: kde.code, flagKey: false, flagOsOrgView: false }])
  const setFlag = (key, val) => setItems(items.map(i => i.kdeCode === kde.code ? { ...i, [key]: val } : i))
  const save = async () => {
    try { const r = await api.saveCteKdes({ cteCode: sel, items }); flash(true, r.data.msg) }
    catch (e) { flash(false, e.message) }
  }
  const cur = items.find(i => i.kdeCode === kde.code)
  return (
    <Modal title={`Ánh xạ ${kde.code} vào sự kiện`} onClose={onClose}>
      <Flash msg={msg} />
      <Field label="Sự kiện (CTE)"><select value={sel} onChange={e => setSel(e.target.value)}>{ctes.map(c => <option key={c.id} value={c.code}>{c.code} · {c.description}</option>)}</select></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 12 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={has} onChange={toggle} /> Sự kiện này cần thu thập <b>{kde.code}</b></label>
      {has && <div className="row" style={{ marginTop: 8 }}>
        <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}><input type="checkbox" style={{ width: 'auto' }} checked={cur.flagKey} onChange={e => setFlag('flagKey', e.target.checked)} /> Là Key (bắt buộc)</label>
        <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}><input type="checkbox" style={{ width: 'auto' }} checked={cur.flagOsOrgView} onChange={e => setFlag('flagOsOrgView', e.target.checked)} /> Cho user ngoài org xem</label></div>}
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: mỗi sự kiện tối đa 1 thành phần loại danh sách và phải có ít nhất 1 thành phần là Key.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Lưu ánh xạ</button></div>
    </Modal>
  )
}

function Glns() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.glns(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (g) => {
    if (!window.confirm(`Xóa địa điểm ${g.code}?`)) return
    try { const r = await api.deleteGln(g.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Địa điểm (GLN)</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / tên…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, code: '', name: '', gpsLat: '', gpsLong: '', remark: '', active: true })}>+ Thêm địa điểm</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Danh mục địa điểm toàn cầu (GS1 Global Location Number) — "từ điển" các địa điểm chuỗi cung ứng (nhà máy/kho/đại lý/cửa hàng) kèm toạ độ GPS để gắn vào sự kiện truy xuất.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã (GLNCode)</th><th>Tên địa điểm</th><th>Vĩ độ</th><th>Kinh độ</th><th>Ghi chú</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>{rows.map(g => (
            <tr key={g.id}><td style={{ fontFamily: 'monospace' }}>{g.code}</td><td>{g.name}</td>
              <td className="muted">{g.gpsLat || '—'}</td><td className="muted">{g.gpsLong || '—'}</td><td className="muted">{g.remark || '—'}</td>
              <td><Badge text={g.active ? 'Đang dùng' : 'Ngưng'} css={g.active ? 'success' : 'secondary'} /></td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(g)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(g)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Chưa có địa điểm.</td></tr>}</tbody></table>
      </div>
      {edit && <GlnForm gln={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function GlnForm({ gln, onClose, onSaved }) {
  const [f, setF] = useState({ ...gln }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveGln({ id: f.id, code: f.code, name: f.name, gpsLat: f.gpsLat, gpsLong: f.gpsLong, remark: f.remark, active: f.active }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa địa điểm ${f.code}` : 'Thêm địa điểm (GLN)'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã địa điểm (GLNCode) *"><input value={f.code} onChange={e => up('code', e.target.value)} placeholder="vd: 8930001000001" /></Field>
        <Field label="Tên địa điểm *"><input value={f.name} onChange={e => up('name', e.target.value)} /></Field></div>
      <div className="row"><Field label="Vĩ độ (GPSLat)"><input value={f.gpsLat || ''} onChange={e => up('gpsLat', e.target.value)} placeholder="10.7769" /></Field>
        <Field label="Kinh độ (GPSLong)"><input value={f.gpsLong || ''} onChange={e => up('gpsLong', e.target.value)} placeholder="106.7009" /></Field></div>
      <Field label="Ghi chú (Remark)"><input value={f.remark || ''} onChange={e => up('remark', e.target.value)} /></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.active} onChange={e => up('active', e.target.checked)} /> Đang sử dụng</label>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

function Templates() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.templates(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (t) => {
    if (!window.confirm(`Xóa mẫu ${t.tplNWType}?`)) return
    try { const r = await api.deleteTemplate(t.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  const approve = async (t) => {
    try { const r = await api.approveTemplate(t.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Mẫu loại tổ chức</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / tên…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0 })}>+ Thêm mẫu</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Mẫu loại tổ chức (GS1 Network Type Template) — "bộ khung" sự kiện (CTE) + thành phần dữ liệu (KDE) + ánh xạ áp dụng cho một loại tổ chức trong chuỗi cung ứng (Nhà sản xuất, Kho, Đại lý…).</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã (TplNWType)</th><th>Tên loại tổ chức</th><th className="right">Sự kiện</th><th className="right">Thành phần</th><th className="right">Ánh xạ</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>{rows.map(t => (
            <tr key={t.id}><td style={{ fontFamily: 'monospace' }}>{t.tplNWType}</td><td>{t.description}</td>
              <td className="right">{t.ctes}</td><td className="right">{t.kdes}</td><td className="right">{t.maps}</td>
              <td><Badge text={t.statusText} css={t.css} /></td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                {t.status === 0 && <button className="btn ghost sm" onClick={() => approve(t)}>Duyệt</button>}{' '}
                <button className="btn ghost sm" onClick={() => setEdit(t)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(t)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Chưa có mẫu loại tổ chức.</td></tr>}</tbody></table>
      </div>
      {edit && <TemplateForm tpl={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function TemplateForm({ tpl, onClose, onSaved }) {
  const [f, setF] = useState({ id: tpl.id, tplNWType: tpl.tplNWType || '', description: tpl.description || '', remark: tpl.remark || '' })
  const [ctes, setCtes] = useState([]); const [kdes, setKdes] = useState([]); const [maps, setMaps] = useState([])
  const [selCtes, setSelCtes] = useState([]); const [selKdes, setSelKdes] = useState([])
  const [err, setErr] = useState(''); const [loading, setLoading] = useState(true)
  const up = (k, v) => setF({ ...f, [k]: v })
  useEffect(() => {
    Promise.all([api.ctes(), api.kdes()]).then(([c, k]) => { setCtes(c.data); setKdes(k.data) })
    if (tpl.id) api.template(tpl.id).then(r => {
      setSelCtes(r.data.ctes.map(c => c.cteCode)); setSelKdes(r.data.kdes.map(k => k.kdeCode))
      setMaps(r.data.cteKdes.map(m => ({ cteCode: m.cteCode, kdeCode: m.kdeCode, flagKey: m.flagKey, flagOsOrgView: m.flagOsOrgView })))
    }).finally(() => setLoading(false))
    else setLoading(false)
  }, [tpl.id])
  const toggleCte = (code) => setSelCtes(selCtes.includes(code) ? selCtes.filter(x => x !== code) : [...selCtes, code])
  const toggleKde = (code) => setSelKdes(selKdes.includes(code) ? selKdes.filter(x => x !== code) : [...selKdes, code])
  const hasMap = (c, k) => maps.some(m => m.cteCode === c && m.kdeCode === k)
  const toggleMap = (c, k) => setMaps(hasMap(c, k) ? maps.filter(m => !(m.cteCode === c && m.kdeCode === k)) : [...maps, { cteCode: c, kdeCode: k, flagKey: false, flagOsOrgView: false }])
  const setMapFlag = (c, k, key, val) => setMaps(maps.map(m => (m.cteCode === c && m.kdeCode === k) ? { ...m, [key]: val } : m))
  const save = async () => {
    try {
      const body = {
        id: f.id, tplNWType: f.tplNWType, description: f.description, remark: f.remark,
        ctes: selCtes.map(c => ({ cteCode: c, active: true })),
        kdes: selKdes.map(k => ({ kdeCode: k, active: true })),
        cteKdes: maps.filter(m => selCtes.includes(m.cteCode) && selKdes.includes(m.kdeCode))
      }
      await api.saveTemplate(body); onSaved()
    } catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa mẫu ${f.tplNWType}` : 'Thêm mẫu loại tổ chức'} onClose={onClose} wide>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã loại tổ chức (TplNWType) *"><input value={f.tplNWType} onChange={e => up('tplNWType', e.target.value)} placeholder="vd: MANUFACTURER" /></Field>
        <Field label="Tên loại tổ chức *"><input value={f.description} onChange={e => up('description', e.target.value)} placeholder="vd: Nhà sản xuất" /></Field></div>
      <Field label="Ghi chú (Remark)"><input value={f.remark} onChange={e => up('remark', e.target.value)} /></Field>
      {loading ? <p className="muted">Đang tải…</p> : (
        <>
          <div className="section-t">Sự kiện (CTE) trong mẫu</div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
            {ctes.map(c => (
              <label key={c.id} style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13 }}>
                <input type="checkbox" style={{ width: 'auto' }} checked={selCtes.includes(c.code)} onChange={() => toggleCte(c.code)} /> {c.code}</label>))}
          </div>
          <div className="section-t">Thành phần dữ liệu (KDE) trong mẫu</div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 10 }}>
            {kdes.map(k => (
              <label key={k.id} style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13 }}>
                <input type="checkbox" style={{ width: 'auto' }} checked={selKdes.includes(k.code)} onChange={() => toggleKde(k.code)} /> {k.code}</label>))}
          </div>
          <div className="section-t">Ánh xạ CTE ↔ KDE (tích ô để gắn thành phần vào sự kiện)</div>
          <div style={{ overflow: 'auto' }}>
            <table><thead><tr><th>Sự kiện \ Thành phần</th>{selKdes.map(k => <th key={k} style={{ fontFamily: 'monospace' }}>{k}</th>)}</tr></thead>
              <tbody>{selCtes.map(c => (
                <tr key={c}><td style={{ fontFamily: 'monospace' }}>{c}</td>
                  {selKdes.map(k => (
                    <td key={k} style={{ textAlign: 'center' }}>
                      <input type="checkbox" style={{ width: 'auto' }} checked={hasMap(c, k)} onChange={() => toggleMap(c, k)} />
                      {hasMap(c, k) && <div style={{ fontSize: 11 }}>
                        <label style={{ display: 'block' }}><input type="checkbox" style={{ width: 'auto' }} checked={maps.find(m => m.cteCode === c && m.kdeCode === k).flagKey} onChange={e => setMapFlag(c, k, 'flagKey', e.target.checked)} /> Key</label>
                        <label style={{ display: 'block' }}><input type="checkbox" style={{ width: 'auto' }} checked={maps.find(m => m.cteCode === c && m.kdeCode === k).flagOsOrgView} onChange={e => setMapFlag(c, k, 'flagOsOrgView', e.target.checked)} /> Ngoài org</label></div>}
                    </td>))}</tr>))}
                {selCtes.length === 0 && <tr><td className="muted" style={{ padding: 12 }}>Chọn ít nhất 1 sự kiện và 1 thành phần.</td></tr>}</tbody></table>
          </div>
        </>
      )}
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: mẫu phải có ít nhất 1 sự kiện và 1 thành phần; mẫu đã duyệt không thể sửa.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Lưu mẫu</button></div>
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
        <Route path="kdes" element={<Kdes />} />
        <Route path="glns" element={<Glns />} />
        <Route path="templates" element={<Templates />} />
      </Route>
    </Routes>
  )
}
