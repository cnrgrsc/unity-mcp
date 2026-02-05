"""
Tool for managing ProBuilder in Unity.
Supports 3D mesh creation, editing, and manipulation.
Note: Requires ProBuilder package installed in Unity project.
"""
from typing import Annotated, Any, Literal

from fastmcp import Context
from services.registry import mcp_for_unity_tool
from services.tools import get_unity_instance_from_context
from transport.unity_transport import send_with_unity_instance
from transport.legacy.unity_connection import async_send_command_with_retry
from services.tools.preflight import preflight


@mcp_for_unity_tool(
    description="Manages Unity ProBuilder - create and edit 3D meshes. Requires ProBuilder package. Actions: pb_create_shape, pb_extrude, pb_bevel, pb_subdivide, pb_merge, pb_set_material, pb_select, pb_delete_faces, pb_flip_normals, pb_export, pb_get_info."
)
async def manage_probuilder(
    ctx: Context,
    action: Annotated[
        Literal[
            "pb_create_shape", "pb_get_info",
            "pb_extrude", "pb_bevel", "pb_subdivide",
            "pb_merge", "pb_set_material",
            "pb_select", "pb_delete_faces", "pb_flip_normals",
            "pb_export", "pb_to_mesh"
        ],
        "Action to perform on ProBuilder mesh"
    ],
    target: Annotated[
        str | int,
        "Target ProBuilder mesh GameObject - instance ID or name/path"
    ] | None = None,
    # Shape creation
    shape_type: Annotated[
        Literal["Cube", "Sphere", "Cylinder", "Plane", "Stairs", "Arch", "Door", "Pipe", "Cone", "Prism", "Torus"],
        "Type of shape to create"
    ] | None = None,
    size: Annotated[list[float] | dict, "Shape size [x, y, z]"] | None = None,
    position: Annotated[list[float] | dict, "Shape position [x, y, z]"] | None = None,
    rotation: Annotated[list[float] | dict, "Shape rotation [x, y, z]"] | None = None,
    # Shape-specific parameters
    radius: Annotated[float, "Radius for curved shapes"] | None = None,
    height: Annotated[float, "Height for shapes like cylinder, stairs"] | None = None,
    width_segments: Annotated[int, "Width segment count"] | None = None,
    height_segments: Annotated[int, "Height segment count"] | None = None,
    depth_segments: Annotated[int, "Depth segment count"] | None = None,
    stairs_count: Annotated[int, "Number of stairs"] | None = None,
    arch_degrees: Annotated[float, "Arch angle in degrees"] | None = None,
    inner_radius: Annotated[float, "Inner radius for pipe/torus"] | None = None,
    # Mesh editing
    extrude_distance: Annotated[float, "Distance to extrude faces"] | None = None,
    bevel_amount: Annotated[float, "Bevel amount"] | None = None,
    subdivide_count: Annotated[int, "Number of subdivisions"] | None = None,
    # Selection
    select_mode: Annotated[
        Literal["Vertex", "Edge", "Face", "Object"],
        "Selection mode"
    ] | None = None,
    face_indices: Annotated[list[int], "Face indices to select/operate on"] | None = None,
    edge_indices: Annotated[list[int], "Edge indices to select/operate on"] | None = None,
    vertex_indices: Annotated[list[int], "Vertex indices to select/operate on"] | None = None,
    # Materials
    material_path: Annotated[str, "Path to material asset"] | None = None,
    submesh_index: Annotated[int, "Submesh index to apply material to"] | None = None,
    # Merge
    merge_targets: Annotated[list[str | int], "List of ProBuilder objects to merge"] | None = None,
    # Export
    export_path: Annotated[str, "Path to export mesh asset"] | None = None,
    export_format: Annotated[
        Literal["Asset", "OBJ", "STL"],
        "Export format"
    ] | None = None,
    # Advanced
    smooth_angle: Annotated[float, "Smoothing angle for normals"] | None = None,
    with_collider: Annotated[bool, "Add MeshCollider after creation"] | None = None,
) -> dict[str, Any]:
    """
    Manage Unity ProBuilder meshes.

    Actions:
    - pb_create_shape: Create a ProBuilder primitive shape
    - pb_get_info: Get mesh information (vertices, faces, materials)
    - pb_extrude: Extrude selected faces
    - pb_bevel: Bevel edges
    - pb_subdivide: Subdivide mesh
    - pb_merge: Merge multiple ProBuilder objects
    - pb_set_material: Apply material to faces
    - pb_select: Select vertices/edges/faces
    - pb_delete_faces: Delete selected faces
    - pb_flip_normals: Flip face normals
    - pb_export: Export to mesh asset
    - pb_to_mesh: Convert to regular mesh

    Examples:
    - Create cube: action="pb_create_shape", shape_type="Cube", size=[2,2,2]
    - Extrude: action="pb_extrude", target="MyMesh", face_indices=[0,1], extrude_distance=1.5
    - Export: action="pb_export", target="MyMesh", export_path="Assets/Meshes/MyMesh.asset"

    Note: Requires ProBuilder package (com.unity.probuilder) installed in Unity project.
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

    # pb_create_shape and pb_merge don't require target
    if action not in ["pb_create_shape", "pb_merge"] and not target:
        return {
            "success": False,
            "message": "Missing required parameter 'target'. Specify ProBuilder mesh GameObject."
        }

    try:
        params: dict[str, Any] = {"action": action}
        
        if target is not None:
            params["target"] = target

        # Shape creation
        if shape_type is not None:
            params["shapeType"] = shape_type
        if size is not None:
            params["size"] = size
        if position is not None:
            params["position"] = position
        if rotation is not None:
            params["rotation"] = rotation

        # Shape-specific parameters
        if radius is not None:
            params["radius"] = radius
        if height is not None:
            params["height"] = height
        if width_segments is not None:
            params["widthSegments"] = width_segments
        if height_segments is not None:
            params["heightSegments"] = height_segments
        if depth_segments is not None:
            params["depthSegments"] = depth_segments
        if stairs_count is not None:
            params["stairsCount"] = stairs_count
        if arch_degrees is not None:
            params["archDegrees"] = arch_degrees
        if inner_radius is not None:
            params["innerRadius"] = inner_radius

        # Mesh editing
        if extrude_distance is not None:
            params["extrudeDistance"] = extrude_distance
        if bevel_amount is not None:
            params["bevelAmount"] = bevel_amount
        if subdivide_count is not None:
            params["subdivideCount"] = subdivide_count

        # Selection
        if select_mode is not None:
            params["selectMode"] = select_mode
        if face_indices is not None:
            params["faceIndices"] = face_indices
        if edge_indices is not None:
            params["edgeIndices"] = edge_indices
        if vertex_indices is not None:
            params["vertexIndices"] = vertex_indices

        # Materials
        if material_path is not None:
            params["materialPath"] = material_path
        if submesh_index is not None:
            params["submeshIndex"] = submesh_index

        # Merge
        if merge_targets is not None:
            params["mergeTargets"] = merge_targets

        # Export
        if export_path is not None:
            params["exportPath"] = export_path
        if export_format is not None:
            params["exportFormat"] = export_format

        # Advanced
        if smooth_angle is not None:
            params["smoothAngle"] = smooth_angle
        if with_collider is not None:
            params["withCollider"] = with_collider

        response = await send_with_unity_instance(
            async_send_command_with_retry,
            unity_instance,
            "manage_probuilder",
            params,
        )

        if isinstance(response, dict) and response.get("success"):
            return {
                "success": True,
                "message": response.get("message", f"ProBuilder {action} successful."),
                "data": response.get("data")
            }
        return response if isinstance(response, dict) else {"success": False, "message": str(response)}

    except Exception as e:
        return {"success": False, "message": f"Error managing ProBuilder: {e!s}"}
