// MIT License
//
// Copyright (c) 2026 DisabiliCristiani <disabilicristiani@gmail.com> - Fate del bene e pregate
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using PdfSharp.Fonts;
using System;
using System.IO;

namespace Input
{
    /// <summary>
    /// Risolutore dei font per i PDF, unico per tutto il programma
    /// (PdfSharp ne accetta uno solo). Ordine di preferenza, valido per
    /// qualsiasi famiglia richiesta: "ET Book" (licenza MIT), poi "Cardo"
    /// (licenza OFL), entrambi in Risorse\Font; solo se mancano entrambi
    /// si ricade su Segoe UI di Windows.
    /// </summary>
    internal sealed class RisolutoreFontPdf : IFontResolver
    {
        public const string FamigliaMit = "ET Book";
        public const string FamigliaCardo = "Cardo";

        private const string PrefissoMit = "ETBook-";
        private const string PrefissoCardo = "Cardo-";

        public static readonly RisolutoreFontPdf Istanza =
            new RisolutoreFontPdf();

        private RisolutoreFontPdf()
        {
        }

        public static void Registra()
        {
            if (ReferenceEquals(GlobalFontSettings.FontResolver, Istanza))
                return;

            GlobalFontSettings.FontResolver = Istanza;
        }

        private static string CartellaFontIncorporati =>
            Path.Combine(AppContext.BaseDirectory, "Risorse", "Font");

        public string DefaultFontName => FamigliaMit;

        public byte[] GetFont(string faceName)
        {
            bool incorporato =
                faceName.StartsWith(PrefissoMit, StringComparison.Ordinal) ||
                faceName.StartsWith(PrefissoCardo, StringComparison.Ordinal);

            string cartella = incorporato
                ? CartellaFontIncorporati
                : Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

            string percorso = Path.Combine(cartella, faceName);

            return File.Exists(percorso)
                ? File.ReadAllBytes(percorso)
                : null;
        }

        public FontResolverInfo ResolveTypeface(
            string familyName,
            bool isBold,
            bool isItalic)
        {
            // 1) ET Book
            string fileMit =
                isBold ? "ETBook-Bold.ttf"
                : isItalic ? "ETBook-Italic.ttf"
                : "ETBook-Regular.ttf";

            if (File.Exists(Path.Combine(CartellaFontIncorporati, fileMit)))
                return new FontResolverInfo(fileMit);

            // 2) Cardo
            string fileCardo =
                isBold ? "Cardo-Bold.ttf"
                : isItalic ? "Cardo-Italic.ttf"
                : "Cardo-Regular.ttf";

            if (File.Exists(Path.Combine(CartellaFontIncorporati, fileCardo)))
                return new FontResolverInfo(fileCardo);

            // 3) Ultima risorsa: Segoe UI di Windows
            string file;

            if (isBold && isItalic)
                file = "segoeuiz.ttf";
            else if (isBold)
                file = "segoeuib.ttf";
            else if (isItalic)
                file = "segoeuii.ttf";
            else
                file = "segoeui.ttf";

            string cartellaFont =
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

            if (!File.Exists(Path.Combine(cartellaFont, file)))
                file = "segoeui.ttf";

            return new FontResolverInfo(file);
        }
    }
}
