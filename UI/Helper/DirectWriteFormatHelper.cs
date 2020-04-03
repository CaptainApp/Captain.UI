using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SharpDX.DirectWrite;

namespace Captain.UI {
  public static class DirectWriteFormatHelper {
    /// <summary>
    ///   Regular expression containing formatting rules
    /// </summary>
    private static readonly Regex FormatRegex =
      new Regex(@"(__|\*\*)(.*?)\k<1>", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    ///   Parses the specified text and returns a tuple containing the string without formatting symbols and an
    ///   <see cref="Action{TextLayout}" /> that can be invoked in order to apply formatting rules to the specified
    ///   <see cref="TextLayout" />
    /// </summary>
    /// <param name="text">The text to be formatted</param>
    /// <returns>A <see cref="ValueTuple{String, Action}" /> with the plaintext and the formatter callback</returns>
    public static (string Plaintext, Action<TextLayout> FormatterCallback) CreateFormatter(string text) {
      var boldRanges = new List<TextRange>();
      var italicRanges = new List<TextRange>();

      Match match = FormatRegex.Match(text);
      while (match.Success) {
        text = text.Substring(0, match.Index) +
               text.Substring(match.Groups[2].Index, match.Groups[2].Length) +
               text.Substring(match.Index + match.Length);

        switch (match.Groups[1].Value) {
          case "__":
            italicRanges.Add(new TextRange(match.Index, match.Groups[2].Length));
            break;
          case "**":
            boldRanges.Add(new TextRange(match.Index, match.Groups[2].Length));
            break;
        }

        match = FormatRegex.Match(text);
      }

      return (text, delegate(TextLayout layout) {
        foreach (TextRange range in boldRanges) { layout.SetFontWeight(FontWeight.Bold, range); }
        foreach (TextRange range in italicRanges) { layout.SetFontStyle(FontStyle.Italic, range); }
      });
    }
  }
}