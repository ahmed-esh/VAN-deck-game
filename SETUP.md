# Van Trip Budget Game — Unity Setup Guide

This guide explains how to wire the Van Trip game in Unity: dual canvases, map, driving/card gameplay (M4), stats HUD, and Inspector-driven data (ScriptableObjects).

---

## Quick start (recommended)

1. Open `Assets/Scenes/SampleScene.unity` (or any scene).
2. Menu: **Van Game → Build UI Hierarchy In Scene**
   - Creates sample data under `Assets/Data/`
   - Builds `GameManager`, `Canvas_Cards`, `Canvas_Map`, HUD, map regions, and wires references
3. Press **Play**
   - Map opens for first destination pick
   - Click a city → cinematic → **card canvas** with hand + driving timer
   - Hover the **card hand area** at the bottom → cards fan out (`CardHandHoverFan`)
   - Click a card → stats change, timer advances, card discards, next card draws from pool
   - Idle time slowly fills the day bar; when full → end of day (fuel drain, hunger check)
   - After all leg days → map opens for next city

Optional debug: **GameManager → Game Flow Controller → Debug/Complete Driving Leg** skips remaining driving days.

---

## Scene hierarchy (after wizard)

```
EventSystem
GameManager
├── GameFlowController
├── StatResolver
├── DeckController
├── EndOfDayResolver
├── DrivingTurnController
├── CanvasTransitionController
└── MapController

Canvas_Cards          (Sort Order 0 — main gameplay)
├── StatsHUD
├── DrivingPanel
│   ├── DrivingTimer
│   └── CardHandArea      (CardHandHoverFan + CardHandController)
├── Button_OpenMap

Canvas_Map            (Sort Order 10 — disabled until opened)
├── MapShadeOverlay
├── MapRoot
│   ├── MapBackground
│   └── MapRegions
│       ├── Region_CityA      → MapRegionView
│       ├── Region_Dentone
│       ├── Region_Southridge
│       ├── Region_Argylle
│       └── Region_CityB
├── MapStatsTooltip
└── Button_CloseMap
```

---

## Manual setup (if not using the wizard)

### Step 1 — Data folders

Create:

- `Assets/Data/`
- `Assets/Data/Cities/`
- `Assets/Data/Cards/`
- `Assets/Data/Decks/`

Or run **Van Game → Create Sample Data Assets**.

### Step 2 — Game Config

**Create → Van Game → Game Config** → save as `Assets/Data/GameConfig.asset`

| Field | Default | Notes |
|-------|---------|-------|
| Starting Money | 500 | |
| Starting Fuel/Morale/Van | 100 | All 0–100% |
| Max Trip Days | 20 | Win if reach City B with day ≤ 20 |
| Canvas Transition Duration | 0.4 | DOTween |
| City Select Shade Alpha | 0.65 | Dark overlay on city confirm |

### Step 3 — Cities

**Create → Van Game → City Definition** for each city.

**Required flags (exactly one of each):**

- One city: `Is Start City` ✓ (City A)
- One city: `Is Destination City` ✓ (City B)

**Connections (parallel lists — same length):**

| City | Neighbor Cities | Driving Days To Neighbor |
|------|-----------------|--------------------------|
| City A | Dentone, Southridge | 2, 3 |
| Dentone | City A, Argylle, City B | 2, 4, 6 |
| … | … | … |

**Profile fields (tooltip display):**

- Parking, Cost Of Living, Fun Theme, Base Morale Delta, Stay Days In City

**No backtracking:** once visited, a city cannot be selected again (except current city display).

### Step 4 — Cards & deck (foundation for M4)

**Create → Van Game → Action Card** for each card.

**Create → Van Game → Deck Definition**:

- `Starting Hand Cards` — exact cards at run start (order matters)
- `Draw Pool Cards` — sequential draw when a card is played (not random)

Assign deck on **GameManager → Deck Controller → Deck Definition**.

### Step 5 — GameManager wiring

On empty GameObject `GameManager`, add:

| Component | Purpose |
|-----------|---------|
| GameFlowController | State machine |
| StatResolver | Applies stat changes |
| DeckController | Hand / draw pool |
| CanvasTransitionController | Canvas DOTween transitions |
| MapController | Map regions + tooltip |

**Game Flow Controller Inspector:**

| Field | Assign |
|-------|--------|
| Game Config | GameConfig.asset |
| Start City | City A asset |
| Destination City | City B asset |
| Deck Definition | MainDeck.asset |
| Stat Resolver | same GameObject |
| Deck Controller | same GameObject |
| Canvas Transition | same GameObject |
| Map Controller | same GameObject |
| Stats Hud | StatsHUD object |
| Open Map Button | Button_OpenMap |
| Close Map Button | Button_CloseMap |
| Driving Panel | DrivingPanel |

### Step 6 — Canvas transition wiring

On **CanvasTransitionController**:

| Field | Assign |
|-------|--------|
| Card Canvas | Canvas_Cards |
| Map Canvas | Canvas_Map |
| Map Canvas Group | CanvasGroup on Canvas_Map |
| Map Root | MapRoot RectTransform |
| Map Shade Overlay | MapShadeOverlay CanvasGroup |
| Game Config | GameConfig.asset |

Both canvases need **CanvasGroup** (card canvas fades when map opens).

### Step 7 — Map UI (your 2D map art)

1. Put full map drawing on **MapBackground** Image (`Raycast Target` **off**).
2. For each clickable city, add a child under **MapRegions**:
   - **UI Image** (semi-transparent hit area, `Raycast Target` **on**)
   - **Map Region View** component
   - Assign **City Definition** asset
   - Optional: child **Highlight Image** for glow on hover
3. Size/position each region Image over the city on your map art.

**Map Region View Inspector:**

| Field | Notes |
|-------|-------|
| City | CityDefinition asset |
| Lift Target | Usually the region RectTransform |
| Hover Lift Y | 24 — DOTween lift on hover |
| Hover Duration / Ease | DOTween settings |
| Tooltip | MapStatsTooltip object |

### Step 8 — Map tooltip

**Map Stats Tooltip View** on `MapStatsTooltip` panel. Wire TMP fields:

- City Name, Parking, Cost, Fun, Morale, Stay Days, Driving Days, Status

Tooltip shows driving days **from current city** to hovered city.

### Step 9 — Map Controller

| Field | Assign |
|-------|--------|
| Map Regions | All MapRegionView components |
| Tooltip | MapStatsTooltip |
| Close Map Button | Button_CloseMap GameObject |

### Step 10 — Stats HUD

**Stats Hud View** — wire TMP texts for Money, Fuel, Morale, Van, Day.

Optional: Image fill bars (Image type **Filled**) for fuel/morale/van.

---

## Play flow (M1–M4)

1. **Run starts** at City A with starting stats from GameConfig.
2. **Map opens** (forced) — pick first neighbor.
3. **Click** reachable city → cinematic → **Driving** phase on card canvas.
4. **Card hand** — hover to fan; click card to play (DOTween out, draw next from pool).
5. **Driving timer** — card `Real Time Seconds` advances the day bar; idle drift uses `Idle Time Multiplier` on GameConfig.
6. **End of driving day** — daily fuel drain; if not fed, morale penalty; trip day +1; leg days −1.
7. **Leg complete** — arrive at city, map opens for next destination (visited cities disabled).
8. **Open Map** during driving — timer pauses; Close returns to Driving phase.

## M4 — Driving & cards (Inspector)

### GameManager components

| Component | Role |
|-----------|------|
| DrivingTurnController | Timer, card play, end-of-day, leg completion |
| EndOfDayResolver | Fuel drain + unfed morale penalty |
| DeckController | Hand + sequential draw pool |

Wire on **Driving Turn Controller**: Game Flow, Game Config, Stat Resolver, Deck Controller, End Of Day Resolver, Card Hand, Timer View.

### CardHandArea

| Component | Role |
|-----------|------|
| CardHandHoverFan | Fan on hover (existing prototype script) |
| CardHandController | Spawns `CardView` prefab per hand card |
| BoxCollider2D | Auto-sized to hand rect |

| Field | Assign |
|-------|--------|
| Hand Container | CardHandArea RectTransform |
| Hover Fan | CardHandHoverFan on same object |
| Card Prefab | `Assets/Prefabs/VanGame/CardView.prefab` |

### Action cards (ScriptableObject)

Key fields for driving:

| Field | Effect |
|-------|--------|
| Money Cost Min/Max | Spend money (max used for afford check) |
| Morale/Fuel/Van deltas | Applied on play |
| Real Time Seconds | Advances driving day timer |
| Counts As Fed Today | Avoids end-of-day hunger penalty |

### GameConfig driving fields

| Field | Default | Notes |
|-------|---------|-------|
| Driving Day Real Time Seconds | 60 | One in-game day ≈ this many seconds when playing cards |
| Idle Time Multiplier | 0.05 | Slow drift when not playing cards |
| Daily Fuel Drain Percent | 5 | End of each driving day |
| Unfed Morale Penalty Percent | 50 | If no food card played that day |
| Card Play/Draw durations | DOTween | Card exit + new card entrance |

---

## Adding content (no code edits)

| Content | How |
|---------|-----|
| New city | Create City Definition asset, wire neighbors on adjacent cities, add Map Region Image |
| New card | Create Action Card asset, add to Deck starting hand or draw pool |
| Tune transitions | Edit GameConfig asset |
| Tune hover lift | Edit Map Region View on each region |
| New random event | Create Random Event asset (M5) |
| New ability | Create Ability Definition asset (M5) |

---

## Script locations

```
Assets/Scripts/VanGame/
├── Data/           ScriptableObject definitions
├── Core/           RunState, StatResolver, DeckController
├── UI/             Map, HUD, canvas transitions
├── GameFlowController.cs
└── Editor/         VanGameSetupWizard.cs (menu items)
```

Existing card hand fan: `Assets/Ahmed/Prototype 1/CardHandHoverFan.cs` (used in M4).

---

## Troubleshooting

| Problem | Fix |
|---------|-----|
| Map regions don't hover | EventSystem in scene; region Image Raycast Target on |
| Tooltip empty | Assign City Definition on MapRegionView |
| Can't click city | City must be neighbor of current city and not visited |
| Canvas doesn't fade | CanvasGroup on both canvases |
| No stats on HUD | Stats Hud View bound; GameManager has GameConfig + Start City |
| Map stays open | Check Map Canvas Group alpha; run wizard again to re-wire |

---

## Next milestones (not yet implemented)

- **M6:** Card tier unlocks, special hidden effects, full polish

---

## M5 — City arrival, abilities, win/lose

### Flow after a driving leg

1. **City arrival** — apply city morale + stay days
2. **Random events** — weighted rolls from each city's `Possible Events` list
3. **Event log** — DOTween fade-in lines, **Continue**
4. **Ability pick** — 3 choices (first city uses `Ability Catalog → First City Rewards`)
5. **Map** — pick next destination (or **Win** at City B)

### New ScriptableObjects

| Asset | Menu path |
|-------|-----------|
| `AbilityCatalog` | Van Game / Ability Catalog |
| `RandomEventDefinition` | Van Game / Random Event (already existed) |

**Random Event** fields: optional parking/cost filters, money/morale/fuel/van/day effects, `Log Text` for the event log.

**Ability Catalog**:

- `First City Rewards` — Selfless / Bargainer / Practitioner (sample)
- `General Pool` — later cities; excludes abilities already owned

### New components (GameManager)

| Component | Role |
|-----------|------|
| `CityArrivalController` | Orchestrates arrival → log → ability pick |
| `CityRandomEventResolver` | Weighted event rolls |

### UI panels (on Canvas_Cards)

| Panel | Component |
|-------|-----------|
| `CityArrivalPanel` | Contains `EventLogView` + `AbilityPickPanel` |
| `WinPanel` | `WinLoseView` + Restart |
| `LosePanel` | `WinLoseView` + Restart |

Tune arrival UI timing on **Game Config** → City Arrival / City Arrival UI sections.

### Win / lose

| Condition | Result |
|-----------|--------|
| Reach **City B** with trip day ≤ 20 | Win screen |
| Fuel, morale, or van ≤ 0 | Lose screen |
| Trip day > 20 | Lose screen |

Restart button calls **Start New Run** (full reset).
