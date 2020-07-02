using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using Directory = System.IO.Directory;
using File = System.IO.File;


namespace Antlr4.Build.Tasks
{
    public class GetJava : Task
    {

        public string JavaExec
        {
            get;
            set;
        }


        [Required]
        public string IntermediateOutputPath
        {
            get;
            set;
        }

        [Output]
        public string UsingJavaExec
        {
            get;
            set;
        }

        public override bool Execute()
        {
            if (JavaExec == null || JavaExec == "")
            {
                // Download Java from web.
                // https://download.java.net/java/GA/jdk14.0.1/664493ef4a6946b186ff29eb326336a2/7/GPL/openjdk-14.0.1_windows-x64_bin.zip
                string zip = "";
                if (System.Environment.OSVersion.Platform == PlatformID.Win32NT
                    || System.Environment.OSVersion.Platform == PlatformID.Win32S
                    || System.Environment.OSVersion.Platform == PlatformID.Win32Windows
                )
                {
                    zip = @"jre.zip";
                    string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string archive_path = Path.GetFullPath(assemblyPath
                           + System.IO.Path.DirectorySeparatorChar
                           + ".."
                           + System.IO.Path.DirectorySeparatorChar
                           + ".."
                           + System.IO.Path.DirectorySeparatorChar
                           + "build"
                           + System.IO.Path.DirectorySeparatorChar
                           + System.IO.Path.GetFileName(zip)
                    );
                    var java_dir = IntermediateOutputPath;
                    //if (!(java_dir.Substring(java_dir.Length - 1) == "\\"
                    //      || java_dir.Substring(java_dir.Length - 1) == "/"))
                    //    java_dir = java_dir + System.IO.Path.DirectorySeparatorChar;
                    java_dir = java_dir + "Java";
                    if (!Directory.Exists(java_dir))
                    {
                        System.IO.Directory.CreateDirectory(java_dir);
                        System.IO.Compression.ZipFile.ExtractToDirectory(archive_path, java_dir);
                    }
                    UsingJavaExec = java_dir
                                    + System.IO.Path.DirectorySeparatorChar
                                    + "jre"
                                    + System.IO.Path.DirectorySeparatorChar
                                    + "bin"
                                    + System.IO.Path.DirectorySeparatorChar
                                    + "java.exe";
                    UsingJavaExec = UsingJavaExec.Replace("\\\\", "\\");
                    UsingJavaExec = UsingJavaExec.Replace("//", "/");
                }
                else throw new Exception("Which OS??");
            }
            else
            {
                // Remove execessive '\\' or '//'.
                UsingJavaExec = JavaExec.Replace("\\\\", "\\");
                UsingJavaExec = UsingJavaExec.Replace("//", "/");
            }
            return true;
        }
    }
}
