using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Linq;
using System.Collections.Generic;
using Directory = System.IO.Directory;

namespace Antlr4.Build.Tasks
{
    public class MakeRelativeList : Task
    {
        private List<ITaskItem> _result = new List<ITaskItem>();
        internal string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();		
        public ITaskItem[] List1 { get; set; }

        [Output] public ITaskItem[] Result
        {
            get { return _result.ToArray(); }
            set { _result = new List<ITaskItem>(value); }
        }

        public override bool Execute()
        {
            try
            {
                _result.Clear();
                if (List1 == null) 
                    List1 = Array.Empty<ITaskItem>();

                var slash = Path.DirectorySeparatorChar;

                // Normalize workDir
                var workDir = Path.GetFullPath(WorkingDirectory);
                if (!workDir.EndsWith(slash)) workDir += slash;
                var workDirRoot = Path.GetPathRoot(workDir);
                var workDirFolders = workDir.Substring(workDirRoot.Length).Split(slash, StringSplitOptions.RemoveEmptyEntries);

                this.Log.LogMessage("MakeRelativeList input: '{0}'", String.Join("', '", List1.AsEnumerable()));
                this.Log.LogMessage("MakeRelativeList working directory: '{0}'", workDir);

                foreach (var v1 in List1)
                {
                    if (v1 == null)
                        continue;

                    try
                    {
                        var path = v1.ItemSpec;
                        if (!Path.IsPathRooted(path))
                        {
                            // It is already relative. Just clean it up.
                            path = path.Replace(slash == '/' ? '\\' : '/', slash);
                            _result.Add(new TaskItem(path));
                            continue;
                        }

                        // - `path` is a full file or directory path.
                        // - `workDir` is a directory path ending with slash.

                        // Convert to a relative path *if possible*...
                        // If we upgrade to netstandard2.1 we can use `IO.Path.GetRelativePath`.
                        // Until then we have to do it the hard way...

                        var comparison =
                            Environment.OSVersion.Platform == PlatformID.Unix
                            ? StringComparison.Ordinal
                            : StringComparison.OrdinalIgnoreCase;
                        var comparer = StringComparer.FromComparison(comparison);

                        // Normalize path
                        path = Path.GetFullPath(path);

                        // 0) Is path a direct subpath of workDir. This is often true.
                        if (path.StartsWith(workDir, comparison))
                        {
                            // Direct subpath
                            var relative_path1 = path.Substring(workDir.Length);
                            if (relative_path1 == "") relative_path1 = ".";
                            _result.Add(new TaskItem(relative_path1));
                            continue;
                        }

                        // 1) To be relative they must have a common root...
                        var pathRoot = Path.GetPathRoot(path);
                        if (!comparer.Equals(workDirRoot, pathRoot))
                        {
                            // Cannot be relative
                            _result.Add(new TaskItem(path));
                            continue;
                        }

                        // 2) Ignore all leading directories they have in common (beware UNC paths)...
                        var pathSegments = path.Substring(pathRoot.Length).Split(slash, StringSplitOptions.RemoveEmptyEntries);

                        var numCommon = 
                            Enumerable
                                .Zip(workDirFolders, pathSegments)
                                .TakeWhile(t => comparer.Equals(t.First, t.Second))
                                .Count();

                        var remainingWorkDirs = workDirFolders.Skip(numCommon);
                        var remainingPathSegments = pathSegments.Skip(numCommon);

                        // 2) What remains in `remainingWorkDirs` is the number of ".."s 
                        //    needed to get to the base of `remainingPathSegments`...
                        var relative_path = String.Join(slash, remainingWorkDirs.Select(_ => "..").Concat(remainingPathSegments));
                        if (relative_path == "") relative_path = ".";
                        if (path.EndsWith(slash)) relative_path += slash;
                        _result.Add(new TaskItem(relative_path));
                    }
                    catch (Exception e)
                    {
                        this.Log.LogWarning("Error in MakeRelativeList while parsing '{0}': {1}\n{2}", v1.ItemSpec, e.Message, e.StackTrace);
                        _result.Add(v1);
                    }
                }

                this.Log.LogMessage("MakeRelativeList output is '{0}'", String.Join("', '", _result));
                return true; // success!
            }
            catch (Exception e)
            {
                this.Log.LogWarning("Error in MakeRelativeList: {0}\n{1}", e.Message, e.StackTrace);
                return false; // failure
            }
        }
    }
}
