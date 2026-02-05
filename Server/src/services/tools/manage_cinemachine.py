"""
manage_cinemachine.py - Cinemachine Camera System Tool

Handles:
- Virtual cameras
- Camera blending
- Dolly tracks
- Follow and look-at targets
- Camera shake/impulse
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_cinemachine."""
    return {
        "name": "manage_cinemachine",
        "description": """Manage Unity Cinemachine virtual cameras and camera systems.

Actions:
- vcam_create: Create a virtual camera
- vcam_configure: Configure virtual camera settings
- vcam_get_info: Get virtual camera information
- vcam_set_follow: Set follow target
- vcam_set_lookat: Set look-at target
- dolly_create: Create a dolly track
- dolly_add_waypoint: Add waypoint to dolly track
- blend_configure: Configure camera blending
- impulse_add: Add camera impulse/shake source
- impulse_trigger: Trigger camera shake

Note: Requires Cinemachine package (com.unity.cinemachine).""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "vcam_create",
                        "vcam_configure",
                        "vcam_get_info",
                        "vcam_set_follow",
                        "vcam_set_lookat",
                        "dolly_create",
                        "dolly_add_waypoint",
                        "blend_configure",
                        "impulse_add",
                        "impulse_trigger"
                    ]
                },
                "target": {
                    "type": ["string", "integer"],
                    "description": "Target virtual camera (name or instance ID)"
                },
                "cameraName": {
                    "type": "string",
                    "description": "Name for the virtual camera"
                },
                "priority": {
                    "type": "integer",
                    "description": "Camera priority (higher = more important)"
                },
                "followTarget": {
                    "type": ["string", "integer"],
                    "description": "Follow target GameObject"
                },
                "lookAtTarget": {
                    "type": ["string", "integer"],
                    "description": "Look-at target GameObject"
                },
                "bodyType": {
                    "type": "string",
                    "description": "Body tracking type",
                    "enum": ["Transposer", "FramingTransposer", "OrbitalTransposer", "TrackedDolly", "HardLockToTarget"]
                },
                "aimType": {
                    "type": "string",
                    "description": "Aim tracking type",
                    "enum": ["Composer", "GroupComposer", "HardLookAt", "POV", "SameAsFollowTarget"]
                },
                "followOffset": {
                    "type": "object",
                    "description": "Offset from follow target {x, y, z}",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"}
                    }
                },
                "damping": {
                    "type": "object",
                    "description": "Damping {x, y, z}",
                    "properties": {
                        "x": {"type": "number"},
                        "y": {"type": "number"},
                        "z": {"type": "number"}
                    }
                },
                "fov": {
                    "type": "number",
                    "description": "Field of view"
                },
                "nearClip": {
                    "type": "number",
                    "description": "Near clip plane"
                },
                "farClip": {
                    "type": "number",
                    "description": "Far clip plane"
                },
                "waypoints": {
                    "type": "array",
                    "description": "Dolly track waypoints",
                    "items": {
                        "type": "object",
                        "properties": {
                            "x": {"type": "number"},
                            "y": {"type": "number"},
                            "z": {"type": "number"}
                        }
                    }
                },
                "blendTime": {
                    "type": "number",
                    "description": "Blend time in seconds"
                },
                "blendStyle": {
                    "type": "string",
                    "description": "Blend style",
                    "enum": ["Cut", "EaseInOut", "EaseIn", "EaseOut", "HardIn", "HardOut", "Linear"]
                },
                "impulseForce": {
                    "type": "number",
                    "description": "Impulse force for camera shake"
                },
                "impulseDuration": {
                    "type": "number",
                    "description": "Impulse duration in seconds"
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_cinemachine tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        "target", "cameraName", "priority", "followTarget", "lookAtTarget",
        "bodyType", "aimType", "followOffset", "damping", "fov", "nearClip",
        "farClip", "waypoints", "blendTime", "blendStyle", "impulseForce", "impulseDuration"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_cinemachine",
        "params": params
    })
