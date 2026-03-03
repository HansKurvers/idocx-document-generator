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

### Placeholder verwerkingsvolgorde (PlaceholderProcessor.cs)

Het placeholder systeem verwerkt in 4 stappen:

1. **Grammatica regels** — `[[heeft/hebben]]`, `[[zijn/is]]` etc. op basis van kinderen
2. **Placeholder builders** — Geregistreerde builders (in `Order` volgorde) genereren systeemplaceholders uit dossierdata
3. **Custom placeholders** — Uit de `placeholder_catalogus` tabel (dossier > gebruiker > systeem > standaard_waarde). Worden NIET overschreven als een builder ze al heeft gezet
4. **Conditionele placeholders** — Placeholders met `heeft_conditie = 1` en een `conditie_config` JSON. Deze worden geëvalueerd door `ConditieEvaluator` en overschrijven bestaande waarden

### Conditioneel placeholder systeem

Conditionele placeholders worden aangemaakt via de Placeholder Catalogus UI en opgeslagen in de `placeholder_catalogus` tabel.

**Belangrijke bestanden:**
- `Models/ConditieModels.cs` — Data models (`ConditieConfig`, `ConditieRegel`, `Conditie`)
- `Services/DocumentGeneration/Processors/ConditieEvaluator.cs` — Evaluatie logica
- `Services/DocumentGeneration/Processors/PlaceholderProcessor.cs` — Orchestratie (stap 4)

**Hoe het werkt:**
1. De `conditie_config` JSON wordt geparsed naar een `ConditieConfig` object
2. Een evaluatie-context wordt opgebouwd uit bestaande replacements + computed fields
3. Context bevat zowel `snake_case` als `PascalCase` varianten (bridge via `SnakeCaseToPascalCase`)
4. Regels worden in volgorde geëvalueerd; de eerste match bepaalt het resultaat
5. Als geen regel matcht, wordt de default waarde gebruikt
6. Het resultaat kan geneste placeholders bevatten (bijv. `[[PARTIJ1_AANDUIDING]]`) die worden opgelost

**Vergelijking:** `AreEqual` normaliseert underscores naar spaties en is case-insensitive. Dus `"partij1"` matcht `"Partij 1"` NIET — het matcht `"Partij_1"` wel.

## CI/CD & Deployment

Alle drie de repositories hebben GitHub Actions workflows voor automatische deployment.

### idocx-document-generator (deze repository)

| Trigger | Workflow | Deploy naar | App naam |
|---------|----------|-------------|----------|
| Push naar `development` | `development_idocx-document-staging.yml` | Staging | `idocx-document-staging` |
| Push naar `main` | `deploy-mediation.yml` | Productie | `mediation-document-generator` |

### idocx-api (backend)

| Trigger | Workflow | Deploy naar | App naam |
|---------|----------|-------------|----------|
| Push naar `development` | `development_idocx-staging.yml` | Staging | `idocx-staging` |
| Push naar `main` | `main_idocx-api.yml` | Productie | `ouderschaps-api-fvgbfwachxabawgs` |

### idocx-web (frontend)

| Trigger | Workflow | Deploy naar | App naam |
|---------|----------|-------------|----------|
| Push naar `development` | `azure-static-web-apps-jolly-glacier-0e4a15003.yml` | Staging | `jolly-glacier-0e4a15003` |
| Push naar `main` | `azure-static-web-apps-agreeable-grass-0622e6803.yml` | Productie | `Ouderschaps-web` |

**Belangrijk**: Na een push naar `development` of `main` wordt automatisch gedeployed. Handmatige deployment is alleen nodig als de GitHub Actions workflow faalt.

### Azure Hostnamen

Azure gebruikt een nieuw URL-formaat met unieke suffix. Dit zijn de actuele hostnamen:

| Service | Omgeving | Hostnaam |
|---------|----------|----------|
| **idocx-api** | Staging | `idocx-staging-fwfnghfua7dwcsdd.westeurope-01.azurewebsites.net` |
| **idocx-api** | Productie | `ouderschaps-api-fvgbfwachxabawgs.westeurope-01.azurewebsites.net` |
| **idocx-document-gen** | Staging | `idocx-document-staging-fqdtbhhmb0fka6g8.westeurope-01.azurewebsites.net` |
| **idocx-document-gen** | Productie | `mediation-document-generator.azurewebsites.net` |
| **idocx-web** | Staging | `jolly-glacier-0e4a15003.6.azurestaticapps.net` |
| **idocx-web** | Productie | `app.idocx.nl` |

## Git workflow

- **main** - Productie branch
- **development** - Development/staging branch

**NOOIT direct op `main` committen of pushen.** Alle wijzigingen gaan via de `development` branch. Commit en push altijd naar `development`. Pas na review/goedkeuring wordt `development` naar `main` gemerged.

### Git Commit & Push Policy

- **NOOIT** uit eigen beweging committen of pushen zonder expliciete toestemming van de gebruiker.
- **ALTIJD** eerst toestemming vragen via AskUserQuestion voordat je commit en/of pusht: "Mag ik de wijzigingen committen en pushen naar development?" met opties Ja/Nee.
- Pas na bevestiging uitvoeren.

### Git Merge Policy

- **NOOIT** uit eigen beweging een merge uitvoeren (bijv. `git merge`, `git rebase` naar een andere branch, of een PR mergen).
- Een merge mag **alleen** plaatsvinden als de gebruiker hier een **directe, expliciete opdracht** toe geeft.
- **ALTIJD** eerst toestemming vragen via AskUserQuestion voordat je een merge uitvoert, ook als de gebruiker zelf om de merge vraagt. Vraag: "Weet u zeker dat u [branch] naar [branch] wilt mergen? Dit kan niet ongedaan worden gemaakt." met opties Ja/Nee.
- **NOOIT** een merge uitvoeren zonder expliciete bevestiging via AskUserQuestion. Geen uitzonderingen.
- Pas na bevestiging uitvoeren.

## Databases & Migraties

Database migraties worden beheerd in de `idocx-api` repository (`/home/hans/idocx-api/migrations/`).

Beide databases draaien op dezelfde Azure SQL server: `sql-ouderschapsplan-server.database.windows.net`

| Omgeving | Database | Branch |
|----------|----------|--------|
| **Staging** | `db-ouderschapsplan-staging` | `development` |
| **Productie** | `db-ouderschapsplan` | `main` |

### Migratie Workflow
1. Nieuwe migratie aanmaken in `idocx-api/migrations/` op `development` branch
2. Uitvoeren op **staging** database
3. Testen op staging omgeving
4. Na goedkeuring: merge `development` naar `main`
5. Uitvoeren op **productie** database

### Claude Code instructies voor migraties
- **ALTIJD** bevestiging vragen voordat een migratie wordt uitgevoerd
- **ALTIJD** duidelijk vermelden op WELKE database (staging of productie)
- **NOOIT** migraties uitvoeren op productie voordat ze op staging zijn getest
- Bij een merge naar `main`: herinner de gebruiker dat de migratie OOK op productie moet worden uitgevoerd

### Backwards-compatible migraties (verplicht)
- Nieuwe kolommen: ALTIJD met `NULL` of `DEFAULT` waarde
- Geen `DROP COLUMN` zonder migratiestrategie
- Geen `NOT NULL` constraints toevoegen aan bestaande kolommen zonder default
