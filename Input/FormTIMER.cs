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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text;
using System.Windows.Forms;

namespace emmaus_v2
{
    public partial class FormTIMER : Form
    {
        private const int CS_NOCLOSE = 0x200;

        private readonly Stopwatch _cronometro = new Stopwatch();

        private TimeSpan _base = TimeSpan.Zero;

        private long _ultimoSalvataggioMs;
        private const int IntervalloSalvataggioMs = 33000;

        private static readonly string PercorsoStato =
            System.IO.Path.Combine(AppContext.BaseDirectory, "timer_stato.txt");

        private TimeSpan Trascorso => _cronometro.Elapsed + _base;

        private const string SimboloPlay = "▶";
        private const string SimboloPausa = "⏸";

        private const string TitoloBase = "Meditazione";
        private const string TitoloPredefinito = "Emmaus 2.0";
        private const string TitoloInMeditazione = "Meditazione...";

        private static readonly TimeSpan DurataGiro = TimeSpan.FromMinutes(30);

        private const int MaxGiri = 7;
        private static readonly TimeSpan DurataMassima = TimeSpan.FromTicks(DurataGiro.Ticks * MaxGiri);

        private TimeSpan TempoTrascorso =>
            Trascorso < DurataMassima ? Trascorso : DurataMassima;

        private static Color ColoreSfondo => TemaBibbia.Sfondo;
        private static Color ColoreOro => TemaBibbia.Oro;
        private static Color ColoreAvorio => TemaBibbia.Avorio;
        private static Color FondoBottone => TemaBibbia.FondoBottone;
        private static Color FondoBottoneSopra => TemaBibbia.FondoBottoneSopra;
        private static Color FondoBottonePremuto => TemaBibbia.FondoBottonePremuto;

        private enum ModalitaAudio { Nessuna, Gesu, Maria }

        private ModalitaAudio _modalita = ModalitaAudio.Nessuna;

        // Sintesi vocale di sistema di Windows (voci installate nel PC
        // dell'utente): nessun file audio e nessuna voce vengono inclusi.
        private SpeechSynthesizer _voce;

        private bool _riprendiTimerAFineAudio;

        private readonly System.Windows.Forms.Timer _timerTitolo;
        private string _titoloScorrevole = "";
        private int _posizioneTitolo;
        private const int LarghezzaTitolo = 24;

        private readonly System.Windows.Forms.Timer _timerAnimazione;
        private float _fase;
        private float _alfaPreghiera;
        private bool _preghieraVisibile;

        private readonly HashSet<Button> _sopra = new HashSet<Button>();
        private readonly HashSet<Button> _premuti = new HashSet<Button>();

        public FormTIMER()
        {
            InitializeComponent();

            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            ApplicaAspetto();

            pictureBoxGesu.Image = CaricaImmagine("jesux4.png");
            pictureBoxMaria.Image = CaricaImmagine("mariax4.png");

            pictureBoxGesu.Cursor = Cursors.Hand;
            pictureBoxMaria.Cursor = Cursors.Hand;
            pictureBoxGesu.Click += (s, e) => ApriFoto("jesux4.png");
            pictureBoxMaria.Click += (s, e) => ApriFoto("mariax4.png");

            _timerTitolo = new System.Windows.Forms.Timer(components) { Interval = 300 };
            _timerTitolo.Tick += TimerTitolo_Tick;

            _timerAnimazione = new System.Windows.Forms.Timer(components) { Interval = 40 };
            _timerAnimazione.Tick += TimerAnimazione_Tick;
            _timerAnimazione.Start();

            RipristinaStato();

            TemaBibbia.Suggerimento(buttonPlayPausa, "Play / Pausa (Ctrl+P)");
            TemaBibbia.Suggerimento(buttonStop, "Stop (Ctrl+S)");
            TemaBibbia.Suggerimento(button_prayGesu, "Intenzioni del Pontefice (Ctrl+G)");
            TemaBibbia.Suggerimento(button_prayMaria, "Intenzioni di S. Maria (Ctrl+M)");

            button_prayGesu.Click += (s, e) =>
            {
                AvviaAudio(Preghiere.Gesu, ModalitaAudio.Gesu, "Per intenzioni Pontefice");
                MostraPreghiera("jesux4.png", "Intenzioni del Pontefice", Preghiere.Gesu);
            };
            button_prayMaria.Click += (s, e) =>
            {
                AvviaAudio(Preghiere.Maria, ModalitaAudio.Maria, "Per intenzioni S. Maria");
                MostraPreghiera("mariax4.png", "Intenzioni di S. Maria", Preghiere.Maria);
            };

            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                    e.Cancel = true;
            };

