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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using emmaus_v2;

namespace Input
{
    // Scaling dell'interfaccia: dimensione (in pt) del font di base su cui
    // WinForms scala tutte le finestre. I Designer sono disegnati per circa
    // 22,5 pt. Il valore e' salvato in "zoom.txt" (cartella dell'app) e si
    // applica al riavvio. Un nuovo valore va confermato entro 10 secondi
    // dopo il riavvio, altrimenti torna automaticamente il precedente.
    internal static class ZoomApp
    {
        // Valore usato se zoom.txt non esiste.
        public const float Predefinito = 10f;

        // Base di riferimento per i font e le misure fisse nel codice.
        public const float Riferimento = 18f;
        public const float Minimo = 10f;
        public const float Massimo = 30f;

        // Valori proposti nella tendina delle impostazioni.
        public static readonly float[] Valori =
            { 10f, 11f, 12f, 13f, 14f, 15f, 16f, 18f, 20f, 22f, 24f, 26f, 28f, 30f };

        private static string Percorso =>
            Path.Combine(AppContext.BaseDirectory, "zoom.txt");

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern int AddFontResourceEx(
            string nomeFile, uint flag, IntPtr riservato);

        private const uint FR_PRIVATE = 0x10;

        private static Font _fontFamigliaApp;

        // Carica i font della cartella Risorse\Font (Cardo) per questo solo
        // processo, in modo che siano usabili sia da GDI che da GDI+, e
        // restituisce la famiglia da usare per l'interfaccia. Se i file
        // mancano si usa il font di sistema.
        public static FontFamily FamigliaApp()
        {
            if (_fontFamigliaApp != null)
                return _fontFamigliaApp.FontFamily;

            try
            {
                string cartella =
                    Path.Combine(AppContext.BaseDirectory, "Risorse", "Font");

                if (Directory.Exists(cartella))
                {
                    foreach (string file in Directory.GetFiles(cartella, "*.ttf"))
                        AddFontResourceEx(file, FR_PRIVATE, IntPtr.Zero);

                    var prova = new Font("Cardo", 10f);

                    if (string.Equals(
                            prova.Name, "Cardo",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        _fontFamigliaApp = prova;
                        return prova.FontFamily;
                    }

                    prova.Dispose();
                }
            }
            catch
            {
            }

            _fontFamigliaApp = new Font(SystemFonts.DefaultFont.FontFamily, 10f);
            return _fontFamigliaApp.FontFamily;
        }

        // Fattore dei font fissi rispetto alla base di riferimento (18 pt):
        // con zoom 18 vale 1, con zoom 10 vale circa 0,55.
        public static float Fattore =>
            DimensioneBase / Riferimento;

        // Font di base effettivamente in uso (Application.DefaultFont non
        // e' pubblico, quindi lo si tiene qui).
        private static float _dimensioneBase = 0f;
        private static FontFamily _famigliaBase;

        public static float DimensioneBase =>
            _dimensioneBase > 0f
                ? _dimensioneBase
                : SystemFonts.DefaultFont.SizeInPoints;

        public static FontFamily FamigliaBase =>
            _famigliaBase ?? SystemFonts.DefaultFont.FontFamily;

        // Imposta il font di base dell'applicazione (zoom > 0).
        public static void ApplicaFontBase(float zoom)
        {
            if (zoom <= 0f)
                return;

            _famigliaBase = FamigliaApp();
            _dimensioneBase = zoom;

            Application.SetDefaultFont(new Font(_famigliaBase, zoom));
        }

        public static float Leggi()
        {
            try
            {
                if (File.Exists(Percorso) &&
                    float.TryParse(
                        File.ReadAllText(Percorso).Trim().Replace(',', '.'),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float valore))
                {
                    if (valore <= 0f)
                        return Predefinito;

                    return Math.Max(Minimo, Math.Min(Massimo, valore));
                }
            }
            catch
            {
            }

            return Predefinito;
        }

        public static void Salva(float valore)
        {
            try
            {
                File.WriteAllText(
                    Percorso,
                    valore.ToString("0.##", CultureInfo.InvariantCulture));
            }
            catch
            {
            }
        }

        // --- Prova del nuovo scaling con conferma ---------------------------

        private static string PercorsoProva =>
            Path.Combine(AppContext.BaseDirectory, "zoom_prova.txt");

        // True se l'app e' stata appena riavviata con un nuovo scaling
        // ancora da confermare.
        public static bool ProvaInCorso { get; private set; }

        // Da chiamare all'avvio, prima di leggere lo scaling. Se la prova
        // precedente non e' stata confermata (app chiusa o bloccata prima
        // della conferma) si torna subito al valore precedente.
        public static void ControllaProvaPendente()
        {
            try
            {
                if (!File.Exists(PercorsoProva))
                    return;

                string[] righe = File.ReadAllLines(PercorsoProva);
                string precedente = righe.Length > 0 ? righe[0].Trim() : "";
                int tentativi =
                    righe.Length > 1 && int.TryParse(righe[1].Trim(), out int t)
                        ? t : 0;

                if (tentativi >= 1)
                {
                    RipristinaPrecedente(precedente);
                    return;
                }

                File.WriteAllLines(PercorsoProva, new[] { precedente, "1" });
                ProvaInCorso = true;
            }
            catch
            {
            }
        }

        // Salva il nuovo valore tenendo da parte quello attuale: se non
        // viene confermato dopo il riavvio, si torna a quello.
        public static void ProponiNuovoValore(float nuovo)
        {
            try
            {
                float attuale = Leggi();

                File.WriteAllLines(
                    PercorsoProva,
                    new[]
                    {
                        attuale.ToString("0.##", CultureInfo.InvariantCulture),
                        "0"
                    });

                Salva(nuovo);
            }
            catch
            {
            }
        }

        public static void ConfermaProva()
        {
            try
            {
                if (File.Exists(PercorsoProva))
                    File.Delete(PercorsoProva);
            }
            catch
            {
            }

            ProvaInCorso = false;
        }

        public static void AnnullaProva()
        {
            try
            {
                string precedente =
                    File.Exists(PercorsoProva)
                        ? (File.ReadAllLines(PercorsoProva).FirstOrDefault() ?? "")
                        : "";

                RipristinaPrecedente(precedente);
            }
            catch
            {
            }

            ProvaInCorso = false;
        }

        private static void RipristinaPrecedente(string precedente)
        {
            if (float.TryParse(
                    precedente.Trim().Replace(',', '.'),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float valore))
            {
                Salva(valore);
            }

            try
            {
                if (File.Exists(PercorsoProva))
                    File.Delete(PercorsoProva);
            }
            catch
            {
            }
        }

        // Come Application.Restart(), ma senza riportare gli argomenti con
        // cui il programma era stato avviato: dopo un riavvio dal link del
        // form About (--riferimento ...) un cambio di scaling non deve
        // riaprire di nuovo quel riferimento.
        public static void RiavviaSenzaArgomenti()
        {
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo(
                        Application.ExecutablePath)
                    {
                        UseShellExecute = false,
                        WorkingDirectory = AppContext.BaseDirectory
                    });
            }
            catch
            {
                return;
            }

            Application.Exit();
        }

        public static string Descrizione(float valore)
        {
            return valore.ToString("0.##", CultureInfo.CurrentCulture);
        }

        // Rende proporzionale allo zoom il font di controlli che nel
        // Designer hanno una dimensione fissa. Da chiamare una sola volta.
        public static void AdattaFont(params Control[] controlli)
        {
            Adatta(false, controlli);
        }

        // Come AdattaFont, ma porta i controlli anche alla famiglia di
        // font dell'applicazione (Cardo).
        public static void AdattaFontApp(params Control[] controlli)
        {
            Adatta(true, controlli);
        }

        private static void Adatta(bool cambiaFamiglia, Control[] controlli)
        {
            float fattore = Fattore;
            FontFamily famigliaApp = FamigliaBase;

            foreach (Control c in controlli)
            {
                if (c == null)
                    continue;

                c.Font =
                    new Font(
                        cambiaFamiglia ? famigliaApp : c.Font.FontFamily,
                        c.Font.Size * fattore,
                        c.Font.Style);
            }
        }
    }

    internal static class Program
    {
        private static ContestoApplicazione contestoApplicazione;

        // Argomento di avvio: --riferimento "Luca 24, 32" apre quel passo
        // nel form Leggi (usato dal link del nome nel form About).
        private static string? LeggiRiferimentoIniziale(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(
                        args[i], "--riferimento",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            // Font dell'interfaccia: Cardo da Risorse\Font (se presente,
            // altrimenti il font di sistema) alla dimensione dello zoom.
            // Se c'e' un nuovo scaling da confermare lo si controlla prima.
            ZoomApp.ControllaProvaPendente();

            float zoom = ZoomApp.Leggi();

            ZoomApp.ApplicaFontBase(zoom);

            contestoApplicazione =
                new ContestoApplicazione(LeggiRiferimentoIniziale(args));

            Application.Run(contestoApplicazione);
        }

        private sealed class ContestoApplicazione : ApplicationContext
        {
            private FormINSERISCI formInserisci;

            public ContestoApplicazione(string? riferimentoIniziale)
            {
                MostraFormInserisci(riferimentoIniziale);
            }

            private void MostraFormInserisci(string? riferimentoIniziale)
            {
                formInserisci = new FormINSERISCI
                {
                    RiferimentoIniziale = riferimentoIniziale
                };

                formInserisci.FormClosed += FormInserisci_FormClosed;

                if (ZoomApp.ProvaInCorso)
                {
                    formInserisci.Shown += (s, e) =>
                        formInserisci.BeginInvoke(new Action(ConfermaScaling));
                }

                formInserisci.Show();
            }

            private void ConfermaScaling()
            {
                using var dialogo = new DialogoConfermaScaling();

                if (dialogo.ShowDialog(formInserisci) == DialogResult.Yes)
                {
                    ZoomApp.ConfermaProva();
                    return;
                }

                ZoomApp.AnnullaProva();
                ZoomApp.RiavviaSenzaArgomenti();
            }

            private void FormInserisci_FormClosed(
                object sender,
                FormClosedEventArgs e)
            {
                if (ReferenceEquals(sender, formInserisci))
                {
                    ExitThread();
                }
            }
        }
    }

    // Finestra di conferma del nuovo scaling: se non si risponde entro
    // 10 secondi vale "Annulla" e si torna al valore precedente.
    internal sealed class DialogoConfermaScaling : Form
    {
        private const int SecondiTotali = 10;

        private int _rimanenti = SecondiTotali;
        private readonly Label _conto;
        private readonly System.Windows.Forms.Timer _timer;

        public DialogoConfermaScaling()
        {
            Text = "Scaling";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ControlBox = false;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            int larghezzaMassima =
                Math.Max(
                    300,
                    (int)((Screen.PrimaryScreen?.WorkingArea.Width ?? 1280) * 0.6));

            var testo = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(larghezzaMassima, 0),
                Text = "Il nuovo scaling e' stato applicato. Vuoi mantenerlo?",
                Margin = new Padding(0, 0, 0, 12)
            };

            _conto = new Label
            {
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };

            var mantieni = new Button
            {
                Text = "Mantieni",
                DialogResult = DialogResult.Yes,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(14, 4, 14, 4)
            };

            var annulla = new Button
            {
                Text = "Annulla",
                DialogResult = DialogResult.No,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(14, 4, 14, 4)
            };

            var riga = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0)
            };
            riga.Controls.Add(mantieni);
            riga.Controls.Add(annulla);

            var layout = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(20)
            };
            layout.Controls.Add(testo);
            layout.Controls.Add(_conto);
            layout.Controls.Add(riga);

            Controls.Add(layout);

            AcceptButton = mantieni;
            CancelButton = annulla;

            TemaBibbia.Applica(this);

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) =>
            {
                _rimanenti--;
                AggiornaConto();

                if (_rimanenti <= 0)
                {
                    _timer.Stop();
                    DialogResult = DialogResult.No;
                    Close();
                }
            };

            AggiornaConto();
        }

        private void AggiornaConto()
        {
            _conto.Text =
                $"Annullamento automatico tra {_rimanenti} secondi";
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _timer?.Dispose();

            base.Dispose(disposing);
        }
    }
}
