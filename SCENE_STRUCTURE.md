# NumberDuel Unity Scene Documentation

## Overview
This document provides a comprehensive breakdown of the scene hierarchies, object structures, and component configurations for all four Unity scenes in the NumberDuel project.

---

## 1. SplashScene.unity

**File Path**: `Assets/Scenes/SplashScene.unity`

### Purpose
Initial loading scene that handles basic setup and initialization before transitioning to the join screen.

### Root GameObjects (Scene Hierarchy)
```
SplashScene
├── =====Cameras=====
├── =======UI=======
├── =====Managers=====
└── =====Objects=====
```

### Detailed Hierarchy

#### =====Cameras===== (Organizer)
- **Main Camera**
  - Components:
    - Transform (Position: 0, 1, -10)
    - Camera (Perspective, FOV: 60, Clear Flags: Skybox)
    - AudioListener
    - UniversalAdditionalCameraData (URP component)

- **Directional Light**
  - Components:
    - Transform (Rotation: 50, -30, 0)
    - Light (Type: Directional, Intensity: 2, Color Temperature: 5000K)
    - UniversalAdditionalLightData (URP component)

- **Global Volume**
  - Components:
    - Transform
    - Volume (Global: true, Weight: 1, Shared Profile reference)

#### =======UI======= (Organizer)
- Empty organizer object (no UI in Splash scene)

#### =====Managers===== (Organizer)
- **SplashManager**
  - Script: `SplashManager.cs` (guid: 108087141d3984c4eb2af390af089344)
  - Purpose: Manages splash screen timing and scene transition

- **GameManager**
  - Script: `GameManager.cs` (guid: a0315acd1a317264f9510cc1358c22c4)
  - Purpose: Persistent game manager with client settings
  - Properties:
    - ClientSettings (ClientID: "DefaultClient", Screen: 1920x1080)

#### =====Objects===== (Organizer)
- Empty organizer object

### Key Features
- Minimal scene structure
- Handles initial setup and configuration
- Transitions to JoinScene after loading

---

## 2. JoinScene.unity

**File Path**: `Assets/Scenes/JoinScene.unity`

### Purpose
Network connection scene where players connect to Photon servers and set their nickname.

### Root GameObjects (Scene Hierarchy)
```
JoinScene
├── =====Cameras=====
├── =======UI=======
├── =====Managers=====
└── =====Objects=====
```

### Detailed Hierarchy

#### =====Cameras===== (Organizer)
- **Main Camera**
  - Components: Camera, AudioListener, UniversalAdditionalCameraData
  - Position: (0, 1, -10)

- **Directional Light**
  - Components: Light, UniversalAdditionalLightData
  - Rotation: (50, -30, 0)

#### =======UI======= (Organizer)
- **JoinCanvas**
  - Components:
    - RectTransform
    - Canvas (Render Mode: Screen Space - Overlay)
    - CanvasScaler (UI Scale Mode: Scale With Screen Size, Reference Resolution: 1920x1080)
    - GraphicRaycaster
  - Children:
    - **Background**
      - Component: Image (Background sprite)
      - Children:
        - **Horizontal**
          - Component: HorizontalLayoutGroup
          - Children:
            - **VerticalText** (VerticalLayoutGroup)
              - **JoinButton** (Button + Image + Text)
                - Text: "Join"
                - OnClick: JoinManager.OnJoinButtonClick

            - **VerticalInput** (VerticalLayoutGroup)
              - **IDInput** (TMP_InputField)
                - Placeholder: "Input ID"
                - TextComponent: Text
                - Children: Text Area > Text, Placeholder

              - **PWInput** (TMP_InputField - Password)
                - Placeholder: "Input Password"
                - InputType: Password (masked with asterisks)
                - Children: Text Area > Text, Placeholder

    - **StatusText** (TextMeshProUGUI)
      - Displays connection status messages
      - Position: Bottom of screen

- **EventSystem**
  - Components:
    - EventSystem
    - StandaloneInputModule

#### =====Managers===== (Organizer)
- **JoinManager**
  - Script: `JoinManager.cs` (guid: cf43c97ee9cda8a439225554796a419d)
  - References:
    - joinButton: JoinButton component
    - inputId: IDInput TMP_InputField
    - inputPassword: PWInput TMP_InputField
  - Purpose: Handles Photon connection and nickname setup

