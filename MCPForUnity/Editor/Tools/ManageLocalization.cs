#if LOCALIZATION_ENABLED
using System;
using System.Collections.Generic;
using System.Linq;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing Unity Localization.
    /// Requires: com.unity.localization package + LOCALIZATION_ENABLED define
    /// </summary>
    [McpForUnityTool("manage_localization")]
    public static class ManageLocalization
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
                    "locale_list" => LocaleList(@params),
                    "locale_add" => LocaleAdd(@params),
                    "locale_set_default" => LocaleSetDefault(@params),
                    "table_create" => TableCreate(@params),
                    "table_get_entries" => TableGetEntries(@params),
                    "entry_add" => EntryAdd(@params),
                    "entry_update" => EntryUpdate(@params),
                    "entry_remove" => EntryRemove(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageLocalization] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        private static object LocaleList(JObject @params)
        {
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                return new ErrorResponse("Localization Settings not found. Create via Window > Asset Management > Localization Tables.");
            }

            var locales = settings.GetAvailableLocales().Locales;
            var localeList = locales.Select(l => new
            {
                code = l.Identifier.Code,
                name = l.LocaleName
            }).ToList();

            return new SuccessResponse($"Found {locales.Count} locales.", new
            {
                locales = localeList,
                defaultLocale = settings.GetSelectedLocale()?.Identifier.Code
            });
        }

        private static object LocaleAdd(JObject @params)
        {
            string code = ParamCoercion.CoerceString(@params["localeCode"], null);
            if (string.IsNullOrEmpty(code))
            {
                return new ErrorResponse("'localeCode' is required.");
            }

            var localeId = new LocaleIdentifier(code);
            var locale = Locale.CreateLocale(localeId);

            string path = $"Assets/Localization/Locales/{code}.asset";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(locale, path);

            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings != null)
            {
                settings.GetAvailableLocales().AddLocale(locale);
                EditorUtility.SetDirty(settings);
            }

            AssetDatabase.SaveAssets();

            return new SuccessResponse($"Locale '{code}' added.", new
            {
                code,
                path
            });
        }

        private static object LocaleSetDefault(JObject @params)
        {
            string code = ParamCoercion.CoerceString(@params["localeCode"], null);
            if (string.IsNullOrEmpty(code))
            {
                return new ErrorResponse("'localeCode' is required.");
            }

            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                return new ErrorResponse("Localization Settings not found.");
            }

            var locale = settings.GetAvailableLocales().GetLocale(new LocaleIdentifier(code));
            if (locale == null)
            {
                return new ErrorResponse($"Locale '{code}' not found.");
            }

            settings.SetSelectedLocale(locale);
            EditorUtility.SetDirty(settings);

            return new SuccessResponse($"Default locale set to '{code}'.", new
            {
                code,
                localeName = locale.LocaleName
            });
        }

        private static object TableCreate(JObject @params)
        {
            string tableName = ParamCoercion.CoerceString(@params["tableName"], null);
            if (string.IsNullOrEmpty(tableName))
            {
                return new ErrorResponse("'tableName' is required.");
            }

            var collection = LocalizationEditorSettings.CreateStringTableCollection(
                tableName,
                $"Assets/Localization/Tables"
            );

            AssetDatabase.SaveAssets();

            return new SuccessResponse($"String table '{tableName}' created.", new
            {
                tableName,
                path = AssetDatabase.GetAssetPath(collection)
            });
        }

        private static object TableGetEntries(JObject @params)
        {
            string tableName = ParamCoercion.CoerceString(@params["tableName"], null);
            if (string.IsNullOrEmpty(tableName))
            {
                return new ErrorResponse("'tableName' is required.");
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection == null)
            {
                return new ErrorResponse($"Table collection '{tableName}' not found.");
            }

            var entries = collection.SharedData.Entries.Select(e => new
            {
                key = e.Key,
                id = e.Id
            }).ToList();

            return new SuccessResponse($"Found {entries.Count} entries in '{tableName}'.", new
            {
                tableName,
                entries
            });
        }

        private static object EntryAdd(JObject @params)
        {
            string tableName = ParamCoercion.CoerceString(@params["tableName"], null);
            string key = ParamCoercion.CoerceString(@params["entryKey"], null);
            string value = ParamCoercion.CoerceString(@params["entryValue"], "");
            string localeCode = ParamCoercion.CoerceString(@params["localeCode"], "en");

            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(key))
            {
                return new ErrorResponse("'tableName' and 'entryKey' are required.");
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection == null)
            {
                return new ErrorResponse($"Table collection '{tableName}' not found.");
            }

            collection.SharedData.AddKey(key);

            var table = collection.GetTable(new LocaleIdentifier(localeCode)) as StringTable;
            if (table != null)
            {
                table.AddEntry(key, value);
                EditorUtility.SetDirty(table);
            }

            AssetDatabase.SaveAssets();

            return new SuccessResponse($"Entry '{key}' added to '{tableName}'.", new
            {
                tableName,
                key,
                value,
                locale = localeCode
            });
        }

        private static object EntryUpdate(JObject @params)
        {
            string tableName = ParamCoercion.CoerceString(@params["tableName"], null);
            string key = ParamCoercion.CoerceString(@params["entryKey"], null);
            string value = ParamCoercion.CoerceString(@params["entryValue"], "");
            string localeCode = ParamCoercion.CoerceString(@params["localeCode"], "en");

            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(key))
            {
                return new ErrorResponse("'tableName' and 'entryKey' are required.");
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection == null)
            {
                return new ErrorResponse($"Table collection '{tableName}' not found.");
            }

            var table = collection.GetTable(new LocaleIdentifier(localeCode)) as StringTable;
            if (table == null)
            {
                return new ErrorResponse($"Table for locale '{localeCode}' not found.");
            }

            var entry = table.GetEntry(key);
            if (entry == null)
            {
                return new ErrorResponse($"Entry '{key}' not found.");
            }

            entry.Value = value;
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"Entry '{key}' updated.", new
            {
                tableName,
                key,
                value,
                locale = localeCode
            });
        }

        private static object EntryRemove(JObject @params)
        {
            string tableName = ParamCoercion.CoerceString(@params["tableName"], null);
            string key = ParamCoercion.CoerceString(@params["entryKey"], null);

            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(key))
            {
                return new ErrorResponse("'tableName' and 'entryKey' are required.");
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
            if (collection == null)
            {
                return new ErrorResponse($"Table collection '{tableName}' not found.");
            }

            collection.SharedData.RemoveKey(key);
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();

            return new SuccessResponse($"Entry '{key}' removed from '{tableName}'.", new
            {
                tableName,
                key
            });
        }
    }
}
#else
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;

namespace MCPForUnity.Editor.Tools
{
    [McpForUnityTool("manage_localization")]
    public static class ManageLocalization
    {
        public static object HandleCommand(JObject @params)
        {
            return new ErrorResponse(
                "Localization not installed. Install 'com.unity.localization' and add 'LOCALIZATION_ENABLED' define."
            );
        }
    }
}
#endif
