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
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Input
{
    public partial class FormMANUTENZIONE : Form
    {

        private readonly string percorsoBibbia = Path.Combine(AppContext.BaseDirectory, "CEI_08.SQLite3");
        private readonly string percorsoCommenti = Path.Combine(AppContext.BaseDirectory, "commenti.db");

        private readonly string percorsoCronologia = "crono.txt";

        public Action PrimaDiEliminare { get; set; }

        private readonly string connectionStringBibbia;
        private readonly string connectionStringCommenti;

        private static string CartellaDesktop =>
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        static FormMANUTENZIONE()
        {
            RisolutoreFontPdf.Registra();
        }

        public FormMANUTENZIONE()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            InizializzaSceltaZoom();
            TemaBibbia.Applica(this);

            foreach (Panel scheda in new[] { panelTemi, panel1, panel2, panel3, panel4 })
                TemaBibbia.Scheda(scheda);

            foreach (Label titolo in new[] { labelTema, label1, label2, label3, label4 })
            {
                titolo.ForeColor = TemaBibbia.Oro;
                titolo.Font = new Font(titolo.Font, FontStyle.Bold);
            }

            InizializzaSceltaTema();

            connectionStringBibbia = $"Data Source={percorsoBibbia};Mode=ReadOnly;";
            connectionStringCommenti = $"Data Source={percorsoCommenti}";

            button1.Click += button1_Click;
            button2.Click += button2_Click;
            button3.Click += button3_Click;

            InizializzaRipristino();
            InizializzaOpzioniEsportazione();
            AdattaLarghezzaSchede();
            CreaPulsanteInfo();

            Load += (s, e) => CentraNelloSchermo();
        }

        // Centra la finestra nell'area di lavoro dello schermo e la tiene
        // interamente visibile (compreso il pulsante informazioni in basso).
        private void CentraNelloSchermo()
        {
            Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;

            int x = area.Left + (area.Width - Width) / 2;
            int y = area.Top + (area.Height - Height) / 2;

            x = Math.Max(area.Left, Math.Min(x, area.Right - Width));
            y = Math.Max(area.Top, Math.Min(y, area.Bottom - Height));

            StartPosition = FormStartPosition.Manual;
            Location = new Point(x, y);
        }

        // La "i" corsiva del pulsante informazioni e' disegnata come
        // vettore (curve di Bezier + puntino), quindi non dipende da
        // nessun font. Coordinate in un riquadro 100 x 100.
        private static readonly PointF[] CurvaInfo =
        {
            new PointF(36f, 52f),
            new PointF(38f, 42f), new PointF(45f, 36f), new PointF(51f, 38f),
            new PointF(49f, 50f), new PointF(45f, 62f), new PointF(44f, 72f),
            new PointF(43f, 82f), new PointF(55f, 82f), new PointF(66f, 66f)
        };

        // Il "?" del pulsante guida e' disegnato nello stesso modo e con lo
        // stesso tratto (spessore 8, estremita' arrotondate, leggera
        // inclinazione corsiva), cosi' i due pulsanti sembrano una coppia.
        private static readonly PointF[] CurvaGuida =
        {
            new PointF(34f, 34f),
            new PointF(35f, 23f), new PointF(44f, 16f), new PointF(54f, 17f),
            new PointF(66f, 18f), new PointF(72f, 27f), new PointF(69f, 37f),
            new PointF(66f, 47f), new PointF(56f, 50f), new PointF(53f, 61f)
        };

        private static void DisegnaIconaInfo(Graphics g, Control c, bool evidenziata) =>
            DisegnaIconaVettoriale(g, c, evidenziata, CurvaInfo, new RectangleF(51f, 14f, 13f, 13f));

        private static void DisegnaIconaGuida(Graphics g, Control c, bool evidenziata) =>
            DisegnaIconaVettoriale(g, c, evidenziata, CurvaGuida, new RectangleF(42f, 71f, 13f, 13f));

        // Disegna una curva di Bezier piu' un puntino, in un riquadro 100 x 100.
        private static void DisegnaIconaVettoriale(Graphics g, Control c, bool evidenziata, PointF[] curva, RectangleF punto)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            float lato = Math.Min(c.Width, c.Height) * 0.62f;

            var stato = g.Save();
            g.TranslateTransform((c.Width - lato) / 2f, (c.Height - lato) / 2f);
            g.ScaleTransform(lato / 100f, lato / 100f);

            Color colore = evidenziata ? TemaBibbia.OroChiaro : TemaBibbia.Avorio;

            using (var penna = new Pen(colore, 8f))
            using (var pennello = new SolidBrush(colore))
            {
                penna.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                penna.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                penna.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;

                g.DrawBeziers(penna, curva);
                g.FillEllipse(pennello, punto.X, punto.Y, punto.Width, punto.Height);
            }

            g.Restore(stato);
        }

        // Pulsante con un'icona vettoriale (nessun font). Il Paint viene
        // collegato dopo Controls.Add, cosi' l'icona e' disegnata sopra lo
        // sfondo dato dal tema.
        private Button CreaPulsanteIcona(string nome, string nomeAccessibile, int lato, int sinistra, int alto,
            Action<Graphics, Control, bool> disegna, Action alClick)
        {
            var pulsante = new Button
            {
                Name = nome,
                Text = string.Empty,
                AccessibleName = nomeAccessibile,
                Size = new Size(lato, lato),
                Left = sinistra,
                Top = alto,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                TabStop = true
            };

            pulsante.Click += (s, e) => alClick();

            Controls.Add(pulsante);

            bool sopra = false;

            pulsante.MouseEnter += (s, e) => { sopra = true; pulsante.Invalidate(); };
            pulsante.MouseLeave += (s, e) => { sopra = false; pulsante.Invalidate(); };

            pulsante.Paint += (s, e) => disegna(e.Graphics, pulsante, sopra || pulsante.Focused);

            return pulsante;
        }

        // Pulsanti in basso a destra: "?" (guida, pagina iniziale) e, alla sua
        // destra, "i" (informazioni, apre il form About). Le misure derivano
        // dal font, quindi seguono lo scaling.
        private void CreaPulsanteInfo()
        {
            int lato = (int)Math.Round(Font.Height * 1.6);
            int margine = (int)Math.Round(Font.Height * 0.5);

            ClientSize = new Size(ClientSize.Width, ClientSize.Height + lato + margine);

            int alto = ClientSize.Height - lato - margine;
            int sinistraInfo = ClientSize.Width - lato - margine;
            int sinistraGuida = sinistraInfo - lato - margine / 2;

            // Prima la guida e poi le info: l'ordine con il Tab e' quello da sinistra a destra.
            var pulsanteGuida = CreaPulsanteIcona("buttonGuida", "Guida", lato, sinistraGuida, alto,
                DisegnaIconaGuida, ApriGuida);

            var pulsanteInfo = CreaPulsanteIcona("buttonInfo", "Informazioni", lato, sinistraInfo, alto,
                DisegnaIconaInfo, ApriInfo);

            // Tooltip "Informazioni" con l'aspetto di quelli del tema, ma con
            // ShowAlways: quello condiviso di TemaBibbia, con MANUTENZIONE
            // aperta senza finestra proprietaria, non compariva.
            var tooltipInfo = new ToolTip
            {
                OwnerDraw = true,
                ShowAlways = true,
                InitialDelay = 300,
                ReshowDelay = 100,
                AutoPopDelay = 6000
            };

            tooltipInfo.Popup += (s, e) =>
            {
                Size m = TextRenderer.MeasureText(tooltipInfo.GetToolTip(e.AssociatedControl), Font);
                e.ToolTipSize = new Size(m.Width + 20, m.Height + 12);
            };

            tooltipInfo.Draw += (s, e) =>
            {
                using (var fondo = new SolidBrush(TemaBibbia.FondoCampo))
                    e.Graphics.FillRectangle(fondo, e.Bounds);

                using (var bordo = new Pen(TemaBibbia.Oro))
                    e.Graphics.DrawRectangle(bordo, 0, 0, e.Bounds.Width - 1, e.Bounds.Height - 1);

                TextRenderer.DrawText(e.Graphics, e.ToolTipText, Font, e.Bounds, TemaBibbia.Avorio,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            };

            tooltipInfo.SetToolTip(pulsanteInfo, "Info");
            tooltipInfo.SetToolTip(pulsanteGuida, "Guida (F1)");

            FormClosed += (s, e) => tooltipInfo.Dispose();
        }

        // True se MANUTENZIONE e' stata chiusa per aprire About. Il chiamante
        // (FormINSERISCI.button_manutenzione_Click) apre FormINFO e, quando
        // About si chiude, riapre MANUTENZIONE.
        public bool InfoRichiesta { get; private set; }

        private void ApriInfo()
        {
            InfoRichiesta = true;
            Close();
        }

        // Pulsante "?": apre la guida (finestra non modale, figlia di questa)
        // sulla pagina iniziale. Se e' gia' aperta la porta davanti.
        private void ApriGuida()
        {
            FormGUIDA.Mostra(this);
        }

        // F1: guida contestuale, direttamente sul capitolo 9, "La Manutenzione".
        private void ApriGuidaCapitolo()
        {
            FormGUIDA.Mostra(this, "c9");
        }

        // F1 apre la guida da qualsiasi controllo della finestra.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F1)
            {
                ApriGuidaCapitolo();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Con il font Cardo alcune scritte (ad esempio "Esporta miei
        // commenti") risultano piu' larghe della scheda calcolata dal
        // Designer: le schede e la finestra si allargano quanto serve.
        private void AdattaLarghezzaSchede()
        {
            float fattore = CurrentAutoScaleDimensions.Height / 37f;

            if (fattore <= 0f)
                fattore = 1f;

            int margine = (int)Math.Round(10 * fattore);

            int Mancante(Panel p)
            {
                int massimo = 0;

                foreach (Control c in p.Controls)
                {
                    int larghezza =
                        c.AutoSize ? c.PreferredSize.Width : c.Width;

                    massimo = Math.Max(massimo, c.Left + larghezza);
                }

                return Math.Max(0, massimo + margine - p.Width);
            }

            int dSinistra = Math.Max(Mancante(panel1), Mancante(panel4));
            int dDestra = Math.Max(Mancante(panel2), Mancante(panel3));

            if (dSinistra == 0 && dDestra == 0)
                return;

            foreach (Panel p in new[] { panel1, panel4 })
                p.Width += dSinistra;

            foreach (Panel p in new[] { panel2, panel3 })
            {
                p.Left += dSinistra;
                p.Width += dDestra;
            }

            panelTemi.Width += dSinistra + dDestra;

            ClientSize =
                new Size(
                    ClientSize.Width + dSinistra + dDestra,
                    ClientSize.Height);
        }

        private List<GestoreBackup.PuntoRipristino> puntiRipristino =
            new List<GestoreBackup.PuntoRipristino>();

        private IEnumerable<(CheckBox Casella, GestoreBackup.ElementoBackup Elemento)> CaselleRipristino()
        {
            yield return (chkRipCronologia, GestoreBackup.Cronologia);
            yield return (chkRipCommenti, GestoreBackup.Commenti);
        }

        private void InizializzaRipristino()
        {
            comboPuntiRipristino.DropDownStyle = ComboBoxStyle.DropDownList;
            comboPuntiRipristino.SelectedIndexChanged += (s, e) => AggiornaOpzioniRipristino();

            buttonRestore.Click += buttonRestore_Click;
            buttonEliminaPuntoRipristino.Click += buttonEliminaPuntoRipristino_Click;

            CaricaPuntiRipristino();
        }

        private void CaricaPuntiRipristino()
        {
            string precedente =
                (comboPuntiRipristino.SelectedItem as GestoreBackup.PuntoRipristino)?.Timestamp;

            puntiRipristino = GestoreBackup.ElencaPuntiRipristino();

            comboPuntiRipristino.BeginUpdate();
            comboPuntiRipristino.Items.Clear();
            foreach (var punto in puntiRipristino)
                comboPuntiRipristino.Items.Add(punto);
            comboPuntiRipristino.EndUpdate();

            int indice = puntiRipristino.FindIndex(p => p.Timestamp == precedente);
            if (indice < 0 && puntiRipristino.Count > 0)
                indice = 0;

            comboPuntiRipristino.SelectedIndex = indice;

            AggiornaOpzioniRipristino();
        }

        private void AggiornaOpzioniRipristino()
        {
            var punto = comboPuntiRipristino.SelectedItem as GestoreBackup.PuntoRipristino;

            foreach (var (casella, elemento) in CaselleRipristino())
            {
                bool disponibile = punto != null && punto.Contiene(elemento);
                casella.Enabled = disponibile;
                casella.Checked = disponibile;
            }

            bool haPunto = punto != null;
            buttonRestore.Enabled = haPunto;
            buttonEliminaPuntoRipristino.Enabled = haPunto;
        }

        private bool LeggiSelezioneRipristino(
            out GestoreBackup.PuntoRipristino punto,
            out List<GestoreBackup.ElementoBackup> elementi)
        {
            punto = comboPuntiRipristino.SelectedItem as GestoreBackup.PuntoRipristino;

            elementi = CaselleRipristino()
                .Where(x => x.Casella.Enabled && x.Casella.Checked)
                .Select(x => x.Elemento)
                .ToList();

            if (punto == null)
            {
                MessaggioBibbia.Mostra("Seleziona un punto di ripristino.", "Occhio",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (elementi.Count == 0)
            {
                MessaggioBibbia.Mostra("Seleziona almeno un elemento da ripristinare.", "Occhio",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void buttonRestore_Click(object sender, EventArgs e)
        {
            if (!LeggiSelezioneRipristino(out var punto, out var elementi))
                return;

            string elenco = string.Join(Environment.NewLine, elementi.Select(x => "•  " + x.Descrizione));

            var conferma = MessaggioBibbia.Mostra(
                $"I dati attuali verranno SOSTITUITI con il backup del {punto}:" +
                Environment.NewLine + Environment.NewLine + elenco +
                Environment.NewLine + Environment.NewLine + "Procedere con il ripristino?",
                "Conferma ripristino",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (conferma != DialogResult.Yes)
                return;

            PrimaDiEliminare?.Invoke();

            Cursor = Cursors.WaitCursor;
            GestoreBackup.Esito esito;
            try
            {
                esito = GestoreBackup.Ripristina(punto, elementi);
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            CaricaLibriEsportazione();

            MostraEsito("Ripristino", esito.Riusciti, esito.Errori, "Esito:");
        }

        private void buttonEliminaPuntoRipristino_Click(object sender, EventArgs e)
        {
            var punto = comboPuntiRipristino.SelectedItem as GestoreBackup.PuntoRipristino;

            if (punto == null)
            {
                MessaggioBibbia.Mostra("Seleziona un punto di ripristino.", "Occhio",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Si elimina l'intero punto, compresi gli eventuali file di vecchi
            // backup (DB Bibbia e DB Indicizzazione) non piu' selezionabili.
            var elementi = GestoreBackup.Elementi.Where(punto.Contiene).ToList();

            string elenco = string.Join(Environment.NewLine, elementi.Select(x => "•  " + x.Descrizione));

            var conferma = MessaggioBibbia.Mostra(
                $"Verranno eliminati definitivamente dalla cartella BackupDB i backup del {punto}:" +
                Environment.NewLine + Environment.NewLine + elenco +
                Environment.NewLine + Environment.NewLine + "Procedere?",
                "Conferma eliminazione punto di ripristino",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (conferma != DialogResult.Yes)
                return;

            var esito = GestoreBackup.EliminaDalPunto(punto, elementi);

            CaricaPuntiRipristino();

            MostraEsito("Eliminazione punto di ripristino", esito.Riusciti, esito.Errori, "Esito:");
        }

        private void InizializzaSceltaZoom()
        {
            // I controlli del Designer sono gia' stati scalati da WinForms,
            // quindi le misure aggiunte qui vanno scalate a mano.
            float fattore = CurrentAutoScaleDimensions.Height / 37f;

            if (fattore <= 0f)
                fattore = 1f;

            int Sc(int px) => (int)Math.Round(px * fattore);

            var etichettaZoom = new Label
            {
                AutoSize = true,
                Text = "Scaling",
                ForeColor = TemaBibbia.Oro
            };
            etichettaZoom.Font = new Font(etichettaZoom.Font, FontStyle.Bold);

            var comboZoom = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = Sc(120)
            };

            int margine = Sc(8);

            comboZoom.Top =
                flowTemi.Top + flowTemi.PreferredSize.Height + margine;

            etichettaZoom.Left = labelTema.Left;
            etichettaZoom.Top =
                comboZoom.Top +
                (comboZoom.Height - etichettaZoom.PreferredSize.Height) / 2;

            comboZoom.Left =
                etichettaZoom.Left + etichettaZoom.PreferredSize.Width + Sc(12);

            int spazioExtra =
                Math.Max(0, comboZoom.Bottom + margine - panelTemi.Height);

            panelTemi.Height += spazioExtra;

            foreach (Panel p in new[] { panel1, panel2, panel3, panel4 })
                p.Top += spazioExtra;

            ClientSize = new Size(ClientSize.Width, ClientSize.Height + spazioExtra);

            float corrente = ZoomApp.Leggi();
            var valori = new List<float>(ZoomApp.Valori);

            // Un valore scritto a mano in zoom.txt (ad esempio 10,3)
            // compare comunque nella tendina.
            if (!valori.Contains(corrente))
            {
                valori.Add(corrente);
                valori.Sort();
            }

            foreach (float v in valori)
                comboZoom.Items.Add(ZoomApp.Descrizione(v));

            comboZoom.SelectedIndex = valori.IndexOf(corrente);

            comboZoom.SelectionChangeCommitted += (s, e) =>
            {
                int i = comboZoom.SelectedIndex;
                if (i < 0 || i >= valori.Count || valori[i] == ZoomApp.Leggi())
                    return;

                // Il nuovo valore va confermato dopo il riavvio (entro
                // 10 secondi), altrimenti torna quello precedente.
                ZoomApp.ProponiNuovoValore(valori[i]);
                ZoomApp.RiavviaSenzaArgomenti();
            };

            panelTemi.Controls.Add(etichettaZoom);
            panelTemi.Controls.Add(comboZoom);
        }

        private void InizializzaSceltaTema()
        {
            RadioButton[] opzioni = { radioTema1, radioTema2, radioTema3, radioTema4, radioTema5 };
            IReadOnlyList<string> nomi = TemaBibbia.NomiTemi;

            for (int i = 0; i < opzioni.Length && i < nomi.Count; i++)
            {
                opzioni[i].Text = nomi[i];
                opzioni[i].Checked = i == TemaBibbia.IndiceTema;
            }

            for (int i = 0; i < opzioni.Length; i++)
            {
                int indice = i;
                opzioni[i].CheckedChanged += (s, e) =>
                {
                    if (((RadioButton)s).Checked)
                        TemaBibbia.ImpostaTema(indice);
                };
            }

            // Se il tema cambia altrove (ad esempio dal menu Tema della guida)
            // la scelta qui si aggiorna. ImpostaTema ignora il tema gia' attivo,
            // quindi non c'e' rimbalzo.
            EventHandler suTema = (s, e) =>
            {
                int attivo = TemaBibbia.IndiceTema;

                if (attivo >= 0 && attivo < opzioni.Length && !opzioni[attivo].Checked)
                    opzioni[attivo].Checked = true;
            };

            TemaBibbia.TemaCambiato += suTema;
            FormClosed += (s, e) => TemaBibbia.TemaCambiato -= suTema;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var selezionate = new List<GestoreBackup.ElementoBackup>();
            if (checkBox3.Checked) selezionate.Add(GestoreBackup.Commenti);
            if (checkBox4.Checked) selezionate.Add(GestoreBackup.Cronologia);

            if (selezionate.Count == 0)
            {
                MessaggioBibbia.Mostra("Seleziona almeno un elemento da salvare.", "Occhio",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            GestoreBackup.Esito esito;
            Cursor = Cursors.WaitCursor;
            try
            {
                esito = GestoreBackup.EseguiBackup(selezionate, segnalaMancanti: true);
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            CaricaPuntiRipristino();

            MostraEsito("Backup", esito.Riusciti, esito.Errori,
                $"Backup salvato nella cartella:{Environment.NewLine}{GestoreBackup.CartellaBackup}{Environment.NewLine}");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var selezionati = LibriEsportazioneSelezionati(out int disponibili);

            if (disponibili == 0)
            {
                MessaggioBibbia.Mostra("Nessun commento da esportare.", "Occhio",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selezionati.Count == 0)
            {
                MessaggioBibbia.Mostra("Seleziona almeno un libro da esportare.", "Occhio",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Tutti i libri selezionati = nessun filtro (include anche i
            // commenti di eventuali libri non presenti nell'elenco CEI).
            HashSet<string> libriDaEsportare = selezionati.Count == disponibili ? null : selezionati;

            // Con una selezione parziale il nome del file la riporta, cosi'
            // non si sovrascrive l'esportazione completa.
            string nomeFile = "Miei_Commenti.pdf";
            if (libriDaEsportare != null)
            {
                if (libriDaEsportare.Count == 1)
                {
                    string pulito = string.Concat(libriDaEsportare.First().Select(c =>
                        Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).Replace(' ', '_');
                    nomeFile = $"Miei_Commenti_{pulito}.pdf";
                }
                else
                {
                    nomeFile = "Miei_Commenti_selezione.pdf";
                }
            }

            var riusciti = new List<string>();
            var errori = new List<string>();

            Cursor = Cursors.WaitCursor;
            try
            {
                int totale = GeneraPdfCommenti(Path.Combine(CartellaDesktop, nomeFile), libriDaEsportare);
                riusciti.Add($"{nomeFile} ({totale} commenti)");
            }
            catch (Exception ex)
            {
                errori.Add($"{nomeFile}: {ex.Message}");
            }
            finally
            {
                Cursor = Cursors.Default;
            }

            MostraEsito("Esportazione PDF", riusciti, errori, "Creato sul Desktop:");
        }

        // ------------------------------------------------------------------
        // Opzioni dell'esportazione PDF: libri da esportare (come nel form
        // Leggi: caselle AT e NT, ciascuna con la sua tendina di libri) e
        // ordine del documento.
        // ------------------------------------------------------------------

        private const string NomeAnticoTestamento = "Antico Testamento";

        private sealed class NotaCommento
        {
            public string Libro;
            public int Capitolo;
            public int Da;
            public int A;
            public string Testo;
        }

        private sealed class GruppoLibro
        {
            public string Intestazione;
            public int Indice;       // posizione nell'ordine biblico
            public int Testamento;   // 0 = Antico, 1 = Nuovo, 2 = altri libri
            public List<NotaCommento> Note;
        }

        private CheckBox checkEsportaAT;
        private CheckBox checkEsportaNT;
        private Button bottoneEsportaAT;
        private Button bottoneEsportaNT;
        private Panel listaEsportaAT;
        private Panel listaEsportaNT;
        private FlowLayoutPanel flowEsportaAT;
        private FlowLayoutPanel flowEsportaNT;
        private CheckBox checkTuttoEsportaAT;
        private CheckBox checkTuttoEsportaNT;

        private bool aggiornamentoSelezioneEsportazione;

        // Come ScalaDesign di FormLEGGI: misure scritte nella scala del
        // Designer, adeguate al font effettivo.
        private int ScalaEsp(int px)
        {
            float fattore = CurrentAutoScaleDimensions.Height / 37f;

            if (fattore <= 0f)
                fattore = 1f;

            return (int)Math.Round(px * fattore);
        }

        private void InizializzaOpzioniEsportazione()
        {
            checkEsportaAT = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Text = "AT",
                TabIndex = 3,
                ForeColor = TemaBibbia.Avorio,
                AccessibleName = "Esporta i commenti dell'Antico Testamento"
            };

            checkEsportaNT = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                Text = "NT",
                TabIndex = 5,
                ForeColor = TemaBibbia.Avorio,
                AccessibleName = "Esporta i commenti del Nuovo Testamento"
            };

            bottoneEsportaAT = CreaBottoneListaEsportazione(4, "Scegli i libri dell'Antico Testamento");
            bottoneEsportaNT = CreaBottoneListaEsportazione(6, "Scegli i libri del Nuovo Testamento");

            listaEsportaAT = CreaListaLibriEsportazione(
                "Libri dell'Antico Testamento", checkEsportaAT,
                out flowEsportaAT, out checkTuttoEsportaAT);

            listaEsportaNT = CreaListaLibriEsportazione(
                "Libri del Nuovo Testamento", checkEsportaNT,
                out flowEsportaNT, out checkTuttoEsportaNT);

            bottoneEsportaAT.Click += (s, e) => AlternaListaEsportazione(true);
            bottoneEsportaNT.Click += (s, e) => AlternaListaEsportazione(false);

            panel2.Controls.Add(checkEsportaAT);
            panel2.Controls.Add(bottoneEsportaAT);
            panel2.Controls.Add(checkEsportaNT);
            panel2.Controls.Add(bottoneEsportaNT);

            // Le tendine stanno sopra le altre schede (come un menu a
            // discesa), quindi sono figlie della finestra e non di panel2.
            Controls.Add(listaEsportaAT);
            Controls.Add(listaEsportaNT);

            CaricaLibriEsportazione();
        }

        private static Button CreaBottoneListaEsportazione(int indiceTab, string nomeAccessibile)
        {
            return new Button
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Text = "▾ Libri",
                TabIndex = indiceTab,
                UseVisualStyleBackColor = true,
                AccessibleName = nomeAccessibile
            };
        }

        private Panel CreaListaLibriEsportazione(
            string nomeAccessibile,
            CheckBox checkTestamento,
            out FlowLayoutPanel flowRisultante,
            out CheckBox checkTuttoRisultante)
        {
            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(2),
                Margin = new Padding(0),
                BackColor = TemaBibbia.FondoCampo
            };

            var checkTutto = new CheckBox
            {
                Dock = DockStyle.Top,
                Height = Font.Height + ScalaEsp(24),
                Text = "Seleziona tutto",
                Font = new Font(Font, FontStyle.Bold),
                AutoCheck = false,
                Checked = true,
                ForeColor = TemaBibbia.Avorio,
                Padding = new Padding(4, 2, 0, 4)
            };

            var contenitore = new Panel
            {
                Visible = false,
                Padding = new Padding(1),
                BackColor = TemaBibbia.FondoCampo,
                AccessibleName = nomeAccessibile
            };

            contenitore.Paint += (s, e) =>
            {
                using var bordo = new Pen(TemaBibbia.Oro);
                e.Graphics.DrawRectangle(bordo, 0, 0, contenitore.Width - 1, contenitore.Height - 1);
            };

            // Prima il flow, poi l'intestazione: cosi' "Seleziona tutto"
            // resta in alto e i libri occupano il resto.
            contenitore.Controls.Add(flow);
            contenitore.Controls.Add(checkTutto);

            checkTutto.Click += (s, e) =>
            {
                ImpostaLibriEsportazione(flow, !checkTutto.Checked);
                AggiornaCaselleEsportazione(flow, checkTutto, checkTestamento);
            };

            checkTestamento.CheckedChanged += (s, e) =>
            {
                if (aggiornamentoSelezioneEsportazione)
                    return;

                ImpostaLibriEsportazione(flow, checkTestamento.Checked);
                AggiornaCaselleEsportazione(flow, checkTutto, null);

                if (!checkTestamento.Checked)
                    ChiudiListeEsportazione();
            };

            flowRisultante = flow;
            checkTuttoRisultante = checkTutto;

            return contenitore;
        }

        private void ImpostaLibriEsportazione(FlowLayoutPanel flow, bool valore)
        {
            aggiornamentoSelezioneEsportazione = true;

            try
            {
                foreach (CheckBox casella in flow.Controls.OfType<CheckBox>())
                    casella.Checked = valore;
            }
            finally
            {
                aggiornamentoSelezioneEsportazione = false;
            }
        }

        // "Seleziona tutto" e' spuntata se lo sono tutti i libri; la casella
        // AT/NT e' spuntata se lo e' almeno un libro.
        private void AggiornaCaselleEsportazione(
            FlowLayoutPanel flow,
            CheckBox checkTutto,
            CheckBox checkTestamento)
        {
            var libri = flow.Controls.OfType<CheckBox>().ToList();

            checkTutto.Checked = libri.Count > 0 && libri.All(c => c.Checked);

            if (checkTestamento == null)
                return;

            bool almenoUno = libri.Any(c => c.Checked);

            if (checkTestamento.Checked == almenoUno)
                return;

            aggiornamentoSelezioneEsportazione = true;

            try
            {
                checkTestamento.Checked = almenoUno;
            }
            finally
            {
                aggiornamentoSelezioneEsportazione = false;
            }
        }

        // Legge i commenti per libro e riempie le due tendine con i soli
        // libri che hanno almeno un commento: prima quelli con piu'
        // commenti, a parita' in ordine biblico. I libri deselezionati
        // prima dell'aggiornamento restano deselezionati.
        private void CaricaLibriEsportazione()
        {
            ChiudiListeEsportazione();

            var deselezionati = new HashSet<string>();

            foreach (var flow in new[] { flowEsportaAT, flowEsportaNT })
            {
                foreach (CheckBox casella in flow.Controls.OfType<CheckBox>())
                {
                    if (!casella.Checked && casella.Tag is LibroBiblico libro)
                        deselezionati.Add(libro.Nome);
                }
            }

            var conteggi = new Dictionary<string, int>();

            try
            {
                if (File.Exists(percorsoCommenti))
                {
                    using var conn = new SqliteConnection(connectionStringCommenti);
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Libro, COUNT(*) FROM Note GROUP BY Libro;";
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                        conteggi[reader.GetString(0)] = reader.GetInt32(1);
                }
            }
            catch (Exception)
            {
                conteggi.Clear();
            }
            finally
            {
                // Rilascia commenti.db, cosi' ripristino ed eliminazione
                // possono sostituirlo o cancellarlo.
                SqliteConnection.ClearAllPools();
            }

            if (LibriCEI.Elenco.Count == 0)
            {
                try
                {
                    LibriCEI.Carica(connectionStringBibbia);
                }
                catch (Exception)
                {
                }
            }

            var perTestamento = LibriCEI.Elenco
                .Select((libro, indice) => (Libro: libro, Indice: indice))
                .Where(x => conteggi.ContainsKey(x.Libro.Nome))
                .Select(x => (x.Libro, x.Indice, Quanti: conteggi[x.Libro.Nome]))
                .OrderByDescending(x => x.Quanti)
                .ThenBy(x => x.Indice)
                .ToLookup(x => LibriCEI.CategoriaDiLibro(x.Libro.BookNumber).Testamento == NomeAnticoTestamento);

            RiempiListaEsportazione(
                flowEsportaAT, checkTuttoEsportaAT, checkEsportaAT, bottoneEsportaAT, "AT",
                perTestamento[true].Select(x => (x.Libro, x.Quanti)).ToList(), deselezionati);

            RiempiListaEsportazione(
                flowEsportaNT, checkTuttoEsportaNT, checkEsportaNT, bottoneEsportaNT, "NT",
                perTestamento[false].Select(x => (x.Libro, x.Quanti)).ToList(), deselezionati);

            RiposizionaRigaTestamentiEsportazione();
        }

        private void RiempiListaEsportazione(
            FlowLayoutPanel flow,
            CheckBox checkTutto,
            CheckBox checkTestamento,
            Button bottone,
            string sigla,
            List<(LibroBiblico Libro, int Quanti)> libri,
            HashSet<string> deselezionati)
        {
            aggiornamentoSelezioneEsportazione = true;

            try
            {
                foreach (Control vecchio in flow.Controls.Cast<Control>().ToList())
                {
                    flow.Controls.Remove(vecchio);
                    vecchio.Dispose();
                }

                foreach (var (libro, quanti) in libri)
                {
                    string testo = $"{LibriCEI.NomeVisualizzato(libro)} ({quanti})";

                    var casella = new CheckBox
                    {
                        Text = testo,
                        Tag = libro,
                        AutoSize = false,
                        Width = TextRenderer.MeasureText(testo, Font).Width + ScalaEsp(40),
                        Height = Font.Height + ScalaEsp(8),
                        Margin = new Padding(3, 2, 3, 2),
                        TextAlign = ContentAlignment.MiddleLeft,
                        ForeColor = TemaBibbia.Avorio,
                        Checked = !deselezionati.Contains(libro.Nome)
                    };

                    casella.CheckedChanged += (s, e) =>
                    {
                        if (aggiornamentoSelezioneEsportazione)
                            return;

                        AggiornaCaselleEsportazione(flow, checkTutto, checkTestamento);
                    };

                    flow.Controls.Add(casella);
                }

                checkTestamento.Text = $"{sigla} ({libri.Sum(x => x.Quanti)})";
                checkTestamento.Enabled = libri.Count > 0;
                bottone.Enabled = libri.Count > 0;
            }
            finally
            {
                aggiornamentoSelezioneEsportazione = false;
            }

            AggiornaCaselleEsportazione(flow, checkTutto, checkTestamento);
        }

        // AT, "Libri", NT, "Libri" sulla stessa riga, sotto il titolo
        // "Esporta miei commenti".
        private void RiposizionaRigaTestamentiEsportazione()
        {
            int gapPiccolo = ScalaEsp(6);
            int gapGrande = ScalaEsp(24);
            int top = label2.Bottom + ScalaEsp(10);

            var elementi = new Control[] { checkEsportaAT, bottoneEsportaAT, checkEsportaNT, bottoneEsportaNT };
            int altezza = elementi.Max(c => c.PreferredSize.Height);

            foreach (Control c in elementi)
                c.Top = top + (altezza - c.PreferredSize.Height) / 2;

            checkEsportaAT.Left = label2.Left;
            bottoneEsportaAT.Left = checkEsportaAT.Left + checkEsportaAT.PreferredSize.Width + gapPiccolo;
            checkEsportaNT.Left = bottoneEsportaAT.Left + bottoneEsportaAT.PreferredSize.Width + gapGrande;
            bottoneEsportaNT.Left = checkEsportaNT.Left + checkEsportaNT.PreferredSize.Width + gapPiccolo;
        }

        private void AlternaListaEsportazione(bool anticoTestamento)
        {
            var lista = anticoTestamento ? listaEsportaAT : listaEsportaNT;
            var altra = anticoTestamento ? listaEsportaNT : listaEsportaAT;
            var flow = anticoTestamento ? flowEsportaAT : flowEsportaNT;

            bool apri = !lista.Visible;

            altra.Visible = false;

            if (apri)
            {
                PosizionaListaEsportazione(lista, flow);
                lista.Visible = true;
                lista.BringToFront();
            }
            else
            {
                lista.Visible = false;
            }

            AggiornaStatoListeEsportazione();
        }

        // La tendina parte sotto la riga AT/NT, ha la larghezza di panel2 e
        // arriva al massimo al bordo inferiore della scheda "Elimina", che
        // copre finche' e' aperta; i libri in piu' scorrono.
        private void PosizionaListaEsportazione(Panel lista, FlowLayoutPanel flow)
        {
            int top = panel2.Top +
                Math.Max(bottoneEsportaAT.Bottom, checkEsportaAT.Bottom) +
                ScalaEsp(4);

            int altezzaDisponibile = Math.Max(ScalaEsp(120), panel3.Bottom - top);

            var caselle = flow.Controls.OfType<CheckBox>().ToList();
            int altezzaRiga = caselle.Count > 0 ? caselle[0].Height + 4 : Font.Height + 12;

            int altezzaIntestazione = Font.Height + ScalaEsp(24);

            int altezzaNecessaria =
                altezzaIntestazione + (altezzaRiga * caselle.Count) + ScalaEsp(14);

            lista.SetBounds(
                panel2.Left,
                top,
                panel2.Width,
                Math.Min(altezzaNecessaria, altezzaDisponibile));

            int larghezza = lista.Width - SystemInformation.VerticalScrollBarWidth - ScalaEsp(24);

            foreach (CheckBox casella in caselle)
                casella.Width = Math.Max(casella.Width, larghezza);
        }

        private void ChiudiListeEsportazione()
        {
            if (listaEsportaAT == null || listaEsportaNT == null)
                return;

            listaEsportaAT.Visible = false;
            listaEsportaNT.Visible = false;

            AggiornaStatoListeEsportazione();
        }

        // Con una tendina aperta la scheda "Elimina" resta nascosta e i
        // controlli coperti sono disattivati, cosi' il Tab non finisce
        // su cose che non si vedono.
        private void AggiornaStatoListeEsportazione()
        {
            bool aperta = listaEsportaAT.Visible || listaEsportaNT.Visible;

            bottoneEsportaAT.Text = listaEsportaAT.Visible ? "▴ Libri" : "▾ Libri";
            bottoneEsportaNT.Text = listaEsportaNT.Visible ? "▴ Libri" : "▾ Libri";

            panel3.Visible = !aperta;
            button2.Enabled = !aperta;

            RiposizionaRigaTestamentiEsportazione();
        }

        // Nomi dei libri con la casella spuntata nelle due tendine.
        // disponibili = quanti libri hanno commenti.
        private HashSet<string> LibriEsportazioneSelezionati(out int disponibili)
        {
            var selezionati = new HashSet<string>();
            disponibili = 0;

            foreach (var flow in new[] { flowEsportaAT, flowEsportaNT })
            {
                foreach (CheckBox casella in flow.Controls.OfType<CheckBox>())
                {
                    disponibili++;

                    if (casella.Checked && casella.Tag is LibroBiblico libro)
                        selezionati.Add(libro.Nome);
                }
            }

            return selezionati;
        }

        private static string NomeTestamento(int testamento)
        {
            switch (testamento)
            {
                case 0: return "Antico Testamento";
                case 1: return "Nuovo Testamento";
                default: return "Altri libri";
            }
        }

        private static List<NotaCommento> OrdinaNote(IEnumerable<NotaCommento> note) =>
            note.OrderBy(n => n.Capitolo).ThenBy(n => n.Da).ThenBy(n => n.A).ToList();

        // Esporta solo i commenti scritti dall'utente, ciascuno con il suo
        // riferimento (ad esempio "Giovanni 3, 16-18").
        // Il testo dei versetti CEI non viene letto ne' scritto nel PDF: dal
        // database della Bibbia si leggono soltanto i nomi dei libri.
        //
        // libriSelezionati: nomi dei libri da esportare, null per tutti.
        // I libri sono ordinati come nelle tendine: prima l'Antico poi il
        // Nuovo Testamento, in ciascuno dal piu' commentato, a parita' di
        // numero secondo l'ordine biblico.
        // Restituisce il numero di commenti esportati.
        private int GeneraPdfCommenti(string percorsoFile, HashSet<string> libriSelezionati)
        {
            if (!File.Exists(percorsoCommenti))
                throw new InvalidOperationException("nessun commento da esportare (database dei commenti non trovato).");

            var note = new List<NotaCommento>();

            using (var conn = new SqliteConnection(connectionStringCommenti))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Libro, Capitolo, VersettoDa, VersettoA, TestoNota FROM Note;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    note.Add(new NotaCommento
                    {
                        Libro = reader.GetString(0),
                        Capitolo = reader.GetInt32(1),
                        Da = reader.GetInt32(2),
                        A = reader.GetInt32(3),
                        Testo = reader.IsDBNull(4) ? "" : reader.GetString(4)
                    });
                }
            }

            if (note.Count == 0)
                throw new InvalidOperationException("nessun commento da esportare.");

            if (libriSelezionati != null)
            {
                note = note.Where(n => libriSelezionati.Contains(n.Libro)).ToList();

                if (note.Count == 0)
                    throw new InvalidOperationException("nessun commento per i libri selezionati.");
            }

            if (LibriCEI.Elenco.Count == 0)
                LibriCEI.Carica(connectionStringBibbia);

            // Gruppi per libro, in ordine biblico; i libri non riconosciuti
            // (nome non presente nell'elenco CEI) vanno in fondo.
            var gruppi = new List<GruppoLibro>();
            var libriNoti = new HashSet<string>();
            int posizione = 0;

            foreach (var libro in LibriCEI.Elenco)
            {
                libriNoti.Add(libro.Nome);

                var delLibro = OrdinaNote(note.Where(n => n.Libro == libro.Nome));

                if (delLibro.Count > 0)
                {
                    gruppi.Add(new GruppoLibro
                    {
                        Intestazione = LibriCEI.NomeVisualizzato(libro),
                        Indice = posizione,
                        Testamento = LibriCEI.CategoriaDiLibro(libro.BookNumber).Testamento == NomeAnticoTestamento ? 0 : 1,
                        Note = delLibro
                    });
                }

                posizione++;
            }

            foreach (var altro in note
                .Where(n => !libriNoti.Contains(n.Libro))
                .GroupBy(n => n.Libro)
                .OrderBy(g => g.Key))
            {
                gruppi.Add(new GruppoLibro
                {
                    Intestazione = altro.Key,
                    Indice = posizione++,
                    Testamento = 2,
                    Note = OrdinaNote(altro)
                });
            }

            gruppi = gruppi
                .OrderBy(g => g.Testamento)
                .ThenByDescending(g => g.Note.Count)
                .ThenBy(g => g.Indice)
                .ToList();

            string nomeFiltro = libriSelezionati != null && gruppi.Count == 1 ? gruppi[0].Intestazione : null;

            // Prima passata: serve solo a sapere in che pagina cade ogni
            // libro, per scrivere i numeri di pagina nell'indice. Le righe
            // dell'indice hanno sempre la stessa altezza, quindi la seconda
            // passata (quella vera) impagina in modo identico.
            var pagineLibri = new Dictionary<string, int>();

            CostruisciPdfCommenti(null, gruppi, note.Count, nomeFiltro, null, pagineLibri);

            CostruisciPdfCommenti(percorsoFile, gruppi, note.Count, nomeFiltro, pagineLibri, null);

            return note.Count;
        }

        // Disegna il documento. Con percorsoFile == null e' la prova per
        // rilevare le pagine (riempie pagineRilevate, non salva nulla); con
        // un percorso e' la stesura definitiva: indice con i numeri di
        // pagina (pagineIndice), segnalibri, piè di pagina e salvataggio.
        private static void CostruisciPdfCommenti(
            string percorsoFile,
            List<GruppoLibro> gruppi,
            int totale,
            string nomeFiltro,
            Dictionary<string, int> pagineIndice,
            Dictionary<string, int> pagineRilevate)
        {
            bool finale = percorsoFile != null;

            string titolo = nomeFiltro == null
                ? $"I miei commenti ({totale})"
                : $"I miei commenti - {nomeFiltro} ({totale})";

            const double margine = 50;
            using var documento = new PdfDocument();
            documento.Info.Title = titolo;
            documento.Info.Creator = "Emmaus 2.0";

            var pagina = documento.AddPage();
            pagina.Size = PdfSharp.PageSize.A4;
            var grafica = XGraphics.FromPdfPage(pagina);
            int numeroPagina = 1;

            var fontTitolo = new XFont(RisolutoreFontPdf.FamigliaMit, 18, XFontStyleEx.Bold);
            var fontLibro = new XFont(RisolutoreFontPdf.FamigliaMit, 15, XFontStyleEx.Bold);
            var fontRiferimento = new XFont(RisolutoreFontPdf.FamigliaMit, 12, XFontStyleEx.Bold);
            var fontNormale = new XFont(RisolutoreFontPdf.FamigliaMit, 10.5, XFontStyleEx.Regular);
            var fontCorsivo = new XFont(RisolutoreFontPdf.FamigliaMit, 10.5, XFontStyleEx.Italic);
            var fontPiede = new XFont(RisolutoreFontPdf.FamigliaMit, 9, XFontStyleEx.Regular);
            var pennelloNota = new XSolidBrush(XColor.FromArgb(Color.DarkSlateBlue.ToArgb()));
            var pennaGuida = new XPen(XColors.Gray, 0.5) { DashStyle = XDashStyle.Dot };
            using var iconaNota = CreaImmagineIconaNota();

            double y = margine;

            void VaiACapoSeNecessario(double altezzaRiga)
            {
                if (y + altezzaRiga > pagina.Height - margine)
                {
                    pagina = documento.AddPage();
                    pagina.Size = PdfSharp.PageSize.A4;
                    grafica.Dispose();
                    grafica = XGraphics.FromPdfPage(pagina);
                    numeroPagina++;
                    y = margine;
                }
            }

            // Segnalibro che porta alla pagina corrente (solo nel PDF vero).
            PdfOutline Segnalibro(PdfOutline genitore, string testo, bool aperto)
            {
                if (!finale)
                    return null;

                return genitore == null
                    ? documento.Outlines.Add(testo, pagina, aperto)
                    : genitore.Outlines.Add(testo, pagina, aperto);
            }

            void ScriviParagrafo(string testoParagrafo, XFont font, XBrush pennello, double indentazione = 0, XImage icona = null)
            {
                double altezzaRiga = font.GetHeight() + 3;
                double xBase = margine + indentazione;
                double xTesto = xBase;

                if (icona != null)
                {
                    VaiACapoSeNecessario(altezzaRiga);
                    double lato = font.GetHeight();
                    grafica.DrawImage(icona, xBase, y, lato, lato);
                    xTesto = xBase + lato + 6;
                }

                bool primaRigaParagrafo = true;
                var parole = (testoParagrafo ?? "").Split(' ');
                string rigaCorrente = "";

                void DisegnaRigaCorrente(string rigaTesto)
                {
                    double xRiga = primaRigaParagrafo ? xTesto : xBase;
                    VaiACapoSeNecessario(altezzaRiga);
                    grafica.DrawString(rigaTesto, font, pennello, new XPoint(xRiga, y + font.GetHeight()));
                    y += altezzaRiga;
                    primaRigaParagrafo = false;
                }

                foreach (var parola in parole)
                {
                    double xRigaCorrente = primaRigaParagrafo ? xTesto : xBase;
                    double disponibile = pagina.Width - margine - xRigaCorrente;
                    string prova = rigaCorrente.Length == 0 ? parola : rigaCorrente + " " + parola;
                    double larghezzaProva = grafica.MeasureString(prova, font).Width;

                    if (larghezzaProva > disponibile && rigaCorrente.Length > 0)
                    {
                        DisegnaRigaCorrente(rigaCorrente);
                        rigaCorrente = parola;
                    }
                    else
                    {
                        rigaCorrente = prova;
                    }
                }

                if (rigaCorrente.Length > 0)
                    DisegnaRigaCorrente(rigaCorrente);

                y += 4;
            }

            // Riga d'indice: nome a sinistra, numero di pagina a destra,
            // collegati da una linea a puntini. Sempre su una sola riga.
            void ScriviRigaIndice(string sinistra, string destra, XFont font, double indentazione)
            {
                double altezzaRiga = font.GetHeight() + 3;
                VaiACapoSeNecessario(altezzaRiga);

                double xSinistra = margine + indentazione;
                double larghezzaSinistra = grafica.MeasureString(sinistra, font).Width;
                double larghezzaDestra = grafica.MeasureString(destra, font).Width;
                double xDestra = pagina.Width - margine - larghezzaDestra;
                double yTesto = y + font.GetHeight();

                grafica.DrawString(sinistra, font, XBrushes.Black, new XPoint(xSinistra, yTesto));
                grafica.DrawString(destra, font, XBrushes.Black, new XPoint(xDestra, yTesto));

                double daX = xSinistra + larghezzaSinistra + 6;
                double aX = xDestra - 6;

                if (aX > daX)
                    grafica.DrawLine(pennaGuida, daX, yTesto - 3, aX, yTesto - 3);

                y += altezzaRiga;
            }

            // Intestazione come nel form Info: nome, sottotitolo, copyright,
            // slogan e licenza.
            ScriviParagrafo("Emmaus 2.0", fontTitolo, XBrushes.Black);
            ScriviParagrafo(
                "Emmaus 2.0 è un programma per Windows per meditare la Sacra Scrittura.",
                fontNormale, XBrushes.Black);
            ScriviParagrafo(
                "Copyright © 2026 DisabiliCristiani disabilicristiani@gmail.com - Fate del bene e pregate",
                fontCorsivo, XBrushes.Black);
            ScriviParagrafo("Codice sorgente sotto licenza MIT.", fontCorsivo, XBrushes.Black);

            y += 4;
            grafica.DrawLine(XPens.Gray, margine, y, pagina.Width - margine, y);
            y += 14;

            ScriviParagrafo(titolo, fontLibro, XBrushes.Black);

            if (nomeFiltro == null)
            {
                ScriviParagrafo(
                    "Ordine: per numero di commenti, prima Antico poi Nuovo Testamento",
                    fontCorsivo, XBrushes.Black);
            }

            y += 2;

            // Indice dei libri commentati, con il numero di commenti e la
            // pagina in cui iniziano.
            VaiACapoSeNecessario(fontRiferimento.GetHeight() + fontNormale.GetHeight() * 2 + 20);
            Segnalibro(null, "Indice", true);
            ScriviParagrafo("Indice", fontRiferimento, XBrushes.Black);

            int testamentoIndice = -1;

            foreach (var gruppo in gruppi)
            {
                if (gruppo.Testamento != testamentoIndice)
                {
                    testamentoIndice = gruppo.Testamento;
                    int commentiTestamento = gruppi
                        .Where(x => x.Testamento == gruppo.Testamento)
                        .Sum(x => x.Note.Count);

                    ScriviParagrafo($"{NomeTestamento(gruppo.Testamento)} ({commentiTestamento})",
                        fontRiferimento, XBrushes.Black);
                }

                int paginaLibro = 0;
                if (pagineIndice != null)
                    pagineIndice.TryGetValue(gruppo.Intestazione, out paginaLibro);

                ScriviRigaIndice($"{gruppo.Intestazione} ({gruppo.Note.Count})",
                    $"pag. {paginaLibro}", fontNormale, 12);
            }

            y += 10;

            PdfOutline segnalibroTestamento = null;
            int testamentoCorrente = -1;

            foreach (var gruppo in gruppi)
            {
                if (gruppo.Testamento != testamentoCorrente)
                {
                    testamentoCorrente = gruppo.Testamento;
                    int commentiTestamento = gruppi
                        .Where(x => x.Testamento == gruppo.Testamento)
                        .Sum(x => x.Note.Count);

                    VaiACapoSeNecessario(
                        fontTitolo.GetHeight() + fontLibro.GetHeight() +
                        fontRiferimento.GetHeight() + fontCorsivo.GetHeight() + 30);

                    segnalibroTestamento = Segnalibro(null, NomeTestamento(gruppo.Testamento), true);

                    ScriviParagrafo($"{NomeTestamento(gruppo.Testamento)} ({commentiTestamento})",
                        fontTitolo, XBrushes.Black);
                }

                VaiACapoSeNecessario(fontLibro.GetHeight() + fontRiferimento.GetHeight() + fontCorsivo.GetHeight() + 20);

                if (pagineRilevate != null)
                    pagineRilevate[gruppo.Intestazione] = numeroPagina;

                PdfOutline segnalibroLibro = Segnalibro(segnalibroTestamento, gruppo.Intestazione, false);

                ScriviParagrafo(gruppo.Intestazione, fontLibro, XBrushes.Black);

                foreach (var nota in gruppo.Note)
                {
                    // Riferimento e prima riga del commento restano insieme.
                    VaiACapoSeNecessario(fontRiferimento.GetHeight() + fontCorsivo.GetHeight() + 10);

                    string riferimento = nota.A > nota.Da
                        ? $"{gruppo.Intestazione} {nota.Capitolo}, {nota.Da}-{nota.A}"
                        : $"{gruppo.Intestazione} {nota.Capitolo}, {nota.Da}";

                    Segnalibro(segnalibroLibro, riferimento, false);

                    ScriviParagrafo(riferimento, fontRiferimento, XBrushes.Black);
                    ScriviParagrafo(nota.Testo, fontCorsivo, pennelloNota, indentazione: 12, icona: iconaNota);
                }

                y += 6;
            }

            grafica.Dispose();

            if (!finale)
                return;

            // Piè di pagina su ogni pagina: data di esportazione e
            // "Pagina N di M" (M si conosce solo a documento finito).
            string dataEsportazione = DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("it-IT"));
            int totalePagine = documento.PageCount;

            for (int i = 0; i < totalePagine; i++)
            {
                var paginaPiede = documento.Pages[i];
                using var graficaPiede = XGraphics.FromPdfPage(paginaPiede);

                double yPiede = paginaPiede.Height - 28;

                graficaPiede.DrawLine(XPens.LightGray, margine, yPiede - 12, paginaPiede.Width - margine, yPiede - 12);

                graficaPiede.DrawString($"Emmaus 2.0 - esportato il {dataEsportazione}",
                    fontPiede, XBrushes.Gray, new XPoint(margine, yPiede));

                string testoPagina = $"Pagina {i + 1} di {totalePagine}";
                double larghezzaTesto = graficaPiede.MeasureString(testoPagina, fontPiede).Width;

                graficaPiede.DrawString(testoPagina, fontPiede, XBrushes.Gray,
                    new XPoint(paginaPiede.Width - margine - larghezzaTesto, yPiede));
            }

            documento.Save(percorsoFile);
        }

        private static XImage CreaImmagineIconaNota()
        {
            const int dimensione = 32;

            using var bitmap = new Bitmap(dimensione, dimensione);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                using var fontEmoji = new Font("Segoe UI Emoji", dimensione * 0.65f, FontStyle.Regular);
                var dimensioneTesto = g.MeasureString("📝", fontEmoji);
                var punto = new PointF(
                    (dimensione - dimensioneTesto.Width) / 2,
                    (dimensione - dimensioneTesto.Height) / 2);

                g.DrawString("📝", fontEmoji, Brushes.Black, punto);
            }

            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;

            return XImage.FromStream(stream);
        }

        private enum EsitoConfermaEliminazione
        {
            Annulla,
            BackupEdElimina,
            EliminaDiretta
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var voci = new List<(bool Selezionato, string Percorso, string Descrizione, GestoreBackup.ElementoBackup Backup)>
            {
                (checkBox9.Checked, percorsoCronologia, "Cronologia (crono.txt)", GestoreBackup.Cronologia),
                (checkBox7.Checked, percorsoCommenti, "DB Commenti (commenti.db)", GestoreBackup.Commenti),
            };

            var selezionate = voci.Where(v => v.Selezionato).ToList();

            bool eliminaPunti = checkBox11.Checked;

            if (selezionate.Count == 0 && !eliminaPunti)
            {
                MessaggioBibbia.Mostra("Seleziona almeno un elemento da eliminare.", "Occhio",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var descrizioni = selezionate.Select(v => v.Descrizione).ToList();
            if (eliminaPunti)
                descrizioni.Add("Tutti i punti di ripristino (cartella BackupDB)");

            var scelta = ChiediConfermaEliminazione(descrizioni, eliminaPunti);
            if (scelta == EsitoConfermaEliminazione.Annulla)
                return;

            if (selezionate.Count > 0)
                PrimaDiEliminare?.Invoke();

            var riusciti = new List<string>();
            var errori = new List<string>();

            var puntiDaEliminare = eliminaPunti
                ? GestoreBackup.ElencaPuntiRipristino()
                : new List<GestoreBackup.PuntoRipristino>();

            Cursor = Cursors.WaitCursor;
            try
            {
                if (scelta == EsitoConfermaEliminazione.BackupEdElimina)
                {
                    var esitoBackup = GestoreBackup.EseguiBackup(
                        selezionate.Select(v => v.Backup), segnalaMancanti: false);

                    foreach (var nomeFile in esitoBackup.Riusciti)
                        riusciti.Add($"Backup creato in BackupDB: {nomeFile}");

                    foreach (var errore in esitoBackup.Errori)
                        errori.Add($"Backup: {errore}");

                    if (esitoBackup.Errori.Count > 0)
                    {
                        MostraEsito("Eliminazione annullata", riusciti, errori, "Esito:",
                            Environment.NewLine + Environment.NewLine +
                            "Nessun file è stato eliminato perché il backup non è andato a buon fine.");
                        CaricaPuntiRipristino();
                        return;
                    }
                }

                SqliteConnection.ClearAllPools();

                foreach (var voce in selezionate)
                {
                    try
                    {
                        if (!File.Exists(voce.Percorso))
                        {
                            riusciti.Add($"{voce.Descrizione} (già assente)");
                            continue;
                        }

                        File.Delete(voce.Percorso);
                        riusciti.Add($"Eliminato: {voce.Descrizione}");
                    }
                    catch (Exception ex)
                    {
                        errori.Add($"{voce.Descrizione}: {ex.Message}");
                    }
                }

                if (eliminaPunti)
                {
                    var esitoPunti = GestoreBackup.EliminaPunti(puntiDaEliminare);
                    riusciti.AddRange(esitoPunti.Riusciti);
                    errori.AddRange(esitoPunti.Errori);
                }

                CaricaPuntiRipristino();
                CaricaLibriEsportazione();

                MostraEsito("Eliminazione", riusciti, errori, "Esito:");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private EsitoConfermaEliminazione ChiediConfermaEliminazione(List<string> descrizioni, bool conPuntiRipristino = false)
        {
            using var dlg = new Form
            {
                Text = "❗ Conferma eliminazione ❗",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowIcon = false,
                AutoScaleMode = AutoScaleMode.Font,
                Font = this.Font,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            TemaBibbia.Applica(dlg);

            // F1: guida sull'eliminazione dei dati (9.5).
            dlg.KeyPreview = true;
            dlg.KeyDown += (s, args) =>
            {
                if (args.KeyCode == Keys.F1)
                {
                    FormGUIDA.Mostra(this, "s9-5");
                    args.Handled = true;
                    args.SuppressKeyPress = true;
                }
            };

            var layoutPrincipale = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = new Padding(20),
                Dock = DockStyle.Fill
            };

            string elenco = string.Join(Environment.NewLine, descrizioni.Select(d => "•  " + d));

            var lblMessaggio = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(
                    (int)Math.Round(
                        440 * (Font.SizeInPoints / 18f) * (DeviceDpi / 96f)),
                    0),
                Margin = new Padding(0, 0, 0, 15),
                Text =
                    "Questa operazione è IRREVERSIBILE ed elimina definitivamente:" + Environment.NewLine + Environment.NewLine +
                    elenco + Environment.NewLine + Environment.NewLine +
                    (conPuntiRipristino
                        ? "Con \"Backup ed Elimina\" il nuovo backup creato adesso viene conservato; " +
                          "vengono eliminati solo i punti di ripristino già esistenti." +
                          Environment.NewLine + Environment.NewLine
                        : "") +
                    "Cosa vuoi fare?"
            };

            var rigaPulsanti = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                Anchor = AnchorStyles.None
            };

            var btnBackupElimina = new Button
            {
                Text = "Backup ed Elimina",
                DialogResult = DialogResult.Yes,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12, 4, 12, 4),
                Margin = new Padding(0, 0, 10, 0)
            };
            var btnEliminaDiretta = new Button
            {
                Text = "Elimina direttamente",
                DialogResult = DialogResult.No,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12, 4, 12, 4),
                Margin = new Padding(0, 0, 10, 0)
            };
            var btnAnnulla = new Button
            {
                Text = "Annulla",
                DialogResult = DialogResult.Cancel,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(12, 4, 12, 4)
            };

            rigaPulsanti.Controls.Add(btnBackupElimina);
            rigaPulsanti.Controls.Add(btnEliminaDiretta);
            rigaPulsanti.Controls.Add(btnAnnulla);

            layoutPrincipale.Controls.Add(lblMessaggio);
            layoutPrincipale.Controls.Add(rigaPulsanti);

            dlg.Controls.Add(layoutPrincipale);
            dlg.CancelButton = btnAnnulla;

            DialogResult risultato = dlg.ShowDialog(this);

            return risultato switch
            {
                DialogResult.Yes => EsitoConfermaEliminazione.BackupEdElimina,
                DialogResult.No => EsitoConfermaEliminazione.EliminaDiretta,
                _ => EsitoConfermaEliminazione.Annulla
            };
        }

        private void MostraEsito(string titoloOperazione, List<string> riusciti, List<string> errori,
            string etichettaRiusciti, string messaggioAggiuntivo = "")
        {
            var testo = new System.Text.StringBuilder();

            if (riusciti.Count > 0)
            {
                testo.AppendLine(etichettaRiusciti);
                foreach (var voce in riusciti)
                    testo.AppendLine($"• {voce}");
            }

            if (errori.Count > 0)
            {
                if (testo.Length > 0) testo.AppendLine();
                testo.AppendLine("Errori:");
                foreach (var voce in errori)
                    testo.AppendLine($"• {voce}");
            }

            testo.Append(messaggioAggiuntivo);

            MessaggioBibbia.Mostra(testo.ToString(), titoloOperazione, MessageBoxButtons.OK,
                errori.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
    }
}
