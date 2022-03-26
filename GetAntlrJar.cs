namespace Antlr4.Build.Tasks
{
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;

    public class GetAntlrJar : Task
    {
        public GetAntlrJar() { }

        public string IntermediateOutputPath
        {
            get;
            set;
        }

        public string ToolPath
        {
            get;
            set;
        }


        [Output] public string UsingToolPath
        {
            get;
            set;
        }

        public override bool Execute()
        {
            bool result = false;
            return result;
        }
    }
}
