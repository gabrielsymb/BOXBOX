// Construction Site — procedural city-block-under-construction terrain.
//
// WHAT THIS KIT'S GROUND IS, AND WHAT IT IS NOT.
//
// This kit is an OVERLAY. Its whole reason to exist is that a buyer can close a
// lane, hoard a corner lot and put a tower crane over a street they already own,
// on the New York City and Neighborhood Pathways 4 m grid. So its ground is not
// open country, and the brief says exactly which part of it belongs to a height
// field: "a terrain program cuts the excavation, the ramp down into it, the
// piling platform and the spoil bund, stepped to the 4 m module."
//
// Everything else a site stands on is a PART, deliberately not here. The
// broken-out road, the compacted hardcore, the mud-and-rut and the fresh concrete
// slab are 4 x 4 m tiles. The trench with its battered sides and its trench box
// is a connecting part. The conical spoil HEAP with its volume knob is a prop.
// What is left for a height field is the shape of the hole, the graded lot it was
// dug out of, the way the dumpers get in and out, the bank of material that came
// up out of it, and the churned made ground of the block around it:
//
//   made-ground      the block outside the hoarding: mounds, hollows, ruts
//   piling-platform  the lot cut and filled to ONE formation level
//   spoil-bund       the bank of dug material inside the hoarding line
//   excavation       the basement box, benched one 4 m module down
//   haul-ramp        the corridor down into it
//
// THE LOT IS AXIS-ALIGNED, AND THAT IS THE ONE PLACE THIS MODULE BREAKS THE
// CATALOGUE'S HOUSE RULE ON PURPOSE.
//
// Every other placed feature in these modules is set out on a bearing off the
// chunk grid, because two straight things within about 25 degrees of each other
// read as parallel stripes. A hoarded lot cannot be: the hoarding runs in 4 m
// bays, the ground tiles are 4 x 4 m squares, the scaffold bay is 4 m, and the
// kit exists to sit inside somebody else's street grid. A lot at 23 degrees is a
// lot none of the kit can be laid on.
//
// So there is exactly ONE straight family here — the lot rectangle, the box
// inside it, the ramp down one wall of that box and the bund following the
// hoarding line — and the only other landform, the made ground outside, is
// organic and has no bearing at all. That is the same escape the Modern
// Restaurant module took: rather than add a second straight family and then have
// to police the angle between it and the first, leave the second one with nothing
// to be parallel to.
//
// It also means this module needs no sine, no cosine and no exponential: every
// number below comes out of +, -, *, / and sqrt, which is the same discipline the
// shared core keeps so that node and a browser cannot disagree about a seam.
//
// WHERE THE HOARDING LINE FALLS, TO THE METRE. The kit's scale rule puts ground
// tiles on 4 m centres with "tiling-edge vertices at exactly +/-2 m", so the tile
// EDGE lines of the world grid are at 4k + 2, not at 4k. The lot's centre is
// snapped to a whole 4 m and its half-widths are 22 and 18 — both 4k + 2 — so
// every edge of the lot lands on a tile edge line. A hoarding run, a crash
// barrier or a row of 4 x 4 m tiles laid against the line lands ON it rather
// than half a bay over it, and the hardcore/made-ground colour boundary falls on
// a quad edge instead of through the middle of one.
import * as THREE from 'three';

/* === TERRAIN CORE v1 === (canonical copy; tools/terrain-sync.mjs vendors this
 * block verbatim into every kit's terrain module, and bin/terrain-check.mjs
 * refuses a build where any copy has drifted.)
 *
 * WHY A SHARED CORE AT ALL.
 *
 * A terrain generator per kit is easy. Terrain generators that JOIN are not,
 * and the joining cannot be bolted on later: it is a property of how every
 * generator computes its heights, so it has to be true of the first one.
 *
 * The contract is three fixed numbers and one rule.
 *
 *   CHUNK = 64 m.  One chunk is one square of world. 64 divides evenly by every
 *     kit module in the catalogue (4 m for NYC / Space Base / Retro Cars /
 *     Little Tokyo / Steampunk Farm / Coral Reef, 2 m for Medieval, 1 m for the
 *     farm kits), so a grid kit's floor tiles land exactly on chunk edges
 *     instead of half a tile over the seam. It is also about one kit diorama
 *     wide, which is what COMPOSE.md already asks for.
 *
 *   SKIRT = 4 m.  The band inside each edge where a kit's own character has to
 *     fade out. At the boundary itself the kit contributes exactly nothing.
 *
 *   res in {1, 2, 4} m.  Vertex pitch. Two chunks may only be joined at equal
 *     res: differing pitch means differing vertex counts along the seam, and no
 *     height rule can close that gap. The check tool tests every res and the
 *     assembler refuses a mismatch rather than emitting a crack.
 *
 * THE RULE: every generator's height is `baseHeight(worldX, worldZ, seed)` plus
 * its own displacement multiplied by a weight that is 0 on the boundary. So two
 * chunks from two different kits, placed side by side, agree vertex for vertex
 * along the seam, because both are evaluating the SAME pure function at the
 * same world coordinates. There is no registry, no neighbour handshake and no
 * runtime negotiation: joinability is a consequence of arithmetic.
 *
 * A consequence worth knowing: baseHeight already says where ocean is and where
 * land is, so a reef chunk is only placeable where the base is below zero. The
 * shared field sorts underwater kits from land kits instead of needing a rule.
 *
 * NO THREE.JS IN HERE. The core is pure arithmetic over plain arrays, so it can
 * be imported and diffed by a node test with no renderer, and so the kit module
 * above it owns every three.js decision. It also uses only +, -, *, /, floor
 * and sqrt: no sin, cos or pow, whose last bits are implementation-defined and
 * would let node and the browser disagree about a seam.
 */

export const TERRAIN_CORE_VERSION = 1;
export const CHUNK = 64;         // metres, square
export const SKIRT = 4;          // metres, taper band at each edge
/* Vertex pitches, finest first. 0.5 m exists for a SMALL block: a 64 m chunk at
 * half-metre pitch is the same 32k triangles a 256 m block spends at 2 m, so the
 * detail a buyer can have is a budget they spend on either size or sharpness. */
export const RESOLUTIONS = [0.5, 1, 2, 4];

/* ------------------------------------------------------------------ math */

const clamp = (v, lo, hi) => (v < lo ? lo : v > hi ? hi : v);
const smooth = (t) => t * t * (3 - 2 * t);
const lerp = (a, b, t) => a + (b - a) * t;

/** Integer hash -> [0,1). Math.imul keeps this exact in 32-bit across engines. */
function hash2(ix, iz, seed) {
  let h = Math.imul(ix | 0, 374761393) ^ Math.imul(iz | 0, 668265263) ^ Math.imul(seed | 0, 1274126177);
  h = Math.imul(h ^ (h >>> 13), 1274126177);
  return ((h ^ (h >>> 16)) >>> 0) / 4294967296;
}

/** Value noise, smoothstep-interpolated. Pure function of world position. */
function vnoise(x, z, seed) {
  const x0 = Math.floor(x), z0 = Math.floor(z);
  const fx = smooth(x - x0), fz = smooth(z - z0);
  const a = hash2(x0, z0, seed), b = hash2(x0 + 1, z0, seed);
  const c = hash2(x0, z0 + 1, seed), d = hash2(x0 + 1, z0 + 1, seed);
  return lerp(lerp(a, b, fx), lerp(c, d, fx), fz);
}

/** Fractal sum of value noise, SPREAD to fill [0,1].
 *
 *  The spread is not cosmetic. A plain octave sum piles up around its own mean —
 *  measured here, 3 octaves put 90% of all values between 0.27 and 0.73 — so
 *  every threshold written against it means something far narrower than it
 *  looks, and a feature gated at "> 0.62" fires nearly everywhere instead of on
 *  the crests. That produced a first reef whose outcrops, cracks and channels
 *  all came out as one flat blanket 2 m above the seabed.
 *
 *  Summing octaves of independent noise shrinks the deviation by
 *  sqrt(sum of amp^2) / sum of amp, so dividing it back out gives the same
 *  spread at any octave count and lets callers treat 0.5 as the true middle. */
function fbm(x, z, seed, octaves = 3) {
  let sum = 0, amp = 1, norm = 0, sq = 0, f = 1;
  for (let i = 0; i < octaves; i++) {
    sum += vnoise(x * f, z * f, (seed | 0) + i * 7919) * amp;
    norm += amp;
    sq += amp * amp;
    amp *= 0.5;
    f *= 2;
  }
  const gain = 1.3 * norm / Math.sqrt(sq);   // 1.3 puts one octave's p05/p95 on 0.05/0.95
  return clamp((sum / norm - 0.5) * gain + 0.5, 0, 1);
}

/** Value noise WITH its analytic derivative: [n, dn/dx, dn/dz].
 *
 *  Needed by erode(). The smoothstep weights are polynomials, so their
 *  derivatives are exact and cheap — no finite differences, which would be both
 *  slower and only approximately right at the lattice lines. */
function vnoiseD(x, z, seed) {
  const x0 = Math.floor(x), z0 = Math.floor(z);
  const tx = x - x0, tz = z - z0;
  const ux = smooth(tx), uz = smooth(tz);
  const dux = 6 * tx * (1 - tx), duz = 6 * tz * (1 - tz);   // d/dt of 3t^2-2t^3
  const a = hash2(x0, z0, seed), b = hash2(x0 + 1, z0, seed);
  const c = hash2(x0, z0 + 1, seed), d = hash2(x0 + 1, z0 + 1, seed);
  const k1 = b - a, k2 = c - a, k3 = a - b - c + d;
  return [a + k1 * ux + k2 * uz + k3 * ux * uz,
          dux * (k1 + k3 * uz),
          duz * (k2 + k3 * ux)];
}

/** EROSION-SHAPED noise: fbm whose octaves are damped by the slope accumulated
 *  so far, so detail dies on steep faces and survives in the flats.
 *
 *  Real hydraulic erosion is a grid simulation, and a grid simulation cannot be
 *  a pure function of (x, z) — which every height in this core has to be, or
 *  heightAt() stops working at arbitrary points and the join contract loses the
 *  one property it rests on. This is the standard analytic stand-in: dividing
 *  each octave by 1 + |accumulated gradient|^2 carves smooth valleys and leaves
 *  sharp crests, which is what erosion looks like from a distance and is free.
 *
 *  The paper that prompted it (arXiv:2608.05248) lists erosion beside peaks,
 *  dunes and terraces as a geomorphic operator over noise, which is exactly the
 *  shape this takes. */
export function erode(x, z, seed, octaves = 5) {
  let sum = 0, amp = 1, norm = 0, f = 1, dx = 0, dz = 0;
  for (let i = 0; i < octaves; i++) {
    const [n, nx, nz] = vnoiseD(x * f, z * f, (seed | 0) + i * 6151);
    dx += nx * f;
    dz += nz * f;
    sum += (amp * n) / (1 + 0.7 * (dx * dx + dz * dz));
    norm += amp;
    amp *= 0.5;
    f *= 2;
  }
  /* SPREAD, for the same reason fbm is spread: the damping drags everything
   * down, so raw output sits around 0.33 with 90% of it between 0.15 and 0.54.
   * A caller writing "> 0.6 is a ridge" against that would be describing the top
   * 2% of the terrain rather than its crests. Measured across 3 to 6 octaves the
   * centre moves 0.357 to 0.324 and the gain 2.11 to 2.35, so one pair of
   * constants is honest across the useful range. */
  return clamp((sum / norm - 0.335) * 2.25 + 0.5, 0, 1);
}

/** Ridged noise: the |1 - 2n| fold that turns smooth hills into crests and
 *  gullies. Kits use it for rock formations and channel walls. */
function ridge(x, z, seed, octaves = 3) {
  const n = fbm(x, z, seed, octaves);
  const r = 1 - Math.abs(1 - 2 * n);
  return r * r;
}

/* ------------------------------------------------------- the shared field */

/** THE contract. Elevation of the world at (x, z) in metres, relative to sea
 *  level at y = 0. Every kit's terrain equals this exactly on its boundary.
 *
 *  Two scales only, deliberately: continent-scale masses that decide land from
 *  ocean, plus broad relief so a seam is never a dead-flat line. Everything
 *  finer is the kit's job, and the kit's job stops at the skirt.
 *
 *  Amplitudes are chosen so a chunk-sized window spans a workable 8-14 m of
 *  relief: enough that a seam is never a dead-flat line, little enough that a
 *  kit's own features are still the thing you look at. */
export function baseHeight(x, z, seed = 1) {
  const continent = fbm(x / 380, z / 380, seed, 4);
  const relief = fbm(x / 88, z / 88, (seed | 0) + 7717, 3);
  return (continent - 0.5) * 46 + (relief - 0.5) * 7;
}

/* ============================================================== RIVERS ==
 *
 * A river is a PROGRAM, not a blue stripe.
 *
 * The first stream here was a noise contour: wherever a warped fbm crossed 0.5,
 * cut a groove and paint it blue. That has two defects you cannot patch. It runs
 * uphill as happily as down, because a contour of noise knows nothing about
 * elevation; and its "water" was a colour on the sloping faceted bed, so it read
 * as a jagged blue sawtooth catching the light at a different angle on every
 * facet.
 *
 * This traces an actual watercourse. Sources sit on a coarse world lattice,
 * every one of them somewhere the shared field is above sea level, and each is
 * walked DOWNHILL along the gradient of baseHeight with a meander term added,
 * until it reaches the sea or runs out of steps. What comes back is a polyline
 * with an elevation that decreases monotonically, which gives three things a
 * contour cannot:
 *
 *   - the channel always descends, so the water in it is going somewhere;
 *   - it WIDENS and DEEPENS downstream, because a river gathers;
 *   - the water surface is the traced elevation, so it is level across the
 *     channel and falling along it, which is what water looks like.
 *
 * It is a pure function of the world seed, and the lattice is in WORLD
 * coordinates, so two neighbouring blocks trace the same river and it crosses
 * the seam instead of stopping at it.
 */

const RIVER_CELL = 380;      // metres between potential sources
const RIVER_STEP = 7;        // metres per traced step
const RIVER_MAX = 260;       // steps before a course gives up
const _riverCache = new Map();

/** Gradient of the shared field, by central difference. */
function baseGrad(x, z, seed, e = 6) {
  return [(baseHeight(x + e, z, seed) - baseHeight(x - e, z, seed)) / (2 * e),
          (baseHeight(x, z + e, seed) - baseHeight(x, z - e, seed)) / (2 * e)];
}

/** Every river course whose lattice cell touches the given world rectangle. */
function riverCourses(seed, x0, z0, x1, z1) {
  const c0 = Math.floor(x0 / RIVER_CELL) - 2, c1 = Math.floor(x1 / RIVER_CELL) + 2;
  const d0 = Math.floor(z0 / RIVER_CELL) - 2, d1 = Math.floor(z1 / RIVER_CELL) + 2;
  const out = [];
  for (let d = d0; d <= d1; d++) {
    for (let c = c0; c <= c1; c++) {
      const key = `${seed}:${c}:${d}`;
      let course = _riverCache.get(key);
      if (course === undefined) {
        course = traceRiver(seed, c, d);
        _riverCache.set(key, course);
      }
      if (course) out.push(course);
    }
  }
  return out;
}

/** Walk one source downhill. Returns null when the cell has no river in it. */
function traceRiver(seed, c, d) {
  let x = (c + 0.15 + 0.7 * hash2(c, d, seed + 61)) * RIVER_CELL;
  let z = (d + 0.15 + 0.7 * hash2(c, d, seed + 62)) * RIVER_CELL;
  /* CLIMB TO A SOURCE FIRST. Dropped straight onto the lattice point, a course
   * starts wherever that point happens to sit — which is usually most of the way
   * down already, so it reaches the sea in thirty steps and the whole river is
   * 90 m long with a 6 m drop. Walking UP the gradient first finds the local
   * high ground that a real catchment drains from, and the course that follows
   * is long because it has the whole hillside to come down. */
  for (let i = 0; i < 60; i++) {
    const [gx, gz] = baseGrad(x, z, seed);
    const len = Math.sqrt(gx * gx + gz * gz);
    if (len < 0.004) break;                    // a summit, or flat: stop climbing
    x += (gx / len) * RIVER_STEP;
    z += (gz / len) * RIVER_STEP;
  }
  // A river starts in high ground. Below this the catchment is a coastal flat,
  // and tracing it produces a puddle that wanders the shoreline.
  if (baseHeight(x, z, seed) < 8) return null;
  const pts = [];
  let stalled = 0, bestY = Infinity;
  for (let i = 0; i < RIVER_MAX; i++) {
    const y = baseHeight(x, z, seed);
    pts.push([x, z, y]);
    if (y < -1.5) break;                       // reached the sea
    /* A course that stops descending is going round a basin, not flowing. Left
     * unguarded it spends its whole step budget circling: measured, one course
     * walked 1,820 m of steps inside a 44 m box. */
    if (y < bestY - 0.05) { bestY = y; stalled = 0; } else if (++stalled > 12) break;
    const [gx, gz] = baseGrad(x, z, seed);
    const len = Math.sqrt(gx * gx + gz * gz);
    /* No fall line, no river. Water does not flow across flat ground, it pools,
     * and a course that keeps walking anyway is steered entirely by its meander
     * term — which is a constant turn, so it goes round in a circle. Measured
     * with the meander at +-40 degrees: courses spending 28 steps inside a 9 m
     * box. Ending the course here is both cheaper and more truthful. */
    if (len < 0.004) break;
    const fx = -gx / len, fz = -gz / len;
    /* And the meander stays small enough that the fall line always wins. At
     * +-17 degrees the course is sinuous; past about 30 it starts to spiral on
     * any ground gentle enough to let it. */
    const m = (fbm(x / 90, z / 90, seed + 811, 2) - 0.5) * 0.6;
    /* A ROTATION of the fall line, so the course bends without ever turning back
     * uphill. Written in place as `dx += -dz*m; dz += dx*m` it is not a rotation
     * at all: the second line reads the dx the first line just changed, which
     * compounds the turn every step and spirals the river. */
    let dx = fx - fz * m, dz = fz + fx * m;
    const dl = Math.sqrt(dx * dx + dz * dz) || 1;
    x += (dx / dl) * RIVER_STEP;
    z += (dz / dl) * RIVER_STEP;
  }
  if (pts.length < 8) return null;
  /* Elevation is forced MONOTONIC. The traced ground can rise slightly through a
   * meander or a noise bump, and water that climbs even 20 cm ruins the whole
   * illusion, so the surface only ever falls. */
  let run = pts[0][2];
  for (const p of pts) { run = Math.min(run, p[2]); p[2] = run; }
  return pts;
}

