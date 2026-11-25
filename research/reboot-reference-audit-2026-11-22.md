# BOXBOX reboot reference audit

Date: 2026-11-22

## Decision

The reboot is an independent Unity 6.3 LTS project under `BOXBOX/reboot`.
The old implementation stays available while the replacement is validated system by system.

Target references:

- Midnight Club 2: responsive arcade steering, chase-camera placement, speed response and drift readability.
- GTA 2: elevated world readability, compact systemic city, pedestrian/traffic interactions and mission loop.
- BOXBOX: commerce simulation, inventory, finance, quests, bilingual presentation and the project's own assets.

The previous BOXBOX implementation is a research reference, not a code dependency. The reboot does not reference its core assembly, scenes or scripts. Features are specified and reimplemented independently after review.

## Source audit

### OpenMC2 / OpenMC2

- Revision audited: `794b6e6bd723600b73fdd39504905541c599ea25` (2023-06-15).
- Repository license: GPL-3.0 for the repository's source.
- Status: abandoned.
- The project explicitly calls the original Midnight Club 2 binary for functions it has not reimplemented.
- No independently implemented vehicle handling, suspension or chase-camera subsystem was found.
- Conclusion: reference-only. It is not a usable donor for the requested driving physics. BOXBOX will recreate the observable feel with original Godot/C# code and tunable data.

Source: https://github.com/OpenMC2/OpenMC2

### CriminalRETeam / gta2_re

- Revision cloned: `d43309d967e0c03d82887bf5a64c778e3e7e0223`.
- No license file was present at the audited revision.
- This matching/decompilation project is substantially more complete and faithful than OpenGTA2, so it replaces OpenGTA2 as the GTA 2 behavioral research reference.
- Allowed project use: identify externally observable mechanics, states, invariants, timing and data relationships; turn those findings into implementation-neutral specifications and tests.
- Prohibited project use: direct translation, copying distinctive code structure, importing original data/assets, linking against the project or shipping any of its source.
- Conclusion: use it as a functional specification source. Implement the resulting specification independently in idiomatic Godot/C#, with REDRIVER2's MIT code remaining the licensed donor for concrete city/vehicle implementation patterns.

Source: https://github.com/CriminalRETeam/gta2_re

### OpenDriver2 / REDRIVER2

- Revision cloned: `b2d8857426f815007486def9bd23f9a6007a2c64`.
- Repository license: MIT.
- This is the strongest city/vehicle donor found so far. It contains complete modules for car physics (`handling.c`, `wheelforces.c`), follow camera (`camera.c`), civilian traffic (`civ_ai.c`), roads (`dr2roads.c`), pedestrians (`pedest.c`), police pursuit (`cop_ai.c`) and missions (`mission.c`).
- Conclusion: use its explicit systems and state machines as the principal technical reference for BOXBOX driving, traffic density, vehicle pooling, pursuit and pedestrian reactions. Port behavior into idiomatic Godot/C# components and retain required MIT attribution for any substantially adapted implementation.

Source: https://github.com/OpenDriver2/REDRIVER2

### 3583Bytes / Moonforge RPG Engine

- Revision cloned: `3864fc363033111b93c68965ed36fcab87a31aa7`.
- Repository license: MIT.
- Stack: deterministic C# library, including atomic buy/sell handlers, wallets, inventory, shop stock/restock, quest signals, persistence snapshots and automated tests.
- Conclusion: compare its transaction boundaries and quest event flow with `BOXBOX.Core`; adopt missing invariants and tests without replacing BOXBOX-specific tax, customer, supplier and operating-cost rules.

Source: https://github.com/3583Bytes/moonforge-rpg-engine

### openmoh / OpenMoHAA

- Revision cloned: `a2f340195975f4f042e28a60b62561dd9a0b2700`.
- Repository license: GPL-2.0.
- Useful reference areas include actor perception, cover, patrol, alarm, grenade response, run-and-shoot behavior, weapon simulation and objective scripting.
- It requires the original Medal of Honor data to run; those assets are not part of BOXBOX.
- Conclusion: behavior reference only unless BOXBOX is deliberately relicensed under a compatible GPL. Build original Godot/C# combat components around the existing facts/perception/report architecture.

Source: https://github.com/openmoh/openmohaa

### endless-sky / Endless Sky

- Revision cloned: `b71cbc6791ec2c8035ffa810f510c3b8b501c96c`.
- Repository license: GPL-3.0.
- Mature reference for commodities, cargo, shops, sales availability, route-sensitive trade and data-driven missions.
- Conclusion: use its economy and mission composition as product-design reference. BOXBOX keeps its own deterministic C# implementation so commerce remains tailored to a neighborhood microbusiness rather than a space-trading market.

Source: https://github.com/endless-sky/endless-sky

## Reboot sequence

1. Driving lab: Unity/PhysX rigid-body controller informed by REDRIVER2 wheel forces, with an arcade tuning layer aimed at the requested Midnight Club feel.
2. One city block: road/lane graph, parking, store entrance and collision layers.
3. Population slice: pooled pedestrians and civilian traffic using explicit state machines and deterministic intent.
4. BOXBOX loop: atomic purchase/sale, stock, supplier, operating costs and a quest, strengthened with Moonforge-style transaction tests.
5. Mission combat slice: perception, cover, alarm, hitscan/projectile weapons and consequences; never a disconnected FPS minigame.
6. City pressure: time of day, incidents, police/inspection response and persistent consequences.

No original GTA or Midnight Club assets, maps, executable data or proprietary code are project dependencies.
