"""
Tool for managing Lighting in Unity.
Supports light creation, configuration, ambient settings, fog, and reflection probes.
"""
from typing import Annotated, Any, Literal

from fastmcp import Context
from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry
from services.tools.preflight import preflight


@mcp_for_unity_tool(
    description="Manages Unity Lighting - create lights, configure properties, set ambient/fog, bake lightmaps, and manage reflection probes. Actions: light_create, light_configure, light_get_info, ambient_set, fog_set, fog_get, reflection_probe_create, reflection_probe_bake, lightmap_bake, lighting_get_settings."
)
async def manage_lighting(
    ctx: Context,
    action: Annotated[
        Literal[
            "light_create", "light_configure", "light_get_info",
            "ambient_set", "ambient_get",
            "fog_set", "fog_get",
            "reflection_probe_create", "reflection_probe_bake",
            "lightmap_bake", "lighting_get_settings"
        ],
        "Action to perform on lighting"
    ],
    target: Annotated[
        str | int,
        "Target Light/ReflectionProbe GameObject - instance ID or name/path"
    ] | None = None,
    # Light creation/configuration
    light_type: Annotated[
        Literal["Directional", "Point", "Spot", "Area"],
        "Type of light to create"
    ] | None = None,
    color: Annotated[
        list[float] | dict | str,
        "Light color [r, g, b] (0-1) or hex string"
    ] | None = None,
    intensity: Annotated[float, "Light intensity"] | None = None,
    range: Annotated[float, "Point/Spot light range"] | None = None,
    spot_angle: Annotated[float, "Spot light angle in degrees"] | None = None,
    inner_spot_angle: Annotated[float, "Spot light inner angle"] | None = None,
    shadows: Annotated[
        Literal["None", "Hard", "Soft"],
        "Shadow type"
    ] | None = None,
    shadow_strength: Annotated[float, "Shadow strength (0-1)"] | None = None,
    shadow_resolution: Annotated[
        Literal["FromQualitySettings", "Low", "Medium", "High", "VeryHigh"],
        "Shadow resolution"
    ] | None = None,
    shadow_bias: Annotated[float, "Shadow bias"] | None = None,
    shadow_normal_bias: Annotated[float, "Shadow normal bias"] | None = None,
    cookie: Annotated[str, "Path to cookie texture"] | None = None,
    cookie_size: Annotated[float, "Cookie projection size"] | None = None,
    culling_mask: Annotated[int | str, "Culling mask layers"] | None = None,
    position: Annotated[list[float] | dict, "Light position [x, y, z]"] | None = None,
    rotation: Annotated[list[float] | dict, "Light rotation [x, y, z]"] | None = None,
    # Ambient settings
    ambient_mode: Annotated[
        Literal["Skybox", "Trilight", "Flat", "Custom"],
        "Ambient lighting mode"
    ] | None = None,
    ambient_color: Annotated[
        list[float] | dict | str,
        "Ambient color for Flat mode"
    ] | None = None,
    ambient_sky_color: Annotated[list[float] | dict, "Sky color for Trilight"] | None = None,
    ambient_equator_color: Annotated[list[float] | dict, "Equator color for Trilight"] | None = None,
    ambient_ground_color: Annotated[list[float] | dict, "Ground color for Trilight"] | None = None,
    ambient_intensity: Annotated[float, "Ambient intensity multiplier"] | None = None,
    # Fog settings
    fog_enabled: Annotated[bool, "Enable/disable fog"] | None = None,
    fog_mode: Annotated[
        Literal["Linear", "Exponential", "ExponentialSquared"],
        "Fog mode"
    ] | None = None,
    fog_color: Annotated[list[float] | dict | str, "Fog color"] | None = None,
    fog_density: Annotated[float, "Fog density for exponential modes"] | None = None,
    fog_start: Annotated[float, "Fog start distance for linear mode"] | None = None,
    fog_end: Annotated[float, "Fog end distance for linear mode"] | None = None,
    # Reflection probe
    probe_resolution: Annotated[int, "Reflection probe resolution"] | None = None,
    probe_size: Annotated[list[float], "Probe box size [x, y, z]"] | None = None,
    probe_center: Annotated[list[float], "Probe center offset [x, y, z]"] | None = None,
    probe_hdr: Annotated[bool, "Enable HDR for probe"] | None = None,
    probe_importance: Annotated[int, "Probe importance for blending"] | None = None,
    # Lightmap baking
    bake_quality: Annotated[
        Literal["Low", "Medium", "High", "VeryHigh"],
        "Lightmap bake quality"
    ] | None = None,
    bake_mode: Annotated[
        Literal["Shadowmask", "Subtractive", "Baked"],
        "Mixed lighting mode"
    ] | None = None,
) -> dict[str, Any]:
    """
    Manage Unity Lighting.

    Actions:
    - light_create: Create a new light
    - light_configure: Configure light properties
    - light_get_info: Get light information
    - ambient_set: Set ambient lighting
    - ambient_get: Get ambient settings
    - fog_set: Configure fog
    - fog_get: Get fog settings
    - reflection_probe_create: Create reflection probe
    - reflection_probe_bake: Bake reflection probe
    - lightmap_bake: Bake lightmaps
    - lighting_get_settings: Get all lighting settings

    Examples:
    - Create light: action="light_create", light_type="Point", color=[1,0.9,0.8], intensity=2
    - Set fog: action="fog_set", fog_enabled=true, fog_mode="Linear", fog_start=10, fog_end=100
    - Bake lightmaps: action="lightmap_bake", bake_quality="High"
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

    # Actions that don't require target
    no_target_actions = ["light_create", "ambient_set", "ambient_get", "fog_set", "fog_get", 
                         "reflection_probe_create", "lightmap_bake", "lighting_get_settings"]
    if action not in no_target_actions and not target:
        return {
            "success": False,
            "message": "Missing required parameter 'target'. Specify Light/Probe GameObject."
        }

    try:
        params: dict[str, Any] = {"action": action}
        
        if target is not None:
            params["target"] = target

        # Light properties
        if light_type is not None:
            params["lightType"] = light_type
        if color is not None:
            params["color"] = color
        if intensity is not None:
            params["intensity"] = intensity
        if range is not None:
            params["range"] = range
        if spot_angle is not None:
            params["spotAngle"] = spot_angle
        if inner_spot_angle is not None:
            params["innerSpotAngle"] = inner_spot_angle
        if shadows is not None:
            params["shadows"] = shadows
        if shadow_strength is not None:
            params["shadowStrength"] = shadow_strength
        if shadow_resolution is not None:
            params["shadowResolution"] = shadow_resolution
        if shadow_bias is not None:
            params["shadowBias"] = shadow_bias
        if shadow_normal_bias is not None:
            params["shadowNormalBias"] = shadow_normal_bias
        if cookie is not None:
            params["cookie"] = cookie
        if cookie_size is not None:
            params["cookieSize"] = cookie_size
        if culling_mask is not None:
            params["cullingMask"] = culling_mask
        if position is not None:
            params["position"] = position
        if rotation is not None:
            params["rotation"] = rotation

        # Ambient settings
        if ambient_mode is not None:
            params["ambientMode"] = ambient_mode
        if ambient_color is not None:
            params["ambientColor"] = ambient_color
        if ambient_sky_color is not None:
            params["ambientSkyColor"] = ambient_sky_color
        if ambient_equator_color is not None:
            params["ambientEquatorColor"] = ambient_equator_color
        if ambient_ground_color is not None:
            params["ambientGroundColor"] = ambient_ground_color
        if ambient_intensity is not None:
            params["ambientIntensity"] = ambient_intensity

        # Fog settings
        if fog_enabled is not None:
            params["fogEnabled"] = fog_enabled
        if fog_mode is not None:
            params["fogMode"] = fog_mode
        if fog_color is not None:
            params["fogColor"] = fog_color
        if fog_density is not None:
            params["fogDensity"] = fog_density
        if fog_start is not None:
            params["fogStart"] = fog_start
        if fog_end is not None:
            params["fogEnd"] = fog_end

        # Reflection probe
        if probe_resolution is not None:
            params["probeResolution"] = probe_resolution
        if probe_size is not None:
            params["probeSize"] = probe_size
        if probe_center is not None:
            params["probeCenter"] = probe_center
        if probe_hdr is not None:
            params["probeHdr"] = probe_hdr
        if probe_importance is not None:
            params["probeImportance"] = probe_importance

        # Lightmap baking
        if bake_quality is not None:
            params["bakeQuality"] = bake_quality
        if bake_mode is not None:
            params["bakeMode"] = bake_mode

        response = await send_with_unity_instance(
            async_send_command_with_retry,
            unity_instance,
            "manage_lighting",
            params,
        )

        if isinstance(response, dict) and response.get("success"):
            return {
                "success": True,
                "message": response.get("message", f"Lighting {action} successful."),
                "data": response.get("data")
            }
        return response if isinstance(response, dict) else {"success": False, "message": str(response)}

    except Exception as e:
        return {"success": False, "message": f"Error managing lighting: {e!s}"}