/** Distance from a point to ONE segment of a course, plus how far down the
 *  course that is. Null when the point is out of reach of this segment. */
function segNear(pts, i, x, z, reach) {
  const [ax, az] = pts[i], [bx, bz] = pts[i + 1];
  const vx = bx - ax, vz = bz - az;
  const vv = vx * vx + vz * vz;
  let t = vv > 0 ? ((x - ax) * vx + (z - az) * vz) / vv : 0;
  t = clamp(t, 0, 1);
  const px = ax + vx * t, pz = az + vz * t;
  const dd = (x - px) * (x - px) + (z - pz) * (z - pz);
  if (dd > reach * reach) return null;
  return { d2: dd, i, t, px, pz, y: lerp(pts[i][2], pts[i + 1][2], t), frac: (i + t) / (pts.length - 1) };
}

/**
 * The river system over one block. Hand the result to buildChunk:
 *
 *   const river = makeRiver({ seed, x0, z0, x1, z1, width: 6, depth: 2.6 });
 *   features: [{ name: 'river', height: river.carve }]
 *   water: { levelAt: river.levelAt, color: [...] }
 *
 * `width` and `depth` are the values at the MOUTH; upstream is narrower and
 * shallower in proportion, because that is the one thing everybody notices when
 * it is missing.
 */
export function makeRiver(o = {}) {
  const seed = (o.seed ?? 1) | 0;
  const wMax = o.width ?? 6;
  const dMax = o.depth ?? 2.6;
  const bank = o.bank ?? 3.5;                   // metres of graded bank each side
  /* Reach is a HALF width plus the bank, not a full width plus the bank. Using
   * the full width made the wetted area about 30 m across for a 7 m river,
   * because everything lower than the water line within reach of the course
   * counts as wet. */
  const reach = wMax / 2 + bank + 1;
  /* Keep only the courses that actually reach this block, and only the biggest
   * couple of those. Every traced course within two lattice cells used to be
   * carved, which put thirteen of them through one 128 m block: the water came
   * out as a braided marsh rather than a river, because that is what thirteen
   * overlapping channels at slightly different levels look like. A landscape
   * gets one river and maybe a tributary. */
  const x0 = o.x0 ?? -1e4, z0 = o.z0 ?? -1e4, x1 = o.x1 ?? 1e4, z1 = o.z1 ?? 1e4;
  const near = riverCourses(seed, x0, z0, x1, z1).filter((pts) => {
    for (const [px, pz] of pts) {
      if (px > x0 - reach && px < x1 + reach && pz > z0 - reach && pz < z1 + reach) return true;
    }
    return false;
  });
  near.sort((a, b) => b.length - a.length);
  const courses = near.slice(0, o.maxCourses ?? 2);

  /* A SPATIAL INDEX over the segments, because the naive version is unusable:
   * every height sample would test every segment of every course, which at 16
   * courses of 260 points against a 128 m block at 1 m pitch is about 65 million
   * segment tests for one build. Bucketed by cell, a sample tests the handful of
   * segments that could possibly be within reach. */
  const CELL = 24;
  const grid = new Map();
  const put = (cx2, cz2, seg) => {
    const k = cx2 + ',' + cz2;
    const a = grid.get(k);
    if (a) a.push(seg); else grid.set(k, [seg]);
  };
  for (const pts of courses) {
    for (let i = 0; i + 1 < pts.length; i++) {
      const x0s = Math.min(pts[i][0], pts[i + 1][0]) - reach;
      const x1s = Math.max(pts[i][0], pts[i + 1][0]) + reach;
      const z0s = Math.min(pts[i][1], pts[i + 1][1]) - reach;
      const z1s = Math.max(pts[i][1], pts[i + 1][1]) + reach;
      for (let cz2 = Math.floor(z0s / CELL); cz2 <= Math.floor(z1s / CELL); cz2++) {
        for (let cx2 = Math.floor(x0s / CELL); cx2 <= Math.floor(x1s / CELL); cx2++) {
          put(cx2, cz2, [pts, i]);
        }
      }
    }
  }

  /** The nearest course at this point, or null. */
  function at(x, z) {
    const segs = grid.get(Math.floor(x / CELL) + ',' + Math.floor(z / CELL));
    if (!segs) return null;
    let best = null;
    for (const [pts, i] of segs) {
      const n = segNear(pts, i, x, z, reach);
      if (n && (!best || n.d2 < best.d2)) best = n;
    }
    return best;
  }

  /** Height displacement: the cut. Negative inside the channel, tapering up
   *  through the bank to nothing. */
  function carve(x, z) {
    const n = at(x, z);
    if (!n) return 0;
    // Downstream growth: a quarter of full at the source, full at the mouth.
    const grow = 0.25 + 0.75 * n.frac;
    const half = wMax * grow / 2;
    const dist = Math.sqrt(n.d2);
    if (dist > half + bank) return 0;
    const t = dist <= half ? 1 : 1 - (dist - half) / bank;
    return -smooth(clamp(t, 0, 1)) * dMax * grow;
  }

  /* The water line is measured on the COURSE, at the nearest point on it, which
   * is what makes the surface flat across the channel however lumpy the bed is.
   *
   * And it is measured from the surface the kit actually built, not from the
   * bare shared field. The channel is cut as a displacement on top of whatever
   * else the kit raised there, so a hillock lifts the bed with it; measuring the
   * water from the bare field then leaves the river running dry across every
   * rise. Measured: 6 wet quads in a whole 128 m block that should have had
   * about 600. `surfaceAt` is the kit's dry terrain height. */
  const surf = typeof o.surfaceAt === 'function' ? o.surfaceAt : null;
  function levelAt(x, z) {
    const n = at(x, z);
    if (!n) return -1e6;                        // never wet
    const grow = 0.25 + 0.75 * n.frac;
    const bed = surf ? surf(n.px, n.pz) : n.y;
    return bed - dMax * grow * 0.45;            // below the bank lip, above the bed
  }

  return { carve, levelAt, courses, count: courses.length };
}

/** 0 on the block boundary, 1 once you are SKIRT metres inside it. `w` is the
 *  block's width in metres: one chunk, or several when `span` is up. Only the
 *  OUTER edge of a block is a seam with anybody; the chunk lines inside it are
 *  not boundaries at all, because one continuous height function crosses them. */
function skirtWeight(lx, lz, w) {
  const d = Math.min(lx, lz, w - lx, w - lz);
  return smooth(clamp(d / SKIRT, 0, 1));
}

/** Island rim mask: 1 in the middle, 0 outside an irregular noise-warped
 *  outline. This is what makes the store presentation a floating island rather
 *  than a square of ground, and it is a KNOB (edges: 'island'), not a separate
 *  program: the same features, palette and pads run underneath either edge. */
function islandMask(lx, lz, seed, w) {
  // Sampled in a normalised 64 m space, so the silhouette keeps the same shape
  // at any block size instead of a 128 m island growing twice the wobble.
  const u = lx * CHUNK / w, v = lz * CHUNK / w;
  const dx = u - CHUNK / 2, dz = v - CHUNK / 2;
  const r = Math.sqrt(dx * dx + dz * dz) / (CHUNK / 2);
  const wob = fbm(u / 17, v / 17, (seed | 0) + 4241, 3);
  const rim = 0.48 + wob * 0.34;          // 0.48..0.82 of the half-width
  return clamp((rim - r) / 0.09, 0, 1);
}

/** 1 inside a pad rect, ramping to 0 `blend` metres outside it. */
function padWeight(wx, wz, p, blend) {
  const dx = Math.abs(wx - p.x) - p.w / 2;
  const dz = Math.abs(wz - p.z) - p.d / 2;
  const d = Math.max(dx, dz);
  if (d <= 0) return 1;
  if (d >= blend) return 0;
  return smooth(1 - d / blend);
}

/* ------------------------------------------------------------- the build */

/**
 * Build one chunk.
 *
 *   buildChunk({
 *     cx, cz,          // chunk coordinates in the world grid (integers)
 *     seed,            // world seed; chunks only join if they share it
 *     res,             // 1 | 2 | 4 metres per quad
 *     edges,           // 'tile' (joins) | 'island' (floats, store presentation)
 *     features,        // [{ name, height(x,z,ctx), paint(x,z,h,ny,ctx) }]
 *     bands,           // [{ upTo, color:[r,g,b] }] ascending, last is catch-all
 *     cliff,           // [r,g,b] used where the face is steeper than cliffAngle
 *     cliffAngle,      // degrees from horizontal
 *     pads,            // [{ x, z, w, d, y? }] flattened buildable areas
 *     padBlend,        // metres of ramp around each pad
 *     skirtDepth,      // island only: how far the sides fall before stopping
 *   })
 *
 * Returns plain arrays plus the query functions a composer needs. Nothing here
 * knows what a Mesh is.
 */
