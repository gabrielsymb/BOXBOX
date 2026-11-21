# Construction Site

**Building with this? Read `API.md` (one paragraph: the whole API of kit.mjs, stage.mjs and clips.mjs) and stop.**
Three lines build the place: `import { buildKit } from './kit.mjs'; const place = await buildKit({ folder: './' }); scene.add(place.group);`. The rest of this file is reference.

https://polyfork.dev/kit/construction-site-9e40b0

62 of 62 parts, packed 2026-11-17.
Kits fill up over time: if this says fewer parts than the site does, re-download.

## What is in this folder

Every part twice: `<part>.glb` for any engine, `<part>.mjs` as a drop-in three.js
module with its options. `scene.glb` is the composed kit scene, the whole thing
arranged as one file.

BUILD THE WHOLE PLACE IN ONE CALL. `kit.mjs` in this folder does it:

```js
import { buildKit } from './kit.mjs';          // serve this folder over http
const place = await buildKit({ folder: './' });   // all 252 placements, merged to a few draw calls
scene.add(place.group);                        // then place.tick(elapsedSeconds) in your loop
```

You get `place.bounds`, `place.boxes()` (a Box3 per placement tall enough to walk into),
`place.of(key)`, `place.terrain` with `place.groundAt(x, z)`, and the placed people idling.
LIGHT IT WITH `stage.mjs`, not with ACES tone mapping at exposure 1: these colours are authored for
no tone mapping under a hemisphere, a warm key and a cool rim, and the usual tutorial rig turns them
muddy. `const look = stage(renderer, scene, { time: 'day' }); look.shadows(place.group);
look.ground(place); await place.surround(); look.follow(player);` then `look.update()` each frame.
`look.setTime(0..1)` runs day to night. `place.surround()` scatters the kit's own trees past its edge.

`clips.mjs` makes any of the humanoids walk: `const anim = await animate(character);
anim.locomotion(metresPerSecond)`. The page needs an import map for `three` and `three/addons/`.

`LAYOUT.json` is the composition: 252 placements of 63 parts, the arrangement
every picture of this kit was taken in. Each row is a part key, a position in
metres (`x`,`y`,`z`, Y up), rotations in degrees (`ry` about Y, and `rx`/`rz` where
present, applied in XYZ order: an upright turn can be written rx = rz = 180 with ry),
an optional scale `s`, and `params` — the knob values
that placement was built with, where they differ from the part's defaults.

```js
for (const p of LAYOUT) {
  const o = createAsset({ THREE, ...(p.params || {}) });   // from `${p.part}.mjs`
  o.position.set(p.x, p.y, p.z);
  const D = Math.PI / 180;   // all three, XYZ order (three.js's default): some upright rows say rx = rz = 180
  o.rotation.set((p.rx || 0) * D, (p.ry || 0) * D, (p.rz || 0) * D);
  if (p.s) o.scale.setScalar(p.s);
  scene.add(o);
}
```

The row whose part is `_terrain` is the ground rather than a model: it carries a
`module` and the knobs `terrain.mjs` was generated with.

## The ground

`terrain.mjs` generates this kit's own ground; `TERRAIN.json` holds the knob values
it is composed and photographed at. It is a program, not a mesh, so it rebuilds at
any size and the parts sit on it at the same scale they were built for.

```js
import { createAsset as terrain } from './terrain.mjs';
scene.add(terrain(await (await fetch('./TERRAIN.json')).json()));
```

## Using them together

The kit agrees on the knobs below. Knobs not listed here are local to one part, vary them freely.

- **`wear`** (61 of 62 parts): 0 to 1. One value per scene, passed to every part that has it.
- **`colorway`** (57 of 62 parts): 41 different option sets. Every part has this knob and its values are curated for that part, so a
  value from one part is not valid on another. Each `.mjs` lists its own in the header.

Snap positions to the 4m grid and rotate in 90 degree steps: tiles are whole 4m modules with flush edges, so copies butt seamlessly. The floor and ground tiles sit ON y=0 and their walkable top is at y=0.11 m (their own thickness), so stand what goes on them at y=0.11, and at y=0 where there is no tile. The scene editor at https://polyfork.dev/editor snaps to this automatically.

**Scale.** Everything takes the New York City and Neighborhood Pathways grid exactly: a 4 m module and a 3 m storey, ground tiles 4x4 m on a 0.06 m slab with base at y=0 and tiling-edge vertices at exactly +/-2 m, runs in 4 m bays with dead-flat ends and shared posts, a scaffold lift of 2 m, freestanding pieces on a footprint that is a clean multiple of 0.5 m flat on y=0, and real sizes real (cone 0.75 m, scaffold tube 48.3 mm, trench plate 2.5x1.5 m, site cabin 6x3 m, tower crane mast 2 m square, excavator 2.9 m to the cab roof, adult 1.8 m).

**Palette.** `#15181A`, `#262B2F`, `#3A4146`, `#555E63`, `#79838A`, `#9BA5AB`, `#C2CACE`, `#E8EBEA`, `#6E7373`, `#8C9190`, `#AFB2AC`, `#D7D6CB`, `#2E241B`, `#4A3A2A`, `#6B5842`, `#8E7859`, `#C69A62`, `#A3743F`, `#7A5530`, `#D9C093`, `#8C4A28`, `#B3663A`, `#9A5544`, `#5F7368`, `#3E4C45`, `#E3A31B`, `#EF6A1E`, `#C9DB2C`, `#F2F7FF`, `#FFC24A`, `#FF4030`, `#3FD86B`

## Parts

62 parts. `PARTS.json` beside this file lists each one on one line: file, size, knobs with their options and defaults, colour zones, rig nodes, lit zones. `grep '"<key>"' PARTS.json` rather than opening its `.mjs`.

Every part also has its own page with renders and live options:
https://polyfork.dev/kit/construction-site-9e40b0

## Building this with an AI agent?

Point it at https://polyfork.dev/prompt.txt. The same assembly contract above is
machine-readable at https://polyfork.dev/api/kits/construction-site-9e40b0 (the `usage` field), and it
updates as the kit fills up, which this file does not.
