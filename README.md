<img width="676" height="380" alt="DevBridge" src="docs/images/devbridge-logo.png" />

<div align="center">

# 🌉 DevBridge

### AI-Powered Unity Development Toolkit

**Connect your AI assistant directly to Unity Editor for next-level game development**

[![GitHub](https://img.shields.io/badge/GitHub-cnrgrsc-181717?style=for-the-badge&logo=github)](https://github.com/cnrgrsc/unity-mcp)
[![Unity](https://img.shields.io/badge/Unity-2021.3+-000000?style=for-the-badge&logo=unity)](https://unity.com/releases/editor/archive)
[![Python](https://img.shields.io/badge/Python-3.10+-3776AB?style=for-the-badge&logo=python&logoColor=white)](https://www.python.org)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![MCP](https://img.shields.io/badge/MCP-Enabled-8B5CF6?style=for-the-badge)](https://modelcontextprotocol.io)

[🚀 Quick Start](#-quick-start) • [✨ Features](#-features) • [📖 Tools Reference](#-tools-reference) • [🤝 Contributing](#-contributing)

</div>

---

## 🎯 What is DevBridge?

**DevBridge** is an advanced AI bridge that connects your favorite AI assistants (Claude, Cursor, VS Code, Gemini, etc.) to Unity Editor via the [Model Context Protocol](https://modelcontextprotocol.io). It provides **28+ specialized tools** for complete game development automation.

> 💡 **Based on [MCP for Unity](https://github.com/CoplayDev/unity-mcp) by CoplayDev** — enhanced with game development superpowers!

---

## 🚀 Quick Start

### Prerequisites

| Requirement | Link |
|-------------|------|
| **Unity 2021.3 LTS+** | [Download](https://unity.com/download) |
| **Python 3.10+** | [Download](https://www.python.org/downloads/) |
| **uv** (Python package manager) | [Install](https://docs.astral.sh/uv/getting-started/installation/) |
| **MCP Client** | Claude Desktop / Cursor / VS Code / Windsurf |

### 1️⃣ Install Unity Package

Open Unity → `Window > Package Manager > + > Add package from git URL...`

```
https://github.com/cnrgrsc/unity-mcp.git?path=/MCPForUnity#beta
```

### 2️⃣ Start the Server

1. In Unity: `Window > DevBridge`
2. Click **Start Server** 
3. Select your MCP Client and click **Configure**
4. Look for 🟢 **Connected ✓**

### 3️⃣ Start Building!

Try these prompts with your AI:
- *"Create a player with physics-based movement"*
- *"Add a navigation system with AI pathfinding"*
- *"Build a complete UI menu with animations"*
- *"Create an audio manager with 3D spatial sound"*

---

## ✨ Features

DevBridge includes **28+ specialized tools** for complete Unity game development:

### 🎮 Core Game Development

| Tool | Description |
|------|-------------|
| `manage_gameobject` | Create, modify, delete GameObjects |
| `manage_scene` | Scene management and hierarchy |
| `manage_prefabs` | Prefab creation and variants |
| `manage_components` | Add/remove/configure components |

### ⚡ Physics & Navigation

| Tool | Description |
|------|-------------|
| `manage_physics` | Rigidbody, Colliders, Joints, Raycasting |
| `manage_navigation` | NavMesh, AI Agents, Pathfinding |
| `manage_2d` | Sprites, Tilemaps, 2D Physics |

### 🎨 Graphics & Visual

| Tool | Description |
|------|-------------|
| `manage_material` | Materials and shaders |
| `manage_texture` | Texture import and settings |
| `manage_lighting` | Lights, Ambient, Fog, Bake |
| `manage_terrain` | Terrain creation and painting |
| `manage_vfx` | Particle systems and effects |
| `manage_shader` | Shader creation and modification |

### 🎬 Animation & Camera

| Tool | Description |
|------|-------------|
| `manage_animation` | Animators, Clips, State Machines |
| `manage_cinemachine` | Virtual Cameras, Dolly, Follow ⚠️ |
| `manage_timeline` | Cinematic sequences ⚠️ |

### 🔊 Audio & Input

| Tool | Description |
|------|-------------|
| `manage_audio` | Audio Sources, Mixers, 3D Sound |
| `manage_input` | Input System, Action Maps |

### 🖥️ UI & Scripting

| Tool | Description |
|------|-------------|
| `manage_ui` | Canvas, UI Elements, Layouts |
| `manage_script` | C# script generation |
| `manage_scriptable_object` | ScriptableObject creation |

### 🔨 Pro Tools

| Tool | Description |
|------|-------------|
| `manage_probuilder` | 3D mesh modeling ⚠️ |
| `manage_build` | Build settings, platforms, deployment |
| `manage_localization` | Multi-language support ⚠️ |

### 🛠️ Editor & Utilities

| Tool | Description |
|------|-------------|
| `manage_editor` | Editor preferences and windows |
| `manage_asset` | Asset import and management |
| `read_console` | Read Unity console logs |
| `refresh_unity` | Force asset refresh |
| `run_tests` | Run unit tests |
| `batch_execute` | Execute multiple commands |

> ⚠️ = Requires optional Unity package (see [Optional Packages](#optional-packages))

---

## 📖 Tools Reference

### Example Commands

```bash
# Physics
"Add Rigidbody to Player with mass 2 and gravity enabled"
"Create a BoxCollider on Enemy with trigger enabled"

# Navigation
"Bake NavMesh for the scene"
"Add NavMeshAgent to Enemy with speed 5"

# 2D
"Create a sprite from Assets/Sprites/hero.png"
"Add Rigidbody2D to Player with gravity scale 2"
"Create a Tilemap named Ground"

# Lighting
"Create Point light at position (5, 3, 0) with blue color"
"Enable fog with linear mode, distance 10-100"

# Terrain
"Create terrain 1000x1000 with height 600"
"Add grass texture to terrain"

# Animation
"Create Animator Controller named PlayerController"
"Add animation clip Walk to Player"

# UI
"Create Canvas with UI Scale Mode"
"Add Button named StartButton with text 'Start Game'"

# Camera
"Create virtual camera following Player"
"Set camera FOV to 60"

# Audio
"Add AudioSource to Player with 3D spatial blend"
"Create AudioMixer named GameMixer"

# Build
"Switch platform to Android"
"Add scene to build: Assets/Scenes/Level1.unity"
"Set product name to MyGame, version 1.0.0"
```

---

## ⚡ Optional Packages

Some tools require additional Unity packages. Add these scripting defines in Project Settings > Player:

| Package | Scripting Define | Install Command |
|---------|------------------|-----------------|
| **ProBuilder** | `PROBUILDER_ENABLED` | `com.unity.probuilder` |
| **Cinemachine** | `CINEMACHINE_ENABLED` | `com.unity.cinemachine` |
| **Timeline** | `TIMELINE_ENABLED` | `com.unity.timeline` |
| **Localization** | `LOCALIZATION_ENABLED` | `com.unity.localization` |

---

## 🏗️ Architecture

```
┌─────────────────┐     MCP Protocol     ┌─────────────────┐
│   AI Assistant  │◄───────────────────►│  Python Server  │
│ (Claude, etc.)  │                      │   (FastMCP)     │
└─────────────────┘                      └────────┬────────┘
                                                  │ HTTP
                                                  ▼
                                         ┌─────────────────┐
                                         │  Unity Editor   │
                                         │  (C# Handlers)  │
                                         └─────────────────┘
```

---

## 🤝 Contributing

Contributions are welcome! Feel free to:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

**Made with ❤️ for Unity Developers**

[⭐ Star this repo](https://github.com/cnrgrsc/unity-mcp) if you find it useful!

</div>
