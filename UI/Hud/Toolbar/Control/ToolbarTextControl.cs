using System.Drawing;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using Brush = SharpDX.Direct2D1.Brush;
using Factory = SharpDX.DirectWrite.Factory;
using FontStyle = SharpDX.DirectWrite.FontStyle;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   Provides simple text rendering as a toolbar control
  /// </summary>
  public class ToolbarTextControl : ToolbarControl {
    /// <summary>
    ///   Text brush
    /// </summary>
    private Brush brush;

    /// <summary>
    ///   Textual content
    /// </summary>
    private string content;

    /// <summary>
    ///   DirectWrite factory
    /// </summary>
    private Factory directWriteFactory;

    /// <summary>
    ///   Text format
    /// </summary>
    private TextFormat textFormat;

    /// <summary>
    ///   Text layout
    /// </summary>
    private TextLayout textLayout;

    /// <summary>
    ///   Gets or sets the textual content for the control
    /// </summary>
    public string Content {
      get => this.content;
      set {
        this.content = value;
        RefreshTextLayout();
      }
    }

    /// <inheritdoc />
    /// <summary>
    ///   Class constructor
    /// </summary>
    /// <param name="toolbar">Parent toolbar</param>
    public ToolbarTextControl(Toolbar toolbar) : base(toolbar) {}

    /// <inheritdoc />
    /// <summary>
    ///   Performs hit testing against a point
    /// </summary>
    /// <param name="point">Point to be tested against</param>
    /// <returns>Whether the point belongs to the control region or not</returns>
    public override bool HitTest(Vector2 point) => false;

    /// <summary>
    ///   Updates the text layout for the control
    /// </summary>
    private void RefreshTextLayout() {
      if (this.directWriteFactory?.IsDisposed ?? true) { this.directWriteFactory = new Factory(); }
      if (this.textFormat?.IsDisposed ?? true) {
        this.textFormat = new TextFormat(this.directWriteFactory,
          SystemFonts.MessageBoxFont.Name,
          FontWeight.Normal,
          FontStyle.Normal,
          FontStretch.Normal,
          12.0f) {
          ParagraphAlignment = ParagraphAlignment.Center,
          TextAlignment = TextAlignment.Center
        };
      }

      this.textLayout?.Dispose();
      this.textLayout = new TextLayout(this.directWriteFactory, this.content, this.textFormat, Size.X, Size.Y);

      if (this.brush?.IsDisposed ?? true) {
        this.brush = new SolidColorBrush(Toolbar.ToolbarRenderTarget, new RawColor4(1, 1, 1, 0.75f));
      }
    }

    /// <inheritdoc />
    /// <summary>
    ///   Releases resources used by the control
    /// </summary>
    public override void Dispose() {
      this.brush?.Dispose();
      this.directWriteFactory?.Dispose();
      this.textFormat?.Dispose();
      this.textLayout?.Dispose();

      this.brush = null;
      this.directWriteFactory = null;
      this.textFormat = null;
      this.content = null;

      base.Dispose();
    }

    /// <inheritdoc />
    /// <summary>
    ///   Refreshes the control
    /// </summary>
    public override void Refresh() {
      RefreshTextLayout();
      base.Refresh();
    }
    
    /// <inheritdoc />
    /// <summary>
    ///   Renders the control
    /// </summary>
    /// <param name="target">Render target</param>
    public override void Render(RenderTarget target) =>
      target.DrawTextLayout(new RawVector2(Location.X, Location.Y), this.textLayout, this.brush);
  }
}