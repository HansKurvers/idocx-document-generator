# Convenant Template Instructies

## Template Bestand
Maak een nieuw Word document `Convenant.docx` gebaseerd op de `Modelovereenkomst.docx`.

## Template Structuur

De template moet de volgende structuur hebben:

```
ECHTSCHEIDINGSCONVENANT

[[ARTIKELEN]]

Ondertekening

Aldus overeengekomen en in tweevoud ondertekend:


[[ONDERTEKEN_PLAATS_PARTIJ1]], [[ONDERTEKEN_DATUM_PARTIJ1]]


___________________________
[[PARTIJ1_VOORNAMEN]] [[PARTIJ1_ACHTERNAAM]]


[[ONDERTEKEN_PLAATS_PARTIJ2]], [[ONDERTEKEN_DATUM_PARTIJ2]]


___________________________
[[PARTIJ2_VOORNAMEN]] [[PARTIJ2_ACHTERNAAM]]
```

## Belangrijk: [[ARTIKELEN]] Placeholder

De `[[ARTIKELEN]]` placeholder wordt automatisch vervangen door alle artikel templates uit de database die voldoen aan de conditievelden van het dossier.

Deze artikelen worden:
1. Gefilterd op document_type = 'convenant'
2. Gesorteerd op volgorde
3. Gefilterd op condities (bijv. heeft_kinderen, partneralimentatie_van_toepassing, etc.)
4. Genummerd met [[ARTIKEL]] placeholders

## Placeholders die beschikbaar zijn

### Partij Placeholders
- [[PARTIJ1_VOORNAMEN]], [[PARTIJ1_ACHTERNAAM]], [[PARTIJ1_GEBOORTEDATUM]], [[PARTIJ1_GEBOORTEPLAATS]]
- [[PARTIJ1_ADRES]], [[PARTIJ1_WOONPLAATS]]
- [[PARTIJ1_AANDUIDING]] - "de vader" of "de man" (afhankelijk van IsAnoniem)
- Zelfde voor PARTIJ2_*

### Huwelijk Placeholders
- [[HUWELIJKSDATUM]], [[HUWELIJKSPLAATS]]
- [[HUWELIJKSVOORWAARDEN_DATUM]], [[HUWELIJKSVOORWAARDEN_NOTARIS]], [[HUWELIJKSVOORWAARDEN_PLAATS]]

### Partneralimentatie Placeholders
- [[ALIMENTATIEPLICHTIGE]], [[ALIMENTATIEGERECHTIGDE]]
- [[HOOGTE_PARTNERALIMENTATIE]], [[PARTNERALIMENTATIE_INGANGSDATUM]]
- [[AFKOOP_BEDRAG]]
- [[NETTO_BEHOEFTE]], [[BRUTO_AANVULLENDE_BEHOEFTE]]
- [[DRAAGKRACHT_PARTIJ1]], [[DRAAGKRACHT_PARTIJ2]]

### Woning Placeholders
- [[WONING_ADRES]], [[WONING_STRAAT]], [[WONING_HUISNUMMER]], [[WONING_POSTCODE]], [[WONING_PLAATS]]
- [[WONING_VOLLEDIG_ADRES]]
- [[WONING_WOZ_WAARDE]], [[WONING_TOEDELING_WAARDE]]
- [[WONING_TOEGEDEELD_AAN]]
- [[KADASTRAAL_VOLLEDIGE_NOTATIE]]

### Notaris Placeholders
- [[NOTARIS_MR]], [[NOTARIS_STANDPLAATS]], [[NOTARIS_LEVERING_DATUM]]

### Mediation Placeholders
- [[MEDIATOR_NAAM]], [[MEDIATOR_PLAATS]], [[RECHTBANK]]
- [[ADVOCAAT_PARTIJ1]], [[ADVOCAAT_PARTIJ2]]

### Ondertekening Placeholders
- [[ONDERTEKEN_PLAATS_PARTIJ1]], [[ONDERTEKEN_DATUM_PARTIJ1]]
- [[ONDERTEKEN_PLAATS_PARTIJ2]], [[ONDERTEKEN_DATUM_PARTIJ2]]

## Conditionele Secties

In de artikel templates kunnen conditionele secties worden gebruikt:

```
[[IF:heeft_kinderen]]
Tekst die alleen getoond wordt als er kinderen zijn
[[ENDIF:heeft_kinderen]]

[[IF:!partneralimentatie_van_toepassing]]
Tekst die getoond wordt als er GEEN partneralimentatie is
[[ENDIF:partneralimentatie_van_toepassing]]
```

## Azure Blob Storage

Upload de template naar Azure Blob Storage en configureer de environment variable:
```
TemplateStorageUrlConvenant = https://[storage].blob.core.windows.net/templates/Convenant.docx?[sas]
```

## API Endpoint

Het convenant wordt gegenereerd via:
```
POST /api/convenant
{
  "DossierId": 123
}
```
