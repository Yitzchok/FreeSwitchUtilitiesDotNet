using System;
using System.Runtime.Serialization;

namespace FreeSwitchUtilities.Irv
{

    public class HangupException : Exception { }
    public class TerminateException : Exception
    {
        public TerminateException() { }

        public TerminateException(string hangupReason)
        {
            HangupReason = hangupReason;
        }

        public TerminateException(string message, string hangupReason)
            : base(message)
        {
            HangupReason = hangupReason;
        }

        public TerminateException(string message, Exception innerException, string hangupReason)
            : base(message, innerException)
        {
            HangupReason = hangupReason;
        }

        protected TerminateException(SerializationInfo info, StreamingContext context, string hangupReason)
            : base(info, context)
        {
            HangupReason = hangupReason;
        }

        public string HangupReason { get; set; }
    }

    public class MaxRetriesExceededException : TerminateException
    {
        public MaxRetriesExceededException() : base("MaxRetriesExceeded") { }
        public MaxRetriesExceededException(string message) : base(message, "MaxRetriesExceeded") { }
        public MaxRetriesExceededException(string message, Exception innerException) : base(message, innerException, "MaxRetriesExceeded") { }
        protected MaxRetriesExceededException(SerializationInfo info, StreamingContext context) : base(info, context, "MaxRetriesExceeded") { }
    }
}