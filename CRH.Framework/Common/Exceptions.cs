using System;

namespace CRH.Framework.Common
{
    /// <summary>
    /// Framework's general exception
    /// </summary>
    internal class FrameworkException : Exception
    {
        public FrameworkException()
        {}

        public FrameworkException(string message)
            : base(message)
        {}

        public FrameworkException(string message, params object[] obj)
            : base(string.Format(message, obj))
        {}

        public FrameworkException(string message, Exception inner)
            : base(message, inner)
        {}
    }

    /// <summary>
    /// Exception for features that are not yet implemented
    /// </summary>
    internal class FrameworkNotYetImplementedException : FrameworkException
    {
        public FrameworkNotYetImplementedException()
        {}

        public FrameworkNotYetImplementedException(string message)
            : base(message)
        {}

        public FrameworkNotYetImplementedException(string message, params object[] obj)
            : base(string.Format(message, obj))
        {}

        public FrameworkNotYetImplementedException(string message, Exception inner)
            : base(message, inner)
        {}
    }

    /// <summary>
    /// Exception for features that are not supported and will never be (who said 'never say never ?')
    /// </summary>
    internal class FrameworkNotSupportedException : FrameworkException
    {
        public FrameworkNotSupportedException()
        {}

        public FrameworkNotSupportedException(string message)
            : base(message)
        {}

        public FrameworkNotSupportedException(string message, params object[] obj)
            : base(string.Format(message, obj))
        {}

        public FrameworkNotSupportedException(string message, Exception inner)
            : base(message, inner)
        {}
    }
}
