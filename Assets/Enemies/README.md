# Enemies

Every enemy is a **Prefab Variant** of `_Template/Enemy_Base.prefab`, so they all share the same structure:

```
<Name>.prefab              layer: Enemy
├─ Enemy                   health, death, events → <Name>.asset (EnemyDefinition)
├─ EnemyMovement           walks an EnemyPath, stays on the Ground layer
├─ SphereCollider          trigger, what projectiles and clicks hit
├─ Rigidbody               kinematic
└─ Visual                  model, Animator, effects and enemy-specific scripts
```

Shared scripts live in `Assets/Scripts/Enemies` and `Assets/Scripts/Paths`. Everything specific to one
enemy lives in its own folder here, e.g. `Mimic/`.

## Adding a new enemy

1. Create a folder `Assets/Enemies/<Name>/`.
2. Right-click `_Template/Enemy_Base.prefab` → **Create → Prefab Variant**, move it into the folder and
   name it `<Name>.prefab`.
3. Put the model, Animator and effects under its `Visual` child. Scripts only this enemy uses go in
   `<Name>/Scripts/`.
4. In the folder, **Create → Horror Defense → Enemy Definition** named `<Name>.asset`, fill in the stats
   and assign it to the prefab's `Enemy` component.
5. To test it, drop the prefab in the scene, set **Starting Path** on its `Enemy` component and press Play.

## Mimic types

Four kinds of Mimic, from a small, fast swarm to a slow boss. The other three are **Prefab Variants** of
`Mimic/Mimic.prefab`, so they share its scripts and only change numbers, sizes and materials:

| Type      | Health | Speed   | Bounty | Base damage | Hover | Hitbox radius | Legs                          | Colour                       |
|-----------|-------:|--------:|-------:|------------:|------:|--------------:|-------------------------------|------------------------------|
| Crawler   | 40     | 8 m/s   | 6      | 5           | 1.8 m | 0.4 m         | 8, thin, reach 5 m            | Pale bone                    |
| Mimic     | 100    | 5 m/s   | 12     | 10          | 3 m   | 0.5 m         | 12, reach 9 m                 | Black                        |
| Brute     | 320    | 3 m/s   | 40     | 25          | 4.5 m | 0.9 m         | 12, thick, reach 12.5 m       | Dark rust                    |
| Matriarch | 1000   | 2 m/s   | 150    | 50          | 6.5 m | 1.4 m         | 16, very thick, reach 17 m    | Black, with a glowing red heart |

Health and speed are before the difficulty's multipliers. Base damage is out of the base's 100 health, so ten
Mimics or two Matriarchs destroy it. Each type has, in `Mimic/`:

| File                            | What it sets                                                                 |
|---------------------------------|------------------------------------------------------------------------------|
| `<Type>.asset`                  | Health, speed, bounty and base damage (`EnemyDefinition`)                     |
| `<Type>.prefab`                 | Hover height (`EnemyMovement` → Ground Offset), hitbox (`SphereCollider` radius), body size and material, death duration, and how many legs and how far they reach (`MimicLegs`) |
| `Prefabs/<Type>Leg.prefab`      | Variant of `MimicLeg`: strand width and material, how high the leg arches, how fast it grows and wobbles |
| `Materials/<Type>.mat`          | Colour of the legs and body. The Matriarch's body has its own, `MatriarchBody.mat`, with emission |

To make another type, duplicate the closest one's three files (four with a body material), point the
prefab at the new definition and leg, and add it to a wave. Keep **Ground Offset** below **Max Leg Distance**,
or the legs can't reach the ground. The bigger the hitbox, the easier it is for projectiles to hit.

## Rules

- Stats go in the Enemy Definition asset, not on the prefab.
- Shared scripts never mention a specific enemy.
- Enemy-specific scripts only react to `Enemy` and `EnemyMovement` (their events, `IsAlive`, `Velocity`).
  They never move the enemy or change its health.
- If an enemy needs time to play a death animation, set `Death Duration` on its `Enemy` component.

## How other systems use enemies

