// Forked and highly modified from sources at https://github.com/tunnelvisionlabs/antlr4cs/tree/master/runtime/CSharp/Antlr4BuildTasks
// Copyright 2022 Ken Domino, MIT License.

// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;
using StringBuilder = System.Text.StringBuilder;
using Antlr4.Build.Tasks.Util;
using System.IO.Compression;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers.Tar;
using SharpCompress.Readers.Tar;


namespace Antlr4.Build.Tasks
{
    public class RunAntlrTool : Task
    {
        private const string ToolVersion = "10.5.0";
        private const string DefaultGeneratedSourceExtension = "g4";
        private List<string> _generatedCodeFiles = new List<string>();
        private List<string> _generatedDirectories = new List<string>();
        private List<string> _generatedFiles = new List<string>();

        public RunAntlrTool()
        {
            this.GeneratedSourceExtension = DefaultGeneratedSourceExtension;
        }

        public bool AllowAntlr4cs { get; set; }
        public string AntlrToolJar { get; set; }
        public string AntOutDir { get; set; }
        public List<string> AntlrProbePath
        {
            get;
            set;
        } = new List<string>();
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
        public string JavaExec { get; set; }
        public List<string> JavaProbePath
        {
            get;
            set;
        } = new List<string>();
        public string LibPath { get; set; }
        public bool Listener { get; set; }
        public string Package { get; set; }
        public List<string> OtherSourceCodeFiles { get; set; }
        [Required] public ITaskItem[] PackageReference { get; set; }
        [Required] public ITaskItem[] Reference { get; set; }
        [Required] public ITaskItem[] PackageVersion { get; set; }
        [Required] public ITaskItem[] SourceCodeFiles { get; set; }
        public string TargetFrameworkVersion { get; set; }
        public ITaskItem[] TokensFiles { get; set; }
        public string Version { get; set; }
        public bool Visitor { get; set; }

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
            // Make sure Antlr4BuildTasks and Antlr4.Runtime.Standard are not Referene'd.
            foreach (var i in Reference)
            {
                if (i.ItemSpec.ToLower().Contains("Antlr4.Runtime.Standard".ToLower()))
                {
                    throw new Exception(
                        @"You are using <Reference> for Antlr4.Runtime.Standard in your .csproj file. You can only use <PackageReference> for the package, never a link to the dll.");
                }
                if (i.ItemSpec.ToLower().Contains("Antlr4BuildTasks".ToLower()))
                {
                    throw new Exception(
                        @"You are using <Reference> for Antlr4BuildTasks in your .csproj file. You can only use <PackageReference> for the package, never a link to the dll.");
                }
            }
            {
                foreach (var i in PackageReference)
                {
                    if (i.ItemSpec.ToLower() == "Antlr4.CodeGenerator".ToLower())
                    {
                        throw new Exception(
                            @"You are referencing Antlr4.CodeGenerator in your .csproj file. This build tool cannot use by the old Antlr4cs tool and 'official' Antlr4 Java tool. Remove package reference Antlr4.CodeGenerator.");
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

            // Set up probe path for Antlr tool jar if there isn't one.
            var paths = AntlrProbePath;
            string user_profile_path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace("\\", "/");
            if (!user_profile_path.EndsWith("/")) user_profile_path = user_profile_path + "/";
            string tool_path = user_profile_path
                + ".nuget/packages/antlr4buildtasks/"
                + ToolVersion
                + "/";
            var assemblyPath = tool_path + "build/";

            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                "Location to stuff Antlr tool jar, if not found, is " + assemblyPath));

            if (paths == null || paths.Count == 0)
            {
                string package_area = "file:///" + assemblyPath;
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
                    bool t = TryProbeAntlrJar(probe, v3, assemblyPath, out string w);
                    if (t)
                    {
                        result = w;
                        break;
                    }
                }
                else if (v3 != null && v3.EndsWith(".0"))
                {
                    bool t = TryProbeAntlrJar(probe, v3.Substring(0, v3.Length - 2), assemblyPath, out string w);
                    if (t)
                    {
                        result = w;
                        break;
                    }
                }
                else if (v2 != null)
                {
                    bool t = TryProbeAntlrJar(probe, v2, assemblyPath, out string w);
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
                    WebClient webClient = new WebClient();
                    var archive_name = place_path
                        + System.IO.Path.GetFileName(j);
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("archive_name is " + archive_name));
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("place_path is " + place_path));
                    var jar_dir = place_path;
                    System.IO.Directory.CreateDirectory(jar_dir);
                    if (!File.Exists(archive_name))
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Downloading " + j));
                        webClient.DownloadFile(j, archive_name);
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
                    WebClient webClient = new WebClient();
                    var archive_name = place_path
                        + System.IO.Path.GetFileName(j);
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("archive_name is " + archive_name));
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("place_path is " + place_path));
                    var jar_dir = place_path;
                    System.IO.Directory.CreateDirectory(jar_dir);
                    if (!File.Exists(archive_name))
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Downloading " + j));
                        webClient.DownloadFile(j, archive_name);
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
            if (JavaExec != null && JavaExec != "")
                return JavaExec;

            string result = null;

            // Set up probe path for Java if there isn't one.
            var paths = JavaProbePath;
            string user_profile_path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace("\\", "/");
            if (!user_profile_path.EndsWith("/")) user_profile_path = user_profile_path + "/";
            string tool_path = user_profile_path
                + ".nuget/packages/antlr4buildtasks/"
                + ToolVersion
                + "/";
            var assemblyPath = tool_path + "build/";

            MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                "Location to stuff JRE, if not found, is " + assemblyPath));

            if (paths == null || paths.Count == 0)
            {
                paths = new List<string>();
               // paths.Add("PATH");
                paths.Add("DOWNLOAD");

                //string package_area = "file:///" + assemblyPath + "jre.zip";
                //paths.Add(package_area);

                //var full_path = "file:///" + Path.GetFullPath(IntermediateOutputPath);
                //paths.Add(full_path);
                //paths.Add("https://download.java.net/java/GA/jdk11/13/GPL/openjdk-11.0.1_windows-x64_bin.zip");
            }

            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Paths to search for Antlr4 jar, in order, are: "
                + String.Join(";", paths)));

            foreach (var probe in paths)
            {
                if (TryProbeJava(probe, assemblyPath, out string where))
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

        private string DownloadFile(string place_path, string java_download_fn, string java_download_url)
        {
            WebClient webClient = new WebClient();
            var archive_name = place_path + java_download_fn;
            var jar_dir = place_path;
            System.IO.Directory.CreateDirectory(jar_dir);
            if (!File.Exists(archive_name))
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Downloading " + java_download_fn));
                webClient.DownloadFile(java_download_url, archive_name);
            }
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found " + archive_name));
            var java_dir = place_path;
            java_dir = java_dir.Replace("\\", "/");
            if (!java_dir.EndsWith("/")) java_dir = java_dir + "/";
            var decompressed_area = java_dir + "Java/";
            System.IO.Directory.CreateDirectory(decompressed_area);
            return decompressed_area;
        }

        private bool TryProbeJava(string path, string place_path, out string where)
        {
            bool result = false;
            where = null;
            path = path.Trim();
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("path is " + path));
            if (path == "PATH")
            {
                var executable_name = (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                    || System.Environment.OSVersion.Platform == PlatformID.Win32S
                    || System.Environment.OSVersion.Platform == PlatformID.Win32Windows) ? "java.exe" : "java";
                var w = executable_name.GetFullPath();
                if (w != null && w != "")
                {
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("java found: '" + w + "'"));
                    where = w;
                    return true;
                }
                else
                {
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage(executable_name + " not found on path"));
                }
            }
            else if (path.StartsWith("file://"))
            {
                var f = path;
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Probing " + f));
                try
                {
                    System.Uri uri = new Uri(f);
                    var local_file = uri.LocalPath;
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Local path " + local_file));
                    if (File.Exists(local_file))
                    {
                        if (Path.GetFileName(local_file) == "jre.zip" &&
                           (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                           || System.Environment.OSVersion.Platform == PlatformID.Win32S
                           || System.Environment.OSVersion.Platform == PlatformID.Win32Windows))
                        {
                            // Unpack and get java executable.
                            var java_dir = place_path;
                            java_dir = java_dir.Replace("\\", "/");
                            if (!java_dir.EndsWith("/"))
                            {
                                java_dir = java_dir + "/";
                            }
                            java_dir = java_dir + "Java/";
                            _generatedDirectories.Add(java_dir);
                            var archive = local_file;
                            if (!Directory.Exists(java_dir))
                            {
                                System.IO.Directory.CreateDirectory(java_dir);
                                System.IO.Compression.ZipFile.ExtractToDirectory(archive, java_dir);
                            }
                            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found."));
                            where = java_dir + "jre/bin/java.exe";
                            return true;
                        }
                        else
                        {
                            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found."));
                            where = local_file;
                            result = true;
                        }
                    }
                }
                catch
                {
                }
            }
            else if (path == "DOWNLOAD")
            {
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Probing " + path));
                // Get OS and native type.
                OperatingSystem os_ver = Environment.OSVersion;
                System.Runtime.InteropServices.Architecture os_arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
                string java_download_fn = null;
                string java_download_url = null;
                // See https://www.oracle.com/java/technologies/downloads/
                switch (os_ver.Platform)
                {
                    case PlatformID.Win32NT:
                        switch (os_arch)
                        {
                            case System.Runtime.InteropServices.Architecture.X64:
                                if (IntPtr.Size != 8) break;
                                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("OS is Windows"));
                                java_download_fn = "jdk-18_windows-x64_bin.zip";
                                java_download_url = "https://download.oracle.com/java/18/latest/jdk-18_windows-x64_bin.zip";
                                try
                                {
                                    string uncompressed_root_dir = DownloadFile(place_path, java_download_fn, java_download_url);
                                    where = uncompressed_root_dir + "jdk-18.0.1.1/bin/java.exe";
                                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Java should be here " + where));
                                    _generatedDirectories.Add(uncompressed_root_dir);
                                    var archive_name = place_path + java_download_fn;
                                    if (! File.Exists(where))
                                    {
                                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Decompressing"));
                                        System.IO.Directory.CreateDirectory(uncompressed_root_dir);
                                        System.IO.Compression.ZipFile.ExtractToDirectory(archive_name, uncompressed_root_dir);
                                    }
                                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found."));
                                    return true;
                                }
                                catch
                                {
                                    where = null;
                                }
                                break;
                        }
                        break;
                    case PlatformID.Unix:
                        switch (os_arch)
                        {
                            case System.Runtime.InteropServices.Architecture.X64:
                                if (IntPtr.Size != 8) break;
                                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("OS is Linux"));
                                java_download_fn = "jdk-18_linux-x64_bin.tar.gz";
                                java_download_url = "https://download.oracle.com/java/18/latest/jdk-18_linux-x64_bin.tar.gz";
                                try
                                {
                                    string uncompressed_root_dir = DownloadFile(place_path, java_download_fn, java_download_url);
                                    where = uncompressed_root_dir + "jdk-18.0.1.1/bin/java";
                                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Java should be here " + where));
                                    _generatedDirectories.Add(uncompressed_root_dir);
                                    var archive_name = place_path + java_download_fn;
                                    if (!File.Exists(where))
                                    {
                                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Decompressing"));
                                        System.IO.Directory.CreateDirectory(uncompressed_root_dir);
                                        Read(uncompressed_root_dir, archive_name, new CompressionType());
                                    }
                                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found."));
                                    return true;
                                }
                                catch
                                {
                                    where = null;
                                }
                                break;
                        }
                        break;
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

        private void HandleStderrDataReceived(object sender, DataReceivedEventArgs e)
        {
            HandleStderrDataReceived(e.Data);
        }

        bool start = false;
        StringBuilder sb = new StringBuilder();
        private void HandleStderrDataReceived(string data)
        {
            //System.Console.Error.WriteLine("XXX3 " + data);
            if (string.IsNullOrEmpty(data))
                return;
            try
            {
                if (data.Contains("Exception in thread"))
                {
                    start = true;
                    sb.AppendLine(data);
                }
                else if (start)
                {
                    sb.AppendLine(data);
                    if (data.Contains("at org.antlr.v4.Tool.main(Tool.java"))
                    {
                        MessageQueue.EnqueueMessage(Message.BuildErrorMessage(sb.ToString()));
                        sb = new StringBuilder();
                        start = false;
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
                Match match = GeneratedFileMessageFormat.Match(data);
                if (!match.Success)
                {
                    MessageQueue.EnqueueMessage(Message.BuildErrorMessage(data));
                    return;
                }

                string fileName = match.Groups["OUTPUT"].Value;
                _generatedCodeFiles.Add(match.Groups["OUTPUT"].Value);
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
            OperatingSystem os_ver = Environment.OSVersion;
            System.Runtime.InteropServices.Architecture os_arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
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
                if (reader.Entry.Attrib != null && os_ver.Platform == PlatformID.Unix)
                {
                    // execute chmod.
                    List<string> arguments = new List<string>();
                    arguments.Add(ToChmodArg(reader.Entry.Attrib));
                    var full_path = destination + reader.Entry.Key;
                    //Console.WriteLine("full path \"" + full_path + "\"");
                    //Log.LogMessage("full path \"" + full_path + "\"");
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("full path \"" + full_path + "\""));
                    arguments.Add(full_path);
                    ProcessStartInfo startInfo = new ProcessStartInfo(
                       "/usr/bin/chmod", JoinArguments(arguments))
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
