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

namespace Input
{
    
    public class LibroBiblico
    {
        public int BookNumber { get; set; }      
        public string Nome { get; set; }          
        public string Abbreviazione { get; set; } 
    }

    public static class LibriCEI
    {
        
        public static List<LibroBiblico> Elenco = new List<LibroBiblico>();

        public static void Carica(string connectionStringBibbia)
        {
            Elenco.Clear(); 
            using (var conn = new SqliteConnection(connectionStringBibbia))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                
                cmd.CommandText = "SELECT book_number, short_name, long_name FROM books ORDER BY book_number;";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        
                        Elenco.Add(new LibroBiblico
                        {
                            BookNumber = reader.GetInt32(0),      
                            Abbreviazione = reader.GetString(1),  
                            Nome = reader.GetString(2)            
                        });
                    }
                }
            }
        }

        private static readonly Dictionary<int, (string Testamento, string Categoria)> Categorie =
            new Dictionary<int, (string, string)>
            {
                
                {10,("Antico Testamento","Pentateuco")}, {20,("Antico Testamento","Pentateuco")},
                {30,("Antico Testamento","Pentateuco")}, {40,("Antico Testamento","Pentateuco")},
                {50,("Antico Testamento","Pentateuco")},
                
                {60,("Antico Testamento","Libri storici")}, {70,("Antico Testamento","Libri storici")},
                {80,("Antico Testamento","Libri storici")}, {90,("Antico Testamento","Libri storici")},
                {100,("Antico Testamento","Libri storici")}, {110,("Antico Testamento","Libri storici")},
                {120,("Antico Testamento","Libri storici")}, {130,("Antico Testamento","Libri storici")},
                {140,("Antico Testamento","Libri storici")}, {150,("Antico Testamento","Libri storici")},
                {160,("Antico Testamento","Libri storici")}, {170,("Antico Testamento","Libri storici")},
                {180,("Antico Testamento","Libri storici")}, {190,("Antico Testamento","Libri storici")},
                {462,("Antico Testamento","Libri storici")}, {464,("Antico Testamento","Libri storici")},
                
                {220,("Antico Testamento","Libri sapienziali")}, {230,("Antico Testamento","Libri sapienziali")},
                {240,("Antico Testamento","Libri sapienziali")}, {250,("Antico Testamento","Libri sapienziali")},
                {260,("Antico Testamento","Libri sapienziali")}, {270,("Antico Testamento","Libri sapienziali")},
                {280,("Antico Testamento","Libri sapienziali")},
                
                {290,("Antico Testamento","Libri profetici")}, {300,("Antico Testamento","Libri profetici")},
                {310,("Antico Testamento","Libri profetici")}, {320,("Antico Testamento","Libri profetici")},
                {330,("Antico Testamento","Libri profetici")}, {340,("Antico Testamento","Libri profetici")},
                {350,("Antico Testamento","Libri profetici")}, {360,("Antico Testamento","Libri profetici")},
                {370,("Antico Testamento","Libri profetici")}, {380,("Antico Testamento","Libri profetici")},
                {390,("Antico Testamento","Libri profetici")}, {400,("Antico Testamento","Libri profetici")},
                {410,("Antico Testamento","Libri profetici")}, {420,("Antico Testamento","Libri profetici")},
                {430,("Antico Testamento","Libri profetici")}, {440,("Antico Testamento","Libri profetici")},
                {450,("Antico Testamento","Libri profetici")}, {460,("Antico Testamento","Libri profetici")},
                
                {470,("Nuovo Testamento","Vangeli")}, {480,("Nuovo Testamento","Vangeli")},
                {490,("Nuovo Testamento","Vangeli")}, {500,("Nuovo Testamento","Vangeli")},
                
                {510,("Nuovo Testamento","Atti degli Apostoli")},
                
                {520,("Nuovo Testamento","Lettere paoline")}, {530,("Nuovo Testamento","Lettere paoline")},
                {540,("Nuovo Testamento","Lettere paoline")}, {550,("Nuovo Testamento","Lettere paoline")},
                {560,("Nuovo Testamento","Lettere paoline")}, {570,("Nuovo Testamento","Lettere paoline")},
                {580,("Nuovo Testamento","Lettere paoline")}, {590,("Nuovo Testamento","Lettere paoline")},
                {600,("Nuovo Testamento","Lettere paoline")}, {610,("Nuovo Testamento","Lettere paoline")},
                {620,("Nuovo Testamento","Lettere paoline")}, {630,("Nuovo Testamento","Lettere paoline")},
                {640,("Nuovo Testamento","Lettere paoline")},
                
                {650,("Nuovo Testamento","Lettera agli Ebrei")},
                
                {660,("Nuovo Testamento","Lettere cattoliche")}, {670,("Nuovo Testamento","Lettere cattoliche")},
                {680,("Nuovo Testamento","Lettere cattoliche")}, {690,("Nuovo Testamento","Lettere cattoliche")},
                {700,("Nuovo Testamento","Lettere cattoliche")}, {710,("Nuovo Testamento","Lettere cattoliche")},
                {720,("Nuovo Testamento","Lettere cattoliche")},
                
                {730,("Nuovo Testamento","Apocalisse")},
            };

        public static (string Testamento, string Categoria) CategoriaDiLibro(int bookNumber)
        {
            return Categorie.TryGetValue(bookNumber, out var c) ? c : ("", "");
        }

        private static readonly Dictionary<string, string> NomiAlternativi = new Dictionary<string, string>
        {
            { "Sirach", "Siracide (Ecclesiastico)" },
            { "Qoelet", "Qoelet (Ecclesiaste)" },
        };

        public static string NomeVisualizzato(LibroBiblico libro)
        {
            return NomiAlternativi.TryGetValue(libro.Nome, out var nome) ? nome : libro.Nome;
        }
    }
}