#### =====Objects===== (Organizer)
- Empty

### UI Input Fields
1. **ID Input** - Player nickname (standard text)
2. **Password Input** - Room password (masked asterisks, InputType: Password)

### Key Features
- Simple login interface
- Photon network connection setup
- Input validation for ID and password
- Status text for feedback

---

## 3. LobbyScene.unity

**File Path**: `Assets/Scenes/LobbyScene.unity`

### Purpose
Room creation/joining lobby where players can create rooms, browse available rooms, or use matchmaking.

### Root GameObjects (Scene Hierarchy)
```
LobbyScene
├── =====Cameras=====
├── =======UI=======
├── =====Managers=====
└── =====Objects=====
```

### Detailed Hierarchy

#### =====Cameras===== (Organizer)
- **Main Camera** (Camera + AudioListener + UniversalAdditionalCameraData)
- **Directional Light** (Light + UniversalAdditionalLightData)

#### =======UI======= (Organizer)
- **LobbyCanvas** (Canvas + CanvasScaler + GraphicRaycaster)
  - **Background** (Image + HorizontalLayoutGroup)
    - **VerticalGroup** (VerticalLayoutGroup)
      - **RoomNameHorizontalGroup** (HorizontalLayoutGroup)
        - **RoomName** (TextMeshProUGUI): Label "Room Name"
        - **RoomNameInput** (TMP_InputField): Text input for room name
          - Placeholder: "Input Room Name"

      - **RoomPwHorizontalGroup** (HorizontalLayoutGroup)
        - **RoomPw** (TextMeshProUGUI): Label "Room Password"
        - **RoomPwInput** (TMP_InputField + DropShadow effect): Password input
          - Placeholder: (password field)

      - **CreateRoomButton** (Button + Image)
        - Text: "Create Room"
        - OnClick: LobbyManager.OnClickCreate

      - **MatchRoomButton** (Button + Image)
        - Text: "Match Room"
        - OnClick: LobbyManager.OnClickMatch

      - **JoinRoomButton** (Button + Image)
        - Text: "Join Room"
        - OnClick: LobbyManager.OnClickJoin

      - **RefreshRoomListButton** (Button + Image)
        - Text: "Refresh Room List"
        - OnClick: LobbyManager.OnClickRefresh

      - **QuitButton** (Button + Image)
        - Text: "Quit"
        - OnClick: LobbyManager.OnClickQuit

    - **RoomList** (ScrollRect + LayoutGroup + Image + Mask)
      - Components:
        - ScrollRect (Vertical scroll)
        - GridLayoutGroup
        - Image (background)
      - Children:
        - **RoomListContent** (ContentSizeFitter + VerticalLayoutGroup)
          - Purpose: Container for dynamically instantiated room entries
        - **Scrollbar Vertical** (Scrollbar)
          - **Sliding Area**
            - **Handle** (Image)
        - **Scrollbar Horizontal** (Scrollbar - disabled by default)

    - **H** (TextMeshProUGUI): Header text
    - **W** (TextMeshProUGUI): Secondary text

- **EventSystem** (EventSystem + StandaloneInputModule)

#### =====Managers===== (Organizer)
- **LobbyManager**
  - Script: `LobbyManager.cs` (guid: ea4a0b733d605804d90875019b2f9b78)
  - Purpose: Manages room creation, joining, matchmaking, and room list display
  - Handles Photon room operations

#### =====Objects===== (Organizer)
- Empty

### Key Features
- Room creation with optional password
- Room browsing with scrollable list
- Quick matchmaking
- Room list refresh functionality
- Input validation for room name and password

---

## 4. GameScene.unity

**File Path**: `Assets/Scenes/GameScene.unity`

### Purpose
Main gameplay scene where the card battle takes place between two networked players.

### Root GameObjects (Scene Hierarchy)
```
GameScene
├── =====Cameras=====
├── =======UI=======
├── =====Managers=====
├── =====Objects=====
└── =====TEST===== (Test utilities)
```

### Detailed Hierarchy

#### =====Cameras===== (Organizer)
- **Main Camera**
  - Position: (0, 1, -10)
  - Components:
    - Camera (Perspective)
    - AudioListener
    - UniversalAdditionalCameraData
    - FixedAspectCamera (custom script for aspect ratio management)
    - CardDetector (custom script for card detection/selection)

- **UICamera**
  - Purpose: Dedicated camera for UI rendering
  - Components: Camera, UniversalAdditionalCameraData
  - Clear Flags: Depth only
  - Culling Mask: UI layer

- **Directional Light**
  - Rotation: (50, -30, 0)
  - Light intensity: 1
  - Color Temperature: 6570K

#### =======UI======= (Organizer)
- **InGameCanvas** (Main UI Canvas)
  - Components: Canvas (Screen Space - Overlay), CanvasScaler, GraphicRaycaster
  - Children:
    - **SafeArea** (Canvas + CanvasScaler for mobile safe area handling)
      - **Buttons**
        - **StartButton** (Button + Image)
          - Text: "Start"
          - OnClick: (Game start logic)
          - Sprite: Enabled/Disabled states

        - **EndButton** (Button + Image)
          - Text: "End Turn"
          - OnClick: (End turn logic)

        - **LeaveButton** (Button + Image)
          - Text: "Leave"
          - OnClick: (Leave game logic)

      - **TurnText** (TextMeshProUGUI)
        - Displays current turn number
        - Position: Bottom right
        - Font size: 36

      - **PlayerText** (TextMeshProUGUI)
        - Displays player's health/status

      - **OpponentText** (TextMeshProUGUI)
        - Displays opponent's health/status

      - **MyProfile** (Container)
        - **MyProfileImage** (Image): Player avatar

      - **YourProfile** (Container)
        - **YourProfileImage** (Image): Opponent avatar

      - **GlowOnOff** (Button - Test utility)
        - Purpose: Toggle card glow effects
        - OnClick: CardEffectTester.ToggleGlowAll

      - **GlowColorChange** (Button - Test utility)
        - Purpose: Change glow colors
        - OnClick: CardEffectTester.ToggleGlowColorAll

- **EventSystem** (EventSystem + StandaloneInputModule)

#### =====Managers===== (Organizer)
Critical game managers that control all game logic:

- **InGameManager**
  - Script: `InGameManager.cs` (guid: 024d3b8672d9f05489f17d3f00f491ba)
  - Purpose: Core game loop, process state management, card limits
  - Properties:
    - playerADeck, playerBDeck: Deck arrays
    - playerAHand, playerBHand: Hand arrays
    - isStart: Game start flag
    - Process state flags (isCopy, isDelete, isPlus, isMinus, isMultiple, isDivision)

- **UIManager**
  - Script: `InGameUIManager.cs` (guid: 234c8dcfbfa26474590ef6aa5755997e)
  - Purpose: UI updates, button states, win/lose displays
  - References:
    - startButton, endButton, leaveButton
    - turn, playerText, opponentText
    - Sprite references for enabled/disabled states
  - Animation settings: fadeInDuration, scaleUpDuration, pulseScale, pulseDuration

- **PhotonManager**
  - Script: `PhotonManager.cs` (guid: 8d0a5c2b3b351d340aef663db5c5c721)
  - Components:
    - PhotonManager (Photon callbacks)
    - PhotonHandler (TurnDuration: 20)
  - Purpose: Network event handling, room property synchronization

- **NetworkGameManager**
  - Script: `NetworkGameManager.cs` (guid: e45772f0ca7b74d4e824749f4e23de52)
  - Components:
    - NetworkGameManager
    - MonoBehaviourPunCallbacks (Photon PUN2)
    - PhotonView (for RPC)
  - Purpose: Central RPC hub for all network synchronization (card draw, placement, combat)

- **TurnManager**
  - Script: `TurnManager.cs` (guid: d548cbe3b25f0e14fbd2fb42bcb23748)
  - Components:
    - TurnManager
    - MonoBehaviourPunCallbacks
    - PhotonView
  - Purpose: Turn flow control, first player determination, turn numbering

- **DeckManager**
  - Script: `DeckManager.cs` (guid: 3647d682f2fce234ca432eb8221a462e)
  - Component: MonoBehaviourPunCallbacks
  - Purpose: Deck initialization and card distribution

- **HealthManager**
  - Script: `HealthManager.cs` (guid: 47ec7ff326bcf9a4aa5034c00b488d56)
  - Purpose: HP tracking for both players

- **FieldAttackManager**
  - Script: `FieldAttackManager.cs` (guid: 2d082705918421b42b044a4a3c8f3736)
  - Property: enableDebugLog (bool)
  - Purpose: Attack logic and combat resolution

- **OperatorManager**
  - Script: `OperatorManager.cs` (guid: 9027d1238241d864fb5896153c3c8631)
  - Purpose: Operator card usage (+, -, ×, ÷)

- **ResourcesManager**
  - Script: `ResourcesManager.cs` (guid: cf3239f435b0bc44fb063ecd1d7f9c6e)
  - Purpose: Card color management and sprite loading

#### =====Objects===== (Organizer)
Game objects for card zones and visual elements:

- **Zone** (Container for all card zones)
  - **PlayerHandZone**
    - Components:
      - Transform (Position: 0, -7.5, 0 / Rotation: -90, 0, 0)
      - ResponsiveObjectSafeArea (mobile safe area handling)
      - CardZone (ZoneType: Hand, OwnerType: Player)
      - CardLayoutHelper (fanRadius: 5, fanAngle: 60, spacing: 2)
    - Purpose: Player's hand card container

  - **PlayerFieldZone**
    - Components:
      - Transform (Position: 0, 0, 0)
      - CardZone (ZoneType: Field, OwnerType: Player)
      - CardLayoutHelper (spacing: 2, maxFieldCards: 5)
    - Purpose: Player's field card container

  - **OpponentHandZone**
    - Components:
      - Transform (Position: 0, 7.5, 0 / Rotation: 90, 0, 0)
      - ResponsiveObjectSafeArea
      - CardZone (ZoneType: Hand, OwnerType: Opponent)
      - CardLayoutHelper
    - Purpose: Opponent's hand card container

  - **OpponentFieldZone**
    - Components:
      - Transform (Position: 0, 3, 0)
      - ResponsiveObjectSafeArea
      - CardZone (ZoneType: Field, OwnerType: Opponent)
      - CardLayoutHelper
    - Purpose: Opponent's field card container

  - **ExpressionZone**
    - Components:
      - Transform (Position: -5, 0, 0)
      - CardZone
      - CardLayoutHelper
      - ExpressionZoneManager (displays math expressions during combat)
    - Purpose: Visual display of math expressions

- **MyDeck** (Player's deck visualization)
  - Components:
    - Transform (Position: 8, -4, 0 / Rotation: 70, 0, 0)
    - FillScreenBackground (responsive background)
  - Properties:
    - cardCount: 30
    - yOffset: 0.02
    - isMyDeck: true

- **YourDeck** (Opponent's deck visualization)
  - Components:
    - Transform (Position: -8, 4, 0 / Rotation: -70, 0, 0)
    - FillScreenBackground
  - Properties:
    - cardCount: 30
    - isMyDeck: false

- **Background** (Scene background)
  - Components:
    - Transform (Position: 0, 0, 10)
    - SpriteRenderer (background sprite)
    - KeepAspectBackground (aspect ratio management)

- **CardDetector** (Invisible game object for card selection)
  - Components:
    - Transform
    - CardDetector script
  - Purpose: Handles card hover and selection detection

- **JokerModeSelector** (Joker card selection UI)
  - Components:
    - Transform (inactive by default)
    - JokerModeSelector script
  - Children:
    - **Panel** (Background panel with ResponsiveCanvasFitSafeArea)
    - **BackGround** (Darkened overlay)
      - Components: SpriteRenderer, ObjectMouseEvent
    - **CancelButton** (World-space button)
      - Components: Transform, ObjectMouseEvent
      - Children:
        - **BackGround** (sprite)
        - **CancelButton** (sprite with text)
        - **Text (TMP)** (3D TextMeshPro: "Cancel")
    - **DrawCard**, **DeleteCard**, **SwapCard** (Joker ability buttons)
      - Each with: ObjectMouseEvent, sprites, 3D text
  - Purpose: UI for selecting Joker card effects (Draw/Delete/Swap)

- **CardModeSelector** (Card mode selection UI for operators)
  - Components:
    - Transform (inactive by default)
    - CardModeSelector script
    - Similar structure to JokerModeSelector
  - Purpose: UI for selecting operator card actions

- **MaterialController** (Organizer for test materials)
  - **Test** (Container)
    - **Smooth**, **Metallic**, **SmoothText**, **MetallicText** (Test shader materials)

#### =====TEST===== (Test Utilities - Organizer)
- **CardEffectTester** (Test object)
  - Script: CardEffectTester.cs
  - Purpose: Debug tools for testing card effects and glow
- Test buttons and debug tools (typically disabled in production)

### Card Zone System
All card zones use consistent components:
- **CardZone**: Manages cards in that zone (Hand/Field, Player/Opponent)
- **CardLayoutHelper**: Auto-arranges cards with spacing and animations
  - Hand zones use fan layout (fanRadius, fanAngle)
  - Field zones use linear layout (spacing, maxFieldCards)
- **ResponsiveObjectSafeArea**: Adapts to mobile device safe areas

### UI Elements - Health Bars
- **HealthBar** (Player)
  - Components: Image (fill type), responsive positioning
  - Parent: SafeArea > PlayerProfile area

- **HealthBar** (Opponent)
  - Components: Image (fill type), responsive positioning
  - Parent: SafeArea > OpponentProfile area

### Network Architecture in GameScene
- **PhotonView** components on:
  - NetworkGameManager
  - TurnManager
- **MonoBehaviourPunCallbacks** on multiple managers for Photon callbacks
- All network synchronization routes through NetworkGameManager RPC system

### Key Prefab Structures
While prefabs aren't directly in the scene file, the scene references these sprite groups:
- **CalculationCard_01** through **CalculationCard_05** (Number cards with different colors)
- **SecretCard** (Back card sprite)
- **OpenCard** sprite assets
- Joker ability sprites (Draw, Delete, Swap)

---

## Common Patterns Across All Scenes

### Scene Organization
All scenes use organizational GameObject separators:
- `=====Cameras=====`: Camera and lighting
- `=======UI=======`: Canvas and UI elements
- `=====Managers=====`: Singleton manager scripts
- `=====Objects=====`: Game objects and visual elements
- `=====TEST=====`: Debug/test utilities (GameScene only)

### Camera Setup
Every scene has:
- Main Camera (Perspective, FOV 60, position 0,1,-10)
- Directional Light (50,-30,0 rotation)
- UniversalAdditionalCameraData (URP rendering)

### UI Structure
All UI uses:
- Canvas with CanvasScaler (1920x1080 reference resolution)
- GraphicRaycaster for interaction
- EventSystem with StandaloneInputModule
- TextMeshPro (TMP) for all text rendering

### Manager Pattern
All managers follow Singleton pattern:
- Single instance per scene
- Organized under `=====Managers=====` parent
- Use `FindAnyObjectByType<>` for cross-manager communication

---

## Component Type Reference

### Unity Built-in Components
- **Transform / RectTransform**: Position, rotation, scale, hierarchy
- **Camera**: Rendering camera
- **Light**: Scene lighting
- **AudioListener**: Audio input
- **Canvas**: UI rendering root
- **CanvasScaler**: UI scaling
- **GraphicRaycaster**: UI input detection
- **Image**: UI sprite rendering
- **Button**: UI button interaction
- **ScrollRect**: Scrollable area
- **GridLayoutGroup / VerticalLayoutGroup / HorizontalLayoutGroup**: Auto-layout
- **ContentSizeFitter**: Dynamic size adjustment
- **Mask**: UI masking
- **Scrollbar**: Scrollbar UI element
- **EventSystem / StandaloneInputModule**: Input system

### TextMeshPro Components
- **TextMeshProUGUI**: Canvas-space text (UI)
- **TextMeshPro (3D)**: World-space 3D text
- **TMP_InputField**: Text input field

### Universal Render Pipeline (URP)
- **UniversalAdditionalCameraData**: URP camera settings
- **UniversalAdditionalLightData**: URP light settings
- **Volume**: Post-processing volume

### Photon PUN2 Components
- **PhotonView**: Network view for RPC
- **MonoBehaviourPunCallbacks**: Photon callback receiver
- **PhotonHandler**: Photon turn management

### Custom Scripts (Game-Specific)
- **Manager Scripts**: `*Manager.cs` (SplashManager, JoinManager, LobbyManager, InGameManager, etc.)
- **Card System**: CardZone, CardLayoutHelper, Card, NetworkCard, CardEffect, CardText
- **Responsive UI**: ResponsiveObjectSafeArea, FillScreenBackground, KeepAspectBackground, FixedAspectCamera
- **Interaction**: ObjectMouseEvent, CardDetector
- **Game Logic**: FieldAttackManager, OperatorManager, HealthManager, TurnManager, DeckManager
- **UI Managers**: JokerModeSelector, CardModeSelector, ExpressionZoneManager
- **Test Utilities**: CardEffectTester

---

## Script GUID to Filename Mapping

### Confirmed Manager Scripts
- `108087141d3984c4eb2af390af089344` → `SplashManager.cs`
- `cf43c97ee9cda8a439225554796a419d` → `JoinManager.cs`
- `ea4a0b733d605804d90875019b2f9b78` → `LobbyManager.cs`
- `024d3b8672d9f05489f17d3f00f491ba` → `InGameManager.cs`
- `234c8dcfbfa26474590ef6aa5755997e` → `InGameUIManager.cs`
- `8d0a5c2b3b351d340aef663db5c5c721` → `PhotonManager.cs`
- `e45772f0ca7b74d4e824749f4e23de52` → `NetworkGameManager.cs`
- `d548cbe3b25f0e14fbd2fb42bcb23748` → `TurnManager.cs`
- `3647d682f2fce234ca432eb8221a462e` → `DeckManager.cs`
- `47ec7ff326bcf9a4aa5034c00b488d56` → `HealthManager.cs`
- `2d082705918421b42b044a4a3c8f3736` → `FieldAttackManager.cs`
- `9027d1238241d864fb5896153c3c8631` → `OperatorManager.cs`
- `cf3239f435b0bc44fb063ecd1d7f9c6e` → `ResourcesManager.cs`

### Card System Scripts
- `68c3b2328bbc0244184c1c9753ad5d03` → `CardZone.cs`
- `a6c49ccbc755fd94d9e9c909ebf800bf` → `ResponsiveObjectSafeArea.cs`
- `c0c65306f7a2c0b4cb921093208a8000` → `CardLayoutHelper.cs`
- `bec8619e2adda6a438bef759de30a417` → `ExpressionZoneManager.cs`

### UI & Interaction Scripts
- `c2b6d4505ce802447a9f381cb53f2108` → `ObjectMouseEvent.cs`
- `dfa3178c179b0024b8ebb4f184bdadf8` → `CardEffectTester.cs`
- `c7e4df7ae0060964b95ca30862410e21` → `JokerModeSelector.cs`
- `2d0fce70b01ef284d97b68540cd15096` → `CardModeSelector.cs`

### Camera & Background Scripts
- `ebf0631623e5f3c4585bc1c9753ad5d0` → `FixedAspectCamera.cs`
- `bf39a4c5ea3363e46b8a2420b12de136` → `CardDetector.cs`
- `048c13dc94a1a854597ce1d0af28de5a` → `FillScreenBackground.cs`
- `7d0617ec8f5454b4e82447f27b4b34f0` → `KeepAspectBackground.cs`

---

## Scene Flow Diagram

```
Application Start
       ↓
  SplashScene
  - Initialize game settings
  - Load resources
       ↓
  JoinScene
  - Connect to Photon
  - Set player nickname
       ↓
  LobbyScene
  - Create/Join/Match room
  - Browse available rooms
       ↓
  GameScene
  - Card battle gameplay
  - Turn-based multiplayer
       ↓
  (Back to LobbyScene on game end)
```

---

## Notes
- All scenes use URP (Universal Render Pipeline)
- Target resolution: 1920x1080
- Mobile support with safe area handling
- Photon PUN2 for networking
- DOTween for animations (referenced in CLAUDE.md)
- Material Property Blocks for efficient rendering
- Singleton pattern for all managers
- Consistent scene organization with labeled separators

This documentation accurately reflects the current state of the Unity project as of the latest commit.
