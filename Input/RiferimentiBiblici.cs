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

using System.Text.RegularExpressions;

namespace Listbox
{
    
    public struct RiferimentoBiblico
    {
        public int Start;
        public int Length;
        public Input.LibroBiblico Libro;
        public int Capitolo;
        public int VersettoDa;
        public int VersettoA;

        public string Etichetta => VersettoDa == VersettoA
            ? $"{Libro.Nome} {Capitolo},{VersettoDa}"
            : $"{Libro.Nome} {Capitolo},{VersettoDa}-{VersettoA}";

        public string EtichettaBreve => VersettoDa == VersettoA
            ? $"{Libro.Abbreviazione} {Capitolo},{VersettoDa}"
            : $"{Libro.Abbreviazione} {Capitolo},{VersettoDa}-{VersettoA}";
    }

    public static class RiferimentiBiblici
    {
        private static readonly Regex RegexRiferimento = new Regex(
            @"\[Cf\.\s*(?<libro>[1-3]?\s?[A-Za-zÀ-ÿ]+(?:\s[A-Za-zÀ-ÿ]+)?)\s+(?<cap>\d+)\s*,\s*(?<versetti>\d+(?:-\d+)?)\]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static List<RiferimentoBiblico> Trova(string testo, int offset = 0)
        {
            var risultati = new List<RiferimentoBiblico>();
            if (string.IsNullOrEmpty(testo))
                return risultati;

            foreach (Match m in RegexRiferimento.Matches(testo))
            {
                string nomeLibro = m.Groups["libro"].Value.Trim();
                if (!int.TryParse(m.Groups["cap"].Value, out int capitolo))
                    continue;

                string versettiTesto = m.Groups["versetti"].Value;
                int versettoDa, versettoA;
                if (versettiTesto.Contains('-'))
                {
                    var parti = versettiTesto.Split('-');
                    if (!int.TryParse(parti[0], out versettoDa) || !int.TryParse(parti[1], out versettoA))
                        continue;
                }
                else
                {
                    if (!int.TryParse(versettiTesto, out versettoDa))
                        continue;
                    versettoA = versettoDa;
                }

                var libroTrovato = TrovaLibro(nomeLibro);
                if (libroTrovato == null)
                    continue;

                risultati.Add(new RiferimentoBiblico
                {
                    Start = m.Index + offset,
                    Length = m.Length,
                    Libro = libroTrovato,
                    Capitolo = capitolo,
                    VersettoDa = versettoDa,
                    VersettoA = versettoA
                });
            }

            return risultati;
        }

        public static Input.LibroBiblico TrovaLibro(string nomeLibro)
        {
            string chiaveCercata = Normalizza(nomeLibro);

            return Input.LibriCEI.Elenco.FirstOrDefault(l =>
                Normalizza(l.Nome) == chiaveCercata ||
                Normalizza(l.Abbreviazione) == chiaveCercata ||
                Normalizza(Input.LibriCEI.NomeVisualizzato(l)) == chiaveCercata);
        }

        public static string Normalizza(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return "";

            string n = nome.Trim();

            n = Regex.Replace(n, @"^III\b\.?\s*", "3 ", RegexOptions.IgnoreCase);
            n = Regex.Replace(n, @"^II\b\.?\s*", "2 ", RegexOptions.IgnoreCase);
            n = Regex.Replace(n, @"^I\b\.?\s*", "1 ", RegexOptions.IgnoreCase);

            n = Regex.Replace(n, @"^([123])\s*[°ªº]?\s*", "$1 ", RegexOptions.IgnoreCase);

            n = Regex.Replace(n, @"\s+", " ").Trim();

            return n.Replace(" ", "").ToLowerInvariant();
        }
    }
}
