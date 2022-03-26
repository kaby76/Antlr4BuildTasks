using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Antlr4.Build.Tasks
{
    public class MessageQueue
    {
        private static StackQueue<Message> _messages = new StackQueue<Message>();

        public static void EmptyMessageQueue(TaskLoggingHelper log)
        {
            while (_messages.Count > 0)
            {
                var message = _messages.DequeueBottom();
                string errorCode;
                switch (message.Severity)
                {
                    case TraceLevel.Error:
                        errorCode = "ANT02";
                        break;
                    case TraceLevel.Warning:
                        errorCode = "ANT01";
                        break;
                    case TraceLevel.Info:
                        errorCode = "ANT00";
                        break;
                    default:
                        errorCode = "ANT00";
                        break;
                }
                var logMessage = message.TheMessage;
                string subcategory = null;
                string helpKeyword = null;
                switch (message.Severity)
                {
                    case TraceLevel.Error:
                        log.LogError(subcategory, errorCode, helpKeyword, message.FileName, message.LineNumber, message.ColumnNumber, 0, 0, logMessage);
                        break;
                    case TraceLevel.Warning:
                        log.LogWarning(subcategory, errorCode, helpKeyword, message.FileName, message.LineNumber, message.ColumnNumber, 0, 0, logMessage);
                        break;
                    case TraceLevel.Info:
                        log.LogMessage(MessageImportance.Normal, logMessage);
                        break;
                    case TraceLevel.Verbose:
                        log.LogMessage(MessageImportance.Low, logMessage);
                        break;
                }
            }
        }

        public static void EnqueueMessage(Message message)
        {
            _messages.Push(message);
        }

        internal static void MutateToError()
        {
            for (int i = 0; i < _messages.Count; ++i)
            {
                var message = _messages[i];
                message.Severity = TraceLevel.Error;
            }
        }
    }
}
