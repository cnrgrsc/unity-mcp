# Block Blast - 2D Puzzle Game

A fully featured 2D Block Blast puzzle game built with Unity MCP (DevBridge).

## 🎮 Game Overview

Block Blast is a classic puzzle game where players:
- Drag and drop block shapes onto an 8x8 grid
- Clear complete rows and columns to score points
- Build combos for bonus points
- Keep playing until no valid moves remain

## 📁 Project Structure

```
BlockBlast/
├── Scripts/
│   ├── BlockPiece.cs        # Individual block with color and destruction
│   ├── BlockShape.cs        # Draggable shape with placement logic
│   ├── GridManager.cs       # 8x8 grid, line clearing, placement
│   ├── ShapeSpawner.cs      # Shape patterns, random spawning
│   ├── GameManager.cs       # Scoring, combos, game state
│   ├── UIManager.cs         # All UI panels and interactions
│   ├── AudioManager.cs      # Sound effects (procedural if no assets)
│   ├── BlockBlast.asmdef    # Assembly definition
│   └── Editor/
│       ├── BlockBlastSceneSetup.cs  # Auto scene creation
│       └── BlockBlast.Editor.asmdef
└── Scenes/
    └── BlockBlastGame.unity  # Main game scene (created via menu)
```

## 🚀 Quick Start

### Setup Scene
1. Open Unity project
2. Go to menu: **BlockBlast → Setup Scene**
3. This automatically creates:
   - Camera configured for 2D
   - Grid Manager (8x8)
   - Shape Spawner
   - Game Manager
   - Audio Manager
   - Complete UI (Canvas, panels, buttons)

### Play
1. Click **Play** in Unity Editor
2. Click **PLAY** button in main menu
3. Drag block shapes from bottom to the grid
4. Clear rows and columns to score!

## 🎯 Features

### Core Mechanics
- **8x8 Grid System** - Configurable size
- **16 Shape Patterns** - Including L, T, S, Z, squares
- **5 Colors** - Red, Blue, Green, Yellow, Purple
- **Row/Column Clearing** - Automatic detection
- **Combo System** - Bonus points for consecutive clears

### UI System
- Main Menu (Play, Settings, Best Score)
- Game HUD (Score, Best, Combo, Pause)
- Pause Panel (Resume, Restart, Menu)
- Game Over Panel (Final Score, Best, Play Again)
- Settings Panel (Music/SFX sliders)

### Audio
- Procedural sound generation (works without audio files)
- Click, Place, Clear, Combo, Game Over sounds
- Volume controls with persistence

### Scoring
- 10 points per block cleared
- 100 points per line (row/column)
- +50 bonus per combo level

## ⚙️ Configuration

Edit `GridManager` component:
- `Grid Width/Height` - Default 8x8
- `Cell Size` - Visual size of grid cells
- `Empty Cell Color` - Background color

Edit `ShapeSpawner` component:
- `Shapes Per Batch` - Default 3 shapes at once

Edit `GameManager` component:
- `Points Per Block` - Scoring values
- `Points Per Line`
- `Combo Multiplier Bonus`

## 🔧 Built with Unity MCP

This game was developed using DevBridge tools:
- `manage_script` - C# code generation
- `manage_2d` - Sprites and 2D physics
- `manage_ui` - UI elements
- `manage_audio` - Sound system
- `manage_prefabs` - Prefab workflow

## 📱 Mobile Build

Ready for Android:
1. File → Build Settings
2. Switch Platform to Android
3. Player Settings → Package Name
4. Build and Run

## 📄 License

MIT License - Free for any use.
