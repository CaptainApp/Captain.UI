using System.Drawing;
using Captain.Common;
using SharpDX.DXGI;

namespace Captain.UI {
  /// <summary>
  ///   Holds information about the HUD container
  /// </summary>
  public struct HudContainerInfo {
    /// <summary>
    ///   Container type
    /// </summary>
    public HudContainerType ContainerType;

    /// <summary>
    ///   Mouse hook behaviour for this container
    /// </summary>
    public Behaviour MouseHookBehaviour;

    /// <summary>
    ///   Keyboard hook behaviour for this container
    /// </summary>
    public Behaviour KeyboardHookBehaviour;

    /// <summary>
    ///   Tidbit manager for this container
    /// </summary>
    public TidbitManager TidbitManager;

    /// <summary>
    ///   Container bounds
    /// </summary>
    public Rectangle VirtualBounds;

    #region DirectX fields

    /// <summary>
    ///   DXGI swap chain
    /// </summary>
    public SwapChain SwapChain;

    /// <summary>
    ///   Pixel format
    /// </summary>
    public Format PixelFormat;

    #endregion
  }
}