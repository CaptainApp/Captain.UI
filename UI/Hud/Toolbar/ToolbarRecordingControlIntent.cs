namespace Captain.UI {
  /// <summary>
  ///   Enumerates the intent types for controlling the motion capture flow
  /// </summary>
  public enum ToolbarRecordingControlIntent {
    /// <summary>
    ///   Starts recording
    /// </summary>
    Start,

    /// <summary>
    ///   Stops recording
    /// </summary>
    Stop,

    /// <summary>
    ///   Pauses an ongoing recording
    /// </summary>
    Pause,

    /// <summary>
    ///   Resumes a paused recording
    /// </summary>
    Resume
  }
}
