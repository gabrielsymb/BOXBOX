// Polyfork kit.mjs. THIS FILE IS THE API. The implementation is ./_internal/kit.mjs: merging, hull maths, caching
// and shaders, none of which changes how you call it. Commented source: https://polyfork.dev/cdn/kit.mjs
/**
 * buildKit(options) -> Promise<place>
 * options: { folder: './kit/' }  or  { base, kit }          where the kit is (unzipped folder, or the CDN)
 *   params    scene-wide knobs, e.g. { season: 'winter' }; they reach only the parts that declare them
 *   keep      [part keys] you will move, hide or pick up; everything else is merged into a few draw calls
 *   only, skip [part keys]     people: 'idle' | 'none'     night: true     loose: false (true: small props instanced and movable)
 *   setting   true (or a density, 2 = twice as thick): the terrain alone, wider, with the kit's vegetation and rock scattered over it; no composed placements
 *   terrainParams  the ground's own knobs over the composed ones, e.g. { relief: 1.8 } (params never reach the ground)
 *   city      { seed, blocks: [x, z], block?, street?, parks?, boundary?, roads? }   street kits: a NEW, bigger town from this kit's buildings
 * place:
 *   group                                   add it to your scene
 *   bounds                                  THREE.Box3
 *   of(key) -> Object3D[]   where(key) -> Vector3[]   placed -> [{ key, object, row }]
 *   blocked(x, z, radius) -> key | null     what a body there would touch (convex outlines): your collision test
 *   boxes() -> [{ key, box, low, hull }]    the same data raw (low: ankle-to-head box; hull: [[x, z], ...])
 *   spots(n, { from, road }) -> Vector3[]   clear, reachable, spread-out points
 *   openSpot({ near }) -> Vector3           a clear point to start at
 *   surfaceAt(x, z) -> key | null           the ground tile under a point (a key containing 'road' on a street)
 *   groundAt(x, z) -> y                     terrain height there
 *   vista({ fov, aspect }) -> { position, target }   a camera that frames the whole place
 *   surround() -> Promise                   the kit's trees and rocks scattered past its edge
 *   night(0..1)                             windows, signs, lamps (stage() drives this from its time of day)
 *   light(object, key)                      the same for something you built from one of this kit's modules
 *   loose -> [{ key, position, quaternion, radius, mass, apply() }]      with loose: true
 *   people -> [{ object, anim }]            tick(seconds): call it every frame
 *   city -> { size: [x, z], street, pavement, streets: { x: [...], z: [...] }, junctions: [[x, z]],
 *             lots: [{ key, x, z, facing, width, depth, door: [x, z] }] }      door = the pavement in front of it
 */
export * from './_internal/kit.mjs';
export { default } from './_internal/kit.mjs';
