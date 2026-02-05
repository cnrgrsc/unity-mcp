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

[🚀 Quick Start](#-quick-start) • [✨ Features](#-features) • [📖 Documentation](#-documentation) • [🤝 Contributing](#-contributing)

</div>

---

## 🎯 What is DevBridge?

**DevBridge** is an advanced AI bridge that connects your favorite AI assistants (Claude, Cursor, VS Code, Gemini, etc.) to Unity Editor via the [Model Context Protocol](https://modelcontextprotocol.io). It goes beyond basic automation — DevBridge gives AI the power to:

- 🎮 **Create complete game mechanics** (physics, navigation, animations)
- 🎨 **Design UI systems** with AI assistance
- 🔊 **Implement audio systems** seamlessly
- 🎯 **Build input handling** for any platform
- ⚡ **Automate repetitive tasks** 10-100x faster

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

### 🎮 Game Development Tools

DevBridge includes **exclusive game development tools** not found in the original:

| Tool | Description | Status |
|------|-------------|--------|
| `manage_physics` | Rigidbodies, colliders, joints, raycasting | ✅ Ready |
| `manage_animation` | Animators, clips, state machines, blend trees | ✅ Ready |
| `manage_navigation` | NavMesh, AI agents, pathfinding, obstacles | ✅ Ready |
| `manage_audio` | Audio sources, mixers, 3D spatial audio | ✅ Ready |
| `manage_input` | Input System, action maps, control schemes | ✅ Ready |
| `manage_ui` | Canvas, UI elements, layouts, interactions | ✅ Ready |

### 🛠️ Core Tools

All the essential Unity automation tools:

| Category | Tools |
|----------|-------|
| **Assets** | `manage_asset` • `manage_prefabs` • `manage_material` • `manage_texture` |
| **Scene** | `manage_scene` • `manage_gameobject` • `manage_components` • `find_gameobjects` |
| **Scripts** | `manage_script` • `script_apply_edits` • `validate_script` • `create_script` |
| **Editor** | `manage_editor` • `execute_menu_item` • `read_console` • `refresh_unity` |
| **Advanced** | `manage_vfx` • `manage_shader` • `manage_scriptable_object` • `batch_execute` |

### ⚡ Performance

Use `batch_execute` for **10-100x faster** multi-object operations!

---

## 🏆 DevBridge vs Competition

| Feature | DevBridge | Others |
|---------|-----------|--------|
| Physics System Control | ✅ Full | ❌ None |
| Animation System | ✅ Complete | ❌ Limited |
| Navigation/AI | ✅ NavMesh + Agents | ❌ None |
| Audio Management | ✅ 3D Spatial | ❌ None |
| Input System | ✅ New Input System | ❌ None |
| UI Building | ✅ Full Canvas | ❌ Basic |
| Batch Operations | ✅ 10-100x faster | ⚠️ Slower |
| Open Source | ✅ MIT License | ⚠️ Varies |

---

## 📖 Documentation

<details>
<summary><strong>Manual Configuration</strong></summary>

If auto-setup doesn't work, add this to your MCP client's config:

**HTTP (Claude Desktop, Cursor, Windsurf):**
```json
{
  "mcpServers": {
    "devBridge": {
      "url": "http://localhost:8080/mcp"
    }
  }
}
```

**VS Code:**
```json
{
  "servers": {
    "devBridge": {
      "type": "http",
      "url": "http://localhost:8080/mcp"
    }
  }
}
```
</details>

<details>
<summary><strong>Multiple Unity Instances</strong></summary>

DevBridge supports multiple Unity Editor instances:

1. Ask your AI to check `unity_instances` resource
2. Use `set_active_instance` with `Name@hash`
3. All commands route to that instance
</details>

<details>
<summary><strong>Troubleshooting</strong></summary>

- **Unity Bridge Not Connecting:** Check `Window > DevBridge` status
- **Server Not Starting:** Run `uv --version` to verify installation
- **Client Not Connecting:** Ensure HTTP server is running on port 8080
</details>

---

## 🤝 Contributing

Contributions are welcome! 

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📜 License

MIT License — see [LICENSE](LICENSE)

> **Attribution:** This project is based on [MCP for Unity](https://github.com/CoplayDev/unity-mcp) by CoplayDev, enhanced with additional game development tools.

---

## 🌟 Star History

If you find DevBridge useful, please consider giving it a ⭐!

---

<div align="center">

**Made with ❤️ by [cnrgrsc](https://github.com/cnrgrsc)**

*DevBridge — Building the bridge between AI and Unity*

</div>
