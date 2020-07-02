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
    public class ComputePackageReferences : Task
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
            if (List1 == null)
                return true;

            bool found = false;
            foreach (var v1 in List1)
            {
                Log.LogMessage("I got " + v1.ToString());
                if (v1.ItemSpec == "Antlr4.Runtime.Standard")
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                _result.Add(new TaskItem("Antlr4.Runtime.Standard"));
            return true;
        }
    }
}
