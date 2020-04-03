using System;
using System.Drawing;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Factory = SharpDX.Direct2D1.Factory;
using Rectangle = System.Drawing.Rectangle;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   Base class for all HUD components
  /// </summary>
  /// <typeparam name="T">Type for the desktop wrapper of this component</typeparam>
  public abstract class HudComponent<T> : IDisposable where T : Control, new() {
    /// <summary>
    ///   Component bounds
    /// </summary>
    private Rectangle bounds = new Rectangle(-0x7FFF, -0x7FFF, 0, 0);

    /// <summary>
    ///   Whether or not this component is visible
    /// </summary>
    private bool visible = true;

    /// <summary>
    ///   Direct2D factory
    /// </summary>
    private Factory factory;

    /// <summary>
    ///   Custom HUD renderer
    /// </summary>
    private HudRenderer renderer;

    /// <summary>
    ///   Determines whether or not this component will be rendered
    /// </summary>
    public virtual bool Visible {
      get => DesktopWrapper?.Visible ?? this.visible;
      set {
        if (DesktopWrapper != null) {
          DesktopWrapper.Visible = value;
        }

        this.visible = value;
      }
    }

    /// <summary>
    ///   Wrapper window for displaying this component on the desktop
    /// </summary>
    protected T DesktopWrapper { get; private set; }

    /// <summary>
    ///   Rendering target for this component
    /// </summary>
    protected RenderTarget RenderTarget { get; private set; }

    /// <summary>
    ///   Holds information about the underlying HUD container
    /// </summary>
    public HudContainerInfo Container { get; }

    /// <summary>
    ///   Determines whether this HUD component has been yet disposed
    /// </summary>
    public bool Disposed { get; private set; }

    /// <summary>
    ///   Represents the location and size of the component on the virtual desktop
    /// </summary>
    public Rectangle Bounds {
      get => DesktopWrapper?.Bounds ?? this.bounds;
      set {
        if (DesktopWrapper != null) {
          DesktopWrapper.Bounds = value;
        }

        this.bounds = value;
      }
    }

    /// <summary>
    ///   Gets or sets the size of the component
    /// </summary>
    public virtual Size Size => Bounds.Size;

    /// <summary>
    ///   Triggered before the component is disposed
    /// </summary>
    public event EventHandler Disposing;

    /// <summary>
    ///   Base constructor for the HUD component
    /// </summary>
    /// <param name="container">Container information</param>
    protected HudComponent(HudContainerInfo container) {
      Container = container;
      this.factory = new Factory(FactoryType.MultiThreaded, DebugLevel.Information);

      switch (container.ContainerType) {
        case HudContainerType.Desktop:
          // create desktop wrapper
          DesktopWrapper = new T {
            // ReSharper disable once VirtualMemberCallInConstructor
            // no danger with this **as long as getter is not affected**
            Visible = Visible,
            Bounds = Bounds
          };

          // create render target
          RenderTarget = new WindowRenderTarget(
            this.factory,
            new RenderTargetProperties(new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied)),
            new HwndRenderTargetProperties {
              PixelSize = new Size2(Bounds.Width, Bounds.Height),
              Hwnd = DesktopWrapper.Handle,
              PresentOptions = PresentOptions.Immediately
            });

          DesktopWrapper.Resize += delegate {
            ((WindowRenderTarget) RenderTarget).Resize(new Size2(DesktopWrapper.Width, DesktopWrapper.Height));
            DesktopWrapper.Invalidate();
          };

          if (DesktopWrapper is HudCommonWrapperWindow commonWrapper) {
            commonWrapper.RenderLoop = RenderLoop;
          } else {
            throw new InvalidOperationException("Wrapper window must inherit from HudCommonWrapperWindow");
          }

          break;

        case HudContainerType.Direct3D11:
          throw new NotImplementedException();
      }

      if (this.renderer != null) {
        RenderTarget = this.renderer.RenderTarget;
      }
    }

    /// <summary>
    ///   Rendering loop procedure
    /// </summary>
    private void RenderLoop() {
      RenderTarget.BeginDraw();
      Render();
      RenderTarget.TryEndDraw(out _, out _);
      this.renderer?.Render();
    }

    /// <inheritdoc />
    /// <summary>
    ///   Releases resources
    /// </summary>
    public virtual void Dispose() {
      Disposing?.Invoke(this, EventArgs.Empty);

      Visible = false;

      DesktopWrapper?.Dispose();
      RenderTarget?.Dispose();

      this.factory?.Dispose();
      this.renderer?.Dispose();

      DestroyRenderingObjects();

      DesktopWrapper = null;
      RenderTarget = null;

      this.factory = null;
      this.renderer = null;

      Disposed = true;
    }

    /// <summary>
    ///   Performs UI rendering
    /// </summary>
    protected abstract void Render();

    /// <summary>
    ///   Creates disposable rendering resources
    /// </summary>
    protected virtual void InitializeRenderingObjects() { }

    /// <summary>
    ///   Disposes all rendering resources
    /// </summary>
    protected virtual void DestroyRenderingObjects() { }
  }
}