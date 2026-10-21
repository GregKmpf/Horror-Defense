# Turrets

Every turret is a **Prefab Variant** of a model from the Sci-Fi Turrets package (`Prefabs/`), so the package files stay
untouched and the gameplay lives in `Gameplay/`:

```
Turret_<Name>.prefab        the package model plus the scripts below
├─ Turret                   targets enemies, aims, fires → <Name>.asset (TurretDefinition)
├─ TurretEffects            muzzle flash, tracers, impacts. Only listens to Turret's events
├─ base                     static mesh
└─ tower                    turns left and right (yaw)
   └─ gun                   tilts up and down (pitch)
      └─ Muzzle_1, Muzzle_2 where shots come from, used in turn
```

Shared scripts live in `Assets/Scripts/Turrets` (effects in `Assets/Scripts/Turrets/Effects`). The effect prefabs and
materials are in `Effects/`.

## The turrets

All numbers live in the `TurretDefinition` assets in `Gameplay/`.

| Turret         | Range | Damage        | Interval | Cost | Targets   | What it does                                |
|----------------|-------|---------------|----------|------|-----------|---------------------------------------------|
| Cannon         | 12    | 25            | 1.0 s    | 100  | First     | Balanced all-rounder                        |
| Rocket Pod     | 14    | 20, splash 3  | 2.0 s    | 175  | First     | Area damage against groups                  |
| Artillery      | 22    | 120           | 3.0 s    | 250  | Strongest | Long range, slow to turn                    |
| Minigun        | 9     | 6             | 0.1 s    | 125  | First     | Fast fire against weak enemies              |
| Heavy          | 13    | 30, splash 2  | 0.4 s    | 400  | First     | Fast fire and splash together               |
| Shock Generator| 8     | 8 to everyone | 0.8 s    | 150  | All       | Hits every enemy in range, doesn't aim      |

## Adding a new turret

1. Right-click a model prefab in `Prefabs/` → **Create → Prefab Variant**, move it into `Gameplay/` and name it
   `Turret_<Name>.prefab`.
2. Add the `Turret` and `TurretEffects` components.
3. Under the `gun` object, add empty children called `Muzzle_1` (and `Muzzle_2`, ...) at the barrel tips, with no
   rotation. Shots alternate between them. With none, shots come from the gun itself.
4. **Create → Horror Defense → Turret Definition** named `<Name>.asset` in `Gameplay/`, fill in the stats, assign the
   prefab's `Turret` component to its **Prefab** field and the definition to the prefab's `Turret` component.
5. Pick its effects in the definition (all optional, see below).
6. To test it, put a `TurretSlot` in the scene, set **Starting Turret** to the definition and press Play.

A model that has no `tower` or `gun` child (like the Shock Generator) doesn't aim, it fires whenever something is in
range.

## Rules

- Stats go in the Turret Definition asset, not on the prefab.
- Shared scripts never mention a specific turret.
- Damage lands the moment a turret fires. Nothing visual may delay it or change it.
- Effects only listen to `Turret`'s events (`Fired`, `Hit`, `Exploded`). They never touch targeting or health.
- The gun's pitch is set outright, not added to the model's rest pose, because the package models rest tilted up.

## Targeting

A turret picks its target from `Enemy.Active` by distance, not with physics triggers, and re-picks every quarter of a
second. **Target Priority** decides who:

| Priority  | Picks                              |
|-----------|------------------------------------|
| First     | Closest to the base                |
| Last      | Furthest from the base             |
| Closest   | Closest to the turret              |
| Strongest | Most health left                   |

**Max Targets** is how many enemies one shot hits (0 = every enemy in range). **Splash Radius** also damages everything
that close to the target.

## Build slots

A `TurretSlot` is a predefined spot where a turret can be built. **Slots are placed by hand**: make an empty
GameObject where a turret should go, add `TurretSlot`, and rotate it the way the turret should face. A green square
means free and a red one means taken.

- `Build(definition)` spawns the turret there. It doesn't charge for it, the economy decides whether the player can
  afford it.
- `Clear()` removes the turret and frees the slot.
- `TurretSlot.All` lists every slot, for a build UI to highlight and pick from.

## Effects

Everything visible when a turret fires is optional and set in its definition:

| Field            | What it is                                                        | Used by                        |
|------------------|-------------------------------------------------------------------|--------------------------------|
| Muzzle Flash     | Particle system played at the muzzle every shot, one per muzzle   | Everything with a gun          |
| Tracer           | A line from the muzzle to every enemy hit (`LineEffect`)          | Bullet trails, the electric arc|
| Impact           | Spawned on every enemy hit                                        | Everything with a tracer       |
| Projectile       | A rocket that flies to the target, damage doesn't wait for it     | Rocket Pod                     |
| Explosion        | Spawned where a splash shot lands, scaled to the splash radius    | Rocket Pod, Heavy              |
| Pulse            | A ring that spreads from the turret to its range every shot       | Shock Generator                |

The explosion is made for a radius of 1 and scaled to the splash radius. Tracers, impacts, rockets, explosions and
rings remove themselves when done, the muzzle flash stays on the muzzle and plays again. The materials are additive URP
particle shaders.

## How other systems use turrets

| System       | Uses                                                                       |
|--------------|----------------------------------------------------------------------------|
| Build UI     | `TurretSlot.All`, `slot.IsFree`, `slot.Build(definition)`, `definition.cost` |
| Economy      | Charge `definition.cost` before calling `Build`                            |
| Enemies      | Nothing to do, turrets read `Enemy.Active` and call `TakeDamage()`         |

## Not done yet

- Sounds and recoil (decided against for now).
- A build UI and the economy that spends `cost`.
- Selling a turret (`TurretSlot.Clear()` exists, nothing refunds it).
- Real projectiles. Rockets are visual only.
- Pooling for effects. The Minigun creates a few short-lived objects per shot.
