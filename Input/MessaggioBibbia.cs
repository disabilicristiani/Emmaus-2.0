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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.Windows.Forms;

namespace emmaus_v2
{
    internal static class MessaggioBibbia
    {
        public static DialogResult Mostra(string testo)
            => Mostra((IWin32Window)null, testo, "", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);

        public static DialogResult Mostra(string testo, string titolo)
            => Mostra((IWin32Window)null, testo, titolo, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);

        public static DialogResult Mostra(string testo, string titolo, MessageBoxButtons pulsanti)
            => Mostra((IWin32Window)null, testo, titolo, pulsanti, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);

        public static DialogResult Mostra(string testo, string titolo, MessageBoxButtons pulsanti, MessageBoxIcon icona)
            => Mostra((IWin32Window)null, testo, titolo, pulsanti, icona, MessageBoxDefaultButton.Button1);

        public static DialogResult Mostra(string testo, string titolo, MessageBoxButtons pulsanti, MessageBoxIcon icona,
            MessageBoxDefaultButton predefinito)
            => Mostra((IWin32Window)null, testo, titolo, pulsanti, icona, predefinito);

        public static DialogResult Mostra(IWin32Window proprietario, string testo)
            => Mostra(proprietario, testo, "", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);

        public static DialogResult Mostra(IWin32Window proprietario, string testo, string titolo)
            => Mostra(proprietario, testo, titolo, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);

        public static DialogResult Mostra(IWin32Window proprietario, string testo, string titolo, MessageBoxButtons pulsanti)
            => Mostra(proprietario, testo, titolo, pulsanti, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);

        public static DialogResult Mostra(IWin32Window proprietario, string testo, string titolo, MessageBoxButtons pulsanti,
            MessageBoxIcon icona)
            => Mostra(proprietario, testo, titolo, pulsanti, icona, MessageBoxDefaultButton.Button1);

        public static DialogResult Mostra(IWin32Window proprietario, string testo, string titolo, MessageBoxButtons pulsanti,
            MessageBoxIcon icona, MessageBoxDefaultButton predefinito)
        {
            if (proprietario == null)
                proprietario = Form.ActiveForm;

            using (var finestra = new FinestraMessaggio(testo ?? "", titolo ?? "", pulsanti, icona, predefinito))
            {
                try
                {
                    switch (icona)
                    {
                        case MessageBoxIcon.Information: SystemSounds.Asterisk.Play(); break;
                        case MessageBoxIcon.Warning: SystemSounds.Exclamation.Play(); break;
                        case MessageBoxIcon.Error: SystemSounds.Hand.Play(); break;
                        case MessageBoxIcon.Question: SystemSounds.Question.Play(); break;
                    }
                }
                catch { }

                return proprietario != null ? finestra.ShowDialog(proprietario) : finestra.ShowDialog();
            }
        }

        // Argomento della guida per un messaggio: prima in base al titolo
        // (che dice di che cosa si tratta), poi in base alla finestra da cui
        // e' comparso. Null = pagina iniziale.
        private static string AncoraGuida(Form proprietario, string titolo)
        {
            string form = "";

            for (Form f = proprietario; f != null; f = f.Owner)
            {
                string nome = f.GetType().Name;

                if (nome == "FormINSERISCI" || nome == "FormLEGGI" ||
                    nome == "FormMappaConcettuale" || nome == "FormTIMER" ||
                    nome == "FormMANUTENZIONE" || nome == "FormINFO")
                {
                    form = nome;
                    break;
                }
            }

            string t = (titolo ?? "").ToLowerInvariant();

            if (t.Contains("backup"))
                return "s9-2";

            if (t.Contains("database cei") || t.Contains("errore di avvio"))
                return "c12";

            if (t.Contains("pdf"))
            {
                switch (form)
                {
                    case "FormMappaConcettuale": return "s7-3";
                    case "FormMANUTENZIONE": return "s9-3";
                    default: return "s4-6";
                }
            }

            if (t.Contains("ripristino"))
                return "s9-4";

            if (t.Contains("eliminazione") || t.Contains("eliminati") ||
                t.Contains("richiesta conferma"))
                return form == "FormLEGGI" ? "s4-4" : "s9-5";

            if (t.Contains("manutenzione"))
                return "c9";

            switch (form)
            {
                case "FormINSERISCI": return "c3";
                case "FormLEGGI": return "c4";
                case "FormMappaConcettuale": return "c7";
                case "FormTIMER": return "c8";
                case "FormMANUTENZIONE": return "c9";
                case "FormINFO": return "c10";
                default: return null;
            }
        }

