# Arcweave Unity Demo

Questo progetto dimostra l'integrazione di Arcweave con Unity, permettendo di importare progetti Arcweave sia durante lo sviluppo che a runtime.

## Caratteristiche

- Importazione di progetti Arcweave da web (usando API key e project hash)
- Importazione di progetti Arcweave da file JSON locale
- Supporto per progetti precaricati inclusi nella build
- Interfaccia utente semplice per l'importazione
- Gestione delle variabili e degli eventi Arcweave

## Per gli sviluppatori

### Configurazione iniziale

1. Clona questo repository
2. Apri il progetto in Unity
3. Assicurati che l'asset Arcweave sia importato correttamente

### Includere un file JSON precaricato nella build

Per includere un file JSON precaricato nella build:

1. Posiziona il tuo file JSON in `Assets/Arcweave/project.json`
2. Vai su `Arcweave > Copy JSON to StreamingAssets` nel menu di Unity
3. Il file verrà copiato in `Assets/StreamingAssets/arcweave/project.json`
4. Quando crei una build, questo file verrà incluso automaticamente

### Processo di build

Durante il processo di build:

1. Il file JSON in StreamingAssets viene incluso automaticamente nella build
2. Viene creata una cartella `arcweave` nella directory della build
3. All'avvio, l'applicazione caricherà automaticamente il file JSON precaricato
4. Gli utenti possono importare nuovi file JSON posizionandoli nella cartella `arcweave`

## Per gli utenti finali

### Importare un progetto Arcweave da web

1. Avvia l'applicazione
2. Inserisci la tua API key e project hash nei campi appropriati
3. Clicca sul pulsante "Import Web"
4. Attendi il completamento dell'importazione

### Importare un progetto Arcweave da file locale

1. Avvia l'applicazione
2. Posiziona il tuo file JSON nella cartella `arcweave` accanto all'eseguibile dell'applicazione
   - Su Windows: `[Cartella Gioco]/arcweave/project.json`
3. Clicca sul pulsante "Import Local"
4. Attendi il completamento dell'importazione

### Risoluzione dei problemi

Se riscontri problemi durante l'importazione:

- Assicurati che il file JSON sia formattato correttamente
- Verifica che il percorso del file sia corretto
- Controlla che l'API key e il project hash siano validi (per l'importazione da web)
- Riavvia l'applicazione e riprova

## Struttura del progetto

- `Assets/Scripts/RuntimeArcweaveImporter.cs`: Gestisce l'importazione dei progetti Arcweave a runtime
- `Assets/Scripts/ArcweaveImporterUI.cs`: Gestisce l'interfaccia utente per l'importazione
- `Assets/Scripts/Editor/ArcweaveBuildProcessor.cs`: Script editor per il processo di build
- `Assets/StreamingAssets/arcweave/project.json`: File JSON precaricato incluso nella build

## Licenza

Questo progetto è rilasciato sotto licenza MIT. Vedi il file LICENSE per maggiori dettagli. 