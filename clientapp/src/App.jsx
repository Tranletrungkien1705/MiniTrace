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
        <NavLink to="/kdes">Thành phần (KDE)</NavLink><NavLink to="/data-types">Kiểu dữ liệu</NavLink><NavLink to="/glns">Địa điểm (GLN)</NavLink>
        <NavLink to="/farms">Nông trại</NavLink>
        <NavLink to="/market-areas">Vùng thị trường</NavLink>
        <NavLink to="/org-glns">Tổ chức ↔ Địa điểm</NavLink>
        <NavLink to="/templates">Mẫu loại tổ chức</NavLink>
        <NavLink to="/tpl-view-events">Mẫu hiển thị</NavLink>
        <NavLink to="/records">Sự kiện truy xuất</NavLink>
        <NavLink to="/stamps">Sinh tem</NavLink>
        <NavLink to="/boxes">Đóng hộp</NavLink>
        <NavLink to="/cartons">Đóng thùng</NavLink>
        <NavLink to="/que-syncs">Hàng đợi đồng bộ</NavLink>
        <NavLink to="/master-datas">Dữ liệu gốc</NavLink>
        <NavLink to="/network-orgs">Tổ chức mạng</NavLink>
        <NavLink to="/secrets">Số bí mật</NavLink>
        <NavLink to="/stamp-pairs">Cặp tem</NavLink>
        <NavLink to="/product-ids">Định danh sản phẩm</NavLink>
        <NavLink to="/config-column-searches">Cấu hình trường tra cứu</NavLink>
        <NavLink to="/manufactured-ids">Dãy sản xuất</NavLink></nav>
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

