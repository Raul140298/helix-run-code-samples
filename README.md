# Helix Run - Code Samples

Code samples from **Helix Run**, a top-down dungeon-crawler roguelite with real-time transformation combat mechanics.

🎮 **Play the demo:** [https://rauljl1.itch.io/helixrun](https://rauljl1.itch.io/helixrun)

## About the Project

Helix Run is a roguelite where players control Gene, a DNA collector who captures monsters ("Mons") and transforms into them to use their unique abilities. The game features:

- Real-time transformation combat inspired by BEN 10
- Elemental types with terrain interactions
- Procedurally generated dungeons
- Evolution mechanics and passive ability systems

## Code Samples

These scripts demonstrate the core Mon (monster) system architecture, showcasing component-based design, async programming, and event-driven patterns.

### Mon.cs
Pure C# class (non-MonoBehaviour) representing a monster instance. Handles:
- Procedural generation of evolution lines with random passives
- Event system for evolution lifecycle (`OnBeforeEvolve`, `OnAfterEvolve`)
- DNA system linking monster tiers and abilities
- Flag-based type checking using bitwise operations

### MonModel.cs
Central component coordinating all Mon subsystems. Demonstrates:
- Composition over inheritance architecture
- Subsystem initialization and wiring
- Async invincibility frames using UniTask and CancellationTokens
- Event aggregation for death, collision, and state changes

### MonStats.cs
Complete stats management system featuring:
- Dynamic health/energy subscription to evolution events
- Async energy recovery with UniTask
- Terrain-based speed modifiers per movement type
- Passive modifier integration through calculated properties

### MonRendering.cs
Visual effects system with priority queue. Showcases:
- `SortedDictionary` for effect prioritization
- Multiple concurrent async effects (damage flash, invincibility, rage, transform)
- Shader/material manipulation for palette swaps
- Proper CancellationToken cleanup

## Tech Stack

- **Engine:** Unity 2D
- **Language:** C#
- **Key Libraries:**
  - [UniTask](https://github.com/Cysharp/UniTask) - Async/await for Unity
  - [DOTween](http://dotween.demigiant.com/) - Tweening engine
  - [Odin Inspector](https://odininspector.com/) - Editor extensions

## Architecture Highlights

- **Component-based design:** Each aspect of a Mon (stats, movement, rendering, abilities) is a separate component
- **Event-driven communication:** Loose coupling between systems via C# events
- **Async patterns:** Proper use of CancellationTokens for coroutine-like behavior without coroutines
- **Data-driven:** ScriptableObjects for Mon definitions, abilities, and passives (not included in samples)

## Contact

Feel free to reach out if you have questions about the implementation details.
