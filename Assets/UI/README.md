# In-game UI

Everything the player sees and clicks while playing is in one prefab, `GameUI.prefab`, placed in the Abandoned Farm.
The scripts live in `Assets/Scripts/UI`; they only read the game systems and call their public methods.

```
GameUI.prefab
├─ SlotPicker               mouse over a turret slot → highlight, click → select
├─ BuildMenu                the panel for the selected slot: build, target priority, sell
├─ GameHud                  the top bar, the wave button and the speed buttons
├─ EndScreen                victory or defeat, with Restart and Menu
├─ RangeRing                GroundRing in the world, shows the range of the turret in question
└─ Canvas                   Screen Space Overlay, 1920 × 1080, sorting order -10 (under the pause menu)
   ├─ TopBar                barn health, wave, time, money
   ├─ NextWave              "Chamar agora", only while counting down to a wave
   ├─ Speed                 1x / 2x
   ├─ Hint                  the controls, bottom left
   ├─ BuildPanel            one card per turret (Options/Option is the template, left inactive)
   ├─ TurretPanel           the selected turret: description, stats, target priority, sell
   └─ EndScreen             full-screen overlay, hidden until the game ends
```

## How it plays

| Input | What happens |
|---|---|
| Mouse over a ring on the ground | The slot lights up (`TurretSlot.SetHighlight`) |
| Left click on a free slot | The build panel opens. Cards the player can't afford are greyed out. Pointing at a card describes the turret and shows its range on the ground |
| Left click on a card | Pays for the turret (`Economy`) and builds it. The panel switches to the new turret |
| Left click on a slot with a turret | The turret panel opens, with its range on the ground |
| Target buttons | Change which enemy that turret shoots first (`Turret.Priority`) |
| Vender | Removes the turret and gives back 70 % of its cost |
| Right click, X, or a click on the ground | Closes the panel |
| Chamar agora | Starts the next wave now (`WaveManager.StartNextWaveNow`) |
| 1x / 2x | Game speed (`GameSpeed`). Pausing and the end of the game stop time; resuming goes back to the chosen speed |

The mouse is ignored over the UI, while paused and after the game ends.

## Money

`Economy` (on the `Game` object) starts at the difficulty's **Starting Money** (Easy 400, Hard 300), adds each killed
enemy's **Bounty** (`EnemyDefinition`, 10 for the Mimic) and pays for turrets (`TurretDefinition.cost`). Selling refunds
**Sell Refund** (70 %) of the cost.

## Style

It follows the start and pause menus: black panels, bone-coloured Liberation Sans for text, buttons that turn dark red
under the mouse, and IM Fell English SC for titles. That font's original asset has no accented letters, so titles use
`Fonts/IMFellEnglishSC-Regular SDF Accents.asset`, made from the same font with ASCII and Portuguese accents. It's
static, so playing doesn't change it.

The turret pictures on the cards are each definition's **Icon** (`Assets/Turrets/Icons`, rendered from the prefabs), and
the line under a turret's name is its **Description**.

## Changing it

- **Texts and layout** are in the prefab. Scripts only fill in numbers and names.
- **A new turret** in the build menu: add its definition to `GameUI` → `BuildMenu` → **Turrets**, and give it an Icon and
  a Description.
- **Colours** of the on/off buttons are on `BuildMenu` and `GameHud`. Other colours are on the images and texts.