function DataTypes() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.dataTypes(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (d) => {
    if (!window.confirm(`Xóa kiểu dữ liệu ${d.code}?`)) return
    try { const r = await api.deleteDataType(d.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Kiểu dữ liệu</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / diễn giải…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, code: '', description: '', networkType: '', active: true })}>+ Thêm kiểu</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Danh mục kiểu dữ liệu (GS1 Data Type — Mst_DataType) — "từ điển" các kiểu dữ liệu mà một thành phần dữ liệu (KDE) có thể nhận (Text/Number/Date/List…).</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã (DataType)</th><th>Diễn giải</th><th>Loại mạng</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>{rows.map(d => (
            <tr key={d.id}><td style={{ fontFamily: 'monospace' }}>{d.code}</td><td>{d.description}</td>
              <td>{d.networkType || '—'}</td>
              <td><Badge text={d.active ? 'Đang dùng' : 'Ngưng'} css={d.active ? 'success' : 'secondary'} /></td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(d)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(d)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={5} className="muted" style={{ padding: 20 }}>Chưa có kiểu dữ liệu.</td></tr>}</tbody></table>
      </div>
      {edit && <DataTypeForm dt={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function DataTypeForm({ dt, onClose, onSaved }) {
  const [f, setF] = useState({ ...dt }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveDataType({ id: f.id, code: f.code, description: f.description, networkType: f.networkType, active: f.active }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa kiểu dữ liệu ${f.code}` : 'Thêm kiểu dữ liệu'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã kiểu dữ liệu (DataType) *"><input value={f.code} onChange={e => up('code', e.target.value)} placeholder="vd: Text" /></Field>
        <Field label="Loại mạng"><select value={f.networkType || ''} onChange={e => up('networkType', e.target.value)}>
          <option value="">—</option>{CTE_NET.map(n => <option key={n} value={n}>{n}</option>)}</select></Field></div>
      <Field label="Diễn giải (DataTypeDesc) *"><input value={f.description} onChange={e => up('description', e.target.value)} placeholder="vd: Chuỗi ký tự" /></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.active} onChange={e => up('active', e.target.checked)} /> Đang sử dụng</label>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: mã kiểu dữ liệu duy nhất; không xóa được kiểu đang được thành phần dữ liệu (KDE) dùng.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Lưu</button></div>
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

function Farms() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.farms(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (f) => {
    if (!window.confirm(`Xóa nông trại ${f.code}?`)) return
    try { const r = await api.deleteFarm(f.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Nông trại / vùng trồng</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / tên…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, code: '', name: '', networkType: '', active: true })}>+ Thêm nông trại</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Danh mục nông trại / vùng trồng (GS1 Farm) — "từ điển" nơi nuôi trồng/thu hoạch trong chuỗi truy xuất nguồn gốc, gắn với loại mạng (nhà sản xuất/đại lý).</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã (FarmCode)</th><th>Tên nông trại</th><th>Loại mạng</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>{rows.map(f => (
            <tr key={f.id}><td style={{ fontFamily: 'monospace' }}>{f.code}</td><td>{f.name}</td>
              <td>{f.networkType || '—'}</td>
              <td><Badge text={f.active ? 'Đang dùng' : 'Ngưng'} css={f.active ? 'success' : 'secondary'} /></td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(f)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(f)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={5} className="muted" style={{ padding: 20 }}>Chưa có nông trại.</td></tr>}</tbody></table>
      </div>
      {edit && <FarmForm farm={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function FarmForm({ farm, onClose, onSaved }) {
  const [f, setF] = useState({ ...farm }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveFarm({ id: f.id, code: f.code, name: f.name, networkType: f.networkType, active: f.active }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa nông trại ${f.code}` : 'Thêm nông trại'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã nông trại (FarmCode) *"><input value={f.code} onChange={e => up('code', e.target.value)} placeholder="vd: FARM-ST01" /></Field>
        <Field label="Loại mạng"><select value={f.networkType || ''} onChange={e => up('networkType', e.target.value)}>
          <option value="">—</option>{CTE_NET.map(n => <option key={n} value={n}>{n}</option>)}</select></Field></div>
      <Field label="Tên nông trại (FarmName) *"><input value={f.name} onChange={e => up('name', e.target.value)} /></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.active} onChange={e => up('active', e.target.checked)} /> Đang sử dụng</label>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

function OrgGlns() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.orgGlns(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (m) => {
    if (!window.confirm(`Xóa ánh xạ ${m.orgCode} ↔ ${m.glnCode}?`)) return
    try { const r = await api.deleteOrgGln(m.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Tổ chức ↔ Địa điểm</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã tổ chức / GLN…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, orgCode: '', glnCode: '', remark: '' })}>+ Thêm ánh xạ</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Ánh xạ tổ chức ↔ địa điểm (GS1 Mst_OrgIDMapGLN) — gắn một tổ chức (OrgID) với một địa điểm (GLN) trong chuỗi cung ứng, cho biết tổ chức đó hoạt động tại những địa điểm nào. Tên địa điểm + toạ độ GPS được join từ danh mục GLN.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã tổ chức (OrgID)</th><th>Mã địa điểm (GLNCode)</th><th>Tên địa điểm</th><th>Vĩ độ</th><th>Kinh độ</th><th>Ghi chú</th><th></th></tr></thead>
          <tbody>{rows.map(m => (
            <tr key={m.id}><td style={{ fontFamily: 'monospace' }}>{m.orgCode}</td><td style={{ fontFamily: 'monospace' }}>{m.glnCode}</td>
              <td>{m.glnName || '—'}</td><td className="muted">{m.gpsLat || '—'}</td><td className="muted">{m.gpsLong || '—'}</td><td className="muted">{m.remark || '—'}</td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(m)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(m)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Chưa có ánh xạ.</td></tr>}</tbody></table>
      </div>
      {edit && <OrgGlnForm map={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function OrgGlnForm({ map, onClose, onSaved }) {
  const [glns, setGlns] = useState([])
  const [f, setF] = useState({ id: map.id, orgCode: map.orgCode || '', glnCode: map.glnCode || '', remark: map.remark || '' }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  useEffect(() => { api.glns().then(r => setGlns(r.data)) }, [])
  const save = async () => {
    try { await api.saveOrgGln({ id: f.id, orgCode: f.orgCode, glnCode: f.glnCode, remark: f.remark }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa ánh xạ ${f.orgCode} ↔ ${f.glnCode}` : 'Thêm ánh xạ tổ chức ↔ địa điểm'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã tổ chức (OrgID) *"><input value={f.orgCode} onChange={e => up('orgCode', e.target.value)} placeholder="vd: MST-NXSX-ST" /></Field>
        <Field label="Địa điểm (GLNCode) *"><select value={f.glnCode} onChange={e => up('glnCode', e.target.value)}>
          <option value="">—</option>{glns.map(g => <option key={g.id} value={g.code}>{g.code} · {g.name}</option>)}</select></Field></div>
      <Field label="Ghi chú (Remark)"><input value={f.remark || ''} onChange={e => up('remark', e.target.value)} /></Field>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: địa điểm phải tồn tại trong danh mục GLN; mỗi cặp (tổ chức, địa điểm) chỉ được gắn một lần.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Lưu ánh xạ</button></div>
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

function TplViewEvents() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const [ctes, setCtes] = useState([])
  const load = () => api.tplViewEvents(q).then(r => setRows(r.data))
  useEffect(() => { load(); api.ctes().then(r => setCtes(r.data)) }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (v) => {
    if (!window.confirm(`Xóa mẫu hiển thị ${v.code}?`)) return
    try { const r = await api.deleteTplViewEvent(v.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Mẫu hiển thị sự kiện</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / mô tả / sự kiện…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, code: '', description: '', detail: '', cteCode: '', remark: '', active: true, flagBG: false })}>+ Thêm mẫu</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Mẫu hiển thị sự kiện truy xuất (GS1 Template View Event) — "khuôn hiển thị" cho một sự kiện (CTE): mô tả + chi tiết bố cục dùng để render hành trình truy xuất cho người tiêu dùng/đối tác.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã (TplVECode)</th><th>Mô tả</th><th>Sự kiện (CTE)</th><th>Chi tiết bố cục</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>{rows.map(v => (
            <tr key={v.id}><td style={{ fontFamily: 'monospace' }}>{v.code}</td><td>{v.description}</td>
              <td>{v.cteCode || '—'}</td><td className="muted" style={{ fontFamily: 'monospace', fontSize: 12 }}>{v.detail}</td>
              <td><Badge text={v.active ? 'Đang dùng' : 'Ngưng'} css={v.active ? 'success' : 'secondary'} />{v.flagBG ? <Badge text="Nền" css="info" /> : null}</td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(v)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(v)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={6} className="muted" style={{ padding: 20 }}>Chưa có mẫu hiển thị.</td></tr>}</tbody></table>
      </div>
      {edit && <TplViewEventForm ve={edit} ctes={ctes} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function TplViewEventForm({ ve, ctes, onClose, onSaved }) {
  const [f, setF] = useState({ ...ve }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveTplViewEvent({ id: f.id, code: f.code, description: f.description, detail: f.detail, cteCode: f.cteCode, remark: f.remark, active: f.active, flagBG: f.flagBG }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa mẫu hiển thị ${f.code}` : 'Thêm mẫu hiển thị sự kiện'} onClose={onClose} wide>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã mẫu hiển thị (TplVECode) *"><input value={f.code} onChange={e => up('code', e.target.value)} placeholder="vd: VE_PRODUCTION" /></Field>
        <Field label="Sự kiện (CTECode)"><select value={f.cteCode || ''} onChange={e => up('cteCode', e.target.value)}>
          <option value="">—</option>{ctes.map(c => <option key={c.id} value={c.code}>{c.code} · {c.description}</option>)}</select></Field></div>
      <Field label="Mô tả (TplVEDesc) *"><input value={f.description} onChange={e => up('description', e.target.value)} /></Field>
      <Field label="Chi tiết bố cục (TplVEDetail) *"><input value={f.detail} onChange={e => up('detail', e.target.value)} placeholder="vd: {Tên SP} · Lô {LOT_NO} · {Location}" /></Field>
      <Field label="Ghi chú (Remark)"><input value={f.remark || ''} onChange={e => up('remark', e.target.value)} /></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.active} onChange={e => up('active', e.target.checked)} /> Đang sử dụng</label>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 6 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.flagBG} onChange={e => up('flagBG', e.target.checked)} /> Mẫu nền (FlagBG)</label>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: mỗi sự kiện chỉ được có tối đa 1 mẫu hiển thị đang hoạt động.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Lưu mẫu</button></div>
    </Modal>
  )
}

function Records() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.records(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (r) => {
    if (!window.confirm(`Xóa bản ghi ${r.eventNo}?`)) return
    try { const res = await api.deleteRecord(r.id); flash(true, res.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Sự kiện truy xuất (CTE + KDE)</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / sự kiện…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0 })}>+ Ghi sự kiện</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Bản ghi hành trình truy xuất (GS1 Event_Event + Event_EventSpec) — mỗi bản ghi gắn một sự kiện trọng yếu (CTE) và tập giá trị thành phần dữ liệu (KDE). Bộ giá trị các KDE Key tạo "dấu vân tay" để chống trùng hành trình.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>EventNo</th><th>Sự kiện (CTE)</th><th>Địa điểm (GLN)</th><th className="right">Thành phần</th><th>Mẫu hiển thị</th><th>Cập nhật</th><th></th></tr></thead>
          <tbody>{rows.map(r => (
            <tr key={r.id}><td style={{ fontFamily: 'monospace' }}>{r.eventNo}</td><td>{r.cteCode}</td>
              <td>{r.glnOrgName || r.glnOrgCode || '—'}</td><td className="right">{r.specs}</td>
              <td className="muted">{r.tplVECode || '—'}</td><td>{fmtDateTime(r.updatedAt)}</td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(r)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(r)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Chưa có bản ghi sự kiện.</td></tr>}</tbody></table>
      </div>
      {edit && <RecordForm rec={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function RecordForm({ rec, onClose, onSaved }) {
  const [ctes, setCtes] = useState([]); const [glns, setGlns] = useState([])
  const [f, setF] = useState({ id: rec.id, cteCode: rec.cteCode || '', glnOrgCode: rec.glnOrgCode || '', remark: rec.remark || '' })
  const [specs, setSpecs] = useState([]); const [err, setErr] = useState(''); const [loading, setLoading] = useState(true)
  const up = (k, v) => setF({ ...f, [k]: v })
  useEffect(() => {
    Promise.all([api.ctes(), api.glns()]).then(([c, g]) => { setCtes(c.data); setGlns(g.data) })
    if (rec.id) api.record(rec.id).then(r => setSpecs(r.data.specs.map(s => ({ kdeCode: s.kdeCode, kdeValue: s.kdeValue || '', flagKey: s.flagKey, flagList: s.flagList })))).finally(() => setLoading(false))
    else setLoading(false)
  }, [rec.id])
  // Khi đổi sự kiện: nạp các KDE đã ánh xạ (CTE_KDE) làm dòng nhập giá trị.
  useEffect(() => {
    if (!f.cteCode) { setSpecs([]); return }
    api.cteKdes(f.cteCode).then(r => setSpecs(r.data.map(m => ({ kdeCode: m.kdeCode, kdeValue: '', flagKey: m.flagKey, flagList: false }))))
  }, [f.cteCode])
  const setVal = (code, val) => setSpecs(specs.map(s => s.kdeCode === code ? { ...s, kdeValue: val } : s))
  const save = async () => {
    try {
      await api.saveRecord({ id: f.id, cteCode: f.cteCode, glnOrgCode: f.glnOrgCode, remark: f.remark, specs: specs.map(s => ({ kdeCode: s.kdeCode, kdeValue: s.kdeValue })) })
      onSaved()
    } catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa bản ghi ${rec.eventNo}` : 'Ghi sự kiện truy xuất'} onClose={onClose} wide>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Sự kiện (CTECode) *"><select value={f.cteCode} onChange={e => up('cteCode', e.target.value)}>
          <option value="">—</option>{ctes.map(c => <option key={c.id} value={c.code}>{c.code} · {c.description}</option>)}</select></Field>
        <Field label="Địa điểm (GLN)"><select value={f.glnOrgCode} onChange={e => up('glnOrgCode', e.target.value)}>
          <option value="">—</option>{glns.map(g => <option key={g.id} value={g.code}>{g.code} · {g.name}</option>)}</select></Field></div>
      <Field label="Ghi chú (Remark)"><input value={f.remark} onChange={e => up('remark', e.target.value)} /></Field>
      <div className="section-t">Giá trị thành phần dữ liệu (KDE)</div>
      {loading ? <p className="muted">Đang tải…</p> : specs.length === 0 ? <p className="muted">Sự kiện này chưa có thành phần dữ liệu — hãy ánh xạ CTE_KDE trước.</p> : (
        <div style={{ overflow: 'auto' }}>
          <table><thead><tr><th>Thành phần (KDECode)</th><th>Giá trị (KDEValue)</th><th>Cờ</th></tr></thead>
            <tbody>{specs.map(s => (
              <tr key={s.kdeCode}><td style={{ fontFamily: 'monospace' }}>{s.kdeCode}</td>
                <td><input value={s.kdeValue} onChange={e => setVal(s.kdeCode, e.target.value)} /></td>
                <td>{s.flagKey ? <Badge text="Key" css="danger" /> : null}{s.flagList ? <Badge text="Danh sách" css="info" /> : null}</td></tr>))}</tbody></table>
        </div>
      )}
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: mọi thành phần Key phải có giá trị; mỗi sự kiện tối đa 1 thành phần loại danh sách; ghi lại cùng bộ Key sẽ cập nhật bản ghi cũ.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Lưu bản ghi</button></div>
    </Modal>
  )
}

const QR_TYPES = ['Tem sản phẩm', 'Tem hộp', 'Tem thùng', 'Tem thường']

function Stamps() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [show, setShow] = useState(false); const [msg, setMsg] = useState(null)
  const [open, setOpen] = useState(null)
  const load = () => api.stampBatches(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (b) => {
    if (!window.confirm(`Xóa lần sinh tem ${b.genTimesNo} và toàn bộ số tem?`)) return
    try { const r = await api.deleteStampBatch(b.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Sinh tem / Kho số tem</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã lần sinh / hàng hoá…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setShow(true)}>+ Sinh tem</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Sinh tem / kho số tem (GS1 Inv_GenTimes + Inv_InventoryGenID) — mỗi lần "chạy số" tem cho một sản phẩm sinh ra một lô số tem (IDNo/QR_ID/PIN/HashInformation) để in và gắn lên sản phẩm.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã lần sinh</th><th>Hàng hoá</th><th>Loại tem</th><th className="right">Số lượng</th><th>PIN</th><th>Lô SX</th><th>Ngày tạo</th><th></th></tr></thead>
          <tbody>{rows.map(b => (
            <tr key={b.id}><td style={{ fontFamily: 'monospace' }}>{b.genTimesNo}</td>
              <td>{b.productCode}{b.productName ? ` · ${b.productName}` : ''}</td>
              <td><Badge text={b.qrTypeText} css={b.css} /></td><td className="right">{b.qty}</td>
              <td>{b.flagPIN ? <Badge text="Có PIN" css="info" /> : <span className="muted">—</span>}</td>
              <td>{b.productionLotNo || '—'}</td><td>{fmtDate(b.createdAt)}</td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setOpen(b.id)}>Xem tem</button>{' '}
                <button className="btn gray sm" onClick={() => del(b)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={8} className="muted" style={{ padding: 20 }}>Chưa có lần sinh tem.</td></tr>}</tbody></table>
      </div>
      {show && <StampForm onClose={() => setShow(false)} onSaved={() => { setShow(false); load() }} />}
      {open && <StampList id={open} onClose={() => setOpen(null)} />}
    </>
  )
}

function StampForm({ onClose, onSaved }) {
  const [f, setF] = useState({ genTimesNo: '', productCode: '', productName: '', qrType: 0, qty: 10, flagPIN: true, productionLotNo: '', productionDate: '', shiftInCode: '', userKCS: '', remark: '' })
  const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.generateStamps({ ...f, qrType: Number(f.qrType), qty: Number(f.qty) }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title="Sinh tem (chạy số tem)" onClose={onClose} wide>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã lần sinh (GenTimesNo) *"><input value={f.genTimesNo} onChange={e => up('genTimesNo', e.target.value)} placeholder="vd: GT2601010001" /></Field>
        <Field label="Loại tem (QRType)"><select value={f.qrType} onChange={e => up('qrType', e.target.value)}>
          {QR_TYPES.map((t, i) => <option key={i} value={i}>{t}</option>)}</select></Field></div>
      <div className="row"><Field label="Mã hàng hoá (ProductCode) *"><input value={f.productCode} onChange={e => up('productCode', e.target.value)} placeholder="vd: 8930001001" /></Field>
        <Field label="Tên hàng hoá"><input value={f.productName} onChange={e => up('productName', e.target.value)} /></Field></div>
      <div className="row"><Field label="Số lượng tem (Qty) *"><input type="number" value={f.qty} onChange={e => up('qty', e.target.value)} /></Field>
        <Field label="Lô sản xuất"><input value={f.productionLotNo} onChange={e => up('productionLotNo', e.target.value)} /></Field></div>
      <div className="row"><Field label="Ngày sản xuất"><input value={f.productionDate} onChange={e => up('productionDate', e.target.value)} placeholder="yyyy-MM-dd" /></Field>
        <Field label="Ca sản xuất"><input value={f.shiftInCode} onChange={e => up('shiftInCode', e.target.value)} /></Field>
        <Field label="Người KCS"><input value={f.userKCS} onChange={e => up('userKCS', e.target.value)} /></Field></div>
      <Field label="Ghi chú (Remark)"><input value={f.remark} onChange={e => up('remark', e.target.value)} /></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.flagPIN} onChange={e => up('flagPIN', e.target.checked)} /> Sinh kèm PIN bí mật (chỉ tem sản phẩm)</label>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: mã lần sinh duy nhất; số lượng 1–100.000; tem sản phẩm + PIN sẽ sinh HashInformation = MD5(IDNo|PIN) để chống giả.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Sinh tem</button></div>
    </Modal>
  )
}

function StampList({ id, onClose }) {
  const [b, setB] = useState(null)
  useEffect(() => { api.stampBatch(id).then(r => setB(r.data)) }, [id])
  if (!b) return <Modal title="…" onClose={onClose}><p className="muted">Đang tải…</p></Modal>
  return (
    <Modal title={`Số tem — ${b.genTimesNo}`} onClose={onClose} wide>
      <dl className="dl"><dt>Hàng hoá</dt><dd>{b.productCode}{b.productName ? ` · ${b.productName}` : ''}</dd>
        <dt>Loại tem</dt><dd>{b.qrTypeText}</dd><dt>Số lượng</dt><dd>{b.qty}</dd>
        <dt>Lô SX</dt><dd>{b.productionLotNo || '—'}</dd><dt>Ngày SX</dt><dd>{b.productionDate || '—'}</dd></dl>
      <div className="section-t">Danh sách tem ({b.stamps.length})</div>
      <div style={{ overflow: 'auto', maxHeight: 360 }}>
        <table><thead><tr><th>IDNo</th><th>QR_ID</th><th>PIN</th><th>HashInformation</th><th>Trạng thái</th></tr></thead>
          <tbody>{b.stamps.map(s => (
            <tr key={s.idNo}><td style={{ fontFamily: 'monospace' }}>{s.idNo}</td><td style={{ fontFamily: 'monospace' }}>{s.qrId}</td>
              <td style={{ fontFamily: 'monospace' }}>{s.pin || '—'}</td><td className="muted" style={{ fontFamily: 'monospace', fontSize: 11 }}>{s.hashInformation || '—'}</td>
              <td><Badge text={s.flagUsed ? 'Đã dùng' : 'Chưa dùng'} css={s.flagUsed ? 'secondary' : 'success'} /></td></tr>))}</tbody></table>
      </div>
    </Modal>
  )
}

function Boxes() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [show, setShow] = useState(false); const [msg, setMsg] = useState(null)
  const [open, setOpen] = useState(null)
  const load = () => api.boxes(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (b) => {
    if (!window.confirm(`Xóa hộp ${b.boxNo} và toàn bộ tem trong hộp?`)) return
    try { const r = await api.deleteBox(b.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Đóng hộp / Gán tem vào hộp</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã hộp / hàng hoá…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setShow(true)}>+ Tạo hộp</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Đóng hộp (GS1 Inv_InventoryGenBox + Map_IDInBox) — gom nhiều tem sản phẩm (IDNo) vào một hộp (BoxNo) để đóng gói vận chuyển. Mỗi tem chỉ được nằm trong một hộp.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã hộp</th><th>Hàng hoá</th><th className="right">Số tem</th><th>Trạng thái</th><th>Ngày tạo</th><th></th></tr></thead>
          <tbody>{rows.map(b => (
            <tr key={b.id}><td style={{ fontFamily: 'monospace' }}>{b.boxNo}</td>
              <td>{b.productCode || '—'}{b.productName ? ` · ${b.productName}` : ''}</td>
              <td className="right">{b.items}</td>
              <td><Badge text={b.flagMap ? 'Đã gán tem' : 'Chưa gán'} css={b.flagMap ? 'success' : 'secondary'} /></td>
              <td>{fmtDate(b.createdAt)}</td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setOpen(b.id)}>Xem tem</button>{' '}
                <button className="btn gray sm" onClick={() => del(b)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={6} className="muted" style={{ padding: 20 }}>Chưa có hộp.</td></tr>}</tbody></table>
      </div>
      {show && <BoxForm onClose={() => setShow(false)} onSaved={() => { setShow(false); load() }} />}
      {open && <BoxDetail id={open} onClose={() => setOpen(null)} onChanged={load} />}
    </>
  )
}

function BoxForm({ onClose, onSaved }) {
  const [f, setF] = useState({ boxNo: '', productCode: '', productName: '', remark: '' }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.createBox(f); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title="Tạo hộp đóng gói" onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã hộp (BoxNo) *"><input value={f.boxNo} onChange={e => up('boxNo', e.target.value)} placeholder="vd: B2601010001" /></Field>
        <Field label="Mã hàng hoá (ProductCode)"><input value={f.productCode} onChange={e => up('productCode', e.target.value)} placeholder="vd: 8930001001" /></Field></div>
      <Field label="Tên hàng hoá"><input value={f.productName} onChange={e => up('productName', e.target.value)} /></Field>
      <Field label="Ghi chú (Remark)"><input value={f.remark} onChange={e => up('remark', e.target.value)} /></Field>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: mã hộp duy nhất trong tenant.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Tạo hộp</button></div>
    </Modal>
  )
}

function BoxDetail({ id, onClose, onChanged }) {
  const [b, setB] = useState(null); const [msg, setMsg] = useState(null)
  const [stamps, setStamps] = useState([]); const [sel, setSel] = useState([]); const [invCode, setInvCode] = useState('')
  const load = () => api.box(id).then(r => setB(r.data))
  useEffect(() => { load(); api.stamps(null, '').then(r => setStamps(r.data)) }, [id])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const inBox = new Set((b?.items || []).map(i => i.idNo))
  const toggle = (idNo) => setSel(sel.includes(idNo) ? sel.filter(x => x !== idNo) : [...sel, idNo])
  const add = async () => {
    if (sel.length === 0) { flash(false, 'Chọn ít nhất 1 tem.'); return }
    try { const r = await api.addStampsToBox(id, { idNos: sel, invCode }); flash(true, r.data.msg); setSel([]); load(); onChanged() }
    catch (e) { flash(false, e.message) }
  }
  if (!b) return <Modal title="…" onClose={onClose}><p className="muted">Đang tải…</p></Modal>
  return (
    <Modal title={`Hộp ${b.boxNo}`} onClose={onClose} wide>
      <Flash msg={msg} />
      <dl className="dl"><dt>Hàng hoá</dt><dd>{b.productCode || '—'}{b.productName ? ` · ${b.productName}` : ''}</dd>
        <dt>Số tem</dt><dd>{b.items.length}</dd><dt>Ghi chú</dt><dd>{b.remark || '—'}</dd></dl>
      <div className="section-t">Tem trong hộp ({b.items.length})</div>
      <div style={{ overflow: 'auto', maxHeight: 220 }}>
        <table><thead><tr><th>IDNo</th><th>Hàng hoá</th><th>Vị trí kho</th><th>Trạng thái</th></tr></thead>
          <tbody>{b.items.map(i => (
            <tr key={i.id}><td style={{ fontFamily: 'monospace' }}>{i.idNo}</td><td>{i.productCode || '—'}</td>
              <td>{i.invCode || '—'}</td><td><Badge text={i.flagActive ? 'Hiệu lực' : 'Ngưng'} css={i.flagActive ? 'success' : 'secondary'} /></td></tr>))}
            {b.items.length === 0 && <tr><td colSpan={4} className="muted" style={{ padding: 16 }}>Hộp chưa có tem.</td></tr>}</tbody></table>
      </div>
      <div className="section-t" style={{ marginTop: 14 }}>Gán tem vào hộp</div>
      <Field label="Vị trí kho (InvCode)"><input value={invCode} onChange={e => setInvCode(e.target.value)} placeholder="vd: KHO-FG-ST" /></Field>
      <div style={{ overflow: 'auto', maxHeight: 220, marginTop: 8 }}>
        <table><thead><tr><th></th><th>IDNo</th><th>QR_ID</th><th>Trạng thái</th></tr></thead>
          <tbody>{stamps.map(s => {
            const used = inBox.has(s.idNo)
            return (
              <tr key={s.id}><td><input type="checkbox" style={{ width: 'auto' }} disabled={used} checked={sel.includes(s.idNo)} onChange={() => toggle(s.idNo)} /></td>
                <td style={{ fontFamily: 'monospace' }}>{s.idNo}</td><td style={{ fontFamily: 'monospace' }}>{s.qrId}</td>
                <td>{used ? <Badge text="Đã trong hộp" css="secondary" /> : <Badge text="Chưa gán" css="success" />}</td></tr>)
          })}</tbody></table>
      </div>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: tem phải tồn tại trong kho số tem; mỗi tem chỉ được nằm trong một hộp.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={add}>Gán {sel.length} tem vào hộp</button></div>
    </Modal>
  )
}

function Cartons() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [show, setShow] = useState(false); const [msg, setMsg] = useState(null)
  const [open, setOpen] = useState(null)
  const load = () => api.cartons(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (c) => {
    if (!window.confirm(`Xóa thùng ${c.canNo} và toàn bộ hộp trong thùng?`)) return
    try { const r = await api.deleteCarton(c.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Đóng thùng / Gán hộp vào thùng</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã thùng / hàng hoá…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setShow(true)}>+ Tạo thùng</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Đóng thùng (GS1 Inv_InventoryGenCarton + Map_BoxInCarton) — cấp cao nhất trong hierarchy Thùng→Hộp→Sản phẩm: gom nhiều hộp (BoxNo) vào một thùng (CanNo) để vận chuyển. Mỗi hộp chỉ được nằm trong một thùng.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã thùng</th><th>Hàng hoá</th><th className="right">Số hộp</th><th>Trạng thái</th><th>Ngày tạo</th><th></th></tr></thead>
          <tbody>{rows.map(c => (
            <tr key={c.id}><td style={{ fontFamily: 'monospace' }}>{c.canNo}</td>
              <td>{c.productCode || '—'}{c.productName ? ` · ${c.productName}` : ''}</td>
              <td className="right">{c.items}</td>
              <td><Badge text={c.flagMap ? 'Đã gán hộp' : 'Chưa gán'} css={c.flagMap ? 'success' : 'secondary'} /></td>
              <td>{fmtDate(c.createdAt)}</td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setOpen(c.id)}>Xem hộp</button>{' '}
                <button className="btn gray sm" onClick={() => del(c)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={6} className="muted" style={{ padding: 20 }}>Chưa có thùng.</td></tr>}</tbody></table>
      </div>
      {show && <CartonForm onClose={() => setShow(false)} onSaved={() => { setShow(false); load() }} />}
      {open && <CartonDetail id={open} onClose={() => setOpen(null)} onChanged={load} />}
    </>
  )
}

function CartonForm({ onClose, onSaved }) {
  const [f, setF] = useState({ canNo: '', productCode: '', productName: '', remark: '' }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.createCarton(f); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title="Tạo thùng đóng gói" onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã thùng (CanNo) *"><input value={f.canNo} onChange={e => up('canNo', e.target.value)} placeholder="vd: C2601010001" /></Field>
        <Field label="Mã hàng hoá (ProductCode)"><input value={f.productCode} onChange={e => up('productCode', e.target.value)} placeholder="vd: 8930001001" /></Field></div>
      <Field label="Tên hàng hoá"><input value={f.productName} onChange={e => up('productName', e.target.value)} /></Field>
      <Field label="Ghi chú (Remark)"><input value={f.remark} onChange={e => up('remark', e.target.value)} /></Field>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: mã thùng duy nhất trong tenant.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Tạo thùng</button></div>
    </Modal>
  )
}

function CartonDetail({ id, onClose, onChanged }) {
  const [c, setC] = useState(null); const [msg, setMsg] = useState(null)
  const [boxes, setBoxes] = useState([]); const [sel, setSel] = useState([]); const [invCode, setInvCode] = useState('')
  const load = () => api.carton(id).then(r => setC(r.data))
  useEffect(() => { load(); api.boxes('').then(r => setBoxes(r.data)) }, [id])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const inCarton = new Set((c?.items || []).map(i => i.boxNo))
  const toggle = (boxNo) => setSel(sel.includes(boxNo) ? sel.filter(x => x !== boxNo) : [...sel, boxNo])
  const add = async () => {
    if (sel.length === 0) { flash(false, 'Chọn ít nhất 1 hộp.'); return }
    try { const r = await api.addBoxesToCarton(id, { boxNos: sel, invCode }); flash(true, r.data.msg); setSel([]); load(); onChanged() }
    catch (e) { flash(false, e.message) }
  }
  if (!c) return <Modal title="…" onClose={onClose}><p className="muted">Đang tải…</p></Modal>
  return (
    <Modal title={`Thùng ${c.canNo}`} onClose={onClose} wide>
      <Flash msg={msg} />
      <dl className="dl"><dt>Hàng hoá</dt><dd>{c.productCode || '—'}{c.productName ? ` · ${c.productName}` : ''}</dd>
        <dt>Số hộp</dt><dd>{c.items.length}</dd><dt>Ghi chú</dt><dd>{c.remark || '—'}</dd></dl>
      <div className="section-t">Hộp trong thùng ({c.items.length})</div>
      <div style={{ overflow: 'auto', maxHeight: 220 }}>
        <table><thead><tr><th>BoxNo</th><th>Hàng hoá</th><th>Vị trí kho</th><th>Trạng thái</th></tr></thead>
          <tbody>{c.items.map(i => (
            <tr key={i.id}><td style={{ fontFamily: 'monospace' }}>{i.boxNo}</td><td>{i.productCode || '—'}</td>
              <td>{i.invCode || '—'}</td><td><Badge text={i.flagActive ? 'Hiệu lực' : 'Ngưng'} css={i.flagActive ? 'success' : 'secondary'} /></td></tr>))}
            {c.items.length === 0 && <tr><td colSpan={4} className="muted" style={{ padding: 16 }}>Thùng chưa có hộp.</td></tr>}</tbody></table>
      </div>
      <div className="section-t" style={{ marginTop: 14 }}>Gán hộp vào thùng</div>
      <Field label="Vị trí kho (InvCode)"><input value={invCode} onChange={e => setInvCode(e.target.value)} placeholder="vd: KHO-FG-ST" /></Field>
      <div style={{ overflow: 'auto', maxHeight: 220, marginTop: 8 }}>
        <table><thead><tr><th></th><th>BoxNo</th><th>Hàng hoá</th><th>Trạng thái</th></tr></thead>
          <tbody>{boxes.map(b => {
            const used = inCarton.has(b.boxNo)
            return (
              <tr key={b.id}><td><input type="checkbox" style={{ width: 'auto' }} disabled={used} checked={sel.includes(b.boxNo)} onChange={() => toggle(b.boxNo)} /></td>
                <td style={{ fontFamily: 'monospace' }}>{b.boxNo}</td><td>{b.productCode || '—'}</td>
                <td>{used ? <Badge text="Đã trong thùng" css="secondary" /> : <Badge text="Chưa gán" css="success" />}</td></tr>)
          })}</tbody></table>
      </div>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: hộp phải tồn tại trong kho số hộp; mỗi hộp chỉ được nằm trong một thùng.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={add}>Gán {sel.length} hộp vào thùng</button></div>
    </Modal>
  )
}

function QueSyncs() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.queSyncs(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (x) => {
    if (!window.confirm(`Xóa bản ghi hàng đợi ${x.queSyncNo}?`)) return
    try { const r = await api.deleteQueSync(x.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  const mark = async (x, status) => {
    try { const r = await api.markQueSync(x.id, { status }); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Hàng đợi đồng bộ</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / bảng / môi trường…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, networkId: '', queSyncNo: '', tableCode: '', flagSyncBL: false, remark: '' })}>+ Thêm vào hàng đợi</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Hàng đợi đồng bộ dữ liệu truy xuất (GS1 MstSv_QueSync) — mỗi dòng là một bản ghi danh mục/sự kiện (TableCode) cần đẩy lên máy chủ eTEM/ELTS theo môi trường (NetworkID). Bộ (NetworkID, QueSyncNo, TableCode) duy nhất để chống đẩy trùng.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Môi trường</th><th>Mã bản ghi</th><th>Loại dữ liệu</th><th>Blockchain</th><th>Trạng thái</th><th className="right">Thử lại</th><th>Đồng bộ lúc</th><th></th></tr></thead>
          <tbody>{rows.map(x => (
            <tr key={x.id}><td>{x.networkId}</td><td style={{ fontFamily: 'monospace' }}>{x.queSyncNo}</td>
              <td style={{ fontFamily: 'monospace' }}>{x.tableCode}</td>
              <td>{x.flagSyncBL ? <Badge text="Có" css="info" /> : <span className="muted">—</span>}</td>
              <td><Badge text={x.statusText} css={x.css} />{x.errorDetail ? <div className="muted" style={{ fontSize: 11 }}>{x.errorDetail}</div> : null}</td>
              <td className="right">{x.retryCount}</td><td>{x.syncedAt ? fmtDateTime(x.syncedAt) : '—'}</td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                {x.status !== 1 && <button className="btn ghost sm" onClick={() => mark(x, 1)}>Đã đồng bộ</button>}{' '}
                {x.status !== 2 && <button className="btn ghost sm" onClick={() => mark(x, 2)}>Báo lỗi</button>}{' '}
                <button className="btn ghost sm" onClick={() => setEdit(x)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(x)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={8} className="muted" style={{ padding: 20 }}>Hàng đợi trống.</td></tr>}</tbody></table>
      </div>
      {edit && <QueSyncForm row={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function QueSyncForm({ row, onClose, onSaved }) {
  const [f, setF] = useState({ id: row.id, networkId: row.networkId || '', queSyncNo: row.queSyncNo || '', tableCode: row.tableCode || '', flagSyncBL: !!row.flagSyncBL, remark: row.remark || '' })
  const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveQueSync({ id: f.id, networkId: f.networkId, queSyncNo: f.queSyncNo, tableCode: f.tableCode, flagSyncBL: f.flagSyncBL, remark: f.remark }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa hàng đợi ${f.queSyncNo}` : 'Thêm vào hàng đợi đồng bộ'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Môi trường (NetworkID) *"><input value={f.networkId} onChange={e => up('networkId', e.target.value)} placeholder="vd: Manufacturer" /></Field>
        <Field label="Loại dữ liệu (TableCode) *"><input value={f.tableCode} onChange={e => up('tableCode', e.target.value)} placeholder="vd: Mst_CTE" /></Field></div>
      <Field label="Mã bản ghi nguồn (QueSyncNo) *"><input value={f.queSyncNo} onChange={e => up('queSyncNo', e.target.value)} placeholder="vd: PRODUCTION_IN" /></Field>
      <Field label="Ghi chú (Remark)"><input value={f.remark} onChange={e => up('remark', e.target.value)} /></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.flagSyncBL} onChange={e => up('flagSyncBL', e.target.checked)} /> Đồng bộ lên blockchain (FlagSyncBL)</label>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: cần đủ môi trường + mã bản ghi + loại dữ liệu; bộ ba này duy nhất trong tenant; lưu xong bản ghi quay về trạng thái chờ đồng bộ.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

function MasterDatas() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.masterDatas(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (m) => {
    if (!window.confirm(`Xóa danh mục dữ liệu gốc ${m.code}?`)) return
    try { const r = await api.deleteMasterData(m.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Dữ liệu gốc</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / tên bảng…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, code: '', networkId: '', tableName: '', active: true, remark: '' })}>+ Thêm danh mục</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Danh mục dữ liệu gốc (GS1 Master Data — Mst_MasterData) — "từ điển" các bảng/danh mục tham chiếu mà eTEM dùng để tra cứu động (MDCode ↔ TableName).</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã (MDCode)</th><th>Tên bảng (TableName)</th><th>Loại mạng</th><th>Trạng thái</th><th>Ghi chú</th><th></th></tr></thead>
          <tbody>{rows.map(m => (
            <tr key={m.id}><td style={{ fontFamily: 'monospace' }}>{m.code}</td><td style={{ fontFamily: 'monospace' }}>{m.tableName}</td>
              <td>{m.networkId || '—'}</td>
              <td><Badge text={m.active ? 'Đang dùng' : 'Ngưng'} css={m.active ? 'success' : 'secondary'} /></td>
              <td>{m.remark || '—'}</td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(m)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(m)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={6} className="muted" style={{ padding: 20 }}>Chưa có danh mục dữ liệu gốc.</td></tr>}</tbody></table>
      </div>
      {edit && <MasterDataForm md={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function MasterDataForm({ md, onClose, onSaved }) {
  const [f, setF] = useState({ ...md }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveMasterData({ id: f.id, code: f.code, networkId: f.networkId, tableName: f.tableName, active: f.active, remark: f.remark }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa danh mục ${f.code}` : 'Thêm danh mục dữ liệu gốc'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã danh mục (MDCode) *"><input value={f.code} onChange={e => up('code', e.target.value)} placeholder="vd: MD_CTE" /></Field>
        <Field label="Loại mạng (NetworkID)"><select value={f.networkId || ''} onChange={e => up('networkId', e.target.value)}>
          <option value="">—</option>{CTE_NET.map(n => <option key={n} value={n}>{n}</option>)}</select></Field></div>
      <Field label="Tên bảng dữ liệu (TableName) *"><input value={f.tableName} onChange={e => up('tableName', e.target.value)} placeholder="vd: Mst_CTE" /></Field>
      <Field label="Ghi chú"><input value={f.remark || ''} onChange={e => up('remark', e.target.value)} /></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.active} onChange={e => up('active', e.target.checked)} /> Đang hoạt động</label>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: cần mã danh mục + tên bảng; mã danh mục duy nhất trong tenant.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

function NetworkOrgs() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.networkOrgs(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (o) => {
    if (!window.confirm(`Xóa tổ chức ${o.mst}?`)) return
    try { const r = await api.deleteNetworkOrg(o.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  const register = async (o) => {
    try { const r = await api.registerNetworkOrg(o.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Tổ chức mạng</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm MST / tên / loại mạng…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, mst: '', fullName: '', networkType: '', orgCode: '', address: '', mobile: '', contactName: '', contactEmail: '', gln: '', active: true, remark: '' })}>+ Thêm tổ chức</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Tổ chức tham gia mạng lưới truy xuất (GS1 Network Organization — Mst_NNT) — doanh nghiệp đăng ký tham gia chuỗi theo loại mạng. Đăng ký mạng sẽ cấp mã định danh ngoài mạng (ELTSMSTId) và đưa vào hàng đợi đồng bộ (Mst_NNT_QueSync).</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>MST</th><th>Tên tổ chức</th><th>Loại mạng</th><th>Mã mạng (ELTSMSTId)</th><th>Trạng thái</th><th>Hoạt động</th><th></th></tr></thead>
          <tbody>{rows.map(o => (
            <tr key={o.id}><td style={{ fontFamily: 'monospace' }}>{o.mst}</td><td>{o.fullName}</td>
              <td>{o.networkType || '—'}</td>
              <td style={{ fontFamily: 'monospace' }}>{o.eltsMstId || '—'}</td>
              <td><Badge text={o.statusText} css={o.css} /></td>
              <td><Badge text={o.active ? 'Đang dùng' : 'Ngưng'} css={o.active ? 'success' : 'secondary'} /></td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => register(o)}>Đăng ký mạng</button>{' '}
                <button className="btn ghost sm" onClick={() => setEdit(o)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(o)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Chưa có tổ chức.</td></tr>}</tbody></table>
      </div>
      {edit && <NetworkOrgForm org={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function NetworkOrgForm({ org, onClose, onSaved }) {
  const [f, setF] = useState({ ...org }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveNetworkOrg({ id: f.id, mst: f.mst, fullName: f.fullName, networkType: f.networkType, orgCode: f.orgCode, address: f.address, mobile: f.mobile, contactName: f.contactName, contactEmail: f.contactEmail, gln: f.gln, active: f.active, remark: f.remark }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa tổ chức ${f.mst}` : 'Thêm tổ chức mạng'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã số thuế / định danh (MST) *"><input value={f.mst} onChange={e => up('mst', e.target.value)} placeholder="vd: MST-NXSX-ST" /></Field>
        <Field label="Loại mạng (NetworkID)"><select value={f.networkType || ''} onChange={e => up('networkType', e.target.value)}>
          <option value="">—</option>{CTE_NET.map(n => <option key={n} value={n}>{n}</option>)}</select></Field></div>
      <Field label="Tên đầy đủ của tổ chức (NNTFullName) *"><input value={f.fullName} onChange={e => up('fullName', e.target.value)} /></Field>
      <div className="row"><Field label="Mã tổ chức nội bộ (OrgID)"><input value={f.orgCode || ''} onChange={e => up('orgCode', e.target.value)} /></Field>
        <Field label="Mã địa điểm (GLN)"><input value={f.gln || ''} onChange={e => up('gln', e.target.value)} /></Field></div>
      <Field label="Địa chỉ"><input value={f.address || ''} onChange={e => up('address', e.target.value)} /></Field>
      <div className="row"><Field label="Điện thoại"><input value={f.mobile || ''} onChange={e => up('mobile', e.target.value)} /></Field>
        <Field label="Người liên hệ"><input value={f.contactName || ''} onChange={e => up('contactName', e.target.value)} /></Field></div>
      <Field label="Email liên hệ"><input value={f.contactEmail || ''} onChange={e => up('contactEmail', e.target.value)} /></Field>
      <Field label="Ghi chú"><input value={f.remark || ''} onChange={e => up('remark', e.target.value)} /></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.active} onChange={e => up('active', e.target.checked)} /> Đang hoạt động</label>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: cần MST + tên đầy đủ; MST duy nhất trong tenant. Đăng ký mạng chỉ cho tổ chức đang hoạt động.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

function Secrets() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [used, setUsed] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.secrets(q, used === '' ? undefined : used === 'true').then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (s) => {
    if (!window.confirm(`Xóa số bí mật ${s.secretNo}?`)) return
    try { const r = await api.deleteSecret(s.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  const use = async (s) => {
    try { const r = await api.markSecretUsed(s.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Số bí mật</h1><div className="sp" />
        <input style={{ maxWidth: 200 }} placeholder="Tìm serial / số bí mật / QR…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <select style={{ maxWidth: 150 }} value={used} onChange={e => { setUsed(e.target.value); setTimeout(load, 0) }}>
          <option value="">Tất cả</option><option value="false">Chưa dùng</option><option value="true">Đã dùng</option></select>
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({ id: 0, serialNo: '', secretNo: '', qrSerialNo: '', networkId: '', mst: '', orgCode: '', genTimesNo: '', flagMap: false, remark: '' })}>+ Thêm số bí mật</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Kho số bí mật (GS1 Secret Inventory — Inv_InventorySecret) — số bí mật in lên tem cào chống giả, gắn với serial sản phẩm. Phát hành/dùng sẽ đánh dấu FlagUsed.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Serial</th><th>Số bí mật</th><th>QR</th><th>Lần sinh</th><th>Ghép serial</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>{rows.map(s => (
            <tr key={s.id}><td style={{ fontFamily: 'monospace' }}>{s.serialNo}</td>
              <td style={{ fontFamily: 'monospace' }}>{s.secretNo}</td>
              <td style={{ fontFamily: 'monospace' }}>{s.qrSerialNo || '—'}</td>
              <td>{s.genTimesNo || '—'}</td>
              <td><Badge text={s.flagMap ? 'Đã ghép' : 'Chưa ghép'} css={s.flagMap ? 'success' : 'secondary'} /></td>
              <td><Badge text={s.flagUsed ? 'Đã dùng' : 'Chưa dùng'} css={s.flagUsed ? 'success' : 'warning'} /></td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                {!s.flagUsed && <button className="btn ghost sm" onClick={() => use(s)}>Phát hành</button>}{' '}
                <button className="btn ghost sm" onClick={() => setEdit(s)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(s)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Chưa có số bí mật.</td></tr>}</tbody></table>
      </div>
      {edit && <SecretForm secret={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function SecretForm({ secret, onClose, onSaved }) {
  const [f, setF] = useState({ ...secret }); const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveSecret({ id: f.id, serialNo: f.serialNo, secretNo: f.secretNo, qrSerialNo: f.qrSerialNo, networkId: f.networkId, mst: f.mst, orgCode: f.orgCode, genTimesNo: f.genTimesNo, flagMap: f.flagMap, remark: f.remark }); onSaved() }
    catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? `Sửa số bí mật ${f.secretNo}` : 'Thêm số bí mật'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Số serial sản phẩm (SerialNo) *"><input value={f.serialNo} onChange={e => up('serialNo', e.target.value)} placeholder="vd: P000001" /></Field>
        <Field label="Số bí mật (SecretNo) *"><input value={f.secretNo} onChange={e => up('secretNo', e.target.value)} placeholder="vd: SEC000001" /></Field></div>
      <div className="row"><Field label="Mã QR in kèm (QR_SerialNo)"><input value={f.qrSerialNo || ''} onChange={e => up('qrSerialNo', e.target.value)} /></Field>
        <Field label="Lần sinh số (GenTimesNo)"><input value={f.genTimesNo || ''} onChange={e => up('genTimesNo', e.target.value)} /></Field></div>
      <div className="row"><Field label="Loại mạng (NetworkID)"><select value={f.networkId || ''} onChange={e => up('networkId', e.target.value)}>
          <option value="">—</option>{CTE_NET.map(n => <option key={n} value={n}>{n}</option>)}</select></Field>
        <Field label="Mã số thuế / định danh (MST)"><input value={f.mst || ''} onChange={e => up('mst', e.target.value)} /></Field></div>
      <Field label="Mã tổ chức nội bộ (OrgID)"><input value={f.orgCode || ''} onChange={e => up('orgCode', e.target.value)} /></Field>
      <Field label="Ghi chú"><input value={f.remark || ''} onChange={e => up('remark', e.target.value)} /></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.flagMap} onChange={e => up('flagMap', e.target.checked)} /> Đã ghép với serial sản phẩm</label>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: cần SerialNo + SecretNo; SecretNo duy nhất trong tenant.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>Lưu</button></div>
    </Modal>
  )
}

function MarketAreas() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.marketAreas(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (m) => {
    if (!window.confirm(`Xóa vùng thị trường ${m.code}?`)) return
    try { const r = await api.deleteMarketArea(m.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Vùng thị trường</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / tên…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({})}>+ Thêm vùng</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Danh mục vùng thị trường (GS1 Mst_MarketArea) — "từ điển" các vùng phân phối (miền/khu vực) dùng để gắn vào hồ sơ phân phối truy xuất.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã</th><th>Tên vùng</th><th>Loại</th><th>Diễn giải</th><th>Trạng thái</th><th>Ngày tạo</th><th></th></tr></thead>
          <tbody>{rows.map(m => (
            <tr key={m.id}><td style={{ fontFamily: 'monospace' }}>{m.code}</td><td>{m.name}</td>
              <td>{m.areaType || '—'}</td><td>{m.description || '—'}</td>
              <td><Badge text={m.active ? 'Hoạt động' : 'Ngưng'} css={m.active ? 'success' : 'secondary'} /></td>
              <td>{fmtDate(m.createdAt)}</td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(m)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(m)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Chưa có vùng thị trường.</td></tr>}</tbody></table>
      </div>
      {edit && <MarketAreaForm row={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function MarketAreaForm({ row, onClose, onSaved }) {
  const [f, setF] = useState({ id: row.id || 0, code: row.code || '', name: row.name || '', areaType: row.areaType || '', description: row.description || '', active: row.active !== false })
  const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveMarketArea(f); onSaved() } catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? 'Sửa vùng thị trường' : 'Thêm vùng thị trường'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã vùng (MarketAreaCode) *"><input value={f.code} onChange={e => up('code', e.target.value)} placeholder="vd: MA-MIENB" /></Field>
        <Field label="Tên vùng (MarketAreaName) *"><input value={f.name} onChange={e => up('name', e.target.value)} placeholder="vd: Miền Bắc" /></Field></div>
      <div className="row"><Field label="Loại vùng (MarketAreaType)"><input value={f.areaType} onChange={e => up('areaType', e.target.value)} placeholder="vd: Region / City" /></Field>
        <Field label="Trạng thái"><select value={f.active ? '1' : '0'} onChange={e => up('active', e.target.value === '1')}><option value="1">Hoạt động</option><option value="0">Ngưng</option></select></Field></div>
      <Field label="Diễn giải (MarketAreaDesc)"><input value={f.description} onChange={e => up('description', e.target.value)} /></Field>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: cần mã + tên vùng thị trường; mã vùng duy nhất trong tenant.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>{f.id ? 'Lưu' : 'Thêm'}</button></div>
    </Modal>
  )
}

function StampPairs() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.stampPairs(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (p) => {
    if (!window.confirm(`Xóa cặp tem ${p.idNo} ↔ ${p.boxNo}?`)) return
    try { const r = await api.deleteStampPair(p.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Ánh xạ cặp tem</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm IDNo / BoxNo…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({})}>+ Ghép cặp tem</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Ánh xạ cặp tem (GS1 Map_StampPair) — ghép 1 tem sản phẩm (IDNo) với 1 tem hộp (BoxNo) thành một cặp để đẩy lên eTEM/ELTS. Mỗi tem sản phẩm và mỗi tem hộp chỉ được ghép một lần.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Tem sản phẩm (IDNo)</th><th>Tem hộp (BoxNo)</th><th>PIN</th><th>Môi trường</th><th>Trạng thái</th><th>Ngày tạo</th><th></th></tr></thead>
          <tbody>{rows.map(p => (
            <tr key={p.id}><td style={{ fontFamily: 'monospace' }}>{p.idNo}</td>
              <td style={{ fontFamily: 'monospace' }}>{p.boxNo}</td>
              <td style={{ fontFamily: 'monospace' }}>{p.pin || '—'}</td>
              <td>{p.networkId || '—'}</td>
              <td><Badge text={p.active ? 'Hiệu lực' : 'Ngưng'} css={p.active ? 'success' : 'secondary'} /></td>
              <td>{fmtDate(p.createdAt)}</td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(p)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(p)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={7} className="muted" style={{ padding: 20 }}>Chưa có cặp tem.</td></tr>}</tbody></table>
      </div>
      {edit && <StampPairForm row={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function StampPairForm({ row, onClose, onSaved }) {
  const [f, setF] = useState({ id: row.id || 0, idNo: row.idNo || '', boxNo: row.boxNo || '', pin: row.pin || '', networkId: row.networkId || '', remark: row.remark || '' })
  const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveStampPair(f); onSaved() } catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? 'Sửa cặp tem' : 'Ghép cặp tem'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Tem sản phẩm (IDNo) *"><input value={f.idNo} onChange={e => up('idNo', e.target.value)} placeholder="vd: P000001" /></Field>
        <Field label="Tem hộp (BoxNo) *"><input value={f.boxNo} onChange={e => up('boxNo', e.target.value)} placeholder="vd: B2601010001" /></Field></div>
      <div className="row"><Field label="PIN (mã bí mật)"><input value={f.pin} onChange={e => up('pin', e.target.value)} placeholder="vd: PIN00001" /></Field>
        <Field label="Môi trường (NetworkID)"><input value={f.networkId} onChange={e => up('networkId', e.target.value)} placeholder="vd: Manufacturer" /></Field></div>
      <Field label="Ghi chú (Remark)"><input value={f.remark} onChange={e => up('remark', e.target.value)} /></Field>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: IDNo phải có trong kho số tem; BoxNo phải có trong kho số hộp; mỗi tem chỉ được ghép một lần.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>{f.id ? 'Lưu' : 'Ghép cặp'}</button></div>
    </Modal>
  )
}

function ProductIds() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.productIds(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (p) => {
    if (!window.confirm(`Xóa định danh ${p.productID}?`)) return
    try { const r = await api.deleteProductId(p.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Định danh sản phẩm</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã / lô / người mua…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({})}>+ Thêm định danh</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Định danh sản phẩm (GS1 Prd_ProductID) — mỗi sản phẩm đã bán gắn một mã định danh duy nhất kèm lô, ngày sản xuất, số bí mật và thông tin bảo hành để tra cứu lịch sử sản phẩm.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã định danh</th><th>Quy cách</th><th>Lô</th><th>Ngày SX</th><th>Người mua</th><th>Bảo hành đến</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>{rows.map(p => (
            <tr key={p.id}><td style={{ fontFamily: 'monospace' }}>{p.productID}</td><td>{p.specCode || '—'}</td>
              <td>{p.lotNo || '—'}</td><td>{p.productionDate || '—'}</td><td>{p.buyer || '—'}</td>
              <td>{p.warrantyExpiredDate || '—'}</td>
              <td><Badge text={p.statusText} css={p.css} /></td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(p)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(p)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={8} className="muted" style={{ padding: 20 }}>Chưa có định danh sản phẩm.</td></tr>}</tbody></table>
      </div>
      {edit && <ProductIdForm row={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function ProductIdForm({ row, onClose, onSaved }) {
  const [f, setF] = useState({
    id: row.id || 0, productID: row.productID || '', specCode: row.specCode || '', productionDate: row.productionDate || '',
    lotNo: row.lotNo || '', buyDate: row.buyDate || '', secretNo: row.secretNo || '',
    warrantyStartDate: row.warrantyStartDate || '', warrantyExpiredDate: row.warrantyExpiredDate || '', warrantyDuration: row.warrantyDuration || '',
    refNo1: row.refNo1 || '', refBiz1: row.refBiz1 || '', refNo2: row.refNo2 || '', refBiz2: row.refBiz2 || '',
    refNo3: row.refNo3 || '', refBiz3: row.refBiz3 || '', buyer: row.buyer || '', networkProductIdCode: row.networkProductIdCode || '',
    status: row.status ?? 0, customField1: row.customField1 || '', customField2: row.customField2 || '',
    customField3: row.customField3 || '', customField4: row.customField4 || '', customField5: row.customField5 || '', remark: row.remark || ''
  })
  const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveProductId({ ...f, status: Number(f.status) }); onSaved() } catch (e) { setErr(e.message) }
  }
  return (
    <Modal wide title={f.id ? 'Sửa định danh sản phẩm' : 'Thêm định danh sản phẩm'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã định danh (ProductID) *"><input value={f.productID} onChange={e => up('productID', e.target.value)} placeholder="vd: PID-ST25-0001" /></Field>
        <Field label="Quy cách (SpecCode)"><input value={f.specCode} onChange={e => up('specCode', e.target.value)} placeholder="vd: 8930001001" /></Field></div>
      <div className="row"><Field label="Ngày sản xuất"><input value={f.productionDate} onChange={e => up('productionDate', e.target.value)} placeholder="yyyy-MM-dd" /></Field>
        <Field label="Số lô (LOTNo)"><input value={f.lotNo} onChange={e => up('lotNo', e.target.value)} /></Field></div>
      <div className="row"><Field label="Ngày mua (BuyDate)"><input value={f.buyDate} onChange={e => up('buyDate', e.target.value)} placeholder="yyyy-MM-dd" /></Field>
        <Field label="Số bí mật (SecretNo)"><input value={f.secretNo} onChange={e => up('secretNo', e.target.value)} /></Field></div>
      <div className="row"><Field label="Bảo hành từ"><input value={f.warrantyStartDate} onChange={e => up('warrantyStartDate', e.target.value)} placeholder="yyyy-MM-dd" /></Field>
        <Field label="Bảo hành đến"><input value={f.warrantyExpiredDate} onChange={e => up('warrantyExpiredDate', e.target.value)} placeholder="yyyy-MM-dd" /></Field>
        <Field label="Thời hạn BH"><input value={f.warrantyDuration} onChange={e => up('warrantyDuration', e.target.value)} placeholder="vd: 12 tháng" /></Field></div>
      <div className="row"><Field label="Người mua (Buyer)"><input value={f.buyer} onChange={e => up('buyer', e.target.value)} /></Field>
        <Field label="Mã định danh ngoài mạng"><input value={f.networkProductIdCode} onChange={e => up('networkProductIdCode', e.target.value)} /></Field>
        <Field label="Trạng thái"><select value={f.status} onChange={e => up('status', e.target.value)}>
          <option value={0}>OK</option><option value={1}>NG</option><option value={2}>Đang sửa chữa</option><option value={3}>Đang kiểm tra</option></select></Field></div>
      <div className="row"><Field label="Tham chiếu 1 (số)"><input value={f.refNo1} onChange={e => up('refNo1', e.target.value)} /></Field>
        <Field label="Tham chiếu 1 (nghiệp vụ)"><input value={f.refBiz1} onChange={e => up('refBiz1', e.target.value)} /></Field></div>
      <div className="row"><Field label="Tham chiếu 2 (số)"><input value={f.refNo2} onChange={e => up('refNo2', e.target.value)} /></Field>
        <Field label="Tham chiếu 2 (nghiệp vụ)"><input value={f.refBiz2} onChange={e => up('refBiz2', e.target.value)} /></Field></div>
      <div className="row"><Field label="Tham chiếu 3 (số)"><input value={f.refNo3} onChange={e => up('refNo3', e.target.value)} /></Field>
        <Field label="Tham chiếu 3 (nghiệp vụ)"><input value={f.refBiz3} onChange={e => up('refBiz3', e.target.value)} /></Field></div>
      <div className="row"><Field label="Trường mở rộng 1"><input value={f.customField1} onChange={e => up('customField1', e.target.value)} /></Field>
        <Field label="Trường mở rộng 2"><input value={f.customField2} onChange={e => up('customField2', e.target.value)} /></Field>
        <Field label="Trường mở rộng 3"><input value={f.customField3} onChange={e => up('customField3', e.target.value)} /></Field></div>
      <div className="row"><Field label="Trường mở rộng 4"><input value={f.customField4} onChange={e => up('customField4', e.target.value)} /></Field>
        <Field label="Trường mở rộng 5"><input value={f.customField5} onChange={e => up('customField5', e.target.value)} /></Field></div>
      <Field label="Ghi chú (Remark)"><input value={f.remark} onChange={e => up('remark', e.target.value)} /></Field>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: cần mã định danh sản phẩm (ProductID); mã định danh duy nhất trong tenant; trạng thái thuộc OK/NG/Đang sửa chữa/Đang kiểm tra.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>{f.id ? 'Lưu' : 'Thêm'}</button></div>
    </Modal>
  )
}

function ConfigColumnSearches() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.configColumnSearches(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (c) => {
    if (!window.confirm(`Xóa cấu hình trường ${c.coumnID}?`)) return
    try { const r = await api.deleteConfigColumnSearch(c.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Cấu hình trường tra cứu</h1><div className="sp" />
        <input style={{ maxWidth: 220 }} placeholder="Tìm mã trường / tab…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({})}>+ Thêm trường</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Cấu hình trường hiển thị khi tra cứu (GS1 Mst_ConfigColumnSearch) — "từ điển" cột hiển thị cho màn tra cứu truy xuất: gắn trường vào Tab theo loại bảng dữ liệu, quy định thứ tự và cờ hiển thị trong/ngoài Org.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã trường</th><th>Tab</th><th>Loại bảng</th><th>Môi trường</th><th className="right">Thứ tự</th><th>Mô tả</th><th>Hiển thị</th><th></th></tr></thead>
          <tbody>{rows.map(c => (
            <tr key={c.id}><td style={{ fontFamily: 'monospace' }}>{c.coumnID}</td>
              <td>{c.tabName || c.tabID}</td><td className="muted">{c.typeId}</td><td>{c.networkId || '—'}</td>
              <td className="right">{c.idxInTab}</td><td>{c.columnDesc || '—'}</td>
              <td><Badge text={c.flagView ? 'Trong Org' : 'Ẩn'} css={c.flagView ? 'success' : 'secondary'} />{' '}
                {c.flagOsOrgView && <Badge text="Ngoài Org" css="info" />}</td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(c)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(c)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={8} className="muted" style={{ padding: 20 }}>Chưa có cấu hình trường.</td></tr>}</tbody></table>
      </div>
      {edit && <ConfigColumnSearchForm row={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function ConfigColumnSearchForm({ row, onClose, onSaved }) {
  const [f, setF] = useState({
    id: row.id || 0, coumnID: row.coumnID || '', tabID: row.tabID || '', tabName: row.tabName || '',
    networkId: row.networkId || '', typeId: row.typeId || '', idxInTab: row.idxInTab ?? 0, columnDesc: row.columnDesc || '',
    flagView: row.flagView ?? true, flagOsOrgView: row.flagOsOrgView ?? false, flagShow: row.flagShow ?? true,
    esColumnId: row.esColumnId || '', eltsObjectId: row.eltsObjectId || ''
  })
  const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveConfigColumnSearch({ ...f, idxInTab: Number(f.idxInTab) }); onSaved() } catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? 'Sửa cấu hình trường' : 'Thêm cấu hình trường'} onClose={onClose}>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã trường (CoumnID) *"><input value={f.coumnID} onChange={e => up('coumnID', e.target.value)} placeholder="vd: ProductName" /></Field>
        <Field label="Mã Tab (TabID) *"><input value={f.tabID} onChange={e => up('tabID', e.target.value)} placeholder="vd: TAB_PRODUCT" /></Field></div>
      <div className="row"><Field label="Tên Tab"><input value={f.tabName} onChange={e => up('tabName', e.target.value)} placeholder="vd: Thông tin sản phẩm" /></Field>
        <Field label="Loại bảng dữ liệu (TypeID) *"><input value={f.typeId} onChange={e => up('typeId', e.target.value)} placeholder="vd: Mst_Product" /></Field></div>
      <div className="row"><Field label="Môi trường (NetworkID)"><input value={f.networkId} onChange={e => up('networkId', e.target.value)} placeholder="vd: Manufacturer" /></Field>
        <Field label="Thứ tự trong Tab"><input type="number" value={f.idxInTab} onChange={e => up('idxInTab', e.target.value)} /></Field></div>
      <Field label="Mô tả trường (ColumnDesc)"><input value={f.columnDesc} onChange={e => up('columnDesc', e.target.value)} /></Field>
      <div className="row"><Field label="Mã ES cột (ESColumnID)"><input value={f.esColumnId} onChange={e => up('esColumnId', e.target.value)} /></Field>
        <Field label="Mã ElasticSearch (ELTSObjectId)"><input value={f.eltsObjectId} onChange={e => up('eltsObjectId', e.target.value)} /></Field></div>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.flagView} onChange={e => up('flagView', e.target.checked)} /> Hiển thị cho người dùng trong Org</label>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 6 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.flagOsOrgView} onChange={e => up('flagOsOrgView', e.target.checked)} /> Hiển thị cho người dùng ngoài Org (người tiêu dùng)</label>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 6 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.flagShow} onChange={e => up('flagShow', e.target.checked)} /> Cờ hiển thị (FlagShow)</label>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: cần mã trường + mã Tab + loại bảng dữ liệu; bộ ba (CoumnID, NetworkID, TypeID) duy nhất trong tenant.</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>{f.id ? 'Lưu' : 'Thêm'}</button></div>
    </Modal>
  )
}

function ManufacturedIds() {
  const [rows, setRows] = useState([]); const [q, setQ] = useState(''); const [edit, setEdit] = useState(null); const [msg, setMsg] = useState(null)
  const load = () => api.manufacturedIds(q).then(r => setRows(r.data))
  useEffect(() => { load() }, [])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3000) }
  const del = async (m) => {
    if (!window.confirm(`Xóa bản ghi sản xuất của tem ${m.idNo}?`)) return
    try { const r = await api.deleteManufacturedId(m.id); flash(true, r.data.msg); load() } catch (e) { flash(false, e.message) }
  }
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Dãy sản xuất</h1><div className="sp" />
        <input style={{ maxWidth: 240 }} placeholder="Tìm mã tem / dãy / lô / dây chuyền…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setEdit({})}>+ Ghi nhận sản xuất</button></div>
      <Flash msg={msg} />
      <p className="muted" style={{ marginTop: 0 }}>Sản phẩm đã sản xuất (GS1 Inv_InventoryManufacturedID) — ghi nhận tem sản phẩm đã sản xuất trên dây chuyền/ca/lô: mắt xích "sản xuất" nối kho số tem với thực tế chạy máy.</p>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã tem</th><th>Dãy sản xuất</th><th>Dây chuyền</th><th>Ca</th><th>Lô</th><th>Hộp</th><th>Trạng thái</th><th></th></tr></thead>
          <tbody>{rows.map(m => (
            <tr key={m.id}><td style={{ fontFamily: 'monospace' }}>{m.idNo}</td>
              <td style={{ fontFamily: 'monospace' }}>{m.iManufacturedIDNo}</td><td>{m.lineCode || '—'}</td><td>{m.shiftCode || '—'}</td>
              <td>{m.productionLotNo || '—'}</td><td>{m.boxNo || '—'}</td>
              <td><Badge text={m.statusText} css={m.css} /></td>
              <td className="right" style={{ whiteSpace: 'nowrap' }}>
                <button className="btn ghost sm" onClick={() => setEdit(m)}>Sửa</button>{' '}
                <button className="btn gray sm" onClick={() => del(m)}>Xóa</button></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={8} className="muted" style={{ padding: 20 }}>Chưa có bản ghi sản xuất.</td></tr>}</tbody></table>
      </div>
      {edit && <ManufacturedIdForm row={edit} onClose={() => setEdit(null)} onSaved={() => { setEdit(null); load() }} />}
    </>
  )
}

function ManufacturedIdForm({ row, onClose, onSaved }) {
  const [f, setF] = useState({
    id: row.id || 0, idNo: row.idNo || '', iManufacturedIDNo: row.iManufacturedIDNo || '', networkId: row.networkId || '',
    boxNo: row.boxNo || '', lineCode: row.lineCode || '', lineRootCode: row.lineRootCode || '', shiftCode: row.shiftCode || '',
    productionLotNo: row.productionLotNo || '', refNoLine: row.refNoLine || '', productCode: row.productCode || '', invCode: row.invCode || '',
    mobileIndex: row.mobileIndex ?? 0, flagMap: row.flagMap ?? false, status: row.status ?? 0, remark: row.remark || ''
  })
  const [err, setErr] = useState('')
  const up = (k, v) => setF({ ...f, [k]: v })
  const save = async () => {
    try { await api.saveManufacturedId({ ...f, mobileIndex: Number(f.mobileIndex), status: Number(f.status) }); onSaved() } catch (e) { setErr(e.message) }
  }
  return (
    <Modal title={f.id ? 'Sửa bản ghi sản xuất' : 'Ghi nhận sản phẩm đã sản xuất'} onClose={onClose} wide>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Mã tem (IDNo) *"><input value={f.idNo} onChange={e => up('idNo', e.target.value)} placeholder="vd: P000001" /></Field>
        <Field label="Mã dãy sản xuất (IManufacturedIDNo) *"><input value={f.iManufacturedIDNo} onChange={e => up('iManufacturedIDNo', e.target.value)} placeholder="vd: MFG2601010001" /></Field></div>
      <div className="row"><Field label="Dây chuyền (LineCode)"><input value={f.lineCode} onChange={e => up('lineCode', e.target.value)} placeholder="vd: LINE-A1" /></Field>
        <Field label="Dây chuyền gốc (LineRootCode)"><input value={f.lineRootCode} onChange={e => up('lineRootCode', e.target.value)} placeholder="vd: LINE-A" /></Field></div>
      <div className="row"><Field label="Ca sản xuất (ShiftCode)"><input value={f.shiftCode} onChange={e => up('shiftCode', e.target.value)} placeholder="vd: CA1" /></Field>
        <Field label="Lô sản xuất (ProductionLotNo)"><input value={f.productionLotNo} onChange={e => up('productionLotNo', e.target.value)} placeholder="vd: L2026-001" /></Field></div>
      <div className="row"><Field label="Mã hộp (BoxNo)"><input value={f.boxNo} onChange={e => up('boxNo', e.target.value)} /></Field>
        <Field label="Mã đối chiếu dãy (RefNoLine)"><input value={f.refNoLine} onChange={e => up('refNoLine', e.target.value)} /></Field></div>
      <div className="row"><Field label="Mã sản phẩm (ProductCode)"><input value={f.productCode} onChange={e => up('productCode', e.target.value)} /></Field>
        <Field label="Mã kho (InvCode)"><input value={f.invCode} onChange={e => up('invCode', e.target.value)} /></Field></div>
      <div className="row"><Field label="Môi trường (NetworkID)"><input value={f.networkId} onChange={e => up('networkId', e.target.value)} placeholder="vd: Manufacturer" /></Field>
        <Field label="Số thứ tự máy quét (MobileIndex)"><input type="number" value={f.mobileIndex} onChange={e => up('mobileIndex', e.target.value)} /></Field></div>
      <Field label="Trạng thái"><select value={f.status} onChange={e => up('status', e.target.value)}>
        <option value={0}>Đang sản xuất</option><option value={1}>Đã hoàn tất</option></select></Field>
      <label style={{ display: 'flex', alignItems: 'center', gap: 8, marginTop: 10 }}>
        <input type="checkbox" style={{ width: 'auto' }} checked={f.flagMap} onChange={e => up('flagMap', e.target.checked)} /> Đã ghép thông tin sản phẩm (FlagMap)</label>
      <Field label="Ghi chú (Remark)"><input value={f.remark} onChange={e => up('remark', e.target.value)} /></Field>
      <p className="muted" style={{ fontSize: 12, marginTop: 10 }}>Quy tắc: cần mã tem + mã dãy sản xuất; tem phải tồn tại trong kho số tem; mỗi tem chỉ được ghi nhận sản xuất 1 lần (chống trùng).</p>
      <div style={{ marginTop: 12 }}><button className="btn" onClick={save}>{f.id ? 'Lưu' : 'Ghi nhận'}</button></div>
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
        <Route path="data-types" element={<DataTypes />} />
        <Route path="glns" element={<Glns />} />
        <Route path="farms" element={<Farms />} />
        <Route path="market-areas" element={<MarketAreas />} />
        <Route path="org-glns" element={<OrgGlns />} />
        <Route path="templates" element={<Templates />} />
        <Route path="tpl-view-events" element={<TplViewEvents />} />
        <Route path="records" element={<Records />} />
        <Route path="stamps" element={<Stamps />} />
        <Route path="boxes" element={<Boxes />} />
        <Route path="cartons" element={<Cartons />} />
        <Route path="que-syncs" element={<QueSyncs />} />
        <Route path="master-datas" element={<MasterDatas />} />
        <Route path="network-orgs" element={<NetworkOrgs />} />
        <Route path="secrets" element={<Secrets />} />
        <Route path="stamp-pairs" element={<StampPairs />} />
        <Route path="product-ids" element={<ProductIds />} />
        <Route path="config-column-searches" element={<ConfigColumnSearches />} />
        <Route path="manufactured-ids" element={<ManufacturedIds />} />
      </Route>
    </Routes>
  )
}
