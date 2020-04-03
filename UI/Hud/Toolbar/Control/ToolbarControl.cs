using System;
using System.Threading;
using SharpDX;
using SharpDX.Direct2D1;
using Color = System.Drawing.Color;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   Abstracts common behaviour for toolbar components
  /// </summary>
  public abstract class ToolbarControl : IDisposable {
    /// <summary>
    ///   Tidbit instance
    /// </summary>
    private Tidbit tidbit;

    /// <summary>
    ///   Parent toolbar
    /// </summary>
    protected Toolbar Toolbar { get; }

    /// <summary>
    ///   Control location
    /// </summary>
    /// <remarks>
    ///   The Z value does not affect appearance but is instead interpreted relatively and specifies the order in which
    ///   controls should be rendered
    /// </remarks>
    public Vector3 Location { get; set; }

    /// <summary>
    ///   Control size
    /// </summary>
    public Vector2 Size { get; set; }

    /// <summary>
    ///   Control gravity
    /// </summary>
    public ToolbarControlGravity Gravity { get; set; } = ToolbarControlGravity.Near;

    /// <summary>
    ///   Control state
    /// </summary>
    public ToolbarControlState State { get; set; } = ToolbarControlState.None;

    /// <summary>
    ///   Gets or sets the enabled state of this button
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///   Tidbit text to be displayed alongside the control
    /// </summary>
    public string Tidbit { get; set; }

    /// <summary>
    ///   Action to be executed upon activation
    /// </summary>
    public Action Action { get; set; }

    /// <summary>
    ///   Class constructor
    /// </summary>
    /// <param name="toolbar">Parent toolbar</param>
    protected ToolbarControl(Toolbar toolbar) => Toolbar = toolbar;

    /// <inheritdoc />
    /// <summary>
    ///   Releases resources used by the control
    /// </summary>
    public virtual void Dispose() {
      this.tidbit?.Dispose();
      this.tidbit = null;
    }

    /// <summary>
    ///   Renders the control
    /// </summary>
    /// <param name="target">Render target</param>
    public abstract void Render(RenderTarget target);

    /// <summary>
    ///   Performs hit testing against a point
    /// </summary>
    /// <param name="point">Point to be tested against</param>
    /// <returns>Whether the point belongs to the control region or not</returns>
    public virtual bool HitTest(Vector2 point) =>
      Enabled &&
      point.X >= Location.X &&
      point.Y >= Location.Y &&
      point.X <= Location.X + Size.X &&
      point.Y <= Location.Y + Size.Y;

    /// <summary>
    ///   Refreshes the control
    /// </summary>
    public virtual void Refresh() {
      if ((State & ToolbarControlState.Hovered) != 0) {
        if ((this.tidbit?.Disposed ?? true) && !String.IsNullOrEmpty(Tidbit)) {
          this.tidbit = new Tidbit(Toolbar.Container, Timeout.InfiniteTimeSpan) {
            Content = Tidbit,
            CustomAccent = Color.Transparent,
            ShowIcon = false
          };
        } else if (!(this.tidbit?.Visible ?? true)) { this.tidbit.Visible = true; }
      } else if (!(this.tidbit?.Disposed ?? true)) { this.tidbit.Visible = false; }

      Toolbar.Refresh();
    }
  }
}