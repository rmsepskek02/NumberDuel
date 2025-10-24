# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**NumberDuel** is a Unity-based multiplayer card game built with Photon PUN2 (Photon Unity Networking). Players compete in a mathematical duel by placing number cards and operator cards (+, -, ×, ÷) on a field, attacking opponents, and managing HP. The game features Secret cards (face-down), Joker cards with special abilities, and turn-based synchronization across network clients.

**Product Name**: NumberDuel
**Company**: GoroCompany
**Platform**: PC, Mobile (responsive design with safe area support)

## Build and Development Commands

### Unity Development
- Open the project in Unity Editor (2021.3 or higher recommended)
- Main scene: `Assets/Scenes/GameScene.unity`
- Play scenes in order: `SplashScene` → `JoinScene` → `LobbyScene` → `GameScene`

### Testing Multiplayer
- Build the project to create multiple client instances
- Each client saves its settings in `ClientSettings.txt` (window resolution, etc.)
- Use Photon's Cloud or local server for testing

### Important Unity Build Settings
- Default resolution: 1280x720
- Screen orientation: Auto-rotation enabled
- Company: GoroCompany
- Product: NumberDuel

## High-Level Architecture

### Core Game Flow
1. **SplashScene**: Initial loading and settings
2. **JoinScene**: Connect to Photon network and set player nickname
3. **LobbyScene**: Create/join rooms
4. **GameScene**: Main gameplay with turn-based card battles

### Manager System (Singleton Pattern)
The game uses a centralized manager architecture with singleton instances:

#### Network & Synchronization
- **PhotonManager** (`Assets/Scripts/Managers/PhotonManager.cs`): Handles Photon callbacks (player join/leave, room events). Manages card color synchronization via room properties.
- **NetworkGameManager** (`Assets/Scripts/Managers/NetworkGameManager.cs`): Central RPC hub for ALL network synchronization:
  - Card draw synchronization (`SyncCardDraw`)
  - Card placement synchronization (`SyncCardPlacement`)
  - Combat action synchronization (`SyncCombatAction`)
  - Operator/Joker effect synchronization
  - NetworkCard registry system for unique card IDs
  - **IMPORTANT**: All owner types are converted between perspectives (Player ↔ Opponent) when syncing across clients

#### Turn Management
- **TurnManager** (`Assets/Scripts/Managers/TurnManager.cs`): Controls turn flow using RPC system (NOT PunTurnManager).
  - First player determined at game start
  - Turn numbering: Odd turns (1,3,5...) = first player, Even turns (2,4,6...) = second player
  - Handles `IsLocalPlayerTurn` logic and turn transitions via `EndTurn()`

#### Game State & Logic
- **InGameManager** (`Assets/Scripts/Managers/InGameManager.cs`): Core game loop manager
  - Process state management (`GameProcessState` enum)
  - Card draw system with 10-card hand limit
  - Game end detection and restart logic
  - Field card registry
- **DeckManager** (`Assets/Scripts/Managers/DeckManager.cs`): Deck initialization and card distribution
- **HealthManager** (`Assets/Scripts/Managers/HealthManager.cs`): HP tracking for both players
- **FieldAttackManager** (`Assets/Scripts/Managers/FieldAttackManager.cs`): Attack logic and combat resolution
- **OperatorManager** (`Assets/Scripts/Managers/OperatorManager.cs`): Handles operator card usage (+, -, ×, ÷)
- **ExpressionZoneManager** (`Assets/Scripts/Managers/ExpressionZoneManager.cs`): Visual display of math expressions during combat/operations

#### UI & Resources
- **InGameUIManager** (`Assets/Scripts/Managers/InGameUIManager.cs`): In-game UI updates and event registration
- **ResourcesManager** (`Assets/Scripts/Managers/ResourcesManager.cs`): Card color management (Red, Green, Purple, Yellow) and sprite loading
- **ScreenManager**, **SplashManager**, **JoinManager**, **LobbyManager**: Scene-specific UI management

### Card System Architecture

#### Card Types (Enum)
Defined in `Assets/Scripts/Objects/ETC/Enum.cs`:
- **CardType**: `Number`, `Operator`, `Joker`
- **OperatorType**: `Plus`, `Minus`, `Multiply`, `Divide`
- **JokerEffectType**: `Draw`, `Delete`, `Swap`

#### Card Components
- **Card** (`Assets/Scripts/Objects/Card/Card.cs`): Main card logic
  - State: `CardType`, `OperatorType`, `IsSecret`, `CanAttack`
  - Turn tracking: `WasPlayedThisTurn`, `HasAttackedThisTurn`, `WasModifiedThisTurn`
  - Secret mode: Hides card value visually using `CardEffect` component
  - Glow system: Green glow for attackable cards, uses `CardEffect.SetGlow()`
  - Interaction: Click/drag handling via `ObjectMouseEvent`
- **NetworkCard** (`Assets/Scripts/Objects/Card/NetworkCard.cs`): Unique ID system for network synchronization
  - Auto-generates 8-character alphanumeric IDs
  - Registers with `NetworkGameManager` for cross-network card tracking
- **CardZone** (`Assets/Scripts/Objects/Card/CardZone.cs`): Container for cards
  - `ZoneType`: `Hand`, `Field`
  - `OwnerType`: `Player`, `Opponent`
  - Layout management using `CardLayoutHelper`
