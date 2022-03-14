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

        public ITaskItem[] PackageVersion
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
            bool result = false;
            try
            {
                // Make sure old crusty Tunnelvision port not being used.
                foreach (var i in PackageReference)
                {
                    if (i.ItemSpec == "Antlr4.Runtime")
                    {
                        result = false;
                        MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                            @"You are referencing Antlr4.Runtime in your .csproj file. This build tool can only reference the NET Standard library https://www.nuget.org/packages/Antlr4.Runtime.Standard/. You can only use either the 'official' Antlr4 or the 'tunnelvision' fork, but not both. You have to choose one."));
                        return false;
                    }
                }
                string version = null;
                bool reference_standard_runtime = false;
                foreach (var i in PackageReference)
                {
                    if (i.ItemSpec == "Antlr4.Runtime.Standard")
                    {
                        reference_standard_runtime = true;
                        version = i.GetMetadata("Version");
                        if (version == null || version.Trim() == "")
                        {
                            foreach (var j in PackageVersion)
                            {
                                if (j.ItemSpec == "Antlr4.Runtime.Standard")
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
                        MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                            @"Antlr4BuildTasks cannot identify the version number you are referencing. Check the Version parameter for Antlr4.Runtime.Standard."));
                    else
                        MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                            @"You are not referencing Antlr4.Runtime.Standard in your .csproj file. Antlr4BuildTasks requires a reference to it in order
to identify which version of the Antlr Java tool to run to generate the parser and lexer."));
                    return false;
                }
                else if (version.Trim() == "")
                {
                    MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                        @"Antlr4BuildTasks cannot determine the version of Antlr4.Runtime.Standard. It's ''!.
version = '" + version + @"'
PackageReference = '" + PackageReference.ToString() + @"'
PackageVersion = '" + PackageVersion.ToString() + @"
"));
                    return false;
                }
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage(
                    @"Antlr4BuildTasks identified that you are looking for version "
                        + version
                        + " of the Antlr4 tool jar."));
                if (AntlrProbePath == null || AntlrProbePath == "")
                {
                    MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                        @"Antlr4BuildTasks requires an AntlrProbePath, which contains the list of places to find and download the Antlr .jar file. AntlrProbePath is null."));
                    return false;
                }
                // Add location of Antlr4BuildTasks for probing.
                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                assemblyPath = Path.GetFullPath(assemblyPath + "/../../build/").Replace("\\","/");
                string archive_path = "file:///" + assemblyPath;
                var paths = AntlrProbePath.Split(';').ToList();
                paths.Insert(0, archive_path);
                MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Paths to search for Antlr4 jar, in order, are: "
                 + String.Join(";", paths)));
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
                        if (UsingToolPath == null || UsingToolPath == "")
                        {
                            MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                                @"Antlr4BuildTasks is going to return an empty UsingToolPath, but it should never do that."));
                            result = false;
                            return result;
                        }
                        else
                        {
                            result = true;
                            return result;
                        }
                    }
                    if (v2 != null && TryProbe(probe, v2))
                    {
                        if (UsingToolPath == null || UsingToolPath == "")
                        {
                            MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                                @"Antlr4BuildTasks is going to return an empty UsingToolPath, but it should never do that."));
                            result = false;
                            return result;
                        }
                        else
                        {
                            result = true;
                            return result;
                        }
                    }
                }
                if (UsingToolPath == null || UsingToolPath == "")
                {
                    MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                        @"Went through the complete probe list looking for an Antlr4 tool jar, but could not find anything. Fail!"));
                }
            }
            catch (Exception ex)
            {
                MessageQueue.EnqueueMessage(Message.BuildErrorMessage(
                    @"Crash " + ex.Message));
            }
            finally
            {
                if (!result)
                {
                    MessageQueue.EnqueueMessage(Message.BuildErrorMessage("The GetAntlrJar tool failed."));
                    MessageQueue.MutateToError();
                }
                MessageQueue.EmptyMessageQueue(Log);
            }
            return result;
        }

        private bool TryProbe(string path, string version)
        {
            path = path.Trim();
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("path is " + path));
            UsingToolPath = null;
            bool result = false;
            if (!(path.EndsWith("/") || path.EndsWith("\\"))) path = path + "/";

            var jar = path + @"antlr4-" + version + @"-complete.jar";
            MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Probing " + jar));
            if (jar.StartsWith("file://"))
            {
                try
                {
                    System.Uri uri = new Uri(jar);
                    var local_file = uri.LocalPath;
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Local path " + local_file));
                    if (File.Exists(local_file))
                    {
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found."));
                        UsingToolPath = local_file;
                        result = true;
                    }
                }
                catch
                {
                }
            }
            else if (jar.StartsWith("https://"))
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
                        MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Downloading " + jar));
                        webClient.DownloadFile(jar, archive_name);
                    }
                    MessageQueue.EnqueueMessage(Message.BuildInfoMessage("Found. Saving to "
                        + archive_name));
                    UsingToolPath = archive_name;
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
                        + jar
                        + "', which doesn't start with 'file://' or 'https://'. "
                        + @"Edit your .csproj file to make sure the path follows that syntax."));
            }
            return result;
        }
    }
}
