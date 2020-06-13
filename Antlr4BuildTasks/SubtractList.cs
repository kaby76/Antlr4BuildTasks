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
            _result = new List<ITaskItem>(List1);
            foreach (var v2 in List2)
            {
                if (_result.Contains(v2))
                    _result.Remove(v2);
            }
            return true;
        }
    }
}
