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

| Difficulty | Wave 1      | Wave 2                       | Wave 3     | Wave 4     |
|------------|-------------|------------------------------|------------|------------|
| Easy       | 5, Bridge   | 4 West Bank, then 4 North Woods | 12, all | -          |
| Hard       | 8, all      | 6 West Bank and 6 North Woods | 16, all   | 20, all    |

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
