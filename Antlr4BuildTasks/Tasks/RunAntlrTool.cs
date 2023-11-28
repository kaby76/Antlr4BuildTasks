// Forked and highly modified from sources at https://github.com/tunnelvisionlabs/antlr4cs/tree/master/runtime/CSharp/Antlr4BuildTasks
// Copyright 2022 Ken Domino, MIT License.

// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Antlr4.Build.Tasks.Util;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;
using StringBuilder = System.Text.StringBuilder;


namespace Antlr4.Build.Tasks
{
    public class RunAntlrTool : Task
    {
        private const string _toolVersion = "10.6.0";
        private const string _defaultGeneratedSourceExtension = "g4";
        private List<string> _generatedCodeFiles = new List<string>();
        private List<string> _generatedDirectories = new List<string>();
        private List<string> _generatedFiles = new List<string>();
        private bool _start = false;
        private StringBuilder _sb = new StringBuilder();
        // See https://www.oracle.com/java/technologies/downloads/
        // https://adoptopenjdk.net/archive.html
        class tableEntry { public string version; public string os; public string link; public string outdir; }
        private List<tableEntry> _tableOfJava = new List<tableEntry>()
        {
            new tableEntry { version = "11", os = "Linux x64", link = "https://github.com/adoptium/temurin11-binaries/releases/download/jdk-11.0.15%2B10/OpenJDK11U-jre_x64_linux_hotspot_11.0.15_10.tar.gz", outdir = "jdk-11.0.15+10-jre" },
            new tableEntry { version = "11", os = "Windows x64", link = "https://github.com/adoptium/temurin11-binaries/releases/download/jdk-11.0.15%2B10/OpenJDK11U-jre_x64_windows_hotspot_11.0.15_10.zip", outdir = "jdk-11.0.15+10-jre" },
            new tableEntry { version = "11", os = "MacOSX x64", link = "https://github.com/adoptium/temurin11-binaries/releases/download/jdk-11.0.15%2B10/OpenJDK11U-jre_x64_mac_hotspot_11.0.15_10.tar.gz", outdir = "jdk-11.0.15+10-jre" },
            new tableEntry { version = "11", os = "Linux aarch64", link = "https://github.com/adoptium/temurin11-binaries/releases/download/jdk-11.0.15%2B10/OpenJDK11U-jre_aarch64_linux_hotspot_11.0.15_10.tar.gz", outdir = "jdk-11.0.15+10-jre" },
            new tableEntry { version = "11", os = "Linux s390x", link = "https://github.com/adoptium/temurin11-binaries/releases/download/jdk-11.0.15%2B10/OpenJDK11U-jre_s390x_linux_hotspot_11.0.15_10.tar.gz", outdir = "jdk-11.0.15+10-jre" },
            new tableEntry { version = "11", os = "Windows x86", link = "https://github.com/adoptium/temurin11-binaries/releases/download/jdk-11.0.15%2B10/OpenJDK11U-jre_x86-32_windows_hotspot_11.0.15_10.zip", outdir = "jdk-11.0.15+10-jre" },
        };

        public bool AllowAntlr4cs { get; set; }
        public string AntlrToolJar { get; set; }
        public string AntlrToolJarDownloadDir { get; set; }
        public string AntOutDir { get; set; }
        public ITaskItem[] AntlrProbePath { get; set; }
        public string DOptions { get; set; }
        public string Encoding { get; set; }
        public bool Error { get; set; }
        public bool ForceAtn { get; set; }
        public bool GAtn { get; set; }
        [Output] public ITaskItem[] GeneratedCodeFiles
        {
            get
            {
                return this._generatedCodeFiles
                    .Select(t => t.Replace("\\", "/"))
                    .Distinct()
                    .OrderBy(q => q)
                    .Select(t => new TaskItem(t)).ToArray();
            }
        }
        [Output] public ITaskItem[] GeneratedFiles
        {
            get
            {
                return this._generatedFiles
                  .Select(t => t.Replace("\\", "/"))
                  .Distinct()
                  .OrderBy(q => q)
                  .Select(t => new TaskItem(t)).ToArray();
            }
            set { this._generatedFiles = value.Select(t => t.ItemSpec).ToList(); }
        }
        [Output] public ITaskItem[] GeneratedDirectories
        {
            get
            {
                return this._generatedDirectories
                   .Select(t => t.Replace("\\", "/"))
                  .Distinct()
                  .OrderBy(q => q)
                  .Select(t => new TaskItem(t)).ToArray();
            }
            set { this._generatedDirectories = value.Select(t => t.ItemSpec).ToList(); }
        }
        [Output] public string GeneratedSourceExtension { get; set; }
        [Required] public string IntermediateOutputPath { get; set; }
        public string JavaDownloadDirectory { get; set; }
        public string JavaExec { get; set; }
        public string LibPath { get; set; }
        public bool Listener { get; set; }
        public bool Log_ { get; set; }
        public bool LongMessages { get; set; }
        public List<string> OtherSourceCodeFiles { get; set; }
        public string Package { get; set; }
        public ITaskItem[] PackageReference { get; set; }
        public ITaskItem[] PackageVersion { get; set; }
        public ITaskItem[] SourceCodeFiles { get; set; }
        public string TargetFrameworkVersion { get; set; }
        public ITaskItem[] TargetFrameworks { get; set; }
        public ITaskItem[] TokensFiles { get; set; }
        public string Version { get; set; }
        public string VersionOfJava { get; set; } = "11";
        public bool Visitor { get; set; }

        public async System.Threading.Tasks.Task DownloadFileAsync(string uri, string outputPath)
        {
            var client = new System.Net.Http.HttpClient();
            var response = await client.GetAsync(uri);
            var fs = new FileStream(outputPath, FileMode.CreateNew);
            await response.Content.CopyToAsync(fs);
        }
        