export function buildChunk(o = {}) {
  const cx = o.cx | 0, cz = o.cz | 0;
  const seed = (o.seed ?? 1) | 0;
  let res = o.res ?? 2;
  if (!RESOLUTIONS.includes(res)) {
    throw new Error(`terrain: res must be one of ${RESOLUTIONS.join(', ')}, got ${res}`);
  }
  const edges = o.edges === 'island' ? 'island' : 'tile';
  /* SPAN: how many chunks across this block is. The join contract is unchanged,
   * because a block's outer edge still falls on chunk lines and still equals
   * baseHeight() there — so a 1-chunk block and a 2-chunk block join each other
   * as happily as two singles. The chunk lines INSIDE a block are not seams at
   * all: one continuous height function crosses them, with no taper. */
  const span = clamp(Math.round(o.span ?? 1), 1, 4);
  const W = CHUNK * span;                      // block width in metres
  /* Size and detail MULTIPLY, so the two knobs together decide the triangle
   * count and either one alone looks harmless. A 256 m block at 1 m pitch is
   * 131,072 triangles, four times the store's ceiling. The block is capped at
   * 128 quads a side and the pitch coarsens to hold it, which is what a buyer
   * dragging the size slider expects anyway: more ground, not more detail. */
  const MAX_QUADS = 128;
  const features = o.features ?? [];
  const bands = (o.bands ?? [{ upTo: Infinity, color: [0.5, 0.5, 0.5] }]).slice()
    .sort((a, b) => a.upTo - b.upTo);
  const cliff = o.cliff ?? null;
  const cliffCos = Math.cos(((o.cliffAngle ?? 38) * Math.PI) / 180);
  const padBlend = o.padBlend ?? 3;
  const skirtDepth = o.skirtDepth ?? 3.5;

  /* Coarsen to the next pitch ON THE LADDER, never to an arbitrary number. The
   * first version divided W by the cap, which handed a 192 m block a pitch of
   * 1.5 m: not a value any other block can be built at, so nothing could join
   * it. Joins need both sides on the same rung. */
  while (W / res > MAX_QUADS) {
    const next = RESOLUTIONS.filter((r) => r > res).sort((a, b) => a - b)[0];
    if (!next) break;
    res = next;
  }

  const ox = cx * CHUNK, oz = cz * CHUNK;
  const ctx = { CHUNK, SKIRT, seed, cx, cz, ox, oz, res, hash2, vnoise, vnoiseD, fbm, ridge, erode, baseHeight, clamp, smooth, lerp };

  /* Pads are resolved once, here, because a pad whose height depended on where
   * you sampled it would not be flat. An explicit y wins; otherwise the pad
   * takes the untouched terrain height at its centre, snapped to 25 cm so a
   * building sits on a round number. */
  const pads = (o.pads ?? []).map((p) => {
    const pad = { x: p.x, z: p.z, w: p.w, d: p.d, y: p.y };
    if (edges === 'tile') {
      // A pad that reached the boundary would flatten the seam and break the
      // join for every neighbour. Refuse loudly rather than ship a bad edge.
      const lx = pad.x - ox, lz = pad.z - oz;
      const near = Math.min(lx - pad.w / 2, lz - pad.d / 2, W - (lx + pad.w / 2), W - (lz + pad.d / 2));
      if (near - padBlend < SKIRT) {
        throw new Error(`terrain: pad at (${p.x}, ${p.z}) reaches inside the ${SKIRT}m skirt; move it or shrink it`);
      }
    }
    return pad;
  });

  /** Height with features but WITHOUT pads: what a pad's own level is read from.
   *
   *  `shape` runs after the features and sees the ACCUMULATED height, which is
   *  what terracing needs: snapping relief to a kit's step height is a statement
   *  about the total, not a displacement you can add. It is blended by the same
   *  skirt weight, so it cannot disturb the seam either. */
  /* FLATTEN clears a LEVEL AREA IN THE MIDDLE, for a town to stand on.
   *
   * Not a global level. Levelling the whole block gives you a plain, and a plain
   * is not what a scene needs: what it needs is somewhere buildable with real
   * landscape around it, so the village sits in a valley floor or on a shelf
   * rather than on a table (owner, 2026-08-11).
   *
   * The knob is the SIZE of that clearing, as a fraction of the block's
   * half-width, with a soft rim so the ground rises out of it instead of
   * stepping. It reaches the shared field's own slope, not just the kit's
   * features: that slope runs 8 to 14 m across a block and is most of what makes
   * ground unbuildable, so a clearing that skipped it would still be a hillside.
   *
   * THE EDGES ARE NEVER TOUCHED, twice over. The clearing plus its rim is capped
   * so it cannot reach within SKIRT of the boundary, and the pull is multiplied
   * by the skirt weight anyway, which is zero there. A block with a town levelled
   * into it still meets its neighbours on the shared field exactly, which
   * bin/terrain-check.mjs keeps proving at delta 0.
   */
  const erosion = clamp(o.erosion ?? 0, 0, 1);
  const erosionDepth = o.erosionDepth ?? 3.2;      // metres at full strength
  const erosionScale = o.erosionScale ?? 34;       // metres per cycle of the coarsest octave
  const flatten = clamp(o.flatten ?? 0, 0, 1);
  const FEATHER = 0.22;                       // rim width, in the same fraction units
  // Cap so the clearing plus its rim stays clear of the skirt band.
  const flatMax = Math.max(0, (W / 2 - SKIRT - 2) / (W / 2)) - FEATHER;
  const flatR = Math.min(flatten * 0.95, flatMax);
  const fcx = ox + W / 2, fcz = oz + W / 2;

  /** 1 inside the clearing, 0 outside its rim. */
  function flatMask(wx, wz) {
    if (flatR <= 0) return 0;
    const dx = (wx - fcx) / (W / 2), dz = (wz - fcz) / (W / 2);
    const r = Math.sqrt(dx * dx + dz * dz);
    return smooth(clamp((flatR + FEATHER - r) / FEATHER, 0, 1));
  }

  /** The surface BEFORE the clearing is cut into it. Separated because the
   *  clearing's own level is read off it: a level derived from a height that
   *  already includes the clearing would chase itself. */
  function preFlat(wx, wz) {
    const base = baseHeight(wx, wz, seed);
    const w = edges === 'tile' ? skirtWeight(wx - ox, wz - oz, W) : 1;
    if (w <= 0) return base;
    let h = base;
    for (const f of features) if (f.height) h += w * f.height(wx, wz, ctx);
    if (o.shape) h = lerp(h, o.shape(h, wx, wz, ctx), w);
    /* EROSION, applied after the kit has built its landforms, because erosion
     * acts on what is there rather than being one more thing that is there.
     * Symmetric about the operator's midpoint, so crests go up and valleys go
     * down rather than the whole block sinking. It does not come out exactly
     * balanced — measured on one chunk, full strength moves the median relief to
     * about -1m — because the operator's own median sits near 0.46 rather than
     * 0.5 and varies by region. That residue is erosion removing material, which
     * is what it is for, and a metre at full strength is small against the 8-14m
     * a block spans.
     * Multiplied by the skirt weight like everything else, so an eroded block
     * still meets its neighbours on the shared field exactly. */
    if (erosion > 0) h += w * erosion * erosionDepth * (erode(wx / erosionScale, wz / erosionScale, seed + 4457, 5) - 0.5) * 2;
    return h;
  }

  /* The clearing sits at the MEAN of the ground it replaces, so it beds into the
   * landscape instead of cutting a step. Snapped to a whole 25 cm, because a
   * building put down on it should land on a round number. */
  let flatY = 0;
  if (flatR > 0) {
    let sum = 0, n = 0;
    const rad = flatR * (W / 2);
    for (let j = -2; j <= 2; j++) {
      for (let i = -2; i <= 2; i++) {
        if (i * i + j * j > 4) continue;                 // stay inside the circle
        sum += preFlat(fcx + (i * rad) / 2, fcz + (j * rad) / 2);
        n++;
      }
    }
    flatY = Math.round((sum / n) * 4) / 4;
  }

  function rawHeight(wx, wz) {
    const h = preFlat(wx, wz);
    if (flatR <= 0) return h;
    const w = edges === 'tile' ? skirtWeight(wx - ox, wz - oz, W) : 1;
    return lerp(h, flatY, flatMask(wx, wz) * w);
  }
  /* AUTOMATIC FLAT AREAS.
   *
   * The rest of the catalogue is FLAT-BOTTOMED. A building, a road tile, a
   * plaza slab and a wall module all assume they are put down on level ground,
   * because that is what a floor tile is. Procedural terrain that is level
   * nowhere is terrain none of those parts can stand on: one corner floats, or
   * the near edge buries itself, and the buyer is left levelling ground by hand
   * for every piece they place. That makes the generator a liability rather
   * than the thing the kit sits on.
   *
   * So flat areas are not an optional extra, they are part of what terrain owes
   * the kit. They are FOUND rather than placed: the flattest low-slope spots
   * the terrain already has get levelled the rest of the way, so a building
   * site looks like somewhere a builder would have chosen instead of a rectangle
   * stamped through a hillside.
   *
   * Snapped to the kit's own module where it has one, so a 4 m tile run lands on
   * the pad edge rather than half a tile over it.
   */
  const flatN = Math.max(0, Math.round(o.flats ?? 0));
  if (flatN > 0) {
    const snap = o.flatSnap || 4;                       // the kit's module, usually
    const size = Math.max(snap, Math.round((o.flatSize ?? 16) / snap) * snap);
    // Stay clear of the seam: the pad plus its ramp must not reach the skirt.
    const lo = SKIRT + padBlend + size / 2;
    const hi = W - lo;
    const cand = [];
    if (hi > lo) {
      for (let k = 0; k < 160; k++) {
        // Snapped candidate centres, so the pad's EDGES land on module lines.
        const cxl = Math.round((lo + hash2(k, 17, seed) * (hi - lo)) / snap) * snap + (size / 2) % snap;
        const czl = Math.round((lo + hash2(k, 83, seed + 5) * (hi - lo)) / snap) * snap + (size / 2) % snap;
        if (cxl < lo || cxl > hi || czl < lo || czl > hi) continue;
        const wx = ox + cxl, wz = oz + czl;
        if (edges === 'island' && islandMask(cxl, czl, seed, W) < 0.9) continue;  // not on the rim
        /* And not in the river. A pad levels the ground it covers, so one laid
         * across a watercourse flattens the channel and the water spreads out
         * over the whole pad as a rectangular lagoon. */
        if (o.flatAvoid && o.flatAvoid(ox + cxl, oz + czl, size / 2 + padBlend)) continue;
        // Roughness: how far the ground already departs from its own mean here.
        let sum = 0, n = 0, mn = Infinity, mx = -Infinity;
        for (let a = -1; a <= 1; a++) {
          for (let b = -1; b <= 1; b++) {
            const h = rawHeight(wx + a * size / 2, wz + b * size / 2);
            sum += h; n++;
            if (h < mn) mn = h;
            if (h > mx) mx = h;
          }
        }
        cand.push({ x: wx, z: wz, mean: sum / n, spread: mx - mn });
      }
    }
    cand.sort((a, b) => a.spread - b.spread);
    const gap = size + padBlend * 2;
    for (const c of cand) {
      if (pads.length >= flatN) break;
      if (pads.some((p) => Math.abs(p.x - c.x) < gap && Math.abs(p.z - c.z) < gap)) continue;
      pads.push({ x: c.x, z: c.z, w: size, d: size, y: Math.round(c.mean * 4) / 4, auto: true });
    }
  }

  for (const p of pads) {
    if (p.y === undefined || p.y === null) p.y = Math.round(rawHeight(p.x, p.z) * 4) / 4;
  }

  /** The full height field, pads included. The mesh and heightAt() share it, so
   *  a part placed with heightAt() sits on the ground and not near it. */
  function sampleHeight(wx, wz) {
    let h = rawHeight(wx, wz);
    for (const p of pads) {
      const w = padWeight(wx, wz, p, padBlend);
      if (w > 0) h = lerp(h, p.y, w);
    }
    return h;
  }

  /* The FINISHED ground, handed to whoever computes a water line.
   *
   * A water level worked out from the kit's own feature functions is a level
   * measured against a surface that does not exist: terrace snapping, erosion,
   * the clearing and the pads all move the ground afterwards, by metres. A pool
   * whose level was read off the pre-shape height then stands proud of the
   * ground beside it, which is exactly what the owner photographed. Anything
   * that has to compare water against land must compare it against THIS. */
  ctx.groundAt = sampleHeight;

  /* --------------------------------------------------------- vertex grid */

  const N = W / res;                          // quads per side
  const S = N + 1;                            // vertices per side
  const hs = new Float64Array(S * S);
  const mask = edges === 'island' ? new Float64Array(S * S) : null;
  for (let j = 0; j < S; j++) {
    for (let i = 0; i < S; i++) {
      const wx = ox + i * res, wz = oz + j * res;
      hs[j * S + i] = sampleHeight(wx, wz);
      if (mask) mask[j * S + i] = islandMask(i * res, j * res, seed, W);
    }
  }

  /* --------------------------------------------------------------- mesh */

  const pos = [];
  const col = [];
  const vx = (i) => ox + i * res;
  const vz = (j) => oz + j * res;
  const vy = (i, j) => hs[j * S + i];

  /* What the height bands are measured against.
   *
   *  'relief' (the default) bands by height ABOVE THE NATURAL FLOOR, so sand is
   *  wherever the kit added nothing, crust is on what it raised and the dark
   *  band is what it cut. Absolute height cannot express that: the same chunk
   *  slopes 10 m across its own width, so a fixed set of world heights paints
   *  one half of it as summit and the other as trench regardless of what the
   *  kit actually built there. 'height' stays available for a kit whose colour
   *  really is depth (a snow line, a tide mark). */
  const byRelief = (o.bandBy ?? 'relief') === 'relief';

  /* EDGE COLOUR BLENDING between neighbouring kits.
   *
   * Height must match exactly at a seam; colour must not. Two kits meeting on a
   * hard line reads as a biome border, which is often right, but a reef running
   * into a beach wants to fade. Colour has no geometric consequence, so unlike
   * height it is free to know about its neighbour: the assembler passes the tone
   * on the other side and each block blends toward it.
   *
   * THE WEIGHT REACHES 0.5 AT THE SEAM, NOT 1, and that is the whole trick. At 1
   * each block would paint the seam entirely in its NEIGHBOUR's colour, which
   * swaps the two rather than blending them and leaves a hard line in exactly
   * the same place. At 0.5 both sides arrive at the same half-and-half mix, so
   * the colour is continuous across the join without either block knowing what
   * the other actually computed.
   *
   * A side may be given either a TONE or the neighbour's own colour FUNCTION:
   *
   *   blend: { east: [r,g,b],                 width: 14 }   // approximate
   *   blend: { east: neighbour.colorAt,       width: 14 }   // exact
   *
   * The tone is cheap and gets most of the way (measured on a reef meeting a
   * forest: the seam difference drops from 0.575 to 0.078). It cannot reach zero
   * because each side blends toward the OTHER's average ground rather than
   * toward what the other actually painted at that spot, and at the seam the
   * local colour is often a cliff or a crack rather than ground.
   *
   * The function is exact. Both sides then compute lerp(mine, theirs, 0.5) from
   * the same two values and land on the same colour to the bit.
   */
  const blend = o.blend ?? null;
  const blendW = blend ? (blend.width ?? 14) : 0;
  function blended(c, wx, wz, hy, ny) {
    if (!blend || blendW <= 0) return c;
    const lx = wx - ox, lz = wz - oz;
    let out = c;
    /* The neighbour is asked about the MIRRORED point, not this one.
     *
     * Colour is decided per face from its centroid, and the two blocks' edge
     * faces have centroids on opposite sides of the line: 127.5 here, 128.5
     * there. Asking the neighbour about 127.5 and letting it ask us about 128.5
     * compares two different places, and the seam stayed 0.024 apart however
     * exact the rest of the arithmetic was. Mirroring makes both sides mix the
     * SAME pair of points at the same weight, so they agree exactly. */
    const sides = [
      ['west', lx, [2 * ox - wx, wz]],
      ['east', W - lx, [2 * (ox + W) - wx, wz]],
      ['north', lz, [wx, 2 * oz - wz]],
      ['south', W - lz, [wx, 2 * (oz + W) - wz]],
    ];
    for (const [side, d, mirror] of sides) {
      const other = blend[side];
      if (!other || d >= blendW || d < 0) continue;
      const t = typeof other === 'function' ? other(mirror[0], mirror[1]) : other;
      if (!t) continue;
      const w = 0.5 * smooth(1 - d / blendW);
      out = [lerp(out[0], t[0], w), lerp(out[1], t[1], w), lerp(out[2], t[2], w)];
    }
    return out;
  }

  function faceColor(cxw, czw, hy, ny) {
    // Features paint last-wins, so a crack drawn after a rock formation reads as
    // a crack. A feature that returns nothing leaves the height bands alone.
    for (let k = features.length - 1; k >= 0; k--) {
      const c = features[k].paint?.(cxw, czw, hy, ny, ctx);
      if (c) return blended(c, cxw, czw, hy, ny);
    }
    if (cliff && ny < cliffCos) return blended(cliff, cxw, czw, hy, ny);
    /* Relief is measured against the ground the CLEARING REPLACED, not against
     * the bare world field. Levelling moves the surface away from that field by
     * design, so banding on the raw difference paints the clearing as trench on
     * one side and summit on the other: a village site came out as a brown mud
     * pit ringed in meadow. Adding back how far the clearing moved the ground
     * restores the colour the terrain had there, which is what a levelled site
     * should look like. Outside the clearing the correction is exactly zero. */
    const lift = flatR > 0 ? preFlat(cxw, czw) - rawHeight(cxw, czw) : 0;
    const v = byRelief ? hy - baseHeight(cxw, czw, seed) + lift : hy;
    for (const b of bands) if (v <= b.upTo) return blended(b.color, cxw, czw, hy, ny);
    return blended(bands[bands.length - 1].color, cxw, czw, hy, ny);
  }

  function emitTri(ax, ay, az, bx, by, bz, cxx, cy, cz2) {
    /* Flat shading is the house look, and the colour is part of it: one tone per
     * FACE, decided from its own centroid and normal.
     *
     * Per-VERTEX colour was tried, so that height bands would blend across a
     * triangle instead of stepping at its edge. It is worse. The gradients read
     * as low quality against a catalogue of crisp flat facets — the whole look
     * depends on every face being one flat colour, and a terrain that softens
     * them stops matching the models standing on it (owner, 2026-08-11). The
     * banding staircase it was meant to fix is better fixed with the bands
     * themselves, or with a finer pitch.
     *
     * REVISITED 2026-09-20, and the two decisions do not conflict. What failed
     * above was a gradient inside a FLAT-shaded triangle. Shown three renders of
     * the Space Base ground side by side (/bench.php, "Look tests", 3a-3c), the
     * owner chose smooth NORMALS with colour blended at the vertex, never across
     * a crease: with smooth shading it is a colour per FACE that looks wrong (it
     * reads as tiles), and ground is the one thing here that is not an object,
     * so its facets are tessellation showing rather than style. The parts keep
     * theirs. That treatment is applied AFTER this function, at run time, by
     * softenTerrain() in public/stage.mjs (buildKit does it to a kit's terrain
     * by default), so this core still emits one flat colour per face and every
     * published terrain, seam check and store render is unchanged. Moving it in
     * here means regenerating and re-rendering every kit's terrain and teaching
     * bin/terrain-check.mjs that seam colours now agree per vertex: a job to do
     * on purpose, not as a side effect. */
    const ux = bx - ax, uy = by - ay, uz = bz - az;
    const wx2 = cxx - ax, wy = cy - ay, wz2 = cz2 - az;
    let nx = uy * wz2 - uz * wy, ny = uz * wx2 - ux * wz2, nz = ux * wy - uy * wx2;
    const len = Math.sqrt(nx * nx + ny * ny + nz * nz) || 1;
    ny /= len;
    const c = faceColor((ax + bx + cxx) / 3, (az + bz + cz2) / 3, (ay + by + cy) / 3, ny);
    pos.push(ax, ay, az, bx, by, bz, cxx, cy, cz2);
    for (let k = 0; k < 3; k++) col.push(c[0], c[1], c[2]);
  }

  const kept = new Uint8Array(N * N);
  for (let j = 0; j < N; j++) {
    for (let i = 0; i < N; i++) {
      if (mask) {
        // A quad survives if any corner is inside the rim; that keeps the
        // outline ragged at quad scale instead of eroding it to a soft circle.
        const m = Math.max(mask[j * S + i], mask[j * S + i + 1], mask[(j + 1) * S + i], mask[(j + 1) * S + i + 1]);
        if (m <= 0) continue;
      }
      kept[j * N + i] = 1;
    }
  }

  /* The outline, needed BEFORE any face is emitted because it is what sets how
   * deep the island goes. A directed edge is on the outline when its reverse is
   * not owned by any kept quad; using DIRECTED edges also hands us the outward
   * side for free, so the wall never comes out inside-out. */
  const key = (i0, j0, i1, j1) => `${i0},${j0},${i1},${j1}`;
  const dir = new Set();
  const outline = [];
  let floorAt = null;
  if (edges === 'island') {
    /* Trim spurs: quads hanging off the body by a single edge, plus quads that
     * are alone entirely. They look like stray teeth on the silhouette, and they
     * are also unbuildable — a one-quad tab makes the outline run out and back
     * along the same line, which is a polygon with no area, and no triangulation
     * of it exists that is not degenerate. Two passes clears the tabs a radial
     * mask actually produces without eating into the landmass. */
    const nbrs = (i, j) =>
      (i > 0 && kept[j * N + i - 1] ? 1 : 0) + (i < N - 1 && kept[j * N + i + 1] ? 1 : 0) +
      (j > 0 && kept[(j - 1) * N + i] ? 1 : 0) + (j < N - 1 && kept[(j + 1) * N + i] ? 1 : 0);
    for (let pass = 0; pass < 2; pass++) {
      const drop = [];
      for (let j = 0; j < N; j++) for (let i = 0; i < N; i++) {
        if (kept[j * N + i] && nbrs(i, j) < 2) drop.push(j * N + i);
      }
      if (!drop.length) break;
      for (const k of drop) kept[k] = 0;
    }

    for (let j = 0; j < N; j++) {
      for (let i = 0; i < N; i++) {
        if (!kept[j * N + i]) continue;
        dir.add(key(i, j, i, j + 1));
        dir.add(key(i, j + 1, i + 1, j + 1));
        dir.add(key(i + 1, j + 1, i + 1, j));
        dir.add(key(i + 1, j, i, j));
      }
    }
    for (const e of dir) {
      const [i0, j0, i1, j1] = e.split(',').map(Number);
      if (dir.has(key(i1, j1, i0, j0))) continue;   // interior: shared by two quads
      outline.push([i0, j0, i1, j1]);
    }
    /* The underside is a constant-thickness slab following the SHARED FIELD, not
     * a flat plate at one height.
     *
     * Two earlier tries were worse. Measured from the lowest point anywhere in
     * the chunk, a single 11 m fissure dragged the base down with it and the
     * island rendered as a tall grey block with terrain on the lid. Measured
     * from the lowest point on the RIM, that fixed the reef but not the forest,
     * whose chunk slopes 11 m across its own width: the plate sat under the low
     * corner, so the wall under the high corner was 11 m tall and the skirt was
     * still 60% of the picture.
     *
     * Hanging the base off baseHeight() instead gives a slab of even thickness
     * that tilts with the ground, which is what a piece of world cut out of the
     * world actually looks like. It also gives features a well-defined floor:
     * they may cut into the slab but not through it. */
    /* The slab follows the FLATTENED surface, not the bare field. Hung off
     * baseHeight alone, a fully flattened island came out as a wedge: a level
     * lawn on top of an underside still tilting with the world. */
    floorAt = (wx, wz) => baseHeight(wx, wz, seed) - skirtDepth;
    for (let j = 0; j < S; j++) {
      for (let i = 0; i < S; i++) {
        const lo = floorAt(ox + i * res, oz + j * res) + 0.4;
        if (hs[j * S + i] < lo) hs[j * S + i] = lo;
      }
    }
  }

  for (let j = 0; j < N; j++) {
    for (let i = 0; i < N; i++) {
      if (!kept[j * N + i]) continue;
      const a = [vx(i), vy(i, j), vz(j)];
      const b = [vx(i), vy(i, j + 1), vz(j + 1)];
      const c = [vx(i + 1), vy(i + 1, j + 1), vz(j + 1)];
      const d = [vx(i + 1), vy(i + 1, j), vz(j)];
      emitTri(a[0], a[1], a[2], b[0], b[1], b[2], c[0], c[1], c[2]);
      emitTri(a[0], a[1], a[2], c[0], c[1], c[2], d[0], d[1], d[2]);
    }
  }

  /* ------------------------------------------------------------- water */

  /* A WATER SURFACE, not a blue stripe painted on the bed.
   *
   * The stream used to be a colour returned by the feature's paint(), which put
   * the water tone on the sloping, faceted floor of the cut. That reads as a
   * jagged blue sawtooth, because it IS the terrain: every facet of the bank
   * catches the light at a different angle and the "water" zigzags with them.
   * Real water is a separate surface with its own plane, and that is the whole
   * fix — it is not a triangles-versus-quads problem. Every renderer draws
   * triangles in the end, and two triangles of a flat quad shade identically;
   * what made it look bad was the surface being the wrong shape, not the wrong
   * primitive.
   *
   * `water.levelAt(x, z, ctx)` returns the water SURFACE height, which for a
   * stream is the valley floor it would have had without the cut. So the surface
   * runs downhill with the valley and stays dead level across it, which is what
   * water does.
   */
  const wpos = [], wcol = [];
  if (o.water) {
    const wcolor = o.water.color ?? [0.2, 0.5, 0.6];
    /* SWELL. A sea at one exact height is a sheet of glass, and the giveaway is
     * that it reads as a lid rather than as water. A few centimetres of noise on
     * the level is enough for the facets to catch the light unevenly, which is
     * all a low-poly sea needs. It is on the LEVEL, not on the mesh, so the
     * shoreline moves with it and the surface stays continuous. */
    const swell = o.water.swell ?? 0;
    const swellScale = o.water.swellScale ?? 9;
    const base = typeof o.water.levelAt === 'function'
      ? (x, z) => o.water.levelAt(x, z, ctx)
      : () => Number(o.water.level ?? 0);
    const levelAt = swell > 0
      ? (x, z) => base(x, z) + (fbm(x / swellScale, z / swellScale, seed + 9091, 2) - 0.5) * 2 * swell
      : base;
    const drop = o.water.drop ?? 0.04;   // sit a hair under the surface line

    /* The water level lives on the VERTEX GRID, not per quad.
     *
     * Both other ways were tried and both are visibly wrong. Sampling a level
     * per CORNER of each quad independently lets neighbouring quads tilt
     * opposite ways wherever the level function steps, and throws bright
     * vertical shards out of the surface. Sampling ONE level per quad fixes the
     * shards and breaks the sheet instead: adjacent quads sit at slightly
     * different heights with nothing joining them, so the river renders as a
     * checkerboard of floating tiles with the bed showing through the gaps.
     *
     * A shared vertex grid is the only arrangement that is both continuous and
     * flat: two quads meeting at an edge use the same two heights for it, by
     * construction.
     */
    const SENT = -1e5;
    const wl = new Float64Array(S * S);
    const blocked = new Uint8Array(S * S);   // ground water may never reach
    for (let j = 0; j < S; j++) {
      for (let i = 0; i < S; i++) {
        /* No water inside the CLEARING. Levelling a town site raises the ground
         * through whatever watercourse ran across it, and the water surface,
         * which follows the river's own traced elevation, is then left perched
         * over the new ground as a bright sheet standing on edge. Filling the
         * channel is what levelling a site does anyway. Faded with the same mask
         * as the clearing, so the river thins out as it reaches it rather than
         * being chopped. */
        const dry = flatR > 0 ? flatMask(vx(i), vz(j)) : 0;
        if (dry > 0.5) blocked[j * S + i] = 1;
        const L = dry > 0.5 ? SENT : levelAt(vx(i), vz(j)) - drop;
        wl[j * S + i] = L <= SENT ? SENT : L;
      }
    }
    /* THE DRAIN — the guarantee, as opposed to the good intention.
     *
     * Everything above trusts `levelAt` to hand back a level the ground can
     * hold. A kit can measure that carefully (this one caps each pool at its
     * crater's own spill point) and still be wrong, because a level function
     * only ever looks at its own feature: terracing steps the whole block by
     * 4 m, a pad levels twenty metres of it, the clearing drops the middle, and
     * the barrier that actually decides where a pool drains can be further away
     * than any of them looked.
     *
     * So the surface is drained here, against the finished vertex grid, where
     * there is nothing left to be wrong about. Each pool is flooded outward at
     * its own level, THROUGH ground the level covers and without caring whether
     * the level function claimed water there — because that is precisely how a
     * pool escapes: over ground its author never sampled. The flood stops on
     * ground that stands above the level, and the LOWEST of those stopping
     * points is where the pool would really run out; the surface drops to just
     * under it. Lowering a level shrinks the body, which can expose a lower rim
     * still, so it runs to a fixed point — two or three passes in practice.
     *
     * AND THE BODY KEEPS THE GROUND IT FLOODED. This is the half that actually
     * removes the wall, and it took a measurement to see: lowering alone leaves
     * the SURFACE where the level function drew it, which on the terraced block
     * was the crater's own bowl, while the ground it covers carries on past that
     * at the same height. The sheet then ends in mid air a metre and a half over
     * a level bench that is plainly under it. Water fills what it reaches, so
     * every flooded vertex gets the level, and the sheet can only end where the
     * ground has risen through it — which is what a shoreline is.
     *
     * A pool that escapes further than `spread` metres is not standing water at
     * all, it is a sheet lying across the countryside, and it is emptied rather
     * than lowered. That case is a level measured against ground that has since
     * been terraced or levelled out from under it.
     *
     * A pool is the case this is right for and a RIVER is the case it is wrong
     * for: a watercourse is one connected body from source to mouth and flatting
     * it to its lowest point would empty the whole valley. `pools: true` is how
     * a kit says its water is standing water. */
    if (o.water.pools) {
      const reach = Math.ceil((o.water.spread ?? 30) / res);
      const cap = reach * reach * 4;
      const stack = [];
      /* Where the ground itself STOPS — the block boundary, and on an island the
       * ragged rim where quads were thrown away. A pool that reaches one of
       * these has nothing holding it in: it pours off the cut edge. There is no
       * level low enough to fix that, so the pool is emptied rather than
       * lowered, which is also what stops a sheet from ending in the air over
       * the skirt. */
      const spillsOff = new Uint8Array(S * S);
      for (let j = 0; j < S; j++) {
        for (let i = 0; i < S; i++) {
          let open = 0;
          for (const [a, b] of [[i - 1, j - 1], [i, j - 1], [i - 1, j], [i, j]]) {
            if (a < 0 || b < 0 || a >= N || b >= N || !kept[b * N + a]) open = 1;
          }
          spillsOff[j * S + i] = open;
        }
      }
      for (let iter = 0; iter < 8; iter++) {
        const seen = new Int32Array(S * S).fill(-1);
        let changed = false, comp = 0;
        for (let s0 = 0; s0 < S * S; s0++) {
          if (seen[s0] >= 0 || wl[s0] <= SENT || hs[s0] >= wl[s0]) continue;
          const level = wl[s0];
          const cells = [];
          let barrier = Infinity, deepest = Infinity, escaped = false;
          seen[s0] = comp; stack.length = 0; stack.push(s0);
          while (stack.length) {
            const c = stack.pop();
            cells.push(c);
            if (hs[c] < deepest) deepest = hs[c];
            if (cells.length > cap || spillsOff[c]) { escaped = true; break; }
            const i = c % S, j = (c - i) / S;
            for (const [a, b] of [[i - 1, j], [i + 1, j], [i, j - 1], [i, j + 1]]) {
              if (a < 0 || b < 0 || a >= S || b >= S) continue;
              const d = b * S + a;
              if (blocked[d]) continue;                         // a wall, not a rim
              if (hs[d] < level) {
                if (seen[d] < 0) { seen[d] = comp; stack.push(d); }
              } else if (hs[d] < barrier) barrier = hs[d];      // the dry rim
            }
          }
          comp++;
          /* maxDepth belongs on the LEVEL, not on the individual quad. Rejecting
           * quads for being too deep cuts a hole in the middle of a surface that
           * carries on around it, and the cut edge stands over the deep ground it
           * refused to cover — a wall of water, which is the very thing this
           * whole pass is about. Lowering the body until it is nowhere deeper
           * than the cap shrinks it from its own shoreline instead. */
          const target = escaped ? SENT
            : Math.min(level, barrier - 0.05, deepest + (o.water.maxDepth ?? 4));
          for (const c of cells) {
            const now = target <= SENT ? SENT : target;
            if (wl[c] !== now && (wl[c] > now || wl[c] <= SENT)) { wl[c] = now; changed = true; }
          }
        }
        if (!changed) break;
      }
    }

    /* A vertex just outside the water's reach still belongs to a quad that is
     * half under water, and it needs a height. Grown from its wet neighbours,
     * a couple of rings out, which is enough for the shoreline fringe.
     *
     * A GROWN LEVEL IS A HEIGHT, NOT A CLAIM THAT THERE IS WATER HERE, and the
     * two were being conflated. The grown value is the pool's own level carried
     * outward past the last place the level function said anything, so on the
     * downhill side of a bowl it lands on ground that is lower still — and the
     * wetness test, which only asks "is the ground under the level", then
     * counted those vertices as open water. The pool crawled two rings out over
     * the lip and stopped in mid air, a flat sheet standing above the field
     * beside it (the owner's screenshot). Grown vertices are recorded and are
     * allowed to CLOSE a quad, never to flood one. */
    const grown = new Uint8Array(S * S);
    for (let pass = 0; pass < 2; pass++) {
      const next = wl.slice();
      for (let j = 0; j < S; j++) {
        for (let i = 0; i < S; i++) {
          if (wl[j * S + i] > SENT) continue;
          let sum = 0, n = 0;
          for (const [a, b] of [[i - 1, j], [i + 1, j], [i, j - 1], [i, j + 1]]) {
            if (a < 0 || b < 0 || a >= S || b >= S) continue;
            const v = wl[b * S + a];
            if (v > SENT) { sum += v; n++; }
          }
          if (n) { next[j * S + i] = sum / n; grown[j * S + i] = 1; }
        }
      }
      wl.set(next);
    }
    /* One smoothing pass over the finished level field. A water surface that
     * changes height quickly between neighbouring vertices renders as a steep
     * facet, and a steep facet in a smooth material catches the key light as a
     * white shard — which is exactly what the outside of the bends looked like.
     * Averaging with the valid neighbours costs nothing and the shoreline moves
     * by centimetres. */
    {
      const sm = wl.slice();
      for (let j = 0; j < S; j++) {
        for (let i = 0; i < S; i++) {
          if (wl[j * S + i] <= SENT) continue;
          let sum = wl[j * S + i], n = 1;
          for (const [a, b] of [[i - 1, j], [i + 1, j], [i, j - 1], [i, j + 1]]) {
            if (a < 0 || b < 0 || a >= S || b >= S) continue;
            const v = wl[b * S + a];
            if (v > SENT) { sum += v; n++; }
          }
          sm[j * S + i] = sum / n;
        }
      }
      wl.set(sm);
    }

    for (let j = 0; j < N; j++) {
      for (let i = 0; i < N; i++) {
        if (!kept[j * N + i]) continue;
        const corner = [[i, j], [i, j + 1], [i + 1, j + 1], [i + 1, j]];
        let wet = 0, known = 0, leaks = 0;
        for (const [a, b] of corner) {
          const L = wl[b * S + a];
          if (L <= SENT) continue;
          known++;
          if (vy(a, b) >= L) continue;
          // Under the level. Where the level was MEASURED that is open water;
          // where it was only grown outward it is the surface running past its
          // own shore onto lower ground, which is the perch the owner caught.
          if (grown[b * S + a]) leaks++; else wet++;
        }
        /* Two of four is the shoreline: fewer and the quad is bank, more and it
         * is open water. Taking any-corner-wet floods the bank instead.
         *
         * EXCEPT ON A DRAINED POOL, where one is enough and two leaves a hole.
         * The two-corner rule assumes a shore shallow enough that the quad
         * before the waterline is half wet, and a terraced or cut bank is not:
         * the ground climbs four metres in one quad, so the last cell has a
         * single submerged corner, fails the test, and the sheet stops a metre
         * short of the bank with its edge standing over ground that is plainly
         * under water. Extending it costs nothing — over the dry corners the
         * surface is below the ground and the terrain draws over it — and it is
         * only safe here because the drain has already proved the level is one
         * the ground can hold. */
        if (wet < (o.water.pools ? 1 : 2) || known < 4 || leaks) continue;
        /* And no perched water. A hollow that happens to sit near the course but
         * well below it would otherwise fill to the channel's level and read as
         * a flooded field rather than a river. */
        let deepest = 0;
        for (const [a, b] of corner) deepest = Math.max(deepest, wl[b * S + a] - vy(a, b));
        /* A SEA is unbounded: the seabed can be twenty metres down and the
         * surface is still the surface. maxDepth exists for a river, where water
         * perched in a hollow far from the channel reads as a flooded field. */
        if (deepest > (o.water.maxDepth ?? 4)) continue;
        const P = corner.map(([a, b]) => [vx(a), wl[b * S + a], vz(b)]);
        wpos.push(...P[0], ...P[1], ...P[2]);
        wpos.push(...P[0], ...P[2], ...P[3]);
        for (let q = 0; q < 6; q++) wcol.push(wcolor[0], wcolor[1], wcolor[2]);
      }
    }
  }

  /* ------------------------------------------------------- island skirt */

  if (edges === 'island') {
    const side = o.skirtColor ?? bands[0].color;
    /* The base ring is pulled INWARD, so the sides lean under the landmass
     * instead of dropping as a vertical extrusion. A straight prism reads as a
     * cookie cutter no matter how good the terrain on its lid is; the batter is
     * most of what makes this look like a piece of ground that was lifted out.
     * For a directed outline edge, (dz, -dx) is the inward perpendicular. */
    /* WALLS drop straight down from the outline to the slab floor.
     *
     * They used to lean inward, which looked better on a tall skirt and stopped
     * mattering once the slab became a constant thickness that follows the
     * ground: a 2.5 m rim reads as a slice cut out of the world whether or not
     * it is battered. What the batter cost was the underside. A tapered ring no
     * longer matches the quad grid, so the cap had to be triangulated from the
     * ring itself, and every triangulation of a 128 m blob from its own boundary
     * produces slivers — a centroid fan makes them 60 m long and 1 m wide, which
     * is what the owner found radiating out of the bottom of the reef.
     *
     * Straight walls keep the bottom ON THE GRID, so the cap is just quads.
     */
    for (const [i0, j0, i1, j1] of outline) {
      const ax = vx(i0), az = vz(j0), ay = vy(i0, j0), aby = floorAt(ax, az);
      const bx = vx(i1), bz = vz(j1), by = vy(i1, j1), bby = floorAt(bx, bz);
      // (a, aDown, bDown) + (a, bDown, b) faces outward for this winding.
      pos.push(ax, ay, az, ax, aby, az, bx, bby, bz);
      pos.push(ax, ay, az, bx, bby, bz, bx, by, bz);
      for (let k = 0; k < 6; k++) col.push(side[0], side[1], side[2]);
    }

    /* The underside, on the same grid as the top, ONE QUAD PER CELL.
     *
     * Merging each row into runs is the obvious saving and it is wrong here: the
     * slab floor follows the world, so a run's single long edge does not lie
     * along the per-cell edges the walls were built from. It measured 374
     * unpaired edges, which is 374 hairline gaps around the rim. Runs would only
     * be safe under a dead-flat plate, and a flat plate is what made the island
     * read as a block sitting on sloping ground in the first place. */
    const under = o.underColor ?? side;
    for (let j = 0; j < N; j++) {
      for (let i = 0; i < N; i++) {
        if (!kept[j * N + i]) continue;
        const x0 = vx(i), x1 = vx(i + 1), z0 = vz(j), z1 = vz(j + 1);
        const h = (x, z) => floorAt(x, z);
        // Reversed against the top faces, so the normal points down.
        pos.push(x0, h(x0, z0), z0, x1, h(x1, z1), z1, x0, h(x0, z1), z1);
        pos.push(x0, h(x0, z0), z0, x1, h(x1, z0), z0, x1, h(x1, z1), z1);
        for (let k = 0; k < 6; k++) col.push(under[0], under[1], under[2]);
      }
    }
  }

  /* ------------------------------------------------------------ queries */

  /** The height a caller should place something at. Identical to the mesh,
   *  including the island's base-plate clamp: a query that disagreed with the
   *  geometry would drop a prop through the floor of a fissure. */
  const groundAt = floorAt
    ? (wx, wz) => Math.max(sampleHeight(wx, wz), floorAt(wx, wz) + 0.4)
    : sampleHeight;

  function slopeAt(wx, wz) {
    const e = res / 2;
    const hx = groundAt(wx + e, wz) - groundAt(wx - e, wz);
    const hz = groundAt(wx, wz + e) - groundAt(wx, wz - e);
    const nx = -hx, nz = -hz, ny = 2 * e;
    const len = Math.sqrt(nx * nx + ny * ny + nz * nz) || 1;
    return Math.acos(clamp(ny / len, -1, 1)) * (180 / Math.PI);
  }

  /** Deterministic placement points, so a composition that scatters 40 rocks
   *  puts them in the same 40 places on every build. */
  function scatter(n, opt = {}) {
    const maxSlope = opt.maxSlope ?? 18;
    const minSlope = opt.minSlope ?? 0;
    const inset = opt.inset ?? 2;
    const avoidPads = opt.avoidPads !== false;
    const sseed = (opt.seed ?? seed) | 0;
    const out = [];
    for (let k = 0; k < n * 60 && out.length < n; k++) {
      const wx = ox + inset + hash2(k, 91, sseed) * (W - 2 * inset);
      const wz = oz + inset + hash2(k, 173, sseed + 31) * (W - 2 * inset);
      if (mask && islandMask(wx - ox, wz - oz, seed, W) <= 0.35) continue;
      if (avoidPads && pads.some((p) => padWeight(wx, wz, p, padBlend) > 0)) continue;
      const s = slopeAt(wx, wz);
      if (s < minSlope || s > maxSlope) continue;
      const y = groundAt(wx, wz);
      if (opt.minY !== undefined && y < opt.minY) continue;
      if (opt.maxY !== undefined && y > opt.maxY) continue;
      out.push({ x: wx, y, z: wz, slope: s, rot: hash2(k, 407, sseed + 77) * Math.PI * 2 });
    }
    return out;
  }

  /* ---------------------------------------------------- placement queries */

  /* WHERE CAN I BUILD? The question a town, a farm or a depot actually asks, and
   * the one a height field answers badly on its own: sampling heightAt() a few
   * times tells you nothing about whether a 20 m building will sit level.
   *
   * Scans the block for square footprints whose ground varies less than
   * `maxDrop` from corner to corner, largest first, and returns the ones that do
   * not overlap. A caller gets somewhere to put a village without knowing
   * anything about noise, features or how this kit builds its relief.
   *
   * Cached coarse grid, because the honest version is unusable: four footprint
   * sizes scanned at 2 m over a 128 m block is about 400,000 height samples, and
   * a height sample here runs the kit's features and its river. Sampling once
   * into a grid and scanning THAT is the same answer for a thousandth of the
   * work.
   */
  function flatAreas(opt = {}) {
    const maxDrop = opt.maxDrop ?? 0.75;
    const want = opt.count ?? 12;
    const minSize = opt.minSize ?? 8;
    const step = Math.max(res, 2);
    const G = Math.floor(W / step) + 1;
    const grid = new Float64Array(G * G);
    for (let j = 0; j < G; j++) {
      for (let i = 0; i < G; i++) grid[j * G + i] = groundAt(ox + i * step, oz + j * step);
    }
    const at = (i, j) => grid[clamp(j, 0, G - 1) * G + clamp(i, 0, G - 1)];

    const sizes = [];
    for (let sz = Math.min(48, Math.floor(W / 3)); sz >= minSize; sz = Math.floor(sz / 1.5)) sizes.push(sz);
    const found = [];
    for (const size of sizes) {
      const half = size / 2 / step;
      for (let j = half; j + half < G; j += 1) {
        for (let i = half; i + half < G; i += 1) {
          let lo = Infinity, hi = -Infinity;
          // 5x5 over the footprint: corners and middle are what decide level.
          for (let b = -2; b <= 2; b++) {
            for (let a = -2; a <= 2; a++) {
              const h = at(Math.round(i + (a * half) / 2), Math.round(j + (b * half) / 2));
              if (h < lo) lo = h;
              if (h > hi) hi = h;
            }
          }
          const drop = hi - lo;
          if (drop > maxDrop) continue;
          const x = ox + i * step, z = oz + j * step;
          // Never straddle the seam: the outer band belongs to the neighbours.
          if (x - size / 2 < ox + SKIRT || x + size / 2 > ox + W - SKIRT) continue;
          if (z - size / 2 < oz + SKIRT || z + size / 2 > oz + W - SKIRT) continue;
          if (mask && islandMask(x - ox, z - oz, seed, W) < 0.95) continue;
          if (opt.avoidWater !== false && o.water) {
            const lv = typeof o.water.levelAt === 'function'
              ? o.water.levelAt(x, z, ctx) : Number(o.water.level ?? 0);
            if (lo < lv) continue;                     // partly under water
          }
          found.push({ x, z, size, y: Math.round(((lo + hi) / 2) * 4) / 4, drop: +drop.toFixed(3) });
        }
      }
    }
    found.sort((a, b) => (b.size - a.size) || (a.drop - b.drop));
    const kept = [];
    for (const f of found) {
      if (kept.length >= want) break;
      const clash = kept.some((k) => Math.abs(k.x - f.x) < (k.size + f.size) / 2
                                  && Math.abs(k.z - f.z) < (k.size + f.size) / 2);
      if (!clash) kept.push(f);
    }
    return kept;
  }

  /** Ground along a straight line, for laying a road, a wall or a bridge: the
   *  heights, the slope between consecutive samples, and whether each point is
   *  under water. A road is chosen by looking at this, not by hoping. */
  function profile(x0, z0, x1, z1, n = 32) {
    const out = [];
    for (let k = 0; k <= n; k++) {
      const t = k / n;
      const x = lerp(x0, x1, t), z = lerp(z0, z1, t);
      const y = groundAt(x, z);
      const wet = o.water
        ? y < (typeof o.water.levelAt === 'function' ? o.water.levelAt(x, z, ctx) : Number(o.water.level ?? 0))
        : false;
      const prev = out[out.length - 1];
      const run = prev ? Math.sqrt((x - prev.x) ** 2 + (z - prev.z) ** 2) : 0;
      out.push({ x: +x.toFixed(2), z: +z.toFixed(2), y: +y.toFixed(2),
                 grade: prev && run > 0 ? +(((y - prev.y) / run) * 100).toFixed(1) : 0, wet });
    }
    return out;
  }

  /** The heights along one boundary, in order. The join test compares these
   *  between neighbours; in tile mode they are baseHeight() by construction. */
  function seam(side) {
    const out = new Float64Array(S);
    for (let k = 0; k < S; k++) {
      if (side === 'north') out[k] = hs[0 * S + k];
      else if (side === 'south') out[k] = hs[(S - 1) * S + k];
      else if (side === 'west') out[k] = hs[k * S + 0];
      else out[k] = hs[k * S + (S - 1)];
    }
    return out;
  }

  return {
    positions: new Float32Array(pos),
    colors: new Float32Array(col),
    tris: pos.length / 9,
    // Water is its own surface so the kit can give it its own material, and so
    // it never lands in the terrain's palette measurement as if it were ground.
    waterPositions: new Float32Array(wpos),
    waterColors: new Float32Array(wcol),
    waterTris: wpos.length / 9,
    chunk: CHUNK, span, width: W, res, edges, seed, cx, cz,
    /* The tone a NEIGHBOUR should blend toward: the band covering ordinary
     * ground, which is the one a viewer reads as "what this kit's floor is". */
    groundColor: (bands.find((b) => b.upTo > 0) ?? bands[bands.length - 1]).color,
    /* What this kit paints at a point, BEFORE any edge blending. A neighbour
     * asks this to blend toward; handing back the blended colour instead would
     * make two neighbours each chase the other. */
    colorAt: (wx, wz) => {
      /* Takes ONLY a position, and works out the height and the slope itself. It
       * has to be a pure function of (x, z): a neighbour calls it while shading
       * its own faces, and if the answer depended on the height THAT block
       * happens to have there, the two sides would mix different values and the
       * seam would not close. */
      const hy = groundAt(wx, wz);
      const ny = Math.cos((slopeAt(wx, wz) * Math.PI) / 180);
      for (let k = features.length - 1; k >= 0; k--) {
        const c = features[k].paint?.(wx, wz, hy, ny, ctx);
        if (c) return c;
      }
      if (cliff && ny < cliffCos) return cliff;
      const lift = flatR > 0 ? preFlat(wx, wz) - rawHeight(wx, wz) : 0;   // see faceColor
      const v = byRelief ? hy - baseHeight(wx, wz, seed) + lift : hy;
      for (const b of bands) if (v <= b.upTo) return b.color;
      return bands[bands.length - 1].color;
    },
    pads,
    heightAt: groundAt,
    flatAreas,
    profile,
    slopeAt,
    scatter,
    seam,
  };
}
/* === END TERRAIN CORE === */

