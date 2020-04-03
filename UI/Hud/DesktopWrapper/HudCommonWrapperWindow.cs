using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Captain.Common.Native;
using SharpDX.Windows;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   Common wrapper window for HUD components.
  /// </summary>
  public class HudCommonWrapperWindow : RenderForm {
    /// <summary>
    ///   Width/height for resize handles
    /// </summary>
    private const int ResizeHandleSize = 24;

    /// <inheritdoc />
    /// <summary>Gets the required creation parameters when the control handle is created.</summary>
    /// <returns>
    ///   A <see cref="T:System.Windows.Forms.CreateParams" /> that contains the required creation parameters when the
    ///   handle to the control is created.
    /// </returns>
    protected override CreateParams CreateParams {
      get {
        CreateParams createParams = base.CreateParams;

        // remove all window styles
        createParams.Style = 0;

        // WS_EX_TOOLWINDOW hides the wrapper from the Alt-Tab menu
        createParams.ExStyle = (int) User32.WindowStylesEx.WS_EX_TOOLWINDOW |
                               (int) User32.WindowStylesEx.WS_EX_COMPOSITED |
                               (int) User32.WindowStylesEx.WS_EX_LAYERED |
                               (int) User32.WindowStylesEx.WS_EX_TRANSPARENT |
                               (int) User32.WindowStylesEx.WS_EX_NOACTIVATE;

        if (DwmApi.DwmIsCompositionEnabled(out bool isComposited) == 0 && !isComposited) {
          // add a shadow on non-composited environments
          createParams.ClassStyle = (int) User32.ClassStyles.CS_DROPSHADOW;
        }

        return createParams;
      }
    }

    /// <summary>
    ///   When set to <c>true</c>, hit tests won't be made on this window
    /// </summary>
    public bool PassThrough {
      get {
        int exStyles = User32.GetWindowLongPtr(Handle, User32.WindowLongParam.GWL_EXSTYLE).ToInt32();
        return (exStyles & (int) User32.WindowStylesEx.WS_EX_TRANSPARENT) != 0;
      }

      set {
        int exStyle = User32.GetWindowLongPtr(Handle, User32.WindowLongParam.GWL_EXSTYLE).ToInt32();

        if (value) {
          // set transparent bit
          exStyle |= (int) User32.WindowStylesEx.WS_EX_TRANSPARENT;
        } else {
          // unset transparent bit
          exStyle &= ~(int) User32.WindowStylesEx.WS_EX_TRANSPARENT;
        }

        User32.SetWindowLongPtr(Handle,
                                User32.WindowLongParam.GWL_EXSTYLE,
                                new IntPtr(exStyle));
      }
    }

    /// <summary>
    ///   Rendering loop procedure
    /// </summary>
    public RenderLoop.RenderCallback RenderLoop { private get; set; }

    /// <summary>
    ///   Allows the wrapper window to be resized and moved
    /// </summary>
    public bool Resizable { get; set; } = true;

    /// <summary>
    ///   Client area margins
    /// </summary>
    protected virtual MARGINS Margins { get; } = new MARGINS {
      bottomWidth = -1,
      leftWidth = -1,
      rightWidth = -1,
      topWidth = -1
    };

    /// <inheritdoc />
    public sealed override Color BackColor {
      get => base.BackColor;
      set => base.BackColor = value;
    }

    /// <inheritdoc />
    /// <summary>
    ///   Class constructor
    /// </summary>
    public HudCommonWrapperWindow() {
      ShowInTaskbar = ShowIcon = MinimizeBox = MaximizeBox = ControlBox = false;
      AllowTransparency = TopMost = true;
      StartPosition = FormStartPosition.Manual;
      FormBorderStyle = FormBorderStyle.None;
      BackColor = Color.Black;
      Location = new Point(-0x7FFF, -0x7FFF);
    }

    /// <inheritdoc />
    /// <summary>
    ///   Processes window messages
    /// </summary>
    /// <param name="msg">Message</param>
    protected override void WndProc(ref Message msg) {
      base.WndProc(ref msg);

      switch (msg.Msg) {
        case (int) User32.WindowMessage.WM_NCHITTEST
          when Resizable:
          Point position = PointToClient(MousePosition);

          if (position.X <= ResizeHandleSize && position.Y <= ResizeHandleSize) {
            // top-left
            msg.Result = new IntPtr((int) User32.HitTestValues.HTTOPLEFT);
          } else if (position.X >= Width - ResizeHandleSize && position.Y >= Height - ResizeHandleSize) {
            // bottom right corner
            msg.Result = new IntPtr((int) User32.HitTestValues.HTBOTTOMRIGHT);
          } else if (position.X <= ResizeHandleSize && position.Y >= Height - ResizeHandleSize) {
            // bottom-left corner
            msg.Result = new IntPtr((int) User32.HitTestValues.HTBOTTOMLEFT);
          } else if (position.X >= Width - ResizeHandleSize && position.Y <= ResizeHandleSize) {
            // top-right corner
            msg.Result = new IntPtr((int) User32.HitTestValues.HTTOPRIGHT);
          } else if (position.Y <= Height - ResizeHandleSize && position.X <= ResizeHandleSize) {
            msg.Result = new IntPtr((int) User32.HitTestValues.HTLEFT);
          } else if (position.Y <= Height - ResizeHandleSize && position.X >= Width - ResizeHandleSize) {
            msg.Result = new IntPtr((int) User32.HitTestValues.HTRIGHT);
          } else if (position.Y <= ResizeHandleSize) {
            // top edge
            msg.Result = new IntPtr((int) User32.HitTestValues.HTTOP);
          } else if (position.Y >= Height - ResizeHandleSize) {
            // bottom edge
            msg.Result = new IntPtr((int) User32.HitTestValues.HTBOTTOM);
          } else {
            // screen region
            msg.Result = new IntPtr((int) User32.HitTestValues.HTCAPTION);
          }

          break;

        case (int) User32.WindowMessage.WM_PAINT:
          RenderLoop();
          break;

        case (int) User32.WindowMessage.WM_SETCURSOR
          when Resizable && (msg.LParam.ToInt32() & 0x0000FFFF) == (int) User32.HitTestValues.HTCAPTION:
          Cursor.Current = Cursors.SizeAll;
          break;

        case (int) User32.WindowMessage.WM_WINDOWPOSCHANGING:
          var pos = (User32.WINDOWPOS) Marshal.PtrToStructure(msg.LParam, typeof(User32.WINDOWPOS));

          if ((pos.flags & (int) User32.SetWindowPosFlags.SWP_NOACTIVATE) == 0) {
            // HACK: when windows get moved beyond the top limits of the screen, windows automatically snaps the bounds
            //       so that the "title bar" (which in our case is non-existant) remains visible. SWP_NOACTIVATE flag
            //       is set by Windows, so we can filter position change messages and ignore those with the flag.
            //       This way the user is allowed to size the grabber UI however they want
            pos.flags |= (int) User32.SetWindowPosFlags.SWP_NOMOVE | (int) User32.SetWindowPosFlags.SWP_NOSIZE;
          }


          Marshal.StructureToPtr(pos, msg.LParam, false);
          break;
      }
    }

    /// <summary>
    ///   Enables window blur styles
    /// </summary>
    public void EnableBlurBehind() {
      if (Environment.OSVersion.Version.Major >= 10) {
        /* Windows 10 blur */
        var attrValue = new IntPtr((int) DwmApi.DwmNcRenderingPolicy.DWMNCRP_DISABLED);
        DwmApi.DwmSetWindowAttribute(Handle,
                                     DwmApi.DwmWindowAttribute.DWMWA_NCRENDERING_POLICY,
                                     ref attrValue,
                                     Marshal.SizeOf(typeof(int)));

        try {
          // HACK(sanlyx): we're using undocumented APIs to display blur here - replace with something better
          //               when you've got nothing better to do
          var accent = new User32.AccentPolicy {
            AccentState = User32.AccentState.ACCENT_ENABLE_BLURBEHIND
          };

          int accentStructSize = Marshal.SizeOf(accent);

          // allocate space for the struct
          IntPtr accentPtr = Marshal.AllocHGlobal(accentStructSize);
          Marshal.StructureToPtr(accent, accentPtr, false);

          // set composition data
          var data = new User32.WindowCompositionAttributeData {
            Attribute = User32.WindowCompositionAttribute.WCA_ACCENT_POLICY,
            SizeOfData = accentStructSize,
            Data = accentPtr
          };

          // change window composition attributes and release resources
          User32.SetWindowCompositionAttribute(Handle, ref data);
          Marshal.FreeHGlobal(accentPtr);
        } catch {
          /* unsupported feature? */
        }
      }
    }

    /// <inheritdoc />
    /// <summary>
    ///   Sets up the window styling upon handle creation.
    /// </summary>
    /// <param name="eventArgs">Arguments passed to this event.</param>
    protected override void OnHandleCreated(EventArgs eventArgs) {
      // make the window actually borderless and set transparency attributes so we can use alpha blending when
      // doing the Direct2D rendering
      MARGINS margins = Margins;
      DwmApi.DwmExtendFrameIntoClientArea(Handle, ref margins);
      base.OnHandleCreated(eventArgs);
    }
  }
}