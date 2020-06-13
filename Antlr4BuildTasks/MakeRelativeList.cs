using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using Directory = System.IO.Directory;
using Path = System.IO.Path;

namespace Antlr4.Build.Tasks
{
    public class MakeRelativeList
            : Task
    {
        public ITaskItem[] List1
        {
            get;
            set;
        }

        private List<ITaskItem> _result = new List<ITaskItem>();

        [Output]
        public ITaskItem[] Result
        {
            get
            {
                return _result.ToArray();
            }
            set
            {
                _result = new List<ITaskItem>(value);
            }
        }

        public override bool Execute()
        {
            _result = new List<ITaskItem>();
            string current = Directory.GetCurrentDirectory();
            foreach (var v2 in List1)
            {
                var f = v2.ToString();
                var is_full_path = System.IO.Path.IsPathRooted(f);
                if (!is_full_path)
                {
                    f = System.IO.Path.GetFullPath(f);
                }
                var absolute = f;
                var relative_path = f.Replace(current,"");
                if (relative_path[0] == '/' || relative_path[0] == '\\')
                {
                    relative_path = relative_path.Substring(1);
                }
                _result.Add(new TaskItem() { ItemSpec = relative_path });
            }
            return true;
        }
    }
}