- **CardText** (`Assets/Scripts/Objects/Card/CardText.cs`): Displays card values
- **CardEffect** (`Assets/Scripts/Objects/Card/CardEffect.cs`): Visual effects (glow, secret mode) using Material Property Blocks

### Network Synchronization Patterns

**Critical**: When syncing data across clients, always convert owner perspective:
- **Sender's view**: `Player` card → **Receiver's view**: `Opponent` card
- **NetworkGameManager** handles this conversion automatically in `ApplyRemoteCardPlacement()` and `ApplyRemoteCombatAction()`

**Card Placement Flow**:
1. Local player places card → `NetworkCard.OnCardPlaced()` triggers
2. `NetworkGameManager.SyncCardPlacement()` sends RPC to opponent
3. Opponent receives `RPC_SyncCardPlacement()` → creates card with converted owner
4. Back card removed from opponent's hand to maintain card count

**Combat Flow**:
1. Local player attacks → `FieldAttackManager` calculates damage
2. `NetworkGameManager.SyncCombatAction()` sends combat data
3. Opponent receives `RPC_SyncCombatAction()` → reveals Secret cards, updates ExpressionZone, applies damage

### Key Utilities & Constants
- **Global** (`Assets/Scripts/ETC/Global.cs`): Centralized constants
  - Colors: `Red`, `Green`, `Purple`, `Yellow`, `GlowGreen`, `GlowRed`
  - Symbols: `Plus`, `Minus`, `Multiply`, `Divide`, `Equal`
  - Resource paths and material names
- **Singleton** / **SingletonDontDestroy** (`Assets/Scripts/Utills/`): Base classes for managers
- **JsonUtills** (`Assets/Scripts/Utills/JsonUtills.cs`): Client settings serialization

### Game Rules & Logic
- **First Round**: No attacks allowed (`TurnManager.IsFirstRound`)
- **Attack Eligibility**: Cards cannot attack if:
  - Played this turn (`WasPlayedThisTurn`)
  - Modified this turn (`WasModifiedThisTurn`)
  - Already attacked this turn (`HasAttackedThisTurn`)
  - Not in Field zone
  - Not owned by current player
- **Card Limit**: Maximum 10 cards in hand; excess draws are discarded
- **Secret Cards**:
  - Player's Secret cards show value to owner only
  - Opponent's Secret cards are completely hidden
  - Automatically revealed when attacking or being attacked

## Common Development Patterns

### Adding a New Manager
1. Inherit from `Singleton<T>` or `SingletonDontDestroy<T>`
2. Place in `Assets/Scripts/Managers/` namespace `Manager`
3. Register with `InGameManager` if needed for game flow
4. Use `FindAnyObjectByType<>` for cross-manager communication

### Implementing Network Features
1. **ALL network synchronization goes through `NetworkGameManager`**
2. Use `NetworkCard.UniqueId` to identify cards across clients
3. Always check `PhotonNetwork.InRoom` and player count before RPC
4. Convert owner types when applying remote changes
5. Use `[PunRPC]` attribute for RPC methods
6. Serialize data as JSON using `[Serializable]` classes

### Card Interaction Flow
1. User input → `ObjectMouseEvent` (click/drag)
2. `Card.HandleClick()` or `Card.HandleEndDrag()`
3. Check game state (`InGameManager.IsProcessing`, `TurnManager.IsLocalPlayerTurn`)
4. Execute action → Update local state
5. Sync via `NetworkGameManager` RPC if multiplayer action

### Process State Management
Use `InGameManager.StartProcess()` / `EndProcess()` to prevent concurrent actions:
```csharp
if (!InGameManager.Instance.StartProcess(GameProcessState.CardAttackProcess))
    return; // Another process is running

// ... perform action ...

InGameManager.Instance.EndProcess();
```

## Important Implementation Notes

### DOTween Usage
- Used extensively for card animations (`Card.AnimateRemoval()`)
- Always kill tweens properly to avoid memory leaks
- Set tween targets for proper cleanup

### Material Property Blocks
- `CardEffect` uses Material Property Blocks to avoid material instance creation
- This prevents Material conflicts when switching sprites (Secret mode)
- Call `CardEffect.OnSpriteChanged()` after changing `SpriteRenderer.sprite`

### Photon Room Properties
- Card colors stored in room custom properties: `masterPlayerColor`, `guestPlayerColor`
- Current player count tracked via `currentPlayers` property
- Use `PhotonNetwork.CurrentRoom.SetCustomProperties()` to update

### Responsive Design
- Use `FixedAspectCamera` for consistent camera view
- Safe area handling via `ResponsiveObjectSafeArea` (mobile devices)
- Background components: `FillScreenBackground`, `KeepAspectBackground`

## Known Architecture Decisions

1. **No PunTurnManager**: Custom `TurnManager` with RPC provides better control
2. **Centralized RPC**: All network calls route through `NetworkGameManager` for consistency
3. **NetworkCard Registry**: Maintains card references by ID to handle card lookups during RPC
4. **Process State System**: Prevents race conditions during multiplayer actions
5. **Owner Perspective Conversion**: Critical for ensuring both clients see consistent game state from their own perspective
