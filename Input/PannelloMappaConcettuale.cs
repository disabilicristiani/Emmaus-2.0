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
namespace Listbox
{
    
    public sealed class PannelloMappaConcettuale : Panel
    {
        
        public sealed class NodoVisuale
        {
            public string Etichetta { get; set; }
            public bool HaNota { get; set; }
            public object Tag { get; set; }
            public PointF PosizioneMondo { get; set; }
            public string Anteprima { get; set; }

            internal RectangleF RettangoloSchermo { get; set; }
        }

        private List<NodoVisuale> nodi = new List<NodoVisuale>();
        private List<(NodoVisuale Da, NodoVisuale A)> archi = new List<(NodoVisuale, NodoVisuale)>();
        private NodoVisuale nodoCentrale;

        private HashSet<NodoVisuale> nodiCentrali = new HashSet<NodoVisuale>();

        private float zoom = 1f;
        private PointF pan = PointF.Empty;
        private Point? puntoTrascinamento;
        private bool trascinamentoInCorso;

        private const float RaggioNodo = 24f;

        private static Color ColoreOro => TemaBibbia.Oro;
        private static readonly Color ColoreBluMariano = Color.FromArgb(255, 47, 90, 171);
        private static Color ColoreAvorio => TemaBibbia.Avorio;

        private readonly System.Windows.Forms.Timer timerAnimazione = new System.Windows.Forms.Timer { Interval = 60 };
        private float faseAnimazione = 0f;

        private Bitmap sfondoStelle;

        private readonly ToolTip tooltipAnteprima = new ToolTip
        {
            OwnerDraw = true,
            AutoPopDelay = 10000,
            InitialDelay = 250,
            ReshowDelay = 100,
            ShowAlways = true
        };
        private NodoVisuale nodoInHover;
        private static readonly Font FontAnteprima = new Font("Segoe UI", 17f, FontStyle.Regular);

        public event Action<NodoVisuale> NodoCliccato;

        public IReadOnlyList<NodoVisuale> NodiCorrenti => nodi;

        public IReadOnlyList<(NodoVisuale Da, NodoVisuale A)> ArchiCorrenti => archi;

        public IReadOnlyCollection<NodoVisuale> NodiCentraliCorrenti => nodiCentrali;

        public PannelloMappaConcettuale()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                      ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = TemaBibbia.Sfondo;

            MouseWheel += PannelloMappaConcettuale_MouseWheel;
            MouseDown += PannelloMappaConcettuale_MouseDown;
            MouseMove += PannelloMappaConcettuale_MouseMove;
            MouseUp += PannelloMappaConcettuale_MouseUp;
            MouseLeave += (s, e) => NascondiAnteprima();
            Resize += (s, e) => RigeneraSfondo();

            tooltipAnteprima.Popup += TooltipAnteprima_Popup;
            tooltipAnteprima.Draw += TooltipAnteprima_Draw;

            timerAnimazione.Tick += (s, e) =>
            {
                faseAnimazione += 0.12f;
                if (faseAnimazione > Math.PI * 2)
                    faseAnimazione -= (float)(Math.PI * 2);
                Invalidate();
            };
            timerAnimazione.Start();

            RigeneraSfondo();

            TemaBibbia.TemaCambiato += SuTemaCambiato;
        }

        private void SuTemaCambiato(object sender, EventArgs e)
        {
            BackColor = TemaBibbia.Sfondo;
            RigeneraSfondo();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                TemaBibbia.TemaCambiato -= SuTemaCambiato;
                timerAnimazione.Stop();
                timerAnimazione.Dispose();
                sfondoStelle?.Dispose();
                tooltipAnteprima.Dispose();
            }
            base.Dispose(disposing);
        }

        public void ImpostaGrafo(List<NodoVisuale> nuoviNodi, List<(NodoVisuale Da, NodoVisuale A)> nuoviArchi, NodoVisuale centro)
        {
            ImpostaGrafo(nuoviNodi, nuoviArchi, centro != null ? new[] { centro } : Enumerable.Empty<NodoVisuale>());
        }

        public void ImpostaGrafo(List<NodoVisuale> nuoviNodi, List<(NodoVisuale Da, NodoVisuale A)> nuoviArchi, IEnumerable<NodoVisuale> centrali)
        {
            NascondiAnteprima();
            nodi = nuoviNodi ?? new List<NodoVisuale>();
            archi = nuoviArchi ?? new List<(NodoVisuale, NodoVisuale)>();
            nodiCentrali = new HashSet<NodoVisuale>(centrali ?? Enumerable.Empty<NodoVisuale>());
            nodoCentrale = nodiCentrali.Count == 1 ? nodiCentrali.First() : null;
            zoom = 1f;
            pan = PointF.Empty;
            Invalidate();
        }

        public void ImpostaZoomRelativo(float fattoreMoltiplicativo)
        {
            zoom = Math.Clamp(zoom * fattoreMoltiplicativo, 0.3f, 3f);
            Invalidate();
        }

        public void Ripristina()
        {
            zoom = 1f;
            pan = PointF.Empty;
            Invalidate();
        }

        public void AdattaAllaFinestra()
        {
            if (nodi.Count == 0 || nodiCentrali.Count == 0)
            {
                Ripristina();
                return;
            }

            float centroX = nodiCentrali.Average(n => n.PosizioneMondo.X);
            float centroY = nodiCentrali.Average(n => n.PosizioneMondo.Y);

            float estensioneX = nodi.Max(n => Math.Abs(n.PosizioneMondo.X - centroX));
            float estensioneY = nodi.Max(n => Math.Abs(n.PosizioneMondo.Y - centroY));

            const float margine = RaggioNodo * 2.5f;
            float mezzaLarghezzaContenuto = estensioneX + margine;
            float mezzaAltezzaContenuto = estensioneY + margine;

            const float bordoFinestra = 30f;
            float mezzaLarghezzaDisponibile = Math.Max(1f, (Width - bordoFinestra * 2) / 2f);
            float mezzaAltezzaDisponibile = Math.Max(1f, (Height - bordoFinestra * 2) / 2f);

            float zoomPerLarghezza = mezzaLarghezzaContenuto > 0.01f ? mezzaLarghezzaDisponibile / mezzaLarghezzaContenuto : 3f;
            float zoomPerAltezza = mezzaAltezzaContenuto > 0.01f ? mezzaAltezzaDisponibile / mezzaAltezzaContenuto : 3f;

            zoom = Math.Clamp(Math.Min(zoomPerLarghezza, zoomPerAltezza), 0.15f, 3f);
            pan = new PointF(-centroX * zoom, -centroY * zoom);

            Invalidate();
        }

        private PointF DaMondoASchermo(PointF puntoMondo)
        {
            float cx = Width / 2f + pan.X;
            float cy = Height / 2f + pan.Y;
            return new PointF(cx + puntoMondo.X * zoom, cy + puntoMondo.Y * zoom);
        }

        private void RigeneraSfondo()
        {
            sfondoStelle?.Dispose();
            sfondoStelle = null;

            if (Width <= 0 || Height <= 0)
                return;

            var bmp = new Bitmap(Width, Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (var sfumatura = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, Width, Height),
                    TemaBibbia.FondoCampo,
                    TemaBibbia.Chiaro
                        ? TemaBibbia.Mescola(TemaBibbia.Sfondo, TemaBibbia.Oro, 0.22f)
                        : TemaBibbia.Mescola(TemaBibbia.Sfondo, Color.Black, 0.45f),
                    System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal))
                {
                    g.FillRectangle(sfumatura, 0, 0, Width, Height);
                }

                var rnd = new Random(20260824);
                int numeroStelle = Math.Max(30, (Width * Height) / 3800);
                for (int i = 0; i < numeroStelle; i++)
                {
                    int x = rnd.Next(Width);
                    int y = rnd.Next(Height);
                    int dimensione = rnd.Next(1, 3);
                    int alpha = TemaBibbia.Chiaro ? rnd.Next(20, 80) : rnd.Next(35, 150);
                    using var pennello = new SolidBrush(TemaBibbia.Chiaro
                        ? Color.FromArgb(alpha, TemaBibbia.Oro)
                        : Color.FromArgb(alpha, 255, 255, 255));
                    g.FillEllipse(pennello, x, y, dimensione, dimensione);
                }
            }

            sfondoStelle = bmp;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            if (sfondoStelle != null)
                g.DrawImageUnscaled(sfondoStelle, 0, 0);
            else
                g.Clear(TemaBibbia.Sfondo);

            if (nodi.Count == 0)
            {
                using var fontVuoto = new Font("Segoe UI", 12f);
                TextRenderer.DrawText(g, "Nessun riferimento incrociato trovato per questo versetto.",
                    fontVuoto, ClientRectangle, Color.FromArgb(210, TemaBibbia.Avorio),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                return;
            }

            foreach (var arco in archi)
            {
                var p1 = DaMondoASchermo(arco.Da.PosizioneMondo);
                var p2 = DaMondoASchermo(arco.A.PosizioneMondo);
                bool coinvolgeCentro = nodiCentrali.Contains(arco.Da) || nodiCentrali.Contains(arco.A);
                DisegnaSinapsi(g, p1, p2, coinvolgeCentro);
            }

            float dimensioneFontNormale = Math.Max(7f, 9.5f * zoom);
            float dimensioneFontCentrale = Math.Max(8f, 11.5f * zoom);
            using var fontNodo = new Font("Segoe UI", dimensioneFontNormale, FontStyle.Regular);
            using var fontNodoCentrale = new Font("Segoe UI", dimensioneFontCentrale, FontStyle.Bold);

            foreach (var nodo in nodi)
            {
                bool ePrincipale = nodiCentrali.Contains(nodo);
                var centro = DaMondoASchermo(nodo.PosizioneMondo);

                float impulso = ePrincipale ? 1f + 0.14f * (float)Math.Sin(faseAnimazione) : 1f;
                float raggio = (ePrincipale ? RaggioNodo * 1.35f : RaggioNodo) * zoom * impulso;

                Color coloreNeurone = ePrincipale
                    ? ColoreOro
                    : nodo.HaNota
                        ? ColoreAvorio
                        : ColoreBluMariano;

                DisegnaNeurone(g, centro, raggio, coloreNeurone, ePrincipale);

                var fontEtichetta = ePrincipale ? fontNodoCentrale : fontNodo;
                var misura = g.MeasureString(nodo.Etichetta, fontEtichetta);
                var puntoTesto = new PointF(centro.X - misura.Width / 2, centro.Y + raggio + 4);

                using (var ombra = new SolidBrush(Color.FromArgb(190, TemaBibbia.Chiaro ? TemaBibbia.Sfondo : Color.Black)))
                    g.DrawString(nodo.Etichetta, fontEtichetta, ombra, puntoTesto.X + 1, puntoTesto.Y + 1);

                Color coloreTesto = ePrincipale ? TemaBibbia.OroChiaro : Color.FromArgb(235, TemaBibbia.Avorio);
                using (var pennelloTesto = new SolidBrush(coloreTesto))
                    g.DrawString(nodo.Etichetta, fontEtichetta, pennelloTesto, puntoTesto);

                nodo.RettangoloSchermo = new RectangleF(centro.X - raggio, centro.Y - raggio, raggio * 2, raggio * 2);
            }
        }

        private void DisegnaSinapsi(Graphics g, PointF p1, PointF p2, bool evidenziata)
        {
            float dx = p2.X - p1.X, dy = p2.Y - p1.Y;
            float lunghezza = (float)Math.Sqrt(dx * dx + dy * dy);
            if (lunghezza < 1f)
                return;

            float px = -dy / lunghezza, py = dx / lunghezza;
            float curvatura = Math.Min(45f, lunghezza * 0.15f);
            var mid = new PointF((p1.X + p2.X) / 2 + px * curvatura, (p1.Y + p2.Y) / 2 + py * curvatura);

            using var percorso = new System.Drawing.Drawing2D.GraphicsPath();
            percorso.AddBezier(
                p1,
                new PointF(p1.X + (mid.X - p1.X) * 0.5f, p1.Y + (mid.Y - p1.Y) * 0.5f),
                new PointF(p2.X + (mid.X - p2.X) * 0.5f, p2.Y + (mid.Y - p2.Y) * 0.5f),
                p2);

            Color coloreBase = evidenziata ? ColoreOro : ColoreBluMariano;

            using (var pennaGlow = new Pen(Color.FromArgb(evidenziata ? 55 : 32, coloreBase), evidenziata ? 5f : 3.5f))
                g.DrawPath(pennaGlow, percorso);

            using (var pennaLinea = new Pen(Color.FromArgb(evidenziata ? 215 : 140, coloreBase), evidenziata ? 1.8f : 1.1f))
                g.DrawPath(pennaLinea, percorso);
        }

        private void DisegnaNeurone(Graphics g, PointF centro, float raggio, Color colore, bool ePrincipale)
        {
            
            for (int i = 3; i >= 1; i--)
            {
                float raggioGlow = raggio * (1f + i * 0.55f);
                int alpha = (ePrincipale ? 28 : 16) / i;
                using var pennelloGlow = new SolidBrush(Color.FromArgb(alpha, colore));
                g.FillEllipse(pennelloGlow, centro.X - raggioGlow, centro.Y - raggioGlow, raggioGlow * 2, raggioGlow * 2);
            }

            var rettangoloNucleo = new RectangleF(centro.X - raggio, centro.Y - raggio, raggio * 2, raggio * 2);

            using (var percorsoNucleo = new System.Drawing.Drawing2D.GraphicsPath())
            {
                percorsoNucleo.AddEllipse(rettangoloNucleo);
                using var sfumaturaNucleo = new System.Drawing.Drawing2D.PathGradientBrush(percorsoNucleo);
                sfumaturaNucleo.CenterColor = ControlPaint.Light(colore, 0.4f);
                sfumaturaNucleo.SurroundColors = new[] { ControlPaint.Dark(colore, 0.05f) };
                g.FillPath(sfumaturaNucleo, percorsoNucleo);
            }

            float raggioLuce = raggio * 0.75f;
            var rettangoloLuce = new RectangleF(
                centro.X - raggio * 0.55f, centro.Y - raggio * 0.65f, raggioLuce, raggioLuce);
            using (var pennelloLuce = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                g.FillEllipse(pennelloLuce, rettangoloLuce);

            using var pennaBordo = new Pen(Color.FromArgb(230, 255, 255, 255), ePrincipale ? 1.8f : 1f);
            g.DrawEllipse(pennaBordo, rettangoloNucleo);
        }

        private void PannelloMappaConcettuale_MouseWheel(object sender, MouseEventArgs e)
        {
            float fattore = e.Delta > 0 ? 1.15f : 1f / 1.15f;
            ImpostaZoomRelativo(fattore);
        }

        private void PannelloMappaConcettuale_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            puntoTrascinamento = e.Location;
            trascinamentoInCorso = false;
        }

        private void PannelloMappaConcettuale_MouseMove(object sender, MouseEventArgs e)
        {
            if (puntoTrascinamento.HasValue && e.Button == MouseButtons.Left)
            {
                int dx = e.X - puntoTrascinamento.Value.X;
                int dy = e.Y - puntoTrascinamento.Value.Y;

                if (!trascinamentoInCorso && (Math.Abs(dx) > 3 || Math.Abs(dy) > 3))
                {
                    trascinamentoInCorso = true;
                    NascondiAnteprima();
                }

                if (trascinamentoInCorso)
                {
                    pan = new PointF(pan.X + dx, pan.Y + dy);
                    puntoTrascinamento = e.Location;
                    Invalidate();
                }
                return;
            }

            var nodo = nodi.FirstOrDefault(n => n.RettangoloSchermo.Contains(e.Location));
            if (nodo == nodoInHover)
                return;

            nodoInHover = nodo;

            if (nodo != null && !string.IsNullOrEmpty(nodo.Anteprima))
                tooltipAnteprima.Show(nodo.Anteprima, this, e.X + 18, e.Y + 18);
            else
                tooltipAnteprima.Hide(this);
        }

        private void NascondiAnteprima()
        {
            if (nodoInHover == null)
                return;

            nodoInHover = null;
            tooltipAnteprima.Hide(this);
        }

        private void TooltipAnteprima_Popup(object sender, PopupEventArgs e)
        {
            using var g = CreateGraphics();
            var dimensione = MisuraTestoMultilinea(g, nodoInHover?.Anteprima ?? "", FontAnteprima);
            e.ToolTipSize = new Size(
                (int)Math.Ceiling(dimensione.Width) + 24,
                (int)Math.Ceiling(dimensione.Height) + 18);
        }

        private void TooltipAnteprima_Draw(object sender, DrawToolTipEventArgs e)
        {
            using (var sfondo = new SolidBrush(Color.FromArgb(250, TemaBibbia.FondoBottone)))
                e.Graphics.FillRectangle(sfondo, e.Bounds);
            using (var bordo = new Pen(ColoreOro, 1.4f))
                e.Graphics.DrawRectangle(bordo, 0, 0, e.Bounds.Width - 1, e.Bounds.Height - 1);

            var area = Rectangle.Inflate(e.Bounds, -12, -9);
            using var formato = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
            using var pennelloTesto = new SolidBrush(TemaBibbia.Avorio);
            e.Graphics.DrawString(e.ToolTipText, FontAnteprima, pennelloTesto, area, formato);
        }

        private static SizeF MisuraTestoMultilinea(Graphics g, string testo, Font font)
        {
            var totale = SizeF.Empty;
            foreach (var riga in (testo ?? "").Split('\n'))
            {
                var misura = g.MeasureString(riga, font);
                totale.Width = Math.Max(totale.Width, misura.Width);
                totale.Height += font.GetHeight(g);
            }
            return totale;
        }

        private void PannelloMappaConcettuale_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !trascinamentoInCorso)
            {
                var nodo = nodi.FirstOrDefault(n => n.RettangoloSchermo.Contains(e.Location));
                if (nodo != null)
                    NodoCliccato?.Invoke(nodo);
            }

            puntoTrascinamento = null;
            trascinamentoInCorso = false;
        }
    }
}