/* ------------------------------------------------------------------ palette */

/* Four STATES OF ONE SITE, not four places. The plant, the cabins and the
 * hoarding are the same hardware whenever you look at them, so the ground is
 * what says what week it is and what the machines are digging through: a working
 * site in ordinary weather, a waterlogged February, weeks without rain, and a
 * box taken out of clay.
 *
 * Every tone is a literal hex from the kit palette, and the saturated half of
 * that palette is left out of all four: the greens, the yellows, the hi-vis
 * orange and the red belong to plant livery, beacons, vests and the buyer's own
 * hoarding graphics, not to dirt. A construction site really is browns and greys,
 * and a green verge here would be the generic grass-rock-and-pond landscape
 * three interior kits arrived at rather than this one. */
const COLORWAYS = {
  'working-site': { earth: '#6B5842', hardcore: '#9BA5AB', spoil: '#D9C093', mud: '#4A3A2A', cut: '#79838A' },
  /* A waterlogged February. Everything steps down TOGETHER, which is the only way
   * a wet site reads: darken the mud alone and the lot turns into a lit stage with
   * a black hole in it. The dug material goes dark enough that the bund stops
   * being the brightest thing on the block and the hardcore takes that job. */
  'wet-winter':   { earth: '#4A3A2A', hardcore: '#79838A', spoil: '#8E7859', mud: '#15181A', cut: '#555E63' },
  /* Weeks without rain. Everything is under the same dust, so the whole ladder
   * moves up and the churned mud — which on a dry site is just the last ground
   * the water got to — is the only warm tone left. */
  'dry-dust':     { earth: '#8E7859', hardcore: '#C2CACE', spoil: '#E8EBEA', mud: '#6B5842', cut: '#9BA5AB' },
  /* The one scheme where the CUT is the warm thing rather than the grey thing: a
   * box taken out of clay comes up orange-brown on every face, so the hole reads
   * as a coloured box in brown ground instead of as a shadow in it. */
  'clay-cut':     { earth: '#6B5842', hardcore: '#9BA5AB', spoil: '#D9C093', mud: '#4A3A2A', cut: '#B3663A' },
};
export const presets = COLORWAYS;

