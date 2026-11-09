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

using emmaus_v2;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Listbox
{
    /// <summary>
    /// Mostra una foto ad alta risoluzione in una finestra non ridimensionabile,
    /// proporzionata all'area di lavoro dello schermo (l'immagine mantiene
    /// le proporzioni originali).
    /// </summary>
    public sealed class FormFoto : Form
    {
        // Percentuale massima dell'area di lavoro occupabile dalla finestra.
        private const double FrazioneSchermo = 0.92;

        private readonly PictureBox pictureBox;
        private readonly Image immagine;
        private Rectangle areaLavoro;

        // Centra la finestra nell'area di lavoro dello schermo, a
        // dimensioni definitive (dopo l'eventuale scalatura DPI).
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Location = new Point(
                areaLavoro.Left + (areaLavoro.Width - Width) / 2,
                areaLavoro.Top + (areaLavoro.Height - Height) / 2);
        }

        // larghezzaRiservata: pixel dell'area di lavoro da lasciare liberi
        // (ad es. per una finestra affiancata).
        public FormFoto(string percorsoImmagine, Screen schermo = null, int larghezzaRiservata = 0)
        {
            // Copia in memoria: il file non resta bloccato.
            using (var flusso = new MemoryStream(File.ReadAllBytes(percorsoImmagine)))
            using (var originale = Image.FromStream(flusso))
            {
                immagine = new Bitmap(originale);
            }

            Text = "Emmaus 2.0";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;

            var area = (schermo ?? Screen.FromPoint(Cursor.Position)).WorkingArea;
            areaLavoro = area;

            // Spazio disponibile per l'area client (esclusi bordi e barra del titolo).
            Size extra = Size.Subtract(
                Size,
                ClientSize);

            int maxLarghezza = Math.Max(
                100,
                (int)(area.Width * FrazioneSchermo) - extra.Width - larghezzaRiservata);
            int maxAltezza = (int)(area.Height * FrazioneSchermo) - extra.Height;

            // Scala per stare nello schermo (mai oltre la grandezza naturale).
            double scala = Math.Min(
                1.0,
                Math.Min(
                    (double)maxLarghezza / immagine.Width,
                    (double)maxAltezza / immagine.Height));

            ClientSize = new Size(
                Math.Max(1, (int)Math.Round(immagine.Width * scala)),
                Math.Max(1, (int)Math.Round(immagine.Height * scala)));

            pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = immagine
            };

            Controls.Add(pictureBox);

            // Chiusura con clic sull'immagine o con Esc.
            pictureBox.Click += (s, e) => Close();
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    Close();
            };

            try
            {
                TemaBibbia.Applica(this);
            }
            catch
            {
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                pictureBox?.Dispose();
                immagine?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
