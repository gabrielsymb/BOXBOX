# Draft Horse

https://polyfork.dev/asset/draft-horse-519160

A parametric low-poly model for three.js. One import, no loader, no textures,
one draw call. `createAsset()` returns a ready `THREE.Group`.

## What is in this folder

| file | what it is |
| --- | --- |
| `index.html` | a complete working page: **double-click it** |
| `draft-horse-519160.mjs` | the model as an ES module, with every option below |
| `draft-horse-519160.glb` | the same model baked, for Unity, Godot, Blender and glTF loaders |
| `LICENSE.txt` | the terms, file by file: which licence each file is under |

## Run it

Double-click `index.html`. It needs an internet connection the first time,
because three.js is pulled from unpkg.com.

It carries its own copy of the model code so that it works from a `file://`
URL, where browsers refuse to load ES modules. If you would rather edit
`draft-horse-519160.mjs` and see the page pick up your changes, serve the folder
and it will import the file instead of the copy:

```
python3 -m http.server     # then open http://localhost:8000
```

## Use it in your own scene

```js
import { createAsset } from './draft-horse-519160.mjs';
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

Prefer the GLB? `new GLTFLoader().load('draft-horse-519160.glb', g => scene.add(g.scene))`.
The module is the smaller and more flexible of the two: it carries the options
below, the GLB is one frozen configuration of them.

## Options

```js
createAsset({
  coat: '#9c7556',
  points: '#6e4e36',
  hair: '#c4ab80',
  dark: '#2c211a',
  hoof: '#5a4434',
  blaze: 'yes',
  muzzle: '#c9b0a3',
  leather: '#4a3324',
  collar: '#5a3e2c',
  brass: '#b08f48',
  build: 0.65,
  legs: 1,
  crest: 0.8,
  neck: 1,
  head: 1.17,
  legBulk: 1.2,
  frontLegs: 0.96,
  tack: 'harnessed',
});
```

| option | type | default | accepts | what it does |
| --- | --- | --- | --- | --- |
| `coat` | color | `'#9c7556'` | any hex or `THREE.Color` | albedo of the whole hide: barrel, quarters, neck, head and legs down to the pastern, one flat tone, the light does the shading |
| `points` | color | `'#6e4e36'` | any hex or `THREE.Color` | albedo of the points: the muzzle end, the inner ear and the band above each hoof, one clear value rung darker than the coat |
| `hair` | color | `'#c4ab80'` | any hex or `THREE.Color` | albedo of the mane, forelock and tail, two rungs off the coat in either direction |
| `dark` | color | `'#2c211a'` | any hex or `THREE.Color` | albedo of the hooves, the eyes and the nostrils, the darkest thing on the animal |
| `hoof` | color | `'#5a4434'` | any hex or `THREE.Color` | albedo of the hoof zone |
| `blaze` | choice | `'yes'` | `'no'`, `'yes'` | yes paints a pale blaze down the front of the face and over the muzzle in the blaze zone; no is a plain face |
| `muzzle` | color | `'#c9b0a3'` | any hex or `THREE.Color` | albedo of the muzzle zone |
| `leather` | color | `'#4a3324'` | any hex or `THREE.Color` | albedo of the leather zone |
| `collar` | color | `'#5a3e2c'` | any hex or `THREE.Color` | albedo of the collar zone |
| `brass` | color | `'#b08f48'` | any hex or `THREE.Color` | albedo of the brass zone |
| `build` | range | `0.65` | `0` to `1` | rebuilds the barrel's cross-sections between a light and a stocky animal, holding the TOPLINE and the width still so anything on the back never moves and the body grows downward: 0 a shallow-barrelled wiry animal with daylight under it, 1 a grass-fed one whose belly hangs below the elbow. Every strap and pad is raycast onto the rebuilt body. |
| `legs` | range | `1` | `0.85` to `1.15` | scales every leg below the shoulder and the hip and re-bases the animal on the ground, so 0.85 is a short-legged pony under the same body and 1.15 a rangy one; the withers height moves with it. |
| `crest` | range | `0.8` | `0` to `1` | rebuilds the neck arc and re-aims the head, mane, forelock and bridle together: 0 a long low working neck with the muzzle carried forward, 1 the poll at its highest and the muzzle drawn in toward the chest. Triangle count is unchanged. |
| `neck` | range | `1` | `0.85` to `1.2` | lengthens the neck arc without changing its section, the head riding out with it. |
| `head` | range | `1.17` | `0.85` to `1.2` | scales the head about the poll, ears and eye with it: 0.85 a small neat pony head, 1.2 the long heavy head of a draft animal. |
| `legBulk` | range | `1.2` | `0.8` to `1.3` | Scales the muscular leg sections and hoof widths without moving the joints or changing leg length. |
| `frontLegs` | range | `0.96` | `0.92` to `1.04` | Adjusts the grounded front support columns independently of the hind legs; 1 restores the previous length. |
| `tack` | choice | `'harnessed'` | `'harnessed'`, `'bare'` | harnessed wears the reference's work harness (collar and hames, back and belly band, traces into the breeching, hip strap, crupper, bridle and driving lines); bare is the horse alone |

## Specs

3,939 triangles, 1 material, 0.93 × 2.57 × 2.8 m, real-world scale.

Animatable parts (pivots sit at the hinge or axle): Head, Neck, LeftFrontUpLeg, RightFrontUpLeg, LeftFrontLeg, RightFrontLeg, LeftHindUpLeg, RightHindUpLeg, LeftHindLeg, RightHindLeg, Tail, LeftHindHock, RightHindHock.

Detachable parts (the surface behind stays closed): attach-harness, attach-bridle.

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

Measured on this model: a natural step of **0.375 m**, and a planted foot that does not move at all (0 mm of drift on level ground, a slope and a ripple).

Not using three.js? The same numbers are in `https://polyfork.dev/api/assets/draft-horse-519160`
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
   Draft Horse by Polyfork (https://polyfork.dev/asset/draft-horse-519160), CC BY 4.0

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
