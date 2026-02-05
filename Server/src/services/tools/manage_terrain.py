"""
Tool for managing Terrain in Unity.
Supports terrain creation, height manipulation, texture painting, and foliage.
"""
from typing import Annotated, Any, Literal

from fastmcp import Context
from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry
from services.tools.preflight import preflight


@mcp_for_unity_tool(
    description="Manages Unity Terrain - create terrains, modify height, paint textures, add trees and details. Actions: terrain_create, terrain_get_info, terrain_set_height, terrain_set_heights, terrain_smooth, terrain_flatten, terrain_add_texture, terrain_paint_texture, terrain_add_tree, terrain_add_detail, terrain_set_settings."
)
async def manage_terrain(
    ctx: Context,
    action: Annotated[
        Literal[
            "terrain_create", "terrain_get_info",
            "terrain_set_height", "terrain_set_heights", "terrain_smooth", "terrain_flatten",
            "terrain_add_texture", "terrain_paint_texture",
            "terrain_add_tree", "terrain_add_detail",
            "terrain_set_settings"
        ],
        "Action to perform on terrain"
    ],
    target: Annotated[
        str | int,
        "Target Terrain GameObject - instance ID or name/path"
    ] | None = None,
    # Terrain creation
    width: Annotated[int, "Terrain width in units (default: 500)"] | None = None,
    length: Annotated[int, "Terrain length in units (default: 500)"] | None = None,
    height: Annotated[int, "Terrain max height (default: 600)"] | None = None,
    heightmap_resolution: Annotated[int, "Heightmap resolution (power of 2 + 1, e.g., 513)"] | None = None,
    detail_resolution: Annotated[int, "Detail resolution (default: 1024)"] | None = None,
    position: Annotated[list[float] | dict, "Terrain position [x, y, z]"] | None = None,
    # Height modification
    x: Annotated[int, "X coordinate on terrain"] | None = None,
    z: Annotated[int, "Z coordinate on terrain"] | None = None,
    value: Annotated[float, "Height value (0-1 normalized)"] | None = None,
    heights: Annotated[list[list[float]], "2D array of heights for bulk modification"] | None = None,
    radius: Annotated[int, "Brush radius for smooth/flatten operations"] | None = None,
    strength: Annotated[float, "Brush strength (0-1)"] | None = None,
    # Texture painting
    texture_path: Annotated[str, "Path to texture asset"] | None = None,
    normal_path: Annotated[str, "Path to normal map texture"] | None = None,
    tile_size: Annotated[list[float], "Texture tile size [x, y]"] | None = None,
    tile_offset: Annotated[list[float], "Texture tile offset [x, y]"] | None = None,
    layer_index: Annotated[int, "Terrain layer index to paint"] | None = None,
    opacity: Annotated[float, "Paint opacity (0-1)"] | None = None,
    # Tree/Detail placement
    prefab_path: Annotated[str, "Path to tree/detail prefab"] | None = None,
    density: Annotated[float, "Density for tree/detail placement"] | None = None,
    min_height: Annotated[float, "Minimum height for placement"] | None = None,
    max_height: Annotated[float, "Maximum height for placement"] | None = None,
    min_slope: Annotated[float, "Minimum slope for placement"] | None = None,
    max_slope: Annotated[float, "Maximum slope for placement"] | None = None,
    # Settings
    draw_trees: Annotated[bool, "Enable tree rendering"] | None = None,
    draw_details: Annotated[bool, "Enable detail/grass rendering"] | None = None,
    pixel_error: Annotated[float, "Terrain pixel error for LOD"] | None = None,
    base_map_distance: Annotated[float, "Base map render distance"] | None = None,
) -> dict[str, Any]:
    """
    Manage Unity Terrain.

    Actions:
    - terrain_create: Create a new terrain
    - terrain_get_info: Get terrain information
    - terrain_set_height: Set height at a specific point
    - terrain_set_heights: Set multiple heights at once
    - terrain_smooth: Smooth terrain area
    - terrain_flatten: Flatten terrain area
    - terrain_add_texture: Add a terrain texture layer
    - terrain_paint_texture: Paint texture on terrain
    - terrain_add_tree: Add tree prototype and place trees
    - terrain_add_detail: Add grass/detail mesh
    - terrain_set_settings: Configure terrain render settings

    Examples:
    - Create terrain: action="terrain_create", width=1000, length=1000, height=300
    - Set height: action="terrain_set_height", target="Terrain", x=100, z=100, value=0.5
    - Paint texture: action="terrain_paint_texture", target="Terrain", layer_index=0, x=50, z=50, radius=10
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

    # terrain_create doesn't require target
    if action != "terrain_create" and not target:
        return {
            "success": False,
            "message": "Missing required parameter 'target'. Specify Terrain GameObject."
        }

    try:
        params: dict[str, Any] = {"action": action}
        
        if target is not None:
            params["target"] = target

        # Terrain creation
        if width is not None:
            params["width"] = width
        if length is not None:
            params["length"] = length
        if height is not None:
            params["height"] = height
        if heightmap_resolution is not None:
            params["heightmapResolution"] = heightmap_resolution
        if detail_resolution is not None:
            params["detailResolution"] = detail_resolution
        if position is not None:
            params["position"] = position

        # Height modification
        if x is not None:
            params["x"] = x
        if z is not None:
            params["z"] = z
        if value is not None:
            params["value"] = value
        if heights is not None:
            params["heights"] = heights
        if radius is not None:
            params["radius"] = radius
        if strength is not None:
            params["strength"] = strength

        # Texture painting
        if texture_path is not None:
            params["texturePath"] = texture_path
        if normal_path is not None:
            params["normalPath"] = normal_path
        if tile_size is not None:
            params["tileSize"] = tile_size
        if tile_offset is not None:
            params["tileOffset"] = tile_offset
        if layer_index is not None:
            params["layerIndex"] = layer_index
        if opacity is not None:
            params["opacity"] = opacity

        # Tree/Detail placement
        if prefab_path is not None:
            params["prefabPath"] = prefab_path
        if density is not None:
            params["density"] = density
        if min_height is not None:
            params["minHeight"] = min_height
        if max_height is not None:
            params["maxHeight"] = max_height
        if min_slope is not None:
            params["minSlope"] = min_slope
        if max_slope is not None:
            params["maxSlope"] = max_slope

        # Settings
        if draw_trees is not None:
            params["drawTrees"] = draw_trees
        if draw_details is not None:
            params["drawDetails"] = draw_details
        if pixel_error is not None:
            params["pixelError"] = pixel_error
        if base_map_distance is not None:
            params["baseMapDistance"] = base_map_distance

        response = await send_with_unity_instance(
            async_send_command_with_retry,
            unity_instance,
            "manage_terrain",
            params,
        )

        if isinstance(response, dict) and response.get("success"):
            return {
                "success": True,
                "message": response.get("message", f"Terrain {action} successful."),
                "data": response.get("data")
            }
        return response if isinstance(response, dict) else {"success": False, "message": str(response)}

    except Exception as e:
        return {"success": False, "message": f"Error managing terrain: {e!s}"}
