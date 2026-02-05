"""
manage_2d.py - 2D Game Development Tool

Handles:
- Sprite management (create, configure)
- 2D Physics (Rigidbody2D, Collider2D)
- Tilemaps
- SpriteRenderer configuration
- Sprite animations (2D animation)
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_2d."""
    return {
        "name": "manage_2d",
        "description": """Manage Unity 2D features including sprites, 2D physics, tilemaps, and sprite animations.

Actions:
- sprite_create: Create a new sprite from texture
- sprite_configure: Configure SpriteRenderer properties
- sprite_get_info: Get sprite information
- rigidbody2d_add: Add Rigidbody2D component
- rigidbody2d_configure: Configure Rigidbody2D properties
- collider2d_add: Add 2D collider (box, circle, polygon, capsule)
- collider2d_configure: Configure 2D collider properties
- tilemap_create: Create a tilemap
- tilemap_set_tile: Set tile at position
- tilemap_clear: Clear tilemap
- sorting_layer_set: Set sorting layer and order
- sprite_animator_add: Add Animator for sprite animation""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "sprite_create",
                        "sprite_configure",
                        "sprite_get_info",
                        "rigidbody2d_add",
                        "rigidbody2d_configure",
                        "collider2d_add",
                        "collider2d_configure",
                        "tilemap_create",
                        "tilemap_set_tile",
                        "tilemap_clear",
                        "sorting_layer_set",
                        "sprite_animator_add"
                    ]
                },
                "target": {
                    "type": ["string", "integer"],
                    "description": "Target GameObject (name or instance ID)"
                },
                "texturePath": {
                    "type": "string",
                    "description": "Path to sprite texture asset"
                },
                "color": {
                    "type": "string",
                    "description": "Sprite color (hex format: #RRGGBB)"
                },
                "flipX": {
                    "type": "boolean",
                    "description": "Flip sprite horizontally"
                },
                "flipY": {
                    "type": "boolean",
                    "description": "Flip sprite vertically"
                },
                "sortingLayer": {
                    "type": "string",
                    "description": "Sorting layer name"
                },
                "sortingOrder": {
                    "type": "integer",
                    "description": "Order in layer"
                },
                "bodyType": {
                    "type": "string",
                    "description": "Rigidbody2D body type: Dynamic, Kinematic, Static",
                    "enum": ["Dynamic", "Kinematic", "Static"]
                },
                "mass": {
                    "type": "number",
                    "description": "Rigidbody2D mass"
                },
                "gravityScale": {
                    "type": "number",
                    "description": "Gravity scale for Rigidbody2D"
                },
                "linearDrag": {
                    "type": "number",
                    "description": "Linear drag"
                },
                "angularDrag": {
                    "type": "number",
                    "description": "Angular drag"
                },
                "freezeRotation": {
                    "type": "boolean",
                    "description": "Freeze Z rotation"
                },
                "colliderType": {
                    "type": "string",
                    "description": "2D Collider type",
                    "enum": ["box", "circle", "polygon", "capsule", "edge", "composite"]
                },
                "size": {
                    "type": "object",
                    "description": "Collider size {x, y}",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"}
                    }
                },
                "radius": {
                    "type": "number",
                    "description": "Circle/Capsule collider radius"
                },
                "isTrigger": {
                    "type": "boolean",
                    "description": "Is trigger collider"
                },
                "offset": {
                    "type": "object",
                    "description": "Collider offset {x, y}",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"}
                    }
                },
                "tilemapName": {
                    "type": "string",
                    "description": "Name for the tilemap"
                },
                "position": {
                    "type": "object",
                    "description": "Tile position {x, y, z}",
                    "properties": {
                        "x": {"type": "integer"},
                        "y": {"type": "integer"},
                        "z": {"type": "integer"}
                    }
                },
                "tilePath": {
                    "type": "string",
                    "description": "Path to tile asset"
                },
                "animationController": {
                    "type": "string",
                    "description": "Path to animation controller asset"
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_2d tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    # Build params for Unity
    params = {"action": action}
    
    # Copy relevant parameters
    param_keys = [
        "target", "texturePath", "color", "flipX", "flipY",
        "sortingLayer", "sortingOrder", "bodyType", "mass",
        "gravityScale", "linearDrag", "angularDrag", "freezeRotation",
        "colliderType", "size", "radius", "isTrigger", "offset",
        "tilemapName", "position", "tilePath", "animationController"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_2d",
        "params": params
    })
