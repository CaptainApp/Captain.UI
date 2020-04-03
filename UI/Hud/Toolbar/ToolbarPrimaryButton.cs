using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using static Captain.UI.Library;
using Color = System.Drawing.Color;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   Primary toolbar button
  /// </summary>
  public class ToolbarPrimaryButton : ToolbarButton<Geometry> {
    /// <summary>
    ///   Button back brush
    /// </summary>
    private Brush backBrush;

    /// <summary>
    ///   Direct2D image
    /// </summary>
    private Image image;

    /// <summary>
    ///   Image size
    /// </summary>
    private Size2F imageSize;

    /// <summary>
    ///   Whether or not to invert the bitmap for better contrast
    /// </summary>
    private bool invert;

    /// <inheritdoc />
    /// <summary>
    ///   Class constructor
    /// </summary>
    /// <param name="toolbar">Parent toolbar</param>
    public ToolbarPrimaryButton(Toolbar toolbar) : base(toolbar) => RefreshColors();

    /// <summary>
    ///   Updates button colors
    /// </summary>
    public void RefreshColors() {
      if (StyleHelper.GetAccentColor() is Color color) {
        var accent = new SharpDX.Color(color.R, color.G, color.B);
        this.backBrush = new SolidColorBrush(Toolbar.ToolbarRenderTarget, accent);

        if (!(this.invert = Environment.OSVersion.Version.Major >= 10 && color.ToYiq() > 0x7F) && this.image != null) {
          this.image.Dispose();
          this.image = null;
          ShowIcon = true;
        }

        accent = SharpDX.Color.AdjustSaturation(accent, 1.0f);
        accent = SharpDX.Color.AdjustContrast(accent, 4);
        HoveredBackgroundColor = accent;
        accent = SharpDX.Color.AdjustSaturation(accent, .75f);
        accent = SharpDX.Color.AdjustContrast(accent, .5f);
        ActiveBackgroundColor = accent;
      } else {
        this.backBrush = new SolidColorBrush(Toolbar.ToolbarRenderTarget, new SharpDX.Color(76, 29, 33));
        HoveredBackgroundColor = new SharpDX.Color(109, 32, 38);
        ActiveBackgroundColor = new SharpDX.Color(138, 44, 52);
      }
    }

    /// <inheritdoc />
    /// <summary>
    ///   Refreshes the control
    /// </summary>
    public override void Refresh() {
      if (this.invert) {
        if (Bitmap != null) {
          // invert button bitmap
          using (var ctx = Toolbar.ToolbarRenderTarget.QueryInterface<DeviceContext>()) {
            try {
              var effect = new Effect(ctx, Effect.Invert);
              effect.SetInput(0, Bitmap, true);

              // set image so that the bitmap does not get rendered
              this.image = effect.Output;
              this.imageSize = Bitmap.Size;
            } catch (Exception exception) {
              Log.Warn($"error inverting bitmap: {exception}");
            }
          }

          ShowIcon = false;
        }
      }

      base.Refresh();
    }

    /// <inheritdoc />
    /// <summary>
    ///   Creates a geometry object for this control
    /// </summary>
    /// <returns>A new geometry object</returns>
    protected override Geometry RefreshGeometry() {
      var geometry = new PathGeometry(Toolbar.ToolbarRenderTarget.Factory);
      GeometrySink sink = geometry.Open();

      sink.SetFillMode(FillMode.Winding);
      sink.BeginFigure(new RawVector2(Location.X, Location.Y + Size.Y / 2), FigureBegin.Filled);
      sink.AddLines(new[] {
        new RawVector2(Location.X + Size.X / 3, Location.Y),
        new RawVector2(Location.X + Size.X - Size.X / 3, Location.Y),
        new RawVector2(Location.X + Size.X, Location.Y + Size.Y / 2),
        new RawVector2(Location.X + Size.X - Size.X / 3, Location.Y + Size.Y),
        new RawVector2(Location.X + Size.X / 3, Location.Y + Size.Y)
      });

      sink.EndFigure(FigureEnd.Closed);
      sink.Close();
      return geometry;
    }

    public override void Render(RenderTarget target) {
      target.FillGeometry(Geometry.Value, this.backBrush);
      base.Render(target);

      if (this.image != null) {
        using (var ctx = target.QueryInterface<DeviceContext>()) {
          ctx.DrawImage(this.image,
                        new RawVector2(Location.X + Size.X / 2 - this.imageSize.Width / 2,
                                       Location.Y + Size.Y / 2 - this.imageSize.Height / 2));
        }
      }
    }

    /// <inheritdoc />
    /// <summary>
    ///   Releases resources used by the control
    /// </summary>
    public override void Dispose() {
      this.backBrush?.Dispose();
      this.backBrush = null;
      base.Dispose();
    }
  }
}