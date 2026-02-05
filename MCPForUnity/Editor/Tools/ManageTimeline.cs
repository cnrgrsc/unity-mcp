#if TIMELINE_ENABLED
using System;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Timeline.
    /// Requires: com.unity.timeline package + TIMELINE_ENABLED define
    /// </summary>
    [McpForUnityTool("manage_timeline")]
    public static class ManageTimeline
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
                    "timeline_create" => TimelineCreate(@params),
                    "timeline_get_info" => TimelineGetInfo(@params),
                    "director_add" => DirectorAdd(@params),
                    "director_configure" => DirectorConfigure(@params),
                    "track_add" => TrackAdd(@params),
                    "playback_play" => PlaybackPlay(@params),
                    "playback_pause" => PlaybackPause(@params),
                    "playback_stop" => PlaybackStop(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageTimeline] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        private static object TimelineCreate(JObject @params)
        {
            string name = ParamCoercion.CoerceString(@params["timelineName"], "Timeline");
            string path = $"Assets/{name}.playable";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, path);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"Timeline '{name}' created.", new
            {
                assetPath = path,
                name
            });
        }

        private static object TimelineGetInfo(JObject @params)
        {
            string path = ParamCoercion.CoerceString(@params["timelinePath"], null);
            if (string.IsNullOrEmpty(path))
            {
                return new ErrorResponse("'timelinePath' is required.");
            }

            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (timeline == null)
            {
                return new ErrorResponse($"Timeline not found at: {path}");
            }

            return new SuccessResponse($"Timeline info for '{timeline.name}'.", new
            {
                name = timeline.name,
                duration = timeline.duration,
                trackCount = timeline.outputTrackCount,
                frameRate = timeline.editorSettings.frameRate
            });
        }

        private static object DirectorAdd(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            PlayableDirector director = target.GetComponent<PlayableDirector>();
            if (director == null)
            {
                director = Undo.AddComponent<PlayableDirector>(target);
            }

            string timelinePath = ParamCoercion.CoerceString(@params["timelinePath"], null);
            if (!string.IsNullOrEmpty(timelinePath))
            {
                TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
                if (timeline != null)
                {
                    director.playableAsset = timeline;
                }
            }

            if (@params["playOnAwake"] != null)
                director.playOnAwake = ParamCoercion.CoerceBool(@params["playOnAwake"], director.playOnAwake);

            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(target.scene);

            return new SuccessResponse($"PlayableDirector added to '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                hasTimeline = director.playableAsset != null,
                playOnAwake = director.playOnAwake
            });
        }

        private static object DirectorConfigure(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            PlayableDirector director = target.GetComponent<PlayableDirector>();
            if (director == null)
            {
                return new ErrorResponse($"'{target.name}' has no PlayableDirector.");
            }

            Undo.RecordObject(director, "Configure Director");

            if (@params["playOnAwake"] != null)
                director.playOnAwake = ParamCoercion.CoerceBool(@params["playOnAwake"], director.playOnAwake);

            if (@params["initialTime"] != null)
                director.initialTime = ParamCoercion.CoerceFloat(@params["initialTime"], (float)director.initialTime);

            if (@params["wrapMode"] != null)
            {
                string mode = ParamCoercion.CoerceString(@params["wrapMode"], "");
                if (Enum.TryParse<DirectorWrapMode>(mode, true, out var wrapMode))
                    director.extrapolationMode = wrapMode;
            }

            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(target.scene);

            return new SuccessResponse($"Director configured on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                playOnAwake = director.playOnAwake,
                wrapMode = director.extrapolationMode.ToString()
            });
        }

        private static object TrackAdd(JObject @params)
        {
            string timelinePath = ParamCoercion.CoerceString(@params["timelinePath"], null);
            if (string.IsNullOrEmpty(timelinePath))
            {
                return new ErrorResponse("'timelinePath' is required.");
            }

            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            if (timeline == null)
            {
                return new ErrorResponse($"Timeline not found at: {timelinePath}");
            }

            string trackType = ParamCoercion.CoerceString(@params["trackType"], "Animation");
            string trackName = ParamCoercion.CoerceString(@params["trackName"], $"{trackType} Track");

            TrackAsset track = trackType.ToLowerInvariant() switch
            {
                "animation" => timeline.CreateTrack<AnimationTrack>(null, trackName),
                "audio" => timeline.CreateTrack<AudioTrack>(null, trackName),
                "activation" => timeline.CreateTrack<ActivationTrack>(null, trackName),
                "signal" => timeline.CreateTrack<SignalTrack>(null, trackName),
                "control" => timeline.CreateTrack<ControlTrack>(null, trackName),
                _ => null
            };

            if (track == null)
            {
                return new ErrorResponse($"Unknown track type: '{trackType}'");
            }

            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"{trackType} track '{trackName}' added.", new
            {
                timelinePath,
                trackType,
                trackName,
                trackCount = timeline.outputTrackCount
            });
        }

        private static object PlaybackPlay(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            PlayableDirector director = target.GetComponent<PlayableDirector>();
            if (director == null)
            {
                return new ErrorResponse($"'{target.name}' has no PlayableDirector.");
            }

            director.Play();

            return new SuccessResponse($"Playback started on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                state = director.state.ToString()
            });
        }

        private static object PlaybackPause(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            PlayableDirector director = target.GetComponent<PlayableDirector>();
            if (director == null)
            {
                return new ErrorResponse($"'{target.name}' has no PlayableDirector.");
            }

            director.Pause();

            return new SuccessResponse($"Playback paused on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID(),
                time = director.time
            });
        }

        private static object PlaybackStop(JObject @params)
        {
            GameObject target = FindTarget(@params);
            if (target == null) return TargetNotFoundError(@params);

            PlayableDirector director = target.GetComponent<PlayableDirector>();
            if (director == null)
            {
                return new ErrorResponse($"'{target.name}' has no PlayableDirector.");
            }

            director.Stop();

            return new SuccessResponse($"Playback stopped on '{target.name}'.", new
            {
                instanceID = target.GetInstanceID()
            });
        }

        #region Helpers

        private static GameObject FindTarget(JObject @params)
        {
            JToken targetToken = @params["target"];
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

        private static object TargetNotFoundError(JObject @params)
        {
            return new ErrorResponse($"Target not found: '{@params["target"]}'");
        }

        #endregion
    }
}
#else
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Tools
{
    [McpForUnityTool("manage_timeline")]
    public static class ManageTimeline
    {
        public static object HandleCommand(JObject @params)
        {
            return new ErrorResponse(
                "Timeline not installed. Install 'com.unity.timeline' and add 'TIMELINE_ENABLED' define."
            );
        }
    }
}
#endif
