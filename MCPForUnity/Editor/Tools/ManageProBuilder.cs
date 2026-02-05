#if PROBUILDER_ENABLED
using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.ProBuilder;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity ProBuilder meshes:
    /// - Create ProBuilder shapes (Cube, Sphere, Cylinder, Stairs, etc.)
    /// - Edit mesh (extrude, bevel, subdivide)
    /// - Selection operations (vertices, edges, faces)
    /// - Material application
    /// - Export to mesh asset
    /// 
    /// Note: Requires ProBuilder package (com.unity.probuilder) installed.
    /// </summary>
    [McpForUnityTool("manage_probuilder")]
    public static class ManageProBuilder
    {
        public static object HandleCommand(JObject @params)
        {
            if (@params == null)
            {
                return new ErrorResponse("Parameters cannot be null.");
            }

            string action = ParamCoercion.CoerceString(@params["action"], null)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
            {
                return new ErrorResponse("'action' parameter is required.");
            }

            try
            {
                return action switch
                {
                    "pb_create_shape" => CreateShape(@params),
                    "pb_get_info" => GetInfo(@params),
                    "pb_extrude" => Extrude(@params),
                    "pb_bevel" => Bevel(@params),
                    "pb_subdivide" => Subdivide(@params),
                    "pb_merge" => Merge(@params),
                    "pb_set_material" => SetMaterial(@params),
                    "pb_select" => Select(@params),
                    "pb_delete_faces" => DeleteFaces(@params),
                    "pb_flip_normals" => FlipNormals(@params),
                    "pb_export" => Export(@params),
                    "pb_to_mesh" => ToMesh(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'. Supported: pb_create_shape, pb_get_info, pb_extrude, pb_bevel, pb_subdivide, pb_merge, pb_set_material, pb_select, pb_delete_faces, pb_flip_normals, pb_export, pb_to_mesh")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageProBuilder] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error processing action '{action}': {e.Message}");
            }
        }

        #region Shape Creation

        private static object CreateShape(JObject @params)
        {
            string shapeTypeStr = ParamCoercion.CoerceString(@params["shapeType"], "Cube");
            Vector3 size = ParseVector3(@params["size"], Vector3.one * 2);
            Vector3 position = ParseVector3(@params["position"], Vector3.zero);
            Vector3 rotation = ParseVector3(@params["rotation"], Vector3.zero);

            ProBuilderMesh mesh = null;

            switch (shapeTypeStr.ToLowerInvariant())
            {
                case "cube":
                case "box":
                    mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
                    break;
                case "sphere":
                    float radius = ParamCoercion.CoerceFloat(@params["radius"], 1f);
                    int subdivisions = ParamCoercion.CoerceInt(@params["heightSegments"], 2);
                    mesh = ShapeGenerator.GenerateIcosahedron(PivotLocation.Center, radius, subdivisions);
                    break;
                case "cylinder":
                    int axisDivisions = ParamCoercion.CoerceInt(@params["widthSegments"], 24);
                    float cylRadius = ParamCoercion.CoerceFloat(@params["radius"], 0.5f);
                    float cylHeight = ParamCoercion.CoerceFloat(@params["height"], 2f);
                    mesh = ShapeGenerator.GenerateCylinder(PivotLocation.Center, axisDivisions, cylRadius, cylHeight, 1, -1);
                    break;
                case "plane":
                    int widthCuts = ParamCoercion.CoerceInt(@params["widthSegments"], 5);
                    int heightCuts = ParamCoercion.CoerceInt(@params["heightSegments"], 5);
                    mesh = ShapeGenerator.GeneratePlane(PivotLocation.Center, size.x, size.z, widthCuts, heightCuts, Axis.Up);
                    break;
                case "stairs":
                    int stairsCount = ParamCoercion.CoerceInt(@params["stairsCount"], 10);
                    mesh = ShapeGenerator.GenerateStair(PivotLocation.Center, new Vector3(2, 2.5f, 4), stairsCount, true);
                    break;
                case "arch":
                    float archRadius = ParamCoercion.CoerceFloat(@params["radius"], 1f);
                    float archDegrees = ParamCoercion.CoerceFloat(@params["archDegrees"], 180f);
                    mesh = ShapeGenerator.GenerateArch(PivotLocation.Center, archDegrees, archRadius, 0.5f, 1f, 6, true, true, true, true, true);
                    break;
                case "cone":
                    float coneRadius = ParamCoercion.CoerceFloat(@params["radius"], 1f);
                    float coneHeight = ParamCoercion.CoerceFloat(@params["height"], 2f);
                    mesh = ShapeGenerator.GenerateCone(PivotLocation.Center, coneRadius, coneHeight, 16);
                    break;
                case "torus":
                    float torusRadius = ParamCoercion.CoerceFloat(@params["radius"], 1f);
                    float tubeRadius = ParamCoercion.CoerceFloat(@params["innerRadius"], 0.3f);
                    mesh = ShapeGenerator.GenerateTorus(PivotLocation.Center, 24, 12, torusRadius, tubeRadius, true, 360f, 360f, true);
                    break;
                case "prism":
                    mesh = ShapeGenerator.GeneratePrism(PivotLocation.Center, size);
                    break;
                default:
                    return new ErrorResponse($"Unknown shape type: '{shapeTypeStr}'. Supported: Cube, Sphere, Cylinder, Plane, Stairs, Arch, Cone, Torus, Prism");
            }

            if (mesh == null)
            {
                return new ErrorResponse($"Failed to create {shapeTypeStr} shape.");
            }

            mesh.gameObject.name = $"ProBuilder {shapeTypeStr}";
            mesh.transform.position = position;
            mesh.transform.eulerAngles = rotation;

            // Add collider if requested
            if (ParamCoercion.CoerceBool(@params["withCollider"], false))
            {
                mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
            }

            Undo.RegisterCreatedObjectUndo(mesh.gameObject, "Create ProBuilder Shape");
            EditorSceneManager.MarkSceneDirty(mesh.gameObject.scene);

            return new SuccessResponse($"ProBuilder {shapeTypeStr} created.", new
            {
                instanceID = mesh.gameObject.GetInstanceID(),
                name = mesh.gameObject.name,
                shapeType = shapeTypeStr,
                position = new { x = position.x, y = position.y, z = position.z },
                vertexCount = mesh.vertexCount,
                faceCount = mesh.faceCount
            });
        }

        #endregion

        #region Mesh Info

        private static object GetInfo(JObject @params)
        {
            ProBuilderMesh mesh = FindProBuilderMesh(@params);
            if (mesh == null) return MeshNotFoundError(@params);

            return new SuccessResponse($"ProBuilder mesh info for '{mesh.name}'.", new
            {
                instanceID = mesh.gameObject.GetInstanceID(),
                name = mesh.name,
                vertexCount = mesh.vertexCount,
                faceCount = mesh.faceCount,
                edgeCount = mesh.selectedEdgeCount,
                triangleCount = mesh.triangleCount,
                materialCount = mesh.GetComponent<MeshRenderer>()?.sharedMaterials?.Length ?? 0
            });
        }

        #endregion

        #region Mesh Editing

        private static object Extrude(JObject @params)
        {
            ProBuilderMesh mesh = FindProBuilderMesh(@params);
            if (mesh == null) return MeshNotFoundError(@params);

            float distance = ParamCoercion.CoerceFloat(@params["extrudeDistance"], 1f);
            JArray faceIndicesArray = @params["faceIndices"] as JArray;

            Undo.RecordObject(mesh, "Extrude Faces");

            Face[] facesToExtrude;
            if (faceIndicesArray != null && faceIndicesArray.Count > 0)
            {
                var indices = faceIndicesArray.Select(t => (int)t).ToList();
                facesToExtrude = mesh.faces.Where((f, i) => indices.Contains(i)).ToArray();
            }
            else
            {
                facesToExtrude = mesh.faces.ToArray();
            }

            mesh.Extrude(facesToExtrude, ExtrudeMethod.FaceNormal, distance);
            mesh.ToMesh();
            mesh.Refresh();

            EditorUtility.SetDirty(mesh);
            EditorSceneManager.MarkSceneDirty(mesh.gameObject.scene);

            return new SuccessResponse($"Extruded {facesToExtrude.Length} faces by {distance}.", new
            {
                instanceID = mesh.gameObject.GetInstanceID(),
                facesExtruded = facesToExtrude.Length,
                distance
            });
        }

        private static object Bevel(JObject @params)
        {
            ProBuilderMesh mesh = FindProBuilderMesh(@params);
            if (mesh == null) return MeshNotFoundError(@params);

            float amount = ParamCoercion.CoerceFloat(@params["bevelAmount"], 0.1f);

            Undo.RecordObject(mesh, "Bevel Edges");

            var edges = mesh.faces.SelectMany(f => f.edges).Distinct().ToList();
            Bevel.BevelEdges(mesh, edges, amount);

            mesh.ToMesh();
            mesh.Refresh();

            EditorUtility.SetDirty(mesh);
            EditorSceneManager.MarkSceneDirty(mesh.gameObject.scene);

            return new SuccessResponse($"Beveled edges by {amount}.", new
            {
                instanceID = mesh.gameObject.GetInstanceID(),
                amount
            });
        }

        private static object Subdivide(JObject @params)
        {
            ProBuilderMesh mesh = FindProBuilderMesh(@params);
            if (mesh == null) return MeshNotFoundError(@params);

            int count = ParamCoercion.CoerceInt(@params["subdivideCount"], 1);

            Undo.RecordObject(mesh, "Subdivide Mesh");

            for (int i = 0; i < count; i++)
            {
                Subdivision.Subdivide(mesh, mesh.faces);
            }

            mesh.ToMesh();
            mesh.Refresh();

            EditorUtility.SetDirty(mesh);
            EditorSceneManager.MarkSceneDirty(mesh.gameObject.scene);

            return new SuccessResponse($"Mesh subdivided {count} times.", new
            {
                instanceID = mesh.gameObject.GetInstanceID(),
                subdivisions = count,
                newVertexCount = mesh.vertexCount,
                newFaceCount = mesh.faceCount
            });
        }

        private static object Merge(JObject @params)
        {
            JArray targetsArray = @params["mergeTargets"] as JArray;
            if (targetsArray == null || targetsArray.Count < 2)
            {
                return new ErrorResponse("'mergeTargets' must contain at least 2 ProBuilder mesh targets.");
            }

            var meshes = new List<ProBuilderMesh>();
            foreach (var token in targetsArray)
            {
                ProBuilderMesh mesh = FindProBuilderMeshFromToken(token);
                if (mesh != null) meshes.Add(mesh);
            }

            if (meshes.Count < 2)
            {
                return new ErrorResponse("Need at least 2 valid ProBuilder meshes to merge.");
            }

            ProBuilderMesh result = CombineMeshes.Combine(meshes, meshes[0]);

            EditorUtility.SetDirty(result);
            EditorSceneManager.MarkSceneDirty(result.gameObject.scene);

            return new SuccessResponse($"Merged {meshes.Count} meshes.", new
            {
                instanceID = result.gameObject.GetInstanceID(),
                name = result.name,
                vertexCount = result.vertexCount,
                faceCount = result.faceCount
            });
        }

        #endregion

        #region Material

        private static object SetMaterial(JObject @params)
        {
            ProBuilderMesh mesh = FindProBuilderMesh(@params);
            if (mesh == null) return MeshNotFoundError(@params);

            string materialPath = ParamCoercion.CoerceString(@params["materialPath"], null);
            if (string.IsNullOrEmpty(materialPath))
            {
                return new ErrorResponse("'materialPath' parameter is required.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                return new ErrorResponse($"Material not found at path: {materialPath}");
            }

            Undo.RecordObject(mesh, "Set Material");

            int submeshIndex = ParamCoercion.CoerceInt(@params["submeshIndex"], -1);

            var renderer = mesh.GetComponent<MeshRenderer>();
            if (submeshIndex < 0)
            {
                // Apply to all
                renderer.sharedMaterial = material;
            }
            else
            {
                var materials = renderer.sharedMaterials;
                if (submeshIndex < materials.Length)
                {
                    materials[submeshIndex] = material;
                    renderer.sharedMaterials = materials;
                }
            }

            EditorUtility.SetDirty(mesh);
            EditorSceneManager.MarkSceneDirty(mesh.gameObject.scene);

            return new SuccessResponse($"Material applied to '{mesh.name}'.", new
            {
                instanceID = mesh.gameObject.GetInstanceID(),
                materialPath,
                submeshIndex
            });
        }

        #endregion

        #region Selection and Delete

        private static object Select(JObject @params)
        {
            ProBuilderMesh mesh = FindProBuilderMesh(@params);
            if (mesh == null) return MeshNotFoundError(@params);

            string selectMode = ParamCoercion.CoerceString(@params["selectMode"], "Face");

            // Set ProBuilder selection mode
            switch (selectMode.ToLowerInvariant())
            {
                case "vertex":
                    ProBuilderEditor.selectMode = SelectMode.Vertex;
                    break;
                case "edge":
                    ProBuilderEditor.selectMode = SelectMode.Edge;
                    break;
                case "face":
                    ProBuilderEditor.selectMode = SelectMode.Face;
                    break;
                case "object":
                    ProBuilderEditor.selectMode = SelectMode.Object;
                    break;
            }

            // Select the mesh object
            Selection.activeGameObject = mesh.gameObject;

            return new SuccessResponse($"Selection mode set to {selectMode}.", new
            {
                instanceID = mesh.gameObject.GetInstanceID(),
                selectMode
            });
        }

        private static object DeleteFaces(JObject @params)
        {
            ProBuilderMesh mesh = FindProBuilderMesh(@params);
            if (mesh == null) return MeshNotFoundError(@params);

            JArray faceIndicesArray = @params["faceIndices"] as JArray;
            if (faceIndicesArray == null || faceIndicesArray.Count == 0)
            {
                return new ErrorResponse("'faceIndices' parameter is required.");
            }

            Undo.RecordObject(mesh, "Delete Faces");

            var indices = faceIndicesArray.Select(t => (int)t).ToList();
            var facesToDelete = mesh.faces.Where((f, i) => indices.Contains(i)).ToArray();

            mesh.DeleteFaces(facesToDelete);
            mesh.ToMesh();
            mesh.Refresh();

            EditorUtility.SetDirty(mesh);
            EditorSceneManager.MarkSceneDirty(mesh.gameObject.scene);

            return new SuccessResponse($"Deleted {facesToDelete.Length} faces.", new
            {
                instanceID = mesh.gameObject.GetInstanceID(),
                facesDeleted = facesToDelete.Length
            });
        }

        private static object FlipNormals(JObject @params)
        {
            ProBuilderMesh mesh = FindProBuilderMesh(@params);
            if (mesh == null) return MeshNotFoundError(@params);

            Undo.RecordObject(mesh, "Flip Normals");

            foreach (var face in mesh.faces)
            {
                face.Reverse();
            }

            mesh.ToMesh();
            mesh.Refresh();

            EditorUtility.SetDirty(mesh);
            EditorSceneManager.MarkSceneDirty(mesh.gameObject.scene);

            return new SuccessResponse($"Flipped normals on '{mesh.name}'.", new
            {
                instanceID = mesh.gameObject.GetInstanceID(),
                faceCount = mesh.faceCount
            });
        }

        #endregion

        #region Export

        private static object Export(JObject @params)
        {
            ProBuilderMesh mesh = FindProBuilderMesh(@params);
            if (mesh == null) return MeshNotFoundError(@params);

            string exportPath = ParamCoercion.CoerceString(@params["exportPath"], null);
            if (string.IsNullOrEmpty(exportPath))
            {
                exportPath = $"Assets/{mesh.name}.asset";
            }

            string format = ParamCoercion.CoerceString(@params["exportFormat"], "Asset")?.ToLowerInvariant();

            // Export to Unity mesh asset
            Mesh exportedMesh = new Mesh();
            exportedMesh.name = mesh.name;
            
            MeshFilter mf = mesh.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                exportedMesh.vertices = mf.sharedMesh.vertices;
                exportedMesh.triangles = mf.sharedMesh.triangles;
                exportedMesh.normals = mf.sharedMesh.normals;
                exportedMesh.uv = mf.sharedMesh.uv;
            }

            AssetDatabase.CreateAsset(exportedMesh, exportPath);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"Mesh exported to '{exportPath}'.", new
            {
                instanceID = mesh.gameObject.GetInstanceID(),
                exportPath,
                format,
                vertexCount = exportedMesh.vertexCount
            });
        }

        private static object ToMesh(JObject @params)
        {
            ProBuilderMesh pbMesh = FindProBuilderMesh(@params);
            if (pbMesh == null) return MeshNotFoundError(@params);

            Undo.RecordObject(pbMesh.gameObject, "Convert to Mesh");

            // Get current mesh data
            MeshFilter mf = pbMesh.GetComponent<MeshFilter>();
            Mesh mesh = mf?.sharedMesh;

            if (mesh != null)
            {
                // Create a copy of the mesh
                Mesh newMesh = UnityEngine.Object.Instantiate(mesh);
                newMesh.name = pbMesh.name + "_Mesh";

                // Remove ProBuilderMesh component
                UnityEngine.Object.DestroyImmediate(pbMesh);

                // Update mesh filter
                mf.sharedMesh = newMesh;
            }

            return new SuccessResponse($"Converted to regular mesh.", new
            {
                name = mf?.gameObject.name
            });
        }

        #endregion

        #region Helpers

        private static ProBuilderMesh FindProBuilderMesh(JObject @params)
        {
            JToken targetToken = @params["target"];
            if (targetToken == null) return null;
            return FindProBuilderMeshFromToken(targetToken);
        }

        private static ProBuilderMesh FindProBuilderMeshFromToken(JToken targetToken)
        {
            if (targetToken == null) return null;

            if (targetToken.Type == JTokenType.Integer)
            {
                int instanceId = targetToken.Value<int>();
                GameObject go = GameObjectLookup.FindById(instanceId);
                return go?.GetComponent<ProBuilderMesh>();
            }

            string targetStr = targetToken.ToString();
            if (int.TryParse(targetStr, out int parsedId))
            {
                GameObject go = GameObjectLookup.FindById(parsedId);
                if (go != null) return go.GetComponent<ProBuilderMesh>();
            }

            GameObject found = GameObjectLookup.FindByTarget(targetToken, "by_name", true);
            return found?.GetComponent<ProBuilderMesh>();
        }

        private static object MeshNotFoundError(JObject @params)
        {
            return new ErrorResponse($"ProBuilder mesh not found: '{@params["target"]}'. Make sure the target has a ProBuilderMesh component.");
        }

        private static Vector3 ParseVector3(JToken token, Vector3 defaultValue)
        {
            if (token == null) return defaultValue;

            if (token is JArray arr && arr.Count >= 3)
            {
                return new Vector3(
                    ParamCoercion.CoerceFloat(arr[0], defaultValue.x),
                    ParamCoercion.CoerceFloat(arr[1], defaultValue.y),
                    ParamCoercion.CoerceFloat(arr[2], defaultValue.z)
                );
            }

            if (token is JObject obj)
            {
                return new Vector3(
                    ParamCoercion.CoerceFloat(obj["x"], defaultValue.x),
                    ParamCoercion.CoerceFloat(obj["y"], defaultValue.y),
                    ParamCoercion.CoerceFloat(obj["z"], defaultValue.z)
                );
            }

            return defaultValue;
        }

        #endregion
    }
}
#else
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Stub implementation when ProBuilder is not installed.
    /// </summary>
    [McpForUnityTool("manage_probuilder")]
    public static class ManageProBuilder
    {
        public static object HandleCommand(JObject @params)
        {
            return new ErrorResponse(
                "ProBuilder is not installed. Install 'com.unity.probuilder' from Package Manager to use this tool. " +
                "After installing, add 'PROBUILDER_ENABLED' to Scripting Define Symbols in Project Settings > Player."
            );
        }
    }
}
#endif
