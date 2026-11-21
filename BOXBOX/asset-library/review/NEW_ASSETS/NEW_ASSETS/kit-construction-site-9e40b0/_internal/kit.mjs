// Polyfork kit.mjs. Its API is in API.md beside this file: you do not need to read this code.
// The commented source is https://polyfork.dev/cdn/kit.mjs
import * as THREE from 'three';

const DEG = Math.PI / 180;
const SCATTER_OK = /(^|-)(tree|trees|bush|shrub|pine|fir|spruce|cedar|conifer|rowan|birch|oak|maple|willow|aspen|sapling|palm|cactus|fern|rock|rocks|boulder|stone|stump|log|snag|grass|reed|flower|flowers|moss|mushroom|driftwood|larch|juniper|yew|alder|beech|elm|hemlock|cypress|poplar|redwood|sequoia|bamboo|heather|gorse|bracken|tussock|thistle|lichen|scree|talus)(-|$)/;
const SCATTER_NOT = /(^|-)(cabin|hut|house|home|cottage|shed|barn|tent|camp|bridge|fence|wall|gate|sign|post|bench|table|chair|dock|tower|lamp|light|run|row|tile|path|road|mill|well|cart|wagon|hoop|rack|station|pylon|shelter|hedge|grave|angel|statue|monument|headstone|tomb|crypt|cross|shrine|altar|obelisk|urn|plinth|pedestal|fountain|arch|vendor|seller|stand|stall|cairn|pile|stack|woodpile)(-|$)/;
const SCATTER_CLASS_OK = new Set(['prop']);
const scatterable = (k, parts = null) => {
  const cls = parts?.[k]?.class;
  if (cls && !SCATTER_CLASS_OK.has(cls)) return false;
  return SCATTER_OK.test(k) && !SCATTER_NOT.test(k);
};
const HARD = /(^|-)(rock|rocks|boulder|stone|stump|log|snag|driftwood)(-|$)/;

export async function buildKit({ base, kit, folder = null, params = {}, keep = [], only = null, skip = [],
                                 merge = true, rigs = false, loose = false, city = null, setting = false, wind = false, people = 'idle', ground = 'auto', night = true, onProgress = null,
                                 edition = 'flat', data: given = null, moduleUrl = null, terrainParams = {} } = {}) {
  let data;
  if (given) {
    data = given;
    if (base && !base.endsWith('/')) base += '/';
  } else if (folder) {
    base = new URL(folder.endsWith('/') ? folder : folder + '/', document.baseURI).href;
    const res = await fetch(`${base}LAYOUT.json`);
    if (!res.ok) throw new Error(`kit.mjs: no LAYOUT.json in ${folder} (HTTP ${res.status}). Unzip the kit download there first.`);
    const layout = (await res.json()).filter((r) => r && r.part);
    const about = await fetch(`${base}kit.json`).then((r) => (r.ok ? r.json() : {})).catch(() => ({}));
    const parts = {};
    for (const r of layout) {
      if (r.part === '_terrain') continue;
      parts[r.part] = { id: r.part, title: r.part };
      if (about.classes?.[r.part]) parts[r.part].class = about.classes[r.part];
    }
    data = { kit: kit || about.kit || 'kit', title: about.title || kit || 'kit', grid_m: about.grid_m ?? null, parts, layout,
             ground: about.ground ?? null, night: about.night ?? [], interior: !!about.interior,
             terrain: about.terrain !== false && layout.some((r) => r.part === '_terrain') ? 'terrain.mjs' : null };
  } else {
    if (!base || !kit) {
      throw new Error('kit.mjs: buildKit({ base, kit }) needs both. `base` is the cdn_base who_am_i '
        + 'returns (https://polyfork.dev/c/<token>/) and `kit` is a kit id from list_kits. '
        + 'Or buildKit({ folder }) for an unzipped kit download.');
    }
    if (!base.endsWith('/')) base += '/';
    const res = await fetch(`${base}kit-${kit}-layout.json`);
    if (!res.ok) {
      const why = res.status === 404 ? 'this kit has nothing buildKit can build yet (no composition and no ground program), or there is no such kit. '
        + 'get_kit says which; its published parts still load one by one with createAsset().' : (await res.text()).slice(0, 240);
      throw new Error(`kit.mjs: HTTP ${res.status} for the layout of ${kit}: ${why}`);
    }
    data = await res.json();
  }

  const wanted = (k) => (!only || only.includes(k)) && !skip.includes(k);
  const groundRow = city ? null : data.layout.find((r) => r.part === '_terrain' && wanted('_terrain'));   // a generated town is flat
  let rows = setting ? [] : data.layout.filter((r) => r.part !== '_terrain' && wanted(r.part));
  const keys = [...new Set(rows.map((r) => r.part))];
  const missing = keys.filter((k) => !data.parts[k]);
  const have = keys.filter((k) => data.parts[k]);

  if (setting) {
    for (const k of Object.keys(data.parts)) {
      if (wanted(k) && scatterable(k, data.parts) && !have.includes(k)) have.push(k);
    }
  }
  const mods = {}, hdKeys = new Set();   // a module namespace is frozen: what loaded as HD is kept here
  let loaded = 0;
  await Promise.all(have.map(async (k) => {
    const id = data.parts[k].id, hd = edition === 'hd' && data.parts[k].hd;
    const url = (ed) => moduleUrl ? moduleUrl(id, ed) : `${base}${id}${ed === 'hd' ? '-hd' : ''}.mjs`;
    try { mods[k] = await import(/* @vite-ignore */ url(hd ? 'hd' : 'flat')); if (hd) hdKeys.add(k); }
    catch (e) {
      if (hd) { try { mods[k] = await import(/* @vite-ignore */ url('flat')); } catch (e2) { if (!folder) throw e2; missing.push(k); } }
      else if (!folder) throw e; else missing.push(k);     // a folder may predate a part; the CDN may not
    }
    onProgress?.(++loaded, have.length, k);
  }));

  const knobsFor = (k, row) => {
    const declared = mods[k].params || {};
    const own = {};
    for (const [name, v] of Object.entries(params)) if (name in declared) own[name] = v;
    return { ...(row.params || {}), ...own };
  };

  const toPair = (q, i) => ({
    i, from: new THREE.Color(q.from), hex: q.to, glow: new THREE.Color(q.to).multiplyScalar(q.intensity ?? 1),
    parts: q.parts ? new Set(q.parts) : null, inst: q.inst || null,
    lamp: q.light === true || (q.light !== false && (q.intensity ?? 1) >= 0.9),
  });
  const pairs = (night && Array.isArray(data.night) ? data.night : []).map(toPair);
  let extraPairs = 0;
  const ownCache = new Map();
  const modulePairs = (k, p) => {
    if (!night) return [];
    const m = mods[k], name = Object.keys(m.params || {}).find((n) => /^colou?rway$/i.test(n)), way = m.presets?.[p[name] ?? m.params?.[name]?.default];
    const id = k + '|' + JSON.stringify(Object.keys(m.night || {}).map((z) => p[z] ?? way?.[z] ?? null));
    if (!ownCache.has(id)) ownCache.set(id, Object.entries(m.night || {}).map(([zone, n]) => {
      const from = p[zone] ?? way?.[zone] ?? m.params?.[zone]?.default;
      return from == null ? null : toPair({ from, to: n.color, intensity: n.intensity ?? 1, light: n.light }, 200000 + extraPairs++);
    }).filter(Boolean));
    return ownCache.get(id);
  };
  const pairsFor = (k, ord) => pairs.filter((q) => !q.parts || (q.parts.has(k) && (!q.inst?.[k] || q.inst[k].includes(ord))));
  const glowMats = new Map();
  const glowFor = (m, q) => {
    const hooked = Object.hasOwn(m, 'onBeforeCompile') || Object.hasOwn(m, 'onBeforeRender');
    const id = [hooked ? m.uuid : '', m.type, m.flatShading, m.transparent, m.opacity, m.side, m.metalness, m.roughness, m.color?.getHexString?.(), q.i].join('|');
    let g = glowMats.get(id);
    if (!g) {
      g = m.clone();
      for (const h of ['onBeforeCompile', 'onBeforeRender', 'customProgramCacheKey']) if (Object.hasOwn(m, h)) g[h] = m[h];
      g.emissive = q.glow.clone(); g.emissiveIntensity = 0;
      g.userData.glow = true;          // merge.mjs and stage.mjs: a lit face casts no shadow, which also halves what it costs to draw
      glowMats.set(id, g);
    }
    return g;
  };
  const lightUp = (root, usable) => {
    if (!usable.length) return [];
    root.updateMatrixWorld(true);
    const inv = new THREE.Matrix4().copy(root.matrixWorld).invert(), v = new THREE.Vector3(), lamps = new Map(), done = new Map();
    const meshes = [];
    root.traverse((n) => { if (n.isMesh && !n.isSkinnedMesh && !Array.isArray(n.material) && n.material?.emissive && n.geometry?.attributes?.color) meshes.push(n); });
    for (const mesh of meshes) {
      let cut = done.get(mesh.geometry);
      if (cut === undefined) {
        const src = mesh.geometry.index ? mesh.geometry.toNonIndexed() : mesh.geometry;
        const col = src.attributes.color, tris = src.attributes.position.count / 3, which = new Int16Array(tris).fill(-1);
        let any = false;
        for (let t = 0; t < tris; t++) {
          for (let u = 0; u < usable.length && which[t] < 0; u++) {
            const f = usable[u].from; let ok = true;
            for (let c = 0; c < 3 && ok; c++) { const i = t * 3 + c; ok = Math.abs(col.getX(i) - f.r) < 0.004 && Math.abs(col.getY(i) - f.g) < 0.004 && Math.abs(col.getZ(i) - f.b) < 0.004; }
            if (ok) { which[t] = u; any = true; }
          }
        }
        cut = null;
        if (any) {
          const carve = (u) => {
            const pick = []; for (let t = 0; t < tris; t++) if (which[t] === u) pick.push(t);
            if (!pick.length) return null;
            const g = new THREE.BufferGeometry();
            for (const [name, a] of Object.entries(src.attributes)) {
              const n = a.itemSize, out = new a.array.constructor(pick.length * 3 * n);
              pick.forEach((t, j) => out.set(a.array.subarray(t * 3 * n, (t + 1) * 3 * n), j * 3 * n));
              g.setAttribute(name, new THREE.BufferAttribute(out, n, a.normalized));
            }
            return g;
          };
          cut = { rest: carve(-1), lit: usable.map((q, u) => ({ q, g: carve(u) })).filter((x) => x.g) };
        }
        done.set(mesh.geometry, cut);
      }
      if (!cut) continue;
      const base = mesh.material, lits = cut.lit.slice();
      if (cut.rest) mesh.geometry = cut.rest; else { mesh.geometry = lits[0].g; mesh.material = glowFor(base, lits[0].q); }
      for (const [n, { q, g }] of lits.entries()) {
        if (cut.rest || n > 0) {
          const twin = new THREE.Mesh(g, glowFor(base, q));
          twin.name = `${mesh.name || 'mesh'}:lit`; twin.castShadow = false; twin.receiveShadow = mesh.receiveShadow;
          mesh.add(twin);
        }
        if (!q.lamp) continue;
        const pos = g.attributes.position;
        for (let i = 0; i < pos.count; i += 3) {
          v.fromBufferAttribute(pos, i).applyMatrix4(mesh.matrixWorld).applyMatrix4(inv);
          const id = `${q.i}|${Math.round(v.x / 2)},${Math.round(v.y / 2)},${Math.round(v.z / 2)}`;
          const c = lamps.get(id) || { hex: q.hex, x: 0, y: 0, z: 0, n: 0, x0: Infinity, x1: -Infinity, z0: Infinity, z1: -Infinity };
          c.x += v.x; c.y += v.y; c.z += v.z; c.n++; c.x0 = Math.min(c.x0, v.x); c.x1 = Math.max(c.x1, v.x); c.z0 = Math.min(c.z0, v.z); c.z1 = Math.max(c.z1, v.z);
          lamps.set(id, c);
        }
      }
    }
    const perPair = new Map();
    for (const [id] of lamps) { const q = id.split('|')[0]; perPair.set(q, (perPair.get(q) || 0) + 1); }
    return [...lamps.entries()].filter(([id, c]) => perPair.get(id.split('|')[0]) <= 4 && Math.max(c.x1 - c.x0, c.z1 - c.z0) < 2.2)
      .map(([, c]) => ({ hex: c.hex, x: c.x / c.n, y: c.y / c.n, z: c.z / c.n }));
  };

  const engine = edition === 'hd' && Object.values(data.parts).some((q) => q.surface)
    ? await import(new URL('materials.mjs', import.meta.url).href).catch(() => null) : null;

  const cache = new Map();
  const cloneable = (o) => { let ok = !o.userData?.tick; o.traverse((n) => { if (n.isSkinnedMesh || n.userData?.tick) ok = false; }); return ok; };
  const make = (k, row, ord = -1) => {
    const p = knobsFor(k, row);
    const usable = row.ownNight ? modulePairs(k, p) : pairsFor(k, ord);
    const sig = k + JSON.stringify(p) + '|' + usable.map((q) => q.i).join(',');
    const hit = cache.get(sig);
    if (hit) return hit.clone(true);
    const o = mods[k].createAsset(p);
    const surf = edition === 'hd' && !hdKeys.has(k) && data.parts[k].surface;
    if (surf && engine) {
      const spec = mods[k].params || {};
      const zones = Object.fromEntries(Object.entries(spec).filter(([, sp]) => sp && sp.type === 'color').map(([z, sp]) => [z, p[z] ?? sp.default]));
      try { if (!engine.dress(o, surf)) engine.dressByAlbedo(o, zones, surf, { smooth: 'auto', blend: 'auto' }); }
      catch (e) { console.warn('kit.mjs: HD surface failed on', k, e); }
    }
    const mats = (hdKeys.has(k) && data.parts[k].materials) || surf;
    if (mats) o.traverse((n) => { if (!n.isMesh) return; for (const m of [].concat(n.material)) { const sp = m && mats[m.name]; if (sp?.kind) m.userData.pf = { kind: sp.kind, finish: sp.finish ?? null }; } });
    const lamps = lightUp(o, usable);
    if (lamps.length) o.userData.lamps = lamps;
    o.userData.sig = sig;            // survives a clone: what `loose` instances by
    if (cloneable(o)) cache.set(sig, o);
    return o;
  };

  let terrain = null, terrainFoot = 0, terrainColor = null;
  if (groundRow && data.terrain) {
    const mod = await import(/* @vite-ignore */ `${base}${data.terrain}`);
    const tp = { ...(groundRow.params || {}), ...(terrainParams || {}) };
    if (setting && mod.params?.span) tp.span = Math.min(mod.params.span.max ?? 4, Math.max(+tp.span || mod.params.span.default || 1, 2));
    terrain = mod.createAsset(tp);
    if (['x', 'y', 'z'].some((k) => groundRow[k] != null)) terrain.position.set(groundRow.x || 0, groundRow.y || 0, groundRow.z || 0);
    if (groundRow.ry) terrain.rotation.y = groundRow.ry * DEG;
    terrain.name = 'terrain';
    if (ground === 'smooth' || ground === 'auto') {
      const { softenTerrain } = await import(new URL('stage.mjs', import.meta.url).href);
      softenTerrain(terrain, { normals: ground === 'auto' ? 'auto' : true });
    }
    if (edition === 'hd') {
      const { dressTerrain } = await import(new URL('terrain-hd.mjs', import.meta.url).href);
      try { dressTerrain(terrain, mod.params || {}); } catch (e) { console.warn('kit.mjs: HD ground failed, keeping the flat one:', e); }
    }
    terrain.updateMatrixWorld(true);
    terrainFoot = new THREE.Box3().setFromObject(terrain).min.y;
    const bins = new Map();
    terrain.traverse((o) => {
      const col = o.isMesh && o.geometry?.attributes?.color, pos = col && o.geometry.attributes.position;
      if (!col || !pos || o.geometry.index || o.material?.transparent) return;
      const F = pos.count / 3, stepF = Math.max(1, Math.floor(F / 1500));
      for (let f = 0; f < F; f += stepF) {
        const i = f * 3, ny = (pos.getZ(i + 1) - pos.getZ(i)) * (pos.getX(i + 2) - pos.getX(i)) - (pos.getX(i + 1) - pos.getX(i)) * (pos.getZ(i + 2) - pos.getZ(i));
        if (ny <= 0) continue;
        const key = [col.getX(i), col.getY(i), col.getZ(i)].map((v) => Math.round(v * 10)).join(',');
        const e = bins.get(key) || { n: 0, r: 0, g: 0, b: 0 }; e.n++; e.r += col.getX(i); e.g += col.getY(i); e.b += col.getZ(i); bins.set(key, e);
      }
    });
    const top = [...bins.values()].sort((p, q) => q.n - p.n)[0];
    if (top) terrainColor = '#' + new THREE.Color(top.r / top.n, top.g / top.n, top.b / top.n).getHexString();
  }
  const tray = new THREE.Raycaster(), tdown = new THREE.Vector3(0, -1, 0);
  const heightAt = (x, z) => { if (!terrain) return 0; tray.set(new THREE.Vector3(x, 10000, z), tdown); const h = tray.intersectObject(terrain, true)[0]; return h ? h.point.y : terrainFoot; };

  if (setting) {
    if (!terrain) throw new Error('kit.mjs: setting: true needs a kit with a terrain program, and this one has none. Build the place itself (drop `setting`).');
    const tb = new THREE.Box3().setFromObject(terrain), hx = (tb.max.x - tb.min.x) / 2, hz = (tb.max.z - tb.min.z) / 2, cx = (tb.min.x + tb.max.x) / 2, cz = (tb.min.z + tb.max.z) / 2;
    const pool = have.filter((k) => mods[k] && scatterable(k, data.parts)), green = pool.filter((k) => !HARD.test(k)), hard = pool.filter((k) => HARD.test(k));
    let r = 17 >>> 0; const rnd = () => ((r = (r * 1664525 + 1013904223) >>> 0) / 4294967296);
    const want = Math.min(2500, Math.round(hx * hz * 4 / 18 * (setting === true ? 1 : +setting || 1)));       // about one piece per 18 m2
    for (let i = 0, tries = 0; pool.length && i < want && tries < want * 8; tries++) {
      const x = cx + (rnd() * 2 - 1) * hx * 0.96, z = cz + (rnd() * 2 - 1) * hz * 0.96, d = Math.hypot((x - cx) / hx, (z - cz) / hz);
      if (rnd() > 0.25 + 0.75 * d) continue;
      const y = heightAt(x, z), slope = Math.abs(heightAt(x + 1, z) - y) + Math.abs(heightAt(x, z + 1) - y);
      if (slope > 0.9) continue;
      const from = green.length && (!hard.length || rnd() < 0.85) ? green : (hard.length ? hard : pool), k = from[Math.floor(rnd() * from.length)];
      rows.push({ part: k, x: +x.toFixed(2), y: +y.toFixed(2), z: +z.toFixed(2), ry: +(rnd() * 360).toFixed(1), s: +(0.8 + rnd() * 0.45).toFixed(2) }); i++;
    }
  }

  let town = null;
  if (city) {
    const { cityLayout } = await import(new URL('city.mjs', import.meta.url).href);
    const person = (o) => { const b = new Set(); o.traverse((n) => { if (n.isBone) b.add(n.name); }); return ['Hips', 'Spine2', 'LeftForeArm', 'RightUpLeg'].every((n) => b.has(n)); };
    town = cityLayout({ rows: rows.filter((r) => mods[r.part]), make, isHuman: person,
      hasWheels: (k) => Object.keys(mods[k]?.rig || {}).some((n) => /wheel/i.test(n)),
      colourways: (k) => { const name = Object.keys(mods[k]?.params || {}).find((n) => /^colou?rway$/i.test(n)); return { name, list: name ? Object.keys(mods[k].presets || {}) : [] }; },
      city: { ...(city === true ? {} : city), turf: !!data.ground?.color } });
    rows = town.rows;
  }

  const staging = new THREE.Group();
  const placed = [];
  const nth = {};              // a lit pair can belong to "the second cottage": placements count per part, in layout order
  for (const row of rows) {
    const k = row.part;
    const ord = row.ord ?? (nth[k] = (nth[k] ?? -1) + 1);      // a generated row remembers which composed placement it copies
    if (!mods[k]) continue;
    const o = make(k, row, ord);
    o.position.set(row.x || 0, row.y || 0, row.z || 0);
    o.rotation.set((row.rx || 0) * DEG, (row.ry || 0) * DEG, (row.rz || 0) * DEG);   // XYZ, as the editor that wrote it
    if (row.s !== undefined) Array.isArray(row.s) ? o.scale.set(row.s[0], row.s[1], row.s[2]) : o.scale.setScalar(row.s);
    o.name = o.name || k;
    o.userData.part = k;
    o.userData.assetId = data.parts[k].id;
    o.userData.rig = o.userData.rig || mods[k].rig || {};
    o.userData.detach = o.userData.detach || mods[k].detach || [];
    staging.add(o);
    placed.push({ key: k, id: data.parts[k].id, object: o, row });
  }
  staging.updateMatrixWorld(true);

  const hullOf = (pts) => {
    if (pts.length < 3) return null;
    pts.sort((p, q) => p[0] - q[0] || p[1] - q[1]);
    const cross = (o, a, b) => (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0]), lo = [], up = [];
    for (const p of pts) { while (lo.length >= 2 && cross(lo[lo.length - 2], lo[lo.length - 1], p) <= 0) lo.pop(); lo.push(p); }
    for (let i = pts.length - 1; i >= 0; i--) { const p = pts[i]; while (up.length >= 2 && cross(up[up.length - 2], up[up.length - 1], p) <= 0) up.pop(); up.push(p); }
    let h = lo.slice(0, -1).concat(up.slice(0, -1));
    while (h.length > 16) { let worst = 0, least = Infinity; for (let i = 0; i < h.length; i++) { const a = h[(i + h.length - 1) % h.length], b = h[i], c = h[(i + 1) % h.length], area = Math.abs(cross(a, b, c)); if (area < least) { least = area; worst = i; } } h.splice(worst, 1); }
    return h.length >= 3 ? h : null;
  };
  const lowBox = (object) => {
    const pts = []; object.userData.__slab = pts;
    const out = new THREE.Box3(), a = new THREE.Vector3(), b = new THREE.Vector3(), c = new THREE.Vector3();
    const y0 = object.position.y + 0.25, y1 = object.position.y + 2.1;
    let skin = false;
    object.traverse((n) => {
      if (n.isSkinnedMesh) { skin = true; return; }
      const pos = n.isMesh && n.geometry?.attributes?.position;
      if (!pos) return;
      const idx = n.geometry.index, tris = (idx ? idx.count : pos.count) / 3, m = n.matrixWorld;
      for (let t = 0; t < tris; t++) {
        a.fromBufferAttribute(pos, idx ? idx.getX(t * 3) : t * 3).applyMatrix4(m);
        b.fromBufferAttribute(pos, idx ? idx.getX(t * 3 + 1) : t * 3 + 1).applyMatrix4(m);
        c.fromBufferAttribute(pos, idx ? idx.getX(t * 3 + 2) : t * 3 + 2).applyMatrix4(m);
        if (Math.max(a.y, b.y, c.y) < y0 || Math.min(a.y, b.y, c.y) > y1) continue;
        out.expandByPoint(a); out.expandByPoint(b); out.expandByPoint(c);
        if (pts.length < 60000) pts.push([a.x, a.z], [b.x, b.z], [c.x, c.z]);
      }
    });
    return skin ? null : out;
  };
  let boxCache = null;
  const measure = () => boxCache || (boxCache = placed.map((p) => {
    const box = new THREE.Box3().setFromObject(p.object), low = lowBox(p.object);
    if (low && !low.isEmpty()) { low.min.y = box.min.y; low.max.y = box.max.y; }
    const lowFinal = low === null ? box : (low.isEmpty() ? null : low);
    const slab = p.object.userData.__slab || []; delete p.object.userData.__slab;
    const hull = !lowFinal ? null : (low === null || slab.length < 3 ? [[box.min.x, box.min.z], [box.max.x, box.min.z], [box.max.x, box.max.z], [box.min.x, box.max.z]] : hullOf(slab));
    return { key: p.key, id: p.id, box, low: p.row?.path ? null : lowFinal, hull: p.row?.path ? null : hull, loose: false };
  }));

  const group = new THREE.Group();
  group.name = `kit:${data.kit}`;
  const ticks = (o) => { let yes = false; o.traverse((n) => { if (typeof n.userData?.tick === 'function') yes = true; }); return yes; };
  const skinned = (o) => { let yes = false; o.traverse((n) => { if (n.isSkinnedMesh) yes = true; }); return yes; };
  const human = (o) => { const b = new Set(); o.traverse((n) => { if (n.isBone) b.add(n.name); }); return ['Hips', 'Spine2', 'LeftForeArm', 'RightUpLeg'].every((n) => b.has(n)); };
  if (people === 'none') {
    for (let i = placed.length - 1; i >= 0; i--) if (human(placed[i].object)) placed.splice(i, 1);
  }
  const stays = (p) => !merge || keep.includes(p.key) || skinned(p.object) || (wind && ticks(p.object));
  const ROOTED = /rock|boulder|stone|lantern|post-?box|mail|hydrant|vending|machine|offering|shrine|statue|fountain|well|stump|tree|bush|hedge|fence|wall|gate|rail|pole|lamp|light|meter|air-con|curtain|flag|banner|bollard|tile|pond|step|stair|crop|plant|flower|vine|heap|signpost|(street|road|traffic|stop)-sign|scarecrow/;
  const looseProps = [];
  if (merge && loose) {
    const boxes = measure();
    const groups = new Map();
    placed.forEach((p, i) => {
      const b = boxes[i], s = b.box.getSize(new THREE.Vector3());
      if (stays(p) || ticks(p.object) || ROOTED.test(p.key) || !b.low) return;
      if (Math.max(s.x, s.z) > 2.2 || s.y > 1.9 || s.y < 0.25 || s.x * s.y * s.z > 1.6) return;
      if (b.box.min.y - p.object.position.y > 0.15) return;                  // hangs on a wall, or from something
      b.loose = true;
      const sig = p.object.userData.sig || p.key;
      (groups.get(sig) || groups.set(sig, []).get(sig)).push({ p, s });
    });
    const M = new THREE.Matrix4(), I = new THREE.Matrix4();
    const { mergeAssets } = await import(new URL('merge.mjs', import.meta.url).href);
    for (const list of groups.values()) {
      const twin = list[0].p.object.clone(true);
      twin.position.set(0, 0, 0); twin.rotation.set(0, 0, 0); twin.scale.set(1, 1, 1); twin.updateMatrixWorld(true);
      const { merged } = mergeAssets([{ object: twin }], { keepDynamic: false });
      const meshes = (merged ? (merged.isMesh ? [merged] : merged.children) : []).map((mesh) => ({ mesh, local: I }));
      const insts = meshes.map(({ mesh, local }) => {
        const im = new THREE.InstancedMesh(mesh.geometry, mesh.material, list.length);
        im.instanceMatrix.setUsage(THREE.DynamicDrawUsage); im.frustumCulled = false; im.name = `loose:${list[0].p.key}`;
        im.castShadow = !mesh.material?.userData?.glow; im.receiveShadow = true;
        group.add(im);
        return { im, local };
      });
      list.forEach(({ p, s }, index) => {
        const o = p.object;
        const prop = { key: p.key, object: o, position: o.position, quaternion: o.quaternion, home: o.position.clone(),
          radius: Math.max(0.18, (s.x + s.z) / 4), height: s.y, mass: Math.min(200, Math.max(3, s.x * s.y * s.z * 150)),
          apply() { o.updateMatrix(); for (const { im, local } of insts) { im.setMatrixAt(index, M.multiplyMatrices(o.matrix, local)); im.instanceMatrix.needsUpdate = true; } } };
        prop.apply(); looseProps.push(prop);
      });
    }
  }
  const isLoose = new Set(looseProps.map((q) => q.object));
  if (merge) {
    measure();
    const { mergeAssets } = await import(new URL('merge.mjs', import.meta.url).href);
    const live = placed.filter(stays);
    const scenery = placed.filter((p) => !live.includes(p) && !isLoose.has(p.object));
    const { merged, dynamic } = mergeAssets(scenery.map((p) => ({ object: p.object, rig: p.object.userData.rig })), { keepDynamic: !!rigs });
    if (merged) { merged.name = 'scenery'; group.add(merged); }
    for (const d of dynamic) group.add(d);
    for (const p of live) group.add(p.object);
  } else {
    for (const p of placed) group.add(p.object);
  }
  const ticking = [];
  for (const p of placed) if (stays(p)) p.object.traverse((n) => { if (typeof n.userData?.tick === 'function') ticking.push(n); });

  const folk = placed.filter((p) => human(p.object));
  if (people === 'idle' && folk.length) {
    const { animate } = await import(new URL('clips.mjs', import.meta.url).href);
    await Promise.all(folk.map(async (p) => {
      try {
        const own = !p.row?.path && typeof p.row?.anim === 'string' && p.row.anim ? p.row.anim : null;
        p.anim = own
          ? await animate(p.object, { clips: [own], inPlace: false }).catch(() => animate(p.object, { clips: ['Idle'] }))
          : await animate(p.object, { clips: p.row?.path ? ['Idle', 'Walk'] : ['Idle'] });
        const first = p.anim.actions[own] || p.anim.actions.Idle;
        if (own && p.anim.actions[own]) p.anim.play(own, { fade: 0 });
        first.time = Math.random() * first.getClip().duration;
        if (p.row?.path?.length > 1 && p.anim.actions.Walk) {
          const path = p.row.path; let best = 0, bd = Infinity;
          path.forEach((q, i) => { const d = Math.hypot(q[0] - p.object.position.x, q[1] - p.object.position.z); if (d < bd) { bd = d; best = i; } });
          p.walk = { path, next: (best + 1) % path.length, speed: 1.05 + Math.random() * 0.5 };
          p.anim.play('Walk'); p.anim.actions.Walk.time = Math.random();
        }
      } catch { /* a skeleton the clips do not fit stays as it was built */ }
    }));
  }
  let lastT = null;

  if (terrain) group.add(terrain);
  const ray = new THREE.Raycaster();
  const down = new THREE.Vector3(0, -1, 0);
  const groundAt = (x, z) => {
    if (!terrain) return 0;
    group.updateWorldMatrix(true, true);
    const from = group.localToWorld(new THREE.Vector3(x, 10000, z));
    ray.set(from, down);
    const hit = ray.intersectObject(terrain, true)[0];
    return hit ? group.worldToLocal(hit.point).y : terrainFoot;     // beyond the island: the plain at its foot
  };

  const bounds = new THREE.Box3();
  for (const b of measure()) bounds.union(b.box);
  if (bounds.isEmpty() && terrain) bounds.setFromObject(terrain);          // nothing placed: the ground is the place
  if (town?.road) { group.add(town.road); for (const r of town.roadRects) { bounds.expandByPoint(new THREE.Vector3(r.x0, 0, r.z0)); bounds.expandByPoint(new THREE.Vector3(r.x1, 0, r.z1)); } }
  const centre = bounds.getCenter(new THREE.Vector3());
  let surfaceCells = null;
  const cells = () => {
    if (surfaceCells) return surfaceCells;
    surfaceCells = new Map();
    for (const b of measure()) {
      if (b.box.max.y - b.box.min.y > 0.35 || b.box.max.y > 0.6) continue;      // flat, and on the ground
      for (let cx = Math.floor(b.box.min.x); cx <= Math.floor(b.box.max.x - 1e-6); cx++) {
        for (let cz = Math.floor(b.box.min.z); cz <= Math.floor(b.box.max.z - 1e-6); cz++) {
          const id = cx + ',' + cz, was = surfaceCells.get(id);
          if (!was || b.box.max.y >= was.y) surfaceCells.set(id, { key: b.key, y: b.box.max.y });
        }
      }
    }
    for (const r of town?.roadRects || []) for (let cx = Math.floor(r.x0); cx < r.x1; cx++) for (let cz = Math.floor(r.z0); cz < r.z1; cz++) {
      const id = cx + ',' + cz; if (!surfaceCells.has(id)) surfaceCells.set(id, { key: 'city-road', y: town.roadY });
    }
    return surfaceCells;
  };
  const topAt = (x, z) => cells().get(Math.floor(x) + ',' + Math.floor(z))?.y ?? groundAt(x, z);

  let pools = null;
  if (pairs.length) {
    const spots = [], w = new THREE.Vector3();
    const boxOf = new Map(measure().map((b, i) => [placed[i]?.object, b.box]));
    for (const p of placed) for (const l of p.object.userData.lamps || []) {
      p.object.localToWorld(w.set(l.x, l.y, l.z));
      const h = w.y - (boxOf.get(p.object)?.min.y ?? 0);
      if (h < 0.2 || h > 14) continue;
      spots.push({ x: w.x, z: w.z, r: Math.min(9, Math.max(2, 1.6 + h * 1.35)), hex: l.hex });
    }
    if (spots.length) {
      const N = Math.min(spots.length, 600), pos = new Float32Array(N * 18), uv = new Float32Array(N * 12), col = new Float32Array(N * 18), c = new THREE.Color();
      const Q = [[-1, -1], [1, 1], [1, -1], [-1, -1], [-1, 1], [1, 1]];      // wound to face UP
      for (let i = 0; i < N; i++) {
        const s = spots[i]; c.set(s.hex);
        const y = Math.max(topAt(s.x, s.z), topAt(s.x + s.r / 2, s.z), topAt(s.x - s.r / 2, s.z), topAt(s.x, s.z + s.r / 2), topAt(s.x, s.z - s.r / 2)) + 0.03;
        Q.forEach(([a, b], j) => { pos.set([s.x + a * s.r, y, s.z + b * s.r], i * 18 + j * 3); uv.set([(a + 1) / 2, (b + 1) / 2], i * 12 + j * 2); col.set([c.r, c.g, c.b], i * 18 + j * 3); });
      }
      const g = new THREE.BufferGeometry();
      g.setAttribute('position', new THREE.BufferAttribute(pos, 3)); g.setAttribute('uv', new THREE.BufferAttribute(uv, 2)); g.setAttribute('color', new THREE.BufferAttribute(col, 3));
      const cv = document.createElement('canvas'); cv.width = cv.height = 128;
      const ctx = cv.getContext('2d'), grad = ctx.createRadialGradient(64, 64, 0, 64, 64, 64);
      grad.addColorStop(0, 'rgba(255,255,255,0.8)'); grad.addColorStop(0.18, 'rgba(255,255,255,0.55)'); grad.addColorStop(0.45, 'rgba(255,255,255,0.22)'); grad.addColorStop(0.75, 'rgba(255,255,255,0.06)'); grad.addColorStop(1, 'rgba(255,255,255,0)');
      ctx.fillStyle = grad; ctx.fillRect(0, 0, 128, 128);
      const tex = new THREE.CanvasTexture(cv); tex.colorSpace = THREE.SRGBColorSpace;
      pools = new THREE.Mesh(g, new THREE.MeshBasicMaterial({ map: tex, vertexColors: true, transparent: true, opacity: 0, blending: THREE.AdditiveBlending,
        depthWrite: false, toneMapped: false, polygonOffset: true, polygonOffsetFactor: -2, polygonOffsetUnits: -2 }));
      pools.name = 'lamp-pools'; pools.visible = false; pools.renderOrder = 2; pools.frustumCulled = false;
      group.add(pools);
    }
  }
  let nightK = 0;
  const setNight = (k) => {
    nightK = Math.min(1, Math.max(0, +k || 0));
    for (const m of glowMats.values()) m.emissiveIntensity = nightK * 1.15;
    if (pools) { pools.material.opacity = nightK * 0.58; pools.visible = nightK > 0.01; }   // 0.42 read as nothing on dark asphalt
    return nightK;
  };
  group.userData.night = setNight;

  return {
    group, placed, missing, bounds, terrain, groundAt,
    hd: [...hdKeys],                 // with edition: 'hd', the part keys built from their HD edition
    kit: data.kit, title: data.title, grid: data.grid_m ?? null, interior: !!data.interior,
    city: town && { tile: town.tile, street: town.street, pavement: town.pavement, size: town.size, streets: town.streets, junctions: town.junctions, lots: town.lotsList },
    of: (key) => placed.filter((p) => p.key === key).map((p) => p.object),
    where: (key) => placed.filter((p) => p.key === key).map((p) => p.object.position.clone()),
    vista: ({ fov = 45, aspect = 16 / 9 } = {}) => {
      const size = bounds.getSize(new THREE.Vector3()), reach = Math.max(size.x, size.z * aspect * 0.75);
      const d = (reach / 2) / Math.tan((fov * Math.PI / 180) / 2) * 0.5;
      return { position: new THREE.Vector3(centre.x + d * 0.62, Math.max(size.y * 1.4, d * 0.46), centre.z + d * 0.82),
               target: new THREE.Vector3(centre.x, size.y * 0.25, centre.z) };
    },
    surfaceAt: (x, z) => cells().get(Math.floor(x) + ',' + Math.floor(z))?.key ?? null,
    night: setNight, lamps: pools,
    loose: looseProps,
    light: (object, key) => {
      if (!night) return [];
      const k = Array.isArray(key) ? null : (data.parts[key] ? key : Object.keys(data.parts).find((n) => data.parts[n].id === key));
      const use = Array.isArray(key) ? key.map((q) => toPair(q, 100000 + extraPairs++)) : (k ? pairs.filter((q) => !q.parts || q.parts.has(k)) : []);
      const found = lightUp(object, use);
      setNight(nightK);
      return found;
    },
    groundColor: data.ground?.color ?? terrainColor ?? (town?.green ? '#86a85c' : null), groundY: terrain ? terrainFoot : (data.ground?.y ?? -0.05),
    surround: async ({ count = 340, depth = 90, gap = 2.5, keys = null, seed = 11 } = {}) => {
      if (data.interior && !keys) return null;
      const pool = (keys || have.filter((k) => scatterable(k, data.parts))).filter((k) => mods[k]);
      if (!pool.length) return null;
      const hard = pool.filter((k) => HARD.test(k)), green = pool.filter((k) => !hard.includes(k));
      let r = seed >>> 0; const rnd = () => ((r = (r * 1664525 + 1013904223) >>> 0) / 4294967296);
      const hx = (bounds.max.x - bounds.min.x) / 2, hz = (bounds.max.z - bounds.min.z) / 2;
      const ring = new THREE.Group();
      for (let i = 0; i < count; i++) {
        const a = rnd() * Math.PI * 2, edge = Math.min(hx / Math.max(1e-6, Math.abs(Math.cos(a))), hz / Math.max(1e-6, Math.abs(Math.sin(a))));
        const d = edge + gap + depth * Math.pow(rnd(), 2.3);
        const from = green.length && (!hard.length || rnd() < 0.88) ? green : (hard.length ? hard : pool), k = from[Math.floor(rnd() * from.length)];
        const o = make(k, { part: k });
        const x = centre.x + Math.cos(a) * d, z = centre.z + Math.sin(a) * d;
        o.position.set(x, groundAt(x, z), z);
        o.rotation.y = rnd() * Math.PI * 2;
        o.scale.setScalar(0.85 + rnd() * 0.45);
        ring.add(o);
      }
      ring.updateMatrixWorld(true);
      const { mergeAssets } = await import(new URL('merge.mjs', import.meta.url).href);
      const { merged } = mergeAssets(ring.children.map((o) => ({ object: o })), { keepDynamic: false });
      if (!merged) return null;        // nothing it could build: no scenery past the edge rather than a TypeError
      merged.name = 'surround';
      group.add(merged);
      return merged;
    },
    openSpot: ({ near = null, minHeight = 0.5 } = {}) => {
      const solid = measure().filter((b) => b.low && b.box.max.y - Math.max(0, b.box.min.y) >= minHeight).map((b) => b.low);
      let best = null, bestScore = -Infinity;
      for (let x = Math.ceil(bounds.min.x); x <= bounds.max.x; x++) {
        for (let z = Math.ceil(bounds.min.z); z <= bounds.max.z; z++) {
          let d = Infinity;
          for (const b of solid) {
            const dx = Math.max(b.min.x - x, 0, x - b.max.x), dz = Math.max(b.min.z - z, 0, z - b.max.z);
            d = Math.min(d, Math.hypot(dx, dz));
          }
          const onFloor = cells().size && cells().has(Math.floor(x) + ',' + Math.floor(z)) ? 2.5 : 0;
          const score = Math.min(d, data.interior ? 1.5 : 4) + onFloor - ( near ? Math.hypot(x - near.x, z - near.z) * 0.08 : Math.hypot(x - centre.x, z - centre.z) * 0.04);
          if (score > bestScore) { bestScore = score; best = new THREE.Vector3(x, 0, z); }
        }
      }
      if (best) best.y = groundAt(best.x, best.z);
      return best;
    },
    boxes: ({ minHeight = 0.3 } = {}) => measure().filter((b) => b.box.max.y - Math.max(0, b.box.min.y) >= minHeight),
    blocked: (x, z, radius = 0.35, { minHeight = 0.45, loose: countLoose = false } = {}) => {
      for (const b of measure()) {
        if (!b.low || (b.loose && !countLoose) || b.box.max.y - Math.max(0, b.box.min.y) < minHeight) continue;
        if (x <= b.low.min.x - radius || x >= b.low.max.x + radius || z <= b.low.min.z - radius || z >= b.low.max.z + radius) continue;
        if (!b.hull) return b.key;
        let inside = true, near = Infinity;
        for (let i = 0; i < b.hull.length; i++) {
          const p = b.hull[i], q = b.hull[(i + 1) % b.hull.length], ex = q[0] - p[0], ez = q[1] - p[1], px = x - p[0], pz = z - p[1];
          if (ex * pz - ez * px < 0) inside = false;
          const t = Math.max(0, Math.min(1, (px * ex + pz * ez) / (ex * ex + ez * ez || 1)));
          near = Math.min(near, Math.hypot(px - ex * t, pz - ez * t));
        }
        if (inside || near < radius) return b.key;
      }
      return null;
    },
    spots(n = 8, { from = null, road = false, radius = 0.5 } = {}) {
      const start = from || this.openSpot(), x0 = Math.floor(bounds.min.x), z0 = Math.floor(bounds.min.z), w = Math.ceil(bounds.max.x) - x0, d = Math.ceil(bounds.max.z) - z0;
      const seen = new Uint8Array(w * d), open = [], queue = [[Math.floor(start.x) - x0, Math.floor(start.z) - z0]];
      while (queue.length) {
        const [cx, cz] = queue.pop();
        if (cx < 0 || cz < 0 || cx >= w || cz >= d || seen[cz * w + cx]) continue;
        seen[cz * w + cx] = 1;
        const x = x0 + cx + 0.5, z = z0 + cz + 0.5;
        if (this.blocked(x, z, radius)) continue;
        if (!road || /street|road|lane|asphalt/.test(this.surfaceAt(x, z) || '')) open.push(new THREE.Vector3(x, groundAt(x, z), z));
        queue.push([cx + 1, cz], [cx - 1, cz], [cx, cz + 1], [cx, cz - 1]);
      }
      const out = [], apart = (p) => Math.min(Math.hypot(p.x - start.x, p.z - start.z), ...out.map((q) => Math.hypot(p.x - q.x, p.z - q.z)));
      while (out.length < n && open.length) { let best = 0, far = -1; open.forEach((p, i) => { const k = apart(p); if (k > far) { far = k; best = i; } }); out.push(open.splice(best, 1)[0]); }
      return out;
    },
    people: folk,
    tick: (t) => {
      for (const n of ticking) n.userData.tick(t);
      const dt = lastT === null ? 0 : Math.max(0, Math.min(0.25, t - lastT));
      lastT = t;
      for (const p of folk) {
        if (p.walk && dt > 0) {
          const o = p.object, w = p.walk, to = w.path[w.next], dx = to[0] - o.position.x, dz = to[1] - o.position.z, dist = Math.hypot(dx, dz), step = w.speed * dt;
          if (dist <= step) { o.position.x = to[0]; o.position.z = to[1]; w.next = (w.next + 1) % w.path.length; }
          else { o.position.x += dx / dist * step; o.position.z += dz / dist * step; let turn = Math.atan2(dx, dz) - o.rotation.y; turn = Math.atan2(Math.sin(turn), Math.cos(turn)); o.rotation.y += turn * Math.min(1, dt * 6); }
          o.position.y = groundAt(o.position.x, o.position.z);
        }
        p.anim?.update(dt);
      }
    },
  };
}

export default buildKit;
