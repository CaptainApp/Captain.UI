using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   Simple control rendering a bitmap
  /// </summary>
  public class ToolbarImage : ToolbarControl {
    /// <summary>
    ///   Icon bitmap
    /// </summary>
    private Bitmap bitmap;

    /// <summary>
    ///   Gets or sets the bitmap for this control
    /// </summary>
    public Bitmap Bitmap {
      get => this.bitmap;
      set {
        this.bitmap = value;
        Refresh();
      }
    }

    /// <summary>
    ///   Determines the opacity for the rendered bitmap
    /// </summary>
    public float Opacity { get; set; } = 1;

    /// <inheritdoc />
    /// <summary>
    ///   Class constructor
    /// </summary>
    /// <param name="toolbar">Parent toolbar</param>
    public ToolbarImage(Toolbar toolbar) : base(toolbar) { }

    /// <inheritdoc />
    /// <summary>
    ///   Releases resources used by the control
    /// </summary>
    public override void Dispose() {
      this.bitmap?.Dispose();
      this.bitmap = null;

      base.Dispose();
    }

    /// <inheritdoc />
    /// <summary>
    ///   Performs hit testing against a point
    /// </summary>
    /// <param name="point">Point to be tested against</param>
    /// <returns>Whether the point belongs to the control region or not</returns>
    public override bool HitTest(Vector2 point) => false;

    /// <inheritdoc />
    /// <summary>
    ///   Renders the control
    /// </summary>
    /// <param name="target">Render target</param>
    public override void Render(RenderTarget target) {
      var center = new Vector2(Location.X + Size.X / 2, Location.Y + Size.Y / 2);
      if (this.bitmap != null) {
        target.DrawBitmap(this.bitmap,
          new RawRectangleF(
            center.X - this.bitmap.Size.Width / 2,
            center.Y - this.bitmap.Size.Height / 2,
            center.X + this.bitmap.Size.Width / 2,
            center.Y + this.bitmap.Size.Height / 2),
          Opacity,
          BitmapInterpolationMode.NearestNeighbor);
      }
    }
  }
}