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
using Listbox;
using Microsoft.Data.Sqlite;
using System.Media;
using System.Text.RegularExpressions;

namespace Input
{
    public partial class FormINSERISCI : Form
    {
        private string connectionStringNote =
            $"Data Source={Path.Combine(AppContext.BaseDirectory, "commenti.db")}";

        private string connectionStringVersettiFts =
            $"Data Source={Path.Combine(AppContext.BaseDirectory, "versetti_fts.db")}";

        private string connectionStringBibbia =
            $"Data Source={Path.Combine(AppContext.BaseDirectory, "CEI_08.SQLite3")};Mode=ReadOnly;";

        // Riferimento da aprire subito nel form Leggi (argomento di avvio
        // --riferimento "Luca 24, 32"); impostato da Program.
        public string? RiferimentoIniziale { get; set; }

        private Point posizioneOriginale;

        private RotellinaAccorpata rotellinaNota;

        private FormLEGGI formLeggiAperta;

        private FormTIMER formTimerAperto;

        private HashSet<int> versettiConNota = new HashSet<int>();

        // Tag (#parola) gia' usati nei commenti, per i suggerimenti.
        private List<string> tagEsistenti = new List<string>();
        private HashSet<string> libriConNota = new HashSet<string>();

        private SuggerimentiLibri suggerimentiLibri;
        private SuggerimentiNumeri suggerimentiCapitolo;
        private SuggerimentiNumeri suggerimentiVersettoDa;
        private SuggerimentiNumeri suggerimentiVersettoFine;
        private HashSet<int> capitoliConNota = new HashSet<int>();

        private const string PercorsoCronologia = "crono.txt";
        private const int MaxCronologiaIndietro = 7;
        private const int MaxCronologiaAvanti = 7;

        private class VoceCronologia
        {
            public int BookNumber;
            public int Capitolo;
            public int? VersettoDa;
            public int? VersettoA;
        }

        private List<VoceCronologia> cronologia =
            new List<VoceCronologia>();

        private int indiceCronologia = -1;

        private bool navigazioneCronologiaInCorso = false;

        private class VoceCombo
        {
            public string Testo { get; set; }
            public int? Valore { get; set; }

            public override string ToString() => Testo;
        }

        private class VoceLibro
        {
            public bool IsHeader { get; set; }
            public string Testo { get; set; }
            public LibroBiblico Libro { get; set; }

            public override string ToString() => Testo;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F1:
                    FormGUIDA.Mostra(this, "c3");
                    return true;

                case Keys.Control | Keys.I:
                    ScorciatoieBibbia.Premi(button_random);
                    return true;

                case Keys.Control | Keys.L:
                    ScorciatoieBibbia.Premi(button_leggi);
                    return true;

                case Keys.Control | Keys.R:
                    ScorciatoieBibbia.Premi(button_cerca);
                    return true;

                case Keys.Control | Keys.C:
                    if (ScorciatoieBibbia.CopiaTestoSelezionato(ActiveControl))
                        break;
                    ScorciatoieBibbia.Premi(button_inserisci);
                    return true;

                case Keys.Control | Keys.M:
                    ScorciatoieBibbia.Premi(button_manutenzione);
                    return true;

                case Keys.Control | Keys.Left:
                    if (ScorciatoieBibbia.ScritturaInCorso(ActiveControl))
                        break;
                    if (ScorciatoieBibbia.Premi(button_indietro))
                        return true;
                    break;

                case Keys.Control | Keys.Right:
                    if (ScorciatoieBibbia.ScritturaInCorso(ActiveControl))
                        break;
                    if (ScorciatoieBibbia.Premi(button_avanti))
                        return true;
                    break;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        public FormINSERISCI()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            TemaBibbia.Applica(this);

            TemaBibbia.IconaIngranaggio(button_manutenzione);
            TemaBibbia.Suggerimento(button_manutenzione, "Manutenzione (Ctrl+M)");

            TemaBibbia.Suggerimento(button_random, "Ispirami (Ctrl+I)");
            TemaBibbia.Suggerimento(button_leggi, "Leggi (Ctrl+L)");
            TemaBibbia.Suggerimento(button_cerca, "Ricerca (Ctrl+R)");
            TemaBibbia.Suggerimento(button_inserisci, "Commenta (Ctrl+C)");
            TemaBibbia.Suggerimento(button_indietro, "Indietro (Ctrl+←)");
            TemaBibbia.Suggerimento(button_avanti, "Avanti (Ctrl+→)");

            this.Load += FormINSERISCI_Load;

            textBox_nota.PlaceholderText =
                "Nota da inserire\r\n" +
                "o testo da cercare\r\n" +
                "(la ricerca riconosce:\r\n" +
                "virgolette, AND/OR/|, +/-, wildcard *,\r\n" +
                "intervalli N..M, #tag.)";

            textBox_nota.AcceptsReturn = true;
            textBox_nota.ScrollBars = ScrollBars.Vertical;

            textBox_nota.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && e.Shift)
                {
                    textBox_nota.SelectedText = Environment.NewLine;
                    e.SuppressKeyPress = true;
                    e.Handled = true;
                }
            };

            rotellinaNota = new RotellinaAccorpata(textBox_nota);

            UndoTesto.Collega(textBox_nota, 10);

            // Scrivendo "#gi" propone i tag gia' usati nei commenti
            // (vale sia per un commento nuovo sia per la ricerca).
            SuggerimentiTag.Collega(
                textBox_nota,
                () => tagEsistenti);

            comboBoxLibri.IntegralHeight = false;

            comboBoxLibri.Leave += (s, e) =>
                ConfermaLibroDigitato();

            comboBoxLibri.TextChanged += (s, e) =>
                AggiornaColoreTestoLibro();

            comboBoxLibri.SelectedIndexChanged += (s, e) =>
                AggiornaColoreTestoLibro();
            comboBox_capitolo.IntegralHeight = false;
            comboBox_versettoDa.IntegralHeight = false;
            comboBox_versettoFine.IntegralHeight = false;

            suggerimentiCapitolo = new SuggerimentiNumeri(comboBox_capitolo)
            {
                HaNota = t => int.TryParse(t, out int n) && capitoliConNota.Contains(n)
            };
            suggerimentiVersettoDa = new SuggerimentiNumeri(comboBox_versettoDa)
            {
                HaNota = t => int.TryParse(t, out int n) && versettiConNota.Contains(n)
            };
            suggerimentiVersettoFine = new SuggerimentiNumeri(comboBox_versettoFine)
            {
                HaNota = t => int.TryParse(t, out int n) && versettiConNota.Contains(n)
            };

            comboBoxLibri.SelectedIndexChanged += (s, e) => AggiornaPulsantiCronologia();
            comboBox_capitolo.SelectedIndexChanged += (s, e) => AggiornaPulsantiCronologia();
            comboBox_versettoDa.SelectedIndexChanged += (s, e) => AggiornaPulsantiCronologia();
            comboBox_versettoFine.SelectedIndexChanged += (s, e) => AggiornaPulsantiCronologia();

            this.Shown += (s, e) =>
                AggiornaAltezzaTendina(comboBoxLibri);

        }

        private void AggiornaAltezzaTendina(ComboBox combo)
        {
            if (!combo.IsHandleCreated || !combo.Visible)
                return;

            var areaSchermo =
                Screen.FromControl(combo).WorkingArea;

            int yAltoControllo =
                combo.PointToScreen(Point.Empty).Y;

            int yBassoControllo =
                combo.PointToScreen(
                    new Point(0, combo.Height)).Y;

            int spazioSotto =
                areaSchermo.Bottom - yBassoControllo;

            int spazioSopra =
                yAltoControllo - areaSchermo.Top;

            float scala = combo.DeviceDpi / 96f;
            int margineSicurezza = (int)Math.Round(20 * scala);
            int altezzaMinima = (int)Math.Round(150 * scala);

            int spazioSicuro =
                Math.Min(
                    spazioSotto,
                    spazioSopra) -
                margineSicurezza;

            combo.DropDownHeight =
                Math.Max(
                    spazioSicuro,
                    altezzaMinima);
        }

        private static void ManutenzioneIndiceNote(
            SqliteConnection conn,
            bool rebuild = false)
        {
            try
            {
                using var cmd = conn.CreateCommand();

                if (rebuild)
                {
                    cmd.CommandText =
                        "INSERT INTO NoteFts(NoteFts) VALUES('rebuild');";

                    cmd.ExecuteNonQuery();
                }

                cmd.CommandText =
                    "INSERT INTO NoteFts(NoteFts) VALUES('optimize');";

                cmd.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                using var cmdRebuild =
                    conn.CreateCommand();

                cmdRebuild.CommandText =
                    "INSERT INTO NoteFts(NoteFts) VALUES('rebuild');";

                cmdRebuild.ExecuteNonQuery();
            }
        }

        private void PreparaDbNote()
        {
            using (var conn =
                new SqliteConnection(connectionStringNote))
            {
                conn.Open();

                var cmd = conn.CreateCommand();

                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Note (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Libro TEXT NOT NULL,
                        Capitolo INTEGER NOT NULL,
                        VersettoDa INTEGER NOT NULL,
                        VersettoA INTEGER NOT NULL,
                        Traduzione TEXT NOT NULL DEFAULT 'CEI2008',
                        TestoNota TEXT NOT NULL,
                        DataCreazione TEXT DEFAULT CURRENT_TIMESTAMP,
                        Ordine INTEGER
                    );";

                cmd.ExecuteNonQuery();

                cmd.CommandText =
                    "PRAGMA table_info(Note);";

                var colonne = new List<string>();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        colonne.Add(reader.GetString(1));
                }

                if (colonne.Contains("Versetto") &&
                    !colonne.Contains("VersettoDa"))
                {
                    cmd.CommandText =
                        "ALTER TABLE Note ADD COLUMN VersettoDa INTEGER;";

                    cmd.ExecuteNonQuery();

                    cmd.CommandText =
                        "ALTER TABLE Note ADD COLUMN VersettoA INTEGER;";

                    cmd.ExecuteNonQuery();

                    cmd.CommandText =
                        "UPDATE Note SET VersettoDa = Versetto, " +
                        "VersettoA = Versetto " +
                        "WHERE VersettoDa IS NULL;";

                    cmd.ExecuteNonQuery();
                }

                if (!colonne.Contains("Ordine"))
                {
                    cmd.CommandText =
                        "ALTER TABLE Note ADD COLUMN Ordine INTEGER;";

                    cmd.ExecuteNonQuery();
                }

                PreparaRicercaFts5(conn);
            }

            PreparaDbVersettiFts();
        }

        private void PreparaRicercaFts5(
            SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                CREATE VIRTUAL TABLE IF NOT EXISTS NoteFts USING fts5(
                    TestoNota,
                    content='Note',
                    content_rowid='Id',
                    tokenize = 'unicode61 remove_diacritics 2'
                );";

            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TRIGGER IF NOT EXISTS Note_ai
                AFTER INSERT ON Note
                BEGIN
                    INSERT INTO NoteFts(rowid, TestoNota)
                    VALUES (new.Id, new.TestoNota);
                END;";

            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TRIGGER IF NOT EXISTS Note_ad
                AFTER DELETE ON Note
                BEGIN
                    INSERT INTO NoteFts(NoteFts, rowid, TestoNota)
                    VALUES('delete', old.Id, old.TestoNota);
                END;";

            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TRIGGER IF NOT EXISTS Note_au
                AFTER UPDATE ON Note
                BEGIN
                    INSERT INTO NoteFts(NoteFts, rowid, TestoNota)
                    VALUES('delete', old.Id, old.TestoNota);

                    INSERT INTO NoteFts(rowid, TestoNota)
                    VALUES (new.Id, new.TestoNota);
                END;";

            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                INSERT INTO NoteFts(rowid, TestoNota)
                SELECT Id, TestoNota
                FROM Note
                WHERE Id NOT IN (
                    SELECT rowid FROM NoteFts
                );";

            cmd.ExecuteNonQuery();
        }

        private void PreparaDbVersettiFts()
        {
            try
            {
                using (var connFts =
                    new SqliteConnection(
                        connectionStringVersettiFts))
                {
                    connFts.Open();

                    using var cmd =
                        connFts.CreateCommand();

                    cmd.CommandText = @"
                        CREATE VIRTUAL TABLE IF NOT EXISTS VersesFts USING fts5(
                            book_number UNINDEXED,
                            chapter UNINDEXED,
                            verse UNINDEXED,
                            text,
                            tokenize = 'unicode61 remove_diacritics 2'
                        );";

                    cmd.ExecuteNonQuery();

                    cmd.CommandText =
                        "SELECT COUNT(*) FROM VersesFts;";

                    long totaleVersettiIndicizzati =
                        (long)cmd.ExecuteScalar();

                    if (totaleVersettiIndicizzati == 0)
                        PopolaIndiceVersetti(connFts);

                    ManutenzioneIndiceVersettiFts(
                        connFts);
                }
            }
            catch (SqliteException)
            {
                RicreaDbVersettiFts();
            }
        }

        private void PopolaIndiceVersetti(
            SqliteConnection connFts)
        {
            using var connBibbia =
                new SqliteConnection(
                    connectionStringBibbia);

            connBibbia.Open();

            using var cmdLettura =
                connBibbia.CreateCommand();

            cmdLettura.CommandText =
                "SELECT book_number, chapter, verse, text " +
                "FROM verses;";

            using var transazione =
                connFts.BeginTransaction();

            using var cmdScrittura =
                connFts.CreateCommand();

            cmdScrittura.Transaction =
                transazione;

            cmdScrittura.CommandText =
                "INSERT INTO VersesFts " +
                "(book_number, chapter, verse, text) " +
                "VALUES (@bn, @cap, @v, @testo);";

            var pBn =
                cmdScrittura.Parameters.Add(
                    "@bn",
                    SqliteType.Integer);

            var pCap =
                cmdScrittura.Parameters.Add(
                    "@cap",
                    SqliteType.Integer);

            var pV =
                cmdScrittura.Parameters.Add(
                    "@v",
                    SqliteType.Integer);

            var pTesto =
                cmdScrittura.Parameters.Add(
                    "@testo",
                    SqliteType.Text);

            using (var reader =
                cmdLettura.ExecuteReader())
            {
                while (reader.Read())
                {
                    pBn.Value =
                        reader.GetInt32(0);

                    pCap.Value =
                        reader.GetInt32(1);

                    pV.Value =
                        reader.GetInt32(2);

                    pTesto.Value =
                        Regex.Replace(
                            reader.GetString(3),
                            "<[^>]+>",
                            "");

                    cmdScrittura.ExecuteNonQuery();
                }
            }

            transazione.Commit();
        }

        private static void ManutenzioneIndiceVersettiFts(
            SqliteConnection connFts)
        {
            try
            {
                using var cmd =
                    connFts.CreateCommand();

                cmd.CommandText =
                    "INSERT INTO VersesFts(VersesFts) " +
                    "VALUES('optimize');";

                cmd.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                using var cmdRebuild =
                    connFts.CreateCommand();

                cmdRebuild.CommandText =
                    "INSERT INTO VersesFts(VersesFts) " +
                    "VALUES('rebuild');";

                cmdRebuild.ExecuteNonQuery();
            }
        }

        private void RicreaDbVersettiFts()
        {
            string percorso =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "versetti_fts.db");

            if (File.Exists(percorso))
                File.Delete(percorso);

            using var connFts =
                new SqliteConnection(
                    connectionStringVersettiFts);

            connFts.Open();

            using var cmd =
                connFts.CreateCommand();

            cmd.CommandText = @"
                CREATE VIRTUAL TABLE VersesFts USING fts5(
                    book_number UNINDEXED,
                    chapter UNINDEXED,
                    verse UNINDEXED,
                    text,
                    tokenize = 'unicode61 remove_diacritics 2'
                );";

            cmd.ExecuteNonQuery();

            PopolaIndiceVersetti(connFts);

            ManutenzioneIndiceVersettiFts(
                connFts);
        }

        private void FormINSERISCI_Load(
            object sender,
            EventArgs e)
        {
            string percorsoCei =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "CEI_08.SQLite3");

            if (!File.Exists(percorsoCei))
            {
                MessaggioBibbia.Mostra(
                    "Il database della Bibbia CEI non è stato trovato.\r\n\r\n" +
                    "Per utilizzare il programma è necessario il file:\r\n" +
                    "CEI_08.SQLite3\r\n\r\n" +
                    "Ripristina il file nella cartella dell'applicazione " +
                    "e riavvia il programma.",
                    "Database CEI mancante",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                BeginInvoke(new Action(() =>
                {
                    Application.Exit();
                }));

                return;
            }

            try
            {

                LibriCEI.Carica(
                    connectionStringBibbia);

                PreparaDbNote();

                PopolaComboLibri();

                AggiornaLibriConNota();

                CaricaCronologiaDaFile();

                EseguiBackupGiornalieroAutomatico();

                ApriRiferimentoIniziale();
            }
            catch (SqliteException ex)
            {
                MessaggioBibbia.Mostra(
                    "Non è possibile aprire il database della Bibbia CEI.\r\n\r\n" +
                    "File:\r\n" +
                    percorsoCei +
                    "\r\n\r\n" +
                    "Errore SQLite:\r\n" +
                    ex.Message,
                    "Errore database CEI",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                BeginInvoke(new Action(() =>
                {
                    Application.Exit();
                }));
            }
            catch (Exception ex)
            {
                MessaggioBibbia.Mostra(
                    "Si è verificato un errore durante l'avvio del programma.\r\n\r\n" +
                    ex.Message,
                    "Errore di avvio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                BeginInvoke(new Action(() =>
                {
                    Application.Exit();
                }));
            }
        }

        // Se il programma e' stato avviato con --riferimento "Libro cap, v"
        // (o "Libro cap, v-w", o solo "Libro cap") apre quel passo nel form
        // Leggi, come se fosse stato scelto dalle tendine.
        private void ApriRiferimentoIniziale()
        {
            string testo =
                RiferimentoIniziale?.Trim() ?? "";

            RiferimentoIniziale = null;

            if (testo.Length == 0)
                return;

            var m =
                Regex.Match(
                    testo,
                    @"^(?<libro>.+?)\s+(?<cap>\d+)" +
                    @"(?:\s*[,:]\s*(?<da>\d+)(?:\s*-\s*(?<a>\d+))?)?\s*$");

            if (!m.Success)
                return;

            string nomeLibro =
                m.Groups["libro"].Value.Trim();

            var libro =
                RiferimentiBiblici.TrovaLibro(nomeLibro) ??
                LibriCEI.Elenco.FirstOrDefault(
                    l =>
                        string.Equals(
                            l.Nome,
                            nomeLibro,
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            l.Abbreviazione,
                            nomeLibro,
                            StringComparison.OrdinalIgnoreCase));

            if (libro == null)
                return;

            int capitolo =
                int.Parse(m.Groups["cap"].Value);

            int? versettoDa =
                m.Groups["da"].Success
                    ? int.Parse(m.Groups["da"].Value)
                    : null;

            int? versettoA =
                m.Groups["a"].Success
                    ? int.Parse(m.Groups["a"].Value)
                    : versettoDa;

            // Dopo il caricamento della finestra, cosi' la posizione di
            // FormINSERISCI e' quella definitiva.
            BeginInvoke(new Action(() =>
            {
                AggiornaComboBoxDaRiferimento(
                    libro,
                    capitolo,
                    versettoDa,
                    versettoA);

                ApriFormLeggi(
                    libro,
                    capitolo,
                    versettoDa,
                    versettoA);
            }));
        }

        private void EseguiBackupGiornalieroAutomatico()
        {
            try
            {
                var esito =
                    GestoreBackup.BackupGiornalieroAutomatico();

                if (esito.Errori.Count > 0)
                {
                    MessaggioBibbia.Mostra(
                        "Il backup giornaliero automatico non è stato completato:" +
                        "\r\n\r\n" +
                        string.Join(
                            "\r\n",
                            esito.Errori.Select(x => "• " + x)),
                        "Backup automatico",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessaggioBibbia.Mostra(
                    "Il backup giornaliero automatico non è stato eseguito:" +
                    "\r\n\r\n" + ex.Message,
                    "Backup automatico",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void CaricaCronologiaDaFile()
        {
            cronologia.Clear();
            indiceCronologia = -1;

            if (!File.Exists(
                PercorsoCronologia))
            {
                using (File.Create(
                    PercorsoCronologia))
                {
                }

                AggiornaPulsantiCronologia();

                return;
            }

            foreach (var riga in
                     File.ReadAllLines(
                         PercorsoCronologia))
            {
                var parti =
                    riga.Split('|');

                if (parti.Length != 4)
                    continue;

                if (!int.TryParse(
                    parti[0],
                    out int bookNumber))
                    continue;

                if (!int.TryParse(
                    parti[1],
                    out int capitolo))
                    continue;

                int? versettoDa =
                    int.TryParse(
                        parti[2],
                        out int vd) &&
                    vd >= 0
                        ? vd
                        : (int?)null;

                int? versettoA =
                    int.TryParse(
                        parti[3],
                        out int va) &&
                    va >= 0
                        ? va
                        : (int?)null;

                cronologia.Add(
                    new VoceCronologia
                    {
                        BookNumber =
                            bookNumber,

                        Capitolo =
                            capitolo,

                        VersettoDa =
                            versettoDa,

                        VersettoA =
                            versettoA
                    });
            }

            indiceCronologia =
                cronologia.Count - 1;

            AggiornaPulsantiCronologia();
        }

        private void SalvaCronologiaSuFile()
        {
            var righe =
                cronologia.Select(v =>
                    $"{v.BookNumber}|{v.Capitolo}|" +
                    $"{v.VersettoDa?.ToString() ?? "-1"}|" +
                    $"{v.VersettoA?.ToString() ?? "-1"}");

            File.WriteAllLines(
                PercorsoCronologia,
                righe);
        }

        private void AggiungiCronologia(
            LibroBiblico libro,
            int capitolo,
            int? versettoDa,
            int? versettoA)
        {
            if (navigazioneCronologiaInCorso ||
                libro == null)
            {
                return;
            }

            if (indiceCronologia >= 0 &&
                indiceCronologia < cronologia.Count)
            {
                var corrente =
                    cronologia[
                        indiceCronologia];

                if (corrente.BookNumber ==
                        libro.BookNumber &&
                    corrente.Capitolo ==
                        capitolo &&
                    corrente.VersettoDa ==
                        versettoDa &&
                    corrente.VersettoA ==
                        versettoA)
                {
                    return;
                }
            }

            if (indiceCronologia <
                cronologia.Count - 1)
            {
                cronologia.RemoveRange(
                    indiceCronologia + 1,
                    cronologia.Count -
                    indiceCronologia - 1);
            }

            cronologia.Add(
                new VoceCronologia
                {
                    BookNumber =
                        libro.BookNumber,

                    Capitolo =
                        capitolo,

                    VersettoDa =
                        versettoDa,

                    VersettoA =
                        versettoA
                });

            indiceCronologia =
                cronologia.Count - 1;

            ApplicaLimitiCronologia();

            SalvaCronologiaSuFile();

            AggiornaPulsantiCronologia();
        }

        private void ApplicaLimitiCronologia()
        {
            while (indiceCronologia >
                   MaxCronologiaIndietro)
            {
                cronologia.RemoveAt(0);

                indiceCronologia--;
            }

            while (
                cronologia.Count - 1 -
                indiceCronologia >
                MaxCronologiaAvanti)
            {
                cronologia.RemoveAt(
                    cronologia.Count - 1);
            }
        }

        private void AggiornaPulsantiCronologia()
        {
            button_indietro.Enabled =
                indiceCronologia > 0;

            bool avantiInCronologia =
                indiceCronologia >= 0 &&
                indiceCronologia <
                    cronologia.Count - 1;

            button_avanti.Enabled =
                avantiInCronologia ||
                TrovaRiferimentoSuccessivo(
                    out _, out _, out _, out _);
        }

        private void NavigaAVoceCronologia(
            VoceCronologia voce)
        {
            var libro =
                LibriCEI.Elenco.FirstOrDefault(
                    l =>
                        l.BookNumber ==
                        voce.BookNumber);

            if (libro == null)
                return;

            navigazioneCronologiaInCorso = true;

            try
            {
                AggiornaComboBoxDaRiferimento(
                    libro,
                    voce.Capitolo,
                    voce.VersettoDa,
                    voce.VersettoA);

                ApriFormLeggi(
                    libro,
                    voce.Capitolo,
                    voce.VersettoDa,
                    voce.VersettoA);
            }
            finally
            {
                navigazioneCronologiaInCorso =
                    false;
            }
        }

        private void button_indietro_Click(
            object sender,
            EventArgs e)
        {
            if (indiceCronologia <= 0)
                return;

            indiceCronologia--;

            ApplicaLimitiCronologia();

            SalvaCronologiaSuFile();

            AggiornaPulsantiCronologia();

            NavigaAVoceCronologia(
                cronologia[
                    indiceCronologia]);
        }

        private void button_avanti_Click(
            object sender,
            EventArgs e)
        {
            ConfermaComboNumerici();

            if (indiceCronologia >= 0 &&
                indiceCronologia <
                    cronologia.Count - 1)
            {
                indiceCronologia++;

                ApplicaLimitiCronologia();

                SalvaCronologiaSuFile();

                AggiornaPulsantiCronologia();

                NavigaAVoceCronologia(
                    cronologia[
                        indiceCronologia]);

                return;
            }

            if (!TrovaRiferimentoSuccessivo(
                    out var libro,
                    out int cap,
                    out int versDa,
                    out int vers))
            {
                SystemSounds.Beep.Play();
                return;
            }

            AggiornaComboBoxDaRiferimento(
                libro,
                cap,
                versDa,
                vers);

            ApriFormLeggi(
                libro,
                cap,
                versDa,
                vers);
        }

        private static int? ScalarInt(
            SqliteConnection conn,
            string sql,
            params (string Nome, object Valore)[] parametri)
        {
            using var cmd =
                conn.CreateCommand();

            cmd.CommandText = sql;

            foreach (var (nome, valore) in parametri)
                cmd.Parameters.AddWithValue(
                    nome,
                    valore);

            object r =
                cmd.ExecuteScalar();

            return (r == null ||
                    r == DBNull.Value)
                ? null
                : (int?)Convert.ToInt32(r);
        }

        private bool TrovaRiferimentoSuccessivo(
            out LibroBiblico libroSucc,
            out int capSucc,
            out int versDaSucc,
            out int versSucc)
        {
            libroSucc = null;
            capSucc = 0;
            versDaSucc = 0;
            versSucc = 0;

            var libro =
                LibroSelezionato;

            if (libro == null ||
                comboBox_capitolo.SelectedItem
                    is not VoceCombo capVoce ||
                capVoce.Valore == null)
            {
                return false;
            }

            int cap =
                capVoce.Valore.Value;

            int? da =
                (comboBox_versettoDa.SelectedItem
                    as VoceCombo)?.Valore;

            int? fine =
                (comboBox_versettoFine.SelectedItem
                    as VoceCombo)?.Valore;

            int? ultimo =
                da.HasValue
                    ? (fine ?? da)
                    : null;

            try
            {
                using var conn =
                    new SqliteConnection(
                        connectionStringBibbia);

                conn.Open();

                if (ultimo.HasValue)
                {
                    int? v = ScalarInt(
                        conn,
                        "SELECT MIN(verse) FROM verses " +
                        "WHERE book_number = @bn " +
                        "AND chapter = @cap " +
                        "AND verse > @v;",
                        ("@bn", libro.BookNumber),
                        ("@cap", cap),
                        ("@v", ultimo.Value));

                    if (v.HasValue)
                    {
                        libroSucc = libro;
                        capSucc = cap;
                        versDaSucc = v.Value;
                        versSucc = v.Value;
                        return true;
                    }
                }

                int? capN = ScalarInt(
                    conn,
                    "SELECT MIN(chapter) FROM verses " +
                    "WHERE book_number = @bn " +
                    "AND chapter > @cap;",
                    ("@bn", libro.BookNumber),
                    ("@cap", cap));

                if (capN.HasValue)
                {
                    int? v = ScalarInt(
                        conn,
                        "SELECT MIN(verse) FROM verses " +
                        "WHERE book_number = @bn " +
                        "AND chapter = @cap;",
                        ("@bn", libro.BookNumber),
                        ("@cap", capN.Value));

                    if (v.HasValue)
                    {
                        libroSucc = libro;
                        capSucc = capN.Value;
                        versDaSucc = v.Value;
                        versSucc = v.Value;
                        return true;
                    }
                }

                int? bnN = ScalarInt(
                    conn,
                    "SELECT MIN(book_number) FROM verses " +
                    "WHERE book_number > @bn;",
                    ("@bn", libro.BookNumber));

                if (bnN.HasValue)
                {
                    var libroN =
                        LibriCEI.Elenco.FirstOrDefault(
                            x =>
                                x.BookNumber ==
                                bnN.Value);

                    int? c = ScalarInt(
                        conn,
                        "SELECT MIN(chapter) FROM verses " +
                        "WHERE book_number = @bn;",
                        ("@bn", bnN.Value));

                    int? v = c.HasValue
                        ? ScalarInt(
                            conn,
                            "SELECT MIN(verse) FROM verses " +
                            "WHERE book_number = @bn " +
                            "AND chapter = @cap;",
                            ("@bn", bnN.Value),
                            ("@cap", c.Value))
                        : null;

                    if (libroN != null &&
                        c.HasValue &&
                        v.HasValue)
                    {
                        libroSucc = libroN;
                        capSucc = c.Value;
                        versDaSucc = v.Value;
                        versSucc = v.Value;
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private void ConfermaComboNumerici()
        {
            suggerimentiCapitolo?.Conferma();
            suggerimentiVersettoDa?.Conferma();
            suggerimentiVersettoFine?.Conferma();
        }

        private static bool NotaContieneRiferimento(
            string testoNota)
        {
            return !string.IsNullOrEmpty(
                       testoNota) &&
                   RiferimentiBiblici.Trova(
                       testoNota,
                       0).Count > 0;
        }

        private void AggiornaLibriConNota()
        {
            libriConNota.Clear();

            tagEsistenti =
                TagNote.LeggiTuttiITag(
                    connectionStringNote);

            using (var conn =
                new SqliteConnection(
                    connectionStringNote))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    "SELECT Libro, TestoNota " +
                    "FROM Note;";

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    string libro =
                        reader.GetString(0);

                    string testoNota =
                        reader.GetString(1);

                    if (NotaContieneRiferimento(
                        testoNota))
                    {
                        libriConNota.Add(
                            libro);
                    }
                }
            }

            AggiornaColoreTestoLibro();
        }

        public void AggiornaColoriNote()
        {
            AggiornaLibriConNota();

            var libro =
                LibroSelezionato;

            if (libro != null)
            {
                AggiornaCapitoliConNota(
                    libro);

                if (comboBox_capitolo.SelectedItem
                    is VoceCombo cap &&
                    cap.Valore.HasValue)
                {
                    AggiornaVersettiConNota(
                        libro,
                        cap.Valore.Value);
                }
            }

            comboBoxLibri.Invalidate();
            comboBox_capitolo.Invalidate();
            comboBox_versettoDa.Invalidate();
            comboBox_versettoFine.Invalidate();
        }

        private void AggiornaCapitoliConNota(
            LibroBiblico libro)
        {
            capitoliConNota.Clear();

            using (var conn =
                new SqliteConnection(
                    connectionStringNote))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    "SELECT Capitolo, TestoNota " +
                    "FROM Note " +
                    "WHERE Libro = @libro;";

                cmd.Parameters.AddWithValue(
                    "@libro",
                    libro.Nome);

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    int capitolo =
                        reader.GetInt32(0);

                    string testoNota =
                        reader.GetString(1);

                    if (NotaContieneRiferimento(
                        testoNota))
                    {
                        capitoliConNota.Add(
                            capitolo);
                    }
                }
            }
        }

        private void PopolaComboLibri()
        {
            var voci =
                new List<VoceLibro>();

            string categoriaCorrente = null;

            foreach (var libro in
                     LibriCEI.Elenco)
            {
                var (_, categoria) =
                    LibriCEI.CategoriaDiLibro(
                        libro.BookNumber);

                if (categoria !=
                    categoriaCorrente)
                {
                    voci.Add(
                        new VoceLibro
                        {
                            IsHeader = true,

                            Testo =
                                categoria.ToUpper()
                        });

                    categoriaCorrente =
                        categoria;
                }

                voci.Add(
                    new VoceLibro
                    {
                        IsHeader = false,

                        Testo =
                            LibriCEI.NomeVisualizzato(
                                libro),

                        Libro = libro
                    });
            }

            comboBoxLibri.DataSource =
                voci;

            comboBoxLibri.DisplayMember =
                "Testo";

            if (suggerimentiLibri == null)
            {
                suggerimentiLibri =
                    new SuggerimentiLibri(
                        comboBoxLibri,
                        ScegliLibro,
                        ConfermaLibroDigitato)
                    {
                        HaNota = l =>
                            libriConNota.Contains(l.Nome)
                    };
            }

            suggerimentiLibri.Ricarica();

            AggiornaAltezzaTendina(
                comboBoxLibri);
        }

        private void AggiornaColoreTestoLibro()
        {
            var libro =
                LibroSelezionato;

            bool completato =
                libro != null &&
                comboBoxLibri.Text ==
                comboBoxLibri.GetItemText(
                    comboBoxLibri.SelectedItem);

            bool haNota =
                completato &&
                libriConNota.Contains(
                    libro.Nome);

            Color colore =
                haNota
                    ? TemaBibbia.Riferimento
                    : TemaBibbia.Avorio;

            if (comboBoxLibri.ForeColor != colore)
                comboBoxLibri.ForeColor = colore;
        }

        private void ConfermaLibroDigitato()
        {
            suggerimentiLibri?.Nascondi();

            string testo =
                comboBoxLibri.Text?.Trim();

            if (string.IsNullOrEmpty(testo))
            {
                RipristinaTestoLibro();
                return;
            }

            var libro =
                RiferimentiBiblici.TrovaLibro(testo)
                ?? suggerimentiLibri?.PrimoSuggerimento(
                    testo);

            ScegliLibro(libro);
        }

        private void ScegliLibro(
            LibroBiblico libro)
        {
            suggerimentiLibri?.Nascondi();

            var voce =
                libro == null
                    ? null
                    : comboBoxLibri.Items
                        .Cast<VoceLibro>()
                        .FirstOrDefault(
                            x =>
                                !x.IsHeader &&
                                x.Libro?.BookNumber ==
                                libro.BookNumber);

            if (voce == null)
            {
                RipristinaTestoLibro();
                return;
            }

            if (!ReferenceEquals(
                    comboBoxLibri.SelectedItem,
                    voce))
            {
                comboBoxLibri.SelectedItem =
                    voce;
            }
            else
            {
                comboBoxLibri.Text =
                    voce.Testo;
            }
        }

        private void RipristinaTestoLibro()
        {
            comboBoxLibri.Text =
                (comboBoxLibri.SelectedItem
                    as VoceLibro)?.Testo ?? "";
        }

        private void comboBoxLibri_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            if (comboBoxLibri.SelectedItem
                is VoceLibro selezionata &&
                selezionata.IsHeader)
            {
                if (comboBoxLibri.SelectedIndex <
                    comboBoxLibri.Items.Count - 1)
                {
                    comboBoxLibri.SelectedIndex++;
                }

                return;
            }

            if (comboBoxLibri.SelectedItem
                is not VoceLibro voce ||
                voce.Libro == null)
            {
                return;
            }

            var libro =
                voce.Libro;

            AggiornaCapitoliConNota(
                libro);

            var capitoli =
                new List<VoceCombo>();

            using (var conn =
                new SqliteConnection(
                    connectionStringBibbia))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    "SELECT DISTINCT chapter " +
                    "FROM verses " +
                    "WHERE book_number = @bn " +
                    "ORDER BY chapter;";

                cmd.Parameters.AddWithValue(
                    "@bn",
                    libro.BookNumber);

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    int cap =
                        reader.GetInt32(0);

                    capitoli.Add(
                        new VoceCombo
                        {
                            Testo =
                                cap.ToString(),

                            Valore =
                                cap
                        });
                }
            }

            comboBox_capitolo.DataSource =
                capitoli;

            AggiornaAltezzaTendina(
                comboBox_capitolo);

            if (capitoli.Count > 0)
            {
                comboBox_capitolo.SelectedIndex =
                    0;

                comboBox_capitolo_SelectedIndexChanged(
                    comboBox_capitolo,
                    EventArgs.Empty);
            }
            else
            {
                comboBox_versettoDa.DataSource =
                    null;

                comboBox_versettoFine.DataSource =
                    null;

                comboBox_versettoFine.Enabled =
                    false;
            }
        }

        private void comboBox_capitolo_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            var libro =
                LibroSelezionato;

            if (libro == null)
                return;

            if (comboBox_capitolo.SelectedItem
                is not VoceCombo cap ||
                cap.Valore == null)
            {
                return;
            }

            AggiornaVersettiConNota(
                libro,
                cap.Valore.Value);

            var versetti =
                new List<VoceCombo>
                {
                    new VoceCombo
                    {
                        Testo = "(Tutti)",
                        Valore = null
                    }
                };

            using (var conn =
                new SqliteConnection(
                    connectionStringBibbia))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    "SELECT verse FROM verses " +
                    "WHERE book_number = @bn " +
                    "AND chapter = @cap " +
                    "ORDER BY verse;";

                cmd.Parameters.AddWithValue(
                    "@bn",
                    libro.BookNumber);

                cmd.Parameters.AddWithValue(
                    "@cap",
                    cap.Valore.Value);

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    int v =
                        reader.GetInt32(0);

                    versetti.Add(
                        new VoceCombo
                        {
                            Testo =
                                v.ToString(),

                            Valore =
                                v
                        });
                }
            }

            comboBox_versettoDa.DataSource =
                versetti;

            AggiornaAltezzaTendina(
                comboBox_versettoDa);

            comboBox_versettoDa.SelectedIndex =
                0;

            comboBox_versettoDa_SelectedIndexChanged(
                comboBox_versettoDa,
                EventArgs.Empty);
        }

        private void comboBox_versettoDa_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            var libro =
                LibroSelezionato;

            if (libro == null)
                return;

            if (comboBox_capitolo.SelectedItem
                is not VoceCombo cap ||
                cap.Valore == null)
            {
                return;
            }

            if (comboBox_versettoDa.SelectedItem
                is not VoceCombo da)
            {
                return;
            }

            if (da.Valore == null)
            {
                comboBox_versettoFine.DataSource =
                    null;

                comboBox_versettoFine.Enabled =
                    false;

                return;
            }

            var versettiFine =
                new List<VoceCombo>();

            using (var conn =
                new SqliteConnection(
                    connectionStringBibbia))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    "SELECT verse FROM verses " +
                    "WHERE book_number = @bn " +
                    "AND chapter = @cap " +
                    "AND verse >= @da " +
                    "ORDER BY verse;";

                cmd.Parameters.AddWithValue(
                    "@bn",
                    libro.BookNumber);

                cmd.Parameters.AddWithValue(
                    "@cap",
                    cap.Valore.Value);

                cmd.Parameters.AddWithValue(
                    "@da",
                    da.Valore.Value);

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    int v =
                        reader.GetInt32(0);

                    versettiFine.Add(
                        new VoceCombo
                        {
                            Testo =
                                v.ToString(),

                            Valore =
                                v
                        });
                }
            }

            comboBox_versettoFine.Enabled =
                true;

            comboBox_versettoFine.DataSource =
                versettiFine;

            AggiornaAltezzaTendina(
                comboBox_versettoFine);
        }

        private void AggiornaVersettiConNota(
            LibroBiblico libro,
            int capitolo)
        {
            versettiConNota.Clear();

            using (var conn =
                new SqliteConnection(
                    connectionStringNote))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    "SELECT VersettoDa, VersettoA, TestoNota " +
                    "FROM Note " +
                    "WHERE Libro = @libro " +
                    "AND Capitolo = @cap;";

                cmd.Parameters.AddWithValue(
                    "@libro",
                    libro.Nome);

                cmd.Parameters.AddWithValue(
                    "@cap",
                    capitolo);

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    int da =
                        reader.GetInt32(0);

                    int a =
                        reader.GetInt32(1);

                    string testoNota =
                        reader.GetString(2);

                    if (!NotaContieneRiferimento(
                        testoNota))
                    {
                        continue;
                    }

                    for (int v = da;
                         v <= a;
                         v++)
                    {
                        versettiConNota.Add(v);
                    }
                }
            }
        }

        private void comboBox_versettoDa_DrawItem(
            object sender,
            DrawItemEventArgs e) =>
            DisegnaVoceVersettoConNota(
                comboBox_versettoDa,
                e);

        private void comboBox_versettoFine_DrawItem(
            object sender,
            DrawItemEventArgs e) =>
            DisegnaVoceVersettoConNota(
                comboBox_versettoFine,
                e);

        private void DisegnaVoceVersettoConNota(
            ComboBox combo,
            DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            var voce =
                (VoceCombo)combo.Items[e.Index];

            TemaBibbia.DisegnaSfondoVoce(e);

            bool haNota =
                voce.Valore.HasValue &&
                versettiConNota.Contains(
                    voce.Valore.Value);

            Color coloreTesto;

            if ((e.State &
                DrawItemState.Selected) ==
                DrawItemState.Selected)
            {
                coloreTesto =
                    TemaBibbia.TestoSuOro;
            }
            else
            {
                coloreTesto =
                    haNota
                        ? TemaBibbia.Riferimento
                        : combo.ForeColor;
            }

            TextRenderer.DrawText(
                e.Graphics,
                voce.Testo,
                combo.Font,
                e.Bounds,
                coloreTesto,
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.Left);

            e.DrawFocusRectangle();
        }

        private LibroBiblico LibroSelezionato =>
            (comboBoxLibri.SelectedItem
                as VoceLibro)?.Libro;

        private bool ProvaLeggiRiferimento(
            out LibroBiblico libro,
            out int capitolo,
            out int? versettoDa,
            out int? versettoA)
        {
            ConfermaComboNumerici();

            libro =
                LibroSelezionato;

            capitolo =
                (comboBox_capitolo.SelectedItem
                    as VoceCombo)?.Valore ?? 0;

            versettoDa =
                (comboBox_versettoDa.SelectedItem
                    as VoceCombo)?.Valore;

            versettoA =
                (comboBox_versettoFine.SelectedItem
                    as VoceCombo)?.Valore ??
                versettoDa;

            if (libro == null ||
                capitolo == 0)
            {
                SystemSounds.Exclamation.Play();

                MessaggioBibbia.Mostra(
                    "Seleziona libro e capitolo",
                    "Occhio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return false;
            }

            return true;
        }

        private void ApriFormLeggi(
            LibroBiblico libro,
            int capitolo,
            int? versettoDa,
            int? versettoA)
        {
            var formLeggi =
                new FormLEGGI(
                    libro,
                    capitolo,
                    versettoDa,
                    versettoA,
                    connectionStringBibbia,
                    connectionStringNote,
                    AggiornaComboBoxDaRiferimento,
                    AggiornaColoriNote);

            PosizionaEApriFormLeggi(
                formLeggi);
        }

        private void ApriFormLeggiRicerca(
            string testoRicerca)
        {
            var formLeggi =
                new FormLEGGI(
                    testoRicerca,
                    connectionStringBibbia,
                    connectionStringNote,
                    AggiornaComboBoxDaRiferimento,
                    AggiornaColoriNote);

            PosizionaEApriFormLeggi(
                formLeggi);
        }

        private void PosizionaEApriFormLeggi(
            FormLEGGI formLeggi)
        {
            if (formLeggiAperta != null &&
                !formLeggiAperta.IsDisposed)
            {
                formLeggiAperta.Close();
            }

            posizioneOriginale =
                this.Location;

            formLeggiAperta =
                formLeggi;

            formLeggi.StartPosition =
                FormStartPosition.Manual;

            formLeggi.Location =
                posizioneOriginale;

            formLeggi.FormClosed +=
                (s, args) =>
                {
                    this.Location =
                        posizioneOriginale;

                    formLeggiAperta =
                        null;
                };

            formLeggi.Show();

            ApriFormTimerSeNecessario();

            DisponiFormLeggiTimerInserisci(
                formLeggi);
        }

        private const int MargineTraForm = 50;

        private void ApriFormTimerSeNecessario()
        {
            if (formTimerAperto != null &&
                !formTimerAperto.IsDisposed)
                return;

            formTimerAperto =
                new FormTIMER();

            formTimerAperto.StartPosition =
                FormStartPosition.Manual;

            formTimerAperto.Location =
                new Point(
                    this.Location.X,
                    this.Location.Y);

            formTimerAperto.Show(
                this);
        }

        private void DisponiFormLeggiTimerInserisci(
            FormLEGGI formLeggi)
        {
            var areaSchermo =
                Screen.FromControl(
                    this).WorkingArea;

            int larghezzaColonna =
                Math.Max(
                    this.Width,
                    formTimerAperto.Width);

            int x =
                formLeggi.Left +
                formLeggi.Width +
                MargineTraForm;

            if (x + larghezzaColonna >
                areaSchermo.Right)
                x = areaSchermo.Right -
                    larghezzaColonna;

            formTimerAperto.Location =
                new Point(
                    x,
                    formLeggi.Top);

            int yInserisci =
                formTimerAperto.Top +
                formTimerAperto.Height +
                MargineTraForm;

            if (yInserisci + this.Height >
                areaSchermo.Bottom)
                yInserisci = areaSchermo.Bottom -
                             this.Height;

            this.Location =
                new Point(
                    x,
                    yInserisci);
        }

        public void AggiornaComboBoxDaRiferimento(
            LibroBiblico libro,
            int capitolo,
            int? versettoDa,
            int? versettoA)
        {
            if (libro == null)
                return;

            AggiungiCronologia(
                libro,
                capitolo,
                versettoDa,
                versettoA);

            var voceLibro =
                comboBoxLibri.Items
                    .Cast<VoceLibro>()
                    .FirstOrDefault(
                        x =>
                            !x.IsHeader &&
                            x.Libro?.BookNumber ==
                            libro.BookNumber);

            if (voceLibro == null)
                return;

            comboBoxLibri.SelectedItem =
                voceLibro;

            comboBox_capitolo.SelectedItem =
                comboBox_capitolo.Items
                    .Cast<VoceCombo>()
                    .FirstOrDefault(
                        x =>
                            x.Valore ==
                            capitolo);

            if (!versettoDa.HasValue)
                return;

            comboBox_versettoDa.SelectedItem =
                comboBox_versettoDa.Items
                    .Cast<VoceCombo>()
                    .FirstOrDefault(
                        x =>
                            x.Valore ==
                            versettoDa.Value);

            comboBox_versettoFine.SelectedItem =
                comboBox_versettoFine.Items
                    .Cast<VoceCombo>()
                    .FirstOrDefault(
                        x =>
                            x.Valore ==
                            (versettoA ??
                             versettoDa.Value));
        }

        // Form aperti prima della manutenzione, da riaprire alla sua chiusura.
        private sealed class StatoFormAperti
        {
            public bool LeggiAperta;
            public bool InRicerca;
            public string TestoRicerca = "";
            public LibroBiblico Libro;
            public int Capitolo;
            public int? VersettoDa;
            public int? VersettoA;
            public bool MappaAperta;
            public List<Form> FormNascosti = new List<Form>();
        }

        // Chiude Leggi (e con lei la mappa concettuale) ricordando
        // riferimento o ricerca, poi nasconde tutte le altre finestre
        // visibili, FormINSERISCI compresa (non si puo' chiudere: e' la
        // finestra principale, chiuderla termina il programma) e il timer
        // (chiuderlo azzererebbe un conto alla rovescia in corso).
        private StatoFormAperti ChiudiFormPerManutenzione()
        {
            var stato = new StatoFormAperti();

            if (formLeggiAperta != null &&
                !formLeggiAperta.IsDisposed)
            {
                var leggi = formLeggiAperta;

                stato.InRicerca = leggi.InModalitaRicerca;
                stato.TestoRicerca = leggi.TestoRicercaCorrente ?? "";
                stato.Libro = leggi.LibroCorrente;
                stato.Capitolo = leggi.CapitoloCorrente;
                stato.VersettoDa = leggi.VersettoDaCorrente;
                stato.VersettoA = leggi.VersettoACorrente;
                stato.MappaAperta = leggi.MappaConcettualeAperta;

                leggi.Close();

                // Se la chiusura e' stata annullata la finestra resta
                // aperta e non va riaperta.
                stato.LeggiAperta = leggi.IsDisposed;

                if (leggi.IsDisposed)
                    formLeggiAperta = null;
            }

            foreach (Form f in
                     Application.OpenForms.Cast<Form>().ToList())
            {
                if (f.IsDisposed || !f.Visible)
                    continue;

                f.Hide();
                stato.FormNascosti.Add(f);
            }

            return stato;
        }

        private void RiapriFormDopoManutenzione(
            StatoFormAperti stato)
        {
            if (stato == null)
                return;

            try
            {
                foreach (Form f in stato.FormNascosti)
                {
                    if (!f.IsDisposed)
                        f.Show();
                }

                if (!stato.LeggiAperta)
                    return;

                if (stato.InRicerca)
                {
                    ApriFormLeggiRicerca(
                        stato.TestoRicerca);
                }
                else if (stato.Libro != null)
                {
                    ApriFormLeggi(
                        stato.Libro,
                        stato.Capitolo,
                        stato.VersettoDa,
                        stato.VersettoA);
                }
                else
                {
                    return;
                }

                if (stato.MappaAperta)
                    formLeggiAperta?.RiapriMappaConcettuale();
            }
            catch (Exception ex)
            {
                MessaggioBibbia.Mostra(
                    "Non è stato possibile riaprire le finestre " +
                    "che erano aperte prima della manutenzione.\r\n\r\n" +
                    ex.Message,
                    "Manutenzione",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void button_manutenzione_Click(
            object sender,
            EventArgs e)
        {

            SqliteConnection.ClearAllPools();

            // Le altre finestre si chiudono mentre c'e' la manutenzione e
            // si riaprono, con gli stessi riferimenti, quando finisce.
            var formAperti =
                ChiudiFormPerManutenzione();

            bool manutenzioneTerminata =
                false;

            try
            {
                bool riapriManutenzione;

                do
                {
                    riapriManutenzione = false;

                    using (var formManutenzione =
                        new FormMANUTENZIONE())
                    {
                        formManutenzione.PrimaDiEliminare = () =>
                        {
                            if (formLeggiAperta != null &&
                                !formLeggiAperta.IsDisposed)
                            {
                                formLeggiAperta.Close();
                                formLeggiAperta = null;
                            }

                            SqliteConnection.ClearAllPools();
                        };

                        // Senza proprietario: FormINSERISCI e' nascosta e
                        // MANUTENZIONE deve comparire nella barra delle
                        // applicazioni.
                        formManutenzione.ShowDialog();

                        riapriManutenzione =
                            formManutenzione.InfoRichiesta;
                    }

                    // Il pulsante "i" di MANUTENZIONE l'ha chiusa: si apre
                    // About e, quando si chiude, MANUTENZIONE si riapre.
                    if (riapriManutenzione)
                    {
                        using var formInfo = new FormINFO();

                        formInfo.ShowDialog();

                        // Dal link del nome il programma e' stato rilanciato
                        // sul riferimento biblico: questa copia si chiude.
                        if (formInfo.RiavvioRichiesto)
                        {
                            Application.Exit();
                            return;
                        }
                    }
                }
                while (riapriManutenzione);

                manutenzioneTerminata = true;
            }
            catch (Exception ex)
            {
                MessaggioBibbia.Mostra(
                    "Si è verificato un errore durante la manutenzione.\r\n\r\n" +
                    ex.Message,
                    "Errore manutenzione",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {

                SqliteConnection.ClearAllPools();
            }

            if (!manutenzioneTerminata)
            {
                Application.Exit();
                return;
            }

            string percorsoCei =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "CEI_08.SQLite3");

            if (!File.Exists(
                percorsoCei))
            {
                MessaggioBibbia.Mostra(
                    "Il database della Bibbia CEI è stato eliminato.\r\n\r\n" +
                    "Per continuare a utilizzare il programma è necessario " +
                    "ripristinare il file:\r\n" +
                    "CEI_08.SQLite3\r\n\r\n" +
                    "Il programma verrà chiuso.",
                    "Database CEI mancante",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                Application.Exit();

                return;
            }

            try
            {
                PreparaDbNote();

                AggiornaLibriConNota();

                CaricaCronologiaDaFile();

                AggiornaColoriNote();
            }
            catch (SqliteException ex)
            {
                MessaggioBibbia.Mostra(
                    "Non è stato possibile ripristinare i database " +
                    "dell'applicazione dopo la manutenzione.\r\n\r\n" +
                    "Errore SQLite:\r\n" +
                    ex.Message,
                    "Errore dopo la manutenzione",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Application.Exit();

                return;
            }
            catch (Exception ex)
            {
                MessaggioBibbia.Mostra(
                    "Si è verificato un errore dopo la manutenzione.\r\n\r\n" +
                    ex.Message,
                    "Errore dopo la manutenzione",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Application.Exit();

                return;
            }

            RiapriFormDopoManutenzione(
                formAperti);

            Activate();
        }

        private void button_random_Click(
            object sender,
            EventArgs e)
        {
            if (LibriCEI.Elenco.Count == 0)
            {
                LibriCEI.Carica(
                    connectionStringBibbia);
            }

            var versettiSenzaNota =
                TrovaVersettiSenzaNota();

            if (versettiSenzaNota.Count > 0)
            {
                var scelto =
                    versettiSenzaNota[
                        Random.Shared.Next(
                            versettiSenzaNota.Count)];

                SelezionaVersettoCasuale(
                    scelto.BookNumber,
                    scelto.Capitolo,
                    scelto.Verso);

                return;
            }

            SelezionaCasualeSenzaPriorita();
        }

        private List<(
            int BookNumber,
            int Capitolo,
            int Verso)>
            TrovaVersettiSenzaNota()
        {
            var coperti =
                new HashSet<(
                    int BookNumber,
                    int Capitolo,
                    int Verso)>();

            using (var conn =
                new SqliteConnection(
                    connectionStringNote))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    "SELECT Libro, Capitolo, " +
                    "VersettoDa, VersettoA " +
                    "FROM Note;";

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    string nomeLibro =
                        reader.GetString(0);

                    int capitolo =
                        reader.GetInt32(1);

                    int da =
                        reader.GetInt32(2);

                    int a =
                        reader.GetInt32(3);

                    var libroNota =
                        LibriCEI.Elenco.FirstOrDefault(
                            x =>
                                x.Nome ==
                                nomeLibro);

                    if (libroNota == null)
                        continue;

                    for (int v = da;
                         v <= a;
                         v++)
                    {
                        coperti.Add(
                            (
                                libroNota.BookNumber,
                                capitolo,
                                v));
                    }
                }
            }

            var senzaNota =
                new List<(
                    int,
                    int,
                    int)>();

            using (var conn =
                new SqliteConnection(
                    connectionStringBibbia))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    "SELECT book_number, chapter, verse " +
                    "FROM verses;";

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    int bn =
                        reader.GetInt32(0);

                    int cap =
                        reader.GetInt32(1);

                    int v =
                        reader.GetInt32(2);

                    if (!coperti.Contains(
                        (bn, cap, v)))
                    {
                        senzaNota.Add(
                            (bn, cap, v));
                    }
                }
            }

            return senzaNota;
        }

        private void SelezionaVersettoCasuale(
            int bookNumber,
            int capitolo,
            int verso)
        {
            var voceLibro =
                comboBoxLibri.Items
                    .Cast<VoceLibro>()
                    .FirstOrDefault(
                        x =>
                            !x.IsHeader &&
                            x.Libro?.BookNumber ==
                            bookNumber);

            if (voceLibro == null)
                return;

            comboBoxLibri.SelectedItem =
                voceLibro;

            comboBox_capitolo.SelectedItem =
                comboBox_capitolo.Items
                    .Cast<VoceCombo>()
                    .FirstOrDefault(
                        x =>
                            x.Valore ==
                            capitolo);

            comboBox_versettoDa.SelectedItem =
                comboBox_versettoDa.Items
                    .Cast<VoceCombo>()
                    .FirstOrDefault(
                        x =>
                            x.Valore ==
                            verso);

            comboBox_versettoFine.SelectedItem =
                comboBox_versettoFine.Items
                    .Cast<VoceCombo>()
                    .FirstOrDefault(
                        x =>
                            x.Valore ==
                            verso);

            button_leggi.Focus();
        }

        private void SelezionaCasualeSenzaPriorita()
        {
            var libri =
                new List<LibroBiblico>();

            using (var conn =
                new SqliteConnection(
                    connectionStringBibbia))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    "SELECT b.book_number " +
                    "FROM books b " +
                    "WHERE EXISTS " +
                    "(SELECT 1 FROM verses v " +
                    "WHERE v.book_number = b.book_number) " +
                    "ORDER BY RANDOM();";

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    var libro =
                        LibriCEI.Elenco.FirstOrDefault(
                            x =>
                                x.BookNumber ==
                                reader.GetInt32(0));

                    if (libro != null)
                        libri.Add(libro);
                }
            }

            if (libri.Count == 0)
                return;

            var libroCasuale =
                libri[
                    Random.Shared.Next(
                        libri.Count)];

            var voceLibro =
                comboBoxLibri.Items
                    .Cast<VoceLibro>()
                    .FirstOrDefault(
                        x =>
                            !x.IsHeader &&
                            x.Libro?.BookNumber ==
                            libroCasuale.BookNumber);

            if (voceLibro == null)
                return;

            comboBoxLibri.SelectedItem =
                voceLibro;

            var capitoli =
                new List<int>();

            using (var conn =
                new SqliteConnection(
                    connectionStringBibbia))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    "SELECT DISTINCT chapter " +
                    "FROM verses " +
                    "WHERE book_number=@bn " +
                    "ORDER BY chapter;";

                cmd.Parameters.AddWithValue(
                    "@bn",
                    libroCasuale.BookNumber);

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    capitoli.Add(
                        reader.GetInt32(0));
                }
            }

            if (capitoli.Count == 0)
                return;

            int capitoloCasuale =
                capitoli[
                    Random.Shared.Next(
                        capitoli.Count)];

            comboBox_capitolo.SelectedItem =
                comboBox_capitolo.Items
                    .Cast<VoceCombo>()
                    .FirstOrDefault(
                        x =>
                            x.Valore ==
                            capitoloCasuale);

            var versetti =
                new List<int>();

            using (var conn =
                new SqliteConnection(
                    connectionStringBibbia))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    "SELECT verse FROM verses " +
                    "WHERE book_number=@bn " +
                    "AND chapter=@cap " +
                    "ORDER BY verse;";

                cmd.Parameters.AddWithValue(
                    "@bn",
                    libroCasuale.BookNumber);

                cmd.Parameters.AddWithValue(
                    "@cap",
                    capitoloCasuale);

                using var reader =
                    cmd.ExecuteReader();

                while (reader.Read())
                {
                    versetti.Add(
                        reader.GetInt32(0));
                }
            }

            if (versetti.Count == 0)
                return;

            int indiceDa =
                Random.Shared.Next(
                    versetti.Count);

            int versettoDaCasuale =
                versetti[indiceDa];

            int versettoAFinale =
                versetti[
                    Random.Shared.Next(
                        indiceDa,
                        versetti.Count)];

            comboBox_versettoDa.SelectedItem =
                comboBox_versettoDa.Items
                    .Cast<VoceCombo>()
                    .FirstOrDefault(
                        x =>
                            x.Valore ==
                            versettoDaCasuale);

            comboBox_versettoFine.SelectedItem =
                comboBox_versettoFine.Items
                    .Cast<VoceCombo>()
                    .FirstOrDefault(
                        x =>
                            x.Valore ==
                            versettoAFinale);

            button_leggi.Focus();
        }

        private void button_cerca_Click(
            object sender,
            EventArgs e)
        {
            string testoCercato =
                textBox_nota.Text;

            if (string.IsNullOrWhiteSpace(
                testoCercato))
            {
                SystemSounds.Exclamation.Play();

                MessaggioBibbia.Mostra(
                    "Scrivi il testo da cercare",
                    "Occhio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                textBox_nota.Focus();

                return;
            }

            ApriFormLeggiRicerca(
                testoCercato.Trim());
        }

        private void button_inserisci_Click(
            object sender,
            EventArgs e)
        {
            if (!ProvaLeggiRiferimento(
                out var libro,
                out var capitolo,
                out var versettoDa,
                out var versettoA))
            {
                return;
            }

            if (versettoDa == null)
            {
                SystemSounds.Exclamation.Play();

                MessaggioBibbia.Mostra(
                    "Per commentare indica il versetto",
                    "Occhio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                comboBox_versettoDa.Focus();

                return;
            }

            if (string.IsNullOrWhiteSpace(
                textBox_nota.Text))
            {
                SystemSounds.Exclamation.Play();

                MessaggioBibbia.Mostra(
                    "Scrivi il commento",
                    "Occhio",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                textBox_nota.Focus();

                return;
            }

            using (var conn =
                new SqliteConnection(
                    connectionStringNote))
            {
                conn.Open();

                var cmd =
                    conn.CreateCommand();

                cmd.CommandText =
                    @"INSERT INTO Note
                    (Libro, Capitolo, VersettoDa, VersettoA, TestoNota)
                    VALUES
                    (@libro, @capitolo, @da, @a, @nota);";

                cmd.Parameters.AddWithValue(
                    "@libro",
                    libro.Nome);

                cmd.Parameters.AddWithValue(
                    "@capitolo",
                    capitolo);

                cmd.Parameters.AddWithValue(
                    "@da",
                    versettoDa.Value);

                cmd.Parameters.AddWithValue(
                    "@a",
                    versettoA ??
                    versettoDa.Value);

                cmd.Parameters.AddWithValue(
                    "@nota",
                    textBox_nota.Text);

                cmd.ExecuteNonQuery();

                ManutenzioneIndiceNote(
                    conn);
            }

            MessaggioBibbia.Mostra(
                $"Commento salvato su {libro.Nome} " +
                $"{capitolo},{versettoDa}" +
                (versettoA.HasValue &&
                 versettoA != versettoDa
                    ? $"-{versettoA}"
                    : ""),
                "Conferma");

            textBox_nota.Clear();

            textBox_nota.Focus();

            AggiornaColoriNote();

            AggiungiCronologia(
                libro,
                capitolo,
                versettoDa,
                versettoA);

            ApriFormLeggi(
                libro,
                capitolo,
                versettoDa,
                versettoA);
        }

        private void button_leggi_Click(
            object sender,
            EventArgs e)
        {
            if (!ProvaLeggiRiferimento(
                out var libro,
                out var capitolo,
                out var versettoDa,
                out var versettoA))
            {
                return;
            }

            AggiungiCronologia(
                libro,
                capitolo,
                versettoDa,
                versettoA);

            ApriFormLeggi(
                libro,
                capitolo,
                versettoDa,
                versettoA);
        }

        private void comboBoxLibri_DrawItem(
            object sender,
            DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            var voce =
                (VoceLibro)
                comboBoxLibri.Items[
                    e.Index];

            if (voce.IsHeader)
            {
                using var sfondo =
                    new SolidBrush(
                        TemaBibbia.FondoBottone);

                e.Graphics.FillRectangle(
                    sfondo,
                    e.Bounds);

                using var fontGrassetto =
                    new Font(
                        comboBoxLibri.Font,
                        FontStyle.Bold);

                TextRenderer.DrawText(
                    e.Graphics,
                    voce.Testo,
                    fontGrassetto,
                    e.Bounds,
                    TemaBibbia.Oro,
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.Left);
            }
            else
            {
                TemaBibbia.DisegnaSfondoVoce(e);

                bool haNota =
                    voce.Libro != null &&
                    libriConNota.Contains(
                        voce.Libro.Nome);

                Color coloreTesto;

                if ((e.State &
                    DrawItemState.Selected) ==
                    DrawItemState.Selected)
                {
                    coloreTesto =
                        TemaBibbia.TestoSuOro;
                }
                else
                {
                    coloreTesto =
                        haNota
                            ? TemaBibbia.Riferimento
                            : TemaBibbia.Avorio;
                }

                var areaTesto =
                    new Rectangle(
                        e.Bounds.X + 24,
                        e.Bounds.Y,
                        e.Bounds.Width - 24,
                        e.Bounds.Height);

                TextRenderer.DrawText(
                    e.Graphics,
                    voce.Testo,
                    comboBoxLibri.Font,
                    areaTesto,
                    coloreTesto,
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.Left);
            }

            e.DrawFocusRectangle();
        }

        private void comboBox_capitolo_DrawItem(
            object sender,
            DrawItemEventArgs e)
        {
            if (e.Index < 0)
                return;

            var voce =
                (VoceCombo)
                comboBox_capitolo.Items[
                    e.Index];

            TemaBibbia.DisegnaSfondoVoce(e);

            bool haNota =
                voce.Valore.HasValue &&
                capitoliConNota.Contains(
                    voce.Valore.Value);

            Color coloreTesto;

            if ((e.State &
                DrawItemState.Selected) ==
                DrawItemState.Selected)
            {
                coloreTesto =
                    TemaBibbia.TestoSuOro;
            }
            else
            {
                coloreTesto =
                    haNota
                        ? TemaBibbia.Riferimento
                        : comboBox_capitolo.ForeColor;
            }

            TextRenderer.DrawText(
                e.Graphics,
                voce.Testo,
                comboBox_capitolo.Font,
                e.Bounds,
                coloreTesto,
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.Left);

            e.DrawFocusRectangle();
        }

        private (
            bool eliminaCommenti,
            bool eliminaCronologia)
            ChiediCosaEliminare()
        {
            using (var dlg =
                new Form())
            {
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
                dlg.Text =
                    "Cosa vuoi eliminare?";

                dlg.FormBorderStyle =
                    FormBorderStyle.FixedDialog;

                dlg.StartPosition =
                    FormStartPosition.CenterParent;

                dlg.MinimizeBox =
                    false;

                dlg.MaximizeBox =
                    false;

                dlg.ShowIcon =
                    false;

                dlg.AutoScaleMode =
                    AutoScaleMode.Font;

                dlg.Font =
                    this.Font;

                dlg.AutoSize =
                    true;

                dlg.AutoSizeMode =
                    AutoSizeMode.GrowAndShrink;

                var layoutPrincipale =
                    new TableLayoutPanel
                    {
                        AutoSize = true,

                        AutoSizeMode =
                            AutoSizeMode.GrowAndShrink,

                        ColumnCount = 1,

                        Padding =
                            new Padding(20),

                        Dock =
                            DockStyle.Fill
                    };

                var chkCommenti =
                    new CheckBox
                    {
                        Text =
                            "Commenti (commenti.db)",

                        AutoSize =
                            true,

                        Checked =
                            true,

                        Margin =
                            new Padding(
                                0, 0, 0, 10)
                    };

                var chkCronologia =
                    new CheckBox
                    {
                        Text =
                            "Cronologia (crono.txt)",

                        AutoSize =
                            true,

                        Checked =
                            false,

                        Margin =
                            new Padding(
                                0, 0, 0, 15)
                    };

                var rigaPulsanti =
                    new FlowLayoutPanel
                    {
                        AutoSize =
                            true,

                        AutoSizeMode =
                            AutoSizeMode.GrowAndShrink,

                        FlowDirection =
                            FlowDirection.LeftToRight,

                        Anchor =
                            AnchorStyles.None
                    };

                var btnOk =
                    new Button
                    {
                        Text =
                            "OK",

                        DialogResult =
                            DialogResult.OK,

                        AutoSize =
                            true,

                        AutoSizeMode =
                            AutoSizeMode.GrowAndShrink,

                        Padding =
                            new Padding(
                                12, 4, 12, 4),

                        Margin =
                            new Padding(
                                0, 0, 10, 0)
                    };

                var btnAnnulla =
                    new Button
                    {
                        Text =
                            "Annulla",

                        DialogResult =
                            DialogResult.Cancel,

                        AutoSize =
                            true,

                        AutoSizeMode =
                            AutoSizeMode.GrowAndShrink,

                        Padding =
                            new Padding(
                                12, 4, 12, 4)
                    };

                rigaPulsanti.Controls.Add(
                    btnOk);

                rigaPulsanti.Controls.Add(
                    btnAnnulla);

                layoutPrincipale.Controls.Add(
                    chkCommenti);

                layoutPrincipale.Controls.Add(
                    chkCronologia);

                layoutPrincipale.Controls.Add(
                    rigaPulsanti);

                rigaPulsanti.Anchor =
                    AnchorStyles.None;

                dlg.Controls.Add(
                    layoutPrincipale);

                dlg.AcceptButton =
                    btnOk;

                dlg.CancelButton =
                    btnAnnulla;

                DialogResult risultato =
                    dlg.ShowDialog(this);

                if (risultato !=
                    DialogResult.OK)
                {
                    return (
                        false,
                        false);
                }

                return (
                    chkCommenti.Checked,
                    chkCronologia.Checked);
            }
        }

        private void button_elimina_Click(
            object sender,
            EventArgs e)
        {
            var (
                eliminaCommenti,
                eliminaCronologia
            ) =
                ChiediCosaEliminare();

            if (!eliminaCommenti &&
                !eliminaCronologia)
            {
                return;
            }

            DialogResult conferma =
                MessaggioBibbia.Mostra(
                    "Procedere all'eliminazione dei dati selezionati?",
                    "Richiesta Conferma",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (conferma !=
                DialogResult.Yes)
            {
                return;
            }

            var errori =
                new List<string>();

            if (eliminaCommenti)
            {
                try
                {
                    using (var conn =
                        new SqliteConnection(
                            connectionStringNote))
                    {
                        conn.Open();

                        using (var cmdCheck =
                            conn.CreateCommand())
                        {
                            cmdCheck.CommandText =
                                "PRAGMA integrity_check;";

                            string esito =
                                (string)
                                cmdCheck.ExecuteScalar();

                            if (!string.Equals(
                                esito,
                                "ok",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                throw new SqliteException(
                                    $"Database corrotto " +
                                    $"(integrity_check: {esito})",
                                    11);
                            }
                        }

                        using (var cmd =
                            conn.CreateCommand())
                        {
                            cmd.CommandText =
                                "DELETE FROM Note;";

                            cmd.ExecuteNonQuery();
                        }

                        ManutenzioneIndiceNote(
                            conn,
                            rebuild: true);
                    }

                    libriConNota.Clear();

                    tagEsistenti = new List<string>();

                    capitoliConNota.Clear();

                    versettiConNota.Clear();

                    AggiornaColoreTestoLibro();

                    comboBoxLibri.Invalidate();

                    comboBox_capitolo.Invalidate();

                    comboBox_versettoDa.Invalidate();

                    comboBox_versettoFine.Invalidate();
                }
                catch (SqliteException ex)
                {
                    errori.Add(
                        "Commenti: il database \"commenti.db\" " +
                        "risulta corrotto " +
                        $"({ex.Message}). Chiudi il programma, " +
                        "fai una copia di backup del file, poi " +
                        "eliminalo per farlo ricreare vuoto " +
                        "al prossimo avvio (i commenti già presenti " +
                        "andranno persi).");
                }
            }

            if (eliminaCronologia)
            {
                try
                {
                    if (File.Exists(
                        PercorsoCronologia))
                    {
                        File.Delete(
                            PercorsoCronologia);
                    }

                    cronologia.Clear();

                    indiceCronologia =
                        -1;

                    AggiornaPulsantiCronologia();
                }
                catch (IOException ex)
                {
                    errori.Add(
                        $"Cronologia: {ex.Message}");
                }
            }

            if (errori.Count > 0)
            {
                MessaggioBibbia.Mostra(
                    string.Join(
                        Environment.NewLine +
                        Environment.NewLine,
                        errori),
                    "Eliminazione non riuscita",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else
            {
                MessaggioBibbia.Mostra(
                    "Dati eliminati",
                    "Dati eliminati");
            }
        }
    }
}
