# Arcweave Unity Demo

Questo progetto dimostra l'integrazione di Arcweave con Unity, permettendo di importare progetti Arcweave sia durante lo sviluppo che a runtime.

## Caratteristiche

- Importazione di progetti Arcweave da web (usando API key e project hash)
- Importazione di progetti Arcweave da file JSON locale
- Supporto per progetti precaricati inclusi nella build
- Supporto per immagini Arcweave da diverse fonti (Resources, StreamingAssets, cartella della build)
- Interfaccia utente semplice per l'importazione
- Gestione delle variabili e degli eventi Arcweave

## Per gli sviluppatori

### Configurazione iniziale

1. Clona questo repository
2. Apri il progetto in Unity
3. Assicurati che l'asset Arcweave sia importato correttamente

### Includere un progetto precaricato nella build

Per includere un progetto precaricato (JSON e immagini) nella build:

1. Posiziona il tuo file JSON in `Assets/Arcweave/project.json`
2. Posiziona le tue immagini in `Assets/Arcweave/images/`
3. Vai su `Arcweave > Copy Project to StreamingAssets` nel menu di Unity
4. I file verranno copiati in `Assets/StreamingAssets/arcweave/`
5. Quando crei una build, questi file verranno inclusi automaticamente

Puoi anche copiare solo il JSON o solo le immagini usando i comandi separati:
- `Arcweave > Copy JSON to StreamingAssets`
- `Arcweave > Copy Images to StreamingAssets`

### Processo di build

Durante il processo di build:

1. I file in StreamingAssets vengono inclusi automaticamente nella build
2. Viene creata una cartella `arcweave` nella directory della build
3. All'avvio, l'applicazione caricherà automaticamente il progetto precaricato
4. Gli utenti possono importare nuovi file posizionandoli nella cartella `arcweave`

### Gestione delle immagini

Le immagini vengono cercate in questo ordine:
1. Cartella `Resources` di Unity (comportamento originale)
2. `StreamingAssets/arcweave/images/` (per immagini precaricate)
3. `[Cartella Gioco]/arcweave/images/` (per immagini aggiunte dall'utente)

Per caricare manualmente un'immagine da qualsiasi fonte, puoi utilizzare il sistema ArcweaveImageLoader:

```csharp
// Ottieni un'istanza del loader
Arcweave.ArcweaveImageLoader imageLoader = Arcweave.ArcweaveImageLoader.Instance;

// Carica un'immagine dal percorso specificato
Texture2D texture = imageLoader.LoadImage("path/to/image.png");
```

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
3. Se il tuo progetto include immagini, posizionale in `[Cartella Gioco]/arcweave/images/`
4. Clicca sul pulsante "Import Local"
5. Attendi il completamento dell'importazione

### Risoluzione dei problemi

Se riscontri problemi durante l'importazione:

- Assicurati che il file JSON sia formattato correttamente
- Verifica che il percorso del file sia corretto
- Controlla che l'API key e il project hash siano validi (per l'importazione da web)
- Per problemi con le immagini, verifica che siano nella cartella corretta e che i nomi file corrispondano a quelli nel JSON
- Riavvia l'applicazione e riprova

## Struttura del progetto

- `Assets/Scripts/RuntimeArcweaveImporter.cs`: Gestisce l'importazione dei progetti Arcweave a runtime
- `Assets/Scripts/ArcweaveImporterUI.cs`: Gestisce l'interfaccia utente per l'importazione
- `Assets/Scripts/ArcweaveImageLoader.cs`: Gestisce il caricamento delle immagini da diverse fonti
- `Assets/Scripts/ArcweaveCoverExtension.cs`: Estende la classe Cover con funzionalità avanzate di caricamento immagini
- `Assets/Scripts/Editor/ArcweaveBuildProcessor.cs`: Script editor per il processo di build
- `Assets/StreamingAssets/arcweave/project.json`: File JSON precaricato incluso nella build
- `Assets/StreamingAssets/arcweave/images/`: Cartella per le immagini precaricate

## Licenza

Questo progetto è rilasciato sotto licenza MIT. Vedi il file LICENSE per maggiori dettagli. 