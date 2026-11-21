# Burro With Marigold Panniers

https://polyfork.dev/asset/burro-with-marigold-panniers-1f04ab

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `burro-with-marigold-panniers-1f04ab.mjs` | the model as an ES module, with every option below |
| `burro-with-marigold-panniers-1f04ab.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`burro-with-marigold-panniers-1f04ab.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './burro-with-marigold-panniers-1f04ab.mjs';
scene.add(createAsset());
```

The bare `three` specifiers resolve through any bundler (Vite, webpack, esbuild),
or through an importmap in your page:

```html
<script type="importmap">
{ "imports": { "three": "https://unpkg.com/three@0.180.0/build/three.module.js",
               "three/addons/": "https://unpkg.com/three@0.180.0/examples/jsm/" } }
</script>
```

Prefer the GLB? `new GLTFLoader().load('burro-with-marigold-panniers-1f04ab.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  coat: '#9b9383',
  points: '#eee9e0',
  belly: '#cdc2a6',
  hair: '#56504a',
  mane: '#7a5335',
  stripe: '#56504a',
  dark: '#2a2a30',
  cord: '#7a5335',
  iron: '#c98b3a',
  rein: '#cf9f6a',
  leather: '#7a5335',
  wood: '#56504a',
  wicker: '#f7e7c3',
  weave: '#cf9f6a',
  rope: '#cf9f6a',
  marigold: '#f59a1e',
  marigoldDeep: '#e3760f',
  petal: '#e3760f',
  wilt: '#a86a26',
  vigil: 'ofrenda',
  tone: 0.45,
  reins: 'looped',
  build: 0.53,
  legs: 0.97,
  crest: 0.61,
  neck: 0.89,
  head: 1.05,
  legBulk: 1,
  frontLegs: 0.96,
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `coat` | color | `'#9b9383'` | any hex or `THREE.Color` | albedo of the grey hide: barrel, neck, head, legs and the tail cord |
| `points` | color | `'#eee9e0'` | any hex or `THREE.Color` | albedo of the pale points: the muzzle from the noseband forward, the lips and the patch round each eye |
| `belly` | color | `'#cdc2a6'` | any hex or `THREE.Color` | albedo of the pale underside: the barrel between the legs, the inner faces of the legs and the patch round each eye |
| `hair` | color | `'#56504a'` | any hex or `THREE.Color` | albedo of the tail tuft, the ear rims and the hooves |
| `mane` | color | `'#7a5335'` | any hex or `THREE.Color` | albedo of the upright brush mane and the forelock |
| `stripe` | color | `'#56504a'` | any hex or `THREE.Color` | albedo of the dark stripe down the face and along the spine from the croup to the tail |
| `dark` | color | `'#2a2a30'` | any hex or `THREE.Color` | albedo of the eyes and nostrils, the darkest thing on the animal |
| `cord` | color | `'#7a5335'` | any hex or `THREE.Color` | albedo of the leather halter: noseband, cheek straps, browband and throatlatch |
| `iron` | color | `'#c98b3a'` | any hex or `THREE.Color` | albedo of the brass rings where the cheek straps meet the browband |
| `rein` | color | `'#cf9f6a'` | any hex or `THREE.Color` | albedo of the rope rein looping from the halter back to the withers |
| `leather` | color | `'#7a5335'` | any hex or `THREE.Color` | albedo of the leather zone |
| `wood` | color | `'#56504a'` | any hex or `THREE.Color` | albedo of the wood zone |
| `wicker` | color | `'#f7e7c3'` | any hex or `THREE.Color` | albedo of the wicker zone |
| `weave` | color | `'#cf9f6a'` | any hex or `THREE.Color` | albedo of the weave zone |
| `rope` | color | `'#cf9f6a'` | any hex or `THREE.Color` | albedo of the rope zone |
| `marigold` | color | `'#f59a1e'` | any hex or `THREE.Color` | albedo of the marigold zone |
| `marigoldDeep` | color | `'#e3760f'` | any hex or `THREE.Color` | albedo of the marigoldDeep zone |
| `petal` | color | `'#e3760f'` | any hex or `THREE.Color` | albedo of the petal zone |
| `wilt` | color | `'#a86a26'` | any hex or `THREE.Color` | albedo of the wilt zone |
| `vigil` | choice | `'ofrenda'` | `'before'`, `'ofrenda'`, `'dawn'` | the kit-wide vigil. before: the burro on any other day, both wicker panniers empty and open to their floors, no garland, no petals. ofrenda: each pannier heaped over its rim with cempasúchil, a marigold garland round her neck. dawn: the heaps sunk to three heads a side with one spent, two garland heads a side browned, browning petals on the bands and rims. The burro herself, her halter and her pack saddle are the same build at every stage. |
| `tone` | range | `0.45` | `0` to `1` | how many marigold heads run to the deeper orange: 0 every head the bright orange, 1 every head the deep one; dealt per head off a fixed hash, so no two neighbours are guaranteed to match |
| `reins` | choice | `'looped'` | `'looped'`, `'none'` | looped hangs the rope rein from the halter under the throat and back to the withers; none leaves a bare halter |
| `build` | range | `0.53` | `0` to `1` | barrel depth under a held back line: 0 a lean working burro, 1 a grass-fed one; the pack saddle and panniers ride the rebuilt barrel |
| `legs` | range | `0.97` | `0.85` to `1.15` | leg length: 0.85 a short-legged burro, 1.15 a rangy one; the animal re-bases on the ground |
| `crest` | range | `0.61` | `0` to `1` | head carriage: 0 the head carried low and forward, 1 the poll high and the muzzle drawn in |
| `neck` | range | `0.89` | `0.85` to `1.2` | neck length, the head riding out with it |
| `head` | range | `1.05` | `0.85` to `1.2` | head size about the poll, the long ears scaling with it |
| `legBulk` | range | `1` | `0.8` to `1.3` | Scales the muscular leg sections and hoof widths without moving the joints or changing leg length. |
| `frontLegs` | range | `0.96` | `0.92` to `1.04` | Adjusts the grounded front support columns independently of the hind legs; 1 restores the previous length. |

## Specs

3,336 triangles, 1 material, 0.92 × 1.51 × 1.42 m, real-world scale.

Animatable parts (pivots sit at the hinge or axle): Head, Neck, LeftFrontUpLeg, RightFrontUpLeg, LeftFrontLeg, RightFrontLeg, LeftHindUpLeg, RightHindUpLeg, LeftHindLeg, RightHindLeg, Tail.

Detachable parts (the surface behind stays closed): attach-panniers, attach-collar, attach-bridle.

## Walking this machine

It has **4 legs** (2-link), and `walk.mjs` in this folder walks it:

```js
import { walker, raycastGround } from './walk.mjs';

const machine = createAsset();
scene.add(machine);
const gait = walker(machine, { ground: raycastGround([yourTerrain]) });

// in your render loop, after you have moved it yourself:
distance += speed * delta;
machine.position.z = distance;
gait.poseAt(distance);
```

You keep the steering; it owns the legs, the ride height and the body tilt. It
finds the ground under each foothold (an analytic height field if you have one, a
downward ray otherwise), plants each foot so it does not slide while the body
passes over it, and leans the chassis onto the plane through the feet that are
down. The phase comes from DISTANCE, not from a clock, so the walk is identical
at any frame rate and can be scrubbed or rendered offline.

Measured on this model: a natural step of **0.19 m**, and a planted foot that does not move at all (0 mm of drift on level ground, a slope and a ripple).

Not using three.js? The same numbers are in `https://polyfork.dev/api/assets/burro-with-marigold-panniers-1f04ab`
under `gait`: leg names, segment lengths, foot offsets, phase order, step and
duty. It is about thirty lines to reimplement against that.

## Building this with an AI agent?

Point it at https://polyfork.dev/prompt.txt: the whole catalogue as working
instructions, no key needed to browse. There is an MCP server too, at https://polyfork.dev/mcp.

## License

```
Licence

THE MESH FILES (.glb, .fbx, .usdz, .usda, .obj, .mtl) are yours under
either licence, your choice:

1. CC BY 4.0, https://creativecommons.org/licenses/by/4.0/
   Use, share, modify, re-upload and sell them, commercially too, as long as you
   credit Polyfork and say if you changed them. The credit line:
   Burro With Marigold Panniers by Polyfork (https://polyfork.dev/asset/burro-with-marigold-panniers-1f04ab), CC BY 4.0

2. The Polyfork licence, below: no credit needed, but no redistributing the files.

EVERYTHING ELSE here, the ES modules (.mjs) above all, is under the
Polyfork licence only. CC BY does not cover it.

The Polyfork licence

Use these files in personal and commercial projects: games, apps, websites, client
work, products you sell. Modify freely. No attribution required. Do not resell or
redistribute the files themselves as assets, or use them to build or train a
COMMERCIAL 3D asset generator: a model, service or pipeline that produces 3D assets
and that you sell or offer to others. Personal experiments, research and learning
are fine. Breaking these terms can end your licence and your access, without a
refund.

Full terms: https://polyfork.dev/licensing
```
