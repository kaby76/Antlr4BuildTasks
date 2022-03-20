using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;

namespace Antlr4.Build.Tasks
{
    public class SubtractList : Task
    {
        public ITaskItem[] List1 { get; set; }
        public ITaskItem[] List2 { get; set; }
        private List<ITaskItem> _result = new List<ITaskItem>();

        [Output]
        public ITaskItem[] Result
        {
            get { return _result.ToArray(); }
            set { _result = new List<ITaskItem>(value); }
        }


        public override bool Execute()
        {
            _result = new List<ITaskItem>();
            if (List1 != null)
            {
                foreach (var v1 in List1)
                {
                    if (v1 == null)
                        continue;
                    bool found = false;
                    var v1_name = v1.ToString();
                    if (List2 != null)
                    {
                        foreach (var v2 in List2)
                        {
                            if (v2 == null)
                                continue;
                            var v2_name = v2.ToString();
                            if (v2_name == v1_name)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                    if (!found) _result.Add(v1);
                }
            }
            return true;
        }
    }
}
