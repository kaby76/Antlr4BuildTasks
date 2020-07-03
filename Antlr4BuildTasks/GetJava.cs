namespace Antlr4.Build.Tasks
{
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.IO;
    using System.Reflection;
    using Directory = System.IO.Directory;

    public class GetJava : Task
    {
        [Required]
        public string IntermediateOutputPath
        {
            get;
            set;
        }

        public string JavaExec
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
                }
                else throw new Exception("Which OS??");
            }
            else
            {
                UsingJavaExec = JavaExec;
            }
            UsingJavaExec = UsingJavaExec.Replace("\\\\", "\\");
            UsingJavaExec = UsingJavaExec.Replace("//", "/");
            return true;
        }
    }
}
