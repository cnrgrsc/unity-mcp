using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Terrain:
    /// - Create terrains with customizable size and resolution
    /// - Modify terrain height (single point, bulk, smooth, flatten)
    /// - Paint textures and add terrain layers
    /// - Add trees and details (grass, foliage)
    /// - Configure terrain rendering settings
    /// </summary>
    [McpForUnityTool("manage_terrain")]
    public static class ManageTerrain
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
                    "terrain_create" => TerrainCreate(@params),
                    "terrain_get_info" => TerrainGetInfo(@params),
                    "terrain_set_height" => TerrainSetHeight(@params),
                    "terrain_set_heights" => TerrainSetHeights(@params),
                    "terrain_smooth" => TerrainSmooth(@params),
                    "terrain_flatten" => TerrainFlatten(@params),
                    "terrain_add_texture" => TerrainAddTexture(@params),
                    "terrain_paint_texture" => TerrainPaintTexture(@params),
                    "terrain_add_tree" => TerrainAddTree(@params),
                    "terrain_add_detail" => TerrainAddDetail(@params),
                    "terrain_set_settings" => TerrainSetSettings(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'. Supported: terrain_create, terrain_get_info, terrain_set_height, terrain_set_heights, terrain_smooth, terrain_flatten, terrain_add_texture, terrain_paint_texture, terrain_add_tree, terrain_add_detail, terrain_set_settings")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageTerrain] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error processing action '{action}': {e.Message}");
            }
        }

        #region Terrain Creation

        private static object TerrainCreate(JObject @params)
        {
            int width = ParamCoercion.CoerceInt(@params["width"], 500);
            int length = ParamCoercion.CoerceInt(@params["length"], 500);
            int height = ParamCoercion.CoerceInt(@params["height"], 600);
            int heightmapRes = ParamCoercion.CoerceInt(@params["heightmapResolution"], 513);
            int detailRes = ParamCoercion.CoerceInt(@params["detailResolution"], 1024);
            Vector3 position = ParseVector3(@params["position"], Vector3.zero);

            // Create terrain data
            TerrainData terrainData = new TerrainData
            {
                heightmapResolution = heightmapRes,
                size = new Vector3(width, height, length)
            };
            terrainData.SetDetailResolution(detailRes, 16);

            // Save terrain data as asset
            string assetPath = AssetDatabase.GenerateUniqueAssetPath("Assets/TerrainData.asset");
            AssetDatabase.CreateAsset(terrainData, assetPath);
            AssetDatabase.SaveAssets();

            // Create terrain game object
            GameObject terrainGo = Terrain.CreateTerrainGameObject(terrainData);
            terrainGo.transform.position = position;
            
            Undo.RegisterCreatedObjectUndo(terrainGo, "Create Terrain");
            EditorSceneManager.MarkSceneDirty(terrainGo.scene);

            return new SuccessResponse($"Terrain created at position ({position.x}, {position.y}, {position.z}).", new
            {
                instanceID = terrainGo.GetInstanceID(),
                name = terrainGo.name,
                size = new { width, height, length },
                heightmapResolution = heightmapRes,
                assetPath
            });
        }

        private static object TerrainGetInfo(JObject @params)
        {
            Terrain terrain = FindTerrain(@params);
            if (terrain == null) return TerrainNotFoundError(@params);

            TerrainData data = terrain.terrainData;

            return new SuccessResponse($"Terrain info for '{terrain.name}'.", new
            {
                instanceID = terrain.gameObject.GetInstanceID(),
                name = terrain.name,
                size = new { x = data.size.x, y = data.size.y, z = data.size.z },
                heightmapResolution = data.heightmapResolution,
                detailResolution = data.detailResolution,
                alphamapResolution = data.alphamapResolution,
                textureLayerCount = data.terrainLayers?.Length ?? 0,
                treePrototypeCount = data.treePrototypes?.Length ?? 0,
                detailPrototypeCount = data.detailPrototypes?.Length ?? 0
            });
        }

        #endregion

        #region Height Modification

        private static object TerrainSetHeight(JObject @params)
        {
            Terrain terrain = FindTerrain(@params);
            if (terrain == null) return TerrainNotFoundError(@params);

            int x = ParamCoercion.CoerceInt(@params["x"], 0);
            int z = ParamCoercion.CoerceInt(@params["z"], 0);
            float value = ParamCoercion.CoerceFloat(@params["value"], 0.5f);

            TerrainData data = terrain.terrainData;
            
            // Clamp to valid range
            x = Mathf.Clamp(x, 0, data.heightmapResolution - 1);
            z = Mathf.Clamp(z, 0, data.heightmapResolution - 1);
            value = Mathf.Clamp01(value);

            Undo.RecordObject(data, "Set Terrain Height");
            
            float[,] heights = new float[1, 1] { { value } };
            data.SetHeights(x, z, heights);

            EditorUtility.SetDirty(data);

            return new SuccessResponse($"Height set at ({x}, {z}) to {value}.", new
            {
                instanceID = terrain.gameObject.GetInstanceID(),
                x, z, value
            });
        }

        private static object TerrainSetHeights(JObject @params)
        {
            Terrain terrain = FindTerrain(@params);
            if (terrain == null) return TerrainNotFoundError(@params);

            int startX = ParamCoercion.CoerceInt(@params["x"], 0);
            int startZ = ParamCoercion.CoerceInt(@params["z"], 0);
            JArray heightsArray = @params["heights"] as JArray;

            if (heightsArray == null || heightsArray.Count == 0)
            {
                return new ErrorResponse("'heights' parameter is required and must be a 2D array.");
            }

            TerrainData data = terrain.terrainData;
            Undo.RecordObject(data, "Set Terrain Heights");

            int rows = heightsArray.Count;
            int cols = (heightsArray[0] as JArray)?.Count ?? 0;
            float[,] heights = new float[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                JArray row = heightsArray[i] as JArray;
                if (row != null)
                {
                    for (int j = 0; j < Mathf.Min(cols, row.Count); j++)
                    {
                        heights[i, j] = Mathf.Clamp01(ParamCoercion.CoerceFloat(row[j], 0));
                    }
                }
            }

            data.SetHeights(startX, startZ, heights);
            EditorUtility.SetDirty(data);

            return new SuccessResponse($"Heights set: {rows}x{cols} area starting at ({startX}, {startZ}).", new
            {
                instanceID = terrain.gameObject.GetInstanceID(),
                startX, startZ, rows, cols
            });
        }

        private static object TerrainSmooth(JObject @params)
        {
            Terrain terrain = FindTerrain(@params);
            if (terrain == null) return TerrainNotFoundError(@params);

            int centerX = ParamCoercion.CoerceInt(@params["x"], 0);
            int centerZ = ParamCoercion.CoerceInt(@params["z"], 0);
            int radius = ParamCoercion.CoerceInt(@params["radius"], 5);
            float strength = ParamCoercion.CoerceFloat(@params["strength"], 0.5f);

            TerrainData data = terrain.terrainData;
            Undo.RecordObject(data, "Smooth Terrain");

            int startX = Mathf.Max(0, centerX - radius);
            int startZ = Mathf.Max(0, centerZ - radius);
            int endX = Mathf.Min(data.heightmapResolution - 1, centerX + radius);
            int endZ = Mathf.Min(data.heightmapResolution - 1, centerZ + radius);

            int width = endX - startX + 1;
            int height = endZ - startZ + 1;

            float[,] heights = data.GetHeights(startX, startZ, width, height);
            float[,] smoothed = new float[height, width];

            // Simple box blur smoothing
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    float sum = 0;
                    int count = 0;

                    for (int dz = -1; dz <= 1; dz++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx >= 0 && nx < width && nz >= 0 && nz < height)
                            {
                                sum += heights[nz, nx];
                                count++;
                            }
                        }
                    }

                    float avg = sum / count;
                    smoothed[z, x] = Mathf.Lerp(heights[z, x], avg, strength);
                }
            }

            data.SetHeights(startX, startZ, smoothed);
            EditorUtility.SetDirty(data);

            return new SuccessResponse($"Terrain smoothed at ({centerX}, {centerZ}) with radius {radius}.", new
            {
                instanceID = terrain.gameObject.GetInstanceID(),
                centerX, centerZ, radius, strength
            });
        }

        private static object TerrainFlatten(JObject @params)
        {
            Terrain terrain = FindTerrain(@params);
            if (terrain == null) return TerrainNotFoundError(@params);

            int centerX = ParamCoercion.CoerceInt(@params["x"], 0);
            int centerZ = ParamCoercion.CoerceInt(@params["z"], 0);
            int radius = ParamCoercion.CoerceInt(@params["radius"], 5);
            float targetHeight = ParamCoercion.CoerceFloat(@params["value"], -1);

            TerrainData data = terrain.terrainData;
            Undo.RecordObject(data, "Flatten Terrain");

            int startX = Mathf.Max(0, centerX - radius);
            int startZ = Mathf.Max(0, centerZ - radius);
            int endX = Mathf.Min(data.heightmapResolution - 1, centerX + radius);
            int endZ = Mathf.Min(data.heightmapResolution - 1, centerZ + radius);

            int width = endX - startX + 1;
            int height = endZ - startZ + 1;

            // If no target height specified, use center point height
            if (targetHeight < 0)
            {
                float[,] centerHeights = data.GetHeights(centerX, centerZ, 1, 1);
                targetHeight = centerHeights[0, 0];
            }

            float[,] heights = new float[height, width];
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    heights[z, x] = targetHeight;
                }
            }

            data.SetHeights(startX, startZ, heights);
            EditorUtility.SetDirty(data);

            return new SuccessResponse($"Terrain flattened at ({centerX}, {centerZ}) to height {targetHeight}.", new
            {
                instanceID = terrain.gameObject.GetInstanceID(),
                centerX, centerZ, radius, targetHeight
            });
        }

        #endregion

        #region Texture Painting

        private static object TerrainAddTexture(JObject @params)
        {
            Terrain terrain = FindTerrain(@params);
            if (terrain == null) return TerrainNotFoundError(@params);

            string texturePath = ParamCoercion.CoerceString(@params["texturePath"], null);
            if (string.IsNullOrEmpty(texturePath))
            {
                return new ErrorResponse("'texturePath' parameter is required.");
            }

            Texture2D diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (diffuse == null)
            {
                return new ErrorResponse($"Texture not found at path: {texturePath}");
            }

            string normalPath = ParamCoercion.CoerceString(@params["normalPath"], null);
            Texture2D normal = string.IsNullOrEmpty(normalPath) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            Vector2 tileSize = ParseVector2(@params["tileSize"], new Vector2(15, 15));
            Vector2 tileOffset = ParseVector2(@params["tileOffset"], Vector2.zero);

            TerrainData data = terrain.terrainData;
            Undo.RecordObject(data, "Add Terrain Texture");

            TerrainLayer layer = new TerrainLayer
            {
                diffuseTexture = diffuse,
                normalMapTexture = normal,
                tileSize = tileSize,
                tileOffset = tileOffset
            };

            // Save layer as asset
            string layerPath = AssetDatabase.GenerateUniqueAssetPath("Assets/TerrainLayer.terrainlayer");
            AssetDatabase.CreateAsset(layer, layerPath);

            // Add to terrain
            TerrainLayer[] layers = data.terrainLayers ?? new TerrainLayer[0];
            Array.Resize(ref layers, layers.Length + 1);
            layers[layers.Length - 1] = layer;
            data.terrainLayers = layers;

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(data);

            return new SuccessResponse($"Texture layer added to terrain.", new
            {
                instanceID = terrain.gameObject.GetInstanceID(),
                layerIndex = layers.Length - 1,
                texturePath,
                layerAssetPath = layerPath
            });
        }

        private static object TerrainPaintTexture(JObject @params)
        {
            Terrain terrain = FindTerrain(@params);
            if (terrain == null) return TerrainNotFoundError(@params);

            int layerIndex = ParamCoercion.CoerceInt(@params["layerIndex"], 0);
            int centerX = ParamCoercion.CoerceInt(@params["x"], 0);
            int centerZ = ParamCoercion.CoerceInt(@params["z"], 0);
            int radius = ParamCoercion.CoerceInt(@params["radius"], 5);
            float opacity = ParamCoercion.CoerceFloat(@params["opacity"], 1f);

            TerrainData data = terrain.terrainData;
            
            if (data.terrainLayers == null || layerIndex >= data.terrainLayers.Length)
            {
                return new ErrorResponse($"Invalid layer index: {layerIndex}. Terrain has {data.terrainLayers?.Length ?? 0} layers.");
            }

            Undo.RecordObject(data, "Paint Terrain Texture");

            int alphamapRes = data.alphamapResolution;
            float scaleX = (float)alphamapRes / data.heightmapResolution;
            float scaleZ = (float)alphamapRes / data.heightmapResolution;

            int aX = Mathf.RoundToInt(centerX * scaleX);
            int aZ = Mathf.RoundToInt(centerZ * scaleZ);
            int aRadius = Mathf.RoundToInt(radius * scaleX);

            int startX = Mathf.Max(0, aX - aRadius);
            int startZ = Mathf.Max(0, aZ - aRadius);
            int endX = Mathf.Min(alphamapRes - 1, aX + aRadius);
            int endZ = Mathf.Min(alphamapRes - 1, aZ + aRadius);

            int width = endX - startX + 1;
            int height = endZ - startZ + 1;
            int layerCount = data.terrainLayers.Length;

            float[,,] alphamaps = data.GetAlphamaps(startX, startZ, width, height);

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Apply paint with opacity
                    for (int l = 0; l < layerCount; l++)
                    {
                        if (l == layerIndex)
                        {
                            alphamaps[z, x, l] = Mathf.Lerp(alphamaps[z, x, l], 1f, opacity);
                        }
                        else
                        {
                            alphamaps[z, x, l] = Mathf.Lerp(alphamaps[z, x, l], 0f, opacity);
                        }
                    }

                    // Normalize
                    float sum = 0;
                    for (int l = 0; l < layerCount; l++) sum += alphamaps[z, x, l];
                    if (sum > 0)
                    {
                        for (int l = 0; l < layerCount; l++) alphamaps[z, x, l] /= sum;
                    }
                }
            }

            data.SetAlphamaps(startX, startZ, alphamaps);
            EditorUtility.SetDirty(data);

            return new SuccessResponse($"Texture painted at ({centerX}, {centerZ}) with layer {layerIndex}.", new
            {
                instanceID = terrain.gameObject.GetInstanceID(),
                layerIndex, centerX, centerZ, radius, opacity
            });
        }

        #endregion

        #region Trees and Details

        private static object TerrainAddTree(JObject @params)
        {
            Terrain terrain = FindTerrain(@params);
            if (terrain == null) return TerrainNotFoundError(@params);

            string prefabPath = ParamCoercion.CoerceString(@params["prefabPath"], null);
            if (string.IsNullOrEmpty(prefabPath))
            {
                return new ErrorResponse("'prefabPath' parameter is required.");
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                return new ErrorResponse($"Tree prefab not found at path: {prefabPath}");
            }

            TerrainData data = terrain.terrainData;
            Undo.RecordObject(data, "Add Tree Prototype");

            TreePrototype prototype = new TreePrototype { prefab = prefab };

            TreePrototype[] prototypes = data.treePrototypes ?? new TreePrototype[0];
            Array.Resize(ref prototypes, prototypes.Length + 1);
            prototypes[prototypes.Length - 1] = prototype;
            data.treePrototypes = prototypes;

            EditorUtility.SetDirty(data);

            return new SuccessResponse($"Tree prototype added: {prefab.name}", new
            {
                instanceID = terrain.gameObject.GetInstanceID(),
                prototypeIndex = prototypes.Length - 1,
                prefabPath
            });
        }

        private static object TerrainAddDetail(JObject @params)
        {
            Terrain terrain = FindTerrain(@params);
            if (terrain == null) return TerrainNotFoundError(@params);

            string prefabPath = ParamCoercion.CoerceString(@params["prefabPath"], null);
            
            TerrainData data = terrain.terrainData;
            Undo.RecordObject(data, "Add Detail Prototype");

            DetailPrototype prototype = new DetailPrototype();
            
            if (!string.IsNullOrEmpty(prefabPath))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    prototype.prototype = prefab;
                    prototype.usePrototypeMesh = true;
                }
            }

            DetailPrototype[] prototypes = data.detailPrototypes ?? new DetailPrototype[0];
            Array.Resize(ref prototypes, prototypes.Length + 1);
            prototypes[prototypes.Length - 1] = prototype;
            data.detailPrototypes = prototypes;

            EditorUtility.SetDirty(data);

            return new SuccessResponse($"Detail prototype added.", new
            {
                instanceID = terrain.gameObject.GetInstanceID(),
                prototypeIndex = prototypes.Length - 1,
                prefabPath
            });
        }

        #endregion

        #region Settings

        private static object TerrainSetSettings(JObject @params)
        {
            Terrain terrain = FindTerrain(@params);
            if (terrain == null) return TerrainNotFoundError(@params);

            Undo.RecordObject(terrain, "Set Terrain Settings");

            if (@params["drawTrees"] != null)
                terrain.drawTreesAndFoliage = ParamCoercion.CoerceBool(@params["drawTrees"], terrain.drawTreesAndFoliage);

            if (@params["pixelError"] != null)
                terrain.heightmapPixelError = ParamCoercion.CoerceFloat(@params["pixelError"], terrain.heightmapPixelError);

            if (@params["baseMapDistance"] != null)
                terrain.basemapDistance = ParamCoercion.CoerceFloat(@params["baseMapDistance"], terrain.basemapDistance);

            EditorUtility.SetDirty(terrain);
            EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);

            return new SuccessResponse($"Terrain settings updated.", new
            {
                instanceID = terrain.gameObject.GetInstanceID(),
                drawTreesAndFoliage = terrain.drawTreesAndFoliage,
                heightmapPixelError = terrain.heightmapPixelError,
                basemapDistance = terrain.basemapDistance
            });
        }

        #endregion

        #region Helpers

        private static Terrain FindTerrain(JObject @params)
        {
            JToken targetToken = @params["target"];
            if (targetToken == null)
            {
                // Find first terrain in scene
                return UnityEngine.Object.FindFirstObjectByType<Terrain>();
            }

            if (targetToken.Type == JTokenType.Integer)
            {
                int instanceId = targetToken.Value<int>();
                GameObject go = GameObjectLookup.FindById(instanceId);
                return go?.GetComponent<Terrain>();
            }

            string targetStr = targetToken.ToString();
            if (int.TryParse(targetStr, out int parsedId))
            {
                GameObject go = GameObjectLookup.FindById(parsedId);
                if (go != null) return go.GetComponent<Terrain>();
            }

            GameObject found = GameObjectLookup.FindByTarget(targetToken, "by_name", true);
            return found?.GetComponent<Terrain>();
        }

        private static object TerrainNotFoundError(JObject @params)
        {
            return new ErrorResponse($"Terrain not found: '{@params["target"]}'.");
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

        private static Vector2 ParseVector2(JToken token, Vector2 defaultValue)
        {
            if (token == null) return defaultValue;

            if (token is JArray arr && arr.Count >= 2)
            {
                return new Vector2(
                    ParamCoercion.CoerceFloat(arr[0], defaultValue.x),
                    ParamCoercion.CoerceFloat(arr[1], defaultValue.y)
                );
            }

            if (token is JObject obj)
            {
                return new Vector2(
                    ParamCoercion.CoerceFloat(obj["x"], defaultValue.x),
                    ParamCoercion.CoerceFloat(obj["y"], defaultValue.y)
                );
            }

            return defaultValue;
        }

        #endregion
    }
}