            FormClosed += (s, e) =>
            {
                bool eraInMarcia = _cronometro.IsRunning
                    || (_modalita != ModalitaAudio.Nessuna && _riprendiTimerAFineAudio);

                TerminaAudio(false);
                ChiudiPreghiera();

                if (eraInMarcia)
                    EliminaStato();
                else
                    SalvaStato();
            };

            TemaBibbia.TemaCambiato += SuTemaCambiato;
            FormClosed += (s, e) => TemaBibbia.TemaCambiato -= SuTemaCambiato;
        }

        private void SalvaStato()
        {
            try
            {
                TimeSpan t = Trascorso;
                if (t > TimeSpan.Zero && t < DurataGiro)
                {
                    string tmp = PercorsoStato + ".tmp";
                    System.IO.File.WriteAllText(tmp, t.Ticks.ToString());
                    System.IO.File.Move(tmp, PercorsoStato, true);
                }
                else
                {
                    EliminaStato();
                }
            }
            catch { }
        }

        private static void EliminaStato()
        {
            try
            {
                if (System.IO.File.Exists(PercorsoStato))
                    System.IO.File.Delete(PercorsoStato);
            }
            catch { }
        }

        private void RipristinaStato()
        {
            try
            {
                if (!System.IO.File.Exists(PercorsoStato)) return;

                if (long.TryParse(System.IO.File.ReadAllText(PercorsoStato).Trim(), out long ticks)
                    && ticks > 0 && ticks < DurataGiro.Ticks)
                {
                    _base = TimeSpan.FromTicks(ticks);
                    AggiornaDisplay();
                    AggiornaSimboloPlayPausa();
                }
                else
                {
                    EliminaStato();
                }
            }
            catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F1:
                    Input.FormGUIDA.Mostra(this, "c8");
                    return true;

                case Keys.Control | Keys.P:
                    ScorciatoieBibbia.Premi(buttonPlayPausa);
                    return true;

                case Keys.Control | Keys.S:
                    ScorciatoieBibbia.Premi(buttonStop);
                    return true;

                case Keys.Control | Keys.G:
                    ScorciatoieBibbia.Premi(button_prayGesu);
                    return true;

                case Keys.Control | Keys.M:
                    ScorciatoieBibbia.Premi(button_prayMaria);
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_NOCLOSE;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation => true;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int valore, int dimensione);

