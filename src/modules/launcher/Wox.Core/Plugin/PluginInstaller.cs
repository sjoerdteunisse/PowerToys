﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Windows;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Wox.Plugin;

namespace Wox.Core.Plugin
{
    internal class PluginInstaller
    {
        internal static void Install(string path)
        {
            if (File.Exists(path))
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), "wox\\plugins");
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }

                UnZip(path, tempFolder, true);

                string iniPath = Path.Combine(tempFolder, "plugin.json");
                if (!File.Exists(iniPath))
                {
                    MessageBox.Show("Install failed: plugin config is missing");
                    return;
                }

                PluginMetadata plugin = GetMetadataFromJson(tempFolder);
                if (plugin == null || plugin.Name == null)
                {
                    MessageBox.Show("Install failed: plugin config is invalid");
                    return;
                }

                string pluginFolderPath = Infrastructure.Constant.PluginsDirectory;

                string newPluginName = plugin.Name
                    .Replace("/", "_")
                    .Replace("\\", "_")
                    .Replace(":", "_")
                    .Replace("<", "_")
                    .Replace(">", "_")
                    .Replace("?", "_")
                    .Replace("*", "_")
                    .Replace("|", "_")
                    + "-" + Guid.NewGuid();
                string newPluginPath = Path.Combine(pluginFolderPath, newPluginName);
                string content = $"Do you want to install following plugin?{Environment.NewLine}{Environment.NewLine}" +
                                 $"Name: {plugin.Name}{Environment.NewLine}" +
                                 $"Version: {plugin.Version}{Environment.NewLine}" +
                                 $"Author: {plugin.Author}";
                PluginPair existingPlugin = PluginManager.GetPluginForId(plugin.ID);

                if (existingPlugin != null)
                {
                    content = $"Do you want to update following plugin?{Environment.NewLine}{Environment.NewLine}" +
                              $"Name: {plugin.Name}{Environment.NewLine}" +
                              $"Old Version: {existingPlugin.Metadata.Version}" +
                              $"{Environment.NewLine}New Version: {plugin.Version}" +
                              $"{Environment.NewLine}Author: {plugin.Author}";
                }

                var result = MessageBox.Show(content, "Install plugin", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    if (existingPlugin != null && Directory.Exists(existingPlugin.Metadata.PluginDirectory))
                    {
                        // when plugin is in use, we can't delete them. That's why we need to make plugin folder a random name
                        File.Create(Path.Combine(existingPlugin.Metadata.PluginDirectory, "NeedDelete.txt")).Close();
                    }

                    UnZip(path, newPluginPath, true);
                    Directory.Delete(tempFolder, true);

                    // existing plugins could be loaded by the application,
                    // if we try to delete those kind of plugins, we will get a  error that indicate the
                    // file is been used now.
                    // current solution is to restart wox. Ugly.
                    // if (MainWindow.Initialized)
                    // {
                    //    Plugins.Initialize();
                    // }
                    if (MessageBox.Show($"You have installed plugin {plugin.Name} successfully.{Environment.NewLine} Restart Wox to take effect?", "Install plugin", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        PluginManager.API.RestartApp();
                    }
                }
            }
        }

        private static PluginMetadata GetMetadataFromJson(string pluginDirectory)
        {
            string configPath = Path.Combine(pluginDirectory, "plugin.json");
            PluginMetadata metadata;

            if (!File.Exists(configPath))
            {
                return null;
            }

            try
            {
                metadata = JsonConvert.DeserializeObject<PluginMetadata>(File.ReadAllText(configPath));
                metadata.PluginDirectory = pluginDirectory;
            }
            catch (Exception)
            {
                string error = $"Parse plugin config {configPath} failed: json format is not valid";
#if DEBUG
                {
                    throw new Exception(error);
                }
#else
                return null;
#endif
            }

            if (!AllowedLanguage.IsAllowed(metadata.Language))
            {
                string error = $"Parse plugin config {configPath} failed: invalid language {metadata.Language}";
#if DEBUG
                {
                    throw new Exception(error);
                }
#else
                return null;
#endif
            }

            if (!File.Exists(metadata.ExecuteFilePath))
            {
                string error = $"Parse plugin config {configPath} failed: ExecuteFile {metadata.ExecuteFilePath} didn't exist";
#if DEBUG
                {
                    throw new Exception(error);
                }
#else
                return null;
#endif
            }

            return metadata;
        }

        /// <summary>
        /// unzip
        /// </summary>
        /// <param name="zippedFile">The zipped file.</param>
        /// <param name="strDirectory">The STR directory.</param>
        /// <param name="overWrite">overwrite</param>
        private static void UnZip(string zippedFile, string strDirectory, bool overWrite)
        {
            if (strDirectory == string.Empty)
            {
                strDirectory = Directory.GetCurrentDirectory();
            }

            if (!strDirectory.EndsWith("\\"))
            {
                strDirectory += "\\";
            }

            using (ZipInputStream s = new ZipInputStream(File.OpenRead(zippedFile)))
            {
                ZipEntry theEntry;

                while ((theEntry = s.GetNextEntry()) != null)
                {
                    string directoryName = string.Empty;
                    string pathToZip = string.Empty;
                    pathToZip = theEntry.Name;

                    if (pathToZip != string.Empty)
                    {
                        directoryName = Path.GetDirectoryName(pathToZip) + "\\";
                    }

                    string fileName = Path.GetFileName(pathToZip);

                    Directory.CreateDirectory(strDirectory + directoryName);

                    if (fileName != string.Empty)
                    {
                        if ((File.Exists(strDirectory + directoryName + fileName) && overWrite) || (!File.Exists(strDirectory + directoryName + fileName)))
                        {
                            using (FileStream streamWriter = File.Create(strDirectory + directoryName + fileName))
                            {
                                byte[] data = new byte[2048];
                                while (true)
                                {
                                    int size = s.Read(data, 0, data.Length);

                                    if (size > 0)
                                    {
                                        streamWriter.Write(data, 0, size);
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                streamWriter.Close();
                            }
                        }
                    }
                }

                s.Close();
            }
        }
    }
}
