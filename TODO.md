# TODO - idocx-document-generator

## Recent afgerond (jan-feb 2026)

### Convenant document generatie

- [x] Convenant document generation endpoint
- [x] GET /api/placeholders endpoint
- [x] Convenant placeholders laden vanuit database

### Artikel rendering

- [x] Nummering type support (nieuw_nummer/doornummeren/geen_nummer)
- [x] ConditieConfig support (AND/OR conditionele zichtbaarheid)
- [x] Opsommingen (bullet/genummerd) in artikel templates
- [x] Sub-bullets en ingesprongen tekst
- [x] Inline opmaak (bold/underline)
- [x] Paragraph alignment ({links}, {rechts}, {centreren}, {uitvullen})
- [x] Uppercase article titles voor hoofdartikelen
- [x] Artikelen zonder titel (geen heading gerenderd)
- [x] Placeholder modifiers (caps, upper, lower)

### Loop mechanisme

- [x] LoopSectionProcessor voor loop-syntax in artikel templates
- [x] Generiek loop-mechanisme voor JSON-collecties
- [x] Grammatica regels (enkelvoud/meervoud) op basis van collectie-aantallen
- [x] Bankrekeningkinderen grammar keys met prefix scoping

### Placeholders

- [x] KINDEREN_OPSOMMING (Word bullets, opsommingstekens, 'hierna te noemen')
- [x] MINDERJARIGE_KINDEREN_ZIN, DIT_KIND_DEZE_KINDEREN
- [x] OUDERLIJK_GEZAG_CONSIDERANS_ZIN
- [x] SoortProcedure + snake_case→PascalCase naming bridge voor conditievelden
- [x] Partij1Geslacht / Partij2Geslacht
- [x] PartneralimentatieBetaler
- [x] AlimentatieplichtigePAL / AlimentatiegerechtigdePAL
- [x] VOORLOPIGE_ALIMENTATIE_BEDRAG (verwijst naar nieuw convenant_info veld)
- [x] Bijdrage hypotheekrente tot wanneer
- [x] "alle" grammar placeholders voor totaal kinderaantal (minderjarig + meerderjarig)
- [x] Veld-met-veld vergelijking in condities

### Inhoudsopgave (TOC)

- [x] Server-side TOC populatie (geen Word update fields dialog)
- [x] Auto-insert TOC voor templates zonder `[[INHOUDSOPGAVE]]`
- [x] TOC layout matching Word native toc 1 format
- [x] Automatische TOC update bij openen in Word

## Backlog

| Prioriteit | Item | Notities |
| --- | --- | --- |
| 💡 Laag | Ouderschapsplan document generatie | Hergebruik convenant-architectuur |
| 💡 Laag | PDF export naast DOCX | Via LibreOffice of andere conversie |

## Technische schuld

- [ ] Uitbreiden unit tests voor LoopSectionProcessor edge cases
- [ ] Performance optimalisatie bij grote documenten met veel loops
