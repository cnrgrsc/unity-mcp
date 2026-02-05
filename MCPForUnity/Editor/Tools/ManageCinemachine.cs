#if CINEMACHINE_ENABLED
using System;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Cinemachine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Cinemachine virtual cameras.
    /// Requires: com.unity.cinemachine package + CINEMACHINE_ENABLED define
    /// </summary>
    [McpForUnityTool("manage_cinemachine")]
    public static class ManageCinemachine
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
                    "vcam_create" => VCamCreate(@params),
                    "vcam_configure" => VCamConfigure(@params),
                    "vcam_get_info" => VCamGetInfo(@params),
                    "vcam_set_follow" => VCamSetFollow(@params),
                    "vcam_set_lookat" => VCamSetLookAt(@params),
                    "dolly_create" => DollyCreate(@params),
                    "blend_configure" => BlendConfigure(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageCinemachine] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        private static object VCamCreate(JObject @params)
        {
            string name = ParamCoercion.CoerceString(@params["cameraName"], "Virtual Camera");
            Vector3 position = ParseVector3(@params["position"], Vector3.zero);
            
            GameObject vcamGo = new GameObject(name);
            vcamGo.transform.position = position;
            
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            
            if (@params["priority"] != null)
                vcam.Priority.Value = ParamCoercion.CoerceInt(@params["priority"], 10);
            
            // Set follow target if provided
            if (@params["followTarget"] != null)
            {
                GameObject followGo = FindTarget(@params["followTarget"]);
                if (followGo != null) vcam.Follow = followGo.transform;
            }
            
            // Set look-at target if provided
            if (@params["lookAtTarget"] != null)
            {
                GameObject lookGo = FindTarget(@params["lookAtTarget"]);
                if (lookGo != null) vcam.LookAt = lookGo.transform;
            }

            Undo.RegisterCreatedObjectUndo(vcamGo, "Create Virtual Camera");
            EditorSceneManager.MarkSceneDirty(vcamGo.scene);

            return new SuccessResponse($"Virtual camera '{name}' created.", new
            {
                instanceID = vcamGo.GetInstanceID(),
                name,
                priority = vcam.Priority.Value
            });
        }

        private static object VCamConfigure(JObject @params)
        {
            CinemachineCamera vcam = FindVCam(@params);
            if (vcam == null) return VCamNotFoundError(@params);

            Undo.RecordObject(vcam, "Configure Virtual Camera");

            if (@params["priority"] != null)
                vcam.Priority.Value = ParamCoercion.CoerceInt(@params["priority"], vcam.Priority.Value);

            if (@params["fov"] != null)
                vcam.Lens.FieldOfView = ParamCoercion.CoerceFloat(@params["fov"], vcam.Lens.FieldOfView);

            if (@params["nearClip"] != null)
                vcam.Lens.NearClipPlane = ParamCoercion.CoerceFloat(@params["nearClip"], vcam.Lens.NearClipPlane);

            if (@params["farClip"] != null)
                vcam.Lens.FarClipPlane = ParamCoercion.CoerceFloat(@params["farClip"], vcam.Lens.FarClipPlane);

            EditorUtility.SetDirty(vcam);
            EditorSceneManager.MarkSceneDirty(vcam.gameObject.scene);

            return new SuccessResponse($"Virtual camera '{vcam.name}' configured.", new
            {
                instanceID = vcam.gameObject.GetInstanceID(),
                priority = vcam.Priority.Value,
                fov = vcam.Lens.FieldOfView
            });
        }

        private static object VCamGetInfo(JObject @params)
        {
            CinemachineCamera vcam = FindVCam(@params);
            if (vcam == null) return VCamNotFoundError(@params);

            return new SuccessResponse($"Virtual camera info for '{vcam.name}'.", new
            {
                instanceID = vcam.gameObject.GetInstanceID(),
                name = vcam.name,
                priority = vcam.Priority.Value,
                fov = vcam.Lens.FieldOfView,
                hasFollow = vcam.Follow != null,
                hasLookAt = vcam.LookAt != null,
                followTarget = vcam.Follow?.name,
                lookAtTarget = vcam.LookAt?.name
            });
        }

        private static object VCamSetFollow(JObject @params)
        {
            CinemachineCamera vcam = FindVCam(@params);
            if (vcam == null) return VCamNotFoundError(@params);

            GameObject target = FindTarget(@params["followTarget"]);
            if (target == null)
            {
                return new ErrorResponse("Follow target not found.");
            }

            Undo.RecordObject(vcam, "Set Follow Target");
            vcam.Follow = target.transform;

            EditorUtility.SetDirty(vcam);
            EditorSceneManager.MarkSceneDirty(vcam.gameObject.scene);

            return new SuccessResponse($"Follow target set to '{target.name}'.", new
            {
                instanceID = vcam.gameObject.GetInstanceID(),
                followTarget = target.name
            });
        }

        private static object VCamSetLookAt(JObject @params)
        {
            CinemachineCamera vcam = FindVCam(@params);
            if (vcam == null) return VCamNotFoundError(@params);

            GameObject target = FindTarget(@params["lookAtTarget"]);
            if (target == null)
            {
                return new ErrorResponse("LookAt target not found.");
            }

            Undo.RecordObject(vcam, "Set LookAt Target");
            vcam.LookAt = target.transform;

            EditorUtility.SetDirty(vcam);
            EditorSceneManager.MarkSceneDirty(vcam.gameObject.scene);

            return new SuccessResponse($"LookAt target set to '{target.name}'.", new
            {
                instanceID = vcam.gameObject.GetInstanceID(),
                lookAtTarget = target.name
            });
        }

        private static object DollyCreate(JObject @params)
        {
            string name = ParamCoercion.CoerceString(@params["dollyName"], "Dolly Track");
            
            GameObject dollyGo = new GameObject(name);
            var spline = dollyGo.AddComponent<CinemachineSplineCart>();

            Undo.RegisterCreatedObjectUndo(dollyGo, "Create Dolly Track");
            EditorSceneManager.MarkSceneDirty(dollyGo.scene);

            return new SuccessResponse($"Dolly track '{name}' created.", new
            {
                instanceID = dollyGo.GetInstanceID(),
                name
            });
        }

        private static object BlendConfigure(JObject @params)
        {
            var brain = UnityEngine.Object.FindFirstObjectByType<CinemachineBrain>();
            if (brain == null)
            {
                // Create brain on main camera
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    brain = mainCam.gameObject.AddComponent<CinemachineBrain>();
                }
                else
                {
                    return new ErrorResponse("No CinemachineBrain found and no main camera available.");
                }
            }

            Undo.RecordObject(brain, "Configure Blend");

            if (@params["blendTime"] != null)
                brain.DefaultBlend.Time = ParamCoercion.CoerceFloat(@params["blendTime"], brain.DefaultBlend.Time);

            EditorUtility.SetDirty(brain);

            return new SuccessResponse("Blend settings configured.", new
            {
                blendTime = brain.DefaultBlend.Time
            });
        }

        #region Helpers

        private static CinemachineCamera FindVCam(JObject @params)
        {
            JToken targetToken = @params["target"];
            if (targetToken == null) return null;

            GameObject go = FindTarget(targetToken);
            return go?.GetComponent<CinemachineCamera>();
        }

        private static GameObject FindTarget(JToken targetToken)
        {
            if (targetToken == null) return null;

            if (targetToken.Type == JTokenType.Integer)
            {
                return GameObjectLookup.FindById(targetToken.Value<int>());
            }

            string targetStr = targetToken.ToString();
            if (int.TryParse(targetStr, out int id))
            {
                var go = GameObjectLookup.FindById(id);
                if (go != null) return go;
            }

            return GameObjectLookup.FindByTarget(targetToken, "by_name", true);
        }

        private static object VCamNotFoundError(JObject @params)
        {
            return new ErrorResponse($"Virtual camera not found: '{@params["target"]}'");
        }

        private static Vector3 ParseVector3(JToken token, Vector3 def)
        {
            if (token == null) return def;
            if (token is JObject obj)
            {
                return new Vector3(
                    ParamCoercion.CoerceFloat(obj["x"], def.x),
                    ParamCoercion.CoerceFloat(obj["y"], def.y),
                    ParamCoercion.CoerceFloat(obj["z"], def.z)
                );
            }
            return def;
        }

        #endregion
    }
}
#else
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Tools
{
    [McpForUnityTool("manage_cinemachine")]
    public static class ManageCinemachine
    {
        public static object HandleCommand(JObject @params)
        {
            return new ErrorResponse(
                "Cinemachine not installed. Install 'com.unity.cinemachine' and add 'CINEMACHINE_ENABLED' define."
            );
        }
    }
}
#endif
