"""
manage_addressables.py - Addressables & Asset Bundles Tool

Handles:
- Unity Addressables system
- Asset bundle management
- Remote asset loading
- Build profiles
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_addressables."""
    return {
        "name": "manage_addressables",
        "description": """Manage Unity Addressables system for asset bundle management and remote loading.

Actions:
- initialize: Initialize Addressables system
- mark_addressable: Mark asset as addressable
- unmark_addressable: Remove addressable flag
- set_address: Set custom address for asset
- set_label: Add/remove labels from asset
- create_group: Create new Addressables group
- delete_group: Delete Addressables group
- move_to_group: Move asset to group
- set_group_settings: Configure group settings
- build_content: Build addressable content
- build_player_content: Build for player
- clean_build: Clean and rebuild
- update_catalog: Update remote catalog
- load_asset: Load asset by address
- release_asset: Release loaded asset
- preload_dependencies: Preload asset dependencies
- get_download_size: Get download size for address
- download_dependencies: Download remote dependencies
- clear_cache: Clear addressables cache
- list_groups: List all addressable groups
- list_assets: List assets in group""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "initialize",
                        "mark_addressable",
                        "unmark_addressable",
                        "set_address",
                        "set_label",
                        "create_group",
                        "delete_group",
                        "move_to_group",
                        "set_group_settings",
                        "build_content",
                        "build_player_content",
                        "clean_build",
                        "update_catalog",
                        "load_asset",
                        "release_asset",
                        "preload_dependencies",
                        "get_download_size",
                        "download_dependencies",
                        "clear_cache",
                        "list_groups",
                        "list_assets"
                    ]
                },
                "assetPath": {
                    "type": "string",
                    "description": "Path to asset in project"
                },
                "address": {
                    "type": "string",
                    "description": "Custom address for asset"
                },
                "labels": {
                    "type": "array",
                    "description": "Labels for asset",
                    "items": {"type": "string"}
                },
                "groupName": {
                    "type": "string",
                    "description": "Addressables group name"
                },
                "groupSettings": {
                    "type": "object",
                    "description": "Group settings",
                    "properties": {
                        "bundleMode": {
                            "type": "string",
                            "enum": ["PackTogether", "PackSeparately", "PackTogetherByLabel"]
                        },
                        "buildPath": {"type": "string"},
                        "loadPath": {"type": "string"},
                        "includeInBuild": {"type": "boolean"}
                    }
                },
                "buildProfile": {
                    "type": "string",
                    "description": "Build profile name"
                },
                "remoteBuildPath": {
                    "type": "string",
                    "description": "Remote build output path"
                },
                "remoteLoadPath": {
                    "type": "string",
                    "description": "Remote load URL"
                },
                "catalogUrl": {
                    "type": "string",
                    "description": "Remote catalog URL"
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_addressables tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        "assetPath", "address", "labels", "groupName", "groupSettings",
        "buildProfile", "remoteBuildPath", "remoteLoadPath", "catalogUrl"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_addressables",
        "params": params
    })
