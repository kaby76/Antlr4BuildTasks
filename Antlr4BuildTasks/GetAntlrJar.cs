namespace Antlr4.Build.Tasks
{
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;

    public class GetAntlrJar : Task
    {
        public GetAntlrJar()
        {
        }

        public string IntermediateOutputPath
        {
            get;
            set;
        }

        public ITaskItem[] PackageReference
        {
            get;
            set;
        }

        public string ToolPath
        {
            get;
            set;
        }

        public string AntlrProbePath
        {
            get;
            set;
        }

        [Output] public string UsingToolPath
        {
            get;
            set;
        }

        public override bool Execute()
        {
            if (!(ToolPath == null || ToolPath == ""))
            {
                var list = new Domemtech.Globbing.Glob().Contents(ToolPath);
                if (list == null) return false;
                else if (list.Count == 1) UsingToolPath = list.First().FullName;
                else return false;
                return true;
            }

            string version = null;
            bool reference_standard_runtime = false;
            foreach (var i in PackageReference)
            {
                if (i.ItemSpec == "Antlr4.Runtime.Standard")
                {
                    reference_standard_runtime = true;
                    version = i.GetMetadata("Version");
                    break;
                }
            }
            if (version == null)
            {
                foreach (var i in PackageReference)
                {
                    if (i.ItemSpec == "Antlr4.Runtime")
                    {
                        throw new Exception(@"You are referencing Antlr4.Runtime. This build tool can only reference the NET Standard library https://www.nuget.org/packages/Antlr4.Runtime.Standard/");
                    }
                }
                if (reference_standard_runtime)
                    throw new Exception(@"Antlr4BuildTasks cannot identify the version number you are referencing. Check the Version parameter.");
                else
                    throw new Exception(@"You are not referencing Antlr4.Runtime.Standard in you .csproj file. Antlr4BuildTasks requires a reference to it in order
to identify which version of the Antlr Java tool to run to generate the parser and lexer.");
            }

            if (AntlrProbePath == null || AntlrProbePath == "")
            {
                throw new Exception(@"Antlr4BuildTasks requires an AntlrProbePath, which contains the list of places to find and download the Antlr .jar file. AntlrProbePath is null.");
            }

            // Assume that it's a string with semi-colon separation. Split, then search for the version.
            var paths = AntlrProbePath.Split(';').ToList();
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string archive_path = "file:///" + Path.GetFullPath(assemblyPath
                                                   + System.IO.Path.DirectorySeparatorChar
                                                   + ".."
                                                   + System.IO.Path.DirectorySeparatorChar
                                                   + ".."
                                                   + System.IO.Path.DirectorySeparatorChar
                                                   + "build"
                                                   + System.IO.Path.DirectorySeparatorChar);

            paths.Insert(0, archive_path);
            foreach (var probe in paths)
            {
                Regex r2 = new Regex("^(?<TWOVERSION>[0-9]+[.][0-9]+)([.][0-9]*)?$");
                var m2 = r2.Match(version);
                var v2 = m2.Success && m2.Groups["TWOVERSION"].Length > 0 ? m2.Groups["TWOVERSION"].Value : null;
                Regex r3 = new Regex("^(?<THREEVERSION>[0-9]+[.][0-9]+[.][0-9]+)$");
                var m3 = r3.Match(version);
                var v3 = m3.Success && m3.Groups["THREEVERSION"].Length > 0 ? m3.Groups["THREEVERSION"].Value : null;
                if (v3 != null && TryProbe(probe, v3))
                {
                    return true;
                }
                if (v2 != null && TryProbe(probe, v2))
                {
                    return true;
                }
            }
            return true;
        }

        private bool TryProbe(string path, string version)
        {
            if (!path.EndsWith("/")) path = path + "/";


            {
                var jar = path + @"antlr-" + version + @"-complete.jar";
                Log.LogMessage(MessageImportance.Normal, "Probing " + jar);
                if (jar.StartsWith("file:///"))
                {
                    try
                    {
                        System.Uri uri = new Uri(jar);
                        var local_file = uri.LocalPath;
                        Log.LogMessage(MessageImportance.Normal, "Local path " + local_file);
                        if (File.Exists(local_file))
                        {
                            Log.LogMessage(MessageImportance.Normal, "got it.");
                            UsingToolPath = local_file;
                            return true;
                        }
                    }
                    catch
                    {
                    }
                    return false;
                }
                else
                {
                    try
                    {
                        WebClient webClient = new WebClient();
                        System.IO.Directory.CreateDirectory(IntermediateOutputPath);
                        var archive_name = IntermediateOutputPath + System.IO.Path.DirectorySeparatorChar +
                                           System.IO.Path.GetFileName(jar);
                        var jar_dir = IntermediateOutputPath;
                        System.IO.Directory.CreateDirectory(jar_dir);
                        if (!File.Exists(archive_name))
                        {
                            this.Log.LogMessage(MessageImportance.Normal, "Downloading " + jar);
                            webClient.DownloadFile(jar, archive_name);
                        }
                        Log.LogMessage(MessageImportance.Normal, "got it.");
                        UsingToolPath = archive_name;
                        return true;
                    }
                    catch
                    {
                    }
                }
            }
            {
                var jar = path + @"antlr4-" + version + @"-complete.jar";
                Log.LogMessage(MessageImportance.Normal, "Probing " + jar);
                if (jar.StartsWith("file:///"))
                {
                    try
                    {
                        System.Uri uri = new Uri(jar);
                        var local_file = uri.LocalPath;
                        Log.LogMessage(MessageImportance.Normal, "Local path " + local_file);
                        if (File.Exists(local_file))
                        {
                            Log.LogMessage(MessageImportance.Normal, "got it.");
                            UsingToolPath = local_file;
                            return true;
                        }
                    }
                    catch
                    {
                    }
                    return false;
                }
                else
                {
                    try
                    {
                        WebClient webClient = new WebClient();
                        System.IO.Directory.CreateDirectory(IntermediateOutputPath);
                        var archive_name = IntermediateOutputPath + System.IO.Path.DirectorySeparatorChar +
                                           System.IO.Path.GetFileName(jar);
                        var jar_dir = IntermediateOutputPath;
                        System.IO.Directory.CreateDirectory(jar_dir);
                        if (!File.Exists(archive_name))
                        {
                            this.Log.LogMessage(MessageImportance.Normal, "Downloading " + jar);
                            webClient.DownloadFile(jar, archive_name);
                        }
                        Log.LogMessage(MessageImportance.Normal, "got it.");
                        UsingToolPath = archive_name;
                        return true;
                    }
                    catch
                    {
                    }
                    return false;
                }
            }

        }
    }
}
