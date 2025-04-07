//                                        NARRATIVE/TACTICAL SQUAD COMBAT ARCHITECTURE
//                                      ===============================================

//  +--------------------------------------------------------------------------------------------------------------------------------+
//  |                                         CORE GAME MANAGER                                                                      |
//  |  +---------------+  +-----------------+  +----------------+  +----------------+  +-------------------+  +-------------------+  |
//  |  | Scene Manager |  | Game State Mgr  |  | Event System   |  | Resource Mgr   |  | Audio Manager     |  | Audio Manager     |  |
//  |  +---------------+  +-----------------+  +----------------+  +----------------+  +-------------------+  +-------------------+  |
//  +--------------------------------------------------------------------------------------------------------------------------------+
//                  |               |                |                  |                     |                       |
//                  ↓               ↓                ↓                  ↓                     ↓                       ↓
//  +----------------------------------------------------------------------------------------------------+
//  |                                                                                                    |
//  |  +-------------------+       +-------------------+        +-------------------+       +--------+   |
//  |  | PLAYER SYSTEMS    |       | SQUAD SYSTEMS     |        | WORLD SYSTEMS     |       | UI     |   |
//  |  |-------------------|       |-------------------|        |-------------------|       |--------|   |
//  |  | - Movement        |       | - Command System  |        | - Environment     |       | - HUD  |   |
//  |  | - Camera Control  |<----->| - AI Controller   |<------>| - Physics         |<----->| - Menus|   |
//  |  | - Input Handler   |       | - Formation Mgr   |        | - Interaction Sys |       | - Map  |   |
//  |  | - Inventory       |       | - Specializations |        | - Mission System  |       | - Tact |   |
//  |  | - Weapons System  |       | - Behavior Trees  |        | - Faction System  |       | - Dial |   |
//  |  | - Abilities       |       | - Context AI      |        | - Spawning System |       +--------+   |
//  |  +-------------------+       +-------------------+        +-------------------+                    |
//  |                                                                                                    |
//  +----------------------------------------------------------------------------------------------------+
//                  |               |                |                  |                     |
//                  ↓               ↓                ↓                  ↓                     ↓
//  +-------------------------------------------------------------------------------------------------------------------------+
//  |                                       MODULAR COMPONENT SYSTEM                                                          |
//  |                                                                                                                         |
//  |  +----------------+  +----------------+  +----------------+  +----------------+  +----------------+  +----------------+ |
//  |  | Character      |  | Combat         |  | Tactical       |  | AI             |  | Physics        |  | Interaction    | |
//  |  | Components     |  | Components     |  | Components     |  | Components     |  | Components     |  | Components     | |
//  |  |----------------|  |----------------|  |----------------|  |----------------|  |----------------|  |----------------| |
//  |  | - Health       |  | - Weapon       |  | - Cover System |  | - NavAgent     |  | - Rigidbody    |  | - Interactable | |
//  |  | - Stats        |  | - Ammunition   |  | - Line of Sight|  | - Perception   |  | - Collider     |  | - Pickupable   | |
//  |  | - Inventory    |  | - Damage       |  | - Threat Assess|  | - BehaviorTree |  | - Raycast      |  | - Usable       | |
//  |  | - Controller   |  | - Projectile   |  | - Tactical Pos |  | - PathFinding  |  | - Trigger      |  | - Door         | |
//  |  | - Faction      |  | -              |  | - Formation    |  | - TeamAwareness|  | - Destruction  |  | - Terminal     | |
//  |  +----------------+  +----------------+  +----------------+  +----------------+  +----------------+  +----------------+ |
//  +-------------------------------------------------------------------------------------------------------------------------+
//                  |               |                |                  |                     |
//                  ↓               ↓                ↓                  ↓                     ↓
//  +-----------------------------------------------------------------------------------------------------+
//  |                                        DATA MANAGEMENT                                              |
//  |                                                                                                     |
//  |  +------------------+  +----------------+  +----------------+  +----------------+  +--------------+ |
//  |  | Scriptable       |  | Save System    |  | Config Manager |  | Asset Bundles  |  | Analytics    | |
//  |  | Objects          |  |                |  |                |  |                |  |              | |
//  |  |------------------|  |----------------|  |----------------|  |----------------|  |--------------| |
//  |  | - WeaponData     |  | - PlayerSave   |  | - GameSettings |  | - Level Data   |  | - Telemetry  | |
//  |  | - EnemyData      |  | - WorldSave    |  | - Controls     |  | - Models       |  | - Metrics    | |
//  |  | - SquadRoleData  |  | - ProgressSave |  | - Graphics     |  | - Textures     |  | - Performance| |
//  |  | - TacticalData   |  | - SquadSave    |  | - Audio        |  | - Effects      |  | - Debugging  | |
//  |  +------------------+  +----------------+  +----------------+  +----------------+  +--------------+ |
//  +----------------------------------------------------------------------------------------------------+

//      Core Game Manager
//      This is the top-level management layer that coordinates all major game systems:
//   +------------------------------------------------------------------------------------------------------------+
//      •	Scene Manager:      Handles loading/unloading levels and transitions
//      •	Game State Manager: Controls game flow states (menu, gameplay, pause, etc.)
//      •	Event System:       Provides communication between decoupled systems via events
//      •	Resource Manager:   Handles asset loading/unloading and memory management
//      •	Audio Manager:      Controls sound effects, music, and voice lines

//      Main Game Systems
//      The architecture divides gameplay into four interconnected domains:
//   +------------------------------------------------------------------------------------------------------------+
//      Player Systems
//      Handles everything related to the player character:
//      •	Movement & Camera Control: Player navigation and viewpoint
//      •	Input Handler: Processes keyboard/mouse/controller input
//      •	Inventory & Weapons: Manages equipment and combat tools
//      •	Abilities: Special actions or powers the player can trigger

//      AI/Squad Systems
//      Manages your AI companions:
//      •	Command System: How you direct squad members
//      •	Formation Manager: Keeps squad members in tactical positions
//      •	AI Controller: Squad member decision-making
//      •	Specializations: Different roles/classes within your squad
//      •	Context AI: Adapts squad behavior based on situation (combat, exploration)

//      World Systems
//      Controls the game environment:
//      •	Environment: Manages terrain, obstacles, and level structure
//      •	Physics: Handles physical interactions and simulations
//      •	Interaction System: How entities interact with the environment
//      •	Mission System: Quest / objective management
//      •	Faction System: Relationships between different groups in the game

//      UI
//      The interface layer :
//      •	HUD: In - game displays and information
//      •	Menus: Game options, inventory screens, etc.
//      •	Map: Navigation tools
//      •	Tactical: Displays for squad commands and combat information

//      Modular Component System
//      This is the foundation for all game entities using a component-based architecture:
//      •	Character Components: Base attributes for characters (health, stats)
//      •	Combat Components: Weapons, ammunition, damage systems
//      •	Tactical Components: Cover, line of sight, threat assessment
//      •	AI Components: Navigation, perception, behavior trees
//      •	Physics Components: Physical interactions with the world
//      •	Interaction Components: How objects can be used/manipulated

//      Data Management
//      The persistence and configuration layer:
//      •	Scriptable Objects: Reusable data templates
//      •	Save System: Persistence between game sessions
//      •	Config Manager: Game settings and options
//      •	Asset Bundles: Resource loading and management
//      •	Analytics: Optional telemetry and debugging tools