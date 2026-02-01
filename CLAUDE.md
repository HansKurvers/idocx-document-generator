# Claude Code Ontwikkelrichtlijnen

Dit bestand bevat richtlijnen voor het ontwikkelen aan de idocx-document-generator.

## Placeholders

### Belangrijke regel bij het maken van nieuwe placeholders

**Wanneer je een nieuwe placeholder toevoegt in de code, moet deze ALTIJD ook worden gedocumenteerd in de volgende bestanden:**

1. **`placeholders.md`** (root) - Hoofddocumentatie voor de admin
2. **`LocalTest/placeholders.md`** - Kopie voor lokaal testen
3. **`README.md`** - In de placeholder sectie indien relevant

### Placeholder documentatie formaat

Voeg nieuwe placeholders toe in het volgende formaat:
```
- [[PlaceholderNaam]] - Beschrijving van wat de placeholder doet
```

### Placeholder conventies

- **Ouderschapsplan placeholders**: CamelCase zonder underscores (bijv. `[[Partij1Benaming]]`)
- **Convenant placeholders**: UPPER_SNAKE_CASE (bijv. `[[PARTIJ1_AANDUIDING]]`)
- **Hoofdletter varianten**: Voeg `Hoofdletter` of `_HOOFDLETTER` toe voor gebruik aan het begin van zinnen

### IsAnoniem gedrag

Placeholders die afhankelijk zijn van `IsAnoniem`:

| Placeholder | IsAnoniem = true | IsAnoniem = false |
|-------------|------------------|-------------------|
| `[[Partij1Benaming]]` | "de vader" / "de moeder" | Roepnaam (bijv. "Jan") |
| `[[PARTIJ1_AANDUIDING]]` | "de man" / "de vrouw" | Roepnaam + achternaam (bijv. "Jan de Vries") |

## Code structuur

### PlaceholderBuilders

Alle placeholder builders bevinden zich in:
`Services/DocumentGeneration/Processors/PlaceholderBuilders/`

- **BasePlaceholderBuilder.cs** - Basis class met gedeelde helper methodes
- **PartijPlaceholderBuilder.cs** (Order: 20) - Partij1/Partij2 placeholders
- **ConvenantPlaceholderBuilder.cs** (Order: 80) - Convenant-specifieke placeholders

### Volgorde van builders

Builders worden uitgevoerd op basis van hun `Order` property (laag naar hoog).

## Git workflow

- **main** - Productie branch
- **development** - Development/staging branch

**NOOIT direct op `main` committen of pushen.** Alle wijzigingen gaan via de `development` branch. Commit en push altijd naar `development`. Pas na review/goedkeuring wordt `development` naar `main` gemerged.

### Git Merge Policy

- **NOOIT** uit eigen beweging een merge uitvoeren (bijv. `git merge`, `git rebase` naar een andere branch, of een PR mergen).
- Een merge mag **alleen** plaatsvinden als de gebruiker hier een **directe, expliciete opdracht** toe geeft.
- Wanneer de gebruiker opdracht geeft tot een merge, **altijd eerst bevestiging vragen** via AskUserQuestion: "Weet u zeker dat u wilt mergen? Dit kan niet ongedaan worden gemaakt." met opties Ja/Nee. Pas na bevestiging uitvoeren.
