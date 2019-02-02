using System;
namespace Lithnet.Laps.Web.Auth
{
    public class AuthResponse
    {
        public bool Success { get;  private set; }

        /// <summary>
        /// Gets the reader element.
        /// </summary>
        /// <value>The reader element.</value>
        /// <remarks>
        /// We need to get rid of this. But for the moment the reader element
        /// is still needed by the logging/audit.
        /// </remarks>
        [Obsolete]
        public ReaderElement ReaderElement { get; private set; }

        public AuthResponse(bool success, ReaderElement reader)
        {
            Success = success;
            ReaderElement = reader;
        }
    }
}
