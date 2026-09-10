using System;

namespace Nolvus.Core.Errors
{
    public class BrowserClosedException : Exception
    {
        public BrowserClosedException()
            : base("The browser window was closed before the operation completed.") { }

        public BrowserClosedException(string message) : base(message) { }
    }
}
