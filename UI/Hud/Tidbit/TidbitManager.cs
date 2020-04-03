using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Captain.Common;

namespace Captain.UI {
  /// <summary>
  ///   Handles and coordinates all tidbits for a HUD container
  /// </summary>
  public class TidbitManager {
    /// <summary>
    ///   HUD container information
    /// </summary>
    private readonly HudContainerInfo containerInfo;

    /// <summary>
    ///   Tidbit registry
    /// </summary>
    private readonly List<Tidbit> tidbits = new List<Tidbit>();

    /// <summary>
    ///   Class constructor
    /// </summary>
    /// <param name="container">HUD container</param>
    public TidbitManager(HudContainerInfo container) =>
      this.containerInfo = container;

    /// <summary>
    ///   Keeps track of a tidbit object
    /// </summary>
    /// <param name="tidbit">Tidbit object instance</param>
    public void RegisterTidbit(Tidbit tidbit) {
      if (this.tidbits.Count == 0) { BindMouseEvents(); }
      tidbit.Location = CalculateTidbitLocation(verticalOffset: this.tidbits.Sum(t => 4 + t.Size.Height));
      this.tidbits.Add(tidbit);
    }

    /// <summary>
    ///   Removes a tidbit object from the registry
    /// </summary>
    /// <param name="tidbit">Tidbit object</param>
    public void UnregisterTidbit(Tidbit tidbit) {
      int index = this.tidbits.IndexOf(tidbit);
      if (index == -1) { return; }

      // pull back all tidbits after this
      this.tidbits.Where((t, i) => i > index)
        .ToList()
        .ForEach(t => t.Location = new Point(t.Location.X, t.Location.Y - tidbit.Size.Height));
      this.tidbits.RemoveAt(index);

      if (this.tidbits.Count == 0) { UnbindMouseEvents(); }
    }

    /// <summary>
    ///   Locks mouse hook and binds mouse events
    /// </summary>
    private void BindMouseEvents() {
      this.containerInfo.MouseHookBehaviour.RequestLock();
      if (this.containerInfo.MouseHookBehaviour is IMouseHookProvider hookProvider) {
        hookProvider.OnMouseMove += OnHookedMouseMove;
      }
    }

    /// <summary>
    ///   Unbinds mouse events and unlocks mouse hook
    /// </summary>
    private void UnbindMouseEvents() {
      if (this.containerInfo.MouseHookBehaviour is IMouseHookProvider hookProvider) {
        hookProvider.OnMouseMove += OnHookedMouseMove;
      }

      this.containerInfo.MouseHookBehaviour.RequestUnlock();
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
      int verticalOffset = 0;

      this.tidbits.ForEach(t => {
        t.Location = CalculateTidbitLocation(extendedEventArgs.EventArgs.Location, verticalOffset);
        verticalOffset += t.Size.Height + 4;
      });
    }

    /// <summary>
    ///   Calculates the location of the tidbit from the specified location, or the current mouse position, if none is
    ///   provided
    /// </summary>
    /// <param name="location">Optional location</param>
    /// <param name="verticalOffset">Offset for the Y coordinate</param>
    /// <returns>A <see cref="Point" /> structure containing the coordinates</returns>
    private static Point CalculateTidbitLocation(Point? location = null, int verticalOffset = 0) =>
      (location ?? Control.MousePosition) +
      new Size(Cursor.Current?.Size.Width / 2 ?? 0, verticalOffset + Cursor.Current?.Size.Height / 2 ?? 0);
  }
}