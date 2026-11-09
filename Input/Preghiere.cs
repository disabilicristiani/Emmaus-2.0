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

using System.Collections.Generic;

namespace emmaus_v2
{
    /// <summary>
    /// Testi delle preghiere mostrate nella griglia di FormLEGGI quando si
    /// preme uno dei due pulsanti di preghiera del timer.
    /// Ogni periodo e' su una riga a parte.
    /// </summary>
    public static class Preghiere
    {
        private static string Righe(params string[] righe) =>
            string.Join("\n", righe);

        private static readonly (string Titolo, string Testo) PadreNostro =
            ("Padre nostro", Righe(
                "Padre nostro, che sei nei Cieli,",
                "sia santificato il Tuo Nome,",
                "venga il Tuo Regno,",
                "sia fatta la Tua Volontà,",
                "come in Cielo così in terra.",
                "Dacci oggi il nostro pane quotidiano,",
                "e rimetti a noi i nostri debiti",
                "come anche noi li rimettiamo ai nostri debitori,",
                "e non abbandonarci alla tentazione,",
                "ma liberaci dal male.",
                "Amen."));

        private static readonly (string Titolo, string Testo) AveMaria =
            ("Ave Maria", Righe(
                "Ave, o Maria, piena di Grazia, il Signore è con Te.",
                "Tu sei Benedetta fra le donne",
                "e Benedetto è il Frutto del Tuo Seno, Gesù.",
                "Santa Maria, Madre di Dio,",
                "prega per noi peccatori,",
                "adesso e nell'ora della nostra morte.",
                "Amen."));

        private static readonly (string Titolo, string Testo) Gloria =
            ("Gloria", Righe(
                "Gloria al Padre e al Figlio e allo Spirito Santo,",
                "come era nel principio, e ora e sempre,",
                "nei secoli dei secoli.",
                "Amen."));

        private static readonly (string Titolo, string Testo) EternoRiposo =
            ("Eterno riposo", Righe(
                "L'eterno riposo dona a loro, Signore,",
                "e splenda ad essi la Luce Perpetua.",
                "Riposino in Pace.",
                "Amen."));

        private static readonly (string Titolo, string Testo) SalveRegina =
            ("Salve Regina", Righe(
                "Salve, Regina, Madre di Misericordia,",
                "Vita, Dolcezza e Speranza nostra, salve.",
                "A Te ricorriamo, esuli figli di Eva;",
                "a Te sospiriamo, gementi e piangenti in questa valle di lacrime.",
                "Orsù dunque, Avvocata nostra,",
                "rivolgi a noi gli Occhi Tuoi Misericordiosi.",
                "E mostraci, dopo questo esilio, Gesù,",
                "il Frutto Benedetto del Tuo Seno.",
                "O Clemente, o Pia, o Dolce Vergine Maria."));

        public static readonly IReadOnlyList<(string Titolo, string Testo)> Gesu =
            new List<(string Titolo, string Testo)>
            {
                ("Intenzione",
                 "Per il Santo Padre, la Chiesa e in Suffragio delle Sante Anime del Purgatorio"),
                PadreNostro,
                AveMaria,
                Gloria,
                EternoRiposo,
                ("Preghiera di Santa Gertrude", Righe(
                    "Eterno Padre,",
                    "io offro il Preziosissimo Sangue,",
                    "del Tuo Divin Figlio, Gesù,",
                    "in unione con le Sante Messe,",
                    "dette in tutto il mondo, oggi,",
                    "per tutte le Sante Anime del Purgatorio,",
                    "per i peccatori di ogni luogo,",
                    "per i peccatori della Chiesa universale,",
                    "quelli della mia casa,",
                    "e dentro la mia famiglia.",
                    "Amen"))
            };

        public static readonly IReadOnlyList<(string Titolo, string Testo)> Maria =
            new List<(string Titolo, string Testo)>
            {
                ("Intenzione", "Per le intenzioni della Madonna"),
                AveMaria,
                SalveRegina,
                Gloria,
                ("Giaculatoria", Righe(
                    "Madre di Dio,",
                    "Corredentrice del mondo,",
                    "Prega per noi.",
                    "Amen."))
            };
    }
}
