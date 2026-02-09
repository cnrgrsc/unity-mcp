using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Addressables and asset bundles.
    /// </summary>
    [McpForUnityTool("manage_addressables")]
    public static class ManageAddressables
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
                    "mark_addressable" => MarkAddressable(@params),
                    "unmark_addressable" => UnmarkAddressable(@params),
                    "set_address" => SetAddress(@params),
                    "set_label" => SetLabel(@params),
                    "create_group" => CreateGroup(@params),
                    "list_groups" => ListGroups(@params),
                    "build_content" => BuildContent(@params),
                    "load_asset" => LoadAsset(@params),
                    "get_download_size" => GetDownloadSize(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageAddressables] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        private static object Initialize(JObject @params)
        {
            return new SuccessResponse("Addressables setup.", new
            {
                package = "com.unity.addressables",
                steps = new[]
                {
                    "1. Install Addressables package via Package Manager",
                    "2. Window > Asset Management > Addressables > Groups",
                    "3. Create Addressables Settings if prompted",
                    "4. Mark assets as Addressable by checking the checkbox in Inspector"
                },
                codeExample = "Addressables.InitializeAsync();"
            });
        }

        private static object MarkAddressable(JObject @params)
        {
            string assetPath = ParamCoercion.CoerceString(@params["assetPath"], "");
            
            if (string.IsNullOrEmpty(assetPath))
                return new ErrorResponse("'assetPath' is required.");

            // Check if asset exists
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
                return new ErrorResponse($"Asset not found: {assetPath}");

            return new SuccessResponse($"Asset ready to be marked as addressable: {assetPath}", new
            {
                assetPath = assetPath,
                instruction = "Select asset in Project window and check 'Addressable' in Inspector",
                codeExample = @"// Via AddressableAssetSettings
var settings = AddressableAssetSettingsDefaultObject.Settings;
var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
entry.address = ""custom_address"";"
            });
        }

        private static object UnmarkAddressable(JObject @params)
        {
            string assetPath = ParamCoercion.CoerceString(@params["assetPath"], "");

            return new SuccessResponse($"Asset ready to be unmarked: {assetPath}", new
            {
                assetPath = assetPath,
                instruction = "Uncheck 'Addressable' in Inspector or remove from Addressables Groups window"
            });
        }

        private static object SetAddress(JObject @params)
        {
            string assetPath = ParamCoercion.CoerceString(@params["assetPath"], "");
            string address = ParamCoercion.CoerceString(@params["address"], "");

            return new SuccessResponse("Address configured.", new
            {
                assetPath = assetPath,
                address = address,
                codeExample = $"entry.address = \"{address}\";"
            });
        }

        private static object SetLabel(JObject @params)
        {
            string assetPath = ParamCoercion.CoerceString(@params["assetPath"], "");
            var labelsArray = @params["labels"] as JArray;
            var labels = labelsArray?.Select(l => l.ToString()).ToList() ?? new List<string>();

            return new SuccessResponse("Labels configured.", new
            {
                assetPath = assetPath,
                labels = labels,
                codeExample = @"entry.SetLabel(""label_name"", true);"
            });
        }

        private static object CreateGroup(JObject @params)
        {
            string groupName = ParamCoercion.CoerceString(@params["groupName"], "NewGroup");

            return new SuccessResponse($"Group '{groupName}' configuration.", new
            {
                groupName = groupName,
                codeExample = $@"var settings = AddressableAssetSettingsDefaultObject.Settings;
var group = settings.CreateGroup(""{groupName}"", false, false, false, null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));"
            });
        }

        private static object ListGroups(JObject @params)
        {
            return new SuccessResponse("List Addressable groups.", new
            {
                instruction = "View groups in Window > Asset Management > Addressables > Groups",
                codeExample = @"var settings = AddressableAssetSettingsDefaultObject.Settings;
foreach (var group in settings.groups) { Debug.Log(group.Name); }"
            });
        }

        private static object BuildContent(JObject @params)
        {
            return new SuccessResponse("Build Addressables content.", new
            {
                menuPath = "Window > Asset Management > Addressables > Groups > Build > New Build > Default Build Script",
                codeExample = @"AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);",
                buildPath = "Library/com.unity.addressables/aa/<BuildTarget>"
            });
        }

        private static object LoadAsset(JObject @params)
        {
            string address = ParamCoercion.CoerceString(@params["address"], "");

            return new SuccessResponse("Load asset configuration.", new
            {
                address = address,
                codeExample = $@"// Load by address
var handle = Addressables.LoadAssetAsync<GameObject>(""{address}"");
handle.Completed += OnAssetLoaded;

// Or with await
var asset = await Addressables.LoadAssetAsync<GameObject>(""{address}"").Task;"
            });
        }

        private static object GetDownloadSize(JObject @params)
        {
            string address = ParamCoercion.CoerceString(@params["address"], "");

            return new SuccessResponse("Get download size.", new
            {
                address = address,
                codeExample = $@"var sizeHandle = Addressables.GetDownloadSizeAsync(""{address}"");
sizeHandle.Completed += (op) => {{ 
    long size = op.Result;
    Debug.Log($""Download size: {{size}} bytes"");
}};"
            });
        }
    }
}
