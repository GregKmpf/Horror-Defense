# Turrets

Every turret is a **Prefab Variant** of a model from the Sci-Fi Turrets package (`Prefabs/`), so the package files stay
untouched and the gameplay lives in `Gameplay/`:

```
Turret_<Name>.prefab        the package model plus the scripts below
├─ Turret                   targets enemies, aims, fires projectiles → <Name>.asset (TurretDefinition)
├─ TurretEffects            muzzle flash and pulse. Only listens to Turret's Fired event
├─ base                     static mesh
└─ tower                    turns left and right (yaw)
   └─ gun                   tilts up and down (pitch)
      └─ Muzzle_1, Muzzle_2 where projectiles come from, used in turn
```

What a turret fires is a projectile prefab in `Projectiles/`:

```
Projectile_<Name>.prefab    layer: Projectile
├─ Projectile               flies to the target, deals the damage when it collides with an enemy
├─ ProjectileEffects        impact and explosion. Only listens to Projectile's events
├─ Rigidbody                kinematic, moved by Projectile
├─ CapsuleCollider          trigger, what touches the enemies
├─ Body                     what you see flying
└─ Trail                    the streak behind it
```

Shared scripts live in `Assets/Scripts/Turrets` (effects in `Assets/Scripts/Turrets/Effects`). The effect prefabs and
materials are in `Effects/`.

## The turrets

All numbers live in the `TurretDefinition` assets in `Gameplay/`.

| Turret         | Range | Damage        | Interval | Cost | Targets   | Projectile             | Speed  | What it does                     |
|----------------|-------|---------------|----------|------|-----------|------------------------|--------|----------------------------------|
| Cannon         | 12    | 25            | 1.0 s    | 100  | First     | Shell                  | 45 m/s | Balanced all-rounder             |
| Rocket Pod     | 14    | 20, splash 3  | 2.0 s    | 175  | First     | Rocket                 | 22 m/s | Area damage against groups       |
| Artillery      | 22    | 120           | 3.0 s    | 250  | Strongest | Heavy Shell            | 35 m/s | Long range, slow to turn         |
| Minigun        | 9     | 6             | 0.1 s    | 125  | First     | Bullet                 | 60 m/s | Fast fire against weak enemies   |
| Heavy          | 13    | 30, splash 2  | 0.4 s    | 400  | First     | Shell                  | 45 m/s | Fast fire and splash together    |
| Shock Generator| 8     | 8 to everyone | 0.8 s    | 150  | All       | Bolt                   | 30 m/s | A bolt at every enemy in range   |

## Adding a new turret

1. Right-click a model prefab in `Prefabs/` → **Create → Prefab Variant**, move it into `Gameplay/` and name it
   `Turret_<Name>.prefab`.
2. Add the `Turret` and `TurretEffects` components.
3. Under the `gun` object, add empty children called `Muzzle_1` (and `Muzzle_2`, ...) at the barrel tips, with no
   rotation. Shots alternate between them. A model without a gun puts them straight under the turret (the Shock
   Generator has one on top of its column). With none, shots come from the gun or the turret itself.
4. **Create → Horror Defense → Turret Definition** named `<Name>.asset` in `Gameplay/`, fill in the stats, assign the
   prefab's `Turret` component to its **Prefab** field and the definition to the prefab's `Turret` component.
5. Pick a **Projectile** and its **Projectile Speed** in the definition. A turret without a projectile logs an error and
   stops.
6. Pick its effects in the definition (optional, see below).
7. To test it, put a `TurretSlot` in the scene, set **Starting Turret** to the definition and press Play.

A model that has no `tower` or `gun` child (like the Shock Generator) doesn't aim, it fires whenever something is in
range.

## Adding a new projectile

Duplicate one of the prefabs in `Projectiles/` and change its `Body`, `Trail`, the collider's radius and the effects on
`ProjectileEffects`. Keep the root on the **Projectile** layer and the collider a trigger. Damage and speed are not on the
projectile, they come from the definition of the turret that fires it, so one projectile can serve several turrets
(the Cannon and the Heavy share the Shell).

## Rules

- Stats go in the Turret Definition asset, not on the prefab.
- Shared scripts never mention a specific turret.
- **Turrets never damage enemies. Projectiles do, and only when they collide with one.**
- Effects only listen to events (`Turret.Fired`, `Projectile.Hit`, `Exploded`, `Expired`). They never touch targeting
  or health.
