namespace Antlr4.Build.Tasks
{
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.IO;
    using System.Net;
    using System.Reflection;

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

        [Output] public string UsingToolPath
        {
            get;
            set;
        }

        public override bool Execute()
        {
            if (ToolPath == null || ToolPath == "")
            {
                if (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                    || System.Environment.OSVersion.Platform == PlatformID.Win32S
                    || System.Environment.OSVersion.Platform == PlatformID.Win32Windows
                    || System.Environment.OSVersion.Platform == PlatformID.Unix
                    || System.Environment.OSVersion.Platform == PlatformID.MacOSX
                )
                {
                    string version = null;
                    foreach (var i in PackageReference)
                    {
                        if (i.ItemSpec == "Antlr4.Runtime.Standard")
                        {
                            version = i.GetMetadata("Version");
                            break;
                        }
                    }

                    if (version == "4.9" || version == "4.9.0")
                    {
                        // Version exists already in package.
                        string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        string archive_path = Path.GetFullPath(assemblyPath
                                                               + System.IO.Path.DirectorySeparatorChar
                                                               + ".."
                                                               + System.IO.Path.DirectorySeparatorChar
                                                               + ".."
                                                               + System.IO.Path.DirectorySeparatorChar
                                                               + "build"
                                                               + System.IO.Path.DirectorySeparatorChar
                                                               + System.IO.Path.GetFileName(@"antlr-4.9-complete.jar")
                        );
                        UsingToolPath = archive_path;
                    }
                    else
                    {
                        // For all others, try to download the file from the internet.
                        try
                        {
                            var jar =
                                @"https://www.antlr.org/download/antlr-" + version + "-complete.jar";
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
                            UsingToolPath = archive_name;
                        }
                        catch (Exception eeks)
                        {
                            throw new Exception("Cannot download version " + version + " of the Antlr toolset. Please check the version and https://www.antlr.org/download/index.html for an available '-complete.jar' file version. Make sure the version number is exact, e.g., '4.9', not '4.9.0'.");
                        }
                    }
                }
                else throw new Exception("Which OS??");
            }
            else
            {
                UsingToolPath = ToolPath;
            }
            return true;
        }
    }
}
