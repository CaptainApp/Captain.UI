using System;
using System.Drawing;
using Captain.Common.Native;

namespace Captain.UI {
  /// <summary>
  ///   Utility functions for styling and appearance
  /// </summary>
  public static class StyleHelper {
    /// <summary>
    ///   Tries to obtain the system accent color
    /// </summary>
    /// <returns>
    ///   A <see cref="Color" /> representing the system accent tint. If the color could not be obtained,
    ///   <c>null</c> is returned
    /// </returns>
    /// <remarks>
    ///   For a better appearance, this method only returns the accent color on Windows 8 and greater.
    ///   HACK: This is using the undocumented dwmapi entry point #137, which can vanish at any time.
    ///   TODO: Find a better way to accomplish this using documented APIs on Windows >= 8
    /// </remarks>
    public static Color? GetAccentColor() {
      // only on Windows >= 8
      if (Environment.OSVersion.Version < new Version(6, 2)) { return null; }

      // make sure DWM composition's enabled
      if (DwmApi.DwmIsCompositionEnabled(out bool composited) == 0 && composited) {
        try {
          var colorizationParams = new DwmApi.DWMCOLORIZATIONPARAMS();

          // retrieve colorization parameters
          DwmApi.DwmGetColorizationParameters(ref colorizationParams);

          // build color
          return Color.FromArgb((byte) (colorizationParams.ColorizationColor >> 16),
            (byte) (colorizationParams.ColorizationColor >> 8),
            (byte) colorizationParams.ColorizationColor);
        } catch (EntryPointNotFoundException) {
          // unsupported
        }
      }

      return null;
    }
  }
}