using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity build settings, player settings, and executing builds.
    /// </summary>
    [McpForUnityTool("manage_build")]
    public static class ManageBuild
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
                    "build_get_settings" => BuildGetSettings(@params),
                    "build_set_platform" => BuildSetPlatform(@params),
                    "build_add_scene" => BuildAddScene(@params),
                    "build_remove_scene" => BuildRemoveScene(@params),
                    "build_set_scenes" => BuildSetScenes(@params),
                    "build_execute" => BuildExecute(@params),
                    "player_get_settings" => PlayerGetSettings(@params),
                    "player_set_settings" => PlayerSetSettings(@params),
                    "define_add" => DefineAdd(@params),
                    "define_remove" => DefineRemove(@params),
                    "define_list" => DefineList(@params),
                    "android_get_settings" => AndroidGetSettings(@params),
                    "android_set_settings" => AndroidSetSettings(@params),
                    "ios_get_settings" => IOSGetSettings(@params),
                    "ios_set_settings" => IOSSetSettings(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageBuild] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        #region Build Settings

        private static object BuildGetSettings(JObject @params)
        {
            var scenes = EditorBuildSettings.scenes
                .Select(s => new { path = s.path, enabled = s.enabled })
                .ToList();

            return new SuccessResponse("Current build settings.", new
            {
                activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                selectedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup.ToString(),
                development = EditorUserBuildSettings.development,
                scenes
            });
        }

        private static object BuildSetPlatform(JObject @params)
        {
            string platform = ParamCoercion.CoerceString(@params["platform"], null);
            if (string.IsNullOrEmpty(platform))
            {
                return new ErrorResponse("'platform' is required.");
            }

            if (!Enum.TryParse<BuildTarget>(platform, true, out var buildTarget))
            {
                return new ErrorResponse($"Unknown platform: '{platform}'");
            }

            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(buildTarget);
            
            bool success = EditorUserBuildSettings.SwitchActiveBuildTarget(group, buildTarget);

            if (!success)
            {
                return new ErrorResponse($"Failed to switch to platform: {platform}. Module may not be installed.");
            }

            return new SuccessResponse($"Platform switched to {platform}.", new
            {
                platform = buildTarget.ToString(),
                group = group.ToString()
            });
        }

        private static object BuildAddScene(JObject @params)
        {
            string scenePath = ParamCoercion.CoerceString(@params["scenePath"], null);
            if (string.IsNullOrEmpty(scenePath))
            {
                return new ErrorResponse("'scenePath' is required.");
            }

            var scenes = EditorBuildSettings.scenes.ToList();
            
            if (scenes.Any(s => s.path == scenePath))
            {
                return new SuccessResponse($"Scene already in build: {scenePath}", new
                {
                    scenePath,
                    alreadyExists = true
                });
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            return new SuccessResponse($"Scene added to build: {scenePath}", new
            {
                scenePath,
                sceneIndex = scenes.Count - 1
            });
        }

        private static object BuildRemoveScene(JObject @params)
        {
            string scenePath = ParamCoercion.CoerceString(@params["scenePath"], null);
            if (string.IsNullOrEmpty(scenePath))
            {
                return new ErrorResponse("'scenePath' is required.");
            }

            var scenes = EditorBuildSettings.scenes.ToList();
            int removed = scenes.RemoveAll(s => s.path == scenePath);
            EditorBuildSettings.scenes = scenes.ToArray();

            return new SuccessResponse($"Scene removed from build: {scenePath}", new
            {
                scenePath,
                removed = removed > 0
            });
        }

        private static object BuildSetScenes(JObject @params)
        {
            JArray scenePaths = @params["scenePaths"] as JArray;
            if (scenePaths == null || scenePaths.Count == 0)
            {
                return new ErrorResponse("'scenePaths' array is required.");
            }

            var scenes = scenePaths
                .Select(p => new EditorBuildSettingsScene(p.ToString(), true))
                .ToArray();

            EditorBuildSettings.scenes = scenes;

            return new SuccessResponse($"Build scenes set: {scenes.Length} scenes.", new
            {
                sceneCount = scenes.Length,
                scenes = scenes.Select(s => s.path).ToList()
            });
        }

        private static object BuildExecute(JObject @params)
        {
            string buildPath = ParamCoercion.CoerceString(@params["buildPath"], null);
            if (string.IsNullOrEmpty(buildPath))
            {
                return new ErrorResponse("'buildPath' is required.");
            }

            BuildOptions options = BuildOptions.None;
            JArray optionsArray = @params["buildOptions"] as JArray;
            if (optionsArray != null)
            {
                foreach (var opt in optionsArray)
                {
                    string optStr = opt.ToString();
                    if (Enum.TryParse<BuildOptions>(optStr, true, out var buildOption))
                    {
                        options |= buildOption;
                    }
                }
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                return new ErrorResponse("No scenes enabled in build settings.");
            }

            BuildReport report = BuildPipeline.BuildPlayer(
                scenes,
                buildPath,
                EditorUserBuildSettings.activeBuildTarget,
                options
            );

            if (report.summary.result == BuildResult.Succeeded)
            {
                return new SuccessResponse("Build succeeded!", new
                {
                    outputPath = report.summary.outputPath,
                    totalSize = report.summary.totalSize,
                    totalTime = report.summary.totalTime.TotalSeconds,
                    platform = report.summary.platform.ToString()
                });
            }
            else
            {
                return new ErrorResponse($"Build failed: {report.summary.result}", new
                {
                    result = report.summary.result.ToString(),
                    totalErrors = report.summary.totalErrors,
                    totalWarnings = report.summary.totalWarnings
                });
            }
        }

        #endregion

        #region Player Settings

        private static object PlayerGetSettings(JObject @params)
        {
            return new SuccessResponse("Current player settings.", new
            {
                productName = PlayerSettings.productName,
                companyName = PlayerSettings.companyName,
                version = PlayerSettings.bundleVersion,
                bundleIdentifier = PlayerSettings.applicationIdentifier,
                defaultScreenWidth = PlayerSettings.defaultScreenWidth,
                defaultScreenHeight = PlayerSettings.defaultScreenHeight,
                fullscreen = PlayerSettings.fullScreenMode.ToString()
            });
        }

        private static object PlayerSetSettings(JObject @params)
        {
            if (@params["productName"] != null)
                PlayerSettings.productName = ParamCoercion.CoerceString(@params["productName"], PlayerSettings.productName);

            if (@params["companyName"] != null)
                PlayerSettings.companyName = ParamCoercion.CoerceString(@params["companyName"], PlayerSettings.companyName);

            if (@params["version"] != null)
                PlayerSettings.bundleVersion = ParamCoercion.CoerceString(@params["version"], PlayerSettings.bundleVersion);

            if (@params["bundleIdentifier"] != null)
            {
                string bundleId = ParamCoercion.CoerceString(@params["bundleIdentifier"], "");
                PlayerSettings.SetApplicationIdentifier(EditorUserBuildSettings.selectedBuildTargetGroup, bundleId);
            }

            return new SuccessResponse("Player settings updated.", new
            {
                productName = PlayerSettings.productName,
                companyName = PlayerSettings.companyName,
                version = PlayerSettings.bundleVersion
            });
        }

        #endregion

        #region Scripting Defines

        private static object DefineAdd(JObject @params)
        {
            string symbol = ParamCoercion.CoerceString(@params["defineSymbol"], null);
            if (string.IsNullOrEmpty(symbol))
            {
                return new ErrorResponse("'defineSymbol' is required.");
            }

            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(group, out string[] defines);
            
            var list = defines.ToList();
            if (!list.Contains(symbol))
            {
                list.Add(symbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, list.ToArray());
            }

            return new SuccessResponse($"Define symbol added: {symbol}", new
            {
                symbol,
                group = group.ToString()
            });
        }

        private static object DefineRemove(JObject @params)
        {
            string symbol = ParamCoercion.CoerceString(@params["defineSymbol"], null);
            if (string.IsNullOrEmpty(symbol))
            {
                return new ErrorResponse("'defineSymbol' is required.");
            }

            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(group, out string[] defines);
            
            var list = defines.ToList();
            if (list.Remove(symbol))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, list.ToArray());
            }

            return new SuccessResponse($"Define symbol removed: {symbol}", new
            {
                symbol,
                group = group.ToString()
            });
        }

        private static object DefineList(JObject @params)
        {
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            PlayerSettings.GetScriptingDefineSymbolsForGroup(group, out string[] defines);

            return new SuccessResponse("Current scripting define symbols.", new
            {
                group = group.ToString(),
                defines = defines.ToList()
            });
        }

        #endregion

        #region Android Settings

        private static object AndroidGetSettings(JObject @params)
        {
            return new SuccessResponse("Current Android settings.", new
            {
                minSdkVersion = (int)PlayerSettings.Android.minSdkVersion,
                targetSdkVersion = (int)PlayerSettings.Android.targetSdkVersion,
                targetArchitectures = PlayerSettings.Android.targetArchitectures.ToString(),
                bundleVersionCode = PlayerSettings.Android.bundleVersionCode,
                useCustomKeystore = PlayerSettings.Android.useCustomKeystore,
                keystoreName = PlayerSettings.Android.keystoreName,
                splitApplicationBinary = PlayerSettings.Android.useAPKExpansionFiles,
                buildAppBundle = EditorUserBuildSettings.buildAppBundle,
                exportAsGoogleAndroidProject = EditorUserBuildSettings.exportAsGoogleAndroidProject,
                androidBuildSystem = EditorUserBuildSettings.androidBuildSystem.ToString()
            });
        }

        private static object AndroidSetSettings(JObject @params)
        {
            // Min SDK Version
            if (@params["minSdkVersion"] != null)
            {
                int minSdk = ParamCoercion.CoerceInt(@params["minSdkVersion"], 24);
                PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)minSdk;
            }

            // Target SDK Version
            if (@params["targetSdkVersion"] != null)
            {
                int targetSdk = ParamCoercion.CoerceInt(@params["targetSdkVersion"], 34);
                PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)targetSdk;
            }

            // Target Architectures
            if (@params["targetArchitectures"] is JArray archArray)
            {
                AndroidArchitecture arch = AndroidArchitecture.None;
                foreach (var item in archArray)
                {
                    string archStr = item.ToString();
                    if (archStr == "ARM64") arch |= AndroidArchitecture.ARM64;
                    else if (archStr == "ARMv7") arch |= AndroidArchitecture.ARMv7;
                    else if (archStr == "X86") arch |= AndroidArchitecture.X86;
                    else if (archStr == "X86_64")
                    {
                        // X86_64 not available in all Unity versions
                    }
                }
                if (arch != AndroidArchitecture.None)
                    PlayerSettings.Android.targetArchitectures = arch;
            }

            // Keystore Settings
            if (@params["keystorePath"] != null)
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = ParamCoercion.CoerceString(@params["keystorePath"], "");
            }
            if (@params["keystorePassword"] != null)
            {
                PlayerSettings.Android.keystorePass = ParamCoercion.CoerceString(@params["keystorePassword"], "");
            }
            if (@params["keyAliasName"] != null)
            {
                PlayerSettings.Android.keyaliasName = ParamCoercion.CoerceString(@params["keyAliasName"], "");
            }
            if (@params["keyAliasPassword"] != null)
            {
                PlayerSettings.Android.keyaliasPass = ParamCoercion.CoerceString(@params["keyAliasPassword"], "");
            }

            // Build App Bundle
            if (@params["buildAppBundle"] != null)
            {
                EditorUserBuildSettings.buildAppBundle = ParamCoercion.CoerceBool(@params["buildAppBundle"], false);
            }

            // Export as Android Project
            if (@params["exportAsGoogleAndroidProject"] != null)
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = 
                    ParamCoercion.CoerceBool(@params["exportAsGoogleAndroidProject"], false);
            }

            return new SuccessResponse("Android settings updated.", new
            {
                minSdkVersion = (int)PlayerSettings.Android.minSdkVersion,
                targetSdkVersion = (int)PlayerSettings.Android.targetSdkVersion,
                targetArchitectures = PlayerSettings.Android.targetArchitectures.ToString(),
                buildAppBundle = EditorUserBuildSettings.buildAppBundle
            });
        }

        #endregion

        #region iOS Settings

        private static object IOSGetSettings(JObject @params)
        {
            return new SuccessResponse("Current iOS settings.", new
            {
                targetOSVersionString = PlayerSettings.iOS.targetOSVersionString,
                appleDeveloperTeamID = PlayerSettings.iOS.appleDeveloperTeamID,
                appleEnableAutomaticSigning = PlayerSettings.iOS.appleEnableAutomaticSigning,
                iOSManualProvisioningProfileID = PlayerSettings.iOS.iOSManualProvisioningProfileID,
                iOSManualProvisioningProfileType = PlayerSettings.iOS.iOSManualProvisioningProfileType.ToString(),
                cameraUsageDescription = PlayerSettings.iOS.cameraUsageDescription,
                microphoneUsageDescription = PlayerSettings.iOS.microphoneUsageDescription,
                locationUsageDescription = PlayerSettings.iOS.locationUsageDescription,
                buildNumber = PlayerSettings.iOS.buildNumber,
                appInBackgroundBehavior = PlayerSettings.iOS.appInBackgroundBehavior.ToString(),
                targetDevice = PlayerSettings.iOS.targetDevice.ToString(),
                sdkVersion = PlayerSettings.iOS.sdkVersion.ToString()
            });
        }

        private static object IOSSetSettings(JObject @params)
        {
            // Target iOS Version
            if (@params["iOSTargetDeployment"] != null || @params["targetOSVersionString"] != null)
            {
                string version = ParamCoercion.CoerceString(
                    @params["iOSTargetDeployment"] ?? @params["targetOSVersionString"], 
                    "13.0");
                PlayerSettings.iOS.targetOSVersionString = version;
            }

            // Apple Developer Team ID
            if (@params["appleTeamId"] != null || @params["appleDeveloperTeamID"] != null)
            {
                string teamId = ParamCoercion.CoerceString(
                    @params["appleTeamId"] ?? @params["appleDeveloperTeamID"], 
                    "");
                PlayerSettings.iOS.appleDeveloperTeamID = teamId;
            }

            // Automatic Signing
            if (@params["automaticSigning"] != null)
            {
                PlayerSettings.iOS.appleEnableAutomaticSigning = 
                    ParamCoercion.CoerceBool(@params["automaticSigning"], true);
            }

            // Manual Provisioning Profile
            if (@params["iOSManualSigningProvisioningProfileID"] != null)
            {
                PlayerSettings.iOS.iOSManualProvisioningProfileID = 
                    ParamCoercion.CoerceString(@params["iOSManualSigningProvisioningProfileID"], "");
            }

            // Provisioning Profile Type
            if (@params["iOSManualSigningProvisioningProfileType"] != null)
            {
                string profileType = ParamCoercion.CoerceString(@params["iOSManualSigningProvisioningProfileType"], "Development");
                PlayerSettings.iOS.iOSManualProvisioningProfileType = profileType == "Distribution" 
                    ? ProvisioningProfileType.Distribution 
                    : ProvisioningProfileType.Development;
            }

            // Privacy Usage Descriptions
            if (@params["iOSCameraUsageDescription"] != null)
            {
                PlayerSettings.iOS.cameraUsageDescription = 
                    ParamCoercion.CoerceString(@params["iOSCameraUsageDescription"], "");
            }
            if (@params["iOSMicrophoneUsageDescription"] != null)
            {
                PlayerSettings.iOS.microphoneUsageDescription = 
                    ParamCoercion.CoerceString(@params["iOSMicrophoneUsageDescription"], "");
            }
            if (@params["iOSLocationUsageDescription"] != null)
            {
                PlayerSettings.iOS.locationUsageDescription = 
                    ParamCoercion.CoerceString(@params["iOSLocationUsageDescription"], "");
            }

            // Supported Orientations
            if (@params["supportedOrientations"] is JArray orientations)
            {
                UIOrientation defaultOrientation = UIOrientation.Portrait;
                bool portrait = false, portraitUpside = false, landscapeLeft = false, landscapeRight = false;
                
                foreach (var item in orientations)
                {
                    string orient = item.ToString();
                    if (orient == "Portrait") portrait = true;
                    else if (orient == "PortraitUpsideDown") portraitUpside = true;
                    else if (orient == "LandscapeLeft") landscapeLeft = true;
                    else if (orient == "LandscapeRight") landscapeRight = true;
                }

                if (portrait) defaultOrientation = UIOrientation.Portrait;
                else if (landscapeLeft) defaultOrientation = UIOrientation.LandscapeLeft;
                
                PlayerSettings.defaultInterfaceOrientation = defaultOrientation;
            }

            return new SuccessResponse("iOS settings updated.", new
            {
                targetOSVersionString = PlayerSettings.iOS.targetOSVersionString,
                appleDeveloperTeamID = PlayerSettings.iOS.appleDeveloperTeamID,
                appleEnableAutomaticSigning = PlayerSettings.iOS.appleEnableAutomaticSigning,
                cameraUsageDescription = PlayerSettings.iOS.cameraUsageDescription
            });
        }

        #endregion
    }
}