        private sealed class PannelloIcona : Control
        {
            private readonly string _simbolo;
            private readonly Color _colore;

            public PannelloIcona(string simbolo, Color colore)
            {
                _simbolo = simbolo;
                _colore = colore;
                TabStop = false;
                SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(TemaBibbia.Sfondo);

                var r = new RectangleF(2f, 2f, Width - 4.5f, Height - 4.5f);
                using (var pennello = new SolidBrush(TemaBibbia.FondoBottone))
                using (var penna = new Pen(_colore, 2.4f))
                {
                    g.FillEllipse(pennello, r);
                    g.DrawEllipse(penna, r);
                }

                using (var font = new Font("Segoe UI Semibold", Height * 0.46f, FontStyle.Regular, GraphicsUnit.Pixel))
                    TextRenderer.DrawText(g, _simbolo, font, ClientRectangle, _colore,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }
        }

        private sealed class FinestraMessaggio : Form
        {
            private readonly string _testoDaCopiare;

            public FinestraMessaggio(string testo, string titolo, MessageBoxButtons pulsanti, MessageBoxIcon icona,
                MessageBoxDefaultButton predefinito)
            {
                _testoDaCopiare = (titolo.Length > 0 ? titolo + "\r\n\r\n" : "") + testo;

                Text = titolo;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false;
                MaximizeBox = false;
                ShowIcon = false;
                ShowInTaskbar = false;
                KeyPreview = true;
                AutoScaleMode = AutoScaleMode.None;
                Font = new Font(Input.ZoomApp.FamigliaBase, 10.5f);

                TemaBibbia.Applica(this);

                float scala = DeviceDpi / 96f;
                int Px(int v) => (int)Math.Round(v * scala);

                int margine = Px(24);
                int spazio = Px(16);
                int larghezzaMassima = Px(460);
                int altezzaMassimaTesto = Px(320);
                int altezzaPulsante = Px(44);

                PannelloIcona pannelloIcona = null;
                int latoIcona = Px(52);
                switch (icona)
                {
                    case MessageBoxIcon.Information:
                        pannelloIcona = new PannelloIcona("i", TemaBibbia.Oro);
                        break;
                    case MessageBoxIcon.Warning:
                        pannelloIcona = new PannelloIcona("!", TemaBibbia.Oro);
                        break;
                    case MessageBoxIcon.Error:
                        pannelloIcona = new PannelloIcona("×", TemaBibbia.Errore);
                        break;
                    case MessageBoxIcon.Question:
                        pannelloIcona = new PannelloIcona("?", TemaBibbia.Oro);
                        break;
                }

                const TextFormatFlags flagsTesto = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix |
                                                   TextFormatFlags.TextBoxControl;
                Size misura = TextRenderer.MeasureText(testo.Length > 0 ? testo : " ", Font,
                    new Size(larghezzaMassima, int.MaxValue), flagsTesto);

                int larghezzaTesto = Math.Min(larghezzaMassima, Math.Max(misura.Width + Px(8), Px(220)));
                int altezzaTesto = misura.Height + Px(6);

                var etichetta = new Label
                {
                    AutoSize = false,
                    UseMnemonic = false,
                    Text = testo,
                    Location = new Point(0, 0),
                    Size = new Size(larghezzaTesto, altezzaTesto)
                };

                bool scorrimento = altezzaTesto > altezzaMassimaTesto;
                var pannelloTesto = new Panel
                {
                    AutoScroll = true,
                    Size = new Size(
                        larghezzaTesto + (scorrimento ? SystemInformation.VerticalScrollBarWidth : 0),
                        Math.Min(altezzaTesto, altezzaMassimaTesto))
                };
                pannelloTesto.Controls.Add(etichetta);

                (string Testo, DialogResult Esito)[] definizioni;
                switch (pulsanti)
                {
                    case MessageBoxButtons.OKCancel:
                        definizioni = new[] { ("OK", DialogResult.OK), ("Annulla", DialogResult.Cancel) };
                        break;
                    case MessageBoxButtons.AbortRetryIgnore:
                        definizioni = new[] { ("Interrompi", DialogResult.Abort), ("Riprova", DialogResult.Retry), ("Ignora", DialogResult.Ignore) };
                        break;
                    case MessageBoxButtons.YesNoCancel:
                        definizioni = new[] { ("Sì", DialogResult.Yes), ("No", DialogResult.No), ("Annulla", DialogResult.Cancel) };
                        break;
                    case MessageBoxButtons.YesNo:
                        definizioni = new[] { ("Sì", DialogResult.Yes), ("No", DialogResult.No) };
                        break;
                    case MessageBoxButtons.RetryCancel:
                        definizioni = new[] { ("Riprova", DialogResult.Retry), ("Annulla", DialogResult.Cancel) };
                        break;
                    default:
                        definizioni = new[] { ("OK", DialogResult.OK) };
                        break;
                }

                var bottoni = new Button[definizioni.Length];
                int larghezzaPulsanti = 0;
                for (int i = 0; i < definizioni.Length; i++)
                {
                    int larghezza = Math.Max(Px(104),
                        TextRenderer.MeasureText(definizioni[i].Testo, Font).Width + Px(44));

                    bottoni[i] = new Button
                    {
                        Text = definizioni[i].Testo,
                        DialogResult = definizioni[i].Esito,
                        Size = new Size(larghezza, altezzaPulsante),
                        Font = Font
                    };
                    larghezzaPulsanti += larghezza + (i > 0 ? spazio : 0);
                }

                int xTesto = margine + (pannelloIcona != null ? latoIcona + spazio : 0);
                int altezzaArea = Math.Max(pannelloIcona != null ? latoIcona : 0, pannelloTesto.Height);
                int yPulsanti = margine + altezzaArea + Px(22);

                int larghezzaContenuto = xTesto + pannelloTesto.Width + margine;
                int larghezzaClient = Math.Max(larghezzaContenuto, larghezzaPulsanti + 2 * margine);

                ClientSize = new Size(larghezzaClient, yPulsanti + altezzaPulsante + margine);

                if (pannelloIcona != null)
                {
                    pannelloIcona.Size = new Size(latoIcona, latoIcona);
                    pannelloIcona.Location = new Point(margine, margine + (altezzaArea - latoIcona) / 2);
                    Controls.Add(pannelloIcona);
                }

                int xTestoCentrato = xTesto + Math.Max(0, (larghezzaClient - larghezzaContenuto) / 2);
                pannelloTesto.Location = new Point(xTestoCentrato, margine + (altezzaArea - pannelloTesto.Height) / 2);
                Controls.Add(pannelloTesto);

                int x = (larghezzaClient - larghezzaPulsanti) / 2;
                foreach (Button b in bottoni)
                {
                    b.Location = new Point(x, yPulsanti);
                    x += b.Width + spazio;
                    Controls.Add(b);
                }

                int indicePredefinito;
                switch (predefinito)
                {
                    case MessageBoxDefaultButton.Button2: indicePredefinito = 1; break;
                    case MessageBoxDefaultButton.Button3: indicePredefinito = 2; break;
                    default: indicePredefinito = 0; break;
                }
                if (indicePredefinito >= bottoni.Length)
                    indicePredefinito = 0;

                AcceptButton = bottoni[indicePredefinito];
                ActiveControl = bottoni[indicePredefinito];

                Button annulla = null;
                foreach (Button b in bottoni)
                    if (b.DialogResult == DialogResult.Cancel)
                        annulla = b;
                if (annulla == null && pulsanti == MessageBoxButtons.OK)
                    annulla = bottoni[0];

                CancelButton = annulla;
                ControlBox = annulla != null;
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (e.KeyCode == Keys.F1)
                {
                    Input.FormGUIDA.Mostra(Owner, AncoraGuida(Owner, Text));
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }

                if (e.Control && e.KeyCode == Keys.C)
                {
                    try { Clipboard.SetText(_testoDaCopiare); }
                    catch { }
                    e.Handled = true;
                    return;
                }
                base.OnKeyDown(e);
            }
        }
    }
}
