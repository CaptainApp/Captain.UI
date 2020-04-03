using System.Drawing.Imaging;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Bitmap = SharpDX.Direct2D1.Bitmap;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Rectangle = System.Drawing.Rectangle;

namespace Captain.UI {
  /// <summary>
  ///   Contains extension methods for <see cref="System.Drawing.Bitmap" /> objects
  /// </summary>
  public static class BitmapExtensions {
    /// <summary>
    ///   Creates a Direct2D bitmap from a GDI bitmap for the specified rendering target
    /// </summary>
    /// <param name="bitmap">Original bitmap</param>
    /// <param name="renderTarget">Destination rendering target</param>
    /// <param name="disposeOriginal">Whether or not to release the resources of the GDI bitmap</param>
    /// <returns>A Direct2D bitmap instance the caller is responsible for disposal</returns>
    public static Bitmap ToDirect2DBitmap(
      this System.Drawing.Bitmap bitmap,
      RenderTarget renderTarget,
      bool disposeOriginal = true) {
      // lock bits from the original bitmap so we can read its data
      BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
        ImageLockMode.ReadOnly,
        PixelFormat.Format32bppPArgb);

      // copy GDI bitmap data to Direct2D one
      var stream = new DataStream(data.Scan0, data.Stride * data.Height, true, false);
      var format = new SharpDX.Direct2D1.PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied);
      var props = new BitmapProperties(format);

      // create Direct2D bitmap and release resources
      var direct2DBitmap = new Bitmap(renderTarget,
        new Size2(bitmap.Width, bitmap.Height),
        stream,
        data.Stride,
        props);

      // release resources
      stream.Dispose();
      bitmap.UnlockBits(data);
      if (disposeOriginal) { bitmap.Dispose(); }

      // return new bitmap
      return direct2DBitmap;
    }
  }
}