namespace Captain.UI {
  /// <summary>
  ///   Specifies the horizontal positioning policy for a toolbar control
  /// </summary>
  public enum ToolbarControlGravity {
    /// <summary>
    ///   The control is always pulled to the start of the layout
    /// </summary>
    Near,

    /// <summary>
    ///   The control position is not enforced
    /// </summary>
    Zero,

    /// <summary>
    ///   The control is always pulled towards the end of the layout
    /// </summary>
    Far
  }
}