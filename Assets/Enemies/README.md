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
| Wave manager | `Instantiate(prefab)` then `enemy.Init(path)`                                |
| Towers       | `Enemy.Active`, `IsAlive`, `RemainingDistance` (smallest = closest to base), `TakeDamage()` |
| Economy      | `Enemy.AnyDied` → add `enemy.definition.bounty`                              |
| Base health  | `Enemy.AnyReachedBase` → subtract `enemy.definition.baseDamage`              |

Subscribe to the static events in `OnEnable` and unsubscribe in `OnDisable`.

## Paths

An `EnemyPath` is a GameObject whose children are the waypoints, followed in hierarchy order. Only
their horizontal position matters; enemies follow whatever is on the **Ground** layer beneath them. Any
surface enemies walk on (terrain, bridges) needs a collider on the Ground layer.
