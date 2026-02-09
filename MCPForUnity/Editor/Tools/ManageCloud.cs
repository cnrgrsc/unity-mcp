using System;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Cloud Services (Authentication, Cloud Save, Remote Config).
    /// </summary>
    [McpForUnityTool("manage_cloud")]
    public static class ManageCloud
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
                    "auth_initialize" => AuthInitialize(@params),
                    "auth_sign_in_anonymous" => AuthSignInAnonymous(@params),
                    "auth_sign_in_email" => AuthSignInEmail(@params),
                    "auth_sign_out" => AuthSignOut(@params),
                    "auth_get_user" => AuthGetUser(@params),
                    "save_initialize" => SaveInitialize(@params),
                    "save_data" => SaveData(@params),
                    "load_data" => LoadData(@params),
                    "config_initialize" => ConfigInitialize(@params),
                    "config_fetch" => ConfigFetch(@params),
                    "config_get_value" => ConfigGetValue(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageCloud] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        private static object AuthInitialize(JObject @params)
        {
            return new SuccessResponse("Unity Authentication setup.", new
            {
                package = "com.unity.services.authentication",
                steps = new[]
                {
                    "1. Install Unity Services packages",
                    "2. Link project in Project Settings > Services",
                    "3. Initialize with UnityServices.InitializeAsync()",
                    "4. Use AuthenticationService.Instance"
                },
                codeExample = @"await UnityServices.InitializeAsync();
await AuthenticationService.Instance.SignInAnonymouslyAsync();"
            });
        }

        private static object AuthSignInAnonymous(JObject @params)
        {
            return new SuccessResponse("Anonymous sign-in configured.", new
            {
                codeExample = "await AuthenticationService.Instance.SignInAnonymouslyAsync();",
                note = "Creates temporary player account, can be linked later"
            });
        }

        private static object AuthSignInEmail(JObject @params)
        {
            string email = ParamCoercion.CoerceString(@params["email"], "");

            return new SuccessResponse("Email sign-in configured.", new
            {
                email = email,
                codeExample = $"await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(\"{email}\", password);",
                note = "Requires Unity Authentication with username/password enabled"
            });
        }

        private static object AuthSignOut(JObject @params)
        {
            return new SuccessResponse("Sign-out configured.", new
            {
                codeExample = "AuthenticationService.Instance.SignOut();"
            });
        }

        private static object AuthGetUser(JObject @params)
        {
            return new SuccessResponse("Get user info.", new
            {
                codeExample = @"var playerId = AuthenticationService.Instance.PlayerId;
var accessToken = AuthenticationService.Instance.AccessToken;
var isSignedIn = AuthenticationService.Instance.IsSignedIn;"
            });
        }

        private static object SaveInitialize(JObject @params)
        {
            return new SuccessResponse("Cloud Save setup.", new
            {
                package = "com.unity.services.cloudsave",
                codeExample = @"// Save data
await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { ""key"", value } });
// Load data
var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { ""key"" });"
            });
        }

        private static object SaveData(JObject @params)
        {
            string key = ParamCoercion.CoerceString(@params["saveKey"], "");
            var data = @params["saveData"];

            return new SuccessResponse("Cloud save configured.", new
            {
                key = key,
                codeExample = $"await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> {{ {{ \"{key}\", data }} }});"
            });
        }

        private static object LoadData(JObject @params)
        {
            string key = ParamCoercion.CoerceString(@params["saveKey"], "");

            return new SuccessResponse("Cloud load configured.", new
            {
                key = key,
                codeExample = $"var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> {{ \"{key}\" }});"
            });
        }

        private static object ConfigInitialize(JObject @params)
        {
            return new SuccessResponse("Remote Config setup.", new
            {
                package = "com.unity.services.remoteconfig",
                dashboardUrl = "https://dashboard.unity3d.com/",
                codeExample = @"RemoteConfigService.Instance.FetchCompleted += ApplyRemoteSettings;
await RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), new AppAttributes());"
            });
        }

        private static object ConfigFetch(JObject @params)
        {
            return new SuccessResponse("Remote config fetch.", new
            {
                codeExample = "await RemoteConfigService.Instance.FetchConfigsAsync(userAttributes, appAttributes);"
            });
        }

        private static object ConfigGetValue(JObject @params)
        {
            string key = ParamCoercion.CoerceString(@params["configKey"], "");

            return new SuccessResponse("Get remote config value.", new
            {
                key = key,
                codeExample = $@"var stringValue = RemoteConfigService.Instance.appConfig.GetString(""{key}"");
var intValue = RemoteConfigService.Instance.appConfig.GetInt(""{key}"");
var floatValue = RemoteConfigService.Instance.appConfig.GetFloat(""{key}"");
var boolValue = RemoteConfigService.Instance.appConfig.GetBool(""{key}"");"
            });
        }
    }
}
