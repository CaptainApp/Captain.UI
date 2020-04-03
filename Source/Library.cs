using Captain.Common;

namespace Captain.UI {
  /// <summary>
  ///   Common library-wide methods
  /// </summary>
  internal static class Library {
    /// <summary>
    ///   Global logger instance
    /// </summary>
    internal static Logger Log { get; private set; } = new Logger();

    /// <summary>
    ///   Initializes the shared library
    /// </summary>
    internal static void AttachLogger(Logger logger) { Log = logger; }
  }
}