const ZONES = ['earth', 'hardcore', 'spoil', 'mud', 'cut'];

/* ------------------------------------------------------------------- params */

/* Terrain is a horizontal surface and the house 18 degrees barely sees one: the
 * excavation, which is the whole point of this ground, compresses into a dark
 * smudge and the bench inside it stops reading as a level at all. 28 shows the
 * box as a box and still reads as a three-quarter view rather than as a site
 * plan. */
export const view = { heroEl: 28 };

/* Ground does not hinge, unbolt or light up, and the contract wants that said out
 * loud rather than inferred from a module with no exports.
 *
 * RIG: nothing here moves. Everything on this site that slews, tips, extends or
 * turns is a machine standing ON this ground, and a terrain that breathed would
 * break the one property the join contract rests on — that height is a pure
 * function of position. DETACH: nothing lifts off; every piece of this asset is
 * the landform. NIGHT: the ground emits nothing. This kit has more lit parts than
 * any other in the catalogue — the lighting tower, the gantry underside, the
 * walkway tunnel, the arrow board, the temporary traffic light — and every one of
 * them earns its light by standing on ground that has none. */
export const rig = {};
export const detach = [];
export const night = {};

export const params = {
  colorway: {
    type: 'choice', default: 'working-site', label: 'Colorway',
    options: Object.keys(COLORWAYS),
    describe: 'What week the site is in and what it is digging through. working-site is warm brown made ground round a grey hardcore platform with pale dug material, the default; wet-winter steps every tone down together for a waterlogged February and takes the bottom of the box to near-black; dry-dust lifts the whole ladder for weeks without rain and leaves the churned mud as the only warm tone; clay-cut keeps the block brown and turns the cut faces orange, so the hole reads as a coloured box rather than as a shadow. The geometry is identical in all four.',
  },
  edges: {
    type: 'choice', default: 'island', label: 'Edges', affects: 'geometry',
    options: ['island', 'tile'],
    describe: 'island cuts the block to an irregular floating landmass with sides that skirt down and stop, which is how one piece of ground is presented on its own. tile keeps the full square and forces the outer 4 m to the shared world field, so this block joins any other kit\'s terrain edge to edge with no crack. Switch to tile when you are laying more than one block, or dropping this beside a New York City or Neighborhood Pathways block — they share this kit\'s 4 m module and they are what it is built to sit in.',
  },
  res: {
    type: 'choice', default: 1, label: 'Detail', affects: 'geometry',
    options: [4, 2, 1, 0.5],
    describe: 'Metres per quad. 4 is coarse faceted ground that loses the bench inside the excavation entirely, 2 is a mid read, 1 resolves the bench, both cut faces of the box and the side walls of the haul ramp as crisp facets with an interior rather than as dotted edges, and is the default. A block is capped at 128 quads a side, so at Size 3 and 4 the vertex pitch coarsens to hold the triangle budget whatever this says. Two blocks can only be joined at the SAME setting, because differing vertex pitch along a shared edge cannot be closed by any height rule.',
  },
  span: {
    type: 'range', default: 2, min: 1, max: 4, step: 1, label: 'Size', affects: 'geometry',
    describe: 'How many 64 m chunks across the block is. A block is anchored at its own CORNER and grows toward +x and +z, so the sizes do not simply zoom out: 2 is 128 x 128 m, is the default, and is the one every number in this module was measured against — the hoarded lot sits dead in the middle of it with the excavation, the ramp, the bund and a belt of made ground around them. 1 is the single 64 x 64 m chunk the default block grew FROM, which is the quarter of it the site is mostly not in: it carries the churned street outside the hoarding, with one corner of the lot and the end of a bund cut off by the block edge — the platform fades into the shared field there, because the outer 4 m of any block belongs to its neighbours. 4 is 256 x 256 m and holds four sites, 152 m apart. Size and detail share one triangle budget, so past 2 the vertex pitch coarsens to hold it — dragging this up buys more ground, not more of both. Any size joins any other, because the outer edge still lands on chunk lines and still matches the shared world field there.',
  },
  relief: {
    type: 'range', default: 1, min: 0, max: 1.8, step: 0.05, label: 'Relief', affects: 'geometry',
    describe: 'Overall vertical exaggeration of everything this kit SHAPES over the shared world field: how deep the box goes, how tall the bund stands and how hard the made ground rolls. 0 is the site on the day the hoarding went up — a levelled lot with nothing dug out of it; 1 is a working site with a 6 m box, two of the kit\'s own 3 m storeys of basement, benched one module down, a bund standing 2.2 m and made ground rolling 2.4 m up and 1.8 m down outside; 1.8 is a deep box whose lower wall has stopped being a batter and become a propped sheeted face. It deliberately does not scale the GRADING: cutting a platform level is not a matter of vertical exaggeration, so at 0 the lot is still dead flat. Does not move the block edges, which always match the neighbours.',
  },
  platform: {
    type: 'range', default: 1, min: 0, max: 1, step: 0.05, label: 'Piling platform', affects: 'geometry',
    describe: 'How completely the hoarded lot is cut and filled to ONE formation level: a 44 x 36 m dead-flat platform — eleven by nine of the kit\'s 4 m bays — with a 10 m apron outside it over which the grading releases the land. It sits at the MEAN of the ground it replaces, so it is a cut on the high side and a fill on the low and the site beds into the street instead of standing on a table; on the default block that is about 2 m of cut at the near corner and as much fill at the far one. It is what lets the kit\'s flat-bottomed cabins, crane base, hoarding runs and 4 x 4 m ground tiles sit flush with nothing levelled by hand, and it is the surface a piling rig actually needs under it. 0 leaves the lot on the fall the block has, which is what nobody could track a crawler across.',
  },
  dig: {
    type: 'range', default: 1, min: 0, max: 1.6, step: 0.05, label: 'Excavation', affects: 'geometry',
    describe: 'Depth of the basement box taken out of the middle of the lot: a 28 x 20 m cut with an 8 m working margin all round it for the crane base, the plant, the cabins and the bund, leaving an 18 x 10 m floor. It is BENCHED, and the bench sits exactly one 4 m module below the formation level whatever this says, because that is where a basement\'s first slab goes and because a bench that slid with the knob would eat the floor: what the knob moves is the second face, which is a 2 m batter at 1 and a near-vertical propped wall by 1.6. At 1 the box is 6 m deep, which is two of the kit\'s 3 m storeys. 0 leaves the platform whole, which is the site before the dig starts.',
  },
  ramp: {
    type: 'range', default: 1, min: 0, max: 1, step: 0.05, label: 'Haul ramp', affects: 'geometry',
    describe: 'The way anything on wheels or tracks gets into the hole: an 8 m corridor — a 4 m running lane between two 2 m cut side walls — let into the near wall of the box, running 26 m from the lot\'s own edge down to the floor at about 1 in 4, which is what a short site ramp really runs at. It cuts through the platform for its first eight metres before it reaches the box, so it has proper cut side walls rather than reading as a notch in a wall, and it comes in on the pavement side, which is where the site gate, the wheel wash and the banksman go. 0 removes it and leaves a box nothing can drive out of, which is a site that lifts its muck out by crane.',
  },
  bund: {
    type: 'range', default: 1, min: 0, max: 1.6, step: 0.05, label: 'Spoil bund', affects: 'geometry',
    describe: 'Height of the long bank of dug material heaped along the two sides of the lot with no gate on them: 7.6 m from toe to toe with a 2.4 m flat trodden top where the dumpers tip, standing 2.2 m at 1 over a batter that peaks at 52 degrees, and wrapping the back corner before it runs out. It is the BUND and not the heap: a bund follows the hoarding line and is what a site does with material it cannot cart away the same day, while the conical heap with its own volume knob is a prop. It comes in LENGTHS with gaps between them, because a dumper tips where the plant can reach and not as a kerb — measured on the default block it covers 75 per cent of one side in two lengths with a 10 m gap, and 56 per cent of the other in one length and a stub. It takes the dug-material colour over its whole footprint. 0 means every load went straight out of the gate.',
  },
  rubble: {
    type: 'range', default: 1, min: 0, max: 1.6, step: 0.05, label: 'Made ground', affects: 'geometry',
    describe: 'How churned the block OUTSIDE the hoarding is — the ground a city block sits on once it has been built over twice. It works three ways at once: mounds about 31 m from one crest to the next and 2.4 m tall at 1, where the last demolition was tipped; hollows as much as 1.8 m deep where the last basement was grubbed out and never filled; and a metre of tracked-over lumpiness at a 6.5 m wavelength across both. Mounds take about 28 per cent of the block and hollows about 29, with a quarter of it left exactly as the world made it, and the three together come out mean-zero — what this knob digs out it also heaps up, so raising it roughens the block rather than lifting it. It is graded away under the lot, because a mound left standing inside the hoarding is a bump in the middle of a working platform. 0 leaves the world\'s own slope.',
  },
  terrace: {
    type: 'range', default: 0, min: 0, max: 1, step: 0.05, label: 'Level stepping', affects: 'geometry',
    describe: 'How hard ALL relief on the block snaps to the kit\'s own 4 m module, on top of the grading the lot already has. 0 is ground shaped only by the earthworks; 1 quantises every height to whole 4 m levels, so street tiles, hoarding bays, crash barrier runs and scaffold lifts sit on dead-level shelves everywhere rather than only inside the hoarding — which is what you want when this block is going under a New York City or Neighborhood Pathways street grid, and is why the knob is here. It is OFF by default, and that is this kit\'s module rather than taste: 4 m is the coarsest step in the catalogue, so a quarter turn of this knob puts a metre-high riser through open ground over a tenth of a metre of plan, and a riser that steep is a cut face — right inside the hoarding and grey litter scattered over the street outside it. Measured on the default block, faces steep enough to paint as cut go from 0.00 per cent of the ground outside the lot at 0, through 0.05 at 0.1, to 1.6 per cent at 0.25 and 6.1 at 0.5. It is held off the haul ramp, because a stepped ramp is a staircase, and off the bund, because a quantised heap is a flight of contour rings.',
  },
  pad: {
    type: 'range', default: 0, min: 0, max: 32, step: 4, label: 'Building plot', affects: 'geometry',
    describe: 'Width in metres of one dead-level plot levelled somewhere OUTSIDE the hoarding, and therefore a whole number of 4 m modules square: at 16 a four-by-four module compound, welfare block or neighbouring slab sits flush on it with no corner floating, and a run of 4 x 4 m tiles laid across it lands on the plot edge rather than half a tile over it. It is FOUND in the flattest low-slope ground the block has rather than stamped through a slope, and it is held clear of the lot and its apron, because the piling platform is already 44 x 36 m of dead-level ground and a plot cut across the excavation would simply fill it in. 0 is none, which is the default for that reason. The plot and its ramp can never reach the 4 m skirt, because flattening the seam would break the join for every neighbour.',
  },
  flatten: {
    type: 'range', default: 0, min: 0, max: 1, step: 0.05, label: 'Level clearing', affects: 'geometry',
    describe: 'Clears a LEVEL AREA IN THE MIDDLE of the block for a whole street grid to stand on, and leaves the ground around it alone. The number is the size of that clearing: 0 is none, 0.5 is a circle roughly a third of the block across, 1 is as large as it can go without touching the edges. Its rim is soft, so the ground rises out of the clearing rather than stepping. Unlike the piling platform it levels the WORLD\'s own slope too, so this is the knob to reach for when you are dropping the block into a flat city rather than onto a hillside — though pushed far it levels the lot and most of the excavation into the clearing, since the site is what the middle of this block is. The block\'s edges are never touched at any setting, so a levelled block still joins its neighbours exactly.',
  },
  erosion: {
    type: 'range', default: 0, min: 0, max: 1, step: 0.05, label: 'Weathering', affects: 'geometry',
    describe: 'Ages the site after the groundworkers leave: hollows silt up and soften while crests stay sharp, which is the difference between a box dug this morning and one that has stood open through a winter — the bench shoulders ravel and the bund slumps. It works on the platform as well, so past about 0.3 the working surface stops being flat, which is a defect on a live site and exactly right on a stalled one. At 1 it moves the ground by about 3 m either way and lowers the average by around a metre, because weathering removes material. The block edges are untouched at any setting, so a weathered block still joins its neighbours exactly.',
  },
  earth: {
    type: 'color', default: '#6B5842', label: 'Made ground',
    describe: 'Albedo of the dry ground of the block outside the hoarding: the brown made ground a city street is laid on, and the largest single area in the model. One flat matte tone; all apparent variation is the facet lighting.',
  },
  hardcore: {
    type: 'color', default: '#9BA5AB', label: 'Hardcore platform',
    describe: 'Albedo of the compacted crushed stone inside the hoarding: the whole piling platform, and the running surface of the haul ramp. It stops dead on the hoarding line rather than fading out, because that line is where the kit\'s hoarding bays stand — and a hard grey rectangle in brown ground is most of what makes this read as a site from above. It is the tone the kit\'s compacted hardcore tile matches.',
  },
  spoil: {
    type: 'color', default: '#D9C093', label: 'Dug material',
    describe: 'Albedo of everything that came out of the ground THIS WEEK: the spoil bund down to its toe, and the bench inside the excavation. It is also the underside of the block in island mode, because that is the same undisturbed subsoil the box is cutting into, seen from below. It is the lightest tone on the block, which is what makes the bund read as a bank rather than as a smudge along the hoarding, and it is deliberately NOT given to the old mounds outside the hoarding: material that has sat through twenty winters is the colour of the ground it was tipped on, and painting it as fresh spoil puts half a dozen pale blobs round the block that read as sand.',
  },
  mud: {
    type: 'color', default: '#4A3A2A', label: 'Churned mud',
    describe: 'Albedo of ground tracked over wet until nothing about it is dry: the bottom of the excavation, the belt of churned ground round the outside of the hoarding where the muck-away traffic runs, and the grubbed-out hollows more than about 1.6 m below the natural floor. It is also the rim wall of the block in island mode, which is this ground seen in section. It is far darker than any lit surface up on the platform, so the box reads as depth rather than as shadow, and its patches outside the site are what stop the open block reading as one flat brown.',
  },
  cut: {
    type: 'color', default: '#79838A', label: 'Cut face',
    describe: 'Albedo of any face steeper than 55 degrees, which on this ground is a short and specific list: both faces of the benched excavation, the side walls of the haul ramp where it is let into the platform, and any riser the level-stepping knob puts through the site. Raw exposed subsoil with the sheeting against it. Measured on the default island it is 5.5 per cent of everything facing the sky, the smallest share of the five tones, and that is the point: nothing else on a levelled city block is steep enough to be a cut face, so wherever this tone appears something has been dug.',
  },
};

/* ------------------------------------------------------------------ helpers */

const MODULE = 4;             // the kit's grid module, in metres — the terrace step
const STOREY = 3;             // the kit's storey; reported for a composer, never a step here

/* THE SITE IS PLACED on a world period rather than grown from noise, because a
 * hoarded lot is not a landform that happened: a developer bought a block and a
 * contractor pegged it out. Its phase is therefore ours to choose, and there is
 * no excuse for a default block that shows the corner of one.
 *
 * 152 m of world between sites means a 128 m block carries exactly one and the
 * NEXT block gets the next site rather than the same one again. The phase was
 * scanned rather than guessed: every 4 m phase of four candidate periods was
 * tried against the default chunk, keeping only those that put a lot within 8 m
 * of the middle of the block, held the whole 44 x 36 m rectangle at FULL island
 * mask (a corner outside the rim is a corner the store shot never shows) and put
 * no second site within 46 m of the block. 33 pairs survived; this one lands the
 * lot at (384, 320), which is dead on the middle of chunk (5, 4) at Size 2.
 *
 * The per-cell jitter stops a laid-out world reading as a chessboard of identical
 * building sites. It is hash2 of the cell, so both sides of any seam agree on it,
 * and the centre is snapped to a whole 4 m with the jitter included, so the lot's
 * own edges always land on the kit's module lines. */
const SITE_PERIOD = 152;
const SITE_OX = 88;
const SITE_OZ = 12;
const SITE_JITTER = 16;

/* THE LOT: 44 x 36 m, eleven bays by nine. Sized against the ISLAND rather than
 * against a planning drawing, because that is the measurement that decides the
 * store shot. The island rim on the default block runs between 26.5 and 46.5 m
 * from the middle depending on bearing, and a 44 x 36 m rectangle reaches 28.4 m
 * from its own centre — so the whole lot, its bund and its ramp head survive the
 * cut on every bearing that matters, and there is still a belt of open block
 * around them for the made ground to show in. */
const LOT_HX = 22, LOT_HZ = 18;
const LOT_FADE = 10;          // the apron over which the grading releases the land

/* THE EXCAVATION: 28 x 20 m, seven bays by five, leaving an 8 m working margin
 * inside the hoarding — which is not a spare number. It is where the tower
 * crane's base, the plant, the cabins and the bund all have to fit, and it is
 * what makes a tight city site tight.
 *
 * ONE BENCH, at exactly one module down, however deep the box goes. Benching
 * every 4 m is the obvious version and it is wrong here: each bench eats 5 m of
 * plan off all four sides, so a 10 m box would bench its own floor down to two
 * metres across and the depth knob would quietly stop doing anything anybody
 * could see. One bench at the module with the rest of the drop taken on the
 * second face keeps the floor at 18 x 10 m at every setting — and a deep box with
 * a propped near-vertical lower wall is what a deep box actually is. */
const DIG_HX = 14, DIG_HZ = 10;
const LIFT = MODULE;          // the bench sits one module below the formation level
/* Plan width of one cut face. 1.2 m of plan for a 4 m drop is 73 degrees, which
 * is a sheeted wall rather than a batter and is what a city basement has; with
 * the weight that switches the box on ramping over 1.5 m either side of it, the
 * face lands on about two quads at the default pitch, which is enough for it to
 * have an interior rather than being all edge. */
const RISER = 1.2;
const BERM = 2.6;             // plan width of the bench itself
const DIG_A = 6;              // metres of box at relief 1: two of the kit's 3 m storeys
/* Capped, and the cap is tied to the island's base slab below. An island hangs
 * its underside off the shared field and clamps anything below it back up, so a
 * box cut deeper than the slab is thick comes back with a floor it never had and
 * the top of the knob does nothing. */
const DIG_CAP = 10.5;

/* THE HAUL RAMP, let into the near wall of the box and out at the corner nearest
 * the pavement. 26 m of run for 6 m of drop is about 1 in 4: steep for a road,
 * normal for a site, and a dumper crawls it. It starts on the PLATFORM, eight
 * metres before the box, so it has proper cut side walls before it reaches the
 * hole — the detail that stops it reading as a notch in a wall. */
const RAMP_CZ = 6;            // its centreline, metres off the lot centre
const RAMP_HALF = 4;          // 8 m of corridor: one dumper and a walkway
/* Its side walls are 2 m of plan, not 1, and the number is set by the mesh rather
 * than by the earthworks. A wall one quad wide has no interior at the default
 * pitch: the quads straddling it are half wall and half platform, the cliff rule
 * decides per face, and the corridor comes back with a dotted grey edge instead
 * of a cut side. At 2 m the wall is two quads of its own and the ramp reads as
 * something let INTO the platform, which is what the first eight metres of it is. */
const RAMP_EDGE = 2;
const RAMP_HEAD = LOT_HX;     // where it leaves the platform: the lot's own edge
const RAMP_TOE = -4;          // where it reaches the floor
const RAMP_END = -9;          // and where it has run out, on the floor itself
const RAMP_TAIL = 5;

/* THE SPOIL BUND, inside the hoarding line and clear of the box. 7.6 m from toe
 * to toe with a 2.4 m flat trodden top, which is what makes it read as something
 * tipped rather than as a mound: a heap with a smoothly rounded crown is a hill.
 * Its centreline is 3.6 m in, so its outer toe falls a shade outside the hoarding
 * and the bank is cut off there rather than tapering into the street, which is
 * what a hoarding does to a heap. 2.2 m over a 2.6 m batter peaks at about 52
 * degrees, comfortably under the 55 the cliff rule catches, so loose dug material
 * does not come back painted as a quarried face. */
const BUND_IN = 3.6;          // its centreline, metres inside the hoarding line
const BUND_CROWN = 1.2;
const BUND_BATTER = 2.6;
const BUND_A = 2.2;           // metres at relief 1

const CLIFF_COS = 0.57358;    // cos 55 degrees, the same threshold cliffAngle uses

/** THE QUAD A FACE BELONGS TO, which is where a paint gate must be sampled.
 *
 *  Colour is decided per FACE, and a quad is two faces: their centroids sit at
 *  (1/3, 2/3) and (2/3, 1/3) of the cell, a third of a quad apart in BOTH axes.
 *  So any gate that changes appreciably over a third of a quad can come back
 *  TRUE for one triangle of a cell and FALSE for the other, and where that
 *  happens along a boundary the result is a one-quad sawtooth — the defect that
 *  put a row of pale teeth down the inner toe of the spoil bund and a row of
 *  loose lozenges along the box lip, which read as litter dropped on the site.
 *  Measured on a face map of the block: 21 quads in a row alternating
 *  spoil/hardcore where the gate happened to hover near its threshold.
 *
 *  Sampling the gate at the quad's own origin instead gives both faces of a cell
 *  the same answer by construction, at any vertex pitch, so a colour boundary can
 *  only ever fall on a quad EDGE. It costs nothing and it is exact rather than a
 *  tuned threshold, which is why it is here rather than in one feature: geometry
 *  (a slope, a box outline) still decides per face, because that is a real
 *  per-face property, but a noise gate is a statement about a patch of ground. */
function quadX(x, c) { return Math.floor(x / c.res) * c.res; }

/** Signed distance from an axis-aligned rectangle, in metres: negative inside,
 *  positive outside.
 *
 *  A BOX distance, not a radius, and that is the whole difference between a
 *  building site and a crater. Every contour of this function is the rectangle
 *  pushed in or out by a constant, which is exactly what a hoarding line, a
 *  piling platform and a benched basement box are. A plain Euclidean radius
 *  would have given a saucer with a site drawn on it. */
function boxD(dx, dz, hx, hz) {
  const qx = Math.abs(dx) - hx, qz = Math.abs(dz) - hz;
  const ax = qx > 0 ? qx : 0, az = qz > 0 ? qz : 0;
  return Math.sqrt(ax * ax + az * az) + Math.min(qx > qz ? qx : qz, 0);
}

/** The nearest site to a point: its cell, its centre snapped to the kit's 4 m
 *  grid, and the elevation its platform is graded to.
 *
 *  Cached per cell, because it is asked for at every vertex and every face
 *  centroid of the block, and because a formation level that varied with where
 *  you sampled it would not be a level. Keyed by the seed as well as the cell,
 *  since two world seeds are two different worlds — and it holds nothing that
 *  depends on a knob, or a second build at another setting would read the first
 *  one's site. */
const _sites = new Map();
function siteAt(x, z, c) {
  const i = Math.round((x - SITE_OX) / SITE_PERIOD);
  const j = Math.round((z - SITE_OZ) / SITE_PERIOD);
  const key = c.seed + ':' + i + ':' + j;
  const hit = _sites.get(key);
  if (hit !== undefined) return hit;

  const px = Math.round((i * SITE_PERIOD + SITE_OX + (c.hash2(i, j, c.seed + 401) - 0.5) * SITE_JITTER) / MODULE) * MODULE;
  const pz = Math.round((j * SITE_PERIOD + SITE_OZ + (c.hash2(i, j, c.seed + 402) - 0.5) * SITE_JITTER) / MODULE) * MODULE;

  /* THE PLATFORM SITS AT THE MEAN OF THE GROUND IT REPLACES, sampled on the
   * lot's own nine points. The mean is what makes the grading a cut on the high
   * side and a fill on the low: taken at the lowest corner the whole site would
   * sit in a pit, and at the highest it would stand on a table and the median
   * relief over the block would walk off zero. Snapped to a whole 25 cm, because
   * everything put down on it should land on a round number. */
  let sum = 0;
  for (let b = -1; b <= 1; b++) {
    for (let a = -1; a <= 1; a++) sum += c.baseHeight(px + a * LOT_HX, pz + b * LOT_HZ, c.seed);
  }
  const s = { i, j, x: px, z: pz, level: Math.round((sum / 9) * 4) / 4 };
  _sites.set(key, s);
  return s;
}

/** How completely the ground here is the contractor's platform: 1 anywhere inside
 *  the hoarding line, releasing to 0 over the apron outside it. */
function lotW(x, z, s, c) {
  const d = boxD(x - s.x, z - s.z, LOT_HX, LOT_HZ);
  if (d <= 0) return 1;
  return c.smooth(c.clamp(1 - d / LOT_FADE, 0, 1));
}

/** Metres of cut below the formation level, `u` metres inside the box outline.
 *
 *  Written out as a profile in METRES rather than as fractions of a width: a
 *  face, a bench, a second face, a floor. The first face drops a whole module and
 *  the second takes whatever is left, so the bench is always on a module line and
 *  the floor is always 18 x 10 m. */
function benchCut(u, D, c) {
  if (u <= 0 || D <= 0) return 0;
  const first = D < LIFT ? D : LIFT;
  let h = first * c.smooth(c.clamp(u / RISER, 0, 1));
  if (D > LIFT) h += (D - LIFT) * c.smooth(c.clamp((u - RISER - BERM) / RISER, 0, 1));
  return h;
}

/** The haul ramp: how completely this point is on it, and how far down its own
 *  run it has got. `t` is a plain LINEAR fall, because a ramp is a plane — a
 *  smoothstepped ramp is flat at both ends and twice as steep in the middle,
 *  which is a thing nothing could drive. */
function rampAt(x, z, s, c, on) {
  if (on <= 0) return { w: 0, t: 0 };
  const across = c.smooth(c.clamp((RAMP_HALF - Math.abs(z - s.z - RAMP_CZ)) / RAMP_EDGE, 0, 1));
  if (across <= 0) return { w: 0, t: 0 };
  const lx = x - s.x;
  /* It runs OUT on the floor rather than ending in a wall. Without the tail the
   * ramp's own plane would still be claiming ground at the far end of the box,
   * where the floor is already lower than the ramp, and the corridor would read
   * as a shelf running into the far face. */
  const along = c.smooth(c.clamp((lx - RAMP_END) / RAMP_TAIL, 0, 1));
  return { w: across * along * on, t: c.clamp((RAMP_HEAD - lx) / (RAMP_HEAD - RAMP_TOE), 0, 1) };
}

/** Which sides of the lot the bund is heaped along: the two with no gate on them,
 *  blended round the corner between them so the bank wraps and runs out rather
 *  than stopping at a line. `w` comes back with it, because which way the bank is
 *  RUNNING decides which way its gaps are allowed to vary.
 *
 *  The x-side and z-side tests flip at the lot's own centrelines, which are the
 *  one place on the block where this weight has no say at all — a point level with
 *  the middle of the lot is 18 or 22 m from the nearest boundary and the bund's
 *  own profile is already zero there. So the two sides may differ completely
 *  without putting a step in the height field. */
function bundRun(x, z, s, c) {
  const qx = Math.abs(x - s.x) - LOT_HX;
  const qz = Math.abs(z - s.z) - LOT_HZ;
  const w = c.smooth(c.clamp((qx - qz) / 8 + 0.5, 0, 1));    // 1 on an x side, 0 on a z side
  const xs = c.smooth(c.clamp(-(x - s.x) / 6 + 0.5, 0, 1));
  const zs = c.smooth(c.clamp(-(z - s.z) / 6 + 0.5, 0, 1));
  return { side: c.lerp(zs, xs, w), w };
}

/** WHERE THE BANK IS AND WHERE THE GAPS ARE, as a ONE-DIMENSIONAL field sampled
 *  along the run: on the lot's x edges the bank runs in z, so its gaps vary with
 *  z and with nothing else, and on the z edges the other way round. The two are
 *  blended with the same weight that wraps the bank round the corner.
 *
 *  A PLAIN 2-D NOISE HERE IS WHAT PUT A ROW OF PALE DIAMONDS ALONG THE BANK, and
 *  it took a mesh dump to see why. Sampled in two dimensions, this field's own
 *  contours cross the bank at whatever angle they like — and where one happens to
 *  run PARALLEL to a 5 m bank, the ground that passes the gate is a strip one quad
 *  wide. One quad is the width at which a colour decided per FACE stops having an
 *  interior: each quad's two triangles sit either side of the line and the sliver
 *  comes back as a dotted row of lozenges, which reads as litter dropped along the
 *  hoarding. In one dimension the gaps can only ever be gaps ACROSS the bank,
 *  which is what a gap in a bund is: the place the dumper could not reach. */
function bundLump(x, z, w, c) {
  /* 13 m, and the period was measured against the RUN rather than chosen. A
   * hoarding side is 44 m long, so at 19 m it sees two cycles: measured, the bank
   * then covered 91 and 94 per cent of the two sides it is heaped on, which is
   * a kerb laid round the lot and the very thing this gate exists to prevent. At
   * 13 m the runs see three and a half cycles and come out 61 and 56 per cent
   * covered — one long bank with a nine-metre gap in it down the side with room,
   * and a shorter length with a stub at the end on the other. */
  const alongX = c.fbm(x / 13, 6.5, c.seed + 643, 2);   // a bank running in x
  const alongZ = c.fbm(9.5, z / 13, c.seed + 647, 2);   // a bank running in z
  return c.smooth(c.clamp((c.lerp(alongX, alongZ, w) - 0.42) / 0.26, 0, 1));
}

/** THE MADE GROUND the block is built on: TWO TAILS OF ONE FIELD — mounds where
 *  the last demolition was tipped, hollows where the last basement was grubbed
 *  out and never filled, and a dead band between them where the ground is exactly
 *  what it always was.
 *
 *  A ONE-TAILED GATE IS NOT ENOUGH HERE, and the measurement is the reason. Every
 *  other kit gates its raising term against a broad second noise and quotes a
 *  median relief near zero; on this block that fails twice over. `fbm` is
 *  normalised over the WORLD rather than over one chunk, and the patch of it the
 *  default block cuts sits high — measured on chunk (5, 4) at Size 2 this field
 *  runs p25 0.513, p50 0.667, p75 0.853 — so the usual threshold near 0.46 fires
 *  on most of the block and takes the median relief metres off zero: a raised
 *  plateau with texture on it, which is the exact failure the gate exists to
 *  prevent. And even a correctly placed one-tailed threshold cannot hold the
 *  MEDIAN, because adding a positive-only term to a third of a block drags its
 *  50th percentile up by whatever its own median lift is.
 *
 *  Two tails fix it for nothing, and they are also simply more truthful: made
 *  ground is not mounds in a lawn, it is what is left after somebody knocked a
 *  building down and took the cellar out, which is mounds AND holes. Measured at
 *  these numbers — mounds 28.2 per cent of the 64 m chunk and 35.3 of the 128 m
 *  block, hollows 28.6 and 38.8, so neither tail empties as the size knob comes
 *  down, which a 2-octave version of this field did. */
const RISE_UP = 0.76;         // above this the ground was tipped on
const RISE_DOWN = 0.60;       // below it the ground was taken out
const RISE_W = 0.20;
function riseAt(x, z, c) {
  const n = c.fbm(x / 31, z / 31, c.seed + 907, 3);
  return { up: c.smooth(c.clamp((n - RISE_UP) / RISE_W, 0, 1)),
           down: c.smooth(c.clamp((RISE_DOWN - n) / RISE_W, 0, 1)) };
}

/** And the lumpiness across both: ground that has been tracked over, broken out
 *  and filled back in twice. SIGNED and mean-zero, so it is texture rather than
 *  one more thing lifting the block.
 *
 *  6.5 m, and the wavelength is set against the VERTEX PITCH rather than against
 *  anything a lorry does. At 1 m a 6.5 m cycle is six or seven quads, which is a
 *  facet the flat shading can catch; the first cut was 7 m at half this amplitude
 *  and the whole block outside the hoarding came back as smooth brown pillows
 *  with nothing for the light to break on, which is countryside and not a city
 *  block that has been built over twice. */
function trackAt(x, z, c) {
  return c.fbm(x / 6.5, z / 6.5, c.seed + 613, 2) - 0.5;
}

/** WHERE THE BLOCK HAS BEEN CHURNED TO MUD, which is a PAINT and not a shape.
 *
 *  Without it the ground outside the hoarding is one flat brown over more than
 *  half the picture — the failure the contract names outright, a terrain that is
 *  one tone with a rim round it — because mounds and hollows are shape and nothing
 *  else once the pale band comes off their crowns.
 *
 *  It is keyed to the SITE as well as to a broad patch field, because what churns
 *  a city block is the traffic in and out of the gate: the weight rises within
 *  about 34 m of the hoarding, so the mud is thickest round the site and thins to
 *  open ground at the far corners of the block, where a muck-away lorry has never
 *  been. Measured at these numbers it takes 26.7 per cent of the open ground on
 *  the 64 m chunk and 27.7 on the 128 m block, so the size knob does not wash it
 *  out. */
function churnAt(x, z, s, c) {
  const d = boxD(x - s.x, z - s.z, LOT_HX, LOT_HZ);
  const near = c.smooth(c.clamp(1 - d / 34, 0, 1));
  return c.fbm(x / 25, z / 25, c.seed + 811, 2) + 0.22 * near;
}
const CHURN = 0.66;

/* -------------------------------------------------------------- the factory */

