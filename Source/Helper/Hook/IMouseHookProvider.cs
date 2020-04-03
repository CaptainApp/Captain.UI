using System;
using System.Windows.Forms;
using Captain.Common;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   A mouse hook provider implements the logic to capture the mouse and intercept its events. A generic interface
  ///   is provided so the logic is independent of the user interface, so it may be used when hooking on DirectInput/
  ///   XInput-enabled applications
  /// </summary>
  public interface IMouseHookProvider : IDisposable {
    /// <summary>
    ///   Triggered when a mouse button is held
    /// </summary>
    event EventHandler<ExtendedEventArgs<MouseEventArgs, bool>> OnMouseDown;

    /// <summary>
    ///   Triggered when a mouse button is released
    /// </summary>
    event EventHandler<ExtendedEventArgs<MouseEventArgs, bool>> OnMouseUp;

    /// <summary>
    ///   Triggered when the mouse moves
    /// </summary>
    event EventHandler<ExtendedEventArgs<MouseEventArgs, bool>> OnMouseMove;
  }
}