# LDtk Celeste Room System - Setup Guide

## Overview
This system provides Celeste-like room-based level design with automatic camera transitions between rooms using LDtk (Level Designer Toolkit).

## File Structure Created

```
Assets/Script/
├── Room/
│   └── RoomManager.cs           # Manages room transitions
├── LDtk/
│   ├── LDtkEntitySpawner.cs     # Maps LDtk entities to Unity prefabs
│   └── Entities/
│       ├── PlayerSpawn.cs       # Player spawn point marker
│       ├── Spike.cs             # Deadly spike hazard
│       ├── Spring.cs            # Bouncer/launcher
│       ├── Strawberry.cs        # Collectible with follow behavior
│       ├── LDtkCheckpoint.cs    # Checkpoint/respawn flag
│       ├── MovingPlatform.cs    # Platform following path
│       ├── DashCrystal.cs       # Dash refill crystal
│       ├── CrumbleBlock.cs      # Crumbling platform
│       └── RoomTransition.cs    # Manual room transition trigger
├── Camera/
│   └── CameraController.cs      # Updated with RoomBounds mode
Assets/Levels/
└── CelesteProject.ldtk          # LDtk project template
```

## Setup Instructions

### 1. Install LDtk Editor
1. Download LDtk from https://ldtk.io
2. Open the `Assets/Levels/CelesteProject.ldtk` file in LDtk
3. This template includes pre-configured:
   - **Layers**: FG_Tiles, Entities, Collision, BG_Tiles
   - **Entities**: PlayerSpawn, Spike, Spring, Strawberry, Checkpoint, DashCrystal, MovingPlatform, CrumbleBlock
   - **GridVania Layout**: Rooms are placed on a grid and auto-detect neighbors

### 2. Add Your Tileset to LDtk
1. In LDtk, go to **Project Settings > Tilesets**
2. Click **+ Add Tileset**
3. Navigate to your tileset image (e.g., `Assets/Sprites/Tilesets/PC _ Computer - Celeste - Tilesets - Foreground Tilesets.png`)
4. Set grid size to **8x8** pixels
5. Assign tileset to **Collision**, **FG_Tiles**, and **BG_Tiles** layers

### 3. Configure LDtk Unity Importer
1. In Unity, select the `.ldtk` file in Project window
2. In Inspector, configure import settings:
   - **Pixels Per Unit**: 8 (or your tileset's PPU)
   - **Main Tileset**: Assign your tileset
3. Create prefabs for each entity and link them:
   - Spike → Spike prefab (with Spike.cs)
   - Spring → Spring prefab (with Spring.cs)
   - etc.

### 4. Scene Setup
1. Create a new scene for your level
2. Drag the imported LDtk asset into the scene
3. Add a **RoomManager** object with the RoomManager script
4. Configure the Camera:
   - Select Main Camera
   - Set **Camera Movement Style** to `RoomBounds`
   - Adjust transition settings as desired

### 5. Configure Camera Controller
The CameraController now supports a new `RoomBounds` mode:

```
Camera Movement Style: RoomBounds
Room Transition Time: 0.3 (seconds)
Smooth Follow In Room: ✓
Smooth Follow Speed: 8
```

## Room Design in LDtk

### Creating Rooms
1. In LDtk, each **Level** = one Room
2. Use **GridVania** world layout (default in template)
3. Rooms automatically detect neighbors based on position
4. Standard room size: **320x180** pixels (Celeste screen ratio)

### Designing a Room
1. **Collision Layer** (IntGrid):
   - Paint solid ground with value `1` (Solid)
   - Use value `2` for jump-through platforms
   - Use value `3` for climbable walls
   
2. **Entities Layer**:
   - Place one `PlayerSpawn` per room (for respawning)
   - Add `Checkpoint` entities for save points
   - Place `Spike`, `Spring`, `DashCrystal` as needed
   
3. **Tiles Layers**:
   - `BG_Tiles` for background decoration
   - `FG_Tiles` for foreground (renders above entities)

### Entity Properties

| Entity | Fields | Description |
|--------|--------|-------------|
| PlayerSpawn | IsDefault, Direction | Where player spawns |
| Spike | Direction (Up/Down/Left/Right) | Kills on contact |
| Spring | Direction, Force, ResetDash | Bounces player |
| Strawberry | ID, Golden, Winged | Collectible |
| Checkpoint | ID, IsRoomSpawn | Respawn point |
| DashCrystal | DoubleDash, RespawnTime, OneTime | Refills dash |
| MovingPlatform | Path, Speed, Loop, PingPong | Moving platform |
| CrumbleBlock | CrumbleDelay, RespawnTime, Respawns | Crumbling block |

## Room Transitions

### Automatic Transitions
The `RoomManager` automatically handles transitions when the player walks past room boundaries. Adjacent rooms are detected via LDtk's neighbor system.

### Manual Transitions
Use `RoomTransition` entities for:
- Doors that require interaction
- One-way passages
- Teleportation points

### Transition Styles
Configure in RoomManager:
- **InstantSnap**: Classic Celeste - immediate camera snap
- **SmoothPan**: Camera smoothly pans to new room
- **FadeTransition**: Fade to black (customizable)

## Creating Entity Prefabs

### Example: Spike Prefab
1. Create new GameObject
2. Add components:
   - `SpriteRenderer` (assign spike sprite)
   - `BoxCollider2D` (set as Trigger)
   - `Spike.cs` script
3. Save as prefab in `Assets/Prefabs/Entities/`
4. Link in LDtk importer settings

### Example: Spring Prefab
1. Create new GameObject
2. Add components:
   - `SpriteRenderer`
   - `BoxCollider2D` (Trigger)
   - `Animator` (for bounce animation)
   - `Spring.cs` script
3. Configure default Force (15) and ResetDash (true)

## Code Integration

### Spawning Player in Room
```csharp
// Find spawn point in current room
PlayerSpawn spawn = FindObjectOfType<PlayerSpawn>();
if (spawn != null && spawn.IsDefaultSpawn)
{
    player.transform.position = spawn.SpawnPosition;
}
```

### Listening to Room Changes
```csharp
RoomManager.Instance.OnRoomChanged += (oldRoom, newRoom) => {
    Debug.Log($"Moved from {oldRoom?.name} to {newRoom.name}");
    // Reset respawning entities, etc.
};
```

### Manual Room Transition
```csharp
LDtkComponentLevel targetRoom = RoomManager.Instance.FindRoomAtPosition(somePosition);
RoomManager.Instance.TransitionToRoom(targetRoom);
```

## Tips

1. **Room Size**: Keep rooms 320x180 for authentic Celeste feel
2. **Checkpoints**: Place one at the start of each room
3. **Testing**: Use Scene view Gizmos to see room bounds and entity directions
4. **Performance**: Single scene with all rooms is most efficient
5. **Save Data**: Strawberry IDs should be unique across the entire chapter

## Troubleshooting

**Camera not following room bounds:**
- Ensure CameraController style is set to `RoomBounds`
- Check that RoomManager found the LDtk project

**Entities not spawning:**
- Verify prefab links in LDtk importer
- Check that entity scripts implement `ILDtkImportedFields`

**Room transitions not working:**
- Verify rooms are adjacent in LDtk (touching edges)
- Check player has "Player" tag
