using System;
using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Analytics and Firebase Analytics.
    /// </summary>
    [McpForUnityTool("manage_analytics")]
    public static class ManageAnalytics
    {
        public static object HandleCommand(JObject @params)
        {
            if (@params == null)
                return new ErrorResponse("Parameters cannot be null.");

            string action = ParamCoercion.CoerceString(@params["action"], null)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(action))
                return new ErrorResponse("'action' parameter is required.");

            try
            {
                return action switch
                {
                    "initialize" => Initialize(@params),
                    "send_event" => SendEvent(@params),
                    "set_user_property" => SetUserProperty(@params),
                    "set_user_id" => SetUserId(@params),
                    "enable" => EnableAnalytics(@params),
                    "disable" => DisableAnalytics(@params),
                    "get_settings" => GetSettings(@params),
                    "firebase_initialize" => FirebaseInitialize(@params),
                    "firebase_log_event" => FirebaseLogEvent(@params),
                    "revenue_event" => RevenueEvent(@params),
                    "level_event" => LevelEvent(@params),
                    "tutorial_event" => TutorialEvent(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageAnalytics] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        private static object Initialize(JObject @params)
        {
            #if UNITY_ANALYTICS
            return new SuccessResponse("Unity Analytics initialized.", new
            {
                enabled = UnityEngine.Analytics.Analytics.enabled,
                note = "Analytics is automatically initialized when enabled in Services"
            });
            #else
            return new SuccessResponse("Unity Analytics setup info.", new
            {
                package = "com.unity.analytics",
                instruction = "Enable Analytics in Window > Services",
                note = "Analytics events will be tracked automatically after setup"
            });
            #endif
        }

        private static object SendEvent(JObject @params)
        {
            string eventName = ParamCoercion.CoerceString(@params["eventName"], "custom_event");
            var eventParams = @params["eventParams"] as JObject;

            var paramDict = new Dictionary<string, object>();
            if (eventParams != null)
            {
                foreach (var prop in eventParams.Properties())
                {
                    paramDict[prop.Name] = prop.Value.ToObject<object>();
                }
            }

            return new SuccessResponse($"Analytics event '{eventName}' configured.", new
            {
                eventName,
                parameters = paramDict,
                codeExample = $"Analytics.CustomEvent(\"{eventName}\", new Dictionary<string, object> {{ ... }});"
            });
        }

        private static object SetUserProperty(JObject @params)
        {
            string propertyName = ParamCoercion.CoerceString(@params["propertyName"], "");
            string propertyValue = ParamCoercion.CoerceString(@params["propertyValue"], "");

            return new SuccessResponse("User property configured.", new
            {
                propertyName,
                propertyValue,
                codeExample = $"Analytics.SetUserProperty(\"{propertyName}\", \"{propertyValue}\");"
            });
        }

        private static object SetUserId(JObject @params)
        {
            string userId = ParamCoercion.CoerceString(@params["userId"], "");

            return new SuccessResponse("User ID configured.", new
            {
                userId,
                codeExample = $"Analytics.SetUserId(\"{userId}\");"
            });
        }

        private static object EnableAnalytics(JObject @params)
        {
            #if UNITY_ANALYTICS
            UnityEngine.Analytics.Analytics.enabled = true;
            return new SuccessResponse("Analytics enabled.", new { enabled = true });
            #else
            return new SuccessResponse("Enable analytics in Services window.", new { enabled = true });
            #endif
        }

        private static object DisableAnalytics(JObject @params)
        {
            #if UNITY_ANALYTICS
            UnityEngine.Analytics.Analytics.enabled = false;
            return new SuccessResponse("Analytics disabled.", new { enabled = false });
            #else
            return new SuccessResponse("Disable analytics in Services window.", new { enabled = false });
            #endif
        }

        private static object GetSettings(JObject @params)
        {
            return new SuccessResponse("Analytics settings.", new
            {
                unityAnalyticsPackage = "com.unity.analytics",
                firebasePackage = "Firebase SDK (external)",
                dashboardUrl = "https://dashboard.unity3d.com/"
            });
        }

        private static object FirebaseInitialize(JObject @params)
        {
            return new SuccessResponse("Firebase Analytics setup info.", new
            {
                sdkUrl = "https://firebase.google.com/docs/unity/setup",
                steps = new[]
                {
                    "1. Download Firebase Unity SDK",
                    "2. Import FirebaseAnalytics.unitypackage",
                    "3. Add google-services.json (Android) or GoogleService-Info.plist (iOS)",
                    "4. Initialize with FirebaseApp.CheckAndFixDependenciesAsync()"
                }
            });
        }

        private static object FirebaseLogEvent(JObject @params)
        {
            string eventName = ParamCoercion.CoerceString(@params["eventName"], "");
            var eventParams = @params["eventParams"] as JObject;

            return new SuccessResponse($"Firebase event '{eventName}' configured.", new
            {
                eventName,
                codeExample = $"FirebaseAnalytics.LogEvent(\"{eventName}\", params);"
            });
        }

        private static object RevenueEvent(JObject @params)
        {
            double revenue = ParamCoercion.CoerceDouble(@params["revenue"], 0);
            string currency = ParamCoercion.CoerceString(@params["currency"], "USD");
            string productId = ParamCoercion.CoerceString(@params["productId"], "");

            return new SuccessResponse("Revenue event configured.", new
            {
                revenue,
                currency,
                productId,
                codeExample = "Analytics.Transaction(productId, (decimal)revenue, currency);"
            });
        }

        private static object LevelEvent(JObject @params)
        {
            string levelName = ParamCoercion.CoerceString(@params["levelName"], "");
            int levelIndex = ParamCoercion.CoerceInt(@params["levelIndex"], 0);
            bool success = ParamCoercion.CoerceBool(@params["success"], true);

            return new SuccessResponse("Level event configured.", new
            {
                levelName,
                levelIndex,
                success,
                codeExample = success 
                    ? $"Analytics.LevelComplete(\"{levelName}\");" 
                    : $"Analytics.LevelFail(\"{levelName}\");"
            });
        }

        private static object TutorialEvent(JObject @params)
        {
            string tutorialStep = ParamCoercion.CoerceString(@params["tutorialStep"], "");
            bool success = ParamCoercion.CoerceBool(@params["success"], true);

            return new SuccessResponse("Tutorial event configured.", new
            {
                tutorialStep,
                success,
                codeExample = $"Analytics.TutorialStep({tutorialStep}, {success});"
            });
        }
    }
}
