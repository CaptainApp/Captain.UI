using System;
using System.Linq;
using System.Windows.Forms;
using Captain.Common.Native;
using SharpDX;
using Point = System.Drawing.Point;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   Wrapper window for toolbar
  /// </summary>
  public class HudToolbarWrapperWindow : HudCommonWrapperWindow {
    /// <inheritdoc />
    /// <summary>
    ///   Sets the client area margins accordingly so that we receive the native window shadow
    /// </summary>
    protected override MARGINS Margins { get; } = new MARGINS {
      bottomWidth = 0,
      leftWidth = 0,
      rightWidth = 0,
      topWidth = 1
    };

    /// <summary>
    ///   Toolbar instance
    /// </summary>
    public Toolbar Toolbar { get; set; }

    /// <inheritdoc />
    /// <summary>
    ///   Processes window messages for moving the snack bar when the grip is held.
    /// </summary>
    /// <param name="msg">Window message.</param>
    protected override void WndProc(ref Message msg) {
      base.WndProc(ref msg);
      switch (msg.Msg) {
        case (int) User32.WindowMessage.WM_DWMCOLORIZATIONCHANGED:
          // refresh primary button color when the system accent color changes
          if (Toolbar?.Controls["primaryButton"] is ToolbarPrimaryButton primaryButton) {
            primaryButton.RefreshColors();
          }

          break;

        case (int) User32.WindowMessage.WM_NCHITTEST:
          Point pos = PointToClient(MousePosition);

          if (!Toolbar.Locked && Toolbar.Controls.Values.Cast<ToolbarControl>()
                                        .All(control => !control.HitTest(new Vector2(pos.X, pos.Y)))) {
            // if there's no active control at the mouse position, simulate a non-client area click so that the toolbar
            // can be moved around
            msg.Result = new IntPtr((int) User32.HitTestValues.HTCAPTION);
          }

          break;
      }
    }
  }
}