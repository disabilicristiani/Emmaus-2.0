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

#nullable disable

using emmaus_v2;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Input
{
    // Guida in linea: un unico file HTML (Guida.html) incorporato nell'eseguibile
    // come risorsa, mostrato in una finestra con WebView2. Se il runtime di
    // WebView2 non c'e', la stessa guida si apre nel browser predefinito.
    //
    // Uso (da qualsiasi form):   FormGUIDA.Mostra(this);            // pagina iniziale
    //                            FormGUIDA.Mostra(this, "c9");      // capitolo 9
    //                            FormGUIDA.Mostra(this, "s6-4");    // sezione 6.4
    // F1: ogni finestra, finestra di dialogo e messaggio apre la guida
    // sull'argomento pertinente (vedi MessaggioBibbia.AncoraGuida).
    // Le ancore sono "c<capitolo>" e "s<capitolo>-<sezione>".
    public sealed class FormGUIDA : Form
    {
        // Nome logico della risorsa incorporata (vedi LogicalName nel .csproj).
        private const string NomeRisorsa = "Emmaus.Guida.html";

        private static FormGUIDA istanza;

        // Stessi id dei temi di TemaBibbia, nello stesso ordine di NomiTemi.
        private static readonly string[] IdTemi = { "notte", "pergamena", "porpora", "riposo", "notturno" };

        private static string IdTemaCorrente
        {
            get
            {
                int i = TemaBibbia.IndiceTema;
                return i >= 0 && i < IdTemi.Length ? IdTemi[i] : IdTemi[0];
            }
        }

        private readonly WebView2 browser = new WebView2 { Dock = DockStyle.Fill };
        private readonly string fileGuida;
        private string ancoraIniziale;
        private string ancoraPendente;
        private bool caricata;

        public static void Mostra(Form proprietario, string ancora = null)
        {
            // La guida appartiene alla finestra "radice": se la apri da un
            // messaggio o da una finestra di dialogo, non si chiude con essa.
            while (proprietario?.Owner != null)
                proprietario = proprietario.Owner;

            string file;

            try
            {
                file = PreparaFile();
            }
            catch (Exception ex)
            {
                MessageBox.Show(proprietario, "Non riesco ad aprire la guida:\r\n\r\n" + ex.Message,
                    "Guida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Gia' aperta: la porta davanti e la sposta sull'argomento richiesto.
            if (istanza != null && !istanza.IsDisposed)
            {
                if (istanza.WindowState == FormWindowState.Minimized)
                    istanza.WindowState = FormWindowState.Normal;

                // Durante una finestra modale la guida verrebbe disabilitata.
                istanza.Enabled = true;

                istanza.VaiA(ancora);
                istanza.Activate();
                return;
            }

            if (!RuntimeWebView2Presente())
            {
                ApriNelBrowserPredefinito(file, ancora);
                return;
            }

            istanza = new FormGUIDA(file, ancora);
            istanza.FormClosed += (s, e) => istanza = null;
            istanza.Show(proprietario);
        }

        private FormGUIDA(string file, string ancora)
        {
            fileGuida = file;
            ancoraIniziale = ancora;

            Text = "Emmaus 2.0 - Guida";
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? Icon;
            MinimumSize = new Size(560, 420);

            // Dimensioni e posizione in proporzione allo schermo.
            Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
            int larghezza = Math.Min(1180, (int)(area.Width * 0.85));
            int altezza = Math.Min(820, (int)(area.Height * 0.90));

            StartPosition = FormStartPosition.Manual;
            Size = new Size(larghezza, altezza);
            Location = new Point(area.Left + (area.Width - larghezza) / 2, area.Top + (area.Height - altezza) / 2);

            try
            {
                TemaBibbia.Applica(this);
            }
            catch
            {
                BackColor = TemaBibbia.FondoCampo;
            }

            // Niente lampo bianco mentre la pagina si carica.
            browser.DefaultBackgroundColor = TemaBibbia.Sfondo;

            Controls.Add(browser);

            // Se il tema cambia mentre la guida e' aperta, la guida lo segue.
            EventHandler suTema = (s, e) =>
            {
                browser.DefaultBackgroundColor = TemaBibbia.Sfondo;

                if (caricata && browser.CoreWebView2 != null)
                    browser.CoreWebView2.ExecuteScriptAsync("window.emmausTema && emmausTema(" + JsonSerializer.Serialize(IdTemaCorrente) + ");");
            };
            TemaBibbia.TemaCambiato += suTema;
            FormClosed += (s, e) => TemaBibbia.TemaCambiato -= suTema;

            Load += async (s, e) => await InizializzaAsync();
        }

        private async Task InizializzaAsync()
        {
            try
            {
                // Cartella dei dati di WebView2 in un posto sempre scrivibile
                // (quella predefinita sta accanto all'eseguibile).
                string datiBrowser = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Emmaus2", "WebView2");

                CoreWebView2Environment ambiente = await CoreWebView2Environment.CreateAsync(null, datiBrowser);
                await browser.EnsureCoreWebView2Async(ambiente);

                CoreWebView2 web = browser.CoreWebView2;
                web.Settings.AreDevToolsEnabled = false;
                web.Settings.IsStatusBarEnabled = false;
                web.Settings.IsGeneralAutofillEnabled = false;
                web.Settings.IsPasswordAutosaveEnabled = false;

                // I collegamenti esterni si aprono nel browser predefinito,
                // mai dentro la guida.
                web.NewWindowRequested += (s, e) =>
                {
                    e.Handled = true;
                    ApriEsterno(e.Uri);
                };

                web.NavigationStarting += (s, e) =>
                {
                    if (!e.Uri.StartsWith("file:", StringComparison.OrdinalIgnoreCase) &&
                        !e.Uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase) &&
                        !e.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Cancel = true;
                        ApriEsterno(e.Uri);
                    }
                };

                // Il tema scelto nel menu della guida diventa il tema del programma.
                web.WebMessageReceived += (s, e) =>
                {
                    string messaggio = e.TryGetWebMessageAsString();

                    if (messaggio != null && messaggio.StartsWith("tema:", StringComparison.Ordinal))
                    {
                        int indice = Array.IndexOf(IdTemi, messaggio.Substring(5));

                        if (indice >= 0)
                            TemaBibbia.ImpostaTema(indice);
                    }
                };

                web.NavigationCompleted += (s, e) =>
                {
                    caricata = true;

                    if (!string.IsNullOrEmpty(ancoraPendente))
                    {
                        string a = ancoraPendente;
                        ancoraPendente = null;
                        VaiA(a);
                    }
                };

                web.Navigate(UriGuida(fileGuida, ancoraIniziale));
            }
            catch
            {
                // Finestra chiusa mentre WebView2 si avviava: niente da fare.
                if (IsDisposed || Disposing)
                    return;

                // WebView2 non utilizzabile: la guida si apre nel browser.
                ApriNelBrowserPredefinito(fileGuida, ancoraIniziale);
                Close();
            }
        }

        // Porta la guida su un argomento (es. "c9" o "s6-4").
        private void VaiA(string ancora)
        {
            if (string.IsNullOrEmpty(ancora))
                return;

            if (!caricata || browser.CoreWebView2 == null)
            {
                ancoraPendente = ancora;
                return;
            }

            browser.CoreWebView2.ExecuteScriptAsync("location.hash = " + JsonSerializer.Serialize("#" + ancora) + ";");
        }

        private static string UriGuida(string file, string ancora)
        {
            // Il tema del programma viaggia nell'indirizzo (?tema=...).
            string uri = new Uri(file).AbsoluteUri + "?tema=" + IdTemaCorrente;
            return string.IsNullOrEmpty(ancora) ? uri : uri + "#" + Uri.EscapeDataString(ancora);
        }

        // Scrive Guida.html (risorsa incorporata) in una cartella locale. Il nome
        // del file contiene l'impronta del contenuto: una guida aggiornata ha un
        // nome nuovo e quelle vecchie vengono eliminate.
        private static string PreparaFile()
        {
            byte[] dati;

            using (Stream risorsa = Assembly.GetExecutingAssembly().GetManifestResourceStream(NomeRisorsa))
            {
                if (risorsa != null)
                {
                    using var memoria = new MemoryStream();
                    risorsa.CopyTo(memoria);
                    dati = memoria.ToArray();
                }
                else
                {
                    // In alternativa, Risorse\Guida.html accanto all'eseguibile.
                    string accanto = Path.Combine(AppContext.BaseDirectory, "Risorse", "Guida.html");

                    if (!File.Exists(accanto))
                        throw new FileNotFoundException("La guida non e' incorporata nel programma (risorsa " + NomeRisorsa + ").");

                    dati = File.ReadAllBytes(accanto);
                }
            }

            string impronta = Convert.ToHexString(SHA256.HashData(dati)).Substring(0, 12);

            string cartella = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Emmaus2", "Guida");

            Directory.CreateDirectory(cartella);

            string file = Path.Combine(cartella, "Guida-" + impronta + ".html");

            if (!File.Exists(file) || new FileInfo(file).Length != dati.Length)
            {
                string provvisorio = file + ".tmp";
                File.WriteAllBytes(provvisorio, dati);
                File.Move(provvisorio, file, true);

                foreach (string vecchio in Directory.GetFiles(cartella, "Guida-*.html"))
                {
                    if (!string.Equals(vecchio, file, StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(vecchio); } catch { }
                    }
                }
            }

            return file;
        }

        private static bool RuntimeWebView2Presente()
        {
            try
            {
                return !string.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString());
            }
            catch
            {
                return false;
            }
        }

        private static void ApriNelBrowserPredefinito(string file, string ancora)
        {
            try
            {
                Process.Start(new ProcessStartInfo(UriGuida(file, ancora)) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Non riesco ad aprire la guida:\r\n\r\n" + ex.Message,
                    "Guida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void ApriEsterno(string uri)
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out Uri u) &&
                (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeMailto))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(u.AbsoluteUri) { UseShellExecute = true });
                }
                catch
                {
                }
            }
        }
    }
}
