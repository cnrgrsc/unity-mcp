<div align="center">

<img width="676" height="380" alt="DevBridge" src="docs/images/devbridge-logo.png" />

# 🌉 DevBridge

### The Most Powerful AI-to-Unity Bridge

**Transform your Unity development with AI. Create complete games, systems, and features in minutes.**

[![GitHub](https://img.shields.io/badge/GitHub-cnrgrsc-181717?style=for-the-badge&logo=github)](https://github.com/cnrgrsc/unity-mcp)
[![Unity](https://img.shields.io/badge/Unity-2021.3+-000000?style=for-the-badge&logo=unity)](https://unity.com/releases/editor/archive)
[![Python](https://img.shields.io/badge/Python-3.10+-3776AB?style=for-the-badge&logo=python&logoColor=white)](https://www.python.org)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![Tools](https://img.shields.io/badge/Tools-35+-FF6B6B?style=for-the-badge)](https://github.com/cnrgrsc/unity-mcp)

[🚀 Installation](#-installation) • [⚡ Quick Start](#-quick-start) • [🛠️ All Tools](#%EF%B8%8F-complete-tool-reference) • [💡 Examples](#-usage-examples)

</div>

---

## 🎯 Why DevBridge?

DevBridge isn't just another Unity plugin. It's **the bridge between AI and game development** — giving AI assistants like Claude, Cursor, and VS Code Copilot **direct control** over Unity Editor.

### What Can AI Do With DevBridge?

| Without DevBridge | With DevBridge |
|-------------------|----------------|
| AI writes code, you copy-paste | AI **directly creates** GameObjects, scripts, prefabs |
| Manual scene setup | AI **builds entire scenes** with physics, lighting, navigation |
| Hours of UI creation | AI **generates complete UI** with Canvas, buttons, layouts |
| Tedious asset configuration | AI **handles materials, textures, audio** automatically |
| Platform builds manually | AI **switches platforms, configures builds, exports** |

> 💡 **Based on [MCP for Unity](https://github.com/CoplayDev/unity-mcp)** — Extended with **35+ specialized tools** for complete game development.

---

## 🚀 Installation

### Prerequisites

Before installing DevBridge, ensure you have:

| Requirement | Version | Installation |
|-------------|---------|--------------|
| **Unity** | 2021.3 LTS or newer | [Download Unity Hub](https://unity.com/download) |
| **Python** | 3.10+ | [Download Python](https://www.python.org/downloads/) |
| **uv** | Latest | See below |
| **MCP Client** | Any | Claude Desktop / Cursor / VS Code |

#### Installing uv (Python Package Manager)

**Windows (PowerShell):**
```powershell
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
```

**macOS/Linux:**
```bash
curl -LsSf https://astral.sh/uv/install.sh | sh
```

---

### Step 1: Install Unity Package

Open your Unity project, then:

1. Go to **Window → Package Manager**
2. Click the **+** button (top-left)
3. Select **"Add package from git URL..."**
4. Paste this URL:

```
https://github.com/cnrgrsc/unity-mcp.git?path=/MCPForUnity#beta
```

5. Click **Add** and wait for installation

> ✅ The package will appear as "DevBridge" in your Package Manager

---

### Step 2: Configure Your AI Client

#### For Claude Desktop

1. In Unity: **Window → DevBridge**
2. Click **"Start Server"**
3. Select **"Claude Desktop"** from the dropdown
4. Click **"Configure"** — this automatically updates your Claude config
5. **Restart Claude Desktop**
6. Look for 🟢 **Connected ✓** in Unity

#### For Cursor / VS Code

1. In Unity: **Window → DevBridge**
2. Click **"Start Server"**
3. Select your editor from the dropdown
4. Click **"Copy Config"** to get the MCP configuration
5. Add to your editor's MCP settings
6. Restart your editor

#### Manual Configuration

If automatic setup doesn't work, add this to your MCP config:

```json
{
  "mcpServers": {
    "devbridge": {
      "command": "uvx",
      "args": ["devbridge"]
    }
  }
}
```

**Config file locations:**
- **Claude Desktop (Windows):** `%APPDATA%\Claude\claude_desktop_config.json`
- **Claude Desktop (macOS):** `~/Library/Application Support/Claude/claude_desktop_config.json`
- **Cursor:** `.cursor/mcp.json` in your project
- **VS Code:** Check MCP extension settings

---

### Step 3: Verify Connection

1. Open your AI assistant
2. You should see DevBridge tools available
3. Try: *"List all GameObjects in the scene"*
4. If it works, you're connected! 🎉

---

## ⚡ Quick Start

Once connected, try these commands with your AI:

```
"Create a Player GameObject with Rigidbody and CapsuleCollider"
"Add a NavMeshAgent to Enemy and set speed to 5"
"Create a Canvas with a Start Button and Quit Button"
"Set up point lights around the scene for atmospheric lighting"
"Build the game for Windows"
```

---

## 🛠️ Complete Tool Reference

DevBridge provides **35+ specialized tools** organized by category:

### 🎮 Core GameObjects

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_gameobject` | create, delete, duplicate, find, modify | Full GameObject lifecycle |
| `manage_scene` | open, save, create, list, get_hierarchy | Scene management |
| `manage_prefabs` | create, instantiate, apply, unpack | Prefab workflow |
| `manage_components` | add, remove, get, set, list | Component manipulation |
| `find_gameobjects` | by_name, by_tag, by_layer, by_component | Advanced searching |

### ⚡ Physics System

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_physics` | rigidbody_add, collider_add, joint_add, raycast | Complete 3D physics |
| `manage_2d` | rigidbody2d_add, collider2d_add, tilemap_create | 2D game physics |

### 🗺️ Navigation & AI

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_navigation` | bake_navmesh, agent_add, obstacle_add, set_destination | NavMesh & pathfinding |

### 🎨 Graphics & Rendering

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_material` | create, assign, set_property, set_shader | Material control |
| `manage_texture` | import, configure, set_settings | Texture management |
| `manage_lighting` | light_create, ambient_set, fog_set, bake | Lighting system |
| `manage_terrain` | create, sculpt, paint_texture, add_trees | Terrain tools |
| `manage_shader` | create, modify, get_properties | Shader access |
| `manage_vfx` | create_particle, configure, play, stop | Particle effects |

### 🎬 Animation & Cinematics

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_animation` | create_controller, add_clip, set_parameter | Animation system |
| `manage_cinemachine` | vcam_create, set_follow, set_lookat ⚠️ | Virtual cameras |
| `manage_timeline` | create, add_track, playback ⚠️ | Cutscenes & sequences |

### 🔊 Audio System

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_audio` | source_add, clip_play, mixer_create, 3d_configure | Complete audio |

### 🎮 Input System

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_input` | action_map_create, binding_add, scheme_create | New Input System |

### 🖥️ UI System

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_ui` | canvas_create, button_add, text_add, layout | Complete UI toolkit |

### 📝 Scripting

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_script` | create, modify, add_method, add_field | C# code generation |
| `manage_scriptable_object` | create, set_field, list | ScriptableObject support |

### 🔨 Pro Tools

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_probuilder` | create_shape, extrude, bevel ⚠️ | 3D modeling |
| `manage_build` | set_platform, add_scene, execute, android_set_settings, ios_set_settings | Build & deploy |
| `manage_localization` | locale_add, table_create, entry_add ⚠️ | Multi-language |

### 💰 Monetization & Analytics

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_ads` | ads_initialize, show_banner, show_rewarded, iap_add_product | Unity Ads, AdMob, IAP |
| `manage_analytics` | send_event, set_user_property, firebase_log_event | Analytics tracking |

### 🌐 Multiplayer & Cloud

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_multiplayer` | setup_network_manager, add_network_object, create_rpc, create_lobby | Netcode & Lobby |
| `manage_cloud` | auth_sign_in, save_data, load_data, config_fetch | Cloud Save, Auth, Remote Config |

### 🤖 AI & Assets

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_addressables` | mark_addressable, create_group, build_content, load_asset | Asset bundles |
| `manage_mlagents` | add_agent, configure_behavior, add_sensor, create_training_config | ML-Agents AI |
| `manage_vcs` | git_status, git_commit, git_push, generate_gitignore | Git integration |

### 🛠️ Editor Utilities

| Tool | Actions | Description |
|------|---------|-------------|
| `manage_editor` | selection_get, focus, preferences | Editor control |
| `manage_asset` | import, move, rename, delete | Asset management |
| `read_console` | get_logs, clear, filter | Console access |
| `refresh_unity` | refresh_assets, recompile | Force refresh |
| `run_tests` | run_all, run_category | Test runner |
| `batch_execute` | execute_multiple | Batch operations |

> ⚠️ = Requires optional Unity package. See [Optional Packages](#-optional-packages).

---

## 💡 Usage Examples

### Create a Complete Player Controller

```
"Create a Player GameObject at origin with:
- Rigidbody (mass 1, drag 0.5, freeze rotation X and Z)
- CapsuleCollider (height 2, radius 0.5)
- A new C# script called PlayerController with movement using Input System"
```

### Build a UI Menu

```
"Create a main menu UI with:
- Canvas using Screen Space Overlay
- Title text saying 'My Game' at the top
- Start Game button in the center
- Settings and Quit buttons below
- All buttons with hover effects"
```

### Set Up Scene Lighting

```
"Set up lighting for an indoor scene:
- Ambient color to warm orange (#FFE4C4)
- Main directional light at 45 degrees, soft shadows
- 4 point lights at corners with blue tint
- Enable fog with exponential falloff"
```

### Configure Build Settings

```
"Prepare the game for Android release:
- Switch platform to Android
- Set package name to com.mycompany.mygame
- Set version to 1.0.0
- Add all scenes from Assets/Scenes to build
- Enable development build with debugging"
```

### Create Navigation System

```
"Set up navigation for the level:
- Bake NavMesh for all static geometry
- Add NavMeshAgent to all Enemy objects
- Set Enemy speed to 4, acceleration to 8
- Add NavMeshObstacle to all Barrel prefabs"
```

---

## 📦 Optional Packages

Some tools require additional Unity packages. Install via Package Manager:

| Feature | Package | Install | Scripting Define |
|---------|---------|---------|------------------|
| **ProBuilder** | `com.unity.probuilder` | Package Manager | `PROBUILDER_ENABLED` |
| **Cinemachine** | `com.unity.cinemachine` | Package Manager | `CINEMACHINE_ENABLED` |
| **Timeline** | `com.unity.timeline` | Package Manager | `TIMELINE_ENABLED` |
| **Localization** | `com.unity.localization` | Package Manager | `LOCALIZATION_ENABLED` |

**To add scripting defines:**
1. Go to **Edit → Project Settings → Player**
2. Find **Scripting Define Symbols**
3. Add the required define (e.g., `CINEMACHINE_ENABLED`)
4. Click **Apply**

---

## 🏗️ Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                        AI Assistant                          │
│              (Claude / Cursor / VS Code / Gemini)            │
└─────────────────────────┬────────────────────────────────────┘
                          │ MCP Protocol (stdio)
                          ▼
┌──────────────────────────────────────────────────────────────┐
│                     DevBridge Server                          │
│                    (Python + FastMCP)                         │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
│  │ 28+ Tools   │ │  Transport  │ │   Router    │            │
│  └─────────────┘ └─────────────┘ └─────────────┘            │
└─────────────────────────┬────────────────────────────────────┘
                          │ HTTP (localhost:5010)
                          ▼
┌──────────────────────────────────────────────────────────────┐
│                      Unity Editor                             │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
│  │  Handlers   │ │  Registry   │ │  Executor   │            │
│  └─────────────┘ └─────────────┘ └─────────────┘            │
└──────────────────────────────────────────────────────────────┘
```

---

## 🔧 Troubleshooting

### Server won't start
- Ensure Python 3.10+ is installed: `python --version`
- Ensure uv is installed: `uv --version`
- Check Unity console for errors

### AI can't see DevBridge tools
- Restart your AI client after configuration
- Check if server shows 🟢 Connected in Unity
- Verify MCP config file syntax

### Commands fail
- Make sure Unity Editor is focused/active
- Check Unity console for detailed errors
- Ensure required packages are installed for optional tools

---

## 🤝 Contributing

We welcome contributions! Here's how:

1. **Fork** the repository
2. **Create** a feature branch: `git checkout -b feature/amazing-feature`
3. **Commit** your changes: `git commit -m 'Add amazing feature'`
4. **Push** to GitHub: `git push origin feature/amazing-feature`
5. **Open** a Pull Request

### Development Setup

```bash
# Clone the repo
git clone https://github.com/cnrgrsc/unity-mcp.git

# Navigate to server
cd unity-mcp/Server

# Install dependencies
uv sync

# Run server locally
uv run devbridge
```

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

<div align="center">

## ⭐ Star This Project!

If DevBridge helps your Unity development, please consider giving it a star!

[![GitHub stars](https://img.shields.io/github/stars/cnrgrsc/unity-mcp?style=social)](https://github.com/cnrgrsc/unity-mcp)

**Made with ❤️ for Unity Developers Worldwide**

</div>