        public RunAntlrTool()
        {
            this.GeneratedSourceExtension = _defaultGeneratedSourceExtension;
        }

        public override bool Execute()
        {
            bool success = false;
            //System.Threading.Thread.Sleep(20000);
            try
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Starting Antlr4 Build Tasks."));
                if (IntermediateOutputPath != null) IntermediateOutputPath = Path.GetFullPath(IntermediateOutputPath);
                if (AntOutDir != null) AntOutDir = Path.GetFullPath(AntOutDir);
                if (AntOutDir == null || AntOutDir == "")
                {
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Placing generated files in IntermediateOutputPath " + IntermediateOutputPath));
                    AntOutDir = IntermediateOutputPath;
                }
                Directory.CreateDirectory(AntOutDir);

                AntlrToolJar = SetupAntlrToolJar();
                if (!File.Exists(AntlrToolJar))
                    throw new Exception("Cannot find Antlr tool jar, currently set to " + "'" + AntlrToolJar + "'");
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("AntlrToolJar is \"" + AntlrToolJar + "\""));

                JavaExec = SetupJava();
                if (!File.Exists(JavaExec))
                    throw new Exception("Cannot find Java executable, currently set to " + "'" + JavaExec + "'");
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("JavaExec is \"" + JavaExec + "\""));

