# ArcweaveElementComponentHandler — Design Notes

Script creato, non ancora implementato. Questo file serve per rivedere le scelte di design prima di usarlo in scena.

---

## Il problema che risolve

Con `ArcweaveAttributeHandler` puoi reagire agli **attributi di componenti globali** del progetto (es. `SceneSettings.Time = "Night"`). Ma non puoi reagire al fatto che **un elemento di dialogo ha un certo componente attaccato**.

Il pattern nei giochi: il narrative designer crea in Arcweave un componente e lo attacca agli elementi dove vuole che succeda qualcosa. Unity vede il componente e reagisce. Nessuna stringa da cercare nel contenuto degli elementi, nessun attributo nascosto — il componente stesso è il segnale.

---

## Use case concreti che motivano il design

### Caso 1 — SwingSword (azione pura, zero attributi)

Il Merchant ha una quest sul combattimento. Quando il dialogo arriva all'elemento che sblocca l'abilità "swing sword", il narrative designer attacca un componente `SwingSword` a quell'elemento. Unity rileva il componente e fa swingare la spada del player.

```
Elemento: "Il mercante ti insegna la tecnica"
Componenti attaccati: [SwingSword]
```

In Unity — `SwingSwordHandler`:
- `OnComponentDetected` → play animazione swing + sfx
- `OnComponentAbsent` → resetta stato (elemento successivo non ha il componente)

Questo caso dimostra che `OnComponentAbsent` **serve**: la spada deve swingare solo durante quell'elemento, non per tutto il resto del dialogo.

Dimostra anche che **non servono attributi**: la sola presenza del componente è il trigger. `GetAttributeValue` non viene usato — e va bene così, l'helper c'è ma non è obbligatorio.

---

### Caso 2 — Merchant con attributi multipli

Il componente `Merchant` ha attributi `ItemName`, `Price`, `Rarity`. Il narrative designer compila questi dati in Arcweave, Unity li legge e popola un pannello shop.

```
Elemento: "Il mercante mostra la sua merce"
Componenti attaccati: [Merchant]
  Merchant.ItemName = "Elixir"
  Merchant.Price    = "50"
  Merchant.Rarity   = "rare"
```

In Unity — `MerchantShopHandler`:
- `OnComponentDetected` → legge tutti e tre gli attributi con `GetAttributeValue`, popola UI
- `OnComponentAbsent` → chiude il pannello shop

Questo giustifica `GetAttributeValue` nella base class: evita di riscrivere il loop attributi in ogni subclass.

---

### Caso 3 — DialogueStart / DialogueEnd come componenti

Invece di usare attributi `dialogue_start` / `dialogue_end`, il narrative designer attacca componenti omonimi agli elementi. Più visivo in Arcweave (si vedono le icone dei componenti sul grafo).

```
Elemento iniziale: componenti [DialogueStart]
Elemento finale:   componenti [DialogueEnd]
```

**⚠️ Nota tecnica importante:** il sistema attuale in `DialogueTrigger.FindDialogueStartElement()` fa matching su **attributi**, non su componenti. Per supportare questo pattern servirebbe modificare quel metodo per cercare `element.HasComponent("DialogueStart")` in alternativa all'attributo. Non è un cambiamento grande, ma va fatto consapevolmente — e nel frattempo i due sistemi coesistono. Da decidere se ha senso migrare o mantenere entrambe le opzioni.

---

## Come funziona

```
Player entra in elemento
        ↓
onElementEnter si attiva
        ↓
   elemento ha il componente "SwingSword"?
       ↙                          ↘
      SÌ                           NO
       ↓                            ↓
OnComponentDetected()         OnComponentAbsent()
(play animazione,             (resetta stato,
 sfx, abilità sbloccata)       nessuna animazione)
```

Si attiva **ogni volta** che il player avanza nel dialogo, non solo alla fine.

---

## Differenza con ArcweaveAttributeHandler

| | `ArcweaveAttributeHandler` | `ArcweaveElementComponentHandler` |
|---|---|---|
| Si attiva su | Fine dialogo + import | Ogni elemento del dialogo |
| Legge da | Componenti globali del progetto | Componenti attaccati all'elemento corrente |
| Uso tipico | Config statica di scena (meteo, colori) | Meccaniche e azioni triggerate durante il dialogo |
| Ha effetti istantanei? | No, configurazione | Sì — animazioni, UI, suoni, spawn |

---

## Le scelte di design

### 1. `OnComponentAbsent()` — confermato necessario

Il caso `SwingSword` lo dimostra: la spada swinga su un elemento, l'elemento successivo non ha il componente, la spada deve smettere. Senza `OnComponentAbsent` devi gestirlo altrove.

È `virtual` con body vuoto — se un handler non ha bisogno di undo, lo ignora semplicemente.

---

### 2. `GetAttributeValue()` nella base class — confermato utile

Il caso `Merchant` lo giustifica. Zero dubbi su questo.

---

### 3. Solo `componentName` nell'Inspector — confermato giusto

- `SwingSword` → zero attributi, solo presenza del componente
- `Merchant` → tre attributi con nomi diversi
- `DialogueStart` → zero attributi

Mettere un secondo campo `attributeName` nella base non avrebbe senso: i casi sono troppo diversi tra loro. Ogni subclass gestisce i propri attributi con `GetAttributeValue`.

---

## Prossimi passi

1. **Decidere se migrare `dialogue_start`/`dialogue_end` a componenti** (Caso 3) o tenerli come attributi. Non è urgente — i due sistemi coesistono.
2. **Implementare `SwingSwordHandler`** come primo test concreto — è il più semplice (zero attributi, solo presenza/assenza).
3. Attaccare l'handler a un GameObject nella scena, creare il componente `SwingSword` in Arcweave, attaccarlo a un elemento di test, verificare in Play Mode.
4. Se funziona, aggiornare il README con questo pattern.
