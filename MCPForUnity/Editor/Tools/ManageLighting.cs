using System;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Lighting:
    /// - Create and configure lights (Directional, Point, Spot, Area)
    /// - Set ambient lighting and environment settings
    /// - Configure fog
    /// - Create and bake reflection probes
    /// - Bake lightmaps
    /// </summary>
    [McpForUnityTool("manage_lighting")]
    public static class ManageLighting
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
                    "light_create" => LightCreate(@params),
                    "light_configure" => LightConfigure(@params),
                    "light_get_info" => LightGetInfo(@params),
                    "ambient_set" => AmbientSet(@params),
                    "ambient_get" => AmbientGet(@params),
                    "fog_set" => FogSet(@params),
                    "fog_get" => FogGet(@params),
                    "reflection_probe_create" => ReflectionProbeCreate(@params),
                    "reflection_probe_bake" => ReflectionProbeBake(@params),
                    "lightmap_bake" => LightmapBake(@params),
                    "lighting_get_settings" => LightingGetSettings(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'. Supported: light_create, light_configure, light_get_info, ambient_set, ambient_get, fog_set, fog_get, reflection_probe_create, reflection_probe_bake, lightmap_bake, lighting_get_settings")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageLighting] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error processing action '{action}': {e.Message}");
            }
        }

        #region Light Creation and Configuration

        private static object LightCreate(JObject @params)
        {
            string lightTypeStr = ParamCoercion.CoerceString(@params["lightType"], "Point");
            if (!Enum.TryParse<LightType>(lightTypeStr, true, out var lightType))
            {
                return new ErrorResponse($"Unknown light type: '{lightTypeStr}'. Supported: Directional, Point, Spot, Area");
            }

            Vector3 position = ParseVector3(@params["position"], Vector3.zero);
            Vector3 rotation = ParseVector3(@params["rotation"], lightType == LightType.Directional ? new Vector3(50, -30, 0) : Vector3.zero);

            GameObject lightGo = new GameObject($"{lightType} Light");
            lightGo.transform.position = position;
            lightGo.transform.eulerAngles = rotation;

            Light light = lightGo.AddComponent<Light>();
            light.type = lightType;

            // Apply properties
            ApplyLightProperties(light, @params);

            Undo.RegisterCreatedObjectUndo(lightGo, "Create Light");
            EditorSceneManager.MarkSceneDirty(lightGo.scene);

            return new SuccessResponse($"{lightType} light created.", new
            {
                instanceID = lightGo.GetInstanceID(),
                name = lightGo.name,
                lightType = lightType.ToString(),
                position = new { x = position.x, y = position.y, z = position.z },
                color = ColorToHex(light.color),
                intensity = light.intensity
            });
        }

        private static object LightConfigure(JObject @params)
        {
            Light light = FindLight(@params);
            if (light == null) return LightNotFoundError(@params);

            Undo.RecordObject(light, "Configure Light");
            ApplyLightProperties(light, @params);

            EditorUtility.SetDirty(light);
            EditorSceneManager.MarkSceneDirty(light.gameObject.scene);

            return new SuccessResponse($"Light '{light.name}' configured.", new
            {
                instanceID = light.gameObject.GetInstanceID(),
                lightType = light.type.ToString(),
                color = ColorToHex(light.color),
                intensity = light.intensity,
                range = light.range,
                shadows = light.shadows.ToString()
            });
        }

        private static object LightGetInfo(JObject @params)
        {
            Light light = FindLight(@params);
            if (light == null) return LightNotFoundError(@params);

            return new SuccessResponse($"Light info for '{light.name}'.", new
            {
                instanceID = light.gameObject.GetInstanceID(),
                name = light.name,
                lightType = light.type.ToString(),
                color = ColorToHex(light.color),
                intensity = light.intensity,
                range = light.range,
                spotAngle = light.spotAngle,
                innerSpotAngle = light.innerSpotAngle,
                shadows = light.shadows.ToString(),
                shadowStrength = light.shadowStrength,
                cullingMask = light.cullingMask,
                enabled = light.enabled
            });
        }

        private static void ApplyLightProperties(Light light, JObject @params)
        {
            if (@params["color"] != null)
                light.color = ParseColor(@params["color"], light.color);

            if (@params["intensity"] != null)
                light.intensity = ParamCoercion.CoerceFloat(@params["intensity"], light.intensity);

            if (@params["range"] != null)
                light.range = ParamCoercion.CoerceFloat(@params["range"], light.range);

            if (@params["spotAngle"] != null)
                light.spotAngle = ParamCoercion.CoerceFloat(@params["spotAngle"], light.spotAngle);

            if (@params["innerSpotAngle"] != null)
                light.innerSpotAngle = ParamCoercion.CoerceFloat(@params["innerSpotAngle"], light.innerSpotAngle);

            if (@params["shadows"] != null)
            {
                string shadowStr = ParamCoercion.CoerceString(@params["shadows"], "None");
                if (Enum.TryParse<LightShadows>(shadowStr, true, out var shadowType))
                    light.shadows = shadowType;
            }

            if (@params["shadowStrength"] != null)
                light.shadowStrength = ParamCoercion.CoerceFloat(@params["shadowStrength"], light.shadowStrength);

            if (@params["shadowBias"] != null)
                light.shadowBias = ParamCoercion.CoerceFloat(@params["shadowBias"], light.shadowBias);

            if (@params["shadowNormalBias"] != null)
                light.shadowNormalBias = ParamCoercion.CoerceFloat(@params["shadowNormalBias"], light.shadowNormalBias);

            if (@params["cookieSize"] != null)
                light.cookieSize = ParamCoercion.CoerceFloat(@params["cookieSize"], light.cookieSize);

            if (@params["cullingMask"] != null)
                light.cullingMask = ParamCoercion.CoerceInt(@params["cullingMask"], light.cullingMask);

            if (@params["enabled"] != null)
                light.enabled = ParamCoercion.CoerceBool(@params["enabled"], light.enabled);
        }

        #endregion

        #region Ambient Lighting

        private static object AmbientSet(JObject @params)
        {
            Undo.RecordObject(RenderSettings.defaultReflectionMode == DefaultReflectionMode.Skybox ? (UnityEngine.Object)RenderSettings.skybox : null, "Set Ambient");

            if (@params["ambientMode"] != null)
            {
                string modeStr = ParamCoercion.CoerceString(@params["ambientMode"], "Skybox");
                if (Enum.TryParse<AmbientMode>(modeStr, true, out var mode))
                    RenderSettings.ambientMode = mode;
            }

            if (@params["ambientColor"] != null)
                RenderSettings.ambientLight = ParseColor(@params["ambientColor"], RenderSettings.ambientLight);

            if (@params["ambientSkyColor"] != null)
                RenderSettings.ambientSkyColor = ParseColor(@params["ambientSkyColor"], RenderSettings.ambientSkyColor);

            if (@params["ambientEquatorColor"] != null)
                RenderSettings.ambientEquatorColor = ParseColor(@params["ambientEquatorColor"], RenderSettings.ambientEquatorColor);

            if (@params["ambientGroundColor"] != null)
                RenderSettings.ambientGroundColor = ParseColor(@params["ambientGroundColor"], RenderSettings.ambientGroundColor);

            if (@params["ambientIntensity"] != null)
                RenderSettings.ambientIntensity = ParamCoercion.CoerceFloat(@params["ambientIntensity"], RenderSettings.ambientIntensity);

            EditorSceneManager.MarkAllScenesDirty();

            return new SuccessResponse("Ambient lighting updated.", new
            {
                ambientMode = RenderSettings.ambientMode.ToString(),
                ambientColor = ColorToHex(RenderSettings.ambientLight),
                ambientIntensity = RenderSettings.ambientIntensity
            });
        }

        private static object AmbientGet(JObject @params)
        {
            return new SuccessResponse("Ambient lighting settings.", new
            {
                ambientMode = RenderSettings.ambientMode.ToString(),
                ambientColor = ColorToHex(RenderSettings.ambientLight),
                ambientSkyColor = ColorToHex(RenderSettings.ambientSkyColor),
                ambientEquatorColor = ColorToHex(RenderSettings.ambientEquatorColor),
                ambientGroundColor = ColorToHex(RenderSettings.ambientGroundColor),
                ambientIntensity = RenderSettings.ambientIntensity
            });
        }

        #endregion

        #region Fog

        private static object FogSet(JObject @params)
        {
            if (@params["fogEnabled"] != null)
                RenderSettings.fog = ParamCoercion.CoerceBool(@params["fogEnabled"], RenderSettings.fog);

            if (@params["fogMode"] != null)
            {
                string modeStr = ParamCoercion.CoerceString(@params["fogMode"], "Linear");
                if (Enum.TryParse<FogMode>(modeStr, true, out var mode))
                    RenderSettings.fogMode = mode;
            }

            if (@params["fogColor"] != null)
                RenderSettings.fogColor = ParseColor(@params["fogColor"], RenderSettings.fogColor);

            if (@params["fogDensity"] != null)
                RenderSettings.fogDensity = ParamCoercion.CoerceFloat(@params["fogDensity"], RenderSettings.fogDensity);

            if (@params["fogStart"] != null)
                RenderSettings.fogStartDistance = ParamCoercion.CoerceFloat(@params["fogStart"], RenderSettings.fogStartDistance);

            if (@params["fogEnd"] != null)
                RenderSettings.fogEndDistance = ParamCoercion.CoerceFloat(@params["fogEnd"], RenderSettings.fogEndDistance);

            EditorSceneManager.MarkAllScenesDirty();

            return new SuccessResponse("Fog settings updated.", new
            {
                enabled = RenderSettings.fog,
                mode = RenderSettings.fogMode.ToString(),
                color = ColorToHex(RenderSettings.fogColor),
                density = RenderSettings.fogDensity,
                startDistance = RenderSettings.fogStartDistance,
                endDistance = RenderSettings.fogEndDistance
            });
        }

        private static object FogGet(JObject @params)
        {
            return new SuccessResponse("Fog settings.", new
            {
                enabled = RenderSettings.fog,
                mode = RenderSettings.fogMode.ToString(),
                color = ColorToHex(RenderSettings.fogColor),
                density = RenderSettings.fogDensity,
                startDistance = RenderSettings.fogStartDistance,
                endDistance = RenderSettings.fogEndDistance
            });
        }

        #endregion

        #region Reflection Probe

        private static object ReflectionProbeCreate(JObject @params)
        {
            Vector3 position = ParseVector3(@params["position"], Vector3.zero);
            Vector3 size = ParseVector3(@params["probeSize"], new Vector3(10, 10, 10));

            GameObject probeGo = new GameObject("Reflection Probe");
            probeGo.transform.position = position;

            ReflectionProbe probe = probeGo.AddComponent<ReflectionProbe>();
            probe.size = size;

            if (@params["probeResolution"] != null)
                probe.resolution = ParamCoercion.CoerceInt(@params["probeResolution"], 128);

            if (@params["probeHdr"] != null)
                probe.hdr = ParamCoercion.CoerceBool(@params["probeHdr"], true);

            if (@params["probeImportance"] != null)
                probe.importance = ParamCoercion.CoerceInt(@params["probeImportance"], 1);

            Undo.RegisterCreatedObjectUndo(probeGo, "Create Reflection Probe");
            EditorSceneManager.MarkSceneDirty(probeGo.scene);

            return new SuccessResponse("Reflection probe created.", new
            {
                instanceID = probeGo.GetInstanceID(),
                name = probeGo.name,
                position = new { x = position.x, y = position.y, z = position.z },
                size = new { x = size.x, y = size.y, z = size.z },
                resolution = probe.resolution
            });
        }

        private static object ReflectionProbeBake(JObject @params)
        {
            ReflectionProbe probe = FindReflectionProbe(@params);
            if (probe == null)
            {
                return new ErrorResponse("Reflection probe not found.");
            }

            // Trigger bake
            Lightmapping.BakeReflectionProbe(probe, probe.bakedTexture?.name ?? "ReflectionProbe");

            return new SuccessResponse($"Reflection probe '{probe.name}' baking started.", new
            {
                instanceID = probe.gameObject.GetInstanceID(),
                name = probe.name
            });
        }

        #endregion

        #region Lightmap Baking

        private static object LightmapBake(JObject @params)
        {
            string qualityStr = ParamCoercion.CoerceString(@params["bakeQuality"], "Medium");
            
            // Note: Lightmap baking is an async operation
            if (Lightmapping.isRunning)
            {
                return new ErrorResponse("Lightmap baking is already in progress.");
            }

            Lightmapping.BakeAsync();

            return new SuccessResponse("Lightmap baking started.", new
            {
                isRunning = Lightmapping.isRunning,
                quality = qualityStr
            });
        }

        private static object LightingGetSettings(JObject @params)
        {
            return new SuccessResponse("Current lighting settings.", new
            {
                ambient = new
                {
                    mode = RenderSettings.ambientMode.ToString(),
                    color = ColorToHex(RenderSettings.ambientLight),
                    intensity = RenderSettings.ambientIntensity
                },
                fog = new
                {
                    enabled = RenderSettings.fog,
                    mode = RenderSettings.fogMode.ToString(),
                    color = ColorToHex(RenderSettings.fogColor),
                    density = RenderSettings.fogDensity
                },
                lightmapping = new
                {
                    isRunning = Lightmapping.isRunning,
                    lightmapsCount = LightmapSettings.lightmaps?.Length ?? 0
                }
            });
        }

        #endregion

        #region Helpers

        private static Light FindLight(JObject @params)
        {
            JToken targetToken = @params["target"];
            if (targetToken == null) return null;

            if (targetToken.Type == JTokenType.Integer)
            {
                int instanceId = targetToken.Value<int>();
                GameObject go = GameObjectLookup.FindById(instanceId);
                return go?.GetComponent<Light>();
            }

            string targetStr = targetToken.ToString();
            if (int.TryParse(targetStr, out int parsedId))
            {
                GameObject go = GameObjectLookup.FindById(parsedId);
                if (go != null) return go.GetComponent<Light>();
            }

            GameObject found = GameObjectLookup.FindByTarget(targetToken, "by_name", true);
            return found?.GetComponent<Light>();
        }

        private static ReflectionProbe FindReflectionProbe(JObject @params)
        {
            JToken targetToken = @params["target"];
            if (targetToken == null)
            {
                return UnityEngine.Object.FindFirstObjectByType<ReflectionProbe>();
            }

            if (targetToken.Type == JTokenType.Integer)
            {
                int instanceId = targetToken.Value<int>();
                GameObject go = GameObjectLookup.FindById(instanceId);
                return go?.GetComponent<ReflectionProbe>();
            }

            string targetStr = targetToken.ToString();
            if (int.TryParse(targetStr, out int parsedId))
            {
                GameObject go = GameObjectLookup.FindById(parsedId);
                if (go != null) return go.GetComponent<ReflectionProbe>();
            }

            GameObject found = GameObjectLookup.FindByTarget(targetToken, "by_name", true);
            return found?.GetComponent<ReflectionProbe>();
        }

        private static object LightNotFoundError(JObject @params)
        {
            return new ErrorResponse($"Light not found: '{@params["target"]}'.");
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

        private static Color ParseColor(JToken token, Color defaultValue)
        {
            if (token == null) return defaultValue;

            // Hex string
            if (token.Type == JTokenType.String)
            {
                string hex = token.ToString();
                if (ColorUtility.TryParseHtmlString(hex, out Color color))
                    return color;
            }

            // Array [r, g, b] or [r, g, b, a]
            if (token is JArray arr && arr.Count >= 3)
            {
                return new Color(
                    ParamCoercion.CoerceFloat(arr[0], defaultValue.r),
                    ParamCoercion.CoerceFloat(arr[1], defaultValue.g),
                    ParamCoercion.CoerceFloat(arr[2], defaultValue.b),
                    arr.Count >= 4 ? ParamCoercion.CoerceFloat(arr[3], defaultValue.a) : 1f
                );
            }

            // Object {r, g, b, a}
            if (token is JObject obj)
            {
                return new Color(
                    ParamCoercion.CoerceFloat(obj["r"], defaultValue.r),
                    ParamCoercion.CoerceFloat(obj["g"], defaultValue.g),
                    ParamCoercion.CoerceFloat(obj["b"], defaultValue.b),
                    ParamCoercion.CoerceFloat(obj["a"], defaultValue.a)
                );
            }

            return defaultValue;
        }

        private static string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
        }

        #endregion
    }
}
