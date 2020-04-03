using System;
using SharpDX.Direct2D1;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   Common HUD renderer logic
  /// </summary>
  internal abstract class HudRenderer : IDisposable {
    /// <summary>
    ///   HUD container associated to this renderer
    /// </summary>
    internal HudContainerInfo Container { get; }

    /// <summary>
    ///   Gets or sets the Direct2D render target.
    ///   This property may be changed on the renderer constructor, and will be replaced by the original RenderTarget of
    ///   the HUD component
    /// </summary>
    public RenderTarget RenderTarget { get; protected set; }

    /// <summary>
    ///   Class constructor
    /// </summary>
    /// <param name="container">HUD container</param>
    internal HudRenderer(HudContainerInfo container) {
      Container = container;
    }

    /// <summary>
    ///   Renders the HUD
    /// </summary>
    internal abstract void Render();

    /// <inheritdoc />
    /// <summary>
    ///   Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
    /// </summary>
    public virtual void Dispose() { }
  }
}