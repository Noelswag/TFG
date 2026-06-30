# Dynamic Environments in Tactical RPGs (TFG Prototype)

A turn-based tactical RPG prototype developed as part of a TFG (Bachelor's Thesis). The project explores the implementation of dynamic, mutable terrain transformations and status effect reaction mechanics within a grid-based combat framework.

---

## 🎮 Key Features

* **Two-Team Alternating Phase System**: Segregates player units and opposing units into discrete turn phases (`Team.Player` vs. `Team.Enemy`). Includes input lockout guards during the enemy's turn phase.
* **Class-Specific Action Menus**:
  * **Warrior**: Options to perform a melee **Attack** on adjacent enemy targets or **Wait**.
  * **Mage**: Options to cast elemental spells (**Ice**, **Fire**, **Earth**, **Wind**) or **Wait** directly from the UI canvas.
* **Elemental Status Reactions**: Combining status elements on a unit triggers unique side-effects:
  * **Ice + Fire = Extinction**: Deals direct damage and applies a speed boost.
  * **Ice + Earth = Freeze**: Prevents unit movement by reducing speed budget to 0.
  * **Wind + Fire = Burst**: Deals AoE damage to the target and adjacent neighbors.
  * **Wind + Earth = Blur**: Boosts unit movement speed.
* **Mutable Terrain Transformations**: Cast spells to dynamically alter the grid landscape (e.g. Grass + Fire = burning Fire terrain; Water + Ice = Ice terrain for straight sliding). Traversal and stop effects adjust unit stats (Evasion, HP healing, extra damage, movement cost penalties).
* **Command Pattern History (Undo/Redo)**: Every gameplay action is encapsulated as a command object, allowing players to undo (`U`) or redo (`R`) movements and attacks before committing their turn. Transitions lock in and clear command history.

---

## 🏛️ Software Architecture

The codebase is strictly decoupled into separate C# assembly definitions using a Model-View-ViewModel (MVVM) approach:

* **Core Assembly (`Core.csproj`)**: Contains the pure C# domain model (characters, grid positions, terrain rules, and Dijkstra pathfinding) and the command transaction engine. Has no dependencies on the Unity Engine.
* **View Assembly (`View.csproj`)**: Manages presentation logic (smooth animations, visual outlines, tile coloring, and the Canvas UI menus).
* **Editor Assembly (`Core.Tests.csproj`)**: Contains the NUnit test suite validating pathfinding, terrain costs, status thaws, and undo operations.

---

## 🛠️ Tech Stack

* **Engine**: Unity 6
* **Language**: C# 12
* **Unit Testing**: Unity Test Framework (NUnit)
* **Assets**: Aseprite (2D Pixel Art)

---

## 🕹️ Controls & Hotkeys

| Action | Control |
| :--- | :--- |
| **Select / Move Unit** | Left Click on unit, then click a highlighted reachable tile |
| **Open Action Menu** | Click on a selected character again (or finish a movement) |
| **Trigger Action** | Click **Attack**, **Wait**, or a **Spell** button on the UI canvas |
| **Cancel Targeting** | Click on any invalid tile during targeting to return to the menu |
| **Transition Turn** | Press `Space` (Ends current turn phase, ticks statuses, locks history) |
| **Undo Last Action** | Press `U` (Only available during Player turn before ending phase) |
| **Redo Undone Action** | Press `R` |
| **Paint Terrain (Dev Mode)** | Hover over any tile and press numeric keys `1` through `8` |
| **Cast Spell (Dev Mode)** | Press `F` (Fire), `I` (Ice), `W` (Wind), or `E` (Earth) to cast on the hovered tile |
