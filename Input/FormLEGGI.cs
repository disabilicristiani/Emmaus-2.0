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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Listbox
{
    public partial class FormLEGGI : Form
    {
        private string connectionStringBibbia = $"Data Source={Path.Combine(AppContext.BaseDirectory, "CEI_08.SQLite3")};Mode=ReadOnly;";
        private string connectionStringNote = $"Data Source={Path.Combine(AppContext.BaseDirectory, "commenti.db")}";
        private string connectionStringVersettiFts = $"Data Source={Path.Combine(AppContext.BaseDirectory, "versetti_fts.db")}";

        private Input.LibroBiblico libro;
        private int capitolo;
        private int? versettoDa;
        private int? versettoA;

        private bool modalitaRicerca = false;
        private string testoRicerca;

        // Modalita' preghiera: la griglia mostra coppie titolo/testo fisse.
        private bool modalitaPreghiera = false;
        private string titoloPreghiera;
        private IReadOnlyList<(string Titolo, string Testo)> sezioniPreghiera;

        private const int DimensionePaginaRicerca = 3;

        private int offsetPaginaRicercaCorrente = 0;
        private bool ricercaHaAltriRisultati = false;
        private int totaleUnitaMostrateRicerca = 0;
        private int totaleUnitaRicercaCorrispondenti = 0;

        private bool aggiornamentoFiltroTestamentoInCorso = false;
        private bool aggiornamentoListaLibriInCorso = false;

        private bool selezioneDefaultLibriRicercaApplicata = false;

        private HashSet<int> libriConOccorrenzeRicercaCorrente =
            new HashSet<int>();

        private HashSet<int> libriConRiferimentoRicercaCorrente =
            new HashSet<int>();

        private int generazioneRicercaCorrente = 0;

        private int larghezzaColonnaLibri = 215;

        private const int ColonneLibriMinimeVisibili = 3;

        private Dictionary<int, int> versettiPerLibroRicerca =
            new Dictionary<int, int>();

        private Dictionary<int, int> notePerLibroRicerca =
            new Dictionary<int, int>();

        private readonly HashSet<(int Libro, int Capitolo, int Da, int A)>
            mappaGiaAggiuntiRicerca =
            new HashSet<(int, int, int, int)>();

        private readonly List<(
            Input.LibroBiblico Libro,
            int Capitolo,
            int VersettoDa,
            int? VersettoA)>
            occorrenzeMappaRicercaAccumulate =
            new List<(Input.LibroBiblico, int, int, int?)>();

        private readonly HashSet<string> chiaviUnitaGiaMostrate =
            new HashSet<string>();

        private Panel contenitoreListaLibriAT;
        private Panel contenitoreListaLibriNT;

        private Panel flowLibriAT;
        private Panel flowLibriNT;

        private CheckBox checkSelezionaTuttoAT;
        private CheckBox checkSelezionaTuttoNT;

        // Le misure in pixel scritte nel codice sono espresse nella stessa
        // scala del Designer (AutoScaleDimensions 15 x 37). Questo fattore
        // le adegua al font/DPI effettivi, come fa WinForms per i controlli.
        private const float AltezzaFontDesigner = 37f;

        private int ScalaDesign(int px)
        {
            float fattore =
                CurrentAutoScaleDimensions.Height / AltezzaFontDesigner;

            if (fattore <= 0f)
                fattore = 1f;

            return (int)Math.Round(px * fattore);
        }

        private int AltezzaBasePanelHeader => ScalaDesign(260);

        private int altezzaCalcolataListaLibri = 300;

        private Action<Input.LibroBiblico, int, int?, int?>
            aggiornaComboBoxInserisci;

        private Action aggiornaColoriInserisci;

        private FormMappaConcettuale formMappaAperta;

        // Stato leggibile dall'esterno: FormINSERISCI lo usa per chiudere
        // questa finestra durante la manutenzione e poi riaprirla con lo
        // stesso riferimento (o la stessa ricerca) e la mappa, se c'era.
        public Input.LibroBiblico LibroCorrente => libro;
        public int CapitoloCorrente => capitolo;
        public int? VersettoDaCorrente => versettoDa;
        public int? VersettoACorrente => versettoA;
        public bool InModalitaRicerca => modalitaRicerca;
        public string TestoRicercaCorrente => testoRicerca;

        public bool MappaConcettualeAperta =>
            formMappaAperta != null &&
            !formMappaAperta.IsDisposed;

        // In ricerca la mappa si puo' costruire solo quando i risultati
        // sono stati caricati: la si apre al primo caricamento.
        private bool riapriMappaDopoRicerca;

        public void RiapriMappaConcettuale()
        {
            if (modalitaRicerca)
            {
                riapriMappaDopoRicerca = true;
                return;
            }

            ApriOAggiornaMappaConcettuale(
                libro,
                capitolo,
                versettoDa,
                versettoA);
        }

        private sealed class NotaInfo
        {
            public long Id { get; set; }
            public string Testo { get; set; }
        }

        private sealed class RigaNotaInfo
        {
            public NotaInfo Nota { get; set; }

            public List<(int Start, int Length)> Intervalli { get; set; }
                = new List<(int Start, int Length)>();

            public List<RiferimentoBiblico> Riferimenti { get; set; }
                = new List<RiferimentoBiblico>();

            // Tag (#parola) presenti nel commento; le posizioni tengono
            // gia' conto dell'icona davanti al testo.
            public List<TagNote.Occorrenza> Tags { get; set; }
                = new List<TagNote.Occorrenza>();
        }

        private sealed class RigaVersettoInfo
        {
            public string Testo { get; set; }

            public List<(int Start, int Length)> Intervalli { get; set; }
                = new List<(int Start, int Length)>();
        }

        private sealed class RigaTitoloInfo
        {
            public Input.LibroBiblico Libro { get; set; }

            public int Capitolo { get; set; }

            public int VersettoDa { get; set; }

            public int? VersettoA { get; set; }
        }

        private sealed class UnitaRisultatoRicerca
        {
            public string ChiaveUnica { get; set; }

            public Input.LibroBiblico Libro { get; set; }

            public int Capitolo { get; set; }

            public int VersettoDa { get; set; }

            public int VersettoA { get; set; }

            public string TestoVersetto { get; set; }

            public List<NotaInfo> Note { get; set; }
                = new List<NotaInfo>();

            public bool TrovatoDaNota { get; set; }
        }

        private sealed class RisultatoPaginaRicerca
        {
            public List<UnitaRisultatoRicerca> UnitaPagina { get; set; }
                = new List<UnitaRisultatoRicerca>();

            public List<(
                Input.LibroBiblico Libro,
                int Capitolo,
                int VersettoDa,
                int? VersettoA)>
                OccorrenzeMappaNuove
            { get; set; }
                = new List<(Input.LibroBiblico, int, int, int?)>();

            public int TotaleUnitaCorrispondenti { get; set; }

            public int TotaleVersettiTrovati { get; set; }

            public int TotaleNoteTrovate { get; set; }

            public Dictionary<int, int> VersettiTrovatiPerLibro { get; set; }
                = new Dictionary<int, int>();

            public Dictionary<int, int> NoteTrovatePerLibro { get; set; }
                = new Dictionary<int, int>();

            public HashSet<int> LibriConOccorrenze { get; set; }
                = new HashSet<int>();

            public HashSet<int> LibriConRiferimento { get; set; }
                = new HashSet<int>();
        }

        private const int OffsetIconaNota = 3;

        private readonly ToolTip tooltipRiferimenti = new ToolTip
        {
            OwnerDraw = true,
            AutoPopDelay = 12000,
            InitialDelay = 300,
            ReshowDelay = 100,
            ShowAlways = true
        };

        private string ultimoTestoTooltip = "";

        private static readonly Font FontTooltipRiferimento =
            new Font(Input.ZoomApp.FamigliaBase, 17f * Input.ZoomApp.Fattore, FontStyle.Regular);

        private int? rigaTooltipCorrente = null;
        private int? offsetTooltipCorrente = null;

        private static readonly PrivateFontCollection ColFontCardo =
            CaricaFontCardo();

        private static PrivateFontCollection CaricaFontCardo()
        {
            var collezione = new PrivateFontCollection();

            string cartellaFont =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Risorse",
                    "Font");

            foreach (var file in new[]
            {
                "Cardo-Regular.ttf",
                "Cardo-Bold.ttf",
                "Cardo-Italic.ttf"
            })
            {
                string percorso =
                    Path.Combine(cartellaFont, file);

                if (File.Exists(percorso))
                    collezione.AddFontFile(percorso);
            }

            return collezione;
        }

        private static FontFamily FamigliaCardo =>
            ColFontCardo.Families.Length > 0
                ? ColFontCardo.Families[0]
                : FontFamily.GenericSerif;

        private static readonly Dictionary<string, Font>
            CacheFontCardo =
            new Dictionary<string, Font>();

        private static readonly Dictionary<float, Font>
            CacheFontTitolo =
            new Dictionary<float, Font>();

        private static readonly Dictionary<float, Font>
            CacheFontBase =
            new Dictionary<float, Font>();

        private static Font CreaFontCardo(
            float dimensione,
            FontStyle stile)
        {
            string chiave =
                dimensione.ToString("0.##") +
                "|" +
                ((int)stile).ToString();

            if (!CacheFontCardo.TryGetValue(
                    chiave,
                    out var font))
            {
                font =
                    new Font(
                        FamigliaCardo,
                        dimensione,
                        stile);

                CacheFontCardo[chiave] =
                    font;
            }

            return font;
        }

        private static Font CreaFontBase(
            float dimensione,
            FontFamily famiglia,
            FontStyle stile)
        {
            if (!CacheFontBase.TryGetValue(
                    dimensione,
                    out var font))
            {
                font =
                    new Font(
                        famiglia,
                        dimensione,
                        stile);

                CacheFontBase[dimensione] =
                    font;
            }

            return font;
        }

        private static Font CreaFontTitolo(
            float dimensione,
            FontFamily famiglia)
        {
            if (!CacheFontTitolo.TryGetValue(
                    dimensione,
                    out var font))
            {
                font =
                    new Font(
                        famiglia,
                        dimensione,
                        FontStyle.Bold);

                CacheFontTitolo[dimensione] =
                    font;
            }

            return font;
        }

        private const float DimensioneFontMinimaGriglia = 9f;
        private const float DimensioneFontMassimaGriglia = 32f;

        private FiltroRotellinaZoom filtroZoomGriglia;

        private RotellinaAccorpata rotellinaGriglia;

        private readonly System.Windows.Forms.Timer
            timerZoomGriglia =
                new System.Windows.Forms.Timer();

        private int passiZoomPendenti;

        private readonly System.Windows.Forms.Timer
            timerRidimensionamentoGriglia =
                new System.Windows.Forms.Timer();

        private sealed class FiltroRotellinaZoom : IMessageFilter
        {
            private const int WM_MOUSEWHEEL = 0x020A;

            private readonly FormLEGGI form;

            public FiltroRotellinaZoom(FormLEGGI form)
            {
                this.form = form;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_MOUSEWHEEL)
                    return false;

                if (!form.dataGridView1.IsHandleCreated ||
                    m.HWnd != form.dataGridView1.Handle)
                    return false;

                if ((Control.ModifierKeys &
                     Keys.Control) == 0)
                    return false;

                long wParam =
                    m.WParam.ToInt64();

                int delta =
                    (short)((wParam >> 16) & 0xFFFF);

                if (delta != 0)
                {
                    form.passiZoomPendenti +=
                        delta > 0 ? 1 : -1;

                    form.passiZoomPendenti =
                        Math.Max(
                            -10,
                            Math.Min(
                                10,
                                form.passiZoomPendenti));

                    form.timerZoomGriglia.Stop();
                    form.timerZoomGriglia.Start();
                }

                return true;
            }
        }

        private void ApplicaZoomPendenti()
        {
            int passi =
                passiZoomPendenti;

            passiZoomPendenti = 0;

            if (passi != 0)
                ZoomTestoGriglia(passi);
        }

        private void ZoomTestoGriglia(int direzione)
        {
            float nuovaDimensione =
                dataGridView1.Font.Size + direzione;

            nuovaDimensione =
                Math.Max(
                    DimensioneFontMinimaGriglia,
                    Math.Min(
                        DimensioneFontMassimaGriglia,
                        nuovaDimensione));

            if (Math.Abs(
                nuovaDimensione -
                dataGridView1.Font.Size) < 0.01f)
                return;

            dataGridView1.SuspendLayout();

            try
            {
                dataGridView1.AutoSizeRowsMode =
                    DataGridViewAutoSizeRowsMode.None;

                var fontBase =
                    CreaFontBase(
                        nuovaDimensione,
                        dataGridView1.Font.FontFamily,
                        dataGridView1.Font.Style);

                dataGridView1.Font =
                    fontBase;

                Font fontTitolo =
                    CreaFontTitolo(
                        nuovaDimensione,
                        fontBase.FontFamily);

                Font fontVersetto =
                    CreaFontCardo(
                        nuovaDimensione,
                        FontStyle.Regular);

                Font fontNota =
                    CreaFontCardo(
                        nuovaDimensione,
                        FontStyle.Italic);

                foreach (DataGridViewRow riga in
                    dataGridView1.Rows)
                {
                    var cella =
                        riga.Cells[0];

                    if (riga.Tag is RigaTitoloInfo)
                    {
                        cella.Style.Font =
                            fontTitolo;
                    }
                    else if (riga.Tag is RigaVersettoInfo)
                    {
                        cella.Style.Font =
                            fontVersetto;
                    }
                    else if (riga.Tag is RigaNotaInfo)
                    {
                        cella.Style.Font =
                            fontNota;
                    }
                }

                AggiornaAltezzeRigheGriglia();
            }
            finally
            {
                dataGridView1.ResumeLayout();
                dataGridView1.Invalidate();
            }
        }

        private int
            CalcolaNumeroRigheDisegnate(
                Graphics g,
                string testo,
                Font font,
                Font fontIcona,
                float larghezza)
        {
            if (string.IsNullOrEmpty(testo))
                return 1;

            int numeroRighe = 1;
            float x = 0;

            var parole =
                Regex.Split(
                    testo,
                    "(?<=[ \\n])");

            foreach (var parola in parole)
            {
                if (parola.Length == 0)
                    continue;

                var fontParola =
                    fontIcona != null &&
                    ContieneIconaNota(parola)
                        ? fontIcona
                        : font;

                if (parola == "\n" ||
                    parola.EndsWith("\n"))
                {
                    x = 0;
                    numeroRighe++;
                    continue;
                }

                var misura =
                    g.MeasureString(
                        parola,
                        fontParola,
                        int.MaxValue,
                        FormatoTesto);

                if (x + misura.Width > larghezza &&
                    x > 0)
                {
                    x = 0;
                    numeroRighe++;
                }

                x += misura.Width;
            }

            return Math.Max(1, numeroRighe);
        }

        private int
            CalcolaAltezzaRigaGriglia(
                Graphics g,
                DataGridViewRow riga,
                Font fontIcona,
                int larghezzaCella)
        {
            var cella =
                riga.Cells[0];

            string testo =
                cella.Value as string ??
                "";

            Font font =
                cella.InheritedStyle.Font ??
                dataGridView1.Font;

            if (riga.Tag is RigaVersettoInfo ||
                riga.Tag is RigaNotaInfo)
            {
                int numeroRighe =
                    CalcolaNumeroRigheDisegnate(
                        g,
                        testo,
                        font,
                        fontIcona,
                        Math.Max(
                            1,
                            larghezzaCella - 8));

                int altezza =
                    (int)Math.Ceiling(
                        font.GetHeight(g) *
                        numeroRighe) + 5;

                return Math.Max(
                    dataGridView1.RowTemplate.Height,
                    altezza);
            }

            Size misura =
                TextRenderer.MeasureText(
                    testo,
                    font,
                    new Size(
                        Math.Max(
                            1,
                            larghezzaCella - 8),
                        int.MaxValue),
                    TextFormatFlags.WordBreak |
                    TextFormatFlags.NoPadding);

            return Math.Max(
                dataGridView1.RowTemplate.Height,
                misura.Height + 5);
        }

        private void AggiornaAltezzeRigheGriglia()
        {
            if (!dataGridView1.IsHandleCreated ||
                dataGridView1.Columns.Count == 0 ||
                dataGridView1.Rows.Count == 0)
                return;

            int larghezzaCella =
                dataGridView1.Columns[0].Width;

            if (larghezzaCella <= 0)
                return;

            using Graphics g =
                dataGridView1.CreateGraphics();

            using Font fontIcona =
                new Font(
                    "Segoe UI Emoji",
                    dataGridView1.Font.Size);

            foreach (DataGridViewRow riga in
                dataGridView1.Rows)
            {
                if (riga.IsNewRow)
                    continue;

                riga.Height =
                    CalcolaAltezzaRigaGriglia(
                        g,
                        riga,
                        fontIcona,
                        larghezzaCella);
            }
        }

        static FormLEGGI()
        {
            Input.RisolutoreFontPdf.Registra();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Guida di contesto: preghiere = capitolo 8, ricerca = 6,
            // lettura di un passo = 4.
            if (keyData == Keys.F1)
            {
                Input.FormGUIDA.Mostra(
                    this,
                    modalitaPreghiera ? "c8" : modalitaRicerca ? "c6" : "c4");
                return true;
            }

            if (keyData == (Keys.Control | Keys.M))
            {
                ScorciatoieBibbia.Premi(button_altriRisultatiRicerca);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // I font delle quattro etichette dell'intestazione sono fissi
            // nel Designer (14 pt) mentre le loro posizioni scalano con lo
            // zoom: li si rende proporzionali al font di base (18 pt).
            float rapporto = Font.SizeInPoints / 18f;

            foreach (Label etichetta in new[]
            {
                label_riferimento,
                label_libro,
                label_categoria,
                label_testamento
            })
            {
                if (etichetta == null)
                    continue;

                etichetta.Font =
                    new Font(
                        Font.FontFamily,
                        14f * rapporto,
                        etichetta.Font.Style);
            }
        }

        public FormLEGGI()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            TemaBibbia.Applica(this);
            ImpostaComportamentoGriglia();
        }

        public FormLEGGI(
            Input.LibroBiblico libro,
            int capitolo,
            int? versettoDa,
            int? versettoA,
            string connStringBibbia,
            string connStringNote,
            Action<Input.LibroBiblico, int, int?, int?>
                aggiornaComboBoxInserisci = null,
            Action aggiornaColoriInserisci = null)
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            TemaBibbia.Applica(this);
            ImpostaComportamentoGriglia();

            this.libro = libro;
            this.capitolo = capitolo;
            this.versettoDa = versettoDa;
            this.versettoA = versettoA;

            this.connectionStringBibbia =
                connStringBibbia;

            this.connectionStringNote =
                connStringNote;

            this.modalitaRicerca = false;

            this.aggiornaComboBoxInserisci =
                aggiornaComboBoxInserisci;

            this.aggiornaColoriInserisci =
                aggiornaColoriInserisci;

            this.Load += FormLEGGI_Load;
        }

        public FormLEGGI(
            string testoRicerca,
            string connStringBibbia,
            string connStringNote,
            Action<Input.LibroBiblico, int, int?, int?>
                aggiornaComboBoxInserisci = null,
            Action aggiornaColoriInserisci = null)
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            TemaBibbia.Applica(this);
            ImpostaComportamentoGriglia();

            this.testoRicerca = testoRicerca;

            this.connectionStringBibbia =
                connStringBibbia;

            this.connectionStringNote =
                connStringNote;

            this.modalitaRicerca = true;

            this.aggiornaComboBoxInserisci =
                aggiornaComboBoxInserisci;

            this.aggiornaColoriInserisci =
                aggiornaColoriInserisci;

            this.Load += FormLEGGI_Load;
        }

        // Mostra nella griglia una serie di preghiere (titolo + testo).
        // Il Load e' gia' collegato dal Designer.
        public FormLEGGI(
            string titolo,
            IReadOnlyList<(string Titolo, string Testo)> sezioni)
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            TemaBibbia.Applica(this);
            ImpostaComportamentoGriglia();

            titoloPreghiera = titolo;
            sezioniPreghiera = sezioni;
            modalitaPreghiera = true;
        }

        private void ImpostaComportamentoGriglia()
        {
            dataGridView1.ShowCellToolTips = false;

            // Righe e colonne non si ridimensionano trascinando col mouse
            // (altezze e larghezze le calcola il programma).
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.AllowUserToResizeColumns = false;

            // Copyright CEI: niente Ctrl+C sulle celle selezionate; si
            // copia solo dal menu del tasto destro, un versetto alla volta.
            dataGridView1.ClipboardCopyMode =
                DataGridViewClipboardCopyMode.Disable;

            TemaBibbia.Suggerimento(button_altriRisultatiRicerca, "Mostra altri risultati (Ctrl+M)");

            dataGridView1.CellMouseMove +=
                dataGridView1_CellMouseMove;

            dataGridView1.CellMouseLeave +=
                dataGridView1_CellMouseLeave;

            tooltipRiferimenti.Popup +=
                TooltipRiferimenti_Popup;

            tooltipRiferimenti.Draw +=
                TooltipRiferimenti_Draw;

            pictureBoxHeader.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Right;

            pictureBoxHeader.BringToFront();

            pictureBoxHeader.Cursor = Cursors.Hand;
            pictureBoxHeader.Click += pictureBoxHeader_Click;

            filtroZoomGriglia =
                new FiltroRotellinaZoom(this);

            timerZoomGriglia.Interval = 40;
            timerZoomGriglia.Tick +=
                (s, e) =>
                {
                    timerZoomGriglia.Stop();
                    ApplicaZoomPendenti();
                };

            timerRidimensionamentoGriglia.Interval = 80;
            timerRidimensionamentoGriglia.Tick +=
                (s, e) =>
                {
                    timerRidimensionamentoGriglia.Stop();
                    AggiornaAltezzeRigheGriglia();
                    dataGridView1.Invalidate();
                };

            dataGridView1.Resize +=
                (s, e) =>
                {
                    timerRidimensionamentoGriglia.Stop();
                    timerRidimensionamentoGriglia.Start();
                };

            Application.AddMessageFilter(
                filtroZoomGriglia);

            rotellinaGriglia =
                new RotellinaAccorpata(dataGridView1);

            FormClosed +=
                (s, e) =>
                {
                    rotellinaGriglia?.Dispose();

                    timerZoomGriglia.Stop();
                    timerZoomGriglia.Dispose();

                    timerRidimensionamentoGriglia.Stop();
                    timerRidimensionamentoGriglia.Dispose();

                    Application.RemoveMessageFilter(
                        filtroZoomGriglia);
                };
        }

        private async void FormLEGGI_Load(
            object sender,
            EventArgs e)
        {
            this.Load -= FormLEGGI_Load;

            var areaDiLavoro =
                Screen.FromControl(this).WorkingArea;

            Size = new Size(
                Math.Min(Width, areaDiLavoro.Width),
                Math.Min(Height, areaDiLavoro.Height));

            int x = Location.X;
            int y = Location.Y;

            if (x + Width > areaDiLavoro.Right)
                x = areaDiLavoro.Right - Width;

            if (x < areaDiLavoro.Left)
                x = areaDiLavoro.Left;

            if (y + Height > areaDiLavoro.Bottom)
                y = areaDiLavoro.Bottom - Height;

            if (y < areaDiLavoro.Top)
                y = areaDiLavoro.Top;

            Location = new Point(x, y);

            if (File.Exists(
                "Risorse\\Gesu_Bibbiax4.png"))
            {
                pictureBoxHeader.Image =
                    TemaBibbia.CaricaOvale(
                        "Risorse\\Gesu_Bibbiax4.png");
            }

            pictureBoxHeader.BringToFront();

            if (modalitaPreghiera)
                CaricaPreghiere();
            else if (modalitaRicerca)
                await CaricaRicercaAsync();
            else
                CaricaRiferimentoSingolo();

            if (Owner != null)
            {
                StartPosition =
                    FormStartPosition.Manual;

                Location = new Point(
                    Owner.Right + 5,
                    Owner.Top);
            }
        }

        private void pictureBoxHeader_Click(object sender, EventArgs e)
        {
            string percorso =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Risorse",
                    "Gesu_Bibbiax4.png");

            if (!File.Exists(percorso))
                return;

            using var formFoto = new FormFoto(percorso);
            formFoto.ShowDialog(this);
        }

        public void CaricaRiferimento(
            Input.LibroBiblico nuovoLibro,
            int nuovoCapitolo,
            int? nuovoVersettoDa,
            int? nuovoVersettoA)
        {
            libro = nuovoLibro;
            capitolo = nuovoCapitolo;
            versettoDa = nuovoVersettoDa;
            versettoA = nuovoVersettoA;

            modalitaRicerca = false;

            NascondiTooltipRiferimento();

            CaricaRiferimentoSingolo();
        }

        private void NavigaVersoRiferimento(
            Input.LibroBiblico libroDest,
            int capitoloDest,
            int? versettoDaDest,
            int? versettoADest)
        {
            CaricaRiferimento(
                libroDest,
                capitoloDest,
                versettoDaDest,
                versettoADest);

            aggiornaComboBoxInserisci?.Invoke(
                libroDest,
                capitoloDest,
                versettoDaDest,
                versettoADest);

            ApriOAggiornaMappaConcettuale(
                libroDest,
                capitoloDest,
                versettoDaDest,
                versettoADest);
        }

        private void ApriOAggiornaMappaConcettuale(
            Input.LibroBiblico libroDest,
            int capitoloDest,
            int? versettoDaDest,
            int? versettoADest)
        {
            if (!versettoDaDest.HasValue)
                return;

            if (formMappaAperta != null &&
                !formMappaAperta.IsDisposed)
            {
                formMappaAperta.CentraSu(
                    libroDest,
                    capitoloDest,
                    versettoDaDest.Value,
                    versettoADest);

                formMappaAperta.Activate();
                return;
            }

            formMappaAperta =
                new FormMappaConcettuale(
                    libroDest,
                    capitoloDest,
                    versettoDaDest.Value,
                    versettoADest,
                    connectionStringBibbia,
                    connectionStringNote,
                    NavigaVersoRiferimento);

            formMappaAperta.FormClosed +=
                (s, e) => formMappaAperta = null;

            formMappaAperta.Show(this);
        }

        private void CaricaPreghiere()
        {
            NascondiListeLibri();

            label_testamento.Text = "Preghiera";
            label_categoria.Text = titoloPreghiera ?? "";
            label_libro.Text = "";
            label_riferimento.Text = "";

            Text = titoloPreghiera ?? "Emmaus 2.0";

            PreparaGriglia();

            foreach (var (titolo, testo) in sezioniPreghiera)
            {
                int rTitolo =
                    dataGridView1.Rows.Add(titolo);

                var cellaTitolo =
                    dataGridView1.Rows[rTitolo].Cells[0];

                cellaTitolo.Style.Font =
                    CreaFontTitolo(
                        dataGridView1.Font.Size,
                        dataGridView1.Font.FontFamily);

                cellaTitolo.Style.BackColor =
                    TemaBibbia.TitoloVersetto;

                cellaTitolo.Style.SelectionBackColor =
                    TemaBibbia.TitoloVersetto;

                cellaTitolo.Style.SelectionForeColor =
                    TemaBibbia.TestoTitoloVersetto;

                cellaTitolo.Style.ForeColor =
                    TemaBibbia.TestoTitoloVersetto;

                // Libro nullo: titolo non navigabile verso la Bibbia.
                dataGridView1.Rows[rTitolo].Tag =
                    new RigaTitoloInfo
                    {
                        Libro = null,
                        Capitolo = 0,
                        VersettoDa = 0,
                        VersettoA = 0
                    };

                int rTesto =
                    dataGridView1.Rows.Add(testo);

                dataGridView1.Rows[rTesto]
                    .Cells[0]
                    .Style
                    .Font =
                    CreaFontCardo(
                        dataGridView1.Font.Size,
                        FontStyle.Regular);

                dataGridView1.Rows[rTesto].Tag =
                    new RigaVersettoInfo
                    {
                        Testo = testo
                    };
            }

            AggiornaAltezzeRigheGriglia();

            dataGridView1.ClearSelection();

            dataGridView1.CurrentCell = null;
        }

        private void CaricaRiferimentoSingolo()
        {
            NascondiListeLibri();

            Cursor.Current = Cursors.WaitCursor;

            try
            {
                var (testamento, categoria) =
                    Input.LibriCEI.CategoriaDiLibro(
                        libro.BookNumber);

                label_testamento.Text =
                    testamento;

                label_categoria.Text =
                    categoria;

                label_libro.Text =
                    $"{libro.Nome} ({libro.Abbreviazione})";

                string riferimento =
                    $"Capitolo {capitolo}";

                if (versettoDa.HasValue)
                {
                    riferimento +=
                        versettoA.HasValue &&
                        versettoA != versettoDa
                        ? $" , versetti {versettoDa}-{versettoA}"
                        : $" , versetto {versettoDa}";
                }
                else
                {
                    riferimento += " (intero)";
                }

                label_riferimento.Text =
                    riferimento;

                Text =
                    $"{libro.Nome} {capitolo}" +
                    (versettoDa.HasValue
                        ? $",{versettoDa}" +
                          (versettoA.HasValue &&
                           versettoA != versettoDa
                              ? $"-{versettoA}"
                              : "")
                        : "");

                var versetti =
                    new Dictionary<int, string>();

                using (var conn =
                    new SqliteConnection(
                        connectionStringBibbia))
                {
                    conn.Open();

                    var cmd =
                        conn.CreateCommand();

                    string filtro =
                        (versettoDa.HasValue
                            ? " AND verse >= @vda"
                            : "") +
                        (versettoA.HasValue
                            ? " AND verse <= @va"
                            : "");

                    cmd.CommandText = $@"
                        SELECT verse, text
                        FROM verses
                        WHERE book_number = @bn
                          AND chapter = @cap
                          {filtro}
                        ORDER BY verse;";

                    cmd.Parameters.AddWithValue(
                        "@bn",
                        libro.BookNumber);

                    cmd.Parameters.AddWithValue(
                        "@cap",
                        capitolo);

                    if (versettoDa.HasValue)
                        cmd.Parameters.AddWithValue(
                            "@vda",
                            versettoDa.Value);

                    if (versettoA.HasValue)
                        cmd.Parameters.AddWithValue(
                            "@va",
                            versettoA.Value);

                    using (var reader =
                        cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            versetti[
                                reader.GetInt32(0)] =
                                Regex.Replace(
                                    reader.GetString(1),
                                    "<[^>]+>",
                                    "");
                        }
                    }
                }

                if (versetti.Count == 0)
                {
                    MessaggioBibbia.Mostra(
                        "Nessun versetto trovato per questo riferimento",
                        "Attenzione");
                }

                var noteXVersetto =
                    new Dictionary<int, List<NotaInfo>>();

                using (var conn =
                    new SqliteConnection(
                        connectionStringNote))
                {
                    conn.Open();

                    var cmd =
                        conn.CreateCommand();

                    cmd.CommandText = @"
                        SELECT Id,
                               VersettoDa,
                               VersettoA,
                               TestoNota
                        FROM Note
                        WHERE Libro = @libro
                          AND Capitolo = @cap
                        ORDER BY COALESCE(Ordine, Id), Id;";

                    cmd.Parameters.AddWithValue(
                        "@libro",
                        libro.Nome);

                    cmd.Parameters.AddWithValue(
                        "@cap",
                        capitolo);

                    using (var reader =
                        cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long id =
                                reader.GetInt64(0);

                            int da =
                                reader.GetInt32(1);

                            int a =
                                reader.GetInt32(2);

                            string testoNota =
                                reader.GetString(3);

                            var nota =
                                new NotaInfo
                                {
                                    Id = id,
                                    Testo = testoNota
                                };

                            for (int v = da; v <= a; v++)
                            {
                                if (!noteXVersetto.ContainsKey(v))
                                    noteXVersetto[v] =
                                        new List<NotaInfo>();

                                noteXVersetto[v].Add(nota);
                            }
                        }
                    }
                }

                PreparaGriglia();

                foreach (var v in
                    versetti.Keys.OrderBy(x => x))
                {
                    int rTitolo =
                        dataGridView1.Rows.Add(
                            $"{libro.Abbreviazione} {capitolo},{v}");

                    var cellaTitolo =
                        dataGridView1.Rows[rTitolo]
                            .Cells[0];

                    cellaTitolo.Style.Font =
                        CreaFontTitolo(
                            dataGridView1.Font.Size,
                            dataGridView1.Font.FontFamily);

                    cellaTitolo.Style.BackColor =
                        TemaBibbia.TitoloVersetto;

                    cellaTitolo.Style.SelectionBackColor =
                        TemaBibbia.TitoloVersetto;

                    cellaTitolo.Style.SelectionForeColor =
                        TemaBibbia.TestoTitoloVersetto;

                    cellaTitolo.Style.ForeColor =
                        TemaBibbia.TestoTitoloVersetto;

                    dataGridView1.Rows[rTitolo].Tag =
                        new RigaTitoloInfo
                        {
                            Libro = libro,
                            Capitolo = capitolo,
                            VersettoDa = v,
                            VersettoA = v
                        };

                    int rTesto =
                        dataGridView1.Rows.Add(
                            versetti[v]);

                    dataGridView1.Rows[rTesto]
                        .Cells[0]
                        .Style
                        .Font =
                        CreaFontCardo(
                            dataGridView1.Font.Size,
                            FontStyle.Regular);

                    dataGridView1.Rows[rTesto].Tag =
                        new RigaVersettoInfo
                        {
                            Testo = versetti[v]
                        };

                    if (noteXVersetto.TryGetValue(
                        v,
                        out var noteList))
                    {
                        foreach (var nota in noteList)
                        {
                            int rNota =
                                dataGridView1.Rows.Add(
                                    "📝 " + nota.Testo);

                            var rigaNotaInfo =
                                new RigaNotaInfo
                                {
                                    Nota = nota,
                                    Riferimenti =
                                        RiferimentiBiblici.Trova(
                                            nota.Testo,
                                            OffsetIconaNota),
                                    Tags =
                                        TagNote.Trova(
                                            nota.Testo,
                                            OffsetIconaNota)
                                };

                            dataGridView1.Rows[rNota].Tag =
                                rigaNotaInfo;

                            var cellaNota =
                                dataGridView1.Rows[rNota]
                                    .Cells[0];

                            cellaNota.Style.Font =
                                CreaFontCardo(
                                    dataGridView1.Font.Size,
                                    FontStyle.Italic);

                            cellaNota.Style.ForeColor =
                                TemaBibbia.Nota;
                        }
                    }
                }

                AggiornaAltezzeRigheGriglia();

                dataGridView1.ClearSelection();

                dataGridView1.CurrentCell = null;
            }
            finally
            {
                Cursor.Current =
                    Cursors.Default;
            }
        }

        private static readonly Regex RegexTokenRicerca =
            new Regex(
                @"(""[^""]*"")|(\d+)\s*\.\.\s*(\d+)|(\S+)",
                RegexOptions.Compiled);

        private string CostruisciQueryFts5(
            string testoInput)
        {
            if (string.IsNullOrWhiteSpace(testoInput))
                return "";

            string testoTrim =
                testoInput.Trim();

            if (testoTrim.StartsWith("\"") &&
                testoTrim.EndsWith("\"") &&
                testoTrim.Length > 2)
            {
                string fraseInterna =
                    testoTrim
                        .Substring(
                            1,
                            testoTrim.Length - 2)
                        .Replace("\"", "");

                return $"\"{fraseInterna}\"";
            }

            var parole =
                Regex.Matches(
                        testoInput,
                        @"\p{L}+|\d+")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .Where(p => p.Length > 0)
                    .ToList();

            if (parole.Count == 0)
                return "";

            var termini =
                parole.Select(p => $"{p}*");

            return string.Join(
                " AND ",
                termini);
        }

        private static List<string>
            EstraiTerminiRicerca(
                string testoInput)
        {
            var termini =
                new List<string>();

            if (string.IsNullOrWhiteSpace(
                testoInput))
                return termini;

            foreach (Match m in
                RegexTokenRicerca.Matches(
                    testoInput))
            {
                string grezzo =
                    m.Groups[1].Success
                        ? m.Groups[1].Value
                        : m.Groups[4].Value;

                if (string.IsNullOrWhiteSpace(
                    grezzo))
                    continue;

                string pulito =
                    grezzo
                        .Trim('"')
                        .Trim('+', '-')
                        .TrimEnd('*');

                if (pulito.Length == 0)
                    continue;

                string maiuscolo =
                    pulito.ToUpperInvariant();

                if (maiuscolo == "AND" ||
                    maiuscolo == "OR" ||
                    maiuscolo == "NOT" ||
                    pulito == "|")
                {
                    continue;
                }

                if (!termini.Contains(
                    pulito,
                    StringComparer.OrdinalIgnoreCase))
                {
                    termini.Add(pulito);
                }
            }

            return termini;
        }

        private static bool
            EOperatoreOEspressione(
                string termine)
        {
            if (string.IsNullOrWhiteSpace(termine))
                return true;

            string t =
                termine.Trim()
                       .ToUpperInvariant();

            return t == "AND" ||
                   t == "OR" ||
                   t == "NOT" ||
                   t.StartsWith("NEAR(");
        }

        private static string
            FormattaTermineRicerca(
                string termine)
        {
            termine =
                termine?.Trim() ?? "";

            if (termine.Length == 0)
                return null;

            return
                $"\"{termine.Replace(
                    "\"",
                    "\"\"")}\"";
        }

        private static List<string>
            EstraiTerminiEvidenziazione(
                string testoInput)
        {
            var termini =
                new List<string>();

            if (string.IsNullOrWhiteSpace(
                testoInput))
                return termini;

            foreach (Match m in
                RegexTokenRicerca.Matches(
                    testoInput))
            {
                string grezzo =
                    m.Groups[1].Success
                        ? m.Groups[1].Value
                        : m.Groups[4].Value;

                if (string.IsNullOrWhiteSpace(
                    grezzo))
                    continue;

                string pulito =
                    grezzo
                        .Trim('+', '-')
                        .TrimEnd('*')
                        .Trim('"');

                // I tag (#parola) si evidenziano a parte.
                if (pulito.Length == 0 ||
                    pulito.StartsWith("#"))
                    continue;

                string maiuscolo =
                    pulito.ToUpperInvariant();

                if (maiuscolo == "AND" ||
                    maiuscolo == "OR" ||
                    maiuscolo == "NOT" ||
                    pulito == "|")
                {
                    continue;
                }

                if (!termini.Contains(
                    pulito,
                    StringComparer.OrdinalIgnoreCase))
                {
                    termini.Add(pulito);
                }
            }

            return termini;
        }

        private static List<(int Start, int Length)>
            TrovaTuttiGliIntervalli(
                string testo,
                string testoRicercaOriginale)
        {
            var risultati =
                new List<(int Start, int Length)>();

            if (string.IsNullOrEmpty(testo))
                return risultati;

            foreach (var termine in
                EstraiTerminiEvidenziazione(
                    testoRicercaOriginale))
            {
                risultati.AddRange(
                    TrovaIntervalli(
                        testo,
                        termine));
            }

            var tagCercati =
                TagNote.EstraiTagRicerca(
                    testoRicercaOriginale);

            if (tagCercati.Count > 0)
            {
                foreach (var occorrenza in
                    TagNote.Trova(testo))
                {
                    if (tagCercati.Any(
                        t => TagNote.Uguali(
                            t,
                            occorrenza.Nome)))
                    {
                        risultati.Add(
                            (occorrenza.Start,
                             occorrenza.Length));
                    }
                }
            }

            return risultati
                .OrderBy(iv => iv.Start)
                .ToList();
        }

        // Ricerca per tag: le parole rimaste oltre ai tag devono comparire
        // nel commento (frase tra virgolette = testo esatto).
        private static bool
            NotaCorrispondeAlResiduo(
                string testoNota,
                string residuo)
        {
            if (string.IsNullOrWhiteSpace(residuo))
                return true;

            testoNota ??= "";

            string frase =
                EstraiFraseEsatta(residuo);

            if (frase != null)
            {
                return testoNota.IndexOf(
                    frase,
                    StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return Regex.Matches(
                    residuo,
                    @"\p{L}+|\d+")
                .Cast<Match>()
                .All(
                    m => testoNota.IndexOf(
                        m.Value,
                        StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static List<(int Start, int Length)>
            TrovaIntervalli(
                string testo,
                string ricerca)
        {
            var risultati =
                new List<(int, int)>();

            if (string.IsNullOrEmpty(testo) ||
                string.IsNullOrEmpty(ricerca))
                return risultati;

            int indice = 0;

            while ((indice =
                testo.IndexOf(
                    ricerca,
                    indice,
                    StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                risultati.Add(
                    (indice, ricerca.Length));

                indice +=
                    Math.Max(
                        ricerca.Length,
                        1);
            }

            return risultati;
        }

        private static string
            EstraiFraseEsatta(
                string testo)
        {
            if (string.IsNullOrWhiteSpace(
                testo))
                return "";

            string trim =
                testo.Trim();

            if (trim.Length >= 2 &&
                trim.StartsWith("\"") &&
                trim.EndsWith("\""))
            {
                return trim.Substring(
                    1,
                    trim.Length - 2)
                    .Replace("\"\"", "\"");
            }

            return null;
        }

        private static void
            AggiungiCondizioniLike(
                SqliteCommand cmd,
                string testo,
                string nomeCampo,
                List<string> parametriDaCreare = null)
        {
            string frase =
                EstraiFraseEsatta(testo);

            if (frase != null)
            {
                string nomeParametro =
                    "@like0";

                cmd.CommandText +=
                    $" AND {nomeCampo} LIKE {nomeParametro}";

                cmd.Parameters.AddWithValue(
                    nomeParametro,
                    "%" + frase + "%");

                return;
            }

            var parole =
                Regex.Matches(
                        testo ?? "",
                        @"\p{L}+|\d+")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .Where(x => x.Length > 0)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            if (parole.Count == 0)
                return;

            for (int i = 0;
                 i < parole.Count;
                 i++)
            {
                string nomeParametro =
                    "@like" + i;

                cmd.CommandText +=
                    $" AND {nomeCampo} LIKE {nomeParametro}";

                cmd.Parameters.AddWithValue(
                    nomeParametro,
                    "%" + parole[i] + "%");
            }
        }

        private static bool
            NotaContieneRiferimento(
                string testoNota)
        {
            return !string.IsNullOrEmpty(testoNota) &&
                   RiferimentiBiblici.Trova(testoNota, 0).Count > 0;
        }

        private static void
            AggiungiRisultatoNota(
                List<UnitaRisultatoRicerca> tutteLeUnita,
                long id,
                string nomeLibro,
                int cap,
                int da,
                int a,
                string testoNota,
                Dictionary<long, int> noteCorrispondenti = null)
        {
            var libroObj =
                Input.LibriCEI.Elenco
                    .FirstOrDefault(
                        l => string.Equals(
                            l.Nome,
                            nomeLibro,
                            StringComparison.OrdinalIgnoreCase));

            if (libroObj == null)
                return;

            if (noteCorrispondenti != null)
                noteCorrispondenti[id] = libroObj.BookNumber;

            string chiave =
                $"{libroObj.BookNumber}_{cap}_{da}_{a}";

            var unita =
                tutteLeUnita.FirstOrDefault(
                    u => u.ChiaveUnica == chiave);

            if (unita == null)
            {
                unita =
                    new UnitaRisultatoRicerca
                    {
                        ChiaveUnica = chiave,
                        Libro = libroObj,
                        Capitolo = cap,
                        VersettoDa = da,
                        VersettoA = a,
                        TrovatoDaNota = true
                    };

                tutteLeUnita.Add(unita);
            }

            if (!unita.Note.Any(
                n => n.Id == id))
            {
                unita.Note.Add(
                    new NotaInfo
                    {
                        Id = id,
                        Testo = testoNota
                    });
            }
        }

        private Task<RisultatoPaginaRicerca>
            EseguiPaginaRicercaAsync(
                string testo,
                int offset,
                bool primaPagina,
                HashSet<(int Libro, int Capitolo, int Da, int A)>
                    giaSullaMappa,
                HashSet<string> giaMostrati,
                HashSet<int> libriSelezionati)
        {
            return Task.Run(() =>
            {
                // Se il testo cercato contiene dei tag (#parola) si cercano
                // solo i commenti che li hanno tutti; i versetti entrano nei
                // risultati come contesto di quei commenti.
                var tagCercati =
                    TagNote.EstraiTagRicerca(testo);

                bool ricercaPerTag =
                    tagCercati.Count > 0;

                string residuoRicercaTag =
                    ricercaPerTag
                        ? TagNote.RimuoviTag(testo)
                        : "";

                string queryFts =
                    ricercaPerTag
                        ? ""
                        : CostruisciQueryFts5(testo);

                var risultato =
                    new RisultatoPaginaRicerca();

                var tutteLeUnita =
                    new List<UnitaRisultatoRicerca>();

                var versettiCorrispondenti =
                    new HashSet<(int Libro, int Capitolo, int Verso)>();

                var noteCorrispondenti =
                    new Dictionary<long, int>();

                using (var connNote =
                    new SqliteConnection(
                        connectionStringNote))
                {
                    connNote.Open();

                    if (!string.IsNullOrWhiteSpace(
                        queryFts))
                    {
                        try
                        {
                            using var cmdNoteFts =
                                connNote.CreateCommand();

                            cmdNoteFts.CommandText = @"
                                SELECT
                                    n.Id,
                                    n.Libro,
                                    n.Capitolo,
                                    n.VersettoDa,
                                    n.VersettoA,
                                    n.TestoNota
                                FROM NoteFts
                                JOIN Note n
                                  ON n.Id = NoteFts.rowid
                                WHERE NoteFts MATCH @query
                                ORDER BY bm25(NoteFts);";

                            cmdNoteFts.Parameters.AddWithValue(
                                "@query",
                                queryFts);

                            using var readerNoteFts =
                                cmdNoteFts.ExecuteReader();

                            while (readerNoteFts.Read())
                            {
                                AggiungiRisultatoNota(
                                    tutteLeUnita,
                                    readerNoteFts.GetInt64(0),
                                    readerNoteFts.GetString(1),
                                    readerNoteFts.GetInt32(2),
                                    readerNoteFts.GetInt32(3),
                                    readerNoteFts.GetInt32(4),
                                    readerNoteFts.GetString(5),
                                    noteCorrispondenti);
                            }
                        }
                        catch (SqliteException)
                        {
                        }
                    }

                    if (ricercaPerTag)
                    {
                        using var cmdNoteTag =
                            connNote.CreateCommand();

                        cmdNoteTag.CommandText = @"
                            SELECT
                                Id,
                                Libro,
                                Capitolo,
                                VersettoDa,
                                VersettoA,
                                TestoNota
                            FROM Note
                            ORDER BY Id;";

                        using var readerNoteTag =
                            cmdNoteTag.ExecuteReader();

                        while (readerNoteTag.Read())
                        {
                            string testoNotaTag =
                                readerNoteTag.GetString(5);

                            if (!TagNote.NotaHaTuttiITag(
                                    testoNotaTag,
                                    tagCercati) ||
                                !NotaCorrispondeAlResiduo(
                                    testoNotaTag,
                                    residuoRicercaTag))
                            {
                                continue;
                            }

                            AggiungiRisultatoNota(
                                tutteLeUnita,
                                readerNoteTag.GetInt64(0),
                                readerNoteTag.GetString(1),
                                readerNoteTag.GetInt32(2),
                                readerNoteTag.GetInt32(3),
                                readerNoteTag.GetInt32(4),
                                testoNotaTag,
                                noteCorrispondenti);
                        }
                    }
                    else
                    using (var cmdNoteLike =
                        connNote.CreateCommand())
                    {
                        cmdNoteLike.CommandText = @"
                            SELECT
                                Id,
                                Libro,
                                Capitolo,
                                VersettoDa,
                                VersettoA,
                                TestoNota
                            FROM Note
                            WHERE 1=1";

                        AggiungiCondizioniLike(
                            cmdNoteLike,
                            testo,
                            "TestoNota");

                        cmdNoteLike.CommandText +=
                            " ORDER BY Id;";

                        using var readerNoteLike =
                            cmdNoteLike.ExecuteReader();

                        while (readerNoteLike.Read())
                        {
                            AggiungiRisultatoNota(
                                tutteLeUnita,
                                readerNoteLike.GetInt64(0),
                                readerNoteLike.GetString(1),
                                readerNoteLike.GetInt32(2),
                                readerNoteLike.GetInt32(3),
                                readerNoteLike.GetInt32(4),
                                readerNoteLike.GetString(5),
                                noteCorrispondenti);
                        }
                    }
                }

                using (var connBibbia =
                    new SqliteConnection(
                        connectionStringBibbia))
                {
                    connBibbia.Open();

                    bool trovatiConFts = false;

                    if (!string.IsNullOrWhiteSpace(
                        queryFts))
                    {
                        try
                        {
                            using var connVersettiFtsDb =
                                new SqliteConnection(connectionStringVersettiFts);
                            connVersettiFtsDb.Open();

                            using var cmdVersettiFts =
                                connVersettiFtsDb.CreateCommand();

                            cmdVersettiFts.CommandText = @"
                                SELECT
                                    book_number,
                                    chapter,
                                    verse,
                                    text
                                FROM VersesFts
                                WHERE VersesFts MATCH @query
                                ORDER BY bm25(VersesFts);";

                            cmdVersettiFts.Parameters.AddWithValue(
                                "@query",
                                queryFts);

                            using var readerVersettiFts =
                                cmdVersettiFts.ExecuteReader();

                            while (readerVersettiFts.Read())
                            {
                                trovatiConFts = true;

                                int bn =
                                    readerVersettiFts.GetInt32(0);

                                int cap =
                                    readerVersettiFts.GetInt32(1);

                                int v =
                                    readerVersettiFts.GetInt32(2);

                                string testoV =
                                    Regex.Replace(
                                        readerVersettiFts.GetString(3),
                                        "<[^>]+>",
                                        "");

                                var libroObj =
                                    Input.LibriCEI.Elenco
                                        .FirstOrDefault(
                                            l => l.BookNumber == bn);

                                if (libroObj == null)
                                    continue;

                                versettiCorrispondenti.Add((bn, cap, v));

                                string chiave =
                                    $"{bn}_{cap}_{v}_{v}";

                                var unita =
                                    tutteLeUnita
                                        .FirstOrDefault(
                                            u => u.ChiaveUnica == chiave);

                                if (unita == null)
                                {
                                    unita =
                                        new UnitaRisultatoRicerca
                                        {
                                            ChiaveUnica = chiave,
                                            Libro = libroObj,
                                            Capitolo = cap,
                                            VersettoDa = v,
                                            VersettoA = v,
                                            TestoVersetto = testoV,
                                            TrovatoDaNota = false
                                        };

                                    tutteLeUnita.Add(unita);
                                }
                                else if (
                                    string.IsNullOrEmpty(
                                        unita.TestoVersetto))
                                {
                                    unita.TestoVersetto =
                                        testoV;
                                }
                            }
                        }
                        catch (SqliteException)
                        {
                        }
                    }

                    // Nella ricerca per tag non si cerca nel testo biblico.
                    if (!ricercaPerTag)
                    using (var cmdVersettiLike =
                        connBibbia.CreateCommand())
                    {
                        cmdVersettiLike.CommandText = @"
                            SELECT
                                book_number,
                                chapter,
                                verse,
                                text
                            FROM verses
                            WHERE 1=1";

                        AggiungiCondizioniLike(
                            cmdVersettiLike,
                            testo,
                            "text");

                        cmdVersettiLike.CommandText +=
                            " ORDER BY book_number, chapter, verse;";

                        using var readerVersettiLike =
                            cmdVersettiLike.ExecuteReader();

                        while (readerVersettiLike.Read())
                        {
                            int bn =
                                readerVersettiLike.GetInt32(0);

                            int cap =
                                readerVersettiLike.GetInt32(1);

                            int v =
                                readerVersettiLike.GetInt32(2);

                            string testoV =
                                Regex.Replace(
                                    readerVersettiLike.GetString(3),
                                    "<[^>]+>",
                                    "");

                            var libroObj =
                                Input.LibriCEI.Elenco
                                    .FirstOrDefault(
                                        l => l.BookNumber == bn);

                            if (libroObj == null)
                                continue;

                            versettiCorrispondenti.Add((bn, cap, v));

                            string chiave =
                                $"{bn}_{cap}_{v}_{v}";

                            var unita =
                                tutteLeUnita
                                    .FirstOrDefault(
                                        u => u.ChiaveUnica == chiave);

                            if (unita == null)
                            {
                                unita =
                                    new UnitaRisultatoRicerca
                                    {
                                        ChiaveUnica = chiave,
                                        Libro = libroObj,
                                        Capitolo = cap,
                                        VersettoDa = v,
                                        VersettoA = v,
                                        TestoVersetto = testoV,
                                        TrovatoDaNota = false
                                    };

                                tutteLeUnita.Add(unita);
                            }
                            else if (
                                string.IsNullOrEmpty(
                                    unita.TestoVersetto))
                            {
                                unita.TestoVersetto =
                                    testoV;
                            }
                        }
                    }
                }

                var unitaAssorbite =
                    tutteLeUnita
                        .Where(
                            uVerso =>
                                !uVerso.TrovatoDaNota &&
                                tutteLeUnita.Any(
                                    uNota =>
                                        uNota.TrovatoDaNota &&
                                        uNota.Libro.BookNumber ==
                                            uVerso.Libro.BookNumber &&
                                        uNota.Capitolo ==
                                            uVerso.Capitolo &&
                                        uNota.VersettoDa <=
                                            uVerso.VersettoDa &&
                                        uNota.VersettoA >=
                                            uVerso.VersettoA))
                        .ToList();

                foreach (var uAssorbita in
                    unitaAssorbite)
                {
                    tutteLeUnita.Remove(
                        uAssorbita);
                }

                using (var connNote =
                    new SqliteConnection(
                        connectionStringNote))
                {
                    connNote.Open();

                    foreach (var u in
                        tutteLeUnita)
                    {
                        using var cmdN =
                            connNote.CreateCommand();

                        cmdN.CommandText = @"
                                SELECT
                                    Id,
                                    TestoNota
                                FROM Note
                                WHERE Libro = @libro
                                  AND Capitolo = @cap
                                  AND VersettoDa <= @a
                                  AND VersettoA >= @da
                                ORDER BY COALESCE(Ordine, Id), Id;";

                        cmdN.Parameters.AddWithValue(
                            "@libro",
                            u.Libro.Nome);

                        cmdN.Parameters.AddWithValue(
                            "@cap",
                            u.Capitolo);

                        cmdN.Parameters.AddWithValue(
                            "@da",
                            u.VersettoDa);

                        cmdN.Parameters.AddWithValue(
                            "@a",
                            u.VersettoA);

                        using var r =
                            cmdN.ExecuteReader();

                        while (r.Read())
                        {
                            long id =
                                r.GetInt64(0);

                            if (!u.Note.Any(
                                n => n.Id == id))
                            {
                                u.Note.Add(
                                    new NotaInfo
                                    {
                                        Id = id,
                                        Testo = r.GetString(1)
                                    });
                            }
                        }
                    }
                }

                using (var connBibbiaPerNote =
                    new SqliteConnection(
                        connectionStringBibbia))
                {
                    connBibbiaPerNote.Open();

                    foreach (var u in
                        tutteLeUnita)
                    {
                        if (!u.TrovatoDaNota ||
                            !string.IsNullOrEmpty(
                                u.TestoVersetto))
                            continue;

                        using var cmdVersettoNota =
                            connBibbiaPerNote.CreateCommand();

                        cmdVersettoNota.CommandText = @"
                            SELECT verse, text
                            FROM verses
                            WHERE book_number = @bn
                              AND chapter = @cap
                              AND verse >= @da
                              AND verse <= @a
                            ORDER BY verse;";

                        cmdVersettoNota.Parameters.AddWithValue(
                            "@bn",
                            u.Libro.BookNumber);

                        cmdVersettoNota.Parameters.AddWithValue(
                            "@cap",
                            u.Capitolo);

                        cmdVersettoNota.Parameters.AddWithValue(
                            "@da",
                            u.VersettoDa);

                        cmdVersettoNota.Parameters.AddWithValue(
                            "@a",
                            u.VersettoA);

                        using var readerVersettoNota =
                            cmdVersettoNota.ExecuteReader();

                        var testiVersettiNota =
                            new List<string>();

                        while (readerVersettoNota.Read())
                        {
                            testiVersettiNota.Add(
                                Regex.Replace(
                                    readerVersettoNota.GetString(1),
                                    "<[^>]+>",
                                    ""));
                        }

                        if (testiVersettiNota.Count > 0)
                        {
                            u.TestoVersetto =
                                string.Join(
                                    " ",
                                    testiVersettiNota);
                        }
                    }
                }

                tutteLeUnita =
                    tutteLeUnita
                        .GroupBy(
                            u => u.ChiaveUnica)
                        .Select(
                            g => g.First())
                        .ToList();

                tutteLeUnita =
                    tutteLeUnita
                        .OrderBy(
                            u => u.Libro.BookNumber)
                        .ThenBy(
                            u => u.Capitolo)
                        .ThenBy(
                            u => u.VersettoDa)
                        .ThenBy(
                            u => u.TrovatoDaNota ? 0 : 1)
                        .ThenBy(
                            u => u.VersettoA)
                        .ToList();

                risultato.LibriConOccorrenze =
                    tutteLeUnita
                        .Select(
                            u => u.Libro.BookNumber)
                        .ToHashSet();

                risultato.LibriConRiferimento =
                    tutteLeUnita
                        .Where(
                            u => u.Note.Any(
                                n => NotaContieneRiferimento(n.Testo)))
                        .Select(
                            u => u.Libro.BookNumber)
                        .ToHashSet();

                if (libriSelezionati != null)
                {
                    tutteLeUnita =
                        tutteLeUnita
                            .Where(
                                u => libriSelezionati.Contains(
                                    u.Libro.BookNumber))
                            .ToList();
                }

                risultato.TotaleUnitaCorrispondenti =
                    tutteLeUnita.Count;

                risultato.VersettiTrovatiPerLibro =
                    versettiCorrispondenti
                        .GroupBy(x => x.Libro)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Count());

                risultato.NoteTrovatePerLibro =
                    noteCorrispondenti
                        .GroupBy(x => x.Value)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Count());

                risultato.TotaleVersettiTrovati =
                    risultato.VersettiTrovatiPerLibro
                        .Where(
                            kv =>
                                libriSelezionati == null ||
                                libriSelezionati.Contains(kv.Key))
                        .Sum(kv => kv.Value);

                risultato.TotaleNoteTrovate =
                    risultato.NoteTrovatePerLibro
                        .Where(
                            kv =>
                                libriSelezionati == null ||
                                libriSelezionati.Contains(kv.Key))
                        .Sum(kv => kv.Value);

                var unitaPagina =
                    tutteLeUnita
                        .Skip(Math.Max(offset, 0))
                        .Take(DimensionePaginaRicerca)
                        .ToList();

                foreach (var u in
                    unitaPagina)
                {
                    var chiaveMappa =
                        (
                            u.Libro.BookNumber,
                            u.Capitolo,
                            u.VersettoDa,
                            u.VersettoA
                        );

                    if (giaSullaMappa.Add(
                        chiaveMappa))
                    {
                        risultato
                            .OccorrenzeMappaNuove
                            .Add(
                                (
                                    u.Libro,
                                    u.Capitolo,
                                    u.VersettoDa,
                                    u.VersettoA
                                ));
                    }

                    giaMostrati.Add(
                        u.ChiaveUnica);
                }

                risultato.UnitaPagina =
                    unitaPagina;

                return risultato;
            });
        }

        private async Task CaricaRicercaAsync()
        {
            int miaGenerazione =
                ++generazioneRicercaCorrente;

            MostraControlliFiltroTestamento();

            versettiPerLibroRicerca = new Dictionary<int, int>();
            notePerLibroRicerca = new Dictionary<int, int>();
            AggiornaContatoriRicerca();

            Text =
                $@"Ricerca: ""{testoRicerca}""";

            offsetPaginaRicercaCorrente = 0;
            ricercaHaAltriRisultati = false;
            totaleUnitaMostrateRicerca = 0;
            totaleUnitaRicercaCorrispondenti = 0;

            mappaGiaAggiuntiRicerca.Clear();
            occorrenzeMappaRicercaAccumulate.Clear();
            chiaviUnitaGiaMostrate.Clear();

            selezioneDefaultLibriRicercaApplicata =
                false;

            libriConOccorrenzeRicercaCorrente =
                new HashSet<int>();

            NascondiPulsanteAltriRisultati();

            PreparaGriglia();

            label_categoria.Text =
                "Ricerca in corso...";

            var paginaCompleta =
                await EseguiPaginaRicercaAsync(
                    testoRicerca,
                    0,
                    true,
                    mappaGiaAggiuntiRicerca,
                    chiaviUnitaGiaMostrate,
                    null);

            if (miaGenerazione !=
                generazioneRicercaCorrente)
                return;

            libriConOccorrenzeRicercaCorrente =
                paginaCompleta.LibriConOccorrenze;

            libriConRiferimentoRicercaCorrente =
                paginaCompleta.LibriConRiferimento;

            AggiornaColoriLibriRicerca();

            versettiPerLibroRicerca =
                paginaCompleta.VersettiTrovatiPerLibro;

            notePerLibroRicerca =
                paginaCompleta.NoteTrovatePerLibro;

            AggiornaContatoriRicerca();

            ApplicaSelezioneDefaultLibriRicerca(
                libriConOccorrenzeRicercaCorrente);

            selezioneDefaultLibriRicercaApplicata =
                true;

            // Ricalcola la disposizione a stato assestato: la prima
            // chiamata (in AggiornaContatoriRicerca) avviene con i
            // contatori ancora azzerati.
            AdattaDimensioniListeLibri();
            AggiornaAltezzaPanelHeader();

            mappaGiaAggiuntiRicerca.Clear();
            occorrenzeMappaRicercaAccumulate.Clear();
            chiaviUnitaGiaMostrate.Clear();

            offsetPaginaRicercaCorrente = 0;
            totaleUnitaMostrateRicerca = 0;

            PreparaGriglia();

            await CaricaPaginaRicercaAsync(
                true,
                miaGenerazione);

            RiadattaListeLibriDifferito();
        }

        // Rilancia la disposizione delle liste dei libri quando la finestra
        // ha finito di elaborare i messaggi in coda: cosi' il layout parte
        // da dimensioni e visibilita' gia' definitive.
        private void RiadattaListeLibriDifferito()
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            BeginInvoke(new Action(() =>
            {
                if (IsDisposed)
                    return;

                AdattaDimensioniListeLibri();
                AggiornaAltezzaPanelHeader();
            }));
        }

        private async Task
            RicaricaRicercaConFiltriAsync()
        {
            int miaGenerazione =
                ++generazioneRicercaCorrente;

            offsetPaginaRicercaCorrente = 0;
            ricercaHaAltriRisultati = false;
            totaleUnitaMostrateRicerca = 0;
            totaleUnitaRicercaCorrispondenti = 0;

            mappaGiaAggiuntiRicerca.Clear();
            occorrenzeMappaRicercaAccumulate.Clear();
            chiaviUnitaGiaMostrate.Clear();

            NascondiPulsanteAltriRisultati();

            PreparaGriglia();

            await CaricaPaginaRicercaAsync(
                true,
                miaGenerazione);

            RiadattaListeLibriDifferito();
        }

        private async Task
            CaricaPaginaRicercaAsync(
                bool primaPagina,
                int? generazioneAttesa = null)
        {
            int generazioneRichiesta =
                generazioneAttesa ??
                generazioneRicercaCorrente;

            if (generazioneRichiesta !=
                generazioneRicercaCorrente)
                return;

            UseWaitCursor = true;

            dataGridView1.Enabled = false;

            if (button_altriRisultatiRicerca != null)
            {
                button_altriRisultatiRicerca.Enabled =
                    false;
            }

            try
            {
                HashSet<int> libriSelezionati =
                    OttieniLibriSelezionatiPerRicerca();

                var pagina =
                    await EseguiPaginaRicercaAsync(
                        testoRicerca,
                        offsetPaginaRicercaCorrente,
                        primaPagina,
                        mappaGiaAggiuntiRicerca,
                        chiaviUnitaGiaMostrate,
                        libriSelezionati);

                if (generazioneRichiesta !=
                    generazioneRicercaCorrente)
                    return;

                label_testamento.Text =
                    "Ricerca";

                label_categoria.Text =
                    TagNote.EstraiTagRicerca(testoRicerca).Count > 0
                        ? $"Tag cercato: {testoRicerca}"
                        : $"Testo cercato: \"{testoRicerca}\"";

                label_libro.Text =
                    $"Versetti trovati: {pagina.TotaleVersettiTrovati}";

                label_riferimento.Text =
                    $"Note trovate: {pagina.TotaleNoteTrovate}";

                totaleUnitaRicercaCorrispondenti =
                    pagina.TotaleUnitaCorrispondenti;

                int primaRigaNuovaPagina =
                    dataGridView1.Rows.Count;

                dataGridView1.SuspendLayout();

                dataGridView1.AutoSizeRowsMode =
                    DataGridViewAutoSizeRowsMode.None;

                foreach (var unita in
                    pagina.UnitaPagina)
                {
                    string abbrev =
                        unita.Libro.Abbreviazione;

                    string etichetta =
                        unita.VersettoDa ==
                        unita.VersettoA
                        ? $"{abbrev} {unita.Capitolo},{unita.VersettoDa}"
                        : $"{abbrev} {unita.Capitolo},{unita.VersettoDa}-{unita.VersettoA}";

                    int rTitolo =
                        dataGridView1.Rows.Add(
                            etichetta);

                    var cellaTitolo =
                        dataGridView1.Rows[rTitolo]
                            .Cells[0];

                    cellaTitolo.Style.Font =
                        CreaFontTitolo(
                            dataGridView1.Font.Size,
                            dataGridView1.Font.FontFamily);

                    cellaTitolo.Style.BackColor =
                        TemaBibbia.TitoloVersetto;

                    cellaTitolo.Style.SelectionBackColor =
                        TemaBibbia.TitoloVersetto;

                    cellaTitolo.Style.SelectionForeColor =
                        TemaBibbia.TestoTitoloVersetto;

                    cellaTitolo.Style.ForeColor =
                        TemaBibbia.TestoTitoloVersetto;

                    dataGridView1.Rows[rTitolo].Tag =
                        new RigaTitoloInfo
                        {
                            Libro = unita.Libro,
                            Capitolo = unita.Capitolo,
                            VersettoDa = unita.VersettoDa,
                            VersettoA = unita.VersettoA
                        };

                    if (!string.IsNullOrEmpty(
                        unita.TestoVersetto))
                    {
                        int rTesto =
                            dataGridView1.Rows.Add(
                                unita.TestoVersetto);

                        dataGridView1.Rows[rTesto]
                            .Cells[0]
                            .Style
                            .Font =
                            CreaFontCardo(
                                dataGridView1.Font.Size,
                                FontStyle.Regular);

                        var evidenzeVersetto =
                            TrovaTuttiGliIntervalli(
                                unita.TestoVersetto,
                                testoRicerca);

                        dataGridView1.Rows[rTesto].Tag =
                            new RigaVersettoInfo
                            {
                                Testo =
                                    unita.TestoVersetto,
                                Intervalli =
                                    evidenzeVersetto
                            };
                    }

                    foreach (var nota in
                        unita.Note)
                    {
                        string testoConEmoji =
                            "📝 " + nota.Testo;

                        int rNota =
                            dataGridView1.Rows.Add(
                                testoConEmoji);

                        var cellaNota =
                            dataGridView1.Rows[rNota]
                                .Cells[0];

                        cellaNota.Style.Font =
                            CreaFontCardo(
                                dataGridView1.Font.Size,
                                FontStyle.Italic);

                        cellaNota.Style.ForeColor =
                            TemaBibbia.Nota;

                        var evidenzeNota =
                            TrovaTuttiGliIntervalli(
                                nota.Testo,
                                testoRicerca);

                        var rigaNota =
                            new RigaNotaInfo
                            {
                                Nota = nota,
                                Riferimenti =
                                    RiferimentiBiblici.Trova(
                                        nota.Testo,
                                        OffsetIconaNota),
                                Tags =
                                    TagNote.Trova(
                                        nota.Testo,
                                        OffsetIconaNota)
                            };

                        if (evidenzeNota.Count > 0)
                        {
                            rigaNota.Intervalli =
                                evidenzeNota
                                    .Select(
                                        iv =>
                                        (
                                            iv.Item1 +
                                            OffsetIconaNota,
                                            iv.Item2
                                        ))
                                    .ToList();
                        }

                        dataGridView1.Rows[rNota].Tag =
                            rigaNota;
                    }
                }

                if (primaPagina &&
                    pagina.TotaleUnitaCorrispondenti == 0)
                {
                    dataGridView1.Rows.Add(
                        $"Nessun risultato per \"{testoRicerca}\"");
                }

                dataGridView1.AutoSizeRowsMode =
                    DataGridViewAutoSizeRowsMode.None;

                dataGridView1.ResumeLayout();

                AggiornaAltezzeRigheGriglia();

                dataGridView1.ClearSelection();

                dataGridView1.CurrentCell = null;

                if (pagina
                    .OccorrenzeMappaNuove
                    .Count > 0)
                {
                    occorrenzeMappaRicercaAccumulate
                        .AddRange(
                            pagina.OccorrenzeMappaNuove);
                }

                if (occorrenzeMappaRicercaAccumulate.Count > 0 &&
                    (riapriMappaDopoRicerca ||
                     (formMappaAperta != null &&
                      !formMappaAperta.IsDisposed)))
                {
                    riapriMappaDopoRicerca = false;

                    ApriMappaPerRicerca(
                        occorrenzeMappaRicercaAccumulate,
                        testoRicerca);
                }

                int risultatiPagina =
                    pagina.UnitaPagina.Count;

                totaleUnitaMostrateRicerca +=
                    risultatiPagina;

                offsetPaginaRicercaCorrente +=
                    risultatiPagina;

                ricercaHaAltriRisultati =
                    offsetPaginaRicercaCorrente <
                    totaleUnitaRicercaCorrispondenti;

                AggiornaPulsanteAltriRisultati();

                if (!primaPagina &&
                    risultatiPagina > 0 &&
                    dataGridView1.Rows.Count > 0)
                {
                    bool eraInFondo =
                        dataGridView1.FirstDisplayedScrollingRowIndex
                            >= 0 &&
                        dataGridView1
                            .FirstDisplayedScrollingRowIndex +
                            dataGridView1.DisplayedRowCount(false)
                            >= primaRigaNuovaPagina;

                    if (eraInFondo)
                    {
                        int ultimaRiga =
                            dataGridView1.Rows.Count - 1;

                        dataGridView1.ClearSelection();

                        dataGridView1.FirstDisplayedScrollingRowIndex =
                            Math.Max(
                                0,
                                Math.Min(
                                    primaRigaNuovaPagina,
                                    ultimaRiga));
                    }
                }
            }
            finally
            {
                if (generazioneRichiesta ==
                    generazioneRicercaCorrente)
                {
                    UseWaitCursor = false;

                    Cursor.Current =
                        Cursors.Default;

                    dataGridView1.Enabled = true;

                    if (button_altriRisultatiRicerca != null)
                        button_altriRisultatiRicerca.Enabled =
                            true;
                }
            }
        }

        private async void
            checkBox_Testamento_CheckedChanged(
                object sender,
                EventArgs e)
        {
            if (aggiornamentoFiltroTestamentoInCorso)
                return;

            PreparaListeLibriTestamento();

            var checkBoxTestamento =
                (CheckBox)sender;

            bool eAT =
                checkBoxTestamento == checkBox_AT;

            var flow =
                eAT
                    ? flowLibriAT
                    : flowLibriNT;

            var checkSelezionaTutto =
                eAT
                    ? checkSelezionaTuttoAT
                    : checkSelezionaTuttoNT;

            aggiornamentoListaLibriInCorso =
                true;

            foreach (CheckBox checkBoxLibro
                in flow.Controls)
            {
                if (!checkBoxTestamento.Checked)
                {
                    checkBoxLibro.Checked =
                        false;
                }
                else if (
                    checkBoxLibro.Tag
                        is Input.LibroBiblico libro)
                {
                    checkBoxLibro.Checked =
                        libriConOccorrenzeRicercaCorrente
                            == null ||
                        libriConOccorrenzeRicercaCorrente
                            .Contains(
                                libro.BookNumber);
                }
            }

            aggiornamentoListaLibriInCorso =
                false;

            AggiornaCheckSelezionaTutto(
                flow,
                checkSelezionaTutto);

            SincronizzaTestamentoDaLibri(
                flow,
                checkBoxTestamento);

            if (!checkBox_AT.Checked)
            {
                ComprimiListaSeEspansa(
                    contenitoreListaLibriAT,
                    bottoneEspandiAT);
            }

            if (!checkBox_NT.Checked)
            {
                ComprimiListaSeEspansa(
                    contenitoreListaLibriNT,
                    bottoneEspandiNT);
            }

            if (modalitaRicerca)
            {
                await RicaricaRicercaConFiltriAsync();
            }
        }

        private void PreparaListeLibriTestamento()
        {
            if (contenitoreListaLibriAT != null)
                return;

            larghezzaColonnaLibri =
                CalcolaLarghezzaColonnaLibro();

            contenitoreListaLibriAT =
                CreaContenitoreListaLibri(
                    LibriDelTestamento(
                        "Antico Testamento"),
                    10,
                    checkBox_AT,
                    out flowLibriAT,
                    out checkSelezionaTuttoAT);

            contenitoreListaLibriNT =
                CreaContenitoreListaLibri(
                    LibriDelTestamento(
                        "Nuovo Testamento"),
                    9,
                    checkBox_NT,
                    out flowLibriNT,
                    out checkSelezionaTuttoNT);

            panelTesti.Controls.Add(
                contenitoreListaLibriAT);

            panelTesti.Controls.Add(
                contenitoreListaLibriNT);

            RiposizionaListeLibri();
        }

        private static IEnumerable<Input.LibroBiblico>
            LibriDelTestamento(
                string testamento)
        {
            return Input.LibriCEI.Elenco
                .Where(
                    l =>
                        Input.LibriCEI
                            .CategoriaDiLibro(
                                l.BookNumber)
                            .Testamento ==
                        testamento);
        }

        private Panel
            CreaContenitoreListaLibri(
                IEnumerable<Input.LibroBiblico> libri,
                int maxElementiPerColonna,
                CheckBox checkBoxTestamento,
                out Panel flowRisultante,
                out CheckBox checkSelezionaTuttoRisultante)
        {
            int larghezzaLibro = larghezzaColonnaLibri;

            int altezzaSingolaRiga =
                Font.Height + 12;

            int altezzaHeaderSeleziona =
                altezzaSingolaRiga + 16;

            int margineVerticaleLibro = 4;

            int altezzaRigaFlow =
                altezzaSingolaRiga +
                margineVerticaleLibro;

            int altezzaFlowPrecisa =
                (altezzaRigaFlow *
                 maxElementiPerColonna) + 18;

            altezzaCalcolataListaLibri =
                altezzaHeaderSeleziona +
                altezzaFlowPrecisa +
                10;

            var flow =
                new Panel
                {
                    // Le colonne dei libri si dispongono a mano (vedi
                    // AdattaDimensioniListeLibri): niente FlowLayoutPanel,
                    // che mandava a capo i libri da solo quando compariva
                    // la barra di scorrimento orizzontale.
                    AutoScroll = true,

                    Dock = DockStyle.Fill,

                    Padding =
                        new Padding(0),

                    Margin =
                        new Padding(0),

                    BackColor =
                        TemaBibbia.FondoCampo
                };

            var checkSelezionaTutto =
                new CheckBox
                {
                    Dock = DockStyle.Top,

                    Height =
                        altezzaHeaderSeleziona,

                    Text =
                        "Seleziona tutto",

                    Font =
                        new Font(
                            Font.FontFamily,
                            Font.Size + 1.5f,
                            FontStyle.Bold),

                    AutoCheck = false,

                    Checked = true,

                    Padding =
                        new Padding(
                            4, 2, 0, 4),

                    Margin =
                        new Padding(
                            0, 0, 0, 6)
                };

            foreach (var libroCorrente in libri)
            {
                var checkBoxLibro =
                    new CheckBox
                    {
                        Text =
                            Input.LibriCEI
                                .NomeVisualizzato(
                                    libroCorrente),

                        Tag =
                            libroCorrente,

                        AutoSize = false,

                        Width =
                            larghezzaLibro,

                        Height =
                            altezzaSingolaRiga,

                        Checked = true,

                        Margin =
                            new Padding(
                                3, 2, 8, 2),

                        TextAlign =
                            ContentAlignment.MiddleLeft
                    };

                checkBoxLibro.CheckedChanged +=
                    async (s, e) =>
                    {
                        if (aggiornamentoListaLibriInCorso)
                            return;

                        AggiornaCheckSelezionaTutto(
                            flow,
                            checkSelezionaTutto);

                        SincronizzaTestamentoDaLibri(
                            flow,
                            checkBoxTestamento);

                        if (modalitaRicerca)
                        {
                            await RicaricaRicercaConFiltriAsync();
                        }
                    };

                flow.Controls.Add(
                    checkBoxLibro);
            }

            checkSelezionaTutto.Click +=
                async (s, e) =>
                {
                    bool selezionaTutto =
                        !checkSelezionaTutto.Checked;

                    aggiornamentoListaLibriInCorso =
                        true;

                    foreach (CheckBox checkBoxLibro
                        in flow.Controls)
                    {
                        if (!selezionaTutto)
                        {
                            checkBoxLibro.Checked =
                                false;
                        }
                        else if (
                            checkBoxLibro.Tag
                                is Input.LibroBiblico libroCorrente2)
                        {
                            checkBoxLibro.Checked =
                                libriConOccorrenzeRicercaCorrente
                                    == null ||
                                libriConOccorrenzeRicercaCorrente
                                    .Contains(
                                        libroCorrente2.BookNumber);
                        }
                    }

                    aggiornamentoListaLibriInCorso =
                        false;

                    AggiornaCheckSelezionaTutto(
                        flow,
                        checkSelezionaTutto);

                    SincronizzaTestamentoDaLibri(
                        flow,
                        checkBoxTestamento);

                    if (modalitaRicerca)
                    {
                        await RicaricaRicercaConFiltriAsync();
                    }
                };

            var contenitore =
                new Panel
                {
                    Size =
                        new Size(
                            720,
                            altezzaCalcolataListaLibri),

                    Visible = false
                };

            contenitore.Controls.Add(flow);

            contenitore.Controls.Add(
                checkSelezionaTutto);

            flowRisultante = flow;

            checkSelezionaTuttoRisultante =
                checkSelezionaTutto;

            return contenitore;
        }

        private void
            SincronizzaTestamentoDaLibri(
                Panel flow,
                CheckBox checkBoxTestamento)
        {
            bool almenoUnLibroSelezionato =
                flow.Controls
                    .Cast<CheckBox>()
                    .Any(cb => cb.Checked);

            if (checkBoxTestamento.Checked ==
                almenoUnLibroSelezionato)
                return;

            aggiornamentoFiltroTestamentoInCorso =
                true;

            try
            {
                checkBoxTestamento.Checked =
                    almenoUnLibroSelezionato;
            }
            finally
            {
                aggiornamentoFiltroTestamentoInCorso =
                    false;
            }
        }

        private void
            AggiornaColoriLibriRicerca()
        {
            void Colora(Panel flow)
            {
                if (flow == null)
                    return;

                foreach (CheckBox checkBoxLibro in flow.Controls.OfType<CheckBox>())
                {
                    if (checkBoxLibro.Tag is not Input.LibroBiblico libroCorrente)
                        continue;

                    checkBoxLibro.ForeColor =
                        libriConRiferimentoRicercaCorrente != null &&
                        libriConRiferimentoRicercaCorrente.Contains(libroCorrente.BookNumber)
                            ? TemaBibbia.Riferimento
                            : TemaBibbia.Avorio;
                }
            }

            Colora(flowLibriAT);
            Colora(flowLibriNT);
        }

        private void
            AggiornaCheckSelezionaTutto(
                Panel flow,
                CheckBox checkTutto)
        {
            var candidati =
                flow.Controls
                    .Cast<CheckBox>()
                    .Where(
                        cb =>
                            libriConOccorrenzeRicercaCorrente
                                == null ||
                            (
                                cb.Tag
                                    is Input.LibroBiblico libro &&
                                libriConOccorrenzeRicercaCorrente
                                    .Contains(
                                        libro.BookNumber)
                            ))
                    .ToList();

            int totale =
                candidati.Count;

            int selezionati =
                candidati.Count(
                    cb => cb.Checked);

            checkTutto.Checked =
                totale > 0 &&
                selezionati == totale;
        }

        private HashSet<int>
            OttieniLibriSelezionatiPerRicerca()
        {
            if (!checkBox_AT.Checked &&
                !checkBox_NT.Checked)
            {
                return new HashSet<int>();
            }

            PreparaListeLibriTestamento();

            var selezionati =
                new HashSet<int>();

            if (checkBox_AT.Checked &&
                flowLibriAT != null)
            {
                foreach (Control control
                    in flowLibriAT.Controls)
                {
                    if (control is CheckBox cb &&
                        cb.Checked &&
                        cb.Tag
                            is Input.LibroBiblico libroCorrente)
                    {
                        selezionati.Add(
                            libroCorrente.BookNumber);
                    }
                }
            }

            if (checkBox_NT.Checked &&
                flowLibriNT != null)
            {
                foreach (Control control
                    in flowLibriNT.Controls)
                {
                    if (control is CheckBox cb &&
                        cb.Checked &&
                        cb.Tag
                            is Input.LibroBiblico libroCorrente)
                    {
                        selezionati.Add(
                            libroCorrente.BookNumber);
                    }
                }
            }

            return selezionati;
        }

        private void
            ApplicaSelezioneDefaultLibriRicerca(
                HashSet<int> libriConOccorrenze)
        {
            PreparaListeLibriTestamento();

            aggiornamentoListaLibriInCorso =
                true;

            aggiornamentoFiltroTestamentoInCorso =
                true;

            try
            {
                bool atHaOccorrenze =
                    ImpostaSelezioneLibriPerOccorrenze(
                        flowLibriAT,
                        checkSelezionaTuttoAT,
                        libriConOccorrenze);

                bool ntHaOccorrenze =
                    ImpostaSelezioneLibriPerOccorrenze(
                        flowLibriNT,
                        checkSelezionaTuttoNT,
                        libriConOccorrenze);

                checkBox_AT.Checked =
                    atHaOccorrenze;

                checkBox_NT.Checked =
                    ntHaOccorrenze;
            }
            finally
            {
                aggiornamentoFiltroTestamentoInCorso =
                    false;

                aggiornamentoListaLibriInCorso =
                    false;
            }
        }

        private bool
            ImpostaSelezioneLibriPerOccorrenze(
                Panel flow,
                CheckBox checkSelezionaTutto,
                HashSet<int> libriConOccorrenze)
        {
            bool almenoUnLibroSelezionato =
                false;

            foreach (CheckBox checkBoxLibro
                in flow.Controls)
            {
                if (checkBoxLibro.Tag
                    is not Input.LibroBiblico libroCorrente)
                    continue;

                bool haOccorrenze =
                    libriConOccorrenze.Contains(
                        libroCorrente.BookNumber);

                checkBoxLibro.Checked =
                    haOccorrenze;

                if (haOccorrenze)
                    almenoUnLibroSelezionato = true;
            }

            AggiornaCheckSelezionaTutto(
                flow,
                checkSelezionaTutto);

            return almenoUnLibroSelezionato;
        }

        private static string FormattaContatori(
            int versetti,
            int note)
        {
            if (versetti <= 0 && note <= 0)
                return "";

            string v = versetti > 0 ? versetti.ToString() : "";
            string n = note > 0 ? note.ToString() : "";

            return $" ({v}/{n})";
        }

        private void AggiornaContatoriRicerca()
        {
            void Applica(
                Panel flow,
                CheckBox checkBoxTestamento,
                string sigla)
            {
                int totVersetti = 0;
                int totNote = 0;

                if (flow != null)
                {
                    foreach (CheckBox cb in
                        flow.Controls.OfType<CheckBox>())
                    {
                        if (cb.Tag is not Input.LibroBiblico libroCorrente)
                            continue;

                        versettiPerLibroRicerca.TryGetValue(
                            libroCorrente.BookNumber,
                            out int v);

                        notePerLibroRicerca.TryGetValue(
                            libroCorrente.BookNumber,
                            out int n);

                        totVersetti += v;
                        totNote += n;

                        cb.Visible = v > 0 || n > 0;

                        cb.Text =
                            Input.LibriCEI.NomeVisualizzato(
                                libroCorrente) +
                            FormattaContatori(v, n);
                    }
                }

                checkBoxTestamento.Text =
                    sigla +
                    FormattaContatori(totVersetti, totNote);
            }

            Applica(flowLibriAT, checkBox_AT, "AT");
            Applica(flowLibriNT, checkBox_NT, "NT");

            RiposizionaControlliTestamento();

            AdattaDimensioniListeLibri();
            AggiornaAltezzaPanelHeader();
        }

        private void RiposizionaControlliTestamento()
        {
            int gapPiccolo = LogicalToDeviceUnits(6);
            int gapGrande = LogicalToDeviceUnits(42);

            bottoneEspandiAT.Left =
                checkBox_AT.Right + gapPiccolo;

            checkBox_NT.Left =
                bottoneEspandiAT.Right + gapGrande;

            bottoneEspandiNT.Left =
                checkBox_NT.Right + gapPiccolo;
        }

        private int CalcolaLarghezzaColonnaLibro()
        {
            int max = 0;

            foreach (var l in Input.LibriCEI.Elenco)
            {
                string t =
                    Input.LibriCEI.NomeVisualizzato(l) +
                    " (999/99)";

                max = Math.Max(
                    max,
                    TextRenderer.MeasureText(t, Font).Width);
            }

            return max + LogicalToDeviceUnits(32);
        }

        private int OccorrenzeLibro(CheckBox cb)
        {
            if (cb.Tag is not Input.LibroBiblico libroCorrente)
                return 0;

            versettiPerLibroRicerca.TryGetValue(
                libroCorrente.BookNumber, out int v);

            notePerLibroRicerca.TryGetValue(
                libroCorrente.BookNumber, out int n);

            return v + n;
        }

        private static string NomeLibroPerOrdine(CheckBox cb)
        {
            return cb.Tag is Input.LibroBiblico libroCorrente
                ? Input.LibriCEI.NomeVisualizzato(libroCorrente)
                : cb.Text;
        }

        private bool LibroHaRisultatiRicerca(CheckBox cb)
        {
            if (cb.Tag is not Input.LibroBiblico libroCorrente)
                return false;

            versettiPerLibroRicerca.TryGetValue(
                libroCorrente.BookNumber, out int v);

            notePerLibroRicerca.TryGetValue(
                libroCorrente.BookNumber, out int n);

            return v > 0 || n > 0;
        }

        private const int RigheMassimeListaLibri = 12;

        private int OffsetVerticaleListaLibri => ScalaDesign(27);

        private void AdattaDimensioniListeLibri()
        {
            if (contenitoreListaLibriAT == null ||
                contenitoreListaLibriNT == null ||
                flowLibriAT == null ||
                flowLibriNT == null ||
                panelTesti == null)
                return;

            int larghezzaPannello =
                panelHeader.ClientSize.Width -
                panelHeader.Padding.Horizontal;

            if (larghezzaPannello > 0)
                panelTesti.Width = larghezzaPannello;

            const int margineOrizzontaleLibro = 3 + 8;

            int disponibile =
                panelTesti.ClientSize.Width -
                contenitoreListaLibriAT.Left - 3;

            if (disponibile <= 0)
                return;

            var tutti =
                flowLibriAT.Controls.OfType<CheckBox>()
                    .Concat(
                        flowLibriNT.Controls.OfType<CheckBox>())
                    .ToList();

            var conRisultati =
                tutti.Where(LibroHaRisultatiRicerca).ToList();

            int larghezzaColonna =
                conRisultati.Count == 0
                    ? 60
                    : conRisultati.Max(
                        cb => TextRenderer.MeasureText(
                            cb.Text, cb.Font).Width) +
                      LogicalToDeviceUnits(32);

            int spazioColonna =
                larghezzaColonna + margineOrizzontaleLibro;

            int colonneChePossonoEntrare =
                Math.Max(1, (disponibile - 8) / spazioColonna);

            foreach (CheckBox cb in tutti)
                cb.Width = larghezzaColonna;

            int altezzaSingolaRiga = Font.Height + 12;
            int altezzaHeaderSeleziona = altezzaSingolaRiga + 16;
            int altezzaRigaFlow = altezzaSingolaRiga + 4;

            void Adatta(
                Panel contenitore,
                Panel flow)
            {
                var libri =
                    flow.Controls.OfType<CheckBox>().ToList();

                int quanti =
                    libri.Count(LibroHaRisultatiRicerca);

                flow.SuspendLayout();

                // Con risultati di ricerca: prima i libri con piu'
                // occorrenze (versetti + note), a parita' ordine alfabetico.
                if (quanti > 0)
                {
                    var ordinati =
                        libri
                            .OrderByDescending(OccorrenzeLibro)
                            .ThenBy(
                                cb => NomeLibroPerOrdine(cb),
                                StringComparer.Create(
                                    new System.Globalization.CultureInfo("it-IT"),
                                    true))
                            .ToList();

                    for (int i = 0; i < ordinati.Count; i++)
                        flow.Controls.SetChildIndex(ordinati[i], i);

                    libri = ordinati;
                }

                int colonneIdeali =
                    Math.Max(
                        ColonneLibriMinimeVisibili,
                        (int)Math.Ceiling(
                            quanti /
                            (double)RigheMassimeListaLibri));

                int colonne =
                    Math.Max(
                        1,
                        Math.Min(
                            colonneIdeali,
                            colonneChePossonoEntrare));

                // La lista non deve superare lo spazio della finestra,
                // altrimenti spinge fuori schermo i risultati.
                int altezzaMinima =
                    altezzaHeaderSeleziona +
                    (altezzaRigaFlow * 3) + 10;

                int altezzaMassima =
                    Math.Max(
                        altezzaMinima,
                        ClientSize.Height -
                        AltezzaBasePanelHeader -
                        OffsetVerticaleListaLibri -
                        ScalaDesign(150));

                // Righe che entrano davvero in altezza (si riserva sempre
                // lo spazio della barra orizzontale).
                int righeVisibili =
                    Math.Max(
                        3,
                        (altezzaMassima -
                         altezzaHeaderSeleziona - 10 -
                         SystemInformation.HorizontalScrollBarHeight) /
                        altezzaRigaFlow);

                int righeBilanciate =
                    Math.Max(
                        1,
                        (int)Math.Ceiling(
                            quanti / (double)colonne));

                // Colonne sempre piene dall'alto verso il basso: la
                // successiva inizia solo quando la precedente e' completa.
                int righe =
                    Math.Max(
                        1,
                        Math.Min(righeBilanciate, righeVisibili));

                int colonneReali =
                    Math.Max(
                        1,
                        (int)Math.Ceiling(
                            quanti / (double)righe));

                // Posizione esplicita di ogni libro: colonne piene dall'alto
                // verso il basso, la successiva inizia solo quando la
                // precedente e' completa. Non dipende da nessun layout
                // automatico, quindi la barra orizzontale non puo' alterarla.
                const int origineX = 5;
                const int origineY = 4;

                int indiceVisibile = 0;

                foreach (CheckBox cb in libri)
                {
                    if (!LibroHaRisultatiRicerca(cb))
                        continue;

                    int colonna = indiceVisibile / righe;
                    int riga = indiceVisibile % righe;

                    cb.Location =
                        new Point(
                            origineX + colonna * spazioColonna,
                            origineY + riga * altezzaRigaFlow);

                    indiceVisibile++;
                }

                int larghezzaContenuto =
                    origineX + colonneReali * spazioColonna;

                flow.AutoScrollMinSize =
                    new Size(larghezzaContenuto, 0);

                bool serveScrollOrizzontale =
                    larghezzaContenuto > disponibile;

                contenitore.Width = disponibile;

                int altezzaNecessaria =
                    altezzaHeaderSeleziona +
                    (altezzaRigaFlow * righe) +
                    10 +
                    (serveScrollOrizzontale
                        ? SystemInformation.HorizontalScrollBarHeight
                        : 0);

                contenitore.Height =
                    Math.Min(altezzaNecessaria, altezzaMassima);

                flow.ResumeLayout(true);
                flow.AutoScrollPosition = new Point(0, 0);
            }

            Adatta(contenitoreListaLibriAT, flowLibriAT);
            Adatta(contenitoreListaLibriNT, flowLibriNT);
        }

        private void
            AlternaEspansioneLista(
                Panel contenitoreDaAprire,
                Panel contenitoreDaChiudere,
                Button bottoneAperto,
                Button bottoneChiuso)
        {
            if (contenitoreDaChiudere != null &&
                contenitoreDaChiudere.Visible)
            {
                contenitoreDaChiudere.Visible =
                    false;

                if (bottoneChiuso != null)
                {
                    bottoneChiuso.Text =
                        "▾ Libri";
                }
            }

            contenitoreDaAprire.Visible =
                !contenitoreDaAprire.Visible;

            if (contenitoreDaAprire.Visible)
                AdattaDimensioniListeLibri();

            bottoneAperto.Text =
                contenitoreDaAprire.Visible
                    ? "▴ Libri"
                    : "▾ Libri";

            RiposizionaListeLibri();
            AggiornaAltezzaPanelHeader();
        }

        private void
            ComprimiListaSeEspansa(
                Panel contenitore,
                Button bottoneEspandi)
        {
            if (contenitore == null ||
                !contenitore.Visible)
                return;

            contenitore.Visible =
                false;

            bottoneEspandi.Text =
                "▾ Libri";

            RiposizionaListeLibri();
            AggiornaAltezzaPanelHeader();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (contenitoreListaLibriAT == null ||
                contenitoreListaLibriNT == null)
                return;

            if (!contenitoreListaLibriAT.Visible &&
                !contenitoreListaLibriNT.Visible)
                return;

            AdattaDimensioniListeLibri();
            RiposizionaListeLibri();
            AggiornaAltezzaPanelHeader();
        }

        private void
            RiposizionaListeLibri()
        {
            if (contenitoreListaLibriAT == null)
                return;

            int y = ScalaDesign(205) + OffsetVerticaleListaLibri;

            contenitoreListaLibriAT.Location =
                new Point(3, y);

            contenitoreListaLibriNT.Location =
                new Point(3, y);
        }

        private void
            AggiornaAltezzaPanelHeader()
        {
            int extra = 0;

            if (contenitoreListaLibriAT != null &&
                contenitoreListaLibriAT.Visible)
            {
                extra =
                    contenitoreListaLibriAT.Height +
                    OffsetVerticaleListaLibri;
            }
            else if (contenitoreListaLibriNT != null &&
                     contenitoreListaLibriNT.Visible)
            {
                extra =
                    contenitoreListaLibriNT.Height +
                    OffsetVerticaleListaLibri;
            }

            panelHeader.Height =
                AltezzaBasePanelHeader +
                extra;

            if (panelTesti != null)
            {
                panelTesti.Height =
                    panelHeader.Height -
                    panelHeader.Padding.Top -
                    panelHeader.Padding.Bottom;
            }

            pictureBoxHeader.BringToFront();
        }

        private void
            MostraControlliFiltroTestamento()
        {
            checkBox_AT.Visible = true;
            checkBox_NT.Visible = true;

            PreparaListeLibriTestamento();

            bottoneEspandiAT.Visible = true;
            bottoneEspandiNT.Visible = true;
        }

        private void
            NascondiListeLibri()
        {
            checkBox_AT.Visible = false;
            checkBox_NT.Visible = false;

            if (bottoneEspandiAT != null)
                bottoneEspandiAT.Visible = false;

            if (bottoneEspandiNT != null)
                bottoneEspandiNT.Visible = false;

            if (contenitoreListaLibriAT != null)
                contenitoreListaLibriAT.Visible = false;

            if (contenitoreListaLibriNT != null)
                contenitoreListaLibriNT.Visible = false;

            AggiornaAltezzaPanelHeader();
        }

        private void
            bottoneEspandiAT_Click(
                object sender,
                EventArgs e)
        {
            AlternaEspansioneLista(
                contenitoreListaLibriAT,
                contenitoreListaLibriNT,
                bottoneEspandiAT,
                bottoneEspandiNT);
        }

        private void
            bottoneEspandiNT_Click(
                object sender,
                EventArgs e)
        {
            AlternaEspansioneLista(
                contenitoreListaLibriNT,
                contenitoreListaLibriAT,
                bottoneEspandiNT,
                bottoneEspandiAT);
        }

        private async void
            button_altriRisultatiRicerca_Click(
                object sender,
                EventArgs e)
        {
            if (!button_altriRisultatiRicerca.Enabled)
                return;

            button_altriRisultatiRicerca.Enabled =
                false;

            await CaricaPaginaRicercaAsync(
                false,
                generazioneRicercaCorrente);
        }

        private void
            AggiornaPulsanteAltriRisultati()
        {
            int rimanenti =
                Math.Max(
                    0,
                    totaleUnitaRicercaCorrispondenti -
                    offsetPaginaRicercaCorrente);

            int daMostrare =
                Math.Min(
                    DimensionePaginaRicerca,
                    rimanenti);

            button_altriRisultatiRicerca.Text =
                daMostrare == 1
                    ? "Mostra altro 1 risultato"
                    : $"Mostra altri {daMostrare} risultati";

            button_altriRisultatiRicerca.Visible =
                ricercaHaAltriRisultati;

            button_altriRisultatiRicerca.Enabled =
                true;
        }

        private void
            NascondiPulsanteAltriRisultati()
        {
            if (button_altriRisultatiRicerca != null)
                button_altriRisultatiRicerca.Visible =
                    false;
        }

        private void
            ApriMappaPerRicerca(
                List<(
                    Input.LibroBiblico Libro,
                    int Capitolo,
                    int VersettoDa,
                    int? VersettoA)> occorrenze,
                string testoDellaRicerca)
        {
            if (formMappaAperta != null &&
                !formMappaAperta.IsDisposed)
            {
                formMappaAperta.CentraSuRicerca(
                    occorrenze,
                    testoDellaRicerca);

                return;
            }

            formMappaAperta =
                new FormMappaConcettuale(
                    occorrenze,
                    testoDellaRicerca,
                    connectionStringBibbia,
                    connectionStringNote,
                    NavigaVersoRiferimento);

            formMappaAperta.FormClosed +=
                (s, e) =>
                    formMappaAperta = null;

            formMappaAperta.Show(this);
        }

        private void
            PreparaGriglia()
        {
            dataGridView1.Columns.Clear();
            dataGridView1.Rows.Clear();

            dataGridView1.ColumnHeadersVisible =
                false;

            dataGridView1.Columns.Add(
                "Contenuto",
                "Contenuto");

            dataGridView1.Columns[0]
                .AutoSizeMode =
                DataGridViewAutoSizeColumnMode.Fill;

            dataGridView1.DefaultCellStyle.WrapMode =
                DataGridViewTriState.True;

            dataGridView1.DefaultCellStyle.SelectionBackColor =
                TemaBibbia.SelezioneRiga;

            dataGridView1.DefaultCellStyle.SelectionForeColor =
                TemaBibbia.Avorio;

            dataGridView1.AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.None;

            dataGridView1.EnableHeadersVisualStyles =
                false;
        }

        private void
            dataGridView1_CellPainting(
                object sender,
                DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 ||
                e.ColumnIndex < 0)
                return;

            List<(int Start, int Length)>
                intervalli;

            List<(int Start, int Length)>
                intervalliRiferimenti = null;

            List<(int Start, int Length)>
                intervalliTag = null;

            if (dataGridView1.Rows[e.RowIndex].Tag
                is RigaVersettoInfo rigaVersetto)
            {
                intervalli =
                    rigaVersetto.Intervalli;
            }
            else if (
                dataGridView1.Rows[e.RowIndex].Tag
                is RigaNotaInfo rigaNota)
            {
                intervalli =
                    rigaNota.Intervalli;

                intervalliRiferimenti =
                    rigaNota.Riferimenti
                        .Select(
                            r => (r.Start, r.Length))
                        .ToList();

                intervalliTag =
                    rigaNota.Tags
                        .Select(
                            t => (t.Start, t.Length))
                        .ToList();
            }
            else
            {
                return;
            }

            e.PaintBackground(
                e.ClipBounds,
                true);

            string testo =
                e.FormattedValue as string ??
                "";

            var font =
                e.CellStyle.Font ??
                dataGridView1.Font;

            var colore =
                e.CellStyle.ForeColor;

            DisegnaTestoEvidenziato(
                e.Graphics,
                e.CellBounds,
                testo,
                font,
                colore,
                intervalli,
                intervalliRiferimenti,
                intervalliTag);

            e.Handled = true;
        }

        private static readonly StringFormat
            FormatoTesto =
            CreaFormatoTesto();

        private static StringFormat
            CreaFormatoTesto()
        {
            var formato =
                new StringFormat(
                    StringFormat.GenericTypographic);

            formato.FormatFlags |=
                StringFormatFlags.MeasureTrailingSpaces;

            return formato;
        }

        private void
            DisegnaTestoEvidenziato(
                Graphics g,
                Rectangle bounds,
                string testo,
                Font font,
                Color colore,
                List<(int Start, int Length)> intervalli,
                List<(int Start, int Length)> intervalliRiferimenti = null,
                List<(int Start, int Length)> intervalliTag = null)
        {
            var area =
                Rectangle.Inflate(
                    bounds,
                    -4,
                    -2);

            float lineHeight =
                font.GetHeight(g);

            float x =
                area.Left;

            float y =
                area.Top;

            using var fontIcona =
                testo.StartsWith("📝")
                    ? new Font(
                        "Segoe UI Emoji",
                        font.Size)
                    : null;

            var parole =
                Regex.Split(
                    testo,
                    "(?<=[ \\n])");

            int indiceCarattere = 0;

            foreach (var parola in parole)
            {
                if (parola.Length == 0)
                    continue;

                var fontParola =
                    fontIcona != null &&
                    ContieneIconaNota(parola)
                        ? fontIcona
                        : font;

                if (parola == "\n" ||
                    parola.EndsWith("\n"))
                {
                    string senzaACapo =
                        parola.TrimEnd('\n');

                    if (senzaACapo.Length > 0)
                    {
                        DisegnaParolaEvidenziata(
                            g,
                            ref x,
                            y,
                            senzaACapo,
                            fontParola,
                            colore,
                            indiceCarattere,
                            intervalli,
                            intervalliRiferimenti,
                            intervalliTag);
                    }

                    indiceCarattere +=
                        parola.Length;

                    x = area.Left;
                    y += lineHeight;

                    continue;
                }

                var misuraParola =
                    g.MeasureString(
                        parola,
                        fontParola,
                        int.MaxValue,
                        FormatoTesto);

                if (x + misuraParola.Width >
                    area.Right &&
                    x > area.Left)
                {
                    x = area.Left;
                    y += lineHeight;
                }

                DisegnaParolaEvidenziata(
                    g,
                    ref x,
                    y,
                    parola,
                    fontParola,
                    colore,
                    indiceCarattere,
                    intervalli,
                    intervalliRiferimenti,
                    intervalliTag);

                indiceCarattere +=
                    parola.Length;
            }
        }

        private static bool
            ContieneIconaNota(
                string parola)
        {
            return parola.StartsWith("📝");
        }

        private void
            DisegnaParolaEvidenziata(
                Graphics g,
                ref float x,
                float y,
                string parola,
                Font font,
                Color colore,
                int indiceBase,
                List<(int Start, int Length)> intervalli,
                List<(int Start, int Length)> intervalliRiferimenti = null,
                List<(int Start, int Length)> intervalliTag = null)
        {
            int pos = 0;

            while (pos < parola.Length)
            {
                int assoluto =
                    indiceBase + pos;

                bool evidenziato =
                    intervalli.Any(
                        iv =>
                            assoluto >= iv.Start &&
                            assoluto <
                                iv.Start + iv.Length);

                bool inRiferimento =
                    intervalliRiferimenti != null &&
                    intervalliRiferimenti.Any(
                        iv =>
                            assoluto >= iv.Start &&
                            assoluto <
                                iv.Start + iv.Length);

                bool inTag =
                    intervalliTag != null &&
                    intervalliTag.Any(
                        iv =>
                            assoluto >= iv.Start &&
                            assoluto <
                                iv.Start + iv.Length);

                int fine =
                    pos + 1;

                while (fine < parola.Length)
                {
                    int assolutoFine =
                        indiceBase + fine;

                    bool fineEvidenziata =
                        intervalli.Any(
                            iv =>
                                assolutoFine >= iv.Start &&
                                assolutoFine <
                                    iv.Start + iv.Length);

                    bool fineInRiferimento =
                        intervalliRiferimenti != null &&
                        intervalliRiferimenti.Any(
                            iv =>
                                assolutoFine >= iv.Start &&
                                assolutoFine <
                                    iv.Start + iv.Length);

                    bool fineInTag =
                        intervalliTag != null &&
                        intervalliTag.Any(
                            iv =>
                                assolutoFine >= iv.Start &&
                                assolutoFine <
                                    iv.Start + iv.Length);

                    if (fineEvidenziata != evidenziato ||
                        fineInRiferimento != inRiferimento ||
                        fineInTag != inTag)
                        break;

                    fine++;
                }

                string segmento =
                    parola.Substring(
                        pos,
                        fine - pos);

                var misura =
                    g.MeasureString(
                        segmento,
                        font,
                        int.MaxValue,
                        FormatoTesto);

                if (evidenziato)
                {
                    using var giallo =
                        new SolidBrush(
                            TemaBibbia.Evidenziatore);

                    g.FillRectangle(
                        giallo,
                        x,
                        y,
                        misura.Width,
                        font.GetHeight(g));
                }

                Color coloreSegmento =
                    evidenziato
                        ? TemaBibbia.TestoEvidenziato
                        : inTag
                            ? TemaBibbia.OroChiaro
                            : inRiferimento
                                ? TemaBibbia.Riferimento
                                : colore;

                using var pennello =
                    new SolidBrush(
                        coloreSegmento);

                g.DrawString(
                    segmento,
                    font,
                    pennello,
                    x,
                    y,
                    FormatoTesto);

                // I tag sono sottolineati: si capisce che sono cliccabili.
                if (inTag)
                {
                    using var penna =
                        new Pen(coloreSegmento);

                    float yLinea =
                        y + font.GetHeight(g) - 1;

                    g.DrawLine(
                        penna,
                        x,
                        yLinea,
                        x + misura.Width,
                        yLinea);
                }

                x += misura.Width;

                pos = fine;
            }
        }

        private void
            dataGridView1_CellMouseDown(
                object sender,
                DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            if (e.Button == MouseButtons.Left)
            {
                if (dataGridView1.Rows[e.RowIndex].Tag
                    is RigaTitoloInfo rigaTitolo &&
                    rigaTitolo.Libro != null)
                {
                    NavigaVersoRiferimento(
                        rigaTitolo.Libro,
                        rigaTitolo.Capitolo,
                        rigaTitolo.VersettoDa,
                        rigaTitolo.VersettoA);
                }
                else
                {
                    var tagCliccato =
                        TrovaTagAlPunto(
                            e.RowIndex,
                            e.ColumnIndex,
                            e.X,
                            e.Y);

                    if (tagCliccato != null)
                        AvviaRicercaTag(tagCliccato.Nome);
                }

                return;
            }

            if (e.Button != MouseButtons.Right)
                return;

            dataGridView1.ClearSelection();

            dataGridView1.Rows[e.RowIndex]
                .Selected = true;

            var menu =
                new ContextMenuStrip();

            TemaBibbia.Applica(menu);

            var tagRiga =
                dataGridView1.Rows[e.RowIndex].Tag;

            if (tagRiga is RigaNotaInfo rigaNota)
            {
                var nota =
                    rigaNota.Nota;

                var modifica =
                    new ToolStripMenuItem(
                        "Modifica commento biblico");

                modifica.Click +=
                    (s, args) =>
                        ModificaCommento(nota);

                menu.Items.Add(modifica);

                var elimina =
                    new ToolStripMenuItem(
                        "Elimina commento biblico");

                elimina.Click +=
                    (s, args) =>
                        EliminaCommento(nota);

                menu.Items.Add(elimina);

                menu.Items.Add(
                    new ToolStripSeparator());

                var copiaNota =
                    new ToolStripMenuItem(
                        "Copia");

                copiaNota.Click +=
                    (s, args) =>
                        CopiaTesto(nota.Testo);

                menu.Items.Add(copiaNota);

                if (rigaNota.Riferimenti.Count > 0)
                {
                    menu.Items.Add(
                        new ToolStripSeparator());

                    foreach (var rif in
                        rigaNota.Riferimenti)
                    {
                        var voceRiferimento =
                            new ToolStripMenuItem(
                                $"Cf. {rif.Etichetta}");

                        voceRiferimento.Click +=
                            (s, args) =>
                                NavigaVersoRiferimento(
                                    rif.Libro,
                                    rif.Capitolo,
                                    rif.VersettoDa,
                                    rif.VersettoA);

                        menu.Items.Add(
                            voceRiferimento);
                    }
                }

                var nomiTag =
                    new List<string>();

                foreach (var occorrenzaTag in
                    rigaNota.Tags)
                {
                    if (!nomiTag.Any(
                        n => TagNote.Uguali(
                            n,
                            occorrenzaTag.Nome)))
                    {
                        nomiTag.Add(
                            occorrenzaTag.Nome);
                    }
                }

                if (nomiTag.Count > 0)
                {
                    menu.Items.Add(
                        new ToolStripSeparator());

                    foreach (var nomeTag in nomiTag)
                    {
                        string nomeDaCercare =
                            nomeTag;

                        var voceTag =
                            new ToolStripMenuItem(
                                $"Cerca #{nomeDaCercare}");

                        voceTag.Click +=
                            (s, args) =>
                                AvviaRicercaTag(
                                    nomeDaCercare);

                        menu.Items.Add(voceTag);
                    }
                }
            }
            else if (
                tagRiga is RigaVersettoInfo)
            {
                // Per il copyright del testo CEI, Copia e Crea PDF
                // riguardano solo il versetto cliccato con i suoi
                // commenti.
                int indiceRigaVersetto =
                    e.RowIndex;

                var copiaVersetto =
                    new ToolStripMenuItem(
                        "Copia");

                copiaVersetto.Click +=
                    (s, args) =>
                        CopiaTesto(
                            CostruisciTestoCopiaVersetto(
                                indiceRigaVersetto));

                menu.Items.Add(
                    copiaVersetto);

                menu.Items.Add(
                    new ToolStripSeparator());

                var creaPdf =
                    new ToolStripMenuItem(
                        "Crea PDF");

                creaPdf.Click +=
                    (s, args) =>
                        CreaPdf(indiceRigaVersetto);

                menu.Items.Add(creaPdf);
            }
            else if (
                tagRiga is RigaTitoloInfo)
            {
                int indiceRigaTitolo =
                    e.RowIndex;

                var creaPdf =
                    new ToolStripMenuItem(
                        "Crea PDF");

                creaPdf.Click +=
                    (s, args) =>
                        CreaPdf(indiceRigaTitolo);

                menu.Items.Add(creaPdf);
            }

            if (menu.Items.Count > 0)
            {
                menu.Show(
                    dataGridView1,
                    e.Location);
            }
        }

        private string
            CostruisciBloccoVersetto(
                int indiceRiga)
        {
            if (indiceRiga < 0 ||
                indiceRiga >= dataGridView1.Rows.Count)
                return "";

            int inizio = indiceRiga;

            while (inizio > 0 &&
                   dataGridView1.Rows[inizio].Tag
                       is not RigaTitoloInfo)
            {
                inizio--;
            }

            var righe = new List<string>();

            for (int i = inizio;
                 i < dataGridView1.Rows.Count;
                 i++)
            {
                var riga = dataGridView1.Rows[i];

                if (i > inizio &&
                    riga.Tag is RigaTitoloInfo)
                    break;

                if (riga.Tag is RigaTitoloInfo)
                {
                    righe.Add(
                        Convert.ToString(
                            riga.Cells[0].Value) ?? "");
                }
                else if (riga.Tag is RigaVersettoInfo v)
                {
                    righe.Add(v.Testo ?? "");
                }
                else if (riga.Tag is RigaNotaInfo n)
                {
                    // Solo il commento com'e' scritto: espandere i
                    // rimandi aggiungerebbe altri versetti CEI.
                    righe.Add(
                        "Nota: " +
                        (n.Nota.Testo ?? ""));
                }
            }

            return string.Join(
                Environment.NewLine +
                Environment.NewLine,
                righe);
        }

        private string
            EspandiRiferimentiInNota(
                RigaNotaInfo rigaNota)
        {
            string testo =
                rigaNota.Nota.Testo ?? "";

            var inserimenti =
                new List<(int Posizione, string Testo)>();

            foreach (var rif in
                     rigaNota.Riferimenti)
            {
                if (rif.Libro == null)
                    continue;

                int inizio =
                    rif.Start - OffsetIconaNota;

                int fine =
                    inizio + rif.Length;

                if (inizio < 0 ||
                    fine > testo.Length ||
                    rif.Length <= 0)
                    continue;

                int apertaPrima =
                    testo.LastIndexOf('[', Math.Max(0, inizio));

                int chiusaPrima =
                    testo.LastIndexOf(']', Math.Max(0, inizio));

                bool inizioNelleQuadre =
                    apertaPrima >= 0 &&
                    apertaPrima >= chiusaPrima;

                if (!inizioNelleQuadre &&
                    testo[inizio] != '[')
                    continue;

                int punto = fine;

                if (testo[fine - 1] == ']')
                    punto = fine - 1;

                string testoBiblico =
                    OttieniSoloTestoRiferimento(rif);

                if (string.IsNullOrEmpty(testoBiblico))
                    continue;

                inserimenti.Add(
                    (punto,
                     $": «{testoBiblico}»"));
            }

            foreach (var ins in
                     inserimenti
                        .OrderByDescending(x => x.Posizione))
            {
                testo =
                    testo.Insert(
                        ins.Posizione,
                        ins.Testo);
            }

            return testo;
        }

        private string
            OttieniSoloTestoRiferimento(
                RiferimentoBiblico rif)
        {
            var testi = new List<string>();

            try
            {
                using var conn =
                    new SqliteConnection(
                        connectionStringBibbia);

                conn.Open();

                using var cmd =
                    conn.CreateCommand();

                cmd.CommandText = @"
                    SELECT text
                    FROM verses
                    WHERE book_number=@bn
                      AND chapter=@cap
                      AND verse BETWEEN @da AND @a
                    ORDER BY verse;";

                cmd.Parameters.AddWithValue(
                    "@bn",
                    rif.Libro.BookNumber);

                cmd.Parameters.AddWithValue(
                    "@cap",
                    rif.Capitolo);

                cmd.Parameters.AddWithValue(
                    "@da",
                    rif.VersettoDa);

                cmd.Parameters.AddWithValue(
                    "@a",
                    rif.VersettoA);

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    testi.Add(
                        Regex.Replace(
                            reader.GetString(0),
                            "<[^>]+>",
                            "").Trim());
                }
            }
            catch (SqliteException)
            {
            }

            return string.Join(" ", testi);
        }

        private void
            CopiaTesto(string testo)
        {
            if (string.IsNullOrEmpty(testo))
                return;

            try
            {
                Clipboard.SetText(testo);
            }
            catch
            {
            }
        }

        private async void
            ModificaCommento(
                NotaInfo nota)
        {
            // Misure pensate per font di base 18 pt a 96 DPI: seguono
            // la dimensione del font e la scala di Windows.
            float fattoreDialogo =
                (Font.SizeInPoints / 18f) * (DeviceDpi / 96f);

            int Sc(int px) => (int)Math.Round(px * fattoreDialogo);

            using var dialog =
                new Form
                {
                    Text =
                        "Modifica commento biblico",

                    StartPosition =
                        FormStartPosition.CenterParent,

                    ClientSize =
                        new Size(Sc(720), Sc(390)),

                    MinimizeBox = false,
                    MaximizeBox = false,

                    FormBorderStyle =
                        FormBorderStyle.FixedDialog
                };

            TemaBibbia.Applica(dialog);

            var testo =
                new TextBox
                {
                    Multiline = true,
                    AcceptsReturn = true,
                    ScrollBars =
                        ScrollBars.Vertical,

                    Dock =
                        DockStyle.Fill,

                    Text =
                        nota.Testo
                };

            var rotellinaTesto =
                new RotellinaAccorpata(testo);

            UndoTesto.Collega(testo, 10);

            // Suggerisce i tag gia' usati mentre si scrive "#...". Va
            // collegato prima del KeyDown sotto: quando il suggerimento e'
            // aperto Invio sceglie il tag invece di salvare.
            var tagDisponibili =
                TagNote.LeggiTuttiITag(
                    connectionStringNote);

            SuggerimentiTag.Collega(
                testo,
                () => tagDisponibili);

            var idBlocco = LeggiBloccoNote(nota.Id);
            var idNuovoOrdine = new List<long>(idBlocco);
            int posizioneIniziale = idBlocco.IndexOf(nota.Id);

            var pannello =
                new Panel
                {
                    Dock =
                        DockStyle.Bottom,

                    Height = Sc(75)
                };

            var ok =
                new Button
                {
                    Text = "SALVA",
                    DialogResult =
                        DialogResult.OK,

                    Width = Sc(150),
                    Height = Sc(50),

                    Left = Sc(380),
                    Top = Sc(10)
                };

            var annulla =
                new Button
                {
                    Text = "ANNULLA",
                    DialogResult =
                        DialogResult.Cancel,

                    Width = Sc(150),
                    Height = Sc(50),

                    Left = Sc(550),
                    Top = Sc(10)
                };

            pannello.Controls.Add(ok);
            pannello.Controls.Add(annulla);

            var su = new Button
            {
                Text = "▲ SU",
                Width = Sc(110),
                Height = Sc(50),
                Left = Sc(10),
                Top = Sc(10),
                TabStop = false
            };

            var giu = new Button
            {
                Text = "▼ GIÙ",
                Width = Sc(110),
                Height = Sc(50),
                Left = Sc(130),
                Top = Sc(10),
                TabStop = false
            };

            var etichettaPosizione = new Label
            {
                AutoSize = false,
                Width = Sc(110),
                Height = Sc(50),
                Left = Sc(250),
                Top = Sc(10),
                TextAlign = ContentAlignment.MiddleCenter
            };

            void AggiornaOrdine()
            {
                int pos = idNuovoOrdine.IndexOf(nota.Id);
                su.Enabled = pos > 0;
                giu.Enabled = pos >= 0 && pos < idNuovoOrdine.Count - 1;
                etichettaPosizione.Text =
                    $"Nota {pos + 1} di {idNuovoOrdine.Count}";
            }

            void Sposta(int passo)
            {
                int pos = idNuovoOrdine.IndexOf(nota.Id);
                int dest = pos + passo;
                if (pos < 0 || dest < 0 || dest >= idNuovoOrdine.Count)
                    return;

                (idNuovoOrdine[pos], idNuovoOrdine[dest]) =
                    (idNuovoOrdine[dest], idNuovoOrdine[pos]);

                AggiornaOrdine();
                testo.Focus();
            }

            su.Click += (s, args) => Sposta(-1);
            giu.Click += (s, args) => Sposta(+1);

            if (idBlocco.Count > 1 && posizioneIniziale >= 0)
            {
                pannello.Controls.Add(su);
                pannello.Controls.Add(giu);
                pannello.Controls.Add(etichettaPosizione);
                AggiornaOrdine();
            }

            dialog.Controls.Add(testo);
            dialog.Controls.Add(pannello);

            dialog.AcceptButton = ok;
            dialog.CancelButton = annulla;

            // F1 apre la guida sul capitolo dei commenti (5), anche da questa
            // finestra di dialogo.
            dialog.KeyPreview = true;
            dialog.KeyDown +=
                (s, args) =>
                {
                    if (args.KeyCode == Keys.F1)
                    {
                        Input.FormGUIDA.Mostra(this, "c5");
                        args.Handled = true;
                        args.SuppressKeyPress = true;
                    }
                };

            testo.KeyDown +=
                (s, args) =>
                {
                    if (args.Handled ||
                        args.KeyCode != Keys.Enter)
                        return;

                    if (args.Shift || args.Control)
                        testo.SelectedText =
                            Environment.NewLine;
                    else
                        ok.PerformClick();

                    args.SuppressKeyPress = true;
                    args.Handled = true;
                };

            var esitoDialogo =
                dialog.ShowDialog(this);

            rotellinaTesto.Dispose();

            if (esitoDialogo !=
                DialogResult.OK)
                return;

            if (string.IsNullOrWhiteSpace(
                testo.Text))
            {
                MessaggioBibbia.Mostra(
                    "Il commento non può essere vuoto.",
                    "Attenzione",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var unitaDellaNota =
                TrovaTitoloDellaNota(nota.Id);

            using var conn =
                new SqliteConnection(
                    connectionStringNote);

            conn.Open();

            using var cmd =
                conn.CreateCommand();

            cmd.CommandText =
                "UPDATE Note SET TestoNota=@nota WHERE Id=@id;";

            cmd.Parameters.AddWithValue(
                "@nota",
                testo.Text.Trim());

            cmd.Parameters.AddWithValue(
                "@id",
                nota.Id);

            cmd.ExecuteNonQuery();

            if (!idNuovoOrdine.SequenceEqual(idBlocco))
                SalvaOrdineNote(conn, idBlocco, idNuovoOrdine);

            ManutenzioneIndiceNote(conn);

            aggiornaColoriInserisci?.Invoke();

            if (modalitaRicerca)
                await RicaricaRicercaMantenendoStatoAsync(
                    unitaDellaNota);
            else
                CaricaRiferimentoSingolo();
        }

        // Titolo (libro, capitolo, versetti) dell'unita' che contiene la
        // nota indicata, cosi' dopo il ricaricamento si torna li'.
        private RigaTitoloInfo TrovaTitoloDellaNota(long idNota)
        {
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Tag is RigaNotaInfo rn &&
                    rn.Nota.Id == idNota)
                {
                    for (int j = i; j >= 0; j--)
                    {
                        if (dataGridView1.Rows[j].Tag is RigaTitoloInfo t)
                            return t;
                    }

                    return null;
                }
            }

            return null;
        }

        // Ricarica la ricerca dopo la modifica/eliminazione di un commento
        // mantenendo i libri selezionati, le pagine gia' mostrate e la
        // posizione sul versetto in cui si stava lavorando.
        private async Task RicaricaRicercaMantenendoStatoAsync(
            RigaTitoloInfo unitaDaRaggiungere)
        {
            PreparaListeLibriTestamento();

            HashSet<int> libriSelezionati =
                OttieniLibriSelezionatiPerRicerca();

            bool atSelezionato = checkBox_AT.Checked;
            bool ntSelezionato = checkBox_NT.Checked;

            int unitaDaRicaricare =
                Math.Max(
                    totaleUnitaMostrateRicerca,
                    DimensionePaginaRicerca);

            int miaGenerazione =
                ++generazioneRicercaCorrente;

            offsetPaginaRicercaCorrente = 0;
            ricercaHaAltriRisultati = false;
            totaleUnitaMostrateRicerca = 0;
            totaleUnitaRicercaCorrispondenti = 0;

            mappaGiaAggiuntiRicerca.Clear();
            occorrenzeMappaRicercaAccumulate.Clear();
            chiaviUnitaGiaMostrate.Clear();

            NascondiPulsanteAltriRisultati();

            // Conteggi per libro aggiornati (senza filtro libri).
            var paginaCompleta =
                await EseguiPaginaRicercaAsync(
                    testoRicerca,
                    0,
                    true,
                    mappaGiaAggiuntiRicerca,
                    chiaviUnitaGiaMostrate,
                    null);

            if (miaGenerazione !=
                generazioneRicercaCorrente)
                return;

            libriConOccorrenzeRicercaCorrente =
                paginaCompleta.LibriConOccorrenze;

            libriConRiferimentoRicercaCorrente =
                paginaCompleta.LibriConRiferimento;

            AggiornaColoriLibriRicerca();

            versettiPerLibroRicerca =
                paginaCompleta.VersettiTrovatiPerLibro;

            notePerLibroRicerca =
                paginaCompleta.NoteTrovatePerLibro;

            AggiornaContatoriRicerca();

            // Si ripristina la selezione fatta dall'utente, invece di
            // quella di default (tutti i libri con occorrenze).
            aggiornamentoListaLibriInCorso = true;
            aggiornamentoFiltroTestamentoInCorso = true;

            try
            {
                foreach (var flow in new[] { flowLibriAT, flowLibriNT })
                {
                    if (flow == null)
                        continue;

                    foreach (CheckBox cb in
                        flow.Controls.OfType<CheckBox>())
                    {
                        if (cb.Tag is Input.LibroBiblico l)
                            cb.Checked =
                                libriSelezionati.Contains(l.BookNumber);
                    }
                }

                AggiornaCheckSelezionaTutto(
                    flowLibriAT,
                    checkSelezionaTuttoAT);

                AggiornaCheckSelezionaTutto(
                    flowLibriNT,
                    checkSelezionaTuttoNT);

                checkBox_AT.Checked = atSelezionato;
                checkBox_NT.Checked = ntSelezionato;
            }
            finally
            {
                aggiornamentoFiltroTestamentoInCorso = false;
                aggiornamentoListaLibriInCorso = false;
            }

            AdattaDimensioniListeLibri();
            AggiornaAltezzaPanelHeader();

            mappaGiaAggiuntiRicerca.Clear();
            occorrenzeMappaRicercaAccumulate.Clear();
            chiaviUnitaGiaMostrate.Clear();

            offsetPaginaRicercaCorrente = 0;
            totaleUnitaMostrateRicerca = 0;

            PreparaGriglia();

            await CaricaPaginaRicercaAsync(
                true,
                miaGenerazione);

            // Stesse pagine che erano gia' state caricate.
            while (miaGenerazione == generazioneRicercaCorrente &&
                   ricercaHaAltriRisultati &&
                   totaleUnitaMostrateRicerca < unitaDaRicaricare)
            {
                await CaricaPaginaRicercaAsync(
                    false,
                    miaGenerazione);
            }

            if (miaGenerazione != generazioneRicercaCorrente)
                return;

            if (unitaDaRaggiungere?.Libro != null)
            {
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (dataGridView1.Rows[i].Tag is RigaTitoloInfo t &&
                        t.Libro != null &&
                        t.Libro.BookNumber ==
                            unitaDaRaggiungere.Libro.BookNumber &&
                        t.Capitolo == unitaDaRaggiungere.Capitolo &&
                        t.VersettoDa == unitaDaRaggiungere.VersettoDa &&
                        t.VersettoA == unitaDaRaggiungere.VersettoA)
                    {
                        try
                        {
                            dataGridView1.FirstDisplayedScrollingRowIndex = i;
                        }
                        catch (InvalidOperationException)
                        {
                        }

                        break;
                    }
                }
            }

            RiadattaListeLibriDifferito();
        }

        private List<long> LeggiBloccoNote(long idNota)
        {
            var risultato = new List<long>();

            int indice = -1;
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (dataGridView1.Rows[i].Tag is RigaNotaInfo rn &&
                    rn.Nota.Id == idNota)
                {
                    indice = i;
                    break;
                }
            }

            if (indice < 0)
                return risultato;

            int inizio = indice;
            while (inizio > 0 && dataGridView1.Rows[inizio - 1].Tag is RigaNotaInfo)
                inizio--;

            int fine = indice;
            while (fine < dataGridView1.Rows.Count - 1 &&
                   dataGridView1.Rows[fine + 1].Tag is RigaNotaInfo)
                fine++;

            for (int i = inizio; i <= fine; i++)
            {
                long id = ((RigaNotaInfo)dataGridView1.Rows[i].Tag).Nota.Id;
                if (!risultato.Contains(id))
                    risultato.Add(id);
            }

            return risultato;
        }

        private static void SalvaOrdineNote(
            SqliteConnection conn,
            List<long> vecchioOrdine,
            List<long> nuovoOrdine)
        {
            var valori = new List<long>();

            using (var lettura = conn.CreateCommand())
            {
                lettura.CommandText =
                    "SELECT COALESCE(Ordine, Id) FROM Note WHERE Id=@id;";
                var p = lettura.Parameters.Add("@id", SqliteType.Integer);

                foreach (long id in vecchioOrdine)
                {
                    p.Value = id;
                    object o = lettura.ExecuteScalar();
                    if (o == null || o == DBNull.Value)
                        return;
                    valori.Add(Convert.ToInt64(o));
                }
            }

            valori.Sort();

            using var tran = conn.BeginTransaction();
            using var scrittura = conn.CreateCommand();
            scrittura.Transaction = tran;
            scrittura.CommandText = "UPDATE Note SET Ordine=@o WHERE Id=@id;";
            var po = scrittura.Parameters.Add("@o", SqliteType.Integer);
            var pi = scrittura.Parameters.Add("@id", SqliteType.Integer);

            for (int k = 0; k < nuovoOrdine.Count; k++)
            {
                po.Value = valori[k];
                pi.Value = nuovoOrdine[k];
                scrittura.ExecuteNonQuery();
            }

            tran.Commit();
        }

        private async void
            EliminaCommento(
                NotaInfo nota)
        {
            var conferma =
                MessaggioBibbia.Mostra(
                    "Eliminare questo commento biblico?",
                    "Conferma eliminazione",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (conferma !=
                DialogResult.Yes)
                return;

            var unitaDellaNota =
                TrovaTitoloDellaNota(nota.Id);

            using var conn =
                new SqliteConnection(
                    connectionStringNote);

            conn.Open();

            using var cmd =
                conn.CreateCommand();

            cmd.CommandText =
                "DELETE FROM Note WHERE Id=@id;";

            cmd.Parameters.AddWithValue(
                "@id",
                nota.Id);

            cmd.ExecuteNonQuery();
            ManutenzioneIndiceNote(conn);

            aggiornaColoriInserisci?.Invoke();

            if (modalitaRicerca)
                await RicaricaRicercaMantenendoStatoAsync(
                    unitaDellaNota);
            else
                CaricaRiferimentoSingolo();
        }

        private static void ManutenzioneIndiceNote(SqliteConnection conn, bool rebuild = false)
        {
            try
            {
                using var cmd = conn.CreateCommand();

                if (rebuild)
                {
                    cmd.CommandText = "INSERT INTO NoteFts(NoteFts) VALUES('rebuild');";
                    cmd.ExecuteNonQuery();
                }

                cmd.CommandText = "INSERT INTO NoteFts(NoteFts) VALUES('optimize');";
                cmd.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                using var cmdRebuild = conn.CreateCommand();
                cmdRebuild.CommandText = "INSERT INTO NoteFts(NoteFts) VALUES('rebuild');";
                cmdRebuild.ExecuteNonQuery();
            }
        }

        // Intestazione comune a Crea PDF e Copia.
        private const string TitoloProgramma =
            "Emmaus 2.0";

        private const string SottotitoloProgramma =
            "Emmaus 2.0 è un programma per Windows per meditare la Sacra Scrittura.";

        private const string CopyrightProgramma =
            "Copyright © 2026 DisabiliCristiani disabilicristiani@gmail.com - Fate del bene e pregate";

        private const string LicenzaMitProgramma =
            "Codice sorgente sotto licenza MIT.";

        private const string DicituraLicenzaCei =
            "Il testo biblico CEI 2008 © Conferenza Episcopale Italiana è soggetto a licenza CEI e non fa parte del codice sorgente rilasciato sotto licenza MIT.";

        private static string
            SottotitoloEsportazioneVersetto(
                bool conCommenti)
        {
            return conCommenti
                ? "Esportazione singolo versetto con commenti personali"
                : "Esportazione singolo versetto";
        }

        // Testo per "Copia" del versetto: stessa intestazione del PDF
        // (titolo, sottotitolo, copyright, licenze, sottotitolo di
        // esportazione) seguita dal versetto con i suoi commenti.
        private string
            CostruisciTestoCopiaVersetto(
                int indiceRiga)
        {
            string blocco =
                CostruisciBloccoVersetto(
                    indiceRiga);

            if (string.IsNullOrEmpty(blocco))
                return "";

            var parti =
                new List<string>
                {
                    TitoloProgramma,
                    SottotitoloProgramma,
                    CopyrightProgramma,
                    LicenzaMitProgramma
                };

            if (!modalitaPreghiera)
            {
                parti.Add(DicituraLicenzaCei);

                TrovaBloccoVersetto(
                    indiceRiga,
                    out int inizio,
                    out int fine);

                bool conCommenti = false;

                for (int i = inizio; i < fine; i++)
                {
                    if (dataGridView1.Rows[i].Tag
                        is RigaNotaInfo)
                    {
                        conCommenti = true;
                        break;
                    }
                }

                parti.Add(
                    SottotitoloEsportazioneVersetto(
                        conCommenti));
            }

            parti.Add(blocco);

            return string.Join(
                Environment.NewLine +
                Environment.NewLine,
                parti);
        }

        // Indice della prima riga (il titolo "Gen 1,1") e della riga
        // successiva all'ultima (fine esclusa) del versetto a cui
        // appartiene la riga cliccata: titolo, testo e commenti.
        private void
            TrovaBloccoVersetto(
                int indiceRiga,
                out int inizio,
                out int fine)
        {
            inizio = indiceRiga;

            while (inizio > 0 &&
                   dataGridView1.Rows[inizio].Tag
                       is not RigaTitoloInfo)
            {
                inizio--;
            }

            fine = inizio + 1;

            while (fine < dataGridView1.Rows.Count &&
                   dataGridView1.Rows[fine].Tag
                       is not RigaTitoloInfo)
            {
                fine++;
            }
        }

        // Per il copyright del testo CEI il PDF contiene soltanto il
        // versetto cliccato con i suoi commenti. Nella modalita'
        // preghiera (testo non CEI) contiene tutta la pagina.
        private void CreaPdf(int indiceRiga)
        {
            if (indiceRiga < 0 ||
                indiceRiga >= dataGridView1.Rows.Count)
                return;

            int inizio;
            int fine;

            if (modalitaPreghiera)
            {
                inizio = 0;
                fine = dataGridView1.Rows.Count;
            }
            else
            {
                TrovaBloccoVersetto(
                    indiceRiga,
                    out inizio,
                    out fine);

                if (dataGridView1.Rows[inizio].Tag
                    is not RigaTitoloInfo)
                    return;
            }

            string nomeBase =
                modalitaPreghiera
                    ? Text
                    : Convert.ToString(
                        dataGridView1.Rows[inizio]
                            .Cells[0].Value);

            if (string.IsNullOrWhiteSpace(nomeBase))
                nomeBase = "Bibbia";

            var caratteriNonValidi =
                Path.GetInvalidFileNameChars();

            nomeBase =
                string.Concat(
                    nomeBase.Trim().Select(
                        c => caratteriNonValidi.Contains(c)
                            ? '_'
                            : c));

            using var dialog =
                new SaveFileDialog
                {
                    Title = "Crea PDF",

                    Filter =
                        "File PDF (*.pdf)|*.pdf",

                    FileName =
                        nomeBase + ".pdf",

                    AddExtension = true
                };

            if (dialog.ShowDialog(this) !=
                DialogResult.OK)
                return;

            try
            {
                GeneraPdfConPdfSharp(
                    dialog.FileName,
                    inizio,
                    fine,
                    !modalitaPreghiera);

                MessaggioBibbia.Mostra(
                    "PDF creato correttamente.",
                    "Crea PDF",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessaggioBibbia.Mostra(
                    $"Impossibile creare il PDF: {ex.Message}",
                    "Errore PDF",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Righe da inizio (incluso) a fine (escluso). Il font del PDF e'
        // ET Book (licenza MIT, cartella Risorse\Font).
        // testoCei: il PDF contiene testo biblico CEI e lo dichiara
        // soggetto a licenza CEI, con il sottotitolo dei commenti.
        private void
            GeneraPdfConPdfSharp(
                string percorsoFile,
                int inizio,
                int fine,
                bool testoCei)
        {
            // Tipo: 0 titolo, 1 testo, 2 commento.
            var voci =
                new List<(int Tipo, string Testo)>();

            for (int i = inizio; i < fine; i++)
            {
                var riga =
                    dataGridView1.Rows[i];

                if (riga.Tag is RigaTitoloInfo)
                {
                    voci.Add(
                        (0,
                         Convert.ToString(
                             riga.Cells[0].Value) ?? ""));
                }
                else if (riga.Tag is RigaVersettoInfo v)
                {
                    voci.Add((1, v.Testo ?? ""));
                }
                else if (riga.Tag is RigaNotaInfo n)
                {
                    // Il commento com'e' scritto, senza espandere
                    // i versetti a cui rimanda.
                    voci.Add((2, n.Nota.Testo ?? ""));
                }
            }

            if (voci.Count == 0)
                throw new InvalidOperationException(
                    "nessun versetto da esportare.");

            const double margine = 50;

            const string famiglia =
                Input.RisolutoreFontPdf.FamigliaMit;

            Input.RisolutoreFontPdf.Registra();

            using var documento =
                new PdfDocument();

            documento.Info.Title =
                voci[0].Tipo == 0 && voci[0].Testo.Length > 0
                    ? $"{voci[0].Testo} - Emmaus 2.0"
                    : "Emmaus 2.0";

            documento.Info.Creator =
                "Emmaus 2.0";

            var pagina =
                documento.AddPage();

            pagina.Size =
                PdfSharp.PageSize.A4;

            var grafica =
                XGraphics.FromPdfPage(
                    pagina);

            var fontTitolo =
                new XFont(famiglia, 20, XFontStyleEx.Bold);

            var fontSottotitolo =
                new XFont(famiglia, 16, XFontStyleEx.Bold);

            var fontRiferimento =
                new XFont(famiglia, 14, XFontStyleEx.Bold);

            var fontNormale =
                new XFont(famiglia, 12, XFontStyleEx.Regular);

            var fontCorsivo =
                new XFont(famiglia, 12, XFontStyleEx.Italic);

            var fontPiede =
                new XFont(famiglia, 10, XFontStyleEx.Regular);

            var coloreNota =
                XColor.FromArgb(
                    Color.DarkSlateBlue.ToArgb());

            var pennelloNota =
                new XSolidBrush(coloreNota);

            double y = margine;

            void VaiACapoSeNecessario(
                double altezzaRiga)
            {
                if (y + altezzaRiga >
                    pagina.Height - margine)
                {
                    pagina =
                        documento.AddPage();

                    pagina.Size =
                        PdfSharp.PageSize.A4;

                    grafica.Dispose();

                    grafica =
                        XGraphics.FromPdfPage(
                            pagina);

                    y = margine;
                }
            }

            // Piccola icona di commento disegnata in vettoriale:
            // nessun font di sistema.
            void DisegnaIconaNota(
                double x,
                double alto,
                double lato)
            {
                var penna =
                    new XPen(coloreNota, 0.8);

                var carta =
                    new XSolidBrush(
                        XColor.FromArgb(
                            255, 255, 244, 179));

                grafica.DrawRectangle(
                    penna,
                    carta,
                    x + lato * 0.15,
                    alto + lato * 0.05,
                    lato * 0.7,
                    lato * 0.9);

                for (int k = 0; k < 3; k++)
                {
                    double yLinea =
                        alto + lato * (0.28 + 0.2 * k);

                    grafica.DrawLine(
                        penna,
                        x + lato * 0.28,
                        yLinea,
                        x + lato * 0.72,
                        yLinea);
                }
            }

            void ScriviParagrafo(
                string testoParagrafo,
                XFont font,
                XBrush pennello,
                double indentazione = 0,
                bool icona = false)
            {
                double altezzaRiga =
                    font.GetHeight() + 3;

                double xBase =
                    margine + indentazione;

                double xTesto =
                    xBase;

                if (icona)
                {
                    VaiACapoSeNecessario(
                        altezzaRiga);

                    double lato =
                        font.GetHeight();

                    DisegnaIconaNota(
                        xBase,
                        y,
                        lato);

                    xTesto =
                        xBase + lato + 6;
                }

                bool primaRigaParagrafo =
                    true;

                void DisegnaRigaCorrente(
                    string rigaTesto)
                {
                    double xRiga =
                        primaRigaParagrafo
                            ? xTesto
                            : xBase;

                    VaiACapoSeNecessario(
                        altezzaRiga);

                    if (rigaTesto.Length > 0)
                    {
                        grafica.DrawString(
                            rigaTesto,
                            font,
                            pennello,
                            new XPoint(
                                xRiga,
                                y + font.GetHeight()));
                    }

                    y += altezzaRiga;

                    primaRigaParagrafo =
                        false;
                }

                string normalizzato =
                    (testoParagrafo ?? "")
                        .Replace("\r\n", "\n")
                        .Replace('\r', '\n');

                foreach (var linea in
                    normalizzato.Split('\n'))
                {
                    string rigaCorrente = "";

                    foreach (var parola in
                        linea.Split(' '))
                    {
                        double xRigaCorrente =
                            primaRigaParagrafo
                                ? xTesto
                                : xBase;

                        double disponibile =
                            pagina.Width -
                            margine -
                            xRigaCorrente;

                        string prova =
                            rigaCorrente.Length == 0
                                ? parola
                                : rigaCorrente +
                                  " " +
                                  parola;

                        double larghezzaProva =
                            grafica.MeasureString(
                                prova,
                                font).Width;

                        if (larghezzaProva >
                            disponibile &&
                            rigaCorrente.Length > 0)
                        {
                            DisegnaRigaCorrente(
                                rigaCorrente);

                            rigaCorrente =
                                parola;
                        }
                        else
                        {
                            rigaCorrente =
                                prova;
                        }
                    }

                    // Anche una riga vuota occupa spazio.
                    DisegnaRigaCorrente(
                        rigaCorrente);
                }

                y += 4;
            }

            // Intestazione come nell'esportazione dei commenti da
            // Manutenzione: nome, sottotitolo, copyright e licenza.
            ScriviParagrafo(
                TitoloProgramma,
                fontTitolo,
                XBrushes.Black);

            ScriviParagrafo(
                SottotitoloProgramma,
                fontNormale,
                XBrushes.Black);

            ScriviParagrafo(
                CopyrightProgramma,
                fontCorsivo,
                XBrushes.Black);

            ScriviParagrafo(
                LicenzaMitProgramma,
                fontCorsivo,
                XBrushes.Black);

            if (testoCei)
            {
                ScriviParagrafo(
                    DicituraLicenzaCei,
                    fontCorsivo,
                    XBrushes.Black);
            }

            y += 4;

            grafica.DrawLine(
                XPens.Gray,
                margine,
                y,
                pagina.Width - margine,
                y);

            y += 14;

            if (testoCei)
            {
                ScriviParagrafo(
                    SottotitoloEsportazioneVersetto(
                        voci.Any(x => x.Tipo == 2)),
                    fontSottotitolo,
                    XBrushes.Black);

                y += 10;
            }

            bool primoTitolo = true;

            foreach (var voce in voci)
            {
                if (voce.Tipo == 0)
                {
                    if (!primoTitolo)
                        y += 10;

                    primoTitolo = false;

                    // Il titolo resta con le prime righe del testo.
                    VaiACapoSeNecessario(
                        fontRiferimento.GetHeight() +
                        fontNormale.GetHeight() * 2 +
                        12);

                    ScriviParagrafo(
                        voce.Testo,
                        fontRiferimento,
                        XBrushes.Black);
                }
                else if (voce.Tipo == 1)
                {
                    ScriviParagrafo(
                        voce.Testo,
                        fontNormale,
                        XBrushes.Black);
                }
                else
                {
                    ScriviParagrafo(
                        voce.Testo,
                        fontCorsivo,
                        pennelloNota,
                        indentazione: 12,
                        icona: true);
                }
            }

            grafica.Dispose();

            // Piè di pagina su ogni pagina: data di esportazione e
            // "Pagina N di M" (M si conosce solo a documento finito).
            string dataEsportazione =
                DateTime.Now.ToString(
                    "dd/MM/yyyy HH:mm",
                    System.Globalization.CultureInfo
                        .GetCultureInfo("it-IT"));

            int totalePagine =
                documento.PageCount;

            for (int i = 0; i < totalePagine; i++)
            {
                var paginaPiede =
                    documento.Pages[i];

                using var graficaPiede =
                    XGraphics.FromPdfPage(
                        paginaPiede);

                double yPiede =
                    paginaPiede.Height - 28;

                graficaPiede.DrawLine(
                    XPens.LightGray,
                    margine,
                    yPiede - 12,
                    paginaPiede.Width - margine,
                    yPiede - 12);

                graficaPiede.DrawString(
                    $"Emmaus 2.0 - esportato il {dataEsportazione}",
                    fontPiede,
                    XBrushes.Gray,
                    new XPoint(margine, yPiede));

                string testoPagina =
                    $"Pagina {i + 1} di {totalePagine}";

                double larghezzaTesto =
                    graficaPiede.MeasureString(
                        testoPagina,
                        fontPiede).Width;

                graficaPiede.DrawString(
                    testoPagina,
                    fontPiede,
                    XBrushes.Gray,
                    new XPoint(
                        paginaPiede.Width -
                        margine -
                        larghezzaTesto,
                        yPiede));
            }

            documento.Save(
                percorsoFile);
        }

        private void
            dataGridView1_CellContentClick(
                object sender,
                DataGridViewCellEventArgs e)
        {
        }

        private string
            OttieniTestoRiferimento(
                RiferimentoBiblico rif)
        {
            var testi =
                new List<string>();

            using (var conn =
                new SqliteConnection(
                    connectionStringBibbia))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText = @"
                    SELECT verse, text
                    FROM verses
                    WHERE book_number=@bn
                      AND chapter=@cap
                      AND verse BETWEEN @da AND @a
                    ORDER BY verse;";

                cmd.Parameters.AddWithValue(
                    "@bn",
                    rif.Libro.BookNumber);

                cmd.Parameters.AddWithValue(
                    "@cap",
                    rif.Capitolo);

                cmd.Parameters.AddWithValue(
                    "@da",
                    rif.VersettoDa);

                cmd.Parameters.AddWithValue(
                    "@a",
                    rif.VersettoA);

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    int v =
                        reader.GetInt32(0);

                    string testo =
                        Regex.Replace(
                            reader.GetString(1),
                            "<[^>]+>",
                            "");

                    testi.Add(
                        $"{v}. {testo}");
                }
            }

            string intestazione =
                rif.VersettoDa ==
                rif.VersettoA
                    ? $"{Input.LibriCEI.NomeVisualizzato(rif.Libro)} {rif.Capitolo},{rif.VersettoDa}"
                    : $"{Input.LibriCEI.NomeVisualizzato(rif.Libro)} {rif.Capitolo},{rif.VersettoDa}-{rif.VersettoA}";

            string corpo =
                testi.Count > 0
                    ? string.Join(
                        "\n",
                        testi)
                    : "(versetto non trovato)";

            return AvvolgiTesto(
                $"{intestazione}\n{corpo}",
                45);
        }

        private static string
            AvvolgiTesto(
                string testo,
                int larghezzaColonna)
        {
            var righeFinali =
                new List<string>();

            foreach (var rigaOriginale
                in testo.Split('\n'))
            {
                var parole =
                    rigaOriginale.Split(' ');

                var rigaCorrente = "";

                foreach (var parola in parole)
                {
                    string prova =
                        rigaCorrente.Length == 0
                            ? parola
                            : rigaCorrente +
                              " " +
                              parola;

                    if (prova.Length >
                        larghezzaColonna &&
                        rigaCorrente.Length > 0)
                    {
                        righeFinali.Add(
                            rigaCorrente);

                        rigaCorrente =
                            parola;
                    }
                    else
                    {
                        rigaCorrente =
                            prova;
                    }
                }

                righeFinali.Add(
                    rigaCorrente);
            }

            return string.Join(
                "\n",
                righeFinali);
        }

        private void
            dataGridView1_CellMouseMove(
                object sender,
                DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 ||
                e.ColumnIndex < 0)
            {
                dataGridView1.Cursor =
                    Cursors.Default;

                NascondiTooltipRiferimento();
                return;
            }

            dataGridView1.Cursor =
                dataGridView1.Rows[e.RowIndex].Tag
                    is RigaTitoloInfo titoloCursore &&
                    titoloCursore.Libro != null
                    ? Cursors.Hand
                    : Cursors.Default;

            if (dataGridView1.Rows[e.RowIndex].Tag
                is not RigaNotaInfo rigaNota ||
                (rigaNota.Riferimenti.Count == 0 &&
                 rigaNota.Tags.Count == 0))
            {
                NascondiTooltipRiferimento();
                return;
            }

            var cellRect =
                dataGridView1
                    .GetCellDisplayRectangle(
                        e.ColumnIndex,
                        e.RowIndex,
                        false);

            string testoCella =
                Convert.ToString(
                    dataGridView1.Rows[e.RowIndex]
                        .Cells[e.ColumnIndex]
                        .Value) ?? "";

            var font =
                dataGridView1.Rows[e.RowIndex]
                    .Cells[e.ColumnIndex]
                    .Style.Font ??
                dataGridView1.Font;

            var puntoAssoluto =
                new Point(
                    cellRect.Left + e.X,
                    cellRect.Top + e.Y);

            int indice;

            using (var g =
                dataGridView1.CreateGraphics())
            {
                indice =
                    TrovaIndiceCarattereAlPunto(
                        g,
                        cellRect,
                        testoCella,
                        font,
                        puntoAssoluto);
            }

            if (indice < 0)
            {
                NascondiTooltipRiferimento();
                return;
            }

            if (rigaNota.Tags.Any(
                t =>
                    indice >= t.Start &&
                    indice < t.Start + t.Length))
            {
                dataGridView1.Cursor =
                    Cursors.Hand;

                NascondiTooltipRiferimento();
                return;
            }

            var riferimento =
                rigaNota.Riferimenti
                    .FirstOrDefault(
                        r =>
                            indice >= r.Start &&
                            indice <
                                r.Start + r.Length);

            if (riferimento.Libro == null)
            {
                NascondiTooltipRiferimento();
                return;
            }

            if (rigaTooltipCorrente ==
                    e.RowIndex &&
                offsetTooltipCorrente ==
                    riferimento.Start)
            {
                return;
            }

            rigaTooltipCorrente =
                e.RowIndex;

            offsetTooltipCorrente =
                riferimento.Start;

            string testoBiblico =
                OttieniTestoRiferimento(
                    riferimento);

            ultimoTestoTooltip =
                testoBiblico;

            tooltipRiferimenti.Show(
                testoBiblico,
                dataGridView1,
                cellRect.Left + e.X + 12,
                cellRect.Top + e.Y + 12);
        }

        // Il tag (#parola) sotto il punto indicato (coordinate della cella),
        // oppure null.
        private TagNote.Occorrenza
            TrovaTagAlPunto(
                int indiceRiga,
                int indiceColonna,
                int xCella,
                int yCella)
        {
            if (indiceRiga < 0 ||
                indiceColonna < 0 ||
                indiceRiga >= dataGridView1.Rows.Count ||
                dataGridView1.Rows[indiceRiga].Tag
                    is not RigaNotaInfo rigaNota ||
                rigaNota.Tags.Count == 0)
            {
                return null;
            }

            var cellRect =
                dataGridView1
                    .GetCellDisplayRectangle(
                        indiceColonna,
                        indiceRiga,
                        false);

            var cella =
                dataGridView1.Rows[indiceRiga]
                    .Cells[indiceColonna];

            string testoCella =
                Convert.ToString(cella.Value) ?? "";

            var font =
                cella.Style.Font ??
                dataGridView1.Font;

            var puntoAssoluto =
                new Point(
                    cellRect.Left + xCella,
                    cellRect.Top + yCella);

            int indice;

            using (var g =
                dataGridView1.CreateGraphics())
            {
                indice =
                    TrovaIndiceCarattereAlPunto(
                        g,
                        cellRect,
                        testoCella,
                        font,
                        puntoAssoluto);
            }

            if (indice < 0)
                return null;

            return rigaNota.Tags
                .FirstOrDefault(
                    t =>
                        indice >= t.Start &&
                        indice < t.Start + t.Length);
        }

        // Mostra in questo stesso form i commenti con il tag, ciascuno
        // preceduto dal suo versetto e dagli altri commenti dello stesso
        // versetto, a pagine come in ogni altra ricerca.
        private async void
            AvviaRicercaTag(
                string nomeTag)
        {
            if (string.IsNullOrWhiteSpace(nomeTag))
                return;

            NascondiTooltipRiferimento();

            testoRicerca =
                "#" + nomeTag;

            modalitaRicerca = true;

            await CaricaRicercaAsync();
        }

        private void
            dataGridView1_CellMouseLeave(
                object sender,
                DataGridViewCellEventArgs e)
        {
            dataGridView1.Cursor =
                Cursors.Default;

            NascondiTooltipRiferimento();
        }

        private void
            NascondiTooltipRiferimento()
        {
            if (rigaTooltipCorrente == null)
                return;

            tooltipRiferimenti.Hide(
                dataGridView1);

            rigaTooltipCorrente = null;
            offsetTooltipCorrente = null;
        }

        private void
            TooltipRiferimenti_Popup(
                object sender,
                PopupEventArgs e)
        {
            using var g =
                dataGridView1.CreateGraphics();

            var dimensione =
                MisuraTestoTooltip(
                    g,
                    ultimoTestoTooltip,
                    FontTooltipRiferimento);

            e.ToolTipSize =
                new Size(
                    (int)Math.Ceiling(
                        dimensione.Width) + 20,
                    (int)Math.Ceiling(
                        dimensione.Height) + 16);
        }

        private void
            TooltipRiferimenti_Draw(
                object sender,
                DrawToolTipEventArgs e)
        {
            using var sfondo =
                new SolidBrush(
                    TemaBibbia.FondoBottone);

            e.Graphics.FillRectangle(
                sfondo,
                e.Bounds);

            using var bordo =
                new Pen(
                    TemaBibbia.Oro);

            e.Graphics.DrawRectangle(
                bordo,
                0,
                0,
                e.Bounds.Width - 1,
                e.Bounds.Height - 1);

            var area =
                Rectangle.Inflate(
                    e.Bounds,
                    -10,
                    -8);

            using var formato =
                new StringFormat
                {
                    Alignment =
                        StringAlignment.Near,

                    LineAlignment =
                        StringAlignment.Near
                };

            using var pennelloTesto = new SolidBrush(TemaBibbia.Avorio);

            e.Graphics.DrawString(
                e.ToolTipText,
                FontTooltipRiferimento,
                pennelloTesto,
                area,
                formato);
        }

        private static SizeF
            MisuraTestoTooltip(
                Graphics g,
                string testo,
                Font font)
        {
            var totale =
                SizeF.Empty;

            foreach (var riga in
                (testo ?? "").Split('\n'))
            {
                var misura =
                    g.MeasureString(
                        riga,
                        font);

                totale.Width =
                    Math.Max(
                        totale.Width,
                        misura.Width);

                totale.Height +=
                    font.GetHeight(g);
            }

            return totale;
        }

        private int
            TrovaIndiceCarattereAlPunto(
                Graphics g,
                Rectangle bounds,
                string testo,
                Font font,
                Point punto)
        {
            var area =
                Rectangle.Inflate(
                    bounds,
                    -4,
                    -2);

            float lineHeight =
                font.GetHeight(g);

            float x =
                area.Left;

            float y =
                area.Top;

            using var fontIcona =
                testo.StartsWith("📝")
                    ? new Font(
                        "Segoe UI Emoji",
                        font.Size)
                    : null;

            var parole =
                Regex.Split(
                    testo,
                    "(?<=[ \\n])");

            int indiceCarattere = 0;

            foreach (var parola in parole)
            {
                if (parola.Length == 0)
                    continue;

                var fontParola =
                    fontIcona != null &&
                    ContieneIconaNota(parola)
                        ? fontIcona
                        : font;

                if (parola == "\n" ||
                    parola.EndsWith("\n"))
                {
                    string senzaACapo =
                        parola.TrimEnd('\n');

                    if (senzaACapo.Length > 0)
                    {
                        int? trovato =
                            TestaParolaAlPunto(
                                g,
                                ref x,
                                y,
                                senzaACapo,
                                fontParola,
                                indiceCarattere,
                                lineHeight,
                                punto);

                        if (trovato.HasValue)
                            return trovato.Value;
                    }

                    indiceCarattere +=
                        parola.Length;

                    x = area.Left;
                    y += lineHeight;

                    continue;
                }

                var misuraParola =
                    g.MeasureString(
                        parola,
                        fontParola,
                        int.MaxValue,
                        FormatoTesto);

                if (x + misuraParola.Width >
                    area.Right &&
                    x > area.Left)
                {
                    x = area.Left;
                    y += lineHeight;
                }

                int? trovatoParola =
                    TestaParolaAlPunto(
                        g,
                        ref x,
                        y,
                        parola,
                        fontParola,
                        indiceCarattere,
                        lineHeight,
                        punto);

                if (trovatoParola.HasValue)
                    return trovatoParola.Value;

                indiceCarattere +=
                    parola.Length;
            }

            return -1;
        }

        private int?
            TestaParolaAlPunto(
                Graphics g,
                ref float x,
                float y,
                string parola,
                Font font,
                int indiceBase,
                float lineHeight,
                Point punto)
        {
            var misuraParola =
                g.MeasureString(
                    parola,
                    font,
                    int.MaxValue,
                    FormatoTesto);

            bool rigaGiusta =
                punto.Y >= y &&
                punto.Y < y + lineHeight;

            bool colonnaGiusta =
                punto.X >= x &&
                punto.X <
                    x + misuraParola.Width;

            if (rigaGiusta &&
                colonnaGiusta)
            {
                for (int i = 0;
                     i < parola.Length;
                     i++)
                {
                    var misuraCarattere =
                        g.MeasureString(
                            parola.Substring(
                                0,
                                i + 1),
                            font,
                            int.MaxValue,
                            FormatoTesto);

                    if (punto.X <
                        x + misuraCarattere.Width)
                    {
                        return indiceBase + i;
                    }
                }

                return indiceBase +
                       parola.Length - 1;
            }

            x += misuraParola.Width;

            return null;
        }
    }
}