        private static int ColoreBgr(Color c) => c.R | (c.G << 8) | (c.B << 16);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplicaBarraTitolo();
        }

        private void ApplicaBarraTitolo()
        {
            try
            {
                int si = TemaBibbia.Chiaro ? 0 : 1;
                DwmSetWindowAttribute(Handle, 20, ref si, sizeof(int));
                int sfondo = ColoreBgr(ColoreSfondo);
                DwmSetWindowAttribute(Handle, 35, ref sfondo, sizeof(int));
                int testo = ColoreBgr(ColoreOro);
                DwmSetWindowAttribute(Handle, 36, ref testo, sizeof(int));
                int angoli = 2;
                DwmSetWindowAttribute(Handle, 33, ref angoli, sizeof(int));
            }
            catch { }
        }

        private static void AbilitaDoppioBuffer(Control c)
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(c, true);
        }

        private void SuTemaCambiato(object sender, EventArgs e)
        {
            BackColor = ColoreSfondo;
            tableLayoutPanel1.BackColor = ColoreSfondo;
            flowLayoutPanelPulsanti.BackColor = ColoreSfondo;
            labelTimer.BackColor = ColoreSfondo;
            labelTimer.ForeColor = ColoreAvorio;

            foreach (Button b in new[] { button_prayGesu, buttonPlayPausa, buttonStop, button_prayMaria })
                b.BackColor = ColoreSfondo;

            Image vecchiaGesu = pictureBoxGesu.Image;
            Image vecchiaMaria = pictureBoxMaria.Image;
            pictureBoxGesu.BackColor = ColoreSfondo;
            pictureBoxMaria.BackColor = ColoreSfondo;
            pictureBoxGesu.Image = CaricaImmagine("jesux4.png");
            pictureBoxMaria.Image = CaricaImmagine("mariax4.png");
            vecchiaGesu?.Dispose();
            vecchiaMaria?.Dispose();

            if (IsHandleCreated) ApplicaBarraTitolo();
            Invalidate(true);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // La riga dei pulsanti prende l'altezza di cui ha bisogno e il
            // timer il resto; se la finestra e' troppo bassa per il font
            // e la scala di Windows in uso, viene alzata quanto basta.
            tableLayoutPanel1.RowStyles[0] = new RowStyle(SizeType.Percent, 100F);
            tableLayoutPanel1.RowStyles[1] = new RowStyle(SizeType.AutoSize);

            int necessario =
                tableLayoutPanel1.Padding.Vertical +
                labelTimer.PreferredHeight + labelTimer.Margin.Vertical +
                flowLayoutPanelPulsanti.PreferredSize.Height +
                flowLayoutPanelPulsanti.Margin.Vertical;

            if (ClientSize.Height < necessario)
                ClientSize = new Size(ClientSize.Width, necessario);
        }

        private void ApplicaAspetto()
        {
            BackColor = ColoreSfondo;

            tableLayoutPanel1.BackColor = ColoreSfondo;
            flowLayoutPanelPulsanti.BackColor = ColoreSfondo;
            pictureBoxGesu.BackColor = ColoreSfondo;
            pictureBoxMaria.BackColor = ColoreSfondo;

            AbilitaDoppioBuffer(tableLayoutPanel1);
            AbilitaDoppioBuffer(flowLayoutPanelPulsanti);
            AbilitaDoppioBuffer(labelTimer);
            AbilitaDoppioBuffer(pictureBoxGesu);
            AbilitaDoppioBuffer(pictureBoxMaria);

            labelTimer.BackColor = ColoreSfondo;
            labelTimer.ForeColor = ColoreAvorio;
            labelTimer.Font = new Font("Segoe UI Light", 20F * Input.ZoomApp.Fattore, FontStyle.Regular);
            labelTimer.Padding = new Padding(0, 0, 0, 10);
            labelTimer.Paint += LabelTimer_Paint;

            Input.ZoomApp.AdattaFont(button_prayGesu, button_prayMaria, buttonPlayPausa, buttonStop);

            button_prayGesu.Size = new Size(44, 44);
            button_prayMaria.Size = new Size(44, 44);
            buttonPlayPausa.Size = new Size(96, 44);
            buttonStop.Size = new Size(96, 44);

            foreach (Button b in new[] { button_prayGesu, buttonPlayPausa, buttonStop, button_prayMaria })
                StileBottone(b);

            button_prayGesu.Enabled = false;
            button_prayMaria.Enabled = false;
            _alfaPreghiera = 0f;
        }

        private void StileBottone(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.UseVisualStyleBackColor = false;
            b.BackColor = ColoreSfondo;
            b.Cursor = Cursors.Hand;
            AbilitaDoppioBuffer(b);

            b.Paint += BottoneDisegna;
            b.MouseEnter += (s, e) => { _sopra.Add(b); b.Invalidate(); };
            b.MouseLeave += (s, e) => { _sopra.Remove(b); _premuti.Remove(b); b.Invalidate(); };
            b.MouseDown += (s, e) => { _premuti.Add(b); b.Invalidate(); };
            b.MouseUp += (s, e) => { _premuti.Remove(b); b.Invalidate(); };
        }

        private static Color Mescola(Color da, Color a, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            return Color.FromArgb(
                (int)(da.R + (a.R - da.R) * t),
                (int)(da.G + (a.G - da.G) * t),
                (int)(da.B + (a.B - da.B) * t));
        }

        private static GraphicsPath PercorsoArrotondato(RectangleF r, float raggio)
        {
            float d = Math.Min(raggio * 2f, Math.Min(r.Width, r.Height));
            var p = new GraphicsPath();
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private void BottoneDisegna(object sender, PaintEventArgs e)
        {
            Button b = (Button)sender;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(ColoreSfondo);

            bool preghiera = b == button_prayGesu || b == button_prayMaria;
            float a = preghiera ? _alfaPreghiera : 1f;
            if (a <= 0.01f) return;

            bool attivo = (b == button_prayGesu && _modalita == ModalitaAudio.Gesu)
                       || (b == button_prayMaria && _modalita == ModalitaAudio.Maria);
            bool sopra = _sopra.Contains(b);
            bool premuto = _premuti.Contains(b);

            Color fondo = attivo ? ColoreOro
                        : premuto ? FondoBottonePremuto
                        : sopra ? FondoBottoneSopra
                        : FondoBottone;
            Color bordo = sopra || attivo ? ColoreOro : Mescola(FondoBottone, ColoreOro, 0.6f);
            Color testo = attivo ? TemaBibbia.TestoSuOro : (preghiera ? ColoreOro : ColoreAvorio);

            var r = new RectangleF(1.5f, 1.5f, b.Width - 3.5f, b.Height - 3.5f);
            using (GraphicsPath path = PercorsoArrotondato(r, Math.Min(r.Height / 2f, TemaBibbia.RaggioBottone)))
            using (var pennello = new SolidBrush(Mescola(ColoreSfondo, fondo, a)))
            using (var penna = new Pen(Mescola(ColoreSfondo, bordo, a), 1.6f))
            {
                g.FillPath(pennello, path);
                g.DrawPath(penna, path);
            }

            TextRenderer.DrawText(g, b.Text, b.Font, b.ClientRectangle,
                Mescola(Mescola(ColoreSfondo, fondo, a), testo, a),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private void LabelTimer_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var r = new RectangleF(5f, 5f, labelTimer.Width - 11f, labelTimer.Height - 11f);
            float rad = r.Height / 2f;
            float rettilineo = r.Width - 2f * rad;
            if (rad < 6f || rettilineo < 4f) return;

            float spessore = Math.Max(3f, r.Height * 0.055f);
            float perimetro = 2f * rettilineo + 2f * (float)Math.PI * rad;

            TimeSpan t = TempoTrascorso;
            int giri;
            double frazione;
            if (t >= DurataMassima)
            {
                giri = MaxGiri;
                frazione = 1.0;
            }
            else
            {
                giri = (int)(t.TotalMinutes / DurataGiro.TotalMinutes);
                frazione = (t.TotalMinutes % DurataGiro.TotalMinutes) / DurataGiro.TotalMinutes;
            }

            bool respira = AudioInRiproduzione;
            float respiro = respira ? 0.5f + 0.5f * (float)Math.Sin(_fase) : 0f;

            float cx = r.Left + r.Width / 2f;
            using (var path = new GraphicsPath())
            {
                path.StartFigure();
                path.AddLine(cx, r.Top, r.Right - rad, r.Top);
                path.AddArc(r.Right - 2f * rad, r.Top, 2f * rad, 2f * rad, -90, 180);
                path.AddLine(r.Right - rad, r.Bottom, r.Left + rad, r.Bottom);
                path.AddArc(r.Left, r.Top, 2f * rad, 2f * rad, 90, 180);
                path.AddLine(r.Left + rad, r.Top, cx, r.Top);
                path.CloseFigure();

                int alfaTraccia = giri >= 1 ? 120 : 55;
                alfaTraccia = Math.Min(255, alfaTraccia + (int)(25 * respiro));
                using (var penTraccia = new Pen(Color.FromArgb(alfaTraccia, ColoreOro), spessore))
                {
                    penTraccia.LineJoin = LineJoin.Round;
                    g.DrawPath(penTraccia, path);
                }

                float lunghezza = (float)(frazione * perimetro);
                if (lunghezza > 0.5f)
                {
                    float lunghezzaTratto = Math.Max(0.01f, lunghezza - spessore);
                    float[] schema = { lunghezzaTratto / spessore, perimetro * 2f / spessore };

                    if (respira)
                    {
                        using (var penAlone = new Pen(Color.FromArgb((int)(25 + 55 * respiro), ColoreOro), spessore * 3f))
                        {
                            penAlone.DashCap = DashCap.Round;
                            penAlone.DashPattern = new[] { (lunghezzaTratto + spessore * 2f) / (spessore * 3f), perimetro * 2f / (spessore * 3f) };
                            penAlone.DashOffset = spessore / (spessore * 3f);
                            g.DrawPath(penAlone, path);
                        }
                    }

                    using (var penAvanz = new Pen(Color.FromArgb(respira ? (int)(190 + 65 * respiro) : 255, ColoreOro), spessore))
                    {
                        penAvanz.DashCap = DashCap.Round;
                        penAvanz.DashPattern = schema;
                        penAvanz.DashOffset = 0f;
                        g.DrawPath(penAvanz, path);
                    }
                }
            }

            if (giri >= 1)
            {
                int n = Math.Min(giri, MaxGiri);
                float raggioPunto = Math.Max(2f, spessore * 0.55f);
                float passo = raggioPunto * 3.4f;
                float xInizio = cx - passo * (n - 1) / 2f;
                float y = r.Bottom - spessore * 2.4f;
                using (var pennello = new SolidBrush(ColoreOro))
                {
                    for (int i = 0; i < n; i++)
                        g.FillEllipse(pennello, xInizio + i * passo - raggioPunto, y - raggioPunto,
                            raggioPunto * 2f, raggioPunto * 2f);
                }
            }
        }

        private bool AudioInRiproduzione =>
            _modalita != ModalitaAudio.Nessuna
            && _voce != null
            && _voce.State != SynthesizerState.Paused;

        private void TimerAnimazione_Tick(object sender, EventArgs e)
        {
            bool ridisegnaAnello = false;

            if (AudioInRiproduzione)
            {
                _fase += 0.07f;
                ridisegnaAnello = true;
            }

            float obiettivo = _preghieraVisibile ? 1f : 0f;
            if (Math.Abs(_alfaPreghiera - obiettivo) > 0.001f)
            {
                _alfaPreghiera = _alfaPreghiera < obiettivo
                    ? Math.Min(obiettivo, _alfaPreghiera + 0.06f)
                    : Math.Max(obiettivo, _alfaPreghiera - 0.06f);

                bool attivi = _preghieraVisibile && _alfaPreghiera >= 0.5f;
                button_prayGesu.Enabled = attivi;
                button_prayMaria.Enabled = attivi;
                button_prayGesu.Invalidate();
                button_prayMaria.Invalidate();
            }

            if (ridisegnaAnello)
                labelTimer.Invalidate();
        }

        private static Image CaricaImmagine(string nomeFile)
        {
            string percorso = TrovaRisorsa(nomeFile);
            if (percorso != null)
            {
                using (Image originale = Image.FromFile(percorso))
                using (Bitmap copia = new Bitmap(originale))
                    return RendiOvale(copia);
            }
            return null;
        }

        private static Bitmap RendiOvale(Image sorgente)
        {
            int w = sorgente.Width;
            int h = sorgente.Height;
            int bordo = Math.Max(4, Math.Min(w, h) / 36);

            var risultato = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(risultato))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                using (var ellisse = new GraphicsPath())
                {
                    ellisse.AddEllipse(bordo, bordo, w - 2 * bordo, h - 2 * bordo);
                    g.SetClip(ellisse);
                    g.DrawImage(sorgente, 0, 0, w, h);
                    g.ResetClip();
                }

                using (var penna = new Pen(ColoreOro, bordo))
                    g.DrawEllipse(penna, bordo / 2f, bordo / 2f, w - bordo, h - bordo);
            }
            return risultato;
        }

        private Listbox.FormFoto _fotoPreghiera;
        private Listbox.FormLEGGI _leggiPreghiera;

        private void ChiudiPreghiera()
        {
            if (_fotoPreghiera != null && !_fotoPreghiera.IsDisposed)
                _fotoPreghiera.Close();
            if (_leggiPreghiera != null && !_leggiPreghiera.IsDisposed)
                _leggiPreghiera.Close();

            _fotoPreghiera = null;
            _leggiPreghiera = null;
        }

        // Apre la foto (centrata nello schermo) e la griglia con le preghiere.
        private void MostraPreghiera(string nomeFoto, string titolo,
            IReadOnlyList<(string Titolo, string Testo)> sezioni)
        {
            ChiudiPreghiera();

            const int distanza = 10;

            Screen schermo = Screen.FromControl(this);
            Rectangle area = schermo.WorkingArea;

            // La griglia si mostra per prima: serve la sua larghezza reale
            // per lasciare alla foto lo spazio accanto.
            var leggi = new Listbox.FormLEGGI(titolo, sezioni);
            leggi.FormClosed += (s, e) =>
            {
                if (_leggiPreghiera == leggi) _leggiPreghiera = null;
            };
            _leggiPreghiera = leggi;
            leggi.Show(this);

            string percorso = TrovaRisorsa(nomeFoto);
            if (percorso == null)
                return;

            var foto = new Listbox.FormFoto(percorso, schermo, leggi.Width + distanza);
            foto.FormClosed += (s, e) =>
            {
                if (_fotoPreghiera == foto) _fotoPreghiera = null;
            };
            _fotoPreghiera = foto;
            foto.Show(this);

            // Foto a sinistra e griglia a destra, affiancate e centrate
            // orizzontalmente nello schermo, con la stessa altezza.
            int larghezzaTotale = foto.Width + distanza + leggi.Width;
            int sinistra = area.Left + (area.Width - larghezzaTotale) / 2;
            int alto = area.Top + (area.Height - foto.Height) / 2;

            foto.Location = new Point(sinistra, alto);

            leggi.Height = Math.Min(foto.Height, area.Height);
            leggi.Location = new Point(sinistra + foto.Width + distanza, alto);
        }

        private void ApriFoto(string nomeFile)
        {
            string percorso = TrovaRisorsa(nomeFile);
            if (percorso == null)
                return;

            using var formFoto = new Listbox.FormFoto(percorso);
            formFoto.ShowDialog(this);
        }

        private static string TrovaRisorsa(string nomeFile)
        {
            foreach (string cartella in new[] { "Risorse", "Resources" })
            {
                string percorso = System.IO.Path.Combine(AppContext.BaseDirectory, cartella, nomeFile);
                if (System.IO.File.Exists(percorso))
                    return percorso;
            }
            return null;
        }

        private void FermaTimer()
        {
            _cronometro.Stop();
            timerAggiorna.Stop();
            SalvaStato();
            AggiornaDisplay();
            AggiornaTitoloFinestra();
        }

        private void RiavviaTimer()
        {
            _cronometro.Start();
            timerAggiorna.Start();
            AggiornaDisplay();
            AggiornaTitoloFinestra();
        }

        // Compone il testo da leggere: titolo e testo di ogni sezione.
        // Le righe senza punteggiatura finale ricevono una virgola, cosi'
        // la voce fa la pausa naturale tra un verso e l'altro.
        private static string TestoDaLeggere(IReadOnlyList<(string Titolo, string Testo)> sezioni)
        {
            var sb = new StringBuilder();

            foreach (var (titolo, testo) in sezioni)
            {
                sb.Append(titolo).Append(". ");

                foreach (string riga in testo.Split('\n'))
                {
                    string r = riga.Trim();
                    if (r.Length == 0)
                        continue;

                    sb.Append(r);
                    if (".,;:!?".IndexOf(r[r.Length - 1]) < 0)
                        sb.Append(',');
                    sb.Append(' ');
                }
            }

            return sb.ToString();
        }

        private void AvviaAudio(IReadOnlyList<(string Titolo, string Testo)> sezioni,
            ModalitaAudio modalita, string titolo)
        {
            bool riprendi = _modalita != ModalitaAudio.Nessuna
                ? _riprendiTimerAFineAudio
                : _cronometro.IsRunning;

            TerminaAudio(false);

            try
            {
                var cultura = new CultureInfo("it-IT");
                var voce = new SpeechSynthesizer();

                // Serve una voce italiana installata in Windows: con una
                // voce di un'altra lingua le preghiere sarebbero illeggibili.
                if (voce.GetInstalledVoices(cultura).Count == 0)
                {
                    voce.Dispose();
                    if (riprendi) RiavviaTimer();
                    AggiornaSimboloPlayPausa();
                    MessaggioBibbia.Mostra(this,
                        "Non e' installata nessuna voce italiana di Windows.\n" +
                        "Aggiungila da Impostazioni > Ora e lingua > Voce.",
                        TitoloBase, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                voce.SelectVoiceByHints(VoiceGender.NotSet, VoiceAge.NotSet, 0, cultura);
                voce.SetOutputToDefaultAudioDevice();
                voce.SpeakCompleted += Voce_SpeakCompleted;

                _voce = voce;
                _modalita = modalita;
                _voce.SpeakAsync(TestoDaLeggere(sezioni));

                _riprendiTimerAFineAudio = riprendi;
                FermaTimer();
                AvviaTitoloScorrevole(titolo);
                AggiornaSimboloPlayPausa();
            }
            catch (Exception ex)
            {
                TerminaAudio(false);
                if (riprendi) RiavviaTimer();
                AggiornaSimboloPlayPausa();
                MessaggioBibbia.Mostra(this, "Impossibile avviare la sintesi vocale:\n" + ex.Message,
                    TitoloBase, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TerminaAudio(bool riprendiTimer = true)
        {
            if (_voce != null)
            {
                _voce.SpeakCompleted -= Voce_SpeakCompleted;
                try
                {
                    // Una voce in pausa va ripresa prima di annullare.
                    if (_voce.State == SynthesizerState.Paused)
                        _voce.Resume();
                    _voce.SpeakAsyncCancelAll();
                }
                catch { }
                _voce.Dispose();
                _voce = null;
            }

            bool eraInAudio = _modalita != ModalitaAudio.Nessuna;
            _modalita = ModalitaAudio.Nessuna;
            FermaTitoloScorrevole();

            if (riprendiTimer && eraInAudio && _riprendiTimerAFineAudio)
                RiavviaTimer();
            if (riprendiTimer)
                _riprendiTimerAFineAudio = false;

            AggiornaSimboloPlayPausa();
        }

        private void Voce_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            // L'evento puo' arrivare da un thread diverso da quello della UI.
            if (IsDisposed)
                return;

            if (InvokeRequired)
                BeginInvoke(new Action(() => { if (!IsDisposed) TerminaAudio(); }));
            else
                TerminaAudio();
        }

        private void AvviaTitoloScorrevole(string testo)
        {
            _titoloScorrevole = testo + "   •   ";
            _posizioneTitolo = 0;
            AggiornaTitolo();
            _timerTitolo.Start();
        }

        private void FermaTitoloScorrevole()
        {
            _timerTitolo.Stop();
            AggiornaTitoloFinestra();
        }

        private void TimerTitolo_Tick(object sender, EventArgs e)
        {
            _posizioneTitolo = (_posizioneTitolo + 1) % _titoloScorrevole.Length;
            AggiornaTitolo();
        }

        private void AggiornaTitolo()
        {
            string ripetuto = _titoloScorrevole + _titoloScorrevole + _titoloScorrevole;
            Text = ripetuto.Substring(_posizioneTitolo, LarghezzaTitolo);
        }

        private void AggiornaTitoloFinestra()
        {
            // Durante l'audio Gesu'/Maria il titolo scorrevole non va mai toccato
            if (_modalita != ModalitaAudio.Nessuna)
                return;

            Text = _cronometro.IsRunning ? TitoloInMeditazione : TitoloPredefinito;
        }

        private void AggiornaSimboloPlayPausa()
        {
            bool inRiproduzione = _modalita != ModalitaAudio.Nessuna
                ? (_voce != null && _voce.State != SynthesizerState.Paused)
                : _cronometro.IsRunning;

            buttonPlayPausa.Text = inRiproduzione ? SimboloPausa : SimboloPlay;

            button_prayGesu.Invalidate();
            button_prayMaria.Invalidate();
            buttonPlayPausa.Invalidate();
            labelTimer.Invalidate();
        }

        private void buttonPlayPausa_Click(object sender, EventArgs e)
        {
            if (_modalita != ModalitaAudio.Nessuna && _voce != null)
            {
                if (_voce.State == SynthesizerState.Paused)
                {
                    _voce.Resume();
                    _timerTitolo.Start();
                }
                else
                {
                    _voce.Pause();
                    _timerTitolo.Stop();
                }

                AggiornaSimboloPlayPausa();
                return;
            }

            if (_cronometro.IsRunning)
                FermaTimer();
            else if (Trascorso < DurataMassima)
                RiavviaTimer();

            AggiornaSimboloPlayPausa();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            if (_modalita != ModalitaAudio.Nessuna)
            {
                TerminaAudio();
                return;
            }

            _cronometro.Reset();
            _base = TimeSpan.Zero;
            EliminaStato();
            timerAggiorna.Stop();
            AggiornaSimboloPlayPausa();
            AggiornaDisplay();
            AggiornaTitoloFinestra();
        }

        private void timerAggiorna_Tick(object sender, EventArgs e)
        {
            AggiornaDisplay();
        }

        private void AggiornaDisplay()
        {
            if (_cronometro.IsRunning && Trascorso >= DurataMassima)
            {
                _cronometro.Stop();
                timerAggiorna.Stop();
                AggiornaSimboloPlayPausa();
                AggiornaTitoloFinestra();
            }

            if (_cronometro.IsRunning &&
                Environment.TickCount64 - _ultimoSalvataggioMs >= IntervalloSalvataggioMs)
            {
                _ultimoSalvataggioMs = Environment.TickCount64;
                SalvaStato();
            }

            TimeSpan t = TempoTrascorso;
            labelTimer.Text = string.Format("{0:00}:{1:00}:{2:00}",
                (int)t.TotalHours, t.Minutes, t.Seconds);

            _preghieraVisibile = t >= DurataGiro;

            labelTimer.Invalidate();
        }
    }
}