- The gun's pitch is set outright, not added to the model's rest pose, because the package models rest tilted up.

## Targeting

A turret picks its target from `Enemy.Active` by distance, not with physics triggers, and re-picks every quarter of a
second. It aims at `enemy.Center`, the middle of the enemy's collider. **Target Priority** decides who:

| Priority  | Picks                              |
|-----------|------------------------------------|
| First     | Closest to the base                |
| Last      | Furthest from the base             |
| Closest   | Closest to the turret              |
| Strongest | Most health left                   |

**Max Targets** is how many projectiles one shot fires, each at a different enemy (0 = one at every enemy in range).

## Projectiles

Every shot spawns one projectile per target at the muzzle, and from then on the projectile is on its own:

1. **Flies** at the definition's speed towards the target's `Center`, following it as it moves. If the target dies
   first, it carries on in a straight line.
2. **Collides.** Its trigger touches an enemy's collider and `OnTriggerEnter` fires. The **Projectile** layer only
   collides with the **Enemy** layer (Project Settings → Physics), so terrain, buildings and other projectiles are
   ignored.
3. **Damages** the first living enemy it touches with the definition's damage. With a **Splash Radius**, every enemy
   whose collider is inside that radius of the impact (`Physics.OverlapSphere` on the Enemy layer) takes the damage
   too. A projectile with **Only Hits Target** (the Shock Generator's bolt) flies through everyone else and only hits
   its own target while the target lives.
4. **Is destroyed** when it hits, or when it leaves the valid area: further from its turret than 1.25 × the turret's
   range, or after 4 seconds.

A fast projectile could jump over an enemy between two physics steps, so its trigger is stretched back over the
distance it covers in one step. Enemies need a collider on the **Enemy** layer to be hit (`Enemy_Base` has one).

| Projectile   | Used by          | Impact       | Explosion (splash only) |
|--------------|------------------|--------------|-------------------------|
| Shell        | Cannon, Heavy    | Impact_Small | Explosion               |
| Bullet       | Minigun          | Impact_Small |                         |
| Heavy Shell  | Artillery        | Impact_Heavy | Explosion               |
| Rocket       | Rocket Pod       |              | Explosion               |
| Bolt         | Shock Generator  | Impact_Shock |                         |

## Build slots

A `TurretSlot` is a predefined spot where a turret can be built. **Slots are placed by hand**: make an empty
GameObject where a turret should go, add `TurretSlot`, and rotate it the way the turret should face. A green square
means free and a red one means taken.

- `Build(definition)` spawns the turret there. It doesn't charge for it, the economy decides whether the player can
  afford it.
- `Clear()` removes the turret and frees the slot.
- `TurretSlot.All` lists every slot, for a build UI to highlight and pick from.

The Abandoned Farm has four slots under `TurretSlots`, on the bank west of the barn where the route comes ashore.
`TurretSlot 1` has a Cannon as its **Starting Turret** so there is a turret in the game until the build UI exists. Clear
that field when the UI is added.

## Effects

What you see when a turret fires is set in its definition, all optional:

| Field            | What it is                                                        | Used by                        |
|------------------|-------------------------------------------------------------------|--------------------------------|
| Muzzle Flash     | Particle system played at the muzzle every shot, one per muzzle   | Everything with a gun          |
| Pulse            | A ring that spreads from the turret to its range every shot       | Shock Generator                |

What you see when a projectile lands is set on its `ProjectileEffects`: an **Impact** spawned where it hits and an
**Explosion** where its splash goes off, made for a radius of 1 and scaled to the splash radius. When a projectile is
destroyed its trail and particles are let go so they fade out instead of vanishing.

Impacts, explosions and rings remove themselves when done, the muzzle flash stays on the muzzle and plays again. The
materials are additive URP particle shaders.

## How other systems use turrets

| System       | Uses                                                                       |
|--------------|----------------------------------------------------------------------------|
| Build UI     | `TurretSlot.All`, `slot.IsFree`, `slot.Build(definition)`, `definition.cost` |
| Economy      | Charge `definition.cost` before calling `Build`                            |
| Enemies      | A collider on the Enemy layer. Turrets read `Enemy.Active` and `Center`, projectiles call `TakeDamage()` |

## Not done yet

- Sounds and recoil.
- A build UI and the economy that spends `cost`.
- Selling a turret (`TurretSlot.Clear()` exists, nothing refunds it).
- Pooling for projectiles and effects. The Minigun creates a few short-lived objects per shot.
