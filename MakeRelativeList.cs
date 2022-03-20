using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using Directory = System.IO.Directory;

namespace Antlr4.Build.Tasks
{
    public class MakeRelativeList : Task
    {
        public ITaskItem[] List1 { get; set; }
        private List<ITaskItem> _result = new List<ITaskItem>();

        [Output] public ITaskItem[] Result
        {
            get { return _result.ToArray(); }
            set { _result = new List<ITaskItem>(value); }
        }

        public override bool Execute()
        {
            Log.LogMessage("Yo I just entered MakeRelativeList");
            bool success = false;
            try
            {
                _result = new List<ITaskItem>();
                string current = Directory.GetCurrentDirectory();
                if (List1 != null)
                {
                    foreach (var v1 in List1)
                    {
                        if (v1 == null)
                            continue;
                        var f = v1.ItemSpec.ToString();
                        try
                        {
                            var is_full_path = System.IO.Path.IsPathRooted(f);
                            if (!is_full_path)
                                f = System.IO.Path.GetFullPath(f);
                            var absolute = f;
                            var relative_path = f.Replace(current, "");
                            if (relative_path[0] == '/' || relative_path[0] == '\\')
                                relative_path = relative_path.Substring(1);
                            _result.Add(new TaskItem() { ItemSpec = relative_path });
                        }
                        catch
                        {
                            _result.Add(v1);
                        }
                    }
                }
                success = true;
            }
            catch (Exception e)
            {
                this.Log.LogMessage("Problem with MakeRelativeList "
                    + e.Message + e.StackTrace
                );
            }
            return success;
        }
    }
}