| System       | Uses                                                                         |
|--------------|------------------------------------------------------------------------------|
| Wave manager | `Instantiate(prefab)` then `enemy.Init(path)`, with the path its group picked |
| Towers       | `Enemy.Active`, `IsAlive`, `RemainingDistance` (smallest = closest to base), `Center` (where they aim) |
| Projectiles  | Collide with the trigger `SphereCollider` on the Enemy layer, then call `TakeDamage()` |
| Economy      | `Enemy.AnyDied` → add `enemy.definition.bounty`                              |
| Base health  | `Enemy.AnyReachedBase` → subtract `enemy.definition.baseDamage`              |

Subscribe to the static events in `OnEnable` and unsubscribe in `OnDisable`.

## Paths

An `EnemyPath` is a GameObject whose children are the waypoints, followed in hierarchy order. Only
their horizontal position matters; enemies follow whatever is on the **Ground** layer beneath them. Any
surface enemies walk on (terrain, bridges) needs a collider on the Ground layer.

A scene can have several paths, one per entrance. The Abandoned Farm has three under `EnemyPaths`, all
ending at the barn's north doors, where `Objetivo` is:

| # | Path        | Entrance                       | Length | Route                                                              |
|---|-------------|--------------------------------|--------|--------------------------------------------------------------------|
| 0 | Bridge      | The river east of the bridge   | 144 m  | Rises out of the river, through the gate, along the bridge, up the jetty and the west shore, past the corral and the windmill |
| 1 | West Bank   | The plateau by the trailer     | 96 m   | Down the slope and along the river, north of the windmill          |
| 2 | North Woods | The forest north of the barn   | 75 m   | Down the valley, past the tractor                                  |

The Bridge path starts in deep water, so its enemies are hidden until they climb out of the river in
the gorge. The bridge's own meshes have no colliders (and the raycast would land on its roofs), so
`Walkways/Bridge` has invisible box colliders on the Ground layer that match its deck, jetty and stairs.

### Which path an enemy walks

The `WaveManager`'s **Paths** list sets the order. Each group in a difficulty's waves has a **Path**
index into that list: `0` sends the whole group over the bridge, `-1` sends each enemy down the next
path in turn, so the group comes from every entrance at once. The turn carries on between groups and
waves. With the list empty, the `WaveManager` uses every `EnemyPath` in the scene, sorted by name.

| Difficulty | Wave 1                     | Wave 2                                                   | Wave 3                                                           | Wave 4                                                             |
|------------|----------------------------|----------------------------------------------------------|------------------------------------------------------------------|--------------------------------------------------------------------|
| Easy       | 5 Mimics, Bridge           | 4 Mimics West Bank, then 6 Crawlers North Woods          | 10 Mimics and 8 Crawlers, all; a Brute West Bank; the Matriarch North Woods | -                                                    |
| Hard       | 6 Mimics and 4 Crawlers, all | 6 Mimics West Bank, 6 Crawlers North Woods, a Brute West Bank | 12 Mimics, 12 Crawlers and 3 Brutes, all                    | 14 Mimics, 14 Crawlers and 4 Brutes, all; the Matriarch North Woods |

The Matriarch walks the shortest path, North Woods, so the slowest enemy still arrives in time.

### Trail

`EnemyPathTrail`, on each path, draws it on the ground when the game starts: a dark red band of claw
marks (`Paths/EnemyTrail.mat`) that crawls towards the base, so the player sees where enemies come from.
It lies on whatever is on the Ground layer, like the enemies, so it follows the bridge's walkway too.
Colour, width and speed are on the component.

### Adding a path

1. Under `EnemyPaths`, add an empty GameObject with `EnemyPath` and `EnemyPathTrail` (material
   `Paths/EnemyTrail.mat`, Ground Layer `Ground`).
2. Add the waypoints as children, from the entrance to the base, in order. Give the path its own
   Gizmo Color to tell it apart where it meets the others.
3. Add it to the end of the `WaveManager`'s **Paths** list and point wave groups at its index.
