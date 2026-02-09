using System;
using System.Diagnostics;
using System.IO;
using MCPForUnity.Editor.Helpers;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MCPForUnity.Editor.Tools
{
    /// <summary>
    /// Tool for managing version control (Git, Plastic SCM).
    /// </summary>
    [McpForUnityTool("manage_vcs")]
    public static class ManageVCS
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
                    "git_status" => GitStatus(@params),
                    "git_add" => GitAdd(@params),
                    "git_commit" => GitCommit(@params),
                    "git_push" => GitPush(@params),
                    "git_pull" => GitPull(@params),
                    "git_branch_list" => GitBranchList(@params),
                    "git_branch_create" => GitBranchCreate(@params),
                    "git_branch_switch" => GitBranchSwitch(@params),
                    "git_log" => GitLog(@params),
                    "set_serialization" => SetSerialization(@params),
                    "set_line_endings" => SetLineEndings(@params),
                    "generate_gitignore" => GenerateGitignore(@params),
                    "set_vcs_mode" => SetVcsMode(@params),
                    _ => new ErrorResponse($"Unknown action: '{action}'.")
                };
            }
            catch (Exception e)
            {
                McpLog.Error($"[ManageVCS] Action '{action}' failed: {e}");
                return new ErrorResponse($"Error: {e.Message}");
            }
        }

        private static string RunGitCommand(string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = Application.dataPath.Replace("/Assets", ""),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return string.IsNullOrEmpty(error) ? output : $"{output}\n{error}";
            }
            catch (Exception e)
            {
                return $"Git command failed: {e.Message}";
            }
        }

        private static object GitStatus(JObject @params)
        {
            string result = RunGitCommand("status --short");
            return new SuccessResponse("Git status.", new
            {
                output = result,
                workingDirectory = Application.dataPath.Replace("/Assets", "")
            });
        }

        private static object GitAdd(JObject @params)
        {
            var filesArray = @params["files"] as JArray;
            string files = ".";
            
            if (filesArray != null && filesArray.Count > 0)
            {
                files = string.Join(" ", filesArray);
            }

            string result = RunGitCommand($"add {files}");
            return new SuccessResponse("Files staged.", new
            {
                files = files,
                output = result
            });
        }

        private static object GitCommit(JObject @params)
        {
            string message = ParamCoercion.CoerceString(@params["message"], "Update");
            
            string result = RunGitCommand($"commit -m \"{message}\"");
            return new SuccessResponse("Commit created.", new
            {
                message = message,
                output = result
            });
        }

        private static object GitPush(JObject @params)
        {
            string remote = ParamCoercion.CoerceString(@params["remote"], "origin");
            string branch = ParamCoercion.CoerceString(@params["branchName"], "");
            
            string pushCommand = string.IsNullOrEmpty(branch) ? $"push {remote}" : $"push {remote} {branch}";
            string result = RunGitCommand(pushCommand);
            return new SuccessResponse("Push completed.", new
            {
                remote = remote,
                output = result
            });
        }

        private static object GitPull(JObject @params)
        {
            string remote = ParamCoercion.CoerceString(@params["remote"], "origin");
            
            string result = RunGitCommand($"pull {remote}");
            return new SuccessResponse("Pull completed.", new
            {
                remote = remote,
                output = result
            });
        }

        private static object GitBranchList(JObject @params)
        {
            string result = RunGitCommand("branch -a");
            return new SuccessResponse("Branch list.", new
            {
                branches = result.Trim().Split('\n')
            });
        }

        private static object GitBranchCreate(JObject @params)
        {
            string branchName = ParamCoercion.CoerceString(@params["branchName"], "");
            
            if (string.IsNullOrEmpty(branchName))
                return new ErrorResponse("'branchName' is required.");

            string result = RunGitCommand($"checkout -b {branchName}");
            return new SuccessResponse($"Branch '{branchName}' created.", new
            {
                branchName = branchName,
                output = result
            });
        }

        private static object GitBranchSwitch(JObject @params)
        {
            string branchName = ParamCoercion.CoerceString(@params["branchName"], "");
            
            if (string.IsNullOrEmpty(branchName))
                return new ErrorResponse("'branchName' is required.");

            string result = RunGitCommand($"checkout {branchName}");
            return new SuccessResponse($"Switched to branch '{branchName}'.", new
            {
                branchName = branchName,
                output = result
            });
        }

        private static object GitLog(JObject @params)
        {
            int logCount = ParamCoercion.CoerceInt(@params["logCount"], 10);
            
            string result = RunGitCommand($"log --oneline -n {logCount}");
            return new SuccessResponse("Git log.", new
            {
                count = logCount,
                commits = result.Trim().Split('\n')
            });
        }

        private static object SetSerialization(JObject @params)
        {
            string mode = ParamCoercion.CoerceString(@params["serializationMode"], "ForceText");
            
            SerializationMode serializationMode = mode switch
            {
                "Mixed" => SerializationMode.Mixed,
                "ForceBinary" => SerializationMode.ForceBinary,
                "ForceText" => SerializationMode.ForceText,
                _ => SerializationMode.ForceText
            };

            EditorSettings.serializationMode = serializationMode;

            return new SuccessResponse($"Serialization mode set to {mode}.", new
            {
                mode = serializationMode.ToString(),
                recommendation = "ForceText is recommended for version control"
            });
        }

        private static object SetLineEndings(JObject @params)
        {
            string mode = ParamCoercion.CoerceString(@params["lineEndings"], "Unix");

            LineEndingsMode lineEndingsMode = mode switch
            {
                "OSNative" => LineEndingsMode.OSNative,
                "Unix" => LineEndingsMode.Unix,
                "Windows" => LineEndingsMode.Windows,
                _ => LineEndingsMode.Unix
            };

            EditorSettings.lineEndingsForNewScripts = lineEndingsMode;

            return new SuccessResponse($"Line endings set to {mode}.", new
            {
                mode = lineEndingsMode.ToString()
            });
        }

        private static object GenerateGitignore(JObject @params)
        {
            string gitignoreContent = @"# Unity generated
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/

# MemoryCaptures
[Mm]emoryCaptures/

# Asset meta data
*.pidb.meta
*.pdb.meta
*.mdb.meta

# Unity3D generated folders
Logs/
[Aa]ssets/AssetStoreTools*

# Autogenerated VS/MD/Rider solution and project files
ExportedObj/
.consulo/
*.csproj
*.unityproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# Unity3D generated files
*.pidb.meta
*.pdb.meta
*.mdb.meta

# Builds
*.apk
*.aab
*.unitypackage
*.app

# Crashlytics
crashlytics-build.properties

# Packed Addressables
/[Aa]ssets/[Aa]ddressable[Aa]ssets[Dd]ata/*/*.bin*

# Temporary
/[Aa]ssets/[Ss]treaming[Aa]ssets/aa.meta
/[Aa]ssets/[Ss]treaming[Aa]ssets/aa/*

# JetBrains Rider
.idea/

# Visual Studio Code
.vscode/

# OS generated
.DS_Store
.DS_Store?
._*
.Spotlight-V100
.Trashes
ehthumbs.db
Thumbs.db
";

            string projectPath = Application.dataPath.Replace("/Assets", "");
            string gitignorePath = Path.Combine(projectPath, ".gitignore");

            File.WriteAllText(gitignorePath, gitignoreContent);

            return new SuccessResponse(".gitignore generated.", new
            {
                path = gitignorePath,
                size = gitignoreContent.Length
            });
        }

        private static object SetVcsMode(JObject @params)
        {
            string mode = ParamCoercion.CoerceString(@params["vcsMode"], "Visible");

            return new SuccessResponse($"VCS mode info.", new
            {
                mode = mode,
                instruction = "Set in Edit > Project Settings > Editor > Version Control Mode",
                options = new[] { "Hidden", "Visible Meta Files", "Perforce", "Plastic SCM" }
            });
        }
    }
}
