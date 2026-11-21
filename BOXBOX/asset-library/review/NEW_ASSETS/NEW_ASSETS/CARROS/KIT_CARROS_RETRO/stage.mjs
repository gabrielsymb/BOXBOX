// Polyfork stage.mjs. THIS FILE IS THE API. The implementation is ./_internal/stage.mjs: merging, hull maths, caching
// and shaders, none of which changes how you call it. Commented source: https://polyfork.dev/cdn/stage.mjs
/**
 * stage(renderer, scene, options) -> look          sets the renderer, three lights, soft shadows, sky and fog
 * options: time   'noon' | 'day' | 'golden' | 'dusk' | 'night' | 'moonlit' | 'dawn' | 'overcast' | 'space', or 0..1 from day to night (yours to choose)
 *          toneMapping 'none' (what these colours are authored for) | 'aces'     shadows true   shadowMap 2048   shadowSpan 90
 *          sky true   fog true   far 400   azimuth 35
 * look: hemi, key, rim (the lights)   sky   night (0..1, how lit the place's own lamps are)   lighting (the name set)
 *       setTime(nameOrNumber)   follow(object)   update() every frame   shadows(root)   ground(place) the kit's turf out to hills
 *       soften(terrain)
 * also exported: LIGHTING (the names), softenTerrain(root, options)
 */
export * from './_internal/stage.mjs';
export { default } from './_internal/stage.mjs';
