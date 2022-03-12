// Copyright (c) Terence Parr, Sam Harwell. All Rights Reserved.
// Licensed under the BSD License. See LICENSE.txt in the project root for license information.

namespace Antlr4.Build.Tasks
{
    using System;
    using System.Text.RegularExpressions;


    internal class Message
    {
        public Message(string message)
        {
            Severity = TraceLevel.Info;
            TheMessage = message;
            FileName = "";
            LineNumber = 0;
            ColumnNumber = 0;
            try
            {
                Regex regex = new Regex(@"^\s*(?<SEVERITY>[a-z]+)\((?<CODE>[0-9]+)\):\s*((?<FILE>.*):(?<LINE>[0-9]+):(?<COLUMN>[0-9]+):)?\s*(?:syntax error:\s*)?(?<MESSAGE>.*)$", RegexOptions.Compiled);
                Match match = regex.Match(message);
                if (match.Success)
                {
                    FileName = match.Groups["FILE"].Length > 0 ? match.Groups["FILE"].Value : "";
                    LineNumber = match.Groups["LINE"].Length > 0 ? int.Parse(match.Groups["LINE"].Value) : 0;
                    ColumnNumber = match.Groups["COLUMN"].Length > 0 ? int.Parse(match.Groups["COLUMN"].Value) + 1 : 0;

                    switch (match.Groups["SEVERITY"].Value)
                    {
                        case "warning":
                            Severity = TraceLevel.Warning;
                            break;
                        case "error":
                            Severity = TraceLevel.Error;
                            break;
                        default:
                            Severity = TraceLevel.Info;
                            break;
                    }
                }
                else
                {
                    TheMessage = message;
                }
            }
            catch (Exception ex)
            {
                if (RunAntlrTool.IsFatalException(ex))
                    throw;
            }
        }

        public static Message BuildDefaultMessage(string message)
        {
            var self = new Message(message);
            return self;
        }

        public static Message BuildErrorMessage(string message)
        {
            var self = new Message(message);
            self.Severity = TraceLevel.Error;
            return self;
        }

        public static Message BuildWarningMessage(string message)
        {
            var self = new Message(message);
            self.Severity = TraceLevel.Warning;
            return self;
        }

        public static Message BuildInfoMessage(string message)
        {
            var self = new Message(message);
            self.Severity = TraceLevel.Info;
            return self;
        }

        public static Message BuildCrashMessage(string message)
        {
            var self = new Message();
            self.Severity = TraceLevel.Error;
            self.TheMessage = message;
            self.FileName = "";
            self.LineNumber = 0;
            self.ColumnNumber = 0;
            try
            {
                Regex regex = new Regex(@"^\s*(?<SEVERITY>[a-z]+)\((?<CODE>[0-9]+)\):\s*((?<FILE>.*):(?<LINE>[0-9]+):(?<COLUMN>[0-9]+):)?\s*(?:syntax error:\s*)?(?<MESSAGE>.*)$", RegexOptions.Compiled);
                Match match = regex.Match(message);
                if (match.Success)
                {
                    self.FileName = match.Groups["FILE"].Length > 0 ? match.Groups["FILE"].Value : "";
                    self.LineNumber = match.Groups["LINE"].Length > 0 ? int.Parse(match.Groups["LINE"].Value) : 0;
                    self.ColumnNumber = match.Groups["COLUMN"].Length > 0 ? int.Parse(match.Groups["COLUMN"].Value) + 1 : 0;
                }
                else
                {
                    self.TheMessage = message;
                }
            }
            catch (Exception ex)
            {
            }
            return self;
        }

        public Message(TraceLevel severity, string message, string fileName, int lineNumber, int columnNumber)
        {
            Severity = severity;
            TheMessage = message;
            FileName = fileName;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public Message()
        {
        }

        public TraceLevel Severity
        {
            get;
            set;
        }

        public string TheMessage
        {
            get;
            set;
        }

        public string FileName
        {
            get;
            set;
        }

        public int LineNumber
        {
            get;
            set;
        }

        public int ColumnNumber
        {
            get;
            set;
        }
    }
}
