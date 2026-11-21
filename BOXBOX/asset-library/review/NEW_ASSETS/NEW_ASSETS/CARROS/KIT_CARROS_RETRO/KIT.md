# Retro Cars Kit

**Building with this? Read `API.md` (one paragraph: the whole API of kit.mjs, stage.mjs and clips.mjs) and stop.**
Three lines build the place: `import { buildKit } from './kit.mjs'; const place = await buildKit({ folder: './' }); scene.add(place.group);`. The rest of this file is reference.

https://polyfork.dev/kit/racing-cars-kit-660d53

29 of 29 parts, packed 2026-11-18.
Kits fill up over time: if this says fewer parts than the site does, re-download.

## What is in this folder

Every part twice: `<part>.glb` for any engine, `<part>.mjs` as a drop-in three.js
module with its options. `scene.glb` is the composed kit scene, the whole thing
arranged as one file.

BUILD THE WHOLE PLACE IN ONE CALL. `kit.mjs` in this folder does it:

```js
import { buildKit } from './kit.mjs';          // serve this folder over http
const place = await buildKit({ folder: './' });   // all 93 placements, merged to a few draw calls
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

`LAYOUT.json` is the composition: 93 placements of 27 parts, the arrangement
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

## Using them together

The kit agrees on the knobs below. Knobs not listed here are local to one part, vary them freely.

- **`colorway`** (29 of 29 parts): 29 different option sets. Every part has this knob and its values are curated for that part, so a
  value from one part is not valid on another. Each `.mjs` lists its own in the header.

Snap positions to the 4m grid and rotate in 90 degree steps: tiles are whole 4m modules with flush edges, so copies butt seamlessly. Walkable tops meet at y=0. The scene editor at https://polyfork.dev/editor snaps to this automatically.

**Scale.** One shared metric scale on a 4m grid: hero cars are 3.6-5.2m long and roll on 0.34m-radius wheels, roadside tiles are exactly 4x4m with flush square edges so copies butt seamlessly into runs, and signs/posts stand 2.4-5m tall.

**Palette.** `#823630`, `#d13d34`, `#ae8566`, `#4887c4`, `#e58132`, `#54b4b1`, `#e8cc42`, `#267466`, `#4c4f57`, `#2a2d35`, `#1a1f26`, `#999ca3`, `#875e43`, `#e2e4e7`, `#0c0e14`, `#3d3f46`, `#ecf1cb`, `#676b72`, `#744d36`, `#dadde0`, `#ddceb0`, `#898c95`, `#4e3c30`, `#747c8a`, `#6f8a4b`, `#c7baa6`, `#6b2627`, `#91776a`, `#f5f6f4`, `#2a3a6b`, `#4e7692`, `#98443d`

## Parts

29 parts. `PARTS.json` beside this file lists each one on one line: file, size, knobs with their options and defaults, colour zones, rig nodes, lit zones. `grep '"<key>"' PARTS.json` rather than opening its `.mjs`.

Every part also has its own page with renders and live options:
https://polyfork.dev/kit/racing-cars-kit-660d53

## Building this with an AI agent?

Point it at https://polyfork.dev/prompt.txt. The same assembly contract above is
machine-readable at https://polyfork.dev/api/kits/racing-cars-kit-660d53 (the `usage` field), and it
updates as the kit fills up, which this file does not.
