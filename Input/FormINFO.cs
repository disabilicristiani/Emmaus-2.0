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
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Input
{
    public partial class FormINFO : Form
    {
        private const string Email = "disabilicristiani@gmail.com";
        private const string OggettoEmail = "Fate del bene e pregate";
        private const string RiferimentoNome = "Luca 24,13-32";

        // True se l'utente ha scelto di riavviare il programma dal link
        // del nome: in tal caso MANUTENZIONE non deve riaprirsi e il
        // programma si chiude (il nuovo processo e' gia' stato avviato).
        public bool RiavvioRichiesto { get; private set; }

        public FormINFO()
        {
            InitializeComponent();

            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;

            TemaBibbia.Applica(this);

            // Dimensioni proporzionali allo scaling, entro lo schermo.
            Rectangle area = Screen.FromControl(this).WorkingArea;
            int larghezza = Math.Min(Font.Height * 38, (int)(area.Width * 0.9));
            int altezza = Math.Min(Font.Height * 46, (int)(area.Height * 0.9));
            ClientSize = new Size(larghezza, altezza);

            int utile =
                ClientSize.Width - SystemInformation.VerticalScrollBarWidth -
                tabellaContenuto.Padding.Horizontal - 12;

            ApplicaStili(utile);
            CaricaLogo();

            linkEmail.Click += (s, e) => ApriEmail();
            linkNome.LinkClicked += (s, e) => RiavviaAlRiferimento();

            TemaBibbia.Suggerimento(linkEmail, "Scrivi una email");
            TemaBibbia.Suggerimento(linkNome, "Riavvia il programma e apri " + RiferimentoNome);

            AggiornaEvidenziatore();
            TemaBibbia.TemaCambiato += TemaCambiato;

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    Close();
                else if (e.KeyCode == Keys.F1)
                {
                    FormGUIDA.Mostra(this, "c10");
                    e.Handled = true;
                }
            };

            // All'apertura il focus va al primo controllo selezionabile
            // (il link in fondo) e il pannello scorrerebbe fin li': si
            // riporta la lettura all'inizio dopo che il focus si e' posato.
            Shown += (s, e) => BeginInvoke(new Action(() =>
            {
                if (!IsDisposed)
                    panelScorrimento.AutoScrollPosition = Point.Empty;
            }));

            FormClosed += (s, e) =>
            {
                TemaBibbia.TemaCambiato -= TemaCambiato;
                pictureBoxLogo.Image?.Dispose();
                pictureBoxLogo.Image = null;
            };
        }

        private void TemaCambiato(object sender, EventArgs e)
        {
            if (IsDisposed)
                return;

            AggiornaEvidenziatore();
        }

        private Font Derivato(float fattore, FontStyle stile)
        {
            return new Font(Font.FontFamily, Font.Size * fattore, stile);
        }

        private void ApplicaStili(int larghezzaUtile)
        {
            labelTitolo.Font = Derivato(2.4f, FontStyle.Bold);
            labelTitolo.ForeColor = TemaBibbia.Oro;

            labelSottotitolo.Font = Derivato(1.15f, FontStyle.Regular);

            // Licenza e copyright in corsivo.
            foreach (Label l in new[] { labelCopyright, labelTrattino, labelSlogan, labelLicenza })
                l.Font = Derivato(1f, FontStyle.Italic);

            linkEmail.Font = Derivato(1f, FontStyle.Italic | FontStyle.Underline);
            linkEmail.ForeColor = TemaBibbia.Riferimento;
            linkEmail.Cursor = Cursors.Hand;

            // Frase in stile sottotitolo; sotto, il solo riferimento
            // biblico in corsivo, cliccabile.
            labelNome.Font = Derivato(1.15f, FontStyle.Regular);
            linkNome.Font = Derivato(1.15f, FontStyle.Italic);
            linkNome.LinkBehavior = LinkBehavior.AlwaysUnderline;

            foreach (Label t in new[] { labelTitoloFunzionalita, labelTitoloFeatures })
            {
                t.Font = Derivato(1.2f, FontStyle.Bold);
                t.ForeColor = TemaBibbia.Oro;
            }

            // Il testo va a capo entro la larghezza disponibile.
            foreach (Label l in new[]
            {
                labelSottotitolo, labelLicenza, labelNome, linkNome,
                labelFunzionalita, labelFeatures
            })
                l.MaximumSize = new Size(larghezzaUtile, 0);

            flowCopyright.MaximumSize = new Size(larghezzaUtile, 0);
            flowCopyright.WrapContents = true;

            int lato = Font.Height * 8;
            pictureBoxLogo.Size = new Size(lato, lato);
            pictureBoxLogo.BackColor = Color.Transparent;

            // Separatore vettoriale: altezza proporzionale al font.
            separatoreNome.Height = (int)(Font.Height * 1.1f);
        }

        private void AggiornaEvidenziatore()
        {
            labelSlogan.BackColor = TemaBibbia.Evidenziatore;
            labelSlogan.ForeColor = TemaBibbia.TestoEvidenziato;

            // I colori del LinkLabel non vengono rimappati dal tema.
            linkNome.LinkColor = TemaBibbia.Riferimento;
            linkNome.ActiveLinkColor = TemaBibbia.OroChiaro;
            linkNome.VisitedLinkColor = TemaBibbia.Riferimento;

            // Il separatore segue l'oro del tema.
            separatoreNome.Colore = TemaBibbia.Oro;
        }

        private void CaricaLogo()
        {
            try
            {
                string percorso =
                    Path.Combine(AppContext.BaseDirectory, "Risorse", "Gesu_icona.png");

                if (!File.Exists(percorso))
                {
                    pictureBoxLogo.Visible = false;
                    return;
                }

                // Copia in memoria per non bloccare il file; il PNG
                // conserva le trasparenze.
                using Image originale = Image.FromFile(percorso);
                pictureBoxLogo.Image = new Bitmap(originale);
            }
            catch
            {
                pictureBoxLogo.Visible = false;
            }
        }

        private void ApriEmail()
        {
            try
            {
                string indirizzo =
                    $"mailto:{Email}?subject={Uri.EscapeDataString(OggettoEmail)}";

                Process.Start(new ProcessStartInfo(indirizzo) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessaggioBibbia.Mostra(
                    "Impossibile aprire il programma di posta: " + ex.Message,
                    "Occhio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Riavvia il programma passando il riferimento biblico da aprire
        // nel form Leggi (argomento: --riferimento "Luca 24,13-32"),
        // senza chiedere conferma.
        private void RiavviaAlRiferimento()
        {
            try
            {
                var avvio = new ProcessStartInfo(Application.ExecutablePath)
                {
                    UseShellExecute = false,
                    WorkingDirectory = AppContext.BaseDirectory
                };
                avvio.ArgumentList.Add("--riferimento");
                avvio.ArgumentList.Add(RiferimentoNome);

                Process.Start(avvio);

                // La chiusura del programma la fa il chiamante
                // (FormINSERISCI), che vede RiavvioRichiesto.
                RiavvioRichiesto = true;
                Close();
            }
            catch (Exception ex)
            {
                RiavvioRichiesto = false;
                MessaggioBibbia.Mostra(
                    "Impossibile riavviare il programma: " + ex.Message,
                    "Occhio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    /// <summary>
    /// Linea di separazione elegante disegnata in vettoriale (GDI+):
    /// due filetti sfumati verso i bordi con al centro una croce
    /// latina affiancata da due punti. Si adatta a
    /// larghezza e DPI e non usa immagini.
    /// </summary>
    internal sealed class SeparatoreElegante : Control
    {
        private Color colore = Color.Goldenrod;

        public SeparatoreElegante()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            SetStyle(ControlStyles.Selectable, false);

            TabStop = false;
            BackColor = Color.Transparent;
        }

        public Color Colore
        {
            get => colore;
            set
            {
                if (colore == value)
                    return;

                colore = value;
                Invalidate();
            }
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            return new Size(0, Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float scala = DeviceDpi / 96f;
            float cx = Width / 2f;
            float cy = Height / 2f;

            // Croce latina: braccio verticale piu' lungo, traversa a circa
            // un terzo dall'alto.
            float altezzaCroce = Math.Min(Height - 2f, 20f * scala);
            float larghezzaCroce = altezzaCroce * 0.58f;
            float spessore = Math.Max(2f, 2.4f * scala);
            float rPiccola = 2f * scala;   // raggio dei due punti laterali
            float spazio = 10f * scala;    // distanza punti - croce
            float vuoto = larghezzaCroce / 2f + spazio + rPiccola + 8f * scala;
            float bordo = 4f * scala;

            float lunghezza = cx - vuoto - bordo;
            if (lunghezza > 4f)
            {
                DisegnaFiletto(g, bordo, cx - vuoto, cy, scala, true);
                DisegnaFiletto(g, cx + vuoto, Width - bordo, cy, scala, false);
            }

            using (var pennello = new SolidBrush(colore))
            {
                float alto = cy - altezzaCroce / 2f;

                // Braccio verticale e traversa.
                g.FillRectangle(pennello,
                    cx - spessore / 2f, alto, spessore, altezzaCroce);
                g.FillRectangle(pennello,
                    cx - larghezzaCroce / 2f,
                    alto + altezzaCroce * 0.32f - spessore / 2f,
                    larghezzaCroce, spessore);

                // Due punti laterali.
                float dx = larghezzaCroce / 2f + spazio;
                g.FillEllipse(pennello, cx - dx - rPiccola, cy - rPiccola,
                    rPiccola * 2, rPiccola * 2);
                g.FillEllipse(pennello, cx + dx - rPiccola, cy - rPiccola,
                    rPiccola * 2, rPiccola * 2);
            }
        }

        // Filetto principale con un secondo filetto piu' tenue sotto,
        // entrambi sfumati: trasparenti al bordo, pieni verso il centro.
        private void DisegnaFiletto(
            Graphics g, float x1, float x2, float cy, float scala, bool dalSinistra)
        {
            float larghezza = x2 - x1;
            if (larghezza < 2f)
                return;

            DisegnaRiga(g, x1, x2, cy, Math.Max(1f, scala), 235, dalSinistra);
            DisegnaRiga(g, x1 + larghezza * 0.18f, x2 - larghezza * 0.18f,
                cy + 3f * scala, Math.Max(1f, scala * 0.75f), 110, dalSinistra);
        }

        private void DisegnaRiga(
            Graphics g, float x1, float x2, float y,
            float spessore, int alfaMax, bool dalSinistra)
        {
            if (x2 - x1 < 2f)
                return;

            var rettangolo = new RectangleF(x1, y - 1f, x2 - x1, 2f);
            Color trasparente = Color.FromArgb(0, colore);
            Color pieno = Color.FromArgb(alfaMax, colore);

            using var pennello = new LinearGradientBrush(
                rettangolo, trasparente, pieno, LinearGradientMode.Horizontal);

            // Sul lato sinistro la sfumatura sale verso il centro, sul
            // destro scende: si specchia invertendo i colori.
            if (!dalSinistra)
            {
                pennello.LinearColors = new[] { pieno, trasparente };
            }

            using var penna = new Pen(pennello, spessore);
            g.DrawLine(penna, x1, y, x2, y);
        }
    }
}
