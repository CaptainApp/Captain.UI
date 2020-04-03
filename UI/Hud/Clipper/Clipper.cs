using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Captain.Common;
using Captain.Common.Native;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using static Captain.UI.Library;
using Bitmap = SharpDX.Direct2D1.Bitmap;
using Brush = SharpDX.Direct2D1.Brush;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   Provides the user with an interface to select a screen region
  /// </summary>
  public sealed class Clipper : HudComponent<HudCommonWrapperWindow> {
    /// <summary>
    ///   Minimum selection width
    /// </summary>
    public const int MinimumWidth = 38;

    /// <summary>
    ///   Minimum selection height
    /// </summary>
    public const int MinimumHeight = 38;

    /// <summary>
    ///   Whether or not the Alt key is held
    /// </summary>
    private bool altDown = (Control.ModifierKeys & Keys.Alt) != 0;

    /// <summary>
    ///   Bitmap containing the locked UI corner images
    /// </summary>
    private Bitmap cornerBitmap;

    /// <summary>
    ///   Current corner padding
    /// </summary>
    private Padding cornerPadding;

    /// <summary>
    ///   Initial coordinates selected by the user on Pick selection mode
    /// </summary>
    private Point? initialPosition;

    /// <summary>
    ///   Border brush for the inner selection rectangle on locked clipper
    /// </summary>
    private Brush innerBorderBrush;

    /// <summary>
    ///   Border brush for the outer selection rectangle
    /// </summary>
    private Brush outerBorderBrush;

    /// <summary>
    ///   Informational tidbit
    /// </summary>
    private Tidbit tidbit;

    /// <summary>
    ///   Mutex used for returning from UnlockAsync() immediately after Lock() is called
    /// </summary>
    private Mutex uiLockMutex;

    /// <summary>
    ///   Handle of the selected window
    /// </summary>
    private IntPtr windowHandle;

    /// <summary>
    ///   Attached toolbar instance
    /// </summary>
    private Toolbar toolbar;

    /// <summary>
    ///   If false, the user won't be able to select window regions
    /// </summary>
    private bool advSelectionCurrentlyAllowed = true;

    /// <summary>
    ///   Gets or sets the current selection mode for the clipper UI
    /// </summary>
    public ClippingMode Mode { get; private set; }

    /// <summary>
    ///   Whether or not the clipper UI is locked
    /// </summary>
    public bool Locked { get; private set; } = true;

    /// <summary>
    ///   Exposes the clipper UI bounds
    /// </summary>
    public Rectangle Area {
      get => Rectangle.Inflate(Bounds,
                               (-this.cornerPadding.Left - this.cornerPadding.Right) / 2,
                               (-this.cornerPadding.Top - this.cornerPadding.Bottom) / 2);
      set {
        Bounds = Rectangle.Inflate(value,
                                   (this.cornerPadding.Left + this.cornerPadding.Right) / 2,
                                   (this.cornerPadding.Top + this.cornerPadding.Bottom) / 2);
        UpdateAttachedToolbar();
      }
    }

    /// <inheritdoc />
    /// <summary>
    ///   Class constructor
    /// </summary>
    /// <param name="container">HUD container</param>
    public Clipper(HudContainerInfo container) : base(container) {
      InitializeRenderingObjects();

      if (container.ContainerType == HudContainerType.Desktop) {
        DesktopWrapper.Resize += delegate { UpdateAttachedToolbar(); };
        DesktopWrapper.Move += delegate { UpdateAttachedToolbar(); };
      }
    }

    /// <summary>
    ///   Updates the clipper UI layout based on the current selection mode
    /// </summary>
    private void RefreshLayout() {
      Rectangle oldArea = Area;

      switch (Mode) {
        case ClippingMode.Pick:
          this.cornerPadding = new Padding(1);

          this.outerBorderBrush?.Dispose();
          this.outerBorderBrush = new SolidColorBrush(RenderTarget, new RawColor4(0.5f, 0.5f, 0.5f, 0.75f));

          break;

        case ClippingMode.Rescale:
          this.cornerPadding = new Padding(5);

          this.outerBorderBrush?.Dispose();
          this.innerBorderBrush?.Dispose();

          this.outerBorderBrush = new SolidColorBrush(RenderTarget, new RawColor4(0, 0, 0, 0.25f));
          this.innerBorderBrush = new SolidColorBrush(RenderTarget, new RawColor4(0.5f, 0.5f, 0.5f, 1));

          if (this.cornerBitmap?.IsDisposed ?? true) {
            this.cornerBitmap = Resources.ClipperCorners.ToDirect2DBitmap(RenderTarget);
          }

          break;
      }

      Area = oldArea;
      DesktopWrapper?.Invalidate();
    }

    /// <inheritdoc />
    /// <summary>
    ///   Releases resources
    /// </summary>
    public override void Dispose() {
      // make sure the mouse hook is unlocked or, at least, that we unbind our event handlers
      Lock();
      base.Dispose();
    }

    /// <inheritdoc />
    /// <summary>
    ///   Renders the clipper UI
    /// </summary>
    protected override void Render() {
      RenderTarget.Clear(Locked ? (RawColor4?) null : new RawColor4(0.5f, 0.5f, 0.5f, 0.25f));

      if (Locked) {
        // render outer border
        RenderTarget.DrawRectangle(new RawRectangleF(1.5f, 1.5f, Bounds.Width - 1.5f, Bounds.Height - 1.5f),
                                   this.outerBorderBrush,
                                   3);

        // render corners
        RenderTarget.DrawBitmap(this.cornerBitmap,
                                new RawRectangleF(0, 0, this.cornerBitmap.Size.Width / 2,
                                                  this.cornerBitmap.Size.Height / 2),
                                1,
                                BitmapInterpolationMode.Linear,
                                new RawRectangleF(0, 0, this.cornerBitmap.Size.Width / 2,
                                                  this.cornerBitmap.Size.Height / 2));
        RenderTarget.DrawBitmap(this.cornerBitmap,
                                new RawRectangleF(Bounds.Width - this.cornerBitmap.Size.Width / 2,
                                                  0,
                                                  Bounds.Width,
                                                  this.cornerBitmap.Size.Width / 2),
                                1,
                                BitmapInterpolationMode.Linear,
                                new RawRectangleF(this.cornerBitmap.Size.Width / 2,
                                                  0,
                                                  this.cornerBitmap.Size.Width,
                                                  this.cornerBitmap.Size.Height / 2));
        RenderTarget.DrawBitmap(this.cornerBitmap,
                                new RawRectangleF(0,
                                                  Bounds.Height - this.cornerBitmap.Size.Height / 2,
                                                  this.cornerBitmap.Size.Width / 2,
                                                  Bounds.Height),
                                1,
                                BitmapInterpolationMode.Linear,
                                new RawRectangleF(0,
                                                  this.cornerBitmap.Size.Height / 2,
                                                  this.cornerBitmap.Size.Width / 2,
                                                  this.cornerBitmap.Size.Height));
        RenderTarget.DrawBitmap(this.cornerBitmap,
                                new RawRectangleF(Bounds.Width - this.cornerBitmap.Size.Width / 2,
                                                  Bounds.Height - this.cornerBitmap.Size.Height / 2,
                                                  Bounds.Width,
                                                  Bounds.Height),
                                1,
                                BitmapInterpolationMode.Linear,
                                new RawRectangleF(this.cornerBitmap.Size.Width / 2,
                                                  this.cornerBitmap.Size.Height / 2,
                                                  this.cornerBitmap.Size.Width,
                                                  this.cornerBitmap.Size.Height));

        // render inner border
        RenderTarget.DrawRectangle(new RawRectangleF(5, 5, Bounds.Width - 4.5f, Bounds.Height - 4.5f),
                                   this.innerBorderBrush);
      } else {
        RenderTarget.DrawRectangle(new RawRectangleF(
                                     (int) Math.Floor(this.cornerPadding.Left / 2f),
                                     (int) Math.Floor(this.cornerPadding.Top / 2f),
                                     (int) Math.Ceiling(Bounds.Width - this.cornerPadding.Right / 2f),
                                     (int) Math.Ceiling(Bounds.Height - this.cornerPadding.Bottom / 2f)),
                                   this.outerBorderBrush,
                                   this.cornerPadding.All);
      }
    }

    /// <summary>
    ///   Attaches a toolbar to the clipper UI so that it moves along the selected area
    /// </summary>
    /// <param name="attachedToolbar">Toolbar instance</param>
    public void AttachToolbar(Toolbar attachedToolbar) {
      this.toolbar = attachedToolbar;
      this.toolbar.Locked = true;
      UpdateAttachedToolbar();
    }

    /// <summary>
    ///   Updates the placement of the attached toolbar
    /// </summary>
    private void UpdateAttachedToolbar() {
      if (this.toolbar == null) {
        return;
      }

      // TODO: optimize this, perhaps?
      Rectangle bounds = this.toolbar.Bounds;
      bounds.X = Math.Max(Container.VirtualBounds.X,
                          Math.Min(Container.VirtualBounds.Width - bounds.Width,
                                   Bounds.X + (Bounds.Width - bounds.Width) / 2));
      bounds.Y = Math.Max(Container.VirtualBounds.Y,
                          Math.Min(Container.VirtualBounds.Height - bounds.Height,
                                   Bounds.Y + Bounds.Height + bounds.Height / 2));
      if (bounds.Y < Bounds.Y + Bounds.Height + bounds.Height / 2) {
        bounds.Y = Bounds.Y - (int) (bounds.Height * 1.5);
      }

      this.toolbar.Bounds = bounds;
    }

    /// <inheritdoc />
    /// <summary>
    ///   Creates disposable rendering resources
    /// </summary>
    protected override void InitializeRenderingObjects() {
      base.InitializeRenderingObjects();
      RefreshLayout();
    }

    /// <inheritdoc />
    /// <summary>
    ///   Disposes all rendering resources
    /// </summary>
    protected override void DestroyRenderingObjects() {
      this.cornerBitmap?.Dispose();
      this.cornerBitmap = null;

      this.innerBorderBrush?.Dispose();
      this.innerBorderBrush = null;

      this.outerBorderBrush?.Dispose();
      this.outerBorderBrush = null;
      
      this.tidbit?.Dispose();
      this.tidbit = null;

      base.DestroyRenderingObjects();
    }

    /// <summary>
    ///   Unlocks the clipper UI so that the user can select a new region or modify the existing one
    /// </summary>
    /// <param name="mode">New selection mode</param>
    /// <param name="allowAdvancedSelection">If false, the user won't be able to select window regions</param>
    public async Task UnlockAsync(ClippingMode mode = ClippingMode.Pick, bool allowAdvancedSelection = true) {
      if (!Locked && Mode == mode) {
        Log.Trace("clipper UI is not locked or there's no mode change - ignoring");
        return;
      }

      Log.Debug("unlocking clipper UI");
      this.advSelectionCurrentlyAllowed = allowAdvancedSelection;

      // create mutex
      this.uiLockMutex?.ReleaseMutex();
      this.uiLockMutex = new Mutex(true);

      // "hide" the clipper UI
      Bounds = new Rectangle(-0x7FFF, -0x7FFF, 0, 0);

      // set the new mode and update the UI layout to match
      Mode = mode;
      RefreshLayout();

      // lock mouse hook
      // on Rescale mode we should not need mouse hooks in order to retrieve events;
      // However, it is reasonable to do so taking into account that, depending on the current HUD container,
      // we may not have window events available, or even a window at all
      if (Container.MouseHookBehaviour is IMouseHookProvider mouseHookProvider) {
        Container.MouseHookBehaviour.RequestLock();

        // bind hook event handlers
        mouseHookProvider.OnMouseDown += OnHookedMouseDown;
        mouseHookProvider.OnMouseMove += OnHookedMouseMove;
        mouseHookProvider.OnMouseUp += OnHookedMouseUp;
      }

      // lock keyboard hook
      // we want to respond to the Alt key immediately, not just when the mouse is moved.
      // For this, we have no other choice
      if (Container.KeyboardHookBehaviour is IKeyboardHookProvider kbdHookProvider) {
        Container.KeyboardHookBehaviour.RequestLock();

        // bind hook event handlers
        kbdHookProvider.OnKeyDown += OnHookedKeyDown;
        kbdHookProvider.OnKeyUp += OnHookedKeyUp;
      }

      Locked = false;
      if (DesktopWrapper != null) {
        DesktopWrapper.PassThrough = true;
        DesktopWrapper.MinimumSize = default;
      }

      Log.Trace("waiting for UI lock mutex release");
      RefreshTidbit();
      await Task.Factory.StartNew(this.uiLockMutex.WaitOne);
    }

    /// <summary>
    ///   Refreshes the clipper UI tidbit
    /// </summary>
    private void RefreshTidbit() {
      if (Mode == ClippingMode.Pick) {
        if (this.tidbit?.Disposed ?? true) {
          this.tidbit = new Tidbit(Container, Timeout.InfiniteTimeSpan);
        }

        if (this.altDown && this.advSelectionCurrentlyAllowed) {
          // TODO: localize this!
          this.tidbit.Content = "Click the window to be captured";
          this.tidbit.CustomIcon = Resources.ClipperPickWindow;
          this.tidbit.CustomAccent = Color.FromArgb(0x28D69C);
          this.tidbit.Visible = true;
        } else if (Area.Width == 0 || Area.Height == 0) {
          this.tidbit.Content = "Select the region you want to capture";
          this.tidbit.CustomIcon = Resources.ClipperPickRegion;
          this.tidbit.CustomAccent = Color.White;
          this.tidbit.Visible = true;
        } else if (Area.Width < MinimumWidth || Area.Height < MinimumHeight) {
          this.tidbit.Status = TidbitStatus.Error;
          this.tidbit.Content = "This region is too small";
          this.tidbit.CustomIcon = null;
          this.tidbit.CustomAccent = Color.Red;
          this.tidbit.Visible = true;
        } else {
          this.tidbit.Visible = false;
        }
      }
    }

    /// <summary>
    ///   Triggered when a key is held
    /// </summary>
    /// <param name="sender">Sender object</param>
    /// <param name="eventArgs">Keyboard event arguments</param>
    private void OnHookedKeyDown(object sender, KeyEventArgs eventArgs) {
      if (eventArgs.KeyData == Keys.LMenu || eventArgs.KeyCode == Keys.RMenu) {
        if (!this.altDown) {
          this.altDown = true;
          OnHookedMouseMove(
            sender,
            new ExtendedEventArgs<MouseEventArgs, bool>(
              new MouseEventArgs(MouseButtons.None, 0, Control.MousePosition.X, Control.MousePosition.Y, 0)));
          RefreshTidbit();
        }
      } else {
        Log.Trace("dismissing capture");
        Dispose();
      }
    }

    /// <summary>
    ///   Triggered when a key is released
    /// </summary>
    /// <param name="sender">Sender object</param>
    /// <param name="eventArgs">Keyboard event arguments</param>
    private void OnHookedKeyUp(object sender, KeyEventArgs eventArgs) {
      if ((eventArgs.KeyCode == Keys.LMenu || eventArgs.KeyCode == Keys.RMenu) && this.altDown) {
        this.altDown = false;
        Bounds = new Rectangle(-0x7FFF, -0x7FFF, 0, 0);
        RefreshTidbit();
      }
    }

    /// <summary>
    ///   Locks the clipper UI so that the user may not modify its region
    /// </summary>
    private void Lock() {
      if (Locked) {
        Log.Trace("clipper UI already locked - ignoring");
        return;
      }

      Log.Debug("locking clipper UI");

      // lock the clipper UI and set the new mode
      Mode = ClippingMode.Rescale;
      Locked = true;
      RefreshLayout();
      this.tidbit?.Dispose();
      this.uiLockMutex?.ReleaseMutex();

      if (DesktopWrapper != null) {
        // we want to display custom cursors for desktop wrapper corners, so we want the window to process mouse events
        DesktopWrapper.PassThrough = false;
        DesktopWrapper.MinimumSize =
          new Size(MinimumWidth + this.cornerPadding.Horizontal, MinimumHeight + this.cornerPadding.Vertical);
      }

      // unlock mouse hook
      if (Container.MouseHookBehaviour is IMouseHookProvider hookProvider) {
        Container.MouseHookBehaviour.RequestUnlock();

        // unbind hook event handlers
        hookProvider.OnMouseDown -= OnHookedMouseDown;
        hookProvider.OnMouseMove -= OnHookedMouseMove;
        hookProvider.OnMouseUp -= OnHookedMouseUp;
      }

      // unlock keyboard hook
      if (Container.KeyboardHookBehaviour is IKeyboardHookProvider kbdHookProvider) {
        Container.KeyboardHookBehaviour.RequestUnlock();

        // bind hook event handlers
        kbdHookProvider.OnKeyDown -= OnHookedKeyDown;
        kbdHookProvider.OnKeyUp -= OnHookedKeyUp;
      }
    }

    /// <summary>
    ///   Handles hooked mouse button down events
    /// </summary>
    /// <param name="sender">Sender object</param>
    /// <param name="extendedEventArgs">
    ///   Extended event arguments, being the extended data a boolean value that, when <c>false</c>, forces the event
    ///   to be passed to the next handler
    /// </param>
    private void OnHookedMouseDown(object sender, ExtendedEventArgs<MouseEventArgs, bool> extendedEventArgs) {
      Log.Debug($"mouse button {extendedEventArgs.EventArgs.Button} down at: {extendedEventArgs.EventArgs.Location}");

      if (this.windowHandle != IntPtr.Zero) {
        Log.Trace("window region selected - ignoring");
        return;
      }

      if (extendedEventArgs.EventArgs.Button == MouseButtons.Left) {
        this.initialPosition = extendedEventArgs.EventArgs.Location;
        Area = new Rectangle(extendedEventArgs.EventArgs.Location, Size.Empty);
        extendedEventArgs.ExtendedData = true; // capture event
      } else {
        Lock();
      }
    }

    /// <summary>
    ///   Handles hooked mouse movements
    /// </summary>
    /// <param name="sender">Sender object</param>
    /// <param name="extendedEventArgs">
    ///   Extended event arguments, being the extended data a boolean value that, when <c>false</c>, forces the event
    ///   to be passed to the next handler
    /// </param>
    private void OnHookedMouseMove(object sender, ExtendedEventArgs<MouseEventArgs, bool> extendedEventArgs) {
      if (this.initialPosition.HasValue) {
        Rectangle rect = default;

        if (this.initialPosition.Value.X < extendedEventArgs.EventArgs.X) {
          rect.X = this.initialPosition.Value.X;
          rect.Width = extendedEventArgs.EventArgs.X - this.initialPosition.Value.X;
        } else {
          rect.X = extendedEventArgs.EventArgs.X;
          rect.Width = this.initialPosition.Value.X - extendedEventArgs.EventArgs.X;
        }

        if (this.initialPosition.Value.Y < extendedEventArgs.EventArgs.Y) {
          rect.Y = this.initialPosition.Value.Y;
          rect.Height = extendedEventArgs.EventArgs.Y - this.initialPosition.Value.Y;
        } else {
          rect.Y = extendedEventArgs.EventArgs.Y;
          rect.Height = this.initialPosition.Value.Y - extendedEventArgs.EventArgs.Y;
        }

        Area = rect;
        RefreshTidbit();

        return;
      }

      if (Container.ContainerType == HudContainerType.Desktop && this.altDown && this.advSelectionCurrentlyAllowed) {
        // if Alt key is down, select active window region
        this.windowHandle = User32.WindowFromPoint(new POINT {
          x = extendedEventArgs.EventArgs.X,
          y = extendedEventArgs.EventArgs.Y
        });

        if (this.windowHandle != IntPtr.Zero) {
          // there's an actual window here
          Rectangle area = WindowHelper.GetWindowBounds(this.windowHandle).ToRectangle();

          if (area != default) {
            // make sure this area is okay
            Area = area;
          } else {
            this.windowHandle = IntPtr.Zero;
          }
        }

        return;
      }

      Bounds = new Rectangle(-0x7FFF, -0x7FFF, 0, 0);
    }

    /// <summary>
    ///   Handles hooked mouse button up events
    /// </summary>
    /// <param name="sender">Sender object</param>
    /// <param name="extendedEventArgs">
    ///   Extended event arguments, being the extended data a boolean value that, when <c>false</c>, forces the event
    ///   to be passed to the next handler
    /// </param>
    private void OnHookedMouseUp(object sender, ExtendedEventArgs<MouseEventArgs, bool> extendedEventArgs) {
      Log.Debug("mouse up at: " + extendedEventArgs.EventArgs.Location);

      if (Area.Width < MinimumWidth || Area.Height < MinimumHeight) {
        Log.Warn("area too small - disposing");
        Dispose();
      } else {
        this.initialPosition = null;
        extendedEventArgs.ExtendedData = true; // capture event

        Lock();
      }
    }
  }
}