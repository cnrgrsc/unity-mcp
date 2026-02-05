"""
Tool for managing Audio components in Unity.
Supports AudioSource, AudioListener, and AudioMixer operations.
"""
from typing import Annotated, Any, Literal

from fastmcp import Context
from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry
from services.tools.preflight import preflight


@mcp_for_unity_tool(
    description="Manages Unity Audio components (AudioSource, AudioListener, AudioMixer). Read-only actions: source_get_info, listener_get_info, mixer_get_info. Modifying actions: source_add, source_configure, source_play, source_stop, source_pause, source_set_clip, listener_add, mixer_set_parameter."
)
async def manage_audio(
    ctx: Context,
    action: Annotated[
        Literal[
            "source_add", "source_configure", "source_get_info",
            "source_play", "source_stop", "source_pause", "source_set_clip",
            "listener_add", "listener_get_info",
            "mixer_get_info", "mixer_set_parameter"
        ],
        "Action to perform on audio components"
    ],
    # Target GameObject
    target: Annotated[
        str | int,
        "Target GameObject - instance ID or name"
    ] | None = None,
    # AudioSource properties
    clip: Annotated[str, "Path to AudioClip asset"] | None = None,
    volume: Annotated[float, "Volume (0.0 - 1.0)"] | None = None,
    pitch: Annotated[float, "Pitch multiplier"] | None = None,
    loop: Annotated[bool, "Whether to loop the audio"] | None = None,
    play_on_awake: Annotated[bool, "Play audio on start"] | None = None,
    mute: Annotated[bool, "Mute the audio source"] | None = None,
    spatial_blend: Annotated[float, "2D (0) to 3D (1) blend"] | None = None,
    doppler_level: Annotated[float, "Doppler effect level"] | None = None,
    spread: Annotated[float, "Spread angle for 3D sound (0-360)"] | None = None,
    min_distance: Annotated[float, "Minimum distance for volume rolloff"] | None = None,
    max_distance: Annotated[float, "Maximum distance for volume rolloff"] | None = None,
    priority: Annotated[int, "Audio priority (0-256, 0 is highest)"] | None = None,
    output_mixer_group: Annotated[str, "Path to AudioMixerGroup"] | None = None,
    delay: Annotated[float, "Delay before playing (seconds)"] | None = None,
    # AudioMixer parameters
    mixer: Annotated[str, "Path to AudioMixer asset"] | None = None,
    parameter: Annotated[str, "Exposed parameter name"] | None = None,
    value: Annotated[float, "Parameter value to set"] | None = None,
) -> dict[str, Any]:
    """
    Manage Unity Audio components.

    Actions:
    - source_add: Add AudioSource to GameObject
    - source_configure: Configure AudioSource properties
    - source_get_info: Get AudioSource information
    - source_play: Play audio (PlayMode only)
    - source_stop: Stop audio playback
    - source_pause: Pause audio playback
    - source_set_clip: Set AudioClip on source
    - listener_add: Add AudioListener to GameObject
    - listener_get_info: Get scene AudioListener info
    - mixer_get_info: Get AudioMixer information
    - mixer_set_parameter: Set exposed AudioMixer parameter

    Examples:
    - Add AudioSource: action="source_add", target="BGM", clip="Assets/Audio/Music.mp3"
    - Configure Source: action="source_configure", target="SFX", volume=0.5, spatial_blend=1.0
    - Play Audio: action="source_play", target="BGM"
    - Set Mixer Parameter: action="mixer_set_parameter", mixer="Assets/Audio/Master.mixer", parameter="MasterVolume", value=-10
    """
    unity_instance = get_unity_instance_from_context(ctx)

    gate = await preflight(ctx, wait_for_no_compile=True, refresh_if_dirty=True)
    if gate is not None:
        return gate.model_dump()

    if not action:
        return {
            "success": False,
            "message": "Missing required parameter 'action'."
        }

    try:
        params: dict[str, Any] = {"action": action}

        if target is not None:
            params["target"] = target

        # AudioSource properties
        if clip is not None:
            params["clip"] = clip
        if volume is not None:
            params["volume"] = volume
        if pitch is not None:
            params["pitch"] = pitch
        if loop is not None:
            params["loop"] = loop
        if play_on_awake is not None:
            params["playOnAwake"] = play_on_awake
        if mute is not None:
            params["mute"] = mute
        if spatial_blend is not None:
            params["spatialBlend"] = spatial_blend
        if doppler_level is not None:
            params["dopplerLevel"] = doppler_level
        if spread is not None:
            params["spread"] = spread
        if min_distance is not None:
            params["minDistance"] = min_distance
        if max_distance is not None:
            params["maxDistance"] = max_distance
        if priority is not None:
            params["priority"] = priority
        if output_mixer_group is not None:
            params["outputMixerGroup"] = output_mixer_group
        if delay is not None:
            params["delay"] = delay

        # AudioMixer parameters
        if mixer is not None:
            params["mixer"] = mixer
        if parameter is not None:
            params["parameter"] = parameter
        if value is not None:
            params["value"] = value

        response = await send_with_unity_instance(
            async_send_command_with_retry,
            unity_instance,
            "manage_audio",
            params,
        )

        if isinstance(response, dict) and response.get("success"):
            return {
                "success": True,
                "message": response.get("message", f"Audio {action} successful."),
                "data": response.get("data")
            }
        return response if isinstance(response, dict) else {"success": False, "message": str(response)}

    except Exception as e:
        return {"success": False, "message": f"Error managing audio: {e!s}"}
