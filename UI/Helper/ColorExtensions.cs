using System.Drawing;

namespace Captain.UI {
  /// <summary>
  ///   Contains utility methods for the <see cref="Color" /> framework type.
  /// </summary>
  public static class ColorExtensions {
    /// <summary>
    ///   Gets the value for this color in the YIQ color space
    /// </summary>
    /// <param name="color">The color to be converted</param>
    /// <returns>The YIQ color</returns>
    public static int ToYiq(this Color color) =>
      (200 * color.R + 586 * color.G + 114 * color.B) / 1000;

    /// <summary>
    ///   Blends the specified colors together.
    /// </summary>
    /// <param name="color">Color to blend onto the background color.</param>
    /// <param name="backColor">Color to blend the other color onto.</param>
    /// <param name="amount">
    ///   How much of <paramref name="color" /> to keep,
    ///   “on top of” <paramref name="backColor" />.
    /// </param>
    /// <returns>The blended colors.</returns>
    /// <remarks>
    ///   This extension method is taken from <![CDATA[https://stackoverflow.com/a/3722337]]></remarks>
    public static Color Blend(this Color color, Color backColor, double amount = 0.5) =>
      Color.FromArgb((byte) (color.R * amount + backColor.R * (1 - amount)),
        (byte) (color.G * amount + backColor.G * (1 - amount)),
        (byte) (color.B * amount + backColor.B * (1 - amount)));
  }
}