"""
manage_build.py - Build & Deployment Tool

Handles:
- Build settings configuration
- Platform switching
- Player settings
- Build execution
- Scenes in build
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_build."""
    return {
        "name": "manage_build",
        "description": """Manage Unity build settings, player settings, and execute builds.

Actions:
- build_get_settings: Get current build settings
- build_set_platform: Switch target platform
- build_add_scene: Add scene to build
- build_remove_scene: Remove scene from build
- build_set_scenes: Set all scenes in build
- build_execute: Execute build
- player_get_settings: Get player settings
- player_set_settings: Set player settings (product name, company, version, etc.)
- define_add: Add scripting define symbol
- define_remove: Remove scripting define symbol
- define_list: List all scripting define symbols""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "build_get_settings",
                        "build_set_platform",
                        "build_add_scene",
                        "build_remove_scene",
                        "build_set_scenes",
                        "build_execute",
                        "player_get_settings",
                        "player_set_settings",
                        "define_add",
                        "define_remove",
                        "define_list"
                    ]
                },
                "platform": {
                    "type": "string",
                    "description": "Target platform",
                    "enum": [
                        "StandaloneWindows64",
                        "StandaloneOSX",
                        "StandaloneLinux64",
                        "Android",
                        "iOS",
                        "WebGL",
                        "PS4",
                        "PS5",
                        "XboxOne",
                        "Switch"
                    ]
                },
                "scenePath": {
                    "type": "string",
                    "description": "Scene path to add/remove"
                },
                "scenePaths": {
                    "type": "array",
                    "description": "List of scene paths",
                    "items": {"type": "string"}
                },
                "buildPath": {
                    "type": "string",
                    "description": "Output path for build"
                },
                "buildOptions": {
                    "type": "array",
                    "description": "Build options",
                    "items": {
                        "type": "string",
                        "enum": ["Development", "AutoRunPlayer", "ShowBuiltPlayer", "CleanBuildCache", "AllowDebugging"]
                    }
                },
                "productName": {
                    "type": "string",
                    "description": "Product name"
                },
                "companyName": {
                    "type": "string",
                    "description": "Company name"
                },
                "version": {
                    "type": "string",
                    "description": "Bundle version"
                },
                "bundleIdentifier": {
                    "type": "string",
                    "description": "Bundle identifier (e.g., com.company.product)"
                },
                "defineSymbol": {
                    "type": "string",
                    "description": "Scripting define symbol"
                },
                "icon": {
                    "type": "string",
                    "description": "Path to icon texture"
                },
                "splashScreen": {
                    "type": "string",
                    "description": "Path to splash screen texture"
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_build tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        "platform", "scenePath", "scenePaths", "buildPath", "buildOptions",
        "productName", "companyName", "version", "bundleIdentifier",
        "defineSymbol", "icon", "splashScreen"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_build",
        "params": params
    })
