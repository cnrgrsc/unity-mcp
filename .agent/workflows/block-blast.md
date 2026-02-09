---
description: How to create and develop Block Blast game using Unity MCP
---

# Block Blast Development Workflow

This workflow describes how to use Unity MCP (DevBridge) to develop and extend the Block Blast puzzle game.

## Prerequisites
- Unity project with DevBridge package installed
- Python 3.10+ with uv package manager
- DevBridge server running (`Window → DevBridge → Start Server`)

// turbo-all

## Initial Setup

1. Start DevBridge Server in Unity:
   ```
   Window → DevBridge → Start Server
   ```

2. Create Block Blast Scene:
   ```
   Menu: BlockBlast → Setup Scene
   ```
   This creates the complete game scene with:
   - 8x8 grid
   - Shape spawner
   - Game manager
   - Audio manager
   - Full UI canvas

## Using MCP Tools for Development

### Create New Scripts
```
Use manage_script tool:
- action: create
- path: Assets/BlockBlast/Scripts/NewScript.cs
- contents: [C# code]
```

### Add 2D Sprites
```
Use manage_2d tool:
- action: sprite_create
- texturePath: Assets/BlockBlast/Textures/block.png
```

### Create UI Elements
```
Use manage_ui tool:
- action: canvas_create (for new canvas)
- action: button_add (for buttons)
- action: text_add (for text elements)
```

### Add Audio
```
Use manage_audio tool:
- action: source_add (add audio source)
- action: clip_play (play sounds)
```

### Create Prefabs
```
Use manage_prefabs tool:
- action: create (from existing GameObject)
- action: instantiate (spawn from prefab)
```

## Testing Workflow

1. Enter Play Mode in Unity
2. Click PLAY button
3. Test drag-drop mechanics
4. Verify line clearing works
5. Check score and combo system
6. Test game over condition

## Building for Android

```
Use manage_build tool:
1. action: set_platform, platform: Android
2. action: player_settings (set package name, version)
3. action: add_scene (add BlockBlastGame scene)
4. action: execute (build APK)
```

## Common Extensions

### Add New Shape Patterns
Edit `ShapeSpawner.cs` → `ShapePatterns` list with new Vector2Int arrays

### Add New Block Colors
Edit `BlockPiece.cs` → `BlockColor` enum and `GetColorValue()` method

### Change Grid Size
Modify `GridManager` component → `gridWidth` and `gridHeight` fields

### Add Power-ups
1. Create PowerUp.cs script
2. Add power-up logic to GameManager
3. Update UI with power-up buttons
