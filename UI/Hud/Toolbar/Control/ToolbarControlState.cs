using System;

namespace Captain.UI {
  /// <summary>
  ///   Represents toolbar control states
  /// </summary>
  [Flags]
  public enum ToolbarControlState {
    /// <summary>
    ///   Control is idle
    /// </summary>
    None = 0,

    /// <summary>
    ///   Mouse pointer is over the control
    /// </summary>
    Hovered,

    /// <summary>
    ///   The primary mouse button is over the control
    /// </summary>
    Active
  }
}