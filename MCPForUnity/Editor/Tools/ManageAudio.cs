using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Audio components:
    /// - AudioSource: playback and configuration
    /// - AudioListener: scene audio listening
    /// - AudioMixer: mixer parameter control
    /// 
    /// Actions:
    /// - source_add: Add AudioSource to GameObject
    /// - source_configure: Configure AudioSource properties
    /// - source_get_info: Get AudioSource information
    /// - source_play: Play audio (PlayMode)
    /// - source_stop: Stop audio (PlayMode)
    /// - source_pause: Pause audio (PlayMode)
    /// - listener_add: Add AudioListener to GameObject
    /// - listener_get_info: Get scene audio listener info
    /// - mixer_get_info: Get AudioMixer information
    /// - mixer_set_parameter: Set exposed AudioMixer parameter
    /// </summary>
    [McpForUnityTool("manage_audio")]
    public static class ManageAudio
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
                    // AudioSource actions
                    "source_add" => SourceAdd(@params),
                    "source_configure" => SourceConfigure(@params),
                    "source_get_info" => SourceGetInfo(@params),
                    "source_play" => SourcePlay(@params),
                    "source_stop" => SourceStop(@params),
                    "source_pause" => SourcePause(@params),
                    "source_set_clip" => SourceSetClip(@params),
                    
                    // AudioListener actions
                    "listener_add" => ListenerAdd(@params),
                    "listener_get_info" => ListenerGetInfo(@params),
                    
                    // AudioMixer actions
                    "mixer_get_info" => MixerGetInfo(@params),
                    "mixer_set_parameter" => MixerSetParameter(@params),
                    
                    _ => new ErrorResponse($"Unknown action: '{action}'. Supported: source_add, source_configure, source_get_info, source_play, source_stop, source_pause, source_set_clip, listener_add, listener_get_info, mixer_get_info, mixer_set_parameter")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageAudio] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error processing action '{action}': {e.Message}");
            }
        }

        #region AudioSource Actions

        private static object SourceAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            if (target.GetComponent<AudioSource>() != null)
            {
                return new ErrorResponse($"GameObject '{target.name}' already has an AudioSource.");
            }

            AudioSource source = Undo.AddComponent<AudioSource>(target);
            
            // Set audio clip if provided
            string clipPath = ParamCoercion.CoerceString(@params["clip"] ?? @params["clipPath"], null);
            if (!string.IsNullOrEmpty(clipPath))
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null)
                {
                    source.clip = clip;
                }
            }

            // Apply properties
            ApplySourceProperties(source, @params);

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"AudioSource added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                hasClip = source.clip != null,
                clipName = source.clip?.name,
                volume = source.volume,
                playOnAwake = source.playOnAwake
            });
        }

        private static object SourceConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            AudioSource source = target.GetComponent<AudioSource>();
            if (source == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an AudioSource.");
            }

            Undo.RecordObject(source, "Configure AudioSource");
            
            // Set audio clip if provided
            string clipPath = ParamCoercion.CoerceString(@params["clip"] ?? @params["clipPath"], null);
            if (!string.IsNullOrEmpty(clipPath))
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null)
                {
                    source.clip = clip;
                }
            }

            ApplySourceProperties(source, @params);

            EditorUtility.SetDirty(source);
            MarkSceneDirty(target);

            return new SuccessResponse($"AudioSource configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                volume = source.volume,
                pitch = source.pitch,
                loop = source.loop,
                spatialBlend = source.spatialBlend
            });
        }

        private static object SourceGetInfo(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            AudioSource source = target.GetComponent<AudioSource>();
            if (source == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an AudioSource.");
            }

            var info = new Dictionary<string, object>
            {
                ["instanceID"] = target.GetInstanceID(),
                ["clip"] = source.clip?.name,
                ["clipPath"] = source.clip != null ? AssetDatabase.GetAssetPath(source.clip) : null,
                ["volume"] = source.volume,
                ["pitch"] = source.pitch,
                ["loop"] = source.loop,
                ["playOnAwake"] = source.playOnAwake,
                ["mute"] = source.mute,
                ["spatialBlend"] = source.spatialBlend,
                ["dopplerLevel"] = source.dopplerLevel,
                ["spread"] = source.spread,
                ["minDistance"] = source.minDistance,
                ["maxDistance"] = source.maxDistance,
                ["priority"] = source.priority,
                ["outputAudioMixerGroup"] = source.outputAudioMixerGroup?.name
            };

            // Add runtime info if in PlayMode
            if (Application.isPlaying)
            {
                info["isPlaying"] = source.isPlaying;
                info["time"] = source.time;
                info["timeSamples"] = source.timeSamples;
            }

            return new SuccessResponse($"AudioSource info for '{target.name}'.", info);
        }

        private static object SourceSetClip(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            AudioSource source = target.GetComponent<AudioSource>();
            if (source == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an AudioSource.");
            }

            string clipPath = ParamCoercion.CoerceString(@params["clip"] ?? @params["clipPath"], null);
            if (string.IsNullOrEmpty(clipPath))
            {
                return new ErrorResponse("'clip' path is required.");
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
            {
                return new ErrorResponse($"AudioClip not found at '{clipPath}'.");
            }

            Undo.RecordObject(source, "Set AudioClip");
            source.clip = clip;

            EditorUtility.SetDirty(source);
            MarkSceneDirty(target);

            return new SuccessResponse($"AudioClip set on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                clipName = clip.name,
                clipPath = clipPath,
                length = clip.length,
                frequency = clip.frequency,
                channels = clip.channels
            });
        }

        private static object SourcePlay(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            AudioSource source = target.GetComponent<AudioSource>();
            if (source == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an AudioSource.");
            }

            if (!Application.isPlaying)
            {
                return new
                {
                    success = true,
                    message = "Audio would play in PlayMode.",
                    data = new
                    {
                        instanceID = target.GetInstanceID(),
                        warning = "Audio playback only works during PlayMode."
                    }
                };
            }

            float delay = ParamCoercion.CoerceFloat(@params["delay"], 0f);
            
            if (delay > 0)
            {
                source.PlayDelayed(delay);
            }
            else
            {
                source.Play();
            }

            return new SuccessResponse($"Playing audio on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                clipName = source.clip?.name,
                delay = delay
            });
        }

        private static object SourceStop(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            AudioSource source = target.GetComponent<AudioSource>();
            if (source == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an AudioSource.");
            }

            if (!Application.isPlaying)
            {
                return new
                {
                    success = true,
                    message = "Audio would stop in PlayMode.",
                    data = new { instanceID = target.GetInstanceID() }
                };
            }

            source.Stop();

            return new SuccessResponse($"Stopped audio on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID()
            });
        }

        private static object SourcePause(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            AudioSource source = target.GetComponent<AudioSource>();
            if (source == null)
            {
                return new ErrorResponse($"GameObject '{target.name}' does not have an AudioSource.");
            }

            if (!Application.isPlaying)
            {
                return new
                {
                    success = true,
                    message = "Audio would pause in PlayMode.",
                    data = new { instanceID = target.GetInstanceID() }
                };
            }

            source.Pause();

            return new SuccessResponse($"Paused audio on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID()
            });
        }

        private static void ApplySourceProperties(AudioSource source, JObject @params)
        {
            if (@params["volume"] != null)
                source.volume = Mathf.Clamp01(ParamCoercion.CoerceFloat(@params["volume"], source.volume));
            
            if (@params["pitch"] != null)
                source.pitch = ParamCoercion.CoerceFloat(@params["pitch"], source.pitch);
            
            if (@params["loop"] != null)
                source.loop = ParamCoercion.CoerceBool(@params["loop"], source.loop);
            
            if (@params["playOnAwake"] != null || @params["play_on_awake"] != null)
                source.playOnAwake = ParamCoercion.CoerceBool(@params["playOnAwake"] ?? @params["play_on_awake"], source.playOnAwake);
            
            if (@params["mute"] != null)
                source.mute = ParamCoercion.CoerceBool(@params["mute"], source.mute);
            
            if (@params["spatialBlend"] != null || @params["spatial_blend"] != null)
                source.spatialBlend = Mathf.Clamp01(ParamCoercion.CoerceFloat(@params["spatialBlend"] ?? @params["spatial_blend"], source.spatialBlend));
            
            if (@params["dopplerLevel"] != null || @params["doppler"] != null)
                source.dopplerLevel = ParamCoercion.CoerceFloat(@params["dopplerLevel"] ?? @params["doppler"], source.dopplerLevel);
            
            if (@params["spread"] != null)
                source.spread = ParamCoercion.CoerceFloat(@params["spread"], source.spread);
            
            if (@params["minDistance"] != null || @params["min_distance"] != null)
                source.minDistance = ParamCoercion.CoerceFloat(@params["minDistance"] ?? @params["min_distance"], source.minDistance);
            
            if (@params["maxDistance"] != null || @params["max_distance"] != null)
                source.maxDistance = ParamCoercion.CoerceFloat(@params["maxDistance"] ?? @params["max_distance"], source.maxDistance);
            
            if (@params["priority"] != null)
                source.priority = Mathf.Clamp(ParamCoercion.CoerceInt(@params["priority"], source.priority), 0, 256);

            // Set output mixer group if provided
            string mixerGroupPath = ParamCoercion.CoerceString(@params["outputMixerGroup"] ?? @params["mixerGroup"], null);
            if (!string.IsNullOrEmpty(mixerGroupPath))
            {
                AudioMixerGroup group = AssetDatabase.LoadAssetAtPath<AudioMixerGroup>(mixerGroupPath);
                if (group != null)
                {
                    source.outputAudioMixerGroup = group;
                }
            }
        }

        #endregion

        #region AudioListener Actions

        private static object ListenerAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            if (target.GetComponent<AudioListener>() != null)
            {
                return new ErrorResponse($"GameObject '{target.name}' already has an AudioListener.");
            }

            // Check if there's already an AudioListener in the scene
            AudioListener existingListener = UnityEngine.Object.FindFirstObjectByType<AudioListener>();
            if (existingListener != null)
            {
                McpLog.Warn($"[ManageAudio] Scene already has an AudioListener on '{existingListener.gameObject.name}'. Adding another may cause issues.");
            }

            AudioListener listener = Undo.AddComponent<AudioListener>(target);

            EditorUtility.SetDirty(target);
            MarkSceneDirty(target);

            return new SuccessResponse($"AudioListener added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                warning = existingListener != null ? $"Scene already had an AudioListener on '{existingListener.gameObject.name}'." : null
            });
        }

        private static object ListenerGetInfo(JObject @params)
        {
            AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            
            if (listeners.Length == 0)
            {
                return new SuccessResponse("No AudioListener found in scene.", new
                {
                    listenerCount = 0
                });
            }

            var listenerInfos = listeners.Select(l => new
            {
                gameObject = l.gameObject.name,
                instanceID = l.gameObject.GetInstanceID(),
                enabled = l.enabled,
                position = new { x = l.transform.position.x, y = l.transform.position.y, z = l.transform.position.z }
            }).ToArray();

            return new SuccessResponse($"Found {listeners.Length} AudioListener(s) in scene.", new
            {
                listenerCount = listeners.Length,
                listeners = listenerInfos,
                globalVolume = AudioListener.volume,
                globalPause = AudioListener.pause
            });
        }

        #endregion

        #region AudioMixer Actions

        private static object MixerGetInfo(JObject @params)
        {
            string mixerPath = ParamCoercion.CoerceString(@params["mixer"] ?? @params["path"], null);
            if (string.IsNullOrEmpty(mixerPath))
            {
                return new ErrorResponse("'mixer' path is required.");
            }

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
            if (mixer == null)
            {
                return new ErrorResponse($"AudioMixer not found at '{mixerPath}'.");
            }

            // Get exposed parameters via reflection (Unity doesn't expose this directly)
            var exposedParams = new List<string>();
            // Note: Unity's AudioMixer doesn't provide a public API to list exposed parameters
            // We can only try to get/set them by name

            return new SuccessResponse($"AudioMixer info for '{mixer.name}'.", new
            {
                name = mixer.name,
                path = mixerPath,
                outputAudioMixerGroup = mixer.outputAudioMixerGroup?.name,
                note = "Use mixer_set_parameter to set exposed parameters by name."
            });
        }

        private static object MixerSetParameter(JObject @params)
        {
            string mixerPath = ParamCoercion.CoerceString(@params["mixer"] ?? @params["path"], null);
            if (string.IsNullOrEmpty(mixerPath))
            {
                return new ErrorResponse("'mixer' path is required.");
            }

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
            if (mixer == null)
            {
                return new ErrorResponse($"AudioMixer not found at '{mixerPath}'.");
            }

            string paramName = ParamCoercion.CoerceString(@params["parameter"] ?? @params["name"], null);
            if (string.IsNullOrEmpty(paramName))
            {
                return new ErrorResponse("'parameter' name is required.");
            }

            float value = ParamCoercion.CoerceFloat(@params["value"], 0f);

            bool success = mixer.SetFloat(paramName, value);
            if (!success)
            {
                return new ErrorResponse($"Failed to set parameter '{paramName}'. Make sure it's exposed in the AudioMixer.");
            }

            // Verify the value was set
            float actualValue;
            mixer.GetFloat(paramName, out actualValue);

            return new SuccessResponse($"Parameter '{paramName}' set on mixer '{mixer.name}'.", new
            {
                mixer = mixer.name,
                parameter = paramName,
                value = actualValue
            });
        }

        #endregion

        #region Helpers

        private static GameObject FindTarget(JObject @params)
        {
            JToken targetToken = @params["target"];
            if (targetToken == null) return null;

            if (targetToken.Type == JTokenType.Integer)
            {
                int instanceId = targetToken.Value<int>();
                return GameObjectLookup.FindById(instanceId);
            }

            string targetStr = targetToken.ToString();

            if (int.TryParse(targetStr, out int parsedId))
            {
                var byId = GameObjectLookup.FindById(parsedId);
                if (byId != null) return byId;
            }

            return GameObjectLookup.FindByTarget(targetToken, "by_name", true);
        }

        private static object TargetNotFoundError(JObject @params)
        {
            return new ErrorResponse($"Target GameObject '{@params["target"]}' not found.");
        }

        private static void MarkSceneDirty(GameObject go)
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            }
            else
            {
                EditorSceneManager.MarkSceneDirty(go.scene);
            }
        }

        #endregion
    }
}
