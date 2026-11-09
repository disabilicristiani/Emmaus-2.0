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
using Microsoft.Data.Sqlite;
using System.Text.RegularExpressions;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace Listbox
{
    public partial class FormMappaConcettuale : Form
    {
        private readonly string connectionStringBibbia;
        private readonly string connectionStringNote;
        private readonly Action<Input.LibroBiblico, int, int?, int?> alSelezionaRiferimento;

        static FormMappaConcettuale()
        {
            Input.RisolutoreFontPdf.Registra();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F1)
            {
                Input.FormGUIDA.Mostra(this, "c7");
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private sealed class NodoMappa
        {
            public Input.LibroBiblico Libro { get; set; }
            public int Capitolo { get; set; }
            public int VersettoDa { get; set; }
            public int VersettoA { get; set; }
            public bool HaNota { get; set; }

            public string Etichetta => VersettoDa == VersettoA
                ? $"{Libro.Abbreviazione} {Capitolo},{VersettoDa}"
                : $"{Libro.Abbreviazione} {Capitolo},{VersettoDa}-{VersettoA}";

            public string EtichettaEstesa => VersettoDa == VersettoA
                ? $"{Libro.Nome} {Capitolo},{VersettoDa}"
                : $"{Libro.Nome} {Capitolo},{VersettoDa}-{VersettoA}";
        }

        public FormMappaConcettuale(Input.LibroBiblico libro, int capitolo, int versettoDa, int? versettoA,
                                     string connStringBibbia, string connStringNote,
                                     Action<Input.LibroBiblico, int, int?, int?> alSelezionaRiferimento)
        {
            InitializeComponent();
            Input.ZoomApp.AdattaFont(label_titoloMappa, button_zoomPiu, button_zoomMeno, button_adatta);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            TemaBibbia.Applica(this);
            panelHeader.BackColor = TemaBibbia.FondoBottone;
            label_titoloMappa.ForeColor = TemaBibbia.Oro;
            pannelloMappa.BackColor = TemaBibbia.Sfondo;

            this.connectionStringBibbia = connStringBibbia;
            this.connectionStringNote = connStringNote;
            this.alSelezionaRiferimento = alSelezionaRiferimento;

            pannelloMappa.NodoCliccato += PannelloMappa_NodoCliccato;
            button_zoomPiu.Click += (s, e) => pannelloMappa.ImpostaZoomRelativo(1.2f);
            button_zoomMeno.Click += (s, e) => pannelloMappa.ImpostaZoomRelativo(1f / 1.2f);
            button_adatta.Click += (s, e) => pannelloMappa.AdattaAllaFinestra();
            pannelloMappa.MouseDown += PannelloMappa_MouseDown;

            this.Load += (s, e) => pannelloMappa.AdattaAllaFinestra();
            this.Resize += (s, e) => pannelloMappa.AdattaAllaFinestra();

            CentraSu(libro, capitolo, versettoDa, versettoA ?? versettoDa);
        }

        public FormMappaConcettuale(List<(Input.LibroBiblico Libro, int Capitolo, int VersettoDa, int? VersettoA)> occorrenze,
                                     string testoRicerca,
                                     string connStringBibbia, string connStringNote,
                                     Action<Input.LibroBiblico, int, int?, int?> alSelezionaRiferimento)
        {
            InitializeComponent();
            Input.ZoomApp.AdattaFont(label_titoloMappa, button_zoomPiu, button_zoomMeno, button_adatta);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            TemaBibbia.Applica(this);
            panelHeader.BackColor = TemaBibbia.FondoBottone;
            label_titoloMappa.ForeColor = TemaBibbia.Oro;
            pannelloMappa.BackColor = TemaBibbia.Sfondo;

            this.connectionStringBibbia = connStringBibbia;
            this.connectionStringNote = connStringNote;
            this.alSelezionaRiferimento = alSelezionaRiferimento;

            pannelloMappa.NodoCliccato += PannelloMappa_NodoCliccato;
            button_zoomPiu.Click += (s, e) => pannelloMappa.ImpostaZoomRelativo(1.2f);
            button_zoomMeno.Click += (s, e) => pannelloMappa.ImpostaZoomRelativo(1f / 1.2f);
            button_adatta.Click += (s, e) => pannelloMappa.AdattaAllaFinestra();
            pannelloMappa.MouseDown += PannelloMappa_MouseDown;

            this.Load += (s, e) => pannelloMappa.AdattaAllaFinestra();
            this.Resize += (s, e) => pannelloMappa.AdattaAllaFinestra();

            CentraSuRicerca(occorrenze, testoRicerca);
        }

        public void CentraSu(Input.LibroBiblico libro, int capitolo, int versettoDa, int? versettoAFinale)
        {
            int versettoA = versettoAFinale ?? versettoDa;

            var nodi = new Dictionary<string, NodoMappa>();
            var archi = new List<(NodoMappa Da, NodoMappa A)>();

            NodoMappa OttieniONuovo(Input.LibroBiblico l, int c, int vd, int va)
            {
                string chiave = $"{l.BookNumber}|{c}|{vd}|{va}";
                if (!nodi.TryGetValue(chiave, out var nodo))
                {
                    nodo = new NodoMappa { Libro = l, Capitolo = c, VersettoDa = vd, VersettoA = va };
                    nodi[chiave] = nodo;
                }
                return nodo;
            }

            using (var conn = new SqliteConnection(connectionStringNote))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Libro, Capitolo, VersettoDa, VersettoA, TestoNota FROM Note;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string nomeLibro = reader.GetString(0);
                    int cap = reader.GetInt32(1);
                    int vDa = reader.GetInt32(2);
                    int vA = reader.GetInt32(3);
                    string testoNota = reader.GetString(4);

                    var libroSorgente = Input.LibriCEI.Elenco.FirstOrDefault(l =>
                        string.Equals(l.Nome, nomeLibro, StringComparison.OrdinalIgnoreCase));
                    if (libroSorgente == null)
                        continue;

                    var nodoSorgente = OttieniONuovo(libroSorgente, cap, vDa, vA);
                    nodoSorgente.HaNota = true;

                    foreach (var rif in RiferimentiBiblici.Trova(testoNota))
                    {
                        var nodoDestinazione = OttieniONuovo(rif.Libro, rif.Capitolo, rif.VersettoDa, rif.VersettoA);
                        archi.Add((nodoSorgente, nodoDestinazione));
                    }
                }
            }

            var nodoCentro = OttieniONuovo(libro, capitolo, versettoDa, versettoA);

            var adiacenza = new Dictionary<NodoMappa, List<NodoMappa>>();
            void AggiungiAdiacenza(NodoMappa a, NodoMappa b)
            {
                if (!adiacenza.TryGetValue(a, out var lista))
                    adiacenza[a] = lista = new List<NodoMappa>();
                if (!lista.Contains(b))
                    lista.Add(b);
            }
            foreach (var (da, a) in archi)
            {
                AggiungiAdiacenza(da, a);
                AggiungiAdiacenza(a, da);
            }

            const int profonditaMassima = 2;
            var distanza = new Dictionary<NodoMappa, int> { [nodoCentro] = 0 };
            var coda = new Queue<NodoMappa>();
            coda.Enqueue(nodoCentro);
            while (coda.Count > 0)
            {
                var attuale = coda.Dequeue();
                if (distanza[attuale] >= profonditaMassima)
                    continue;
                if (!adiacenza.TryGetValue(attuale, out var vicini))
                    continue;
                foreach (var vicino in vicini)
                {
                    if (distanza.ContainsKey(vicino))
                        continue;
                    distanza[vicino] = distanza[attuale] + 1;
                    coda.Enqueue(vicino);
                }
            }

            var nodiVisibili = distanza.Keys.ToList();
            var perAnello = nodiVisibili.GroupBy(n => distanza[n]).OrderBy(g => g.Key).ToList();

            const float raggioBase = 130f;
            var posizioni = new Dictionary<NodoMappa, PointF>();
            foreach (var anello in perAnello)
            {
                if (anello.Key == 0)
                {
                    posizioni[nodoCentro] = PointF.Empty;
                    continue;
                }

                var elenco = anello.ToList();
                float raggio = raggioBase * anello.Key;
                for (int i = 0; i < elenco.Count; i++)
                {
                    double angolo = 2 * Math.PI * i / elenco.Count;
                    posizioni[elenco[i]] = new PointF(
                        (float)(raggio * Math.Cos(angolo)),
                        (float)(raggio * Math.Sin(angolo)));
                }
            }

            var mappaVisuali = new Dictionary<NodoMappa, PannelloMappaConcettuale.NodoVisuale>();
            var nodiVisuali = new List<PannelloMappaConcettuale.NodoVisuale>();
            foreach (var nodo in nodiVisibili)
            {
                var visuale = new PannelloMappaConcettuale.NodoVisuale
                {
                    Etichetta = nodo.Etichetta,
                    HaNota = nodo.HaNota,
                    Tag = nodo,
                    PosizioneMondo = posizioni[nodo],
                    Anteprima = OttieniAnteprimaVersetto(nodo)
                };
                mappaVisuali[nodo] = visuale;
                nodiVisuali.Add(visuale);
            }

            var archiVisuali = new List<(PannelloMappaConcettuale.NodoVisuale, PannelloMappaConcettuale.NodoVisuale)>();
            foreach (var (da, a) in archi)
            {
                if (mappaVisuali.TryGetValue(da, out var vDa2) && mappaVisuali.TryGetValue(a, out var vA2))
                    archiVisuali.Add((vDa2, vA2));
            }

            pannelloMappa.ImpostaGrafo(nodiVisuali, archiVisuali, mappaVisuali[nodoCentro]);

            label_titoloMappa.Text = $"Mappa dei riferimenti incrociati — {nodoCentro.EtichettaEstesa}";
            this.Text = $"Mappa concettuale — {nodoCentro.EtichettaEstesa}";

            pannelloMappa.AdattaAllaFinestra();
        }

        public void CentraSuRicerca(List<(Input.LibroBiblico Libro, int Capitolo, int VersettoDa, int? VersettoA)> occorrenze, string testoRicerca)
        {
            var nodi = new Dictionary<string, NodoMappa>();
            var archi = new List<(NodoMappa Da, NodoMappa A)>();

            NodoMappa OttieniONuovo(Input.LibroBiblico l, int c, int vd, int va)
            {
                string chiave = $"{l.BookNumber}|{c}|{vd}|{va}";
                if (!nodi.TryGetValue(chiave, out var nodo))
                {
                    nodo = new NodoMappa { Libro = l, Capitolo = c, VersettoDa = vd, VersettoA = va };
                    nodi[chiave] = nodo;
                }
                return nodo;
            }

            using (var conn = new SqliteConnection(connectionStringNote))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Libro, Capitolo, VersettoDa, VersettoA, TestoNota FROM Note;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string nomeLibro = reader.GetString(0);
                    int cap = reader.GetInt32(1);
                    int vDa = reader.GetInt32(2);
                    int vA = reader.GetInt32(3);
                    string testoNota = reader.GetString(4);

                    var libroSorgente = Input.LibriCEI.Elenco.FirstOrDefault(l =>
                        string.Equals(l.Nome, nomeLibro, StringComparison.OrdinalIgnoreCase));
                    if (libroSorgente == null)
                        continue;

                    var nodoSorgente = OttieniONuovo(libroSorgente, cap, vDa, vA);
                    nodoSorgente.HaNota = true;

                    foreach (var rif in RiferimentiBiblici.Trova(testoNota))
                    {
                        var nodoDestinazione = OttieniONuovo(rif.Libro, rif.Capitolo, rif.VersettoDa, rif.VersettoA);
                        archi.Add((nodoSorgente, nodoDestinazione));
                    }
                }
            }

            var adiacenza = new Dictionary<NodoMappa, List<NodoMappa>>();
            void AggiungiAdiacenza(NodoMappa a, NodoMappa b)
            {
                if (!adiacenza.TryGetValue(a, out var lista))
                    adiacenza[a] = lista = new List<NodoMappa>();
                if (!lista.Contains(b))
                    lista.Add(b);
            }
            foreach (var (da, a) in archi)
            {
                AggiungiAdiacenza(da, a);
                AggiungiAdiacenza(a, da);
            }

            var nodiFocus = new List<NodoMappa>();
            foreach (var occ in occorrenze)
            {
                int vA = occ.VersettoA ?? occ.VersettoDa;
                var nodo = OttieniONuovo(occ.Libro, occ.Capitolo, occ.VersettoDa, vA);
                if (!nodiFocus.Contains(nodo))
                    nodiFocus.Add(nodo);
            }

            const int profonditaMassima = 1;
            var distanza = new Dictionary<NodoMappa, int>();
            var coda = new Queue<NodoMappa>();
            foreach (var nf in nodiFocus)
            {
                if (!distanza.ContainsKey(nf))
                {
                    distanza[nf] = 0;
                    coda.Enqueue(nf);
                }
            }
            while (coda.Count > 0)
            {
                var attuale = coda.Dequeue();
                if (distanza[attuale] >= profonditaMassima)
                    continue;
                if (!adiacenza.TryGetValue(attuale, out var vicini))
                    continue;
                foreach (var vicino in vicini)
                {
                    if (distanza.ContainsKey(vicino))
                        continue;
                    distanza[vicino] = distanza[attuale] + 1;
                    coda.Enqueue(vicino);
                }
            }

            var nodiVisibili = distanza.Keys.ToList();
            var perAnello = nodiVisibili.GroupBy(n => distanza[n]).OrderBy(g => g.Key).ToList();

            const float raggioBase = 130f;
            var posizioni = new Dictionary<NodoMappa, PointF>();
            foreach (var anello in perAnello)
            {
                var elenco = anello.ToList();

                if (anello.Key == 0 && elenco.Count == 1)
                {
                    posizioni[elenco[0]] = PointF.Empty;
                    continue;
                }

                float raggio = anello.Key == 0 ? raggioBase * 0.75f : raggioBase * (anello.Key + 1);
                for (int i = 0; i < elenco.Count; i++)
                {
                    double angolo = 2 * Math.PI * i / elenco.Count;
                    posizioni[elenco[i]] = new PointF(
                        (float)(raggio * Math.Cos(angolo)),
                        (float)(raggio * Math.Sin(angolo)));
                }
            }

            var mappaVisuali = new Dictionary<NodoMappa, PannelloMappaConcettuale.NodoVisuale>();
            var nodiVisuali = new List<PannelloMappaConcettuale.NodoVisuale>();
            foreach (var nodo in nodiVisibili)
            {
                var visuale = new PannelloMappaConcettuale.NodoVisuale
                {
                    Etichetta = nodo.Etichetta,
                    HaNota = nodo.HaNota,
                    Tag = nodo,
                    PosizioneMondo = posizioni[nodo],
                    Anteprima = OttieniAnteprimaVersetto(nodo)
                };
                mappaVisuali[nodo] = visuale;
                nodiVisuali.Add(visuale);
            }

            var archiVisuali = new List<(PannelloMappaConcettuale.NodoVisuale, PannelloMappaConcettuale.NodoVisuale)>();
            foreach (var (da, a) in archi)
            {
                if (mappaVisuali.TryGetValue(da, out var vDa2) && mappaVisuali.TryGetValue(a, out var vA2))
                    archiVisuali.Add((vDa2, vA2));
            }

            var focusVisuali = nodiFocus.Select(nf => mappaVisuali[nf]);
            pannelloMappa.ImpostaGrafo(nodiVisuali, archiVisuali, focusVisuali);

            label_titoloMappa.Text = $"Mappa dei riferimenti — ricerca \"{testoRicerca}\" ({nodiFocus.Count} risultati)";
            this.Text = $"Mappa concettuale — ricerca \"{testoRicerca}\"";

            pannelloMappa.AdattaAllaFinestra();
        }

        private string OttieniAnteprimaVersetto(NodoMappa nodo)
        {
            var testi = new List<string>();
            using (var conn = new SqliteConnection(connectionStringBibbia))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT verse, text FROM verses
                                     WHERE book_number=@bn AND chapter=@cap AND verse BETWEEN @da AND @a
                                     ORDER BY verse;";
                cmd.Parameters.AddWithValue("@bn", nodo.Libro.BookNumber);
                cmd.Parameters.AddWithValue("@cap", nodo.Capitolo);
                cmd.Parameters.AddWithValue("@da", nodo.VersettoDa);
                cmd.Parameters.AddWithValue("@a", nodo.VersettoA);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int v = reader.GetInt32(0);
                    string testo = Regex.Replace(reader.GetString(1), "<[^>]+>", "");
                    testi.Add($"{v}. {testo}");
                }
            }

            string corpo = testi.Count > 0 ? string.Join(" ", testi) : "(testo non disponibile)";
            if (corpo.Length > 260)
                corpo = corpo.Substring(0, 260).TrimEnd() + "…";

            return AvvolgiTesto($"{nodo.EtichettaEstesa}\n{corpo}", 32);
        }

        private static string AvvolgiTesto(string testo, int larghezzaColonna)
        {
            var righeFinali = new List<string>();
            foreach (var rigaOriginale in testo.Split('\n'))
            {
                var parole = rigaOriginale.Split(' ');
                var rigaCorrente = "";
                foreach (var parola in parole)
                {
                    string prova = rigaCorrente.Length == 0 ? parola : rigaCorrente + " " + parola;
                    if (prova.Length > larghezzaColonna && rigaCorrente.Length > 0)
                    {
                        righeFinali.Add(rigaCorrente);
                        rigaCorrente = parola;
                    }
                    else
                    {
                        rigaCorrente = prova;
                    }
                }
                righeFinali.Add(rigaCorrente);
            }
            return string.Join("\n", righeFinali);
        }

        private void PannelloMappa_NodoCliccato(PannelloMappaConcettuale.NodoVisuale nodoVisuale)
        {
            if (nodoVisuale.Tag is not NodoMappa nodo)
                return;

            alSelezionaRiferimento?.Invoke(nodo.Libro, nodo.Capitolo, nodo.VersettoDa, nodo.VersettoA);
        }

        private void PannelloMappa_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var menu = new ContextMenuStrip();
            TemaBibbia.Applica(menu);
            var esporta = new ToolStripMenuItem("Esporta in PDF");
            esporta.Click += (s, args) => EsportaMappaInPdf();
            menu.Items.Add(esporta);

            var cattura = new ToolStripMenuItem("Cattura");
            cattura.Click += (s, args) => CatturaMappaNegliAppunti();
            menu.Items.Add(cattura);

            menu.Show(pannelloMappa, e.Location);
        }

        private void CatturaMappaNegliAppunti()
        {
            try
            {
                if (pannelloMappa.Width <= 0 || pannelloMappa.Height <= 0)
                    return;

                using var bitmap = new Bitmap(pannelloMappa.Width, pannelloMappa.Height);
                pannelloMappa.DrawToBitmap(bitmap, new Rectangle(Point.Empty, pannelloMappa.Size));
                Clipboard.SetImage(bitmap);
            }
            catch (Exception ex)
            {
                MessaggioBibbia.Mostra($"Impossibile catturare la mappa: {ex.Message}", "Errore cattura", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EsportaMappaInPdf()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Esporta mappa in PDF",
                Filter = "File PDF (*.pdf)|*.pdf",
                FileName = "MappaConcettuale.pdf",
                AddExtension = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                GeneraPdfMappa(dialog.FileName);
                MessaggioBibbia.Mostra("PDF creato correttamente.", "Esporta in PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessaggioBibbia.Mostra($"Impossibile creare il PDF: {ex.Message}", "Errore PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GeneraPdfMappa(string percorsoFile)
        {
            var nodiMappa = pannelloMappa.NodiCorrenti;
            var archiMappa = pannelloMappa.ArchiCorrenti;
            var nodiCentraliMappa = pannelloMappa.NodiCentraliCorrenti;

            using var documento = new PdfDocument();
            documento.Info.Title = this.Text;

            var pagina = documento.AddPage();
            pagina.Size = PdfSharp.PageSize.A4;
            pagina.Orientation = PdfSharp.PageOrientation.Landscape;

            bool mappaVuota;
            using (var grafica = XGraphics.FromPdfPage(pagina))
            {
                grafica.SmoothingMode = XSmoothingMode.AntiAlias;

                const double margine = 36;
                var fontTitolo = new XFont(Input.RisolutoreFontPdf.FamigliaMit, 13, XFontStyleEx.Bold);
                var fontEtichetta = new XFont(Input.RisolutoreFontPdf.FamigliaMit, 8, XFontStyleEx.Regular);
                var fontEtichettaPrincipale = new XFont(Input.RisolutoreFontPdf.FamigliaMit, 8, XFontStyleEx.Bold);

                double yCursore = DisegnaIntestazione(grafica, pagina.Width, margine);

                grafica.DrawString(label_titoloMappa.Text, fontTitolo, XBrushes.Black,
                    new XRect(margine, yCursore, pagina.Width - margine * 2, 24), XStringFormats.TopLeft);

                double yArea = yCursore + 34;
                double areaLarghezza = pagina.Width - margine * 2;
                double areaAltezza = pagina.Height - yArea - margine - 8;

                mappaVuota = nodiMappa == null || nodiMappa.Count == 0;

                if (mappaVuota)
                {
                    grafica.DrawString("Nessun riferimento incrociato da esportare.", fontEtichetta, XBrushes.Black,
                        new XRect(margine, yArea, areaLarghezza, areaAltezza), XStringFormats.TopLeft);
                }
                else
                {
                    
                    const float raggioNodo = 5.5f;
                    const float paddingMondo = 70f;

                    float minX = nodiMappa.Min(n => n.PosizioneMondo.X) - paddingMondo;
                    float maxX = nodiMappa.Max(n => n.PosizioneMondo.X) + paddingMondo;
                    float minY = nodiMappa.Min(n => n.PosizioneMondo.Y) - paddingMondo;
                    float maxY = nodiMappa.Max(n => n.PosizioneMondo.Y) + paddingMondo;

                    float larghezzaMondo = Math.Max(1f, maxX - minX);
                    float altezzaMondo = Math.Max(1f, maxY - minY);

                    double scala = Math.Min(areaLarghezza / larghezzaMondo, areaAltezza / altezzaMondo);
                    scala = Math.Min(scala, 2.2);

                    double centroMondoX = (minX + maxX) / 2.0;
                    double centroMondoY = (minY + maxY) / 2.0;
                    double centroPaginaX = margine + areaLarghezza / 2.0;
                    double centroPaginaY = yArea + areaAltezza / 2.0;

                    XPoint Trasforma(PointF puntoMondo)
                    {
                        double x = centroPaginaX + (puntoMondo.X - centroMondoX) * scala;
                        double y = centroPaginaY + (puntoMondo.Y - centroMondoY) * scala;
                        return new XPoint(x, y);
                    }

                    var pennaArco = new XPen(XColors.Black, 0.5);
                    foreach (var (da, a) in archiMappa)
                        grafica.DrawLine(pennaArco, Trasforma(da.PosizioneMondo), Trasforma(a.PosizioneMondo));

                    var pennaNodo = new XPen(XColors.Black, 1.0);
                    var pennaNodoPrincipale = new XPen(XColors.Black, 1.8);

                    foreach (var nodo in nodiMappa)
                    {
                        var centro = Trasforma(nodo.PosizioneMondo);
                        bool principale = nodiCentraliMappa.Contains(nodo);
                        double raggio = Math.Max(2.0, (principale ? raggioNodo * 1.5 : raggioNodo) * scala);

                        grafica.DrawEllipse(principale ? pennaNodoPrincipale : pennaNodo,
                            centro.X - raggio, centro.Y - raggio, raggio * 2, raggio * 2);

                        var fontUsato = principale ? fontEtichettaPrincipale : fontEtichetta;
                        var dimensioneTesto = grafica.MeasureString(nodo.Etichetta, fontUsato);
                        var puntoTesto = new XPoint(centro.X - dimensioneTesto.Width / 2, centro.Y + raggio + dimensioneTesto.Height);
                        grafica.DrawString(nodo.Etichetta, fontUsato, XBrushes.Black, puntoTesto);
                    }
                }
            }

            if (!mappaVuota)
                AggiungiPagineNote(documento, nodiMappa);

            DisegnaPiedePagina(documento);

            documento.Save(percorsoFile);
        }

        private static double DisegnaIntestazione(XGraphics grafica, double larghezzaPagina, double margine)
        {
            var fontTitolo = new XFont(Input.RisolutoreFontPdf.FamigliaMit, 22, XFontStyleEx.Bold);
            var fontTesto = new XFont(Input.RisolutoreFontPdf.FamigliaMit, 10, XFontStyleEx.Regular);
            var fontCorsivo = new XFont(Input.RisolutoreFontPdf.FamigliaMit, 10, XFontStyleEx.Italic);
            double larghezza = larghezzaPagina - margine * 2;
            double y = margine;

            grafica.DrawString("Emmaus 2.0", fontTitolo, XBrushes.Black,
                new XRect(margine, y, larghezza, 30), XStringFormats.TopLeft);
            y += 32;

            grafica.DrawString("Emmaus 2.0 è un programma per Windows per meditare la Sacra Scrittura.",
                fontTesto, XBrushes.Black, new XRect(margine, y, larghezza, 16), XStringFormats.TopLeft);
            y += 16;

            grafica.DrawString("Copyright © 2026 DisabiliCristiani disabilicristiani@gmail.com - Fate del bene e pregate",
                fontCorsivo, XBrushes.Black, new XRect(margine, y, larghezza, 16), XStringFormats.TopLeft);
            y += 16;

            grafica.DrawString("Codice sorgente sotto licenza MIT.",
                fontCorsivo, XBrushes.Black, new XRect(margine, y, larghezza, 16), XStringFormats.TopLeft);
            y += 20;

            grafica.DrawLine(new XPen(XColors.Gray, 1.0), margine, y, margine + larghezza, y);
            return y + 12;
        }

        private static void DisegnaPiedePagina(PdfDocument documento)
        {
            var font = new XFont(Input.RisolutoreFontPdf.FamigliaMit, 8, XFontStyleEx.Regular);
            const double margine = 40;
            int totale = documento.PageCount;

            for (int i = 0; i < totale; i++)
            {
                var pagina = documento.Pages[i];
                using var grafica = XGraphics.FromPdfPage(pagina);
                double larghezza = pagina.Width - margine * 2;
                double yLinea = pagina.Height - 32;

                grafica.DrawLine(new XPen(XColors.Gray, 0.5), margine, yLinea, margine + larghezza, yLinea);

                grafica.DrawString("Emmaus 2.0 - Copyright © 2026 DisabiliCristiani", font, XBrushes.Gray,
                    new XRect(margine, yLinea + 4, larghezza, 12), XStringFormats.TopLeft);
                grafica.DrawString($"Pagina {i + 1} di {totale}", font, XBrushes.Gray,
                    new XRect(margine, yLinea + 4, larghezza, 12), XStringFormats.TopRight);
            }
        }

        private void AggiungiPagineNote(PdfDocument documento, IReadOnlyList<PannelloMappaConcettuale.NodoVisuale> nodiMappa)
        {
            var nodiOrdinati = nodiMappa
                .Select(nv => nv.Tag as NodoMappa)
                .Where(n => n != null)
                .OrderBy(n => n.Libro.BookNumber)
                .ThenBy(n => n.Capitolo)
                .ThenBy(n => n.VersettoDa)
                .ToList();

            var note = new List<(string Riferimento, string Testo)>();
            var giaInserite = new HashSet<string>();
            foreach (var nodo in nodiOrdinati)
            {
                foreach (var nota in OttieniNotePerNodo(nodo))
                {
                    if (giaInserite.Add(nota.Riferimento + "|" + nota.Testo))
                        note.Add(nota);
                }
            }

            if (note.Count == 0)
                return;

            const double margine = 40;
            var fontTitolo = new XFont(Input.RisolutoreFontPdf.FamigliaMit, 12, XFontStyleEx.Bold);
            var fontNota = new XFont(Input.RisolutoreFontPdf.FamigliaMit, 10, XFontStyleEx.Regular);

            PdfPage paginaTesto = null;
            XGraphics graficaTesto = null;
            double y = 0;
            double areaLarghezza = 0;
            double limiteInferiore = 0;

            void NuovaPagina()
            {
                graficaTesto?.Dispose();
                paginaTesto = documento.AddPage();
                paginaTesto.Size = PdfSharp.PageSize.A4;
                paginaTesto.Orientation = PdfSharp.PageOrientation.Portrait;
                graficaTesto = XGraphics.FromPdfPage(paginaTesto);
                graficaTesto.SmoothingMode = XSmoothingMode.AntiAlias;
                areaLarghezza = paginaTesto.Width - margine * 2;
                limiteInferiore = paginaTesto.Height - margine - 10;
                y = margine;
            }

            void AssicuraSpazio(double altezzaNecessaria)
            {
                if (y + altezzaNecessaria > limiteInferiore)
                    NuovaPagina();
            }

            List<string> AvvolgiPerLarghezza(string testo, XFont font, double larghezzaDisponibile)
            {
                var righe = new List<string>();
                foreach (var paragrafo in (testo ?? "").Split('\n'))
                {
                    var parole = paragrafo.Split(' ');
                    string rigaCorrente = "";
                    foreach (var parola in parole)
                    {
                        string prova = rigaCorrente.Length == 0 ? parola : rigaCorrente + " " + parola;
                        if (graficaTesto.MeasureString(prova, font).Width > larghezzaDisponibile && rigaCorrente.Length > 0)
                        {
                            righe.Add(rigaCorrente);
                            rigaCorrente = parola;
                        }
                        else
                        {
                            rigaCorrente = prova;
                        }
                    }
                    righe.Add(rigaCorrente);
                }
                return righe;
            }

            NuovaPagina();

            double altezzaTitolo = graficaTesto.MeasureString("Note", fontTitolo).Height;
            graficaTesto.DrawString("Note", fontTitolo, XBrushes.Black,
                new XRect(margine, y, areaLarghezza, altezzaTitolo + 4), XStringFormats.TopLeft);
            y += altezzaTitolo + 10;

            foreach (var (riferimento, testo) in note)
            {
                foreach (var riga in AvvolgiPerLarghezza($"Nota a {riferimento}: {testo}", fontNota, areaLarghezza))
                {
                    double altezzaRiga = graficaTesto.MeasureString(riga, fontNota).Height + 2;
                    AssicuraSpazio(altezzaRiga);
                    graficaTesto.DrawString(riga, fontNota, XBrushes.Black,
                        new XRect(margine, y, areaLarghezza, altezzaRiga), XStringFormats.TopLeft);
                    y += altezzaRiga;
                }
                y += 8;
            }

            graficaTesto?.Dispose();
        }

        private List<(string Riferimento, string Testo)> OttieniNotePerNodo(NodoMappa nodo)
        {
            var note = new List<(string Riferimento, string Testo)>();
            using (var conn = new SqliteConnection(connectionStringNote))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT Libro, Capitolo, VersettoDa, VersettoA, TestoNota FROM Note
                                     WHERE Libro=@libro AND Capitolo=@cap AND VersettoDa<=@a AND VersettoA>=@da
                                     ORDER BY VersettoDa, VersettoA;";
                cmd.Parameters.AddWithValue("@libro", nodo.Libro.Nome);
                cmd.Parameters.AddWithValue("@cap", nodo.Capitolo);
                cmd.Parameters.AddWithValue("@da", nodo.VersettoDa);
                cmd.Parameters.AddWithValue("@a", nodo.VersettoA);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string libro = reader.GetString(0);
                    int cap = reader.GetInt32(1);
                    int vDa = reader.GetInt32(2);
                    int vA = reader.GetInt32(3);
                    string riferimento = vDa == vA ? $"{libro} {cap},{vDa}" : $"{libro} {cap},{vDa}-{vA}";
                    note.Add((riferimento, reader.GetString(4)));
                }
            }

            return note;
        }
    }
}