                success = GetGeneratedFileNameList()
                    && GenerateFiles(out success);
            }
            catch (Exception exception)
            {
                ProcessExceptionAsBuildMessage(exception);
                success = false;
            }
            finally
            {
                if (!success)
                {
                    MessageQueue.EnqueueMessage(Message.BuildErrorMessage("The Antlr4 tool failed."));
                    MessageQueue.MutateToError();
                    _generatedCodeFiles.Clear();
                    _generatedFiles.Clear();
                    _generatedDirectories.Clear();
                }
                MessageQueue.EmptyMessageQueue(Log);
            }
            return success;
        }

        private string SetupAntlrToolJar()
        {
            if (AntlrToolJar != null && AntlrToolJar != "")
                return AntlrToolJar;

            string result = null;

            // Make sure old crusty Tunnelvision port not being used.
            if (!this.AllowAntlr4cs)
            {
                foreach (var i in PackageReference)
                {
                    if (i.ItemSpec.ToLower() == "Antlr4.Runtime".ToLower())
                    {
                        throw new Exception(
                            @"You are referencing Antlr4.Runtime in your .csproj file. This build tool can only reference the NET Standard library https://www.nuget.org/packages/Antlr4.Runtime.Standard/. You can only use either the 'official' Antlr4 or the 'tunnelvision' fork, but not both. You have to choose one.");
                    }
                }
            }

            // Get version
            string version = null;
            bool reference_standard_runtime = false;
            foreach (var i in PackageReference)
            {
                if (i.ItemSpec.ToLower() == "Antlr4.Runtime.Standard".ToLower())
                {
                    reference_standard_runtime = true;
                    version = i.GetMetadata("Version");
                    if (version == null || version.Trim() == "")
                    {
                        foreach (var j in PackageVersion)
                        {
                            if (j.ItemSpec.ToLower() == "Antlr4.Runtime.Standard".ToLower())
                            {
                                reference_standard_runtime = true;
                                version = j.GetMetadata("Version");
                                break;
                            }
                        }
                    }
                    break;
                }
            }
            if (version == null)
            {
                if (reference_standard_runtime)
                    throw new Exception(
                        @"Antlr4BuildTasks cannot identify the version number you are referencing. Check the Version parameter for Antlr4.Runtime.Standard.");
                else
                    throw new Exception(
                        @"You are not referencing Antlr4.Runtime.Standard in your .csproj file. Antlr4BuildTasks requires a reference to it in order
to identify which version of the Antlr Java tool to run to generate the parser and lexer.");
            }
            else if (version.Trim() == "")
            {
                throw new Exception(
                    @"Antlr4BuildTasks cannot determine the version of Antlr4.Runtime.Standard. It's ''!.
version = '" + version + @"'
PackageReference = '" + PackageReference.ToString() + @"'
PackageVersion = '" + PackageVersion.ToString() + @"
");
            }
            Version = version;
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                @"Antlr4BuildTasks identified that you are looking for version "
                + Version
                + " of the Antlr4 tool jar."));

            Regex r2 = new Regex("^(?<TWOVERSION>[0-9]+[.][0-9]+)$");
            var m2 = r2.Match(Version);
            var v2 = m2.Success && m2.Groups["TWOVERSION"].Length > 0 ? m2.Groups["TWOVERSION"].Value : null;

            Regex r3 = new Regex("^(?<THREEVERSION>[0-9]+[.][0-9]+[.][0-9]+)$");
            var m3 = r3.Match(Version);
            var v3 = m3.Success && m3.Groups["THREEVERSION"].Length > 0 ? m3.Groups["THREEVERSION"].Value : null;

            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                "Version '"
                + Version
                + "' v2 match='"
                + v2
                + "', v3 match='"
                + v3
                + "'"));

            List<string> paths = new List<string>();
            if (AntlrProbePath != null)
                    paths = AntlrProbePath.Select(p => p.ItemSpec).ToList();

            var path = "";
            // Set up probe path for Antlr tool jar if there isn't one.
            if (AntlrToolJarDownloadDir.Contains("USERPROFILE"))
            {
                string user_profile_path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace("\\", "/");
                if (user_profile_path.EndsWith("/")) user_profile_path = user_profile_path.Substring(1, user_profile_path.Length - 1);
                path = AntlrToolJarDownloadDir.Replace("USERPROFILE", user_profile_path).Replace("\\", "/");
            }
            else
            {
                path = AntlrToolJarDownloadDir.Replace("\\", "/");
            }
            
            if (!path.EndsWith("/")) path = path + "/";
            
            
            string tool_path = path + ".nuget/packages/antlr4buildtasks/" + _toolVersion + "/";

            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                "Location to stuff Antlr tool jar, if not found, is " + path));

            if (paths == null || paths.Count == 0)
            {
                string package_area = "file:///" + path;
                paths.Add(package_area);
                var full_path = "file:///" + Path.GetFullPath(IntermediateOutputPath);
                paths.Add(full_path);
                paths.Add("https://repo1.maven.org/maven2/org/antlr/antlr4/");
            }

            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Paths to search for Antlr4 jar, in order, are: "
                + String.Join(";", paths)));

            foreach (var probe in paths)
            {
                // Version can be "x.y.z" or "x.y".
                // If "x.y.z", probe for "x.y.z".
                // If "x.y.0", probe for "x.y".
                // If "x.y", probe for "x.y".
                if (v3 != null)
                {
                    bool t = TryProbeAntlrJar(probe, v3, path, out string w);
                    if (t)
                    {
                        result = w;
                        break;
                    }
                }
                else if (v3 != null && v3.EndsWith(".0"))
                {
                    bool t = TryProbeAntlrJar(probe, v3.Substring(0, v3.Length - 2), path, out string w);
                    if (t)
                    {
                        result = w;
                        break;
                    }
                }
                else if (v2 != null)
                {
                    bool t = TryProbeAntlrJar(probe, v2, path, out string w);
                    if (t)
                    {
                        result = w;
                        break;
                    }
                }
            }
            if (result == null || result == "")
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                    @"Went through the complete probe list looking for an Antlr4 tool jar, but could not find anything. Fail!"));
            }
            if (!File.Exists(result))
                throw new Exception("Cannot find Antlr4 jar file, currently set to "
                                    + "'" + result + "'");
            return result;
        }

        private bool TryProbeAntlrJar(string path, string version, string place_path, out string where)
        {
            bool result = false;
            where = null;
            path = path.Trim();
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("path is " + path));
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("place_path is " + place_path));
            if (!(path.EndsWith("/") || path.EndsWith("\\"))) path = path + "/";
            if (path.StartsWith("file://"))
            {
                var f = path + @"antlr4-" + version + @"-complete.jar";
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Probing " + f));
                try
                {
                    System.Uri uri = new Uri(f);
                    var local_file = uri.LocalPath;
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Local path " + local_file));
                    if (File.Exists(local_file))
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found."));
                        where = local_file;
                        result = true;
                    }
                }
                catch
                {
                }
            }
            else if (path == "https://repo1.maven.org/maven2/org/antlr/antlr4/")
            {
                var j = path + version + @"/antlr4-" + version + @"-complete.jar";
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Probing " + j));
                try
                {
                    var archive_name = place_path + System.IO.Path.GetFileName(j);
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("archive_name is " + archive_name));
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("place_path is " + place_path));
                    var jar_dir = place_path;
                    System.IO.Directory.CreateDirectory(jar_dir);
                    if (!File.Exists(archive_name))
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Downloading " + j));
                        DownloadFileAsync(j, archive_name).Wait(new TimeSpan(0, 3, 0));
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("archive_name is " + archive_name));
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("place_path is " + place_path));
                    }
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found "
                        + archive_name));
                    where = archive_name;
                    result = true;
                }
                catch
                {
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                        "Problem downloading or saving probed file."));
                }
            }
            else if (path.StartsWith("https://") || path.StartsWith("http://"))
            {
                var j = path + @"antlr4-" + version + @"-complete.jar";
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Probing " + j));
                try
                {
                    var archive_name = place_path
                        + System.IO.Path.GetFileName(j);
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("archive_name is " + archive_name));
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("place_path is " + place_path));
                    var jar_dir = place_path;
                    System.IO.Directory.CreateDirectory(jar_dir);
                    if (!File.Exists(archive_name))
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Downloading " + j));
                        DownloadFileAsync(j, archive_name).Wait(new TimeSpan(0, 3, 0));
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("archive_name is " + archive_name));
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("place_path is " + place_path));
                    }
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found. Saving to "
                        + archive_name));
                    where = archive_name;
                    result = true;
                }
                catch
                {
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                        "Problem downloading or saving probed file."));
                }
            }
            else
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                    @"The AntlrProbePath contains '"
                        + path
                        + "', which doesn't start with 'file://' or 'https://'. "
                        + @"Edit your .csproj file to make sure the path follows that syntax."));
            }
            return result;
        }

        public string SetupJava()
        {
            string result = null;
            string user_profile_path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace("\\", "/");
            if (user_profile_path.EndsWith("/")) user_profile_path = user_profile_path.Substring(1, user_profile_path.Length - 1);

            // Replace USERPROFILE in various input to tool.
            var java_download_directory = JavaDownloadDirectory.Replace("USERPROFILE", user_profile_path);
            var java_exec = JavaExec.Replace("USERPROFILE", user_profile_path);

            // Split up probe path, which is a combination of different paths.
            List<string> paths = new List<string>();
            paths = java_exec.Split(';').ToList();

            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Search path for java (JavaExec): " + java_exec));
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Download area for JRE (JavaDownloadDirectory): " + java_download_directory));
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Paths to search for the java executable, in order, are: " + String.Join(";", paths)));

            foreach (var try_path in paths)
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("probing java executable at " + try_path));
                if (TryJava(try_path, java_download_directory, out string where))
                {
                    if (where == null || where == "")
                    {
                        throw new Exception(
                            @"TryProbeJava returned an empty path, should not happen.");
                    }
                    else
                    {
                        return where;
                    }
                }
            }
            if (!File.Exists(result))
                throw new Exception("Cannot find Java executable"
                    + (result != null ? "'" + result + "'" : "''"));
            return result;
        }

        private bool TryJava(string try_path, string place_path, out string where)
        {
            bool result = false;
            where = null;
            try_path = try_path.Trim();
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Path to try is " + try_path));
            if (try_path == "PATH")
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Trying " + try_path));
                var executable_name = (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                    || System.Environment.OSVersion.Platform == PlatformID.Win32S
                    || System.Environment.OSVersion.Platform == PlatformID.Win32Windows) ? "java.exe" : "java";
                var locations_on_path = SearchEnvPathForProgram(executable_name);
                foreach (var location_on_path in locations_on_path)
                {
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("found a java executable at " + location_on_path));
                    string w = (!Path.IsPathRooted(location_on_path)) ? Path.GetFullPath(location_on_path)
                        : location_on_path;
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("w = " + w));
                    // Try java.
                    ProcessStartInfo startInfo = new ProcessStartInfo(
                        w, "-version")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                        "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments));
                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.ErrorDataReceived += TestStderrDataReceived;
                    process.OutputDataReceived += TestStdoutDataReceived;
                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.StandardInput.Dispose();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("java found at '" + w + "', but it doesn't work."));
                        continue;
                    }
                    else if (!good_version)
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("java at '" + w + "', but not a good version."));
                        continue;
                    }
                    else
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("java found: '" + w + "'"));
                        where = w;
                        return true;
                    }
                }
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage(executable_name + " not found on path"));
                return false;
            }
            else if (try_path == "DOWNLOAD")
            {
                try
                {
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Probing " + try_path));

                    // First look at the downloads directory
                    JavaDownloadDirectory = JavaDownloadDirectory.Replace('\\', '/');
                    if (!JavaDownloadDirectory.EndsWith("/")) JavaDownloadDirectory = JavaDownloadDirectory + "/";
                    var executable_name = (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                                   || System.Environment.OSVersion.Platform == PlatformID.Win32S
                                   || System.Environment.OSVersion.Platform == PlatformID.Win32Windows) ? "java.exe" : "java";
                    try_path = JavaDownloadDirectory + ".*/" + executable_name;
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Trying pattern " + try_path));
                    var locations_on_path = new Domemtech.Globbing.Glob(JavaDownloadDirectory)
                                .RegexContents()
                                .Where(f => f is FileInfo)
                                .Select(f => f.FullName)
                                .Select(f => f.Replace("\\", "/"))
                                .ToList();
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Got a list of " + locations_on_path.Count() + " items."));
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("List = " + String.Join(" ", locations_on_path)));
                    locations_on_path = locations_on_path
                                .Where(f => new Regex(try_path).Match(f).Success)
                                .ToList();
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Got a list of " + locations_on_path.Count() + " items."));
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("List = " + String.Join(" ", locations_on_path)));
                    foreach (var loc in locations_on_path)
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Trying " + loc));
                        try
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo(
                                loc, "--version")
                            {
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardInput = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                            };
                            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                                "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments));
                            Process process = new Process();
                            process.StartInfo = startInfo;
                            process.ErrorDataReceived += TestStderrDataReceived;
                            process.OutputDataReceived += TestStdoutDataReceived;
                            process.Start();
                            process.BeginErrorReadLine();
                            process.BeginOutputReadLine();
                            process.StandardInput.Dispose();
                            process.WaitForExit();
                            if (process.ExitCode != 0)
                            {
                                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("java found at '" + loc + "', but it doesn't work."));
                                return false;
                            }
                            else
                            {
                                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("java found: '" + loc + "'"));
                                where = loc;
                                return true;
                            }
                        }
                        catch (Exception)
                        {
                        }
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("doesn't work."));
                    }
                }
                catch (Exception)
                {
                }

                // Get OS and native type.
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("os_arch str is " + ConvertOSArch()));
                string java_download_fn = null;
                string java_download_url = null;
                var which_java = _tableOfJava.Where(e => e.os == ConvertOSArch()).FirstOrDefault();

                if (which_java == default(tableEntry))
                    return false;

                if (which_java.link.EndsWith(".zip"))
                {
                    // Make sure multiple targets are not being used.
                    if (true)
                    {
                        if (TargetFrameworks != null)
                        {
                            var count = TargetFrameworks.Count();
                            if (count > 1)
                            {
                                throw new Exception(
                                    @"Multiple TargetFrameworks is not supported with auto downloading of JRE, Issue #48. Install Java, and set up PATH to include a it.");
                            }
                        }
                    }

                    var ok = Locker.Grab();
                    if (!ok) return false;
                    try
                    {
                        java_download_fn = which_java.link.Substring(which_java.link.LastIndexOf('/') + 1);
                        java_download_url = which_java.link;
                        string uncompressed_root_dir = JavaDownloadFile(place_path, java_download_fn, java_download_url);
                        if (uncompressed_root_dir == null || uncompressed_root_dir == "")
                        {
                            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Problem in downloading JRE"));
                            where = null;
                            return false;
                        }
                        where = uncompressed_root_dir + which_java.outdir + "/bin/java.exe";
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Downloaded JRE. Testing for java executable at " + where));
                        // https://github.com/kaby76/Antlr4BuildTasks/issues/51
                        // Do not delete the uncompressed root directory for JRE.
                        // For mullti-targets, the directory is listed in each target.
                        // Can't delete multiple times. Besides, it's intended to be
                        // shared with antlr4.py https://github.com/antlr/antlr4-tools
                        // _generatedDirectories.Add(uncompressed_root_dir);
                        var archive_name = place_path + java_download_fn;
                        if (!File.Exists(where))
                        {
                            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(where + " does not seem to exist. Decompressing."));
                            var r = DecompressJava(uncompressed_root_dir, archive_name);
                            if (!r)
                            {
                                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Problem in decompressing JRE"));
                                where = null;
                                return false;
                            }
                            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found java at " + where));
                            return true;
                        }
                        else
                        {
                            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found java at " + where));
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        Locker.Release();
                    }
                }
                else if (which_java.link.EndsWith(".tar.gz"))
                {
                    // Make sure multiple targets are not being used.
                    if (true)
                    {
                        if (TargetFrameworks != null)
                        {
                            var count = TargetFrameworks.Count();
                            if (count > 1)
                            {
                                throw new Exception(
                                    @"Multiple TargetFrameworks is not supported with auto downloading of JRE, Issue #48. Install Java, and set up PATH to include a it.");
                            }
                        }
                    }

                    var ok = Locker.Grab();
                    if (!ok) return false;
                    try
                    {
                        java_download_fn = which_java.link.Substring(which_java.link.LastIndexOf('/') + 1);
                        java_download_url = which_java.link;
                        try
                        {
                            string uncompressed_root_dir = JavaDownloadFile(place_path, java_download_fn, java_download_url);
                            where = uncompressed_root_dir + which_java.outdir
                                + (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/bin/java"
                                : System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "/Contents/Home/bin/java" : "java");
                            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Java should be here " + where));
                            _generatedDirectories.Add(uncompressed_root_dir);
                            var archive_name = place_path + java_download_fn;
                            if (!File.Exists(where))
                            {
                                lock ("")
                                {
                                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Decompressing"));
                                    System.IO.Directory.CreateDirectory(uncompressed_root_dir);
                                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("created " + uncompressed_root_dir));
                                    Read(uncompressed_root_dir, archive_name, new CompressionType());
                                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Finished Decompressing"));
                                }
                            }
                            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found."));
                            return true;
                        }
                        catch
                        {
                            where = null;
                            return false;
                        }
                    }
                    catch (Exception)
                    { }
                    finally
                    {
                        Locker.Release();
                    }
                }
            }
            else
            {
                try
                {

                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Trying " + try_path));
                    ProcessStartInfo startInfo = new ProcessStartInfo(
                        try_path, "--version")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    };
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                        "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments));
                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.ErrorDataReceived += TestStderrDataReceived;
                    process.OutputDataReceived += TestStdoutDataReceived;
                    process.Start();
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                    process.StandardInput.Dispose();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("java found at '" + try_path + "', but it doesn't work."));
                        return false;
                    }
                    else
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("java found: '" + try_path + "'"));
                        where = try_path;
                        return true;
                    }
                }
                catch (Exception)
                {
                }
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("doesn't work."));
                return false;
            }
            return result;
        }

        private string JavaDownloadFile(string place_path, string java_download_fn, string java_download_url)
        {
            try
            {
                var archive_name = place_path + java_download_fn;
                var jar_dir = place_path;
                System.IO.Directory.CreateDirectory(jar_dir);
                if (!File.Exists(archive_name))
                {
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Downloading " + java_download_fn));
                    DownloadFileAsync(java_download_url, archive_name).Wait(new TimeSpan(0, 3, 0));
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Completed downloading of " + java_download_fn));
                }
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found " + archive_name));
                var java_dir = place_path;
                java_dir = java_dir.Replace("\\", "/");
                if (!java_dir.EndsWith("/")) java_dir = java_dir + "/";
                var decompressed_area = java_dir;
                System.IO.Directory.CreateDirectory(decompressed_area);
                return decompressed_area;
            }
            catch (Exception e)
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Caught throw in JavaDownloadFile code."));
                throw e;
            }
            finally
            {
            }
        }

        private bool DecompressJava(string uncompressed_root_dir, string archive_name)
        {
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Decompressing"));
            try
            {
                System.IO.Directory.CreateDirectory(uncompressed_root_dir);
            }
            catch (Exception e)
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Caught throw in directory creation code."));
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(e.Message + e.StackTrace));
                return false;
            }
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Create directory apparently worked."));
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(archive_name, uncompressed_root_dir);
            }
            catch (Exception e)
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Caught throw in extraction code."));
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(e.Message + e.StackTrace));
                return false;
            }
            return true;
        }

        private List<string> SearchEnvPathForProgram(string filename)
        {
            var delimiter = (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                || System.Environment.OSVersion.Platform == PlatformID.Win32S
                || System.Environment.OSVersion.Platform == PlatformID.Win32Windows)
                ? ';' : ':';
            List<string> p = Environment.GetEnvironmentVariable("PATH").Split(delimiter)
                    .Select(dir => Path.Combine(dir, filename))
                    .Where(path => File.Exists(path))
                    .ToList();
            return p;
        }

        private string ConvertOSArch()
        {
            OperatingSystem os_ver = Environment.OSVersion;
            var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var isOSX = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            var isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            System.Runtime.InteropServices.Architecture os_arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("os_arch is " + os_ver));
            if (isWindows)
            {
                switch (os_arch)
                {
                    case System.Runtime.InteropServices.Architecture.X64:
                        return "Windows x64";
                }
            }
            if (isLinux)
            {
                switch (os_arch)
                {
                    case System.Runtime.InteropServices.Architecture.X64:
                        if (IntPtr.Size != 8) break;
                        return "Linux x64";
                }
            }
            if (isOSX)
            {
                switch (os_arch)
                {
                    case System.Runtime.InteropServices.Architecture.X64:
                        if (IntPtr.Size != 8) break;
                        return "MacOSX x64";
                }
            }
            return "";
        }

        private bool GetGeneratedFileNameList()
        {
            // Because we're using the Java version of the Antlr tool,
            // we're going to execute this command twice: first with the
            // -depend option so as to get the list of generated files,
            // then a second time to actually generate the files.
            // The code that was here probably worked, but only for the C#
            // version of the Antlr tool chain.
            //
            // After collecting the output of the first command, convert the
            // output so as to get a clean list of files generated.
            List<string> arguments = new List<string>();
            arguments.Add("-cp");
            arguments.Add(AntlrToolJar.Replace("\\", "/"));
            arguments.Add("org.antlr.v4.Tool");
            arguments.Add("-depend");
            arguments.Add("-o");
            arguments.Add(AntOutDir.Replace("\\", "/"));
            if (!string.IsNullOrEmpty(LibPath))
            {
                var split = LibPath.Split(';');
                foreach (var p in split)
                {
                    if (string.IsNullOrEmpty(p))
                        continue;
                    if (string.IsNullOrWhiteSpace(p))
                        continue;
                    arguments.Add("-lib");
                    arguments.Add(p.Replace("\\", "/"));
                }
            }
            if (GAtn) arguments.Add("-atn");
            if (LongMessages) arguments.Add("-long-messages");
            if (!string.IsNullOrEmpty(Encoding))
            {
                arguments.Add("-encoding");
                arguments.Add(Encoding);
            }
            arguments.Add(Listener ? "-listener" : "-no-listener");
            arguments.Add(Visitor ? "-visitor" : "-no-visitor");
            if (!(string.IsNullOrEmpty(Package) || string.IsNullOrWhiteSpace(Package)))
            {
                arguments.Add("-package");
                arguments.Add(Package);
            }
            if (!string.IsNullOrEmpty(DOptions))
            {
                // The Antlr tool can take multiple -D options, but
                // DOptions is just a string. We allow for multiple
                // options by separating each with a semi-colon. At this
                // point, convert each option into separate "-D" option
                // arguments.
                var split = DOptions.Split(';');
                foreach (var p in split)
                {
                    var q = p.Trim();
                    if (string.IsNullOrEmpty(q))
                        continue;
                    if (string.IsNullOrWhiteSpace(q))
                        continue;
                    arguments.Add("-D" + q);
                }
            }
            if (Error) arguments.Add("-Werror");
            if (ForceAtn) arguments.Add("-Xforce-atn");
            if (SourceCodeFiles == null) arguments.AddRange(OtherSourceCodeFiles);
            else arguments.AddRange(SourceCodeFiles?.Select(s => s.ItemSpec));
            ProcessStartInfo startInfo = new ProcessStartInfo(
                JavaExec, JoinArguments(arguments))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments));
            Process process = new Process();
            process.StartInfo = startInfo;
            process.ErrorDataReceived += HandleStderrDataReceived;
            process.OutputDataReceived += HandleOutputDataReceivedFirstTime;
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.StandardInput.Dispose();
            process.WaitForExit();
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                "Finished executing Antlr jar command."));
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                "The generated file list contains " + _generatedCodeFiles.Count() + " items."));
            if (process.ExitCode != 0)
            {
                return false;
            }
            // Add in tokens and interp files since Antlr Tool does not do that.
            // pick off lexer and add in .interp and .tokens.
            var lexer = _generatedCodeFiles.Where(s => s.EndsWith("Lexer.cs")).ToList();
            foreach (var l in lexer)
            {
                var dire = Path.GetDirectoryName(l);
                var filen = Path.GetFileName(l);
                var stem = Path.GetFileNameWithoutExtension(l);
                var pre = dire + "/" + stem;
                _generatedFiles.Add(pre + ".tokens");
            }
            return true;
        }

        private bool GenerateFiles(out bool success)
        {
            List<string> arguments = new List<string>();
            {
                arguments.Add("-cp");
                arguments.Add(AntlrToolJar.Replace("\\", "/"));
                //arguments.Add("org.antlr.v4.CSharpTool");
                arguments.Add("org.antlr.v4.Tool");
            }
            arguments.Add("-o");
            arguments.Add(AntOutDir.Replace("\\", "/"));
            if (!string.IsNullOrEmpty(LibPath))
            {
                var split = LibPath.Split(';');
                foreach (var p in split)
                {
                    if (string.IsNullOrEmpty(p))
                        continue;
                    if (string.IsNullOrWhiteSpace(p))
                        continue;
                    arguments.Add("-lib");
                    arguments.Add(p.Replace("\\", "/"));
                }
            }
            if (GAtn) arguments.Add("-atn");
            if (Log_) arguments.Add("-Xlog");
            if (LongMessages) arguments.Add("-long-messages");
            if (!string.IsNullOrEmpty(Encoding))
            {
                arguments.Add("-encoding");
                arguments.Add(Encoding);
            }
            arguments.Add(Listener ? "-listener" : "-no-listener");
            arguments.Add(Visitor ? "-visitor" : "-no-visitor");
            if (!(string.IsNullOrEmpty(Package) || string.IsNullOrWhiteSpace(Package)))
            {
                arguments.Add("-package");
                arguments.Add(Package);
            }
            if (!string.IsNullOrEmpty(DOptions))
            {
                // Since the C# target currently produces the same code for all target framework versions, we can
                // avoid bugs with support for newer frameworks by just passing CSharp as the language and allowing
                // the tool to use a default.
                var split = DOptions.Split(';');
                foreach (var p in split)
                {
                    if (string.IsNullOrEmpty(p))
                        continue;
                    if (string.IsNullOrWhiteSpace(p))
                        continue;
                    arguments.Add("-D" + p);
                }
            }
            if (Error) arguments.Add("-Werror");
            if (ForceAtn) arguments.Add("-Xforce-atn");
            if (SourceCodeFiles == null) arguments.AddRange(OtherSourceCodeFiles);
            else arguments.AddRange(SourceCodeFiles?.Select(s => s.ItemSpec));
            ProcessStartInfo startInfo = new ProcessStartInfo(JavaExec, JoinArguments(arguments))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments));
            Process process = new Process();
            process.StartInfo = startInfo;
            process.ErrorDataReceived += HandleStderrDataReceived;
            process.OutputDataReceived += HandleStdoutDataReceived;
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.StandardInput.Dispose();
            process.WaitForExit();
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                "Finished executing Antlr jar command."));
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                "The generated file list contains " + _generatedCodeFiles.Count() + " items."));
            foreach (var fn in _generatedCodeFiles)
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Generated file " + fn));
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments));
            // At this point, regenerate the entire GeneratedCodeFiles list.
            // This is because (1) it contains duplicates; (2) it contains
            // files that really actually weren't generated. This can happen
            // if the grammar was a Lexer grammar. (Note, I don't think it
            // wise to look at the grammar file to figure out what it is, nor
            // do I think it wise to expose a switch to the user for him to
            // indicate what type of grammar it is.)
            var new_code_list = new List<string>();
            var new_all_list = new List<string>();
            foreach (var fn in _generatedCodeFiles.Distinct().ToList())
            {
                var ext = Path.GetExtension(fn);
                if ((ext == ".tokens"))
                {
                    var interp = fn.Substring(0, fn.Length - ext.Length) + ".interp";
                    new_all_list.Add(interp);
                }
                if (File.Exists(fn) && !(ext == ".g4" && ext == ".g"))
                    new_all_list.Add(fn);
                if ((ext == ".cs" || ext == ".java" || ext == ".cpp" ||
                     ext == ".php" || ext == ".js") && File.Exists(fn))
                    new_code_list.Add(fn);
            }
            foreach (var fn in _generatedFiles.Distinct().ToList())
            {
                var ext = Path.GetExtension(fn);
                if ((ext == ".tokens"))
                {
                    var interp = fn.Substring(0, fn.Length - ext.Length) + ".interp";
                    new_all_list.Add(interp);
                }
                if (File.Exists(fn) && !(ext == ".g4" && ext == ".g"))
                    new_all_list.Add(fn);
                if ((ext == ".cs" || ext == ".java" || ext == ".cpp" ||
                     ext == ".php" || ext == ".js") && File.Exists(fn))
                    new_code_list.Add(fn);
            }
            _generatedFiles = new_all_list.ToList();
            _generatedCodeFiles = new_code_list.ToList();
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("List of generated files " + String.Join(" ", _generatedFiles)));
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("List of generated code files " + String.Join(" ", _generatedCodeFiles)));
            success = process.ExitCode == 0;
            return success;
        }

        private void ProcessExceptionAsBuildMessage(Exception exception)
        {
            MessageQueue.EnqueueMessage(Message.BuildCrashMessage(exception.Message
                + exception.StackTrace));
        }

        internal static bool IsFatalException(Exception exception)
        {
            while (exception != null)
            {
                if (exception is OutOfMemoryException)
                {
                    return true;
                }

                if (!(exception is TypeInitializationException) && !(exception is TargetInvocationException))
                {
                    break;
                }

                exception = exception.InnerException;
            }

            return false;
        }

        private static string JoinArguments(IEnumerable<string> arguments)
        {
            if (arguments == null)
                throw new ArgumentNullException("arguments");

            StringBuilder builder = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (builder.Length > 0)
                    builder.Append(' ');

                if (argument.IndexOfAny(new[] { '"', ' ' }) < 0)
                {
                    builder.Append(argument);
                    continue;
                }

                // escape a backslash appearing before a quote
                string arg = argument.Replace("\\\"", "\\\\\"");
                // escape double quotes
                arg = arg.Replace("\"", "\\\"");

                // wrap the argument in outer quotes
                builder.Append('"').Append(arg).Append('"');
            }

            return builder.ToString();
        }

        private static readonly Regex GeneratedFileMessageFormat = new Regex(@"^Generating file '(?<OUTPUT>.*?)' for grammar '(?<GRAMMAR>.*?)'$", RegexOptions.Compiled);
        private static readonly Regex CheckroteMessage = new Regex(".*wrote.*", RegexOptions.Compiled);

        bool good_version = false;
        private void TestStderrDataReceived(object sender, DataReceivedEventArgs e)
        {
            TestDataReceived(e.Data);
        }
        private void TestStdoutDataReceived(object sender, DataReceivedEventArgs e)
        {
            TestDataReceived(e.Data);
        }
        private void TestDataReceived(string data)
        {
            MessageQueue.EnqueueMessage(Message.BuildDefaultMessage("got data " + data));
            if (string.IsNullOrEmpty(data))
            {
                return;
            }
            try
            {
                Regex valid_version_pattern = new Regex("(OpenJDK Runtime Environment [^()]*[(]build (1[1-9]|[2-9][0-9]))|(Java[(]TM[)] SE Runtime Environment [(]build (1[1-9]|[2-9][0-9]))");
                var matches = valid_version_pattern.Matches(data);
                var found = matches.Count > 0;
                MessageQueue.EnqueueMessage(Message.BuildDefaultMessage("got matches " + matches.Count + " for '" + data + "'"));
                if (data.Contains("Exception in thread"))
                {
                    good_version = false;
                }
                else if (found)
                {
                    MessageQueue.EnqueueMessage(Message.BuildDefaultMessage("contains a 'good' version."));
                    good_version = true;
                }
            }
            catch (Exception)
            {
                good_version = false;
            }
        }

        private void HandleStderrDataReceived(object sender, DataReceivedEventArgs e)
        {
            HandleStderrDataReceived(e.Data);
        }

        private void HandleStderrDataReceived(string data)
        {
            //System.Console.Error.WriteLine("XXX3 " + data);
            if (string.IsNullOrEmpty(data))
                return;
            try
            {
                if (data.Contains("Exception in thread"))
                {
                    _start = true;
                    _sb.AppendLine(data);
                }
                else if (_start)
                {
                    _sb.AppendLine(data);
                    if (data.Contains("at org.antlr.v4.Tool.main(Tool.java"))
                    {
                        MessageQueue.EnqueueMessage(Message.BuildErrorMessage(_sb.ToString()));
                        _sb = new StringBuilder();
                        _start = false;
                    }
                }
                else
                    MessageQueue.EnqueueMessage(Message.BuildDefaultMessage(data));
            }
            catch (Exception ex)
            {
                if (RunAntlrTool.IsFatalException(ex))
                    throw;

                MessageQueue.EnqueueMessage(Message.BuildCrashMessage(ex.Message));
            }
        }

        private void HandleOutputDataReceivedFirstTime(object sender, DataReceivedEventArgs e)
        {
            string str = e.Data as string;
            if (string.IsNullOrEmpty(str))
                return;

            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Got '" + str + "' from Antlr Tool."));

            // There could all kinds of shit coming out of the Antlr Tool, so we need to
            // take care of what to record.
            // Parse the dep string as "file-name1 : file-name2". Strip off the name
            // file-name1 and save it away.
            try
            {
                Regex regex = new Regex(@"^(?<OUTPUT>[^\n\r]+)\s{0,}[:]\s{1,}");
                Match match = regex.Match(str);
                if (!match.Success)
                {
                    MessageQueue.EnqueueMessage(Message.BuildErrorMessage("Output from Antlr4 tool was '"
                        + str
                        + "'. It wasn't expected!"));
                    return;
                }
                string fn = match.Groups["OUTPUT"].Value;
                fn = fn.Trim();
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Yo value matched is " + fn));
                var ext = Path.GetExtension(fn);
                if (ext == ".cs" || ext == ".java" || ext == ".cpp" ||
                    ext == ".php" || ext == ".js" || ext == ".tokens" || ext == ".interp" ||
                    ext == ".dot")
                    _generatedCodeFiles.Add(fn);
            }
            catch (Exception ex)
            {
                if (RunAntlrTool.IsFatalException(ex))
                    throw;

                MessageQueue.EnqueueMessage(Message.BuildCrashMessage(ex.Message));
            }
        }

        private void HandleStdoutDataReceived(object sender, DataReceivedEventArgs e)
        {
            HandleStdoutDataReceived(e.Data);
        }

        private void HandleStdoutDataReceived(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Yo got " + data + " from Antlr Tool."));

            try
            {
                Match match = CheckroteMessage.Match(data);
                if (!match.Success)
                {
                    MessageQueue.EnqueueMessage(Message.BuildErrorMessage(data));
                    return;
                }

//                string fileName = match.Groups["OUTPUT"].Value;
//                _generatedCodeFiles.Add(match.Groups["OUTPUT"].Value);
            }
            catch (Exception ex)
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(ex.Message
                                                            + ex.StackTrace));

                if (RunAntlrTool.IsFatalException(ex))
                    throw;
            }
        }

        private void Read(string destination, string testArchive, CompressionType expectedCompression, ReaderOptions options = null)
        {
            try
            {
                Stream stream = File.OpenRead(testArchive);
                var reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        reader.WriteEntryToDirectory(destination, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                    if (reader.Entry.Attrib != null &&
                        (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                        || System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(OSPlatform.OSX)))
                    {
                        // find chmod path
                        Process processWhich = new Process();
                        processWhich.StartInfo.FileName = "/bin/sh";
                        processWhich.StartInfo.Arguments = $"-c \"which chmod\"";
                        processWhich.StartInfo.UseShellExecute = false;
                        processWhich.StartInfo.CreateNoWindow = true;
                        processWhich.StartInfo.RedirectStandardInput = false;
                        processWhich.StartInfo.RedirectStandardOutput = true;
                        processWhich.StartInfo.RedirectStandardError = true;

                        processWhich.Start();
                        var fullChmodPath = processWhich.StandardOutput.ReadToEnd().Trim();
                        processWhich.WaitForExit();

                        // execute chmod.
                        List<string> arguments = new List<string>();
                        arguments.Add(ToChmodArg(reader.Entry.Attrib));
                        var full_path = destination + reader.Entry.Key;
                        //Console.WriteLine("full path \"" + full_path + "\"");
                        //Log.LogMessage("full path \"" + full_path + "\"");
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("full path \"" + full_path + "\""));
                        arguments.Add(full_path);
                        ProcessStartInfo startInfo = new ProcessStartInfo(
                           fullChmodPath,
                           JoinArguments(arguments))
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardInput = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                        };
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                            "Executing command: \"" + startInfo.FileName + "\" " + startInfo.Arguments));
                        Process process = new Process();
                        process.StartInfo = startInfo;
                        process.ErrorDataReceived += HandleStderrDataReceived;
                        process.OutputDataReceived += HandleStdoutDataReceived;
                        process.Start();
                        process.BeginErrorReadLine();
                        process.BeginOutputReadLine();
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                        {
                            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Chmod didn't work, returned " + process.ExitCode));
                        }
                    }
                }
                reader.Dispose();
                stream.Dispose();
            }
            catch (Exception ex)
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("something didn't work -- exception " + ex.Message));
                throw ex;
            }

        }

        private string ToChmodArg(long? attrib)
        {
            var value = attrib.Value;
            var c1 = (value & 0x7).ToString();
            value = value >> 3;
            var c2 = (value & 0x7).ToString();
            value = value >> 3;
            var c3 = (value & 0x7).ToString();
            return c3 + c2 + c1;
        }
    }
}
