using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using Bitmap = SharpDX.Direct2D1.Bitmap;
using Brush = SharpDX.Direct2D1.Brush;
using Color = SharpDX.Color;
using Rectangle = System.Drawing.Rectangle;

namespace Captain.UI {
  /// <inheritdoc />
  /// <summary>
  ///   Control interface for recording sessions
  /// </summary>
  public sealed class Toolbar : HudComponent<HudToolbarWrapperWindow> {
    /// <summary>
    ///   If true, a "modern" style is assumed (i.e. Windows >= 8)
    /// </summary>
    private readonly bool isModernStyle;

    /// <summary>
    ///   Background brush for non-modern style
    /// </summary>
    private Brush backgroundBrush;

    /// <summary>
    ///   Background rounded rectangle for non-modern style
    /// </summary>
    private RoundedRectangle backgroundRoundedRectangle;

    /// <summary>
    ///   Noise texture
    /// </summary>
    private Bitmap noiseTexture;

    /// <summary>
    ///   Recording intent to be sent by the primary button
    /// </summary>
    private ToolbarRecordingControlIntent nextRecordingIntent;

    /// <inheritdoc />
    /// <summary>
    ///   Size of the toolbar
    /// </summary>
    public override Size Size => new Size(256, 32);

    /// <summary>
    ///   Exposes the toolbar <see cref="RenderTarget" /> to child controls
    /// </summary>
    public RenderTarget ToolbarRenderTarget => RenderTarget;

    /// <summary>
    ///   Toolbar controls
    /// </summary>
    public OrderedDictionary Controls { get; } = new OrderedDictionary();

    /// <summary>
    ///   Locks or unlocks the toolbar
    /// </summary>
    public bool Locked {
      get => Math.Abs(((ToolbarImage) Controls["grip"]).Opacity) < 0.5f;
      set => ((ToolbarImage) Controls["grip"]).Opacity = value ? 0.125f : 0.5f;
    }

    /// <summary>
    ///   Triggered when an option button has been triggered
    /// </summary>
    public event EventHandler<ToolbarOptionRequestType> OnOptionsRequested;

    /// <summary>
    ///   Triggered when the recording control button has been triggered
    /// </summary>
    public event EventHandler<ToolbarRecordingControlIntent> OnRecordingIntentReceived;

    /// <inheritdoc />
    /// <summary>
    ///   Class constructor
    /// </summary>
    /// <param name="container">HUD container information</param>
    public Toolbar(HudContainerInfo container) : base(container) {
      this.isModernStyle = Environment.OSVersion.Version >= new Version(6, 2);

      if (DesktopWrapper != null) {
        DesktopWrapper.Size = Size;
        DesktopWrapper.Toolbar = this;
        DesktopWrapper.PassThrough = false;
        DesktopWrapper.Resizable = false;
        DesktopWrapper.EnableBlurBehind();

        // lock mouse hook - we're doing a heavy usage of tidbits for each button
        // this will prevent mouse hooks from being installed/uninstalled constantly when hovering/unhovering buttons
        container.MouseHookBehaviour.RequestLock();

        // bind mouse event handlers
        DesktopWrapper.MouseMove += OnToolbarMouseMove;
        DesktopWrapper.MouseDown += OnToolbarMouseDown;
        DesktopWrapper.MouseUp += OnToolbarMouseUp;
        DesktopWrapper.MouseLeave += delegate {
          foreach (ToolbarControl control in Controls.Values) {
            control.State &= ~ToolbarControlState.Hovered;
          }
        };
      }

      Bounds = new Rectangle(320, 320, Size.Width, Size.Height);
      InitializeRenderingObjects();
    }

    /// <summary>
    ///   Sets the action/state of the primary button
    /// </summary>
    /// <param name="recordingControlIntent">New state for the button</param>
    /// <param name="enabled">Whether or not the button is enabled</param>
    public void SetPrimaryButtonState(ToolbarRecordingControlIntent? recordingControlIntent = null,
                                      bool? enabled = null) {
      if (recordingControlIntent.HasValue) {
        switch (this.nextRecordingIntent = recordingControlIntent.Value) {
          case ToolbarRecordingControlIntent.Start:
            // start recording
            ((ToolbarPrimaryButton) Controls["primaryButton"]).Bitmap =
              Resources.SnackBarRecord.ToDirect2DBitmap(RenderTarget);
            break;

          case ToolbarRecordingControlIntent.Stop:
            // stop recording
            ((ToolbarPrimaryButton) Controls["primaryButton"]).Bitmap =
              Resources.SnackBarStop.ToDirect2DBitmap(RenderTarget);
            break;

          default:
            throw new NotImplementedException();
        }
      }

      if (enabled.HasValue) {
        ((ToolbarPrimaryButton) Controls["primaryButton"]).Enabled = enabled.Value;
      }
    }

    /// <inheritdoc />
    /// <summary>
    ///   Releases resources
    /// </summary>
    public override void Dispose() {
      // unlock previously locked mouse hook
      Container.MouseHookBehaviour.RequestUnlock();
      base.Dispose();
    }

    /// <summary>
    ///   Triggered when the mouse primary button is released
    /// </summary>
    /// <param name="sender">Sender object</param>
    /// <param name="eventArgs">Event arguments</param>
    private void OnToolbarMouseUp(object sender, MouseEventArgs eventArgs) {
      ToolbarControl[] controls = Controls.Values
                                          .Cast<ToolbarControl>()
                                          .Where(c => (c.State & ToolbarControlState.Active) != 0)
                                          .ToArray();

      if (controls.Length != 0) {
        controls[0].State &= ~ToolbarControlState.Active;
        if (controls[0].HitTest(new Vector2(eventArgs.X, eventArgs.Y))) {
          controls[0].Action?.Invoke();
        }
      }
    }

    /// <summary>
    ///   Triggered when the mouse primary button is held
    /// </summary>
    /// <param name="sender">Sender object</param>
    /// <param name="eventArgs">Event arguments</param>
    private void OnToolbarMouseDown(object sender, MouseEventArgs eventArgs) {
      ToolbarControl[] controls = Controls.Values.Cast<ToolbarControl>()
                                                     .Where(c => c.HitTest(new Vector2(eventArgs.X, eventArgs.Y)))
                                                     .OrderByDescending(c => c.Location.Z)
                                                     .ToArray();

      if (controls.Length != 0) {
        controls[0].State |= ToolbarControlState.Active;
        controls[0].Refresh();
      }
    }

    /// <summary>
    ///   Triggered when the mouse moves over the toolbar
    /// </summary>
    /// <param name="sender">Sender object</param>
    /// <param name="eventArgs">Event arguments</param>
    private void OnToolbarMouseMove(object sender, MouseEventArgs eventArgs) {
      var vec = new Vector2(eventArgs.X, eventArgs.Y);

      foreach (ToolbarControl control in Controls.Values) {
        control.State &= ~ToolbarControlState.Hovered;
      }

      foreach (ToolbarControl control in Controls.Values.Cast<ToolbarControl>().OrderByDescending(c => c.Location.Z)) {
        if (control.HitTest(vec) && (control.State & ToolbarControlState.Hovered) == 0) {
          control.State |= ToolbarControlState.Hovered;
          control.Refresh();
          break;
        }
      }

      Refresh();
    }

    /// <inheritdoc />
    /// <summary>
    ///   Creates disposable rendering resources
    /// </summary>
    protected override void InitializeRenderingObjects() {
      base.InitializeRenderingObjects();

      if (this.isModernStyle) {
        this.noiseTexture = Resources.AcrylicNoiseTexture.ToDirect2DBitmap(RenderTarget);
      }
      else {
        this.backgroundBrush = new SolidColorBrush(RenderTarget, new RawColor4(0, 0, 0, 0.75f));
        this.backgroundRoundedRectangle = new RoundedRectangle {
          RadiusX = 8,
          RadiusY = 8,
          Rect = new RawRectangleF(0, 0, Size.Width, Size.Height)
        };
      }

      // create controls
      Controls["timerText"] = new ToolbarTextControl(this) {
        Content = "00:00",
        Size = new Vector2(1.5f * Size.Height, Size.Height)
      };

      Controls["optionsButton"] = new ToolbarButton(this) {
        Bitmap = Resources.SnackBarOptions.ToDirect2DBitmap(RenderTarget),
        Size = new Vector2(Size.Height, Size.Height),
        Tidbit = "Options",
        Action = () => OnOptionsRequested?.Invoke(this, ToolbarOptionRequestType.Generic)
      };

      Controls["grip"] = new ToolbarImage(this) {
        Bitmap = Resources.SnackBarGrip.ToDirect2DBitmap(RenderTarget),
        Size = new Vector2(0.5f * Size.Height, Size.Height),
        Opacity = 0.5f
      };

      Controls["primaryButton"] = new ToolbarPrimaryButton(this) {
        Bitmap = Resources.SnackBarRecord.ToDirect2DBitmap(RenderTarget),
        Location = new Vector3((Size.Width - 64.0f) / 2, -16, 64),
        Size = new Vector2(64, 64),
        Gravity = ToolbarControlGravity.Zero,
        Tidbit = "Start recording",
        Action = () => OnRecordingIntentReceived?.Invoke(this, this.nextRecordingIntent)
      };

      Controls["microphoneButton"] = new ToolbarButton(this) {
        Bitmap = Resources.SnackBarUnmute.ToDirect2DBitmap(RenderTarget),
        Gravity = ToolbarControlGravity.Far,
        Size = new Vector2(Size.Height, Size.Height),
        Tidbit = "Audio options",
        Action = () => OnOptionsRequested?.Invoke(this, ToolbarOptionRequestType.Audio)
      };

      Controls["regionButton"] = new ToolbarButton(this) {
        Bitmap = Resources.ClipperPickRegion.ToDirect2DBitmap(RenderTarget),
        Gravity = ToolbarControlGravity.Far,
        Size = new Vector2(Size.Height, Size.Height),
        Tidbit = "Region options",
        Action = () => OnOptionsRequested?.Invoke(this, ToolbarOptionRequestType.Region)
      };

      Controls["closeButton"] = new ToolbarButton(this) {
        Bitmap = Resources.SnackBarClose.ToDirect2DBitmap(RenderTarget),
        Gravity = ToolbarControlGravity.Far,
        Size = new Vector2(Size.Height, Size.Height),
        HoveredBackgroundColor = new Color(232, 17, 35, 255),
        ActiveBackgroundColor = new Color(231, 16, 34, 153),
        Tidbit = "Close",
        Action = Dispose
      };
    }

    /// <inheritdoc />
    /// <summary>
    ///   Disposes all rendering resources
    /// </summary>
    protected override void DestroyRenderingObjects() {
      foreach (ToolbarControl control in Controls.Values) {
        control.Dispose();
      }

      Controls.Clear();

      this.backgroundBrush?.Dispose();
      this.noiseTexture?.Dispose();

      this.backgroundBrush = null;
      this.noiseTexture = null;

      base.DestroyRenderingObjects();
    }

    /// <summary>
    ///   Calculates the effective location for a specific control based on its gravity setting
    /// </summary>
    /// <param name="control">The toolbar control</param>
    /// <param name="idx">Control index</param>
    /// <returns>A 3D vector containing the control position</returns>
    /// <remarks>The Y coordinate and Z order are not changed by this method</remarks>
    private Vector3 CalculateEffectiveLocation(ToolbarControl control, int idx) {
      Vector3 location = control.Location;

      switch (control.Gravity) {
        case ToolbarControlGravity.Near:
          // pull controls from the left side of the toolbar
          location.X = Controls.Values.Cast<ToolbarControl>()
                               .Where((c, i) => c.Gravity == ToolbarControlGravity.Near && i > idx)
                               .Sum(c => c.Size.X);
          break;

        case ToolbarControlGravity.Far:
          // pull controls from the right side of the toolbar
          location.X = Size.Width -
                       Controls.Values.Cast<ToolbarControl>()
                               .Where((c, i) => c.Gravity == ToolbarControlGravity.Far && i > idx)
                               .Sum(c => c.Size.X);
          break;
      }

      return location;
    }

    /// <inheritdoc />
    /// <summary>
    ///   Performs UI rendering
    /// </summary>
    protected override void Render() {
      RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;

      if (this.isModernStyle) {
        RenderTarget.Clear(new RawColor4(0.125f, 0.125f, 0.125f, 0.75f));
        RenderTarget.DrawBitmap(this.noiseTexture,
                                new RawRectangleF(0, 0, Size.Width, Size.Height),
                                0.3333f,
                                BitmapInterpolationMode.NearestNeighbor);
      }
      else {
        RenderTarget.Clear(null);
        RenderTarget.FillRoundedRectangle(this.backgroundRoundedRectangle, this.backgroundBrush);
      }

      // render the controls following their Z orders
      IEnumerable<ToolbarControl> controls = Controls.Values.Cast<ToolbarControl>()
                                                     .OrderBy(c => c.Location.Z)
                                                     .Select((c, i) => {
                                                       // enforce control gravity
                                                       c.Location = CalculateEffectiveLocation(c, i);
                                                       c.Refresh();
                                                       return c;
                                                     });

      foreach (ToolbarControl control in controls) {
        control.Render(RenderTarget);
      }
    }

    /// <summary>
    ///   Refreshes toolbar graphics
    /// </summary>
    public void Refresh() => DesktopWrapper?.Invalidate();
  }
}