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

using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Input
{
    internal static class GestoreBackup
    {
        public const int GiorniMassimi = 12;

        private const string FormatoTimestamp = "yyyyMMdd_HHmmss";

        private static readonly Regex NomeFileBackup = new Regex(
            @"^(?<nome>.+)_backup_(?<ts>\d{8}_\d{6})(?<ext>\.[^.]*)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string CartellaBackup =>
            Path.Combine(AppContext.BaseDirectory, "BackupDB");

        public sealed class ElementoBackup
        {
            public string NomeBase { get; init; }
            public string Percorso { get; init; }
            public string Descrizione { get; init; }
            public bool IsDatabase { get; init; }
        }

        public static readonly ElementoBackup Cronologia = new ElementoBackup
        {
            NomeBase = "crono",
            Percorso = "crono.txt",
            Descrizione = "Cronologia",
            IsDatabase = false
        };

        public static readonly ElementoBackup Indicizzazione = new ElementoBackup
        {
            NomeBase = "versetti_fts",
            Percorso = Path.Combine(AppContext.BaseDirectory, "versetti_fts.db"),
            Descrizione = "DB Indicizzazione",
            IsDatabase = true
        };

        public static readonly ElementoBackup Commenti = new ElementoBackup
        {
            NomeBase = "commenti",
            Percorso = Path.Combine(AppContext.BaseDirectory, "commenti.db"),
            Descrizione = "DB Commenti",
            IsDatabase = true
        };

        public static readonly ElementoBackup Bibbia = new ElementoBackup
        {
            NomeBase = "CEI_08",
            Percorso = Path.Combine(AppContext.BaseDirectory, "CEI_08.SQLite3"),
            Descrizione = "DB Bibbia CEI 08",
            IsDatabase = true
        };

        // Tutti gli elementi riconosciuti nella cartella BackupDB, compresi
        // quelli dei vecchi punti di ripristino (Bibbia e Indicizzazione),
        // cosi' restano visibili ed eliminabili.
        public static readonly IReadOnlyList<ElementoBackup> Elementi =
            new[] { Cronologia, Indicizzazione, Commenti, Bibbia };

        // Gli unici elementi che il programma salva: dati dell'utente.
        // Il testo CEI (CEI_08.SQLite3 e l'indice versetti_fts.db che ne
        // deriva) non viene mai copiato.
        public static readonly IReadOnlyList<ElementoBackup> ElementiSalvabili =
            new[] { Cronologia, Commenti };

        public sealed class Esito
        {
            public List<string> Riusciti { get; } = new List<string>();
            public List<string> Errori { get; } = new List<string>();
        }

        public sealed class PuntoRipristino
        {
            public string Timestamp { get; init; }
            public DateTime Data { get; init; }

            public Dictionary<string, string> File { get; } =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public bool Contiene(ElementoBackup e) => File.ContainsKey(e.NomeBase);

            public override string ToString() =>
                Data.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);
        }

        private sealed class FileTrovato
        {
            public ElementoBackup Elemento;
            public string Timestamp;
            public DateTime Data;
            public string Percorso;
        }

        private static List<FileTrovato> ElencaFile()
        {
            var risultato = new List<FileTrovato>();

            if (!Directory.Exists(CartellaBackup))
                return risultato;

            foreach (string percorso in Directory.EnumerateFiles(CartellaBackup))
            {
                var m = NomeFileBackup.Match(Path.GetFileName(percorso));
                if (!m.Success)
                    continue;

                var elemento = Elementi.FirstOrDefault(e =>
                    string.Equals(e.NomeBase, m.Groups["nome"].Value,
                        StringComparison.OrdinalIgnoreCase));
                if (elemento == null)
                    continue;

                string ts = m.Groups["ts"].Value;
                if (!DateTime.TryParseExact(ts, FormatoTimestamp,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data))
                    continue;

                risultato.Add(new FileTrovato
                {
                    Elemento = elemento,
                    Timestamp = ts,
                    Data = data,
                    Percorso = percorso
                });
            }

            return risultato;
        }

        public static List<PuntoRipristino> ElencaPuntiRipristino()
        {
            return ElencaFile()
                .GroupBy(f => f.Timestamp)
                .Select(g =>
                {
                    var punto = new PuntoRipristino
                    {
                        Timestamp = g.Key,
                        Data = g.First().Data
                    };
                    foreach (var f in g)
                        punto.File[f.Elemento.NomeBase] = f.Percorso;
                    return punto;
                })
                .OrderByDescending(p => p.Data)
                .ToList();
        }

        public static Esito EseguiBackup(
            IEnumerable<ElementoBackup> elementi,
            bool segnalaMancanti)
        {
            var esito = new Esito();

            try
            {
                Directory.CreateDirectory(CartellaBackup);
            }
            catch (Exception ex)
            {
                esito.Errori.Add($"Impossibile creare la cartella {CartellaBackup}: {ex.Message}");
                return esito;
            }

            string suffisso = DateTime.Now.ToString(FormatoTimestamp, CultureInfo.InvariantCulture);

            foreach (var elemento in elementi)
            {
                try
                {
                    if (!File.Exists(elemento.Percorso))
                    {
                        if (segnalaMancanti)
                            esito.Errori.Add($"{elemento.Descrizione}: file non trovato ({elemento.Percorso}).");
                        continue;
                    }

                    string estensione = Path.GetExtension(elemento.Percorso);
                    string destinazione = Path.Combine(
                        CartellaBackup,
                        $"{elemento.NomeBase}_backup_{suffisso}{estensione}");

                    if (elemento.IsDatabase)
                        CopiaDatabase(elemento.Percorso, destinazione);
                    else
                        File.Copy(elemento.Percorso, destinazione, overwrite: true);

                    esito.Riusciti.Add(Path.GetFileName(destinazione));
                }
                catch (Exception ex)
                {
                    esito.Errori.Add($"{elemento.Descrizione}: {ex.Message}");
                }
            }

            ApplicaLimiteGiorni();

            return esito;
        }

        private static void CopiaDatabase(string origine, string destinazione)
        {
            if (File.Exists(destinazione))
                File.Delete(destinazione);

            try
            {
                using (var src = new SqliteConnection($"Data Source={origine};Mode=ReadOnly;Pooling=False"))
                using (var dst = new SqliteConnection($"Data Source={destinazione};Pooling=False"))
                {
                    src.Open();
                    dst.Open();
                    src.BackupDatabase(dst);
                }
            }
            catch
            {
                SqliteConnection.ClearAllPools();

                if (File.Exists(destinazione))
                    File.Delete(destinazione);

                File.Copy(origine, destinazione, overwrite: true);
            }
        }

        public static Esito BackupGiornalieroAutomatico()
        {
            DateTime oggi = DateTime.Now.Date;
            var file = ElencaFile();

            var daSalvare = new List<ElementoBackup>();

            foreach (var elemento in ElementiSalvabili)
            {
                if (!File.Exists(elemento.Percorso) || new FileInfo(elemento.Percorso).Length == 0)
                    continue;

                bool giaPresente = file.Any(f => f.Elemento == elemento && f.Data.Date == oggi);
                if (!giaPresente)
                    daSalvare.Add(elemento);
            }

            if (daSalvare.Count == 0)
                return new Esito();

            return EseguiBackup(daSalvare, segnalaMancanti: false);
        }

        public static void ApplicaLimiteGiorni()
        {
            try
            {
                foreach (var gruppo in ElencaFile().GroupBy(f => f.Elemento))
                {
                    var giorniDaTenere = gruppo
                        .Select(f => f.Data.Date)
                        .Distinct()
                        .OrderByDescending(d => d)
                        .Take(GiorniMassimi)
                        .ToHashSet();

                    foreach (var f in gruppo.Where(f => !giorniDaTenere.Contains(f.Data.Date)))
                    {
                        try { File.Delete(f.Percorso); }
                        catch { }
                    }
                }
            }
            catch
            {
            }
        }

        public static Esito Ripristina(PuntoRipristino punto, IEnumerable<ElementoBackup> elementi)
        {
            var esito = new Esito();

            SqliteConnection.ClearAllPools();

            foreach (var elemento in elementi)
            {
                try
                {
                    if (!punto.File.TryGetValue(elemento.NomeBase, out string sorgente) ||
                        !File.Exists(sorgente))
                    {
                        esito.Errori.Add($"{elemento.Descrizione}: backup non presente in questo punto.");
                        continue;
                    }

                    File.Copy(sorgente, elemento.Percorso, overwrite: true);

                    if (elemento.IsDatabase)
                    {
                        foreach (string suffissoAccessorio in new[] { "-wal", "-shm", "-journal" })
                        {
                            string accessorio = elemento.Percorso + suffissoAccessorio;
                            if (File.Exists(accessorio))
                                File.Delete(accessorio);
                        }
                    }

                    esito.Riusciti.Add($"Ripristinato: {elemento.Descrizione}");
                }
                catch (Exception ex)
                {
                    esito.Errori.Add($"{elemento.Descrizione}: {ex.Message}");
                }
            }

            return esito;
        }

        public static Esito EliminaDalPunto(PuntoRipristino punto, IEnumerable<ElementoBackup> elementi)
        {
            var esito = new Esito();

            foreach (var elemento in elementi)
            {
                try
                {
                    if (punto.File.TryGetValue(elemento.NomeBase, out string file) && File.Exists(file))
                    {
                        File.Delete(file);
                        esito.Riusciti.Add($"Eliminato backup: {elemento.Descrizione}");
                    }
                }
                catch (Exception ex)
                {
                    esito.Errori.Add($"{elemento.Descrizione}: {ex.Message}");
                }
            }

            return esito;
        }

        public static Esito EliminaPunti(IReadOnlyCollection<PuntoRipristino> punti)
        {
            var esito = new Esito();

            if (punti.Count == 0)
            {
                esito.Riusciti.Add("Punti di ripristino: nessuno da eliminare");
                return esito;
            }

            int fileEliminati = 0;

            foreach (var punto in punti)
            {
                foreach (string file in punto.File.Values)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                            fileEliminati++;
                        }
                    }
                    catch (Exception ex)
                    {
                        esito.Errori.Add($"{Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            }

            esito.Riusciti.Add(
                $"Eliminati {punti.Count} punti di ripristino ({fileEliminati} file in BackupDB)");

            return esito;
        }
    }
}
