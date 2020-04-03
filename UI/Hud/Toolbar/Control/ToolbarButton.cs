using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   Represents a rectangle-shaped button
  /// </summary>
  public class ToolbarButton : ToolbarButton<RectangleGeometry> {
    /// <inheritdoc />
    /// <summary>
    ///   Class constructor
    /// </summary>
    /// <param name="toolbar">Parent toolbar</param>
    public ToolbarButton(Toolbar toolbar) : base(toolbar) { }

    /// <inheritdoc />
    /// <summary>
    ///   Creates a geometry object for this control
    /// </summary>
    /// <returns>A new geometry object</returns>
    protected override RectangleGeometry RefreshGeometry() =>
      new RectangleGeometry(Toolbar.ToolbarRenderTarget.Factory,
        new RawRectangleF(Location.X, Location.Y, Location.X + Size.X, Location.Y + Size.Y));
  }

  /// <inheritdoc />
  /// <summary>
  ///   Provides simple user input in form of buttons
  /// </summary>
  /// <typeparam name="T">Button geometry type</typeparam>
  public abstract class ToolbarButton<T> : ToolbarControl where T : Geometry {
    /// <summary>
    ///   Background brush to be used when the control is active
    /// </summary>
    private SolidColorBrush activeBackgroundBrush;

    /// <summary>
    ///   Background brush
    /// </summary>
    private SolidColorBrush backgroundBrush;

    /// <summary>
    ///   Icon bitmap
    /// </summary>
    private Bitmap bitmap;

    /// <summary>
    ///   Background brush to be used when the control is hovered
    /// </summary>
    private SolidColorBrush hoverBackgroundBrush;

    /// <summary>
    ///   Button geometry
    /// </summary>
    protected Lazy<T> Geometry { get; }

    /// <summary>
    ///   When <c>true</c> the button icon will be rendered
    /// </summary>
    public bool ShowIcon { get; set; } = true;

    /// <summary>
    ///   Gets or sets the bitmap to be rendered alongside this control
    /// </summary>
    public Bitmap Bitmap {
      get => this.bitmap;
      set {
        this.bitmap = value;
        Refresh();
      }
    }

    /// <summary>
    ///   Button background color
    /// </summary>
    public RawColor4 BackgroundColor {
      get => this.backgroundBrush?.Color ?? Color.Zero;
      set {
        this.backgroundBrush?.Dispose();
        this.backgroundBrush = new SolidColorBrush(Toolbar.ToolbarRenderTarget, value);
      }
    }

    /// <summary>
    ///   Hovered button background color
    /// </summary>
    public RawColor4 HoveredBackgroundColor {
      get => this.hoverBackgroundBrush?.Color ?? Color.Zero;
      set {
        this.hoverBackgroundBrush?.Dispose();
        this.hoverBackgroundBrush = new SolidColorBrush(Toolbar.ToolbarRenderTarget, value);
      }
    }

    /// <summary>
    ///   Active button background color
    /// </summary>
    public RawColor4 ActiveBackgroundColor {
      get => this.activeBackgroundBrush?.Color ?? Color.Zero;
      set {
        this.activeBackgroundBrush?.Dispose();
        this.activeBackgroundBrush = new SolidColorBrush(Toolbar.ToolbarRenderTarget, value);
      }
    }

    /// <inheritdoc />
    /// <summary>
    ///   Class constructor
    /// </summary>
    /// <param name="toolbar">Parent toolbar</param>
    protected ToolbarButton(Toolbar toolbar) : base(toolbar) {
      if (this.hoverBackgroundBrush == null) { HoveredBackgroundColor = Color.FromRgba(0x20FFFFFF); }
      if (this.activeBackgroundBrush == null) { ActiveBackgroundColor = Color.FromRgba(0x40FFFFFF); }
      Geometry = new Lazy<T>(RefreshGeometry);
    }

    /// <summary>
    ///   Creates a geometry object for this control
    /// </summary>
    /// <returns>A new geometry object</returns>
    protected abstract T RefreshGeometry();

    /// <inheritdoc />
    /// <summary>
    ///   Releases resources used by the control
    /// </summary>
    public override void Dispose() {
      this.backgroundBrush?.Dispose();
      this.hoverBackgroundBrush?.Dispose();
      this.activeBackgroundBrush?.Dispose();
      this.bitmap?.Dispose();
      Geometry.Value?.Dispose();

      this.backgroundBrush = null;
      this.hoverBackgroundBrush = null;
      this.activeBackgroundBrush = null;
      this.bitmap = null;

      base.Dispose();
    }

    /// <inheritdoc />
    /// <summary>
    ///   Performs hit testing against a point
    /// </summary>
    /// <param name="point">Point to be tested against</param>
    /// <returns>Whether the point belongs to the control region or not</returns>
    public override bool HitTest(Vector2 point) =>
      Geometry.Value?.FillContainsPoint(point) ?? false;

    /// <inheritdoc />
    /// <summary>
    ///   Renders the control
    /// </summary>
    /// <param name="target">Render target</param>
    public override void Render(RenderTarget target) {
      var center = new Vector2(Location.X + Size.X / 2, Location.Y + Size.Y / 2);
      Brush brush = this.backgroundBrush;

      if ((State & ToolbarControlState.Active) != 0) {
        // control is active
        brush = this.activeBackgroundBrush;
      } else if ((State & ToolbarControlState.Hovered) != 0) {
        // control is hovered
        brush = this.hoverBackgroundBrush;
      }

      if (Geometry.Value != null && brush != null) { target.FillGeometry(Geometry.Value, brush); }
      if (ShowIcon && this.bitmap != null) {
        target.DrawBitmap(this.bitmap,
          new RawRectangleF(
            center.X - this.bitmap.Size.Width / 2,
            center.Y - this.bitmap.Size.Height / 2,
            center.X + this.bitmap.Size.Width / 2,
            center.Y + this.bitmap.Size.Height / 2),
          1,
          BitmapInterpolationMode.NearestNeighbor);
      }
    }
  }
}