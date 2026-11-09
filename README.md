Fate del bene e pregate

# emmaus_v2

Applicazione Windows Forms (.NET 10, `net10.0-windows`) 
per meditare la Sacra Scrittura.

## Prima di compilare

Questo repository **non contiene**:

- `CEI_08.SQLite3`: il testo CEI 2008 è © Conferenza Episcopale Italiana.
  Non essendo distribuito insieme al programma va procurato a parte,
  per conto proprio e nel rispetto delle condizioni stabilite dalla CEI,
  e va inserito nella cartella del progetto (oppure
  dell'eseguibile). Senza questo file il programma non si avvia.
- `commenti.db`: viene creato dal programma con le note dell'utente.

Il repository **contiene** invece, nella cartella `Risorse`:

- immagini e icone del programma, create con strumenti di intelligenza
  artificiale e non coperte dalla licenza MIT, salvo diversa indicazione;
- i font `Cardo-*.ttf` (testo biblico), con la relativa licenza in
  `Risorse/Font/OFL.txt`;
- i font `ETBook-*.ttf` (licenza MIT), usati per tutti i PDF generati dal
  programma, con il relativo testo di licenza in `Risorse/Font/ETBook-LICENSE.txt`;
- `Guida.html`, la guida in linea (si apre con F1 da ogni finestra, sul capitolo pertinente), che
  contiene anche i font ET Book incorporati.

## Requisiti

- Windows e runtime .NET 10.
- Microsoft Edge WebView2 (già presente in Windows 11) per mostrare la guida
  nella finestra del programma; se manca, la guida si apre nel browser
  predefinito.
- Una voce italiana di sintesi vocale installata in Windows, per la lettura
  delle preghiere del timer.

## Documentazione

Il manuale d'uso completo è in `docs/Emmaus_2.0_Manuale_d_uso.pdf`
(sommario cliccabile); la stessa guida è consultabile dal programma con F1 o con il pulsante «?» della Manutenzione.

## Copyright del testo CEI

Il programma non esporta né salva copie estese del testo CEI. Nella finestra
di meditazione Ctrl+A e Ctrl+C sono disattivati; «Copia» e «Crea PDF» del clic
destro valgono per un solo versetto e riportano la nota di licenza CEI.
Backup, esportazione dei commenti e PDF della mappa concettuale riguardano
solo i commenti e la cronologia dell'utente e non contengono il testo dei
versetti.

## Sintesi vocale

Le preghiere del timer sono lette dalla sintesi vocale di sistema di Windows
(API `System.Speech`), con le voci italiane installate nel PC dell'utente
(Impostazioni > Ora e lingua > Voce). Il programma non include, non
ridistribuisce e non registra voci o file audio.

Le voci di Windows restano di proprietà dei rispettivi produttori (ad esempio
Microsoft) e sono soggette ai loro termini di licenza: la licenza MIT di questo
progetto non le riguarda. Non copiare le voci nel repository né in un
installer, e non pubblicare registrazioni audio generate con esse senza
averne verificato i termini d'uso. Microsoft e Windows sono marchi dei
rispettivi proprietari.

## Licenza

Codice sorgente: MIT, vedere `LICENSE`. scritto da DisabiliCristiani
disabilicristiani@gmail.com - Fate del bene e pregate.

Librerie, font, risorse e testo CEI: vedere `THIRD-PARTY-NOTICES.md`.
