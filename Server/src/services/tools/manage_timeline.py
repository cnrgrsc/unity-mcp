"""
manage_timeline.py - Timeline/Cinematic Sequences Tool

Handles:
- Timeline asset creation
- Track management (animation, audio, signal, control)
- Playback control
- Clip management
"""

from typing import Any
from mcp.types import TextContent
from .utils import generate_tool_response


def get_tool_definition():
    """Returns the tool definition for manage_timeline."""
    return {
        "name": "manage_timeline",
        "description": """Manage Unity Timeline for cinematic sequences and cutscenes.

Actions:
- timeline_create: Create a new timeline asset
- timeline_get_info: Get timeline information
- director_add: Add PlayableDirector to GameObject
- director_configure: Configure PlayableDirector
- track_add: Add track to timeline (animation, audio, activation, signal, control)
- track_remove: Remove track from timeline
- clip_add: Add clip to track
- clip_configure: Configure clip timing
- playback_play: Start playback
- playback_pause: Pause playback
- playback_stop: Stop playback
- binding_set: Set track binding

Note: Requires Timeline package (com.unity.timeline).""",
        "inputSchema": {
            "type": "object",
            "properties": {
                "action": {
                    "type": "string",
                    "description": "Action to perform",
                    "enum": [
                        "timeline_create",
                        "timeline_get_info",
                        "director_add",
                        "director_configure",
                        "track_add",
                        "track_remove",
                        "clip_add",
                        "clip_configure",
                        "playback_play",
                        "playback_pause",
                        "playback_stop",
                        "binding_set"
                    ]
                },
                "target": {
                    "type": ["string", "integer"],
                    "description": "Target GameObject with PlayableDirector"
                },
                "timelineName": {
                    "type": "string",
                    "description": "Name for the timeline asset"
                },
                "timelinePath": {
                    "type": "string",
                    "description": "Path to timeline asset"
                },
                "trackType": {
                    "type": "string",
                    "description": "Type of track to add",
                    "enum": ["Animation", "Audio", "Activation", "Signal", "Control", "Cinemachine"]
                },
                "trackName": {
                    "type": "string",
                    "description": "Name for the track"
                },
                "trackIndex": {
                    "type": "integer",
                    "description": "Index of the track"
                },
                "clipPath": {
                    "type": "string",
                    "description": "Path to clip asset (animation, audio, etc.)"
                },
                "clipStart": {
                    "type": "number",
                    "description": "Clip start time in seconds"
                },
                "clipDuration": {
                    "type": "number",
                    "description": "Clip duration in seconds"
                },
                "clipEnd": {
                    "type": "number",
                    "description": "Clip end time in seconds"
                },
                "wrapMode": {
                    "type": "string",
                    "description": "Director wrap mode",
                    "enum": ["Hold", "Loop", "None"]
                },
                "playOnAwake": {
                    "type": "boolean",
                    "description": "Play timeline on awake"
                },
                "initialTime": {
                    "type": "number",
                    "description": "Initial playback time"
                },
                "bindingTarget": {
                    "type": ["string", "integer"],
                    "description": "Target to bind to track"
                }
            },
            "required": ["action"]
        }
    }


async def handle_tool_call(arguments: dict[str, Any]) -> list[TextContent]:
    """Handle manage_timeline tool calls."""
    action = arguments.get("action")
    
    if not action:
        return generate_tool_response({
            "success": False,
            "error": "'action' parameter is required"
        })

    params = {"action": action}
    
    param_keys = [
        "target", "timelineName", "timelinePath", "trackType", "trackName",
        "trackIndex", "clipPath", "clipStart", "clipDuration", "clipEnd",
        "wrapMode", "playOnAwake", "initialTime", "bindingTarget"
    ]
    
    for key in param_keys:
        if key in arguments and arguments[key] is not None:
            params[key] = arguments[key]

    return generate_tool_response({
        "tool": "manage_timeline",
        "params": params
    })
