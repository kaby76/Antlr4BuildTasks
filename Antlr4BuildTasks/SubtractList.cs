using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Directory = System.IO.Directory;
using File = System.IO.File;
using FileAttributes = System.IO.FileAttributes;
using Path = System.IO.Path;

namespace Antlr4.Build.Tasks
{
    public class SubtractList
            : Task
    {
        public ITaskItem[] List1
        {
            get;
            set;
        }

        public ITaskItem[] List2
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
            if (List1 != null) foreach (var v1 in List1)
            {
                bool found = false;
                var v1_name = v1.ToString();
                foreach (var v2 in List2)
                {
                    var v2_name = v2.ToString();
                    if (v2_name == v1_name)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) _result.Add(v1);
            }
            return true;
        }
    }
}