export function createAsset(o = {}) {
  const p = {};
  for (const k of Object.keys(params)) p[k] = o[k] !== undefined ? o[k] : params[k].default;
  // A colorway sets the zones the caller did not name, so picking a scheme and
  // then overriding one colour both work.
  const cw = COLORWAYS[p.colorway] ?? COLORWAYS['working-site'];
  for (const z of ZONES) if (o[z] === undefined) p[z] = cw[z];

  // Vertex colour is LINEAR; the hex above is sRGB. THREE.Color converts on
  // construction, so read .r/.g/.b and never convert again.
  const rgb = (hex) => { const c = new THREE.Color(hex); return [c.r, c.g, c.b]; };

  /* A city block at about +21.5 m, falling 10.6 m across the default 128 m block
   * and 4.1 m across the lot alone. It is deliberately NOT one of the flattest
   * chunks in the world: the piling platform is an earthwork that exists to fight
   * a fall, and on ground that was already level the cut and fill would have
   * nothing to do and the knob would be invisible. The NEAR corner is the high
   * one — 3.1 m above the far one, measured at +/-38 m along the hero camera's own
   * bearing — so the near edge of the lot is a cut and the camera looks over a low
   * bank into the hole rather than at an embankment standing in front of it.
   * Nothing here is within 14 m of sea level, so no part of a city block reads as
   * foreshore. The lot phase above was scanned against this chunk. */
  const cx = o.cx ?? 5, cz = o.cz ?? 4;
  const seed = o.seed ?? 1;
  const res = Number(p.res);
  const relief = Number(p.relief);

  const kP = Number(p.platform), kD = Number(p.dig), kR = Number(p.ramp);
  const kB = Number(p.bund), kG = Number(p.rubble), kT = Number(p.terrace);

  const DIG = clampNum(DIG_A * relief * kD, 0, DIG_CAP);
  const BUND_H = BUND_A * relief * kB;
  const RISE_H = 2.4 * relief * kG;
  const SCOOP_H = 1.8 * relief * kG;
  const TRACK_H = 1.0 * relief * kG;

  /* The three GRADINGS come on quickly and then leave the knob to the depth,
   * because each of them is a statement about WHERE the ground is rather than
   * about how far: a box that is only a third levelled is a tilted box. */
  const platOn = clampNum(kP, 0, 1);
  const digOn = clampNum(kD * 4, 0, 1);
  const rampOn = clampNum(kR * 4, 0, 1);

  /* The feature helpers only reach for the shared noise, so a minimal context is
   * enough to evaluate them outside buildChunk — which the building plot needs,
   * because it has to know where the lot is before there is a mesh to ask. */
  const ctx0 = { seed, fbm, ridge, vnoise, hash2, baseHeight,
                 clamp: (v, a, b) => (v < a ? a : v > b ? b : v),
                 smooth: (t) => t * t * (3 - 2 * t), lerp: (a, b, t) => a + (b - a) * t };

  /* THE GRADINGS, WORKED OUT TOGETHER, because feature heights ADD.
   *
   * Both the platform and the excavation say "put the ground at MY level here",
   * which each expresses as (my level - the bare field) — and two of those landing
   * on the same square metre lift it by the sum of the two, which is a spike
   * several metres tall around the whole lip of the box. Resolving them against
   * each other keeps the pair at no more than 1 everywhere, so where the box takes
   * over from the platform the two expressions agree exactly: at the box outline
   * the cut is zero and both say `level`.
   *
   * The box wins, because it is the thing that was dug and the platform is what
   * gives way. The ramp is folded into the box's own cut rather than being a third
   * claimant, because a ramp is not a separate earthwork — it is the shape of one
   * wall of the hole.
   *
   * ONE-SLOT MEMO, because five features and then the paint pass all ask for this
   * at the same point, one after another. It lives inside createAsset and not at
   * module scope on purpose: every weight in it depends on a knob, so a
   * module-level cache would hand the next build the last one's site. */
  let _gx = NaN, _gz = NaN, _gv = null;
  const grade = (x, z, c) => {
    if (x === _gx && z === _gz) return _gv;
    const s = siteAt(x, z, c);
    const lw = lotW(x, z, s, c);
    const r = rampAt(x, z, s, c, rampOn);
    /* The ramp is held inside the lot by the platform's own weight. Left
     * unbounded its corridor ran on past the hoarding and levelled a rectangular
     * tongue of the street outside the site, which is a slip road nobody asked
     * for. */
    const rw = r.w * lw;
    const pitU = -boxD(x - s.x, z - s.z, DIG_HX, DIG_HZ);
    const dig = digOn <= 0 ? 0
      : Math.max(c.smooth(c.clamp(pitU / 1.5, 0, 1)), rw) * digOn;
    const plat = platOn <= 0 ? 0 : lw * platOn * (1 - dig);
    _gx = x; _gz = z;
    _gv = { s, lw, rw, pitU, dig, plat, t: r.t };
    return _gv;
  };

  /** What the excavation would be with no ramp in it, and what it is with one.
   *  Kept as two numbers because the two features below split them: the box owns
   *  the benched cut and the ramp owns the difference, so each knob moves its own
   *  geometry and the pair still sums to one continuous surface. */
  const bench = (g, c) => benchCut(g.pitU, DIG, c);
  const cutAt = (g, c) => (g.dig <= 0 ? 0 : c.lerp(bench(g, c), DIG * g.t, g.rw));

  /** HOW MUCH BANK THERE IS HERE, before the section across it is applied: which
   *  side of the lot the point is on, where the gaps between the tipped lengths
   *  fall, and the excavation holding the heap off its own lip.
   *
   *  IN LENGTHS, NOT AS A KERB, and this is the half of it that decides the
   *  picture. A bank of even height running the whole way along the hoarding reads
   *  as one more concentric rectangle, and with the benched box inside it the
   *  block comes back as a stepped pyramid seen from above rather than as a
   *  building site: bund, platform, bench, floor, four rings one inside the next.
   *  Dumpers do not tip a kerb — they tip in lengths where the plant can reach,
   *  with gaps where it crosses.
   *
   *  Nothing in here depends on how far ACROSS the bank the point is, which is
   *  what lets the paint sample it on the quad grid (see quadX) while the height
   *  samples it exactly. */
  const bundGate = (x, z, g, c) => {
    const r = bundRun(x, z, g.s, c);
    return r.side * bundLump(x, z, r.w, c) * (1 - c.clamp(g.dig * 2, 0, 1));
  };

  /** And the bank itself: the section across it, times that gate. Held off the
   *  excavation, which it never reaches on the default lot but would on a box a
   *  buyer has widened by hand. */
  const bundAt = (x, z, g, c) => {
    if (BUND_H <= 0) return 0;
    const d = -boxD(x - g.s.x, z - g.s.z, LOT_HX, LOT_HZ);
    if (d <= 0) return 0;                       // it stops at the hoarding line
    const across = c.smooth(c.clamp((BUND_CROWN + BUND_BATTER - Math.abs(d - BUND_IN)) / BUND_BATTER, 0, 1));
    return across <= 0 ? 0 : across * bundGate(x, z, g, c);
  };
  /* WHERE THE DUG-MATERIAL TONE STOPS, in metres inside the hoarding line rather
   * than at a contour of the bank's own weight. The lot's edges are at 4k + 2, so
   * a band that runs from the line to 8 m inside it has both its edges on even
   * world coordinates on all four sides — vertex lines at the 1 m and 2 m pitches,
   * so the colour boundary falls on a quad edge. The bank's own toe is at 7.4 m,
   * which leaves the last 60 cm of the band flat: that reads as what the loader
   * dropped short of the heap, and it costs one quad. */
  const BUND_PAINT = 8;

  const features = [
    /* THE MADE GROUND FIRST, and graded away under everything the contractor
     * levelled: a rise left standing under a flat platform is a bump in the middle
     * of a site. `1 - w` on its own is not enough — it only reaches zero at the
     * dead centre of the lot, so the outer half of the platform comes back with
     * rises still in it. Doubling the weight clears the whole levelled area and
     * still releases smoothly across the apron, which is what keeps the height
     * field continuous. */
    {
      name: 'made-ground',
      height: (x, z, c) => {
        if (RISE_H === 0 && TRACK_H === 0) return 0;
        const g = grade(x, z, c);
        const off = 1 - c.clamp((g.plat + g.dig) * 2, 0, 1);
        if (off <= 0) return 0;
        const r = riseAt(x, z, c);
        return (r.up * RISE_H - r.down * SCOOP_H + trackAt(x, z, c) * TRACK_H) * off;
      },
      /* The churned belt, and NOTHING ELSE — everywhere else it hands back null
       * and lets the bands say what the ground is. It is the FIRST feature in the
       * list, so it is the last one the painter asks, and it can never paint over
       * the platform, the bund, the box or the ramp; inside the hoarding it never
       * gets the chance anyway, because the platform is a feature above it.
       *
       * It is also the one paint on this block that survives `relief` 0, because
       * whether ground has been driven into mud is not a question about vertical
       * exaggeration. */
      paint: (x, z, h, ny, c) => {
        if (kG <= 0.15) return null;
        const qx = quadX(x, c), qz = quadX(z, c);   // per quad, not per face
        return churnAt(qx, qz, siteAt(qx, qz, c), c) > CHURN ? rgb(p.mud) : null;
      },
    },
    /* THE PILING PLATFORM. Its levelling is NOT multiplied by relief: cutting a
     * working platform flat is not a matter of vertical exaggeration, and a
     * `relief` of 0 should still leave somewhere a crawler could stand.
     *
     * PAINTED EXPLICITLY, and this is the one place a feature has to do the bands'
     * job itself. The bands measure relief against the bare world field, and the
     * grading moves the ground metres away from that field by design: the high
     * half of the lot sits BELOW the block it was cut out of and the low half
     * stands above it, so banded on the raw difference the platform comes out as a
     * brown trench joined to a pale mound down the middle of the site. A hardcore
     * platform is hardcore whichever way the block fell before anybody graded it,
     * and it says so — right up to the hoarding line and not a centimetre past it,
     * because that line is where the hoarding bays stand and it lands on a tile
     * edge, so the colour boundary falls on a quad edge rather than through the
     * middle of one. */
    {
      name: 'piling-platform',
      height: (x, z, c) => {
        const g = grade(x, z, c);
        return g.plat <= 0 ? 0 : g.plat * (g.s.level - c.baseHeight(x, z, c.seed));
      },
      paint: (x, z, h, ny, c) => {
        if (platOn <= 0.05) return null;
        const s = siteAt(x, z, c);
        if (boxD(x - s.x, z - s.z, LOT_HX, LOT_HZ) > 0) return null;
        // The cut bank at the high edge of the lot is a cut face like any other.
        return ny < CLIFF_COS ? rgb(p.cut) : rgb(p.hardcore);
      },
    },
    /* The bund AFTER the platform, because paint is last-wins and a bank heaped on
     * the hardcore is still the bank. Painted DOWN TO ITS TOE rather than from its
     * crown — a threshold up on the flat top puts the colour boundary on the
     * batter, which is under three metres of plan, so the tone stops halfway down
     * a bank that is plainly all one material — and painted on the section rather
     * than on the finished weight, for the reason given at bundAt above. */
    {
      name: 'spoil-bund',
      height: (x, z, c) => (BUND_H <= 0 ? 0 : bundAt(x, z, grade(x, z, c), c) * BUND_H),
      paint: (x, z, h, ny, c) => {
        if (kB <= 0.15 || BUND_H <= 0.05) return null;
        /* The BAND is read at the face and the GATE at its quad. The band's own
         * edges are the hoarding line and a line 8 m inside it, both of which are
         * even world coordinates on all four sides and so fall on vertex lines;
         * the gate is noise and is sampled per quad, for the reason at quadX. */
        const g = grade(x, z, c);
        const d = -boxD(x - g.s.x, z - g.s.z, LOT_HX, LOT_HZ);
        if (d <= 0 || d >= BUND_PAINT) return null;
        return bundGate(quadX(x, c), quadX(z, c), g, c) > 0.3 ? rgb(p.spoil) : null;
      },
    },
    /* THE EXCAVATION, and it carries its own levelling for the same reason the
     * platform does: a box whose floor followed the slope of the block would not
     * be a basement, it would be a ditch. At the box outline the cut is zero and
     * this expression equals the platform's exactly, which is what lets the two
     * hand over without a step. */
    {
      name: 'excavation',
      height: (x, z, c) => {
        const g = grade(x, z, c);
        return g.dig <= 0 ? 0
          : g.dig * ((g.s.level - bench(g, c)) - c.baseHeight(x, z, c.seed));
      },
      paint: (x, z, h, ny, c) => {
        if (digOn <= 0 || DIG <= 0.05) return null;
        const g = grade(x, z, c);
        if (g.rw > 0.5 || g.pitU <= 0) return null;   // the ramp paints its own
        if (ny < CLIFF_COS) return rgb(p.cut);        // both faces of the bench
        /* EVERYTHING FLAT INSIDE THE BOX IS MUD, bench and floor alike, and the
         * bench being the same tone as the floor is a fix rather than an
         * economy. Painted as fresh dug material it was pale, and the block then
         * came back as four nested rectangles — bund, platform, bench, floor —
         * which from above is a ziggurat and not an excavation. There is one
         * pale family on this block and it is the bund: material that has come
         * UP out of the hole. Anything still in the hole has been tracked over
         * by an excavator all week, and the bench now reads by its own shadow
         * between two grey sheeted faces, which is how you read one on a site. */
        return rgb(p.mud);
      },
    },
    /* AND THE RAMP LAST OF ALL, so the way in beats everything: a corridor cut
     * through a bench is still the corridor, and its side walls are still cut face
     * where they are let into the platform. Its height is the DIFFERENCE between
     * the benched wall and the ramp's own plane, so the two features sum to one
     * surface and turning this knob to 0 restores the wall exactly. */
    {
      name: 'haul-ramp',
      height: (x, z, c) => {
        const g = grade(x, z, c);
        return g.dig <= 0 || g.rw <= 0 ? 0 : g.dig * (bench(g, c) - cutAt(g, c));
      },
      paint: (x, z, h, ny, c) => {
        if (digOn <= 0 || rampOn <= 0 || DIG <= 0.05) return null;
        const g = grade(x, z, c);
        if (g.rw <= 0.5) return null;
        return ny < CLIFF_COS ? rgb(p.cut) : rgb(p.hardcore);
      },
    },
  ];

  /* Bands are metres of relief above the natural block, not absolute height, so
   * the same module reads correctly on the high side of the site and on the low.
   * Everything the contractor made paints itself, above, for the reason given
   * there — so what is left for the bands is exactly the ground OUTSIDE the
   * hoarding, and they are DELIBERATELY SHORT: two facts about it and no more.
   *
   * THERE IS NO PALE BAND ON THE HIGH GROUND, and leaving it out is what makes
   * this block read as a city block rather than as countryside. Cut anywhere above
   * about 1.4 m, the mounds of old made ground come out capped in the same tone as
   * the spoil bund — big pale blobs with a contour drawn round each of them, which
   * read as sand dunes, and which leave the one thing on this block that IS
   * freshly dug material competing with half a dozen things that are not. Tipped
   * material that has sat through twenty winters is the colour of the ground it
   * was tipped on. So the mounds show by their own shading, the dug-material tone
   * belongs to the bund and to the bench inside the box and to nothing else, and
   * the site is the only pale thing in the picture.
   *
   * The lower cut sits at -1.6 rather than at the usual -0.5 because the
   * platform's apron releases about a metre of cut over its outer half: at -0.5
   * that comes back as a dark ring drawn round the whole site, which reads as a
   * moat rather than as a graded edge. At -1.6 it catches the grubbed-out hollows
   * and nothing else.
   *
   * THE MEASURED DISTRIBUTION these cuts were chosen against. Relief above the
   * shared field, sampled at 1 m over the default 128 m block: min -6.75, p05
   * -2.16, p25 -1.03, p50 -0.02, p75 1.19, p95 2.62, max 4.20. The median is the
   * number that matters and it is two centimetres: what this kit digs out it also
   * heaps up, which is what a building site is. Taken over the ground OUTSIDE the
   * lot alone the median is +0.01 with a quarter of it below -0.95 and a quarter
   * above 1.56, so the open block straddles zero rather than sitting on one side
   * of it, and the mud band at -1.6 lands inside its bottom tenth. It holds as the
   * knobs move: median -0.03 at relief 1.8 and -0.03 at made ground 1.6.
   *
   * The tone shares that come out of it, by face area on the default island:
   * churned mud 39.0%, made ground 36.9%, hardcore 12.9%, dug material 5.7%, cut
   * face 5.5%. The three tones that only the contractor makes are a quarter of
   * everything facing the sky, which is a hoarded lot in a city block rather than
   * a landscape with a scratch in it. */
  const datum = baseHeight((cx + 0.5) * CHUNK, (cz + 0.5) * CHUNK, seed);
  const bands = [
    { upTo: -1.6, color: rgb(p.mud) },
    { upTo: Infinity, color: rgb(p.earth) },
  ];

  /* THE PLOT. A whole number of the kit's own 4 m modules, FOUND in the flattest
   * low-slope ground the block has rather than stamped through the middle of it —
   * which on this block is the difference between a building plot and a rectangle
   * levelled through the excavation. Clamped rather than refused: the core throws
   * out a pad that would reach the 4 m skirt, and refusing a setting a buyer can
   * reach is worse than quietly fitting it. */
  const W = CHUNK * clampNum(Math.round(Number(p.span)), 1, 4);
  const padBlend = 3;
  const padMax = Math.max(0, Math.floor((W - 2 * (SKIRT + padBlend) - 2) / MODULE)) * MODULE;
  const padSize = Math.min(Math.round(Number(p.pad) / MODULE) * MODULE, padMax);

  const t = buildChunk({
    /* PADS ARE ALSO THE COMPOSITION'S CALL. The `pad` knob finds one plot for a
     * buyer who wants somewhere level outside the hoarding, and anything a diorama
     * places itself is appended. */
    pads: o.pads ?? [],
    flats: padSize > 0 ? 1 : (o.flats ?? 0),
    flatSize: padSize > 0 ? padSize : o.flatSize,
    flatSnap: o.flatSnap ?? MODULE,
    /* And not on the site. A plot levels the ground it covers, so one laid across
     * the lot fills the excavation in and flattens the bund with it — and there is
     * nothing to gain, because the piling platform is already 44 x 36 m of
     * dead-level ground. */
    flatAvoid: (x, z, r) => {
      const s = siteAt(x, z, ctx0);
      return boxD(x - s.x, z - s.z, LOT_HX, LOT_HZ) < r + LOT_FADE * 0.5;
    },
    cx, cz, seed, res,
    span: Number(p.span),
    edges: p.edges,
    flatten: Number(p.flatten),
    erosion: Number(p.erosion),
    features,
    /* MODULE STEPPING, applied to the ACCUMULATED height, which is what terracing
     * means: it is a statement about the total, not one more displacement. The
     * platform and the floor of the box are already level, so snapping moves each
     * of them as one shelf and they stay level at every setting.
     *
     * Held off the ramp and off the bund. A stepped ramp is a staircase and a
     * quantised heap is a flight of contour rings, and both are what a blanket
     * quantiser does to a site if nothing stops it. */
    shape: kT > 0 ? (h, x, z, c) => {
      const g = grade(x, z, c);
      if (g.rw > 0.15) return h;
      const w = kT * (1 - c.clamp(bundAt(x, z, g, c) * 2, 0, 1));
      return w <= 0 ? h : c.lerp(h, Math.round(h / MODULE) * MODULE, w);
    } : null,
    bands,
    bandBy: 'relief',
    blend: o.blend ?? null,   // neighbour tones, wired by whoever lays the blocks
    /* 55 degrees, and every batter on the block is sized against it rather than
     * the other way round. The two faces of the excavation are a 4 m and a 2 m
     * step over 1.2 m of plan, which peak near 79 and 68 degrees, so both come out
     * as cut face the whole way along; the ramp's side walls are the same; and the
     * bund's batter peaks near 52, so loose dug material stays dug material
     * instead of reading as something quarried. */
    cliff: rgb(p.cut),
    cliffAngle: 55,
    /* NO WATER, and that is a statement about this ground rather than an omission.
     * The water on a building site is in the ruts and in the bottom of the trench,
     * and both of those are PARTS with their own knobs — the mud-and-rut tile
     * floods its ruts, and the trench run holds what it holds. Standing water in
     * the middle of a city block being built is the generic grass-rock-and-pond
     * landscape three interior kits arrived at, not this one, and a basement box
     * with a lake in it is a site that has stopped work. */
    water: null,
    padBlend,
    /* THE SLAB GROWS WITH THE BOX, because it has to be thicker than the deepest
     * thing cut into this ground: an island hangs its underside off the shared
     * field and clamps anything below it back up, which on an excavation means the
     * floor rises, the box fills and the last third of the depth knob does nothing
     * anybody could see. At the defaults the box is 6 m and the grading cuts about
     * 1.4 m more on the high side, so 7.9 clears the measured deepest point with
     * room to spare.
     *
     * AND KEPT AS THIN AS THAT WILL ALLOW, because every centimetre of slab is
     * another centimetre of rim wall in the picture: at 9.5 the underside was half
     * the block's whole vertical extent and the hero read as a tall brown plinth
     * with a site on the lid. The ceiling is there because a 20 m slab is a wall
     * around the picture whatever the excavation knob says. */
    skirtDepth: clampNum(DIG + 1.9, 5, 14),
    /* THE RIM WALL is the churned-mud tone, and the choice is a value one rather
     * than a geological one. The cut-face grey would have been the obvious answer,
     * and it sits close enough to the made ground standing on top of it that the
     * two would merge along the whole outline of the island and the block would
     * lose its own edge. The mud is several steps down from the made ground and
     * reads as a cut through it. */
    skirtColor: rgb(p.mud),
    /* The UNDERSIDE is the dug-material tone and not the made-ground one, for the
     * same reason the bench inside the box is: what lies under a city block's made
     * ground is the undisturbed subsoil the excavation is cutting into, and it is
     * the pale thing in this palette. It is also the only tone bright enough to
     * survive down there — an underside catches almost no light, and with the
     * brown on it the low view came back as a solid black silhouette. */
    underColor: rgb(p.spoil),
  });

  const geo = new THREE.BufferGeometry();
  geo.setAttribute('position', new THREE.Float32BufferAttribute(t.positions, 3));
  geo.setAttribute('color', new THREE.Float32BufferAttribute(t.colors, 3));
  geo.computeVertexNormals();

  const mesh = new THREE.Mesh(geo, new THREE.MeshStandardMaterial({
    vertexColors: true, flatShading: true, roughness: 0.97, metalness: 0,
  }));
  mesh.name = 'site-ground';

  const g = new THREE.Group();
  g.name = 'construction-site-terrain';
  g.add(mesh);

  /* A lone island presents like every other asset in the catalogue: centred on
   * the origin, its middle on y = 0. A TILE keeps its true world position, because
   * that position IS the contract with its neighbours. */
  if (p.edges === 'island') {
    const mid = t.width / 2;
    g.position.set(-(cx * CHUNK + mid), -t.heightAt(cx * CHUNK + mid, cz * CHUNK + mid), -(cz * CHUNK + mid));
  }

  /* WHERE THE SITE ACTUALLY IS, in the block's own world coordinates, and what
   * the contractor levelled it to. A composition standing this kit on its own
   * ground needs all of it: the hoarding runs on the lot outline, the crane base
   * and the cabins go in the working margin between that outline and the box, the
   * core section goes in the box, and the dumpers come up the ramp. */
  const site = siteAt(cx * CHUNK + t.width / 2, cz * CHUNK + t.width / 2, ctx0);

  g.userData.terrain = {
    chunk: CHUNK, span: t.span, width: t.width, groundColor: t.groundColor, colorAt: t.colorAt, res, cx, cz, seed, edges: p.edges, datum,
    /* The RESOLVED knobs this block was built from, so a composition standing on
     * this ground can reproduce exactly the site it was laid out on rather than
     * the terrain's defaults. */
    params: { ...p },
    module: MODULE, storey: STOREY,
    lot: { x: site.x, z: site.z, w: LOT_HX * 2, d: LOT_HZ * 2, level: site.level },
    dig: { x: site.x, z: site.z, w: DIG_HX * 2, d: DIG_HZ * 2, depth: DIG, bench: Math.min(LIFT, DIG) },
    heightAt: t.heightAt, slopeAt: t.slopeAt, scatter: t.scatter, seam: t.seam, pads: t.pads,
    // Placement queries: where can I build, and what does the ground do along a line.
    flatAreas: t.flatAreas, profile: t.profile,
  };
  return g;
}

/** The core keeps its own `clamp` private, and both the depth cap and the plot
 *  cap need one before buildChunk has handed a context back. */
function clampNum(v, lo, hi) { return v < lo ? lo : v > hi ? hi : v; }

export default createAsset;
