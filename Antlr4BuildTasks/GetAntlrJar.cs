using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Net;
using System.Reflection;

namespace Antlr4.Build.Tasks
{
    public class GetAntlrJar : Task
    {
        public GetAntlrJar()
        {
        }

        public string ToolPath
        {
            get;
            set;
        }

        public ITaskItem[] PackageReference
        {
            get;
            set;
        }


        public string IntermediateOutputPath
        {
            get;
            set;
        }

        private string result;

        [Output] public string UsingToolPath
        {
            get { return result; }
            set { }
        }

        public override bool Execute()
        {
            if (ToolPath == null || ToolPath == "")
            {
                if (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                    || System.Environment.OSVersion.Platform == PlatformID.Win32S
                    || System.Environment.OSVersion.Platform == PlatformID.Win32Windows
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

                    if (version == "4.8" || version == "4.8.0")
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
                                                               + System.IO.Path.GetFileName(@"antlr-4.8-complete.jar")
                        );
                        result = archive_path;
                    }
                    else if (version == "4.7.2")
                    {
                        // Download.
                        var jar =
                            @"https://www.antlr.org/download/antlr-4.7.2-complete.jar"; WebClient webClient = new WebClient();
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
                        result = archive_name;
                    }
                    else throw new Exception("Unhandled version of Antlr4.Runtime.Standard");
                }
                else throw new Exception("Which OS??");
            }
            else
            {
                result = ToolPath;
            }
            return true;
        }
    }
}
