# Convenant Placeholders — Compleet Overzicht

Per convenant stap: alle placeholders, hun database-velden, mogelijke waarden en type.

**Legenda:**
- **Display** = Placeholder voor in Word-templates (bijv. `[[HOOGTE_PARTNERALIMENTATIE]]`)
- **Conditie** = Veld voor artikel-template condities (bijv. `partneralimentatie_betaler = "partij1"`)
- **Naam-resolved** = Wordt automatisch omgezet van "partij1"/"partij2" naar de werkelijke naam/aanduiding
- **Catalogus** = Beschikbaar als conditieveld in admin Placeholder Catalogus (als `is_conditie_veld_beschikbaar = 1`)

---

## Stap 0: Partneralimentatie

### Substap 0: Behoefteberekening & Draagkracht

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[NETTO_GEZINSINKOMEN]]` | `netto_gezinsinkomen` | Display | Bedrag (€ x.xxx,xx) |
| `[[KOSTEN_KINDEREN_PARTNERALIMENTATIE]]` | `kosten_kinderen_partneralimentatie` | Display | Bedrag |
| `[[NETTO_BEHOEFTE]]` | `netto_behoefte` | Display | Bedrag |
| `[[BRUTO_AANVULLENDE_BEHOEFTE]]` | `bruto_aanvullende_behoefte` | Display | Bedrag |
| `[[BRUTO_JAARINKOMEN_PARTIJ1]]` | `bruto_jaarinkomen_partij1` | Display | Bedrag |
| `[[DRAAGKRACHTLOOS_INKOMEN_PARTIJ1]]` | `draagkrachtloos_inkomen_partij1` | Display | Bedrag |
| `[[DRAAGKRACHT_PARTIJ1]]` | `draagkracht_partij1` | Display | Bedrag |
| `[[BRUTO_JAARINKOMEN_PARTIJ2]]` | `bruto_jaarinkomen_partij2` | Display | Bedrag |
| `[[DRAAGKRACHTLOOS_INKOMEN_PARTIJ2]]` | `draagkrachtloos_inkomen_partij2` | Display | Bedrag |
| `[[DRAAGKRACHT_PARTIJ2]]` | `draagkracht_partij2` | Display | Bedrag |
| `[[EIGEN_INKOMSTEN_BEDRAG]]` | `eigen_inkomsten_bedrag` | Display | Bedrag |
| `[[VERDIENCAPACITEIT_BEDRAG]]` | `verdiencapaciteit_bedrag` | Display | Bedrag |
| `[[DUURZAAM_GESCHEIDEN_DATUM]]` | `duurzaam_gescheiden_datum` | Display | Datum (d MMMM yyyy) |
| `[[VOORLOPIGE_ALIMENTATIE_BEDRAG]]` | `voorlopige_partneralimentatie_bedrag` | Display | Bedrag |
| `duurzaam_gescheiden` | `duurzaam_gescheiden` | Conditie | `true` / `false` |
| `alimentatie_berekening_aanhechten` | `alimentatie_berekening_aanhechten` | Conditie | `true` / `false` |
| `berekening_methode` | `berekening_methode` | Conditie | `hofnorm`, `behoeftelijst` |
| `verdiencapaciteit_type` | `verdiencapaciteit_type` | Conditie | `werkelijk`, `verdiencapaciteit`, `geen` |
| `eigen_inkomsten` | `eigen_inkomsten` | Conditie | `geen`, `bruto_jaar` |

### Substap 1: Bedrag & Afkoop

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[ALIMENTATIEPLICHTIGE]]` | afgeleid van `partneralimentatie_betaler` | Display, Naam-resolved | Naam van de betalende partij |
| `[[ALIMENTATIEGERECHTIGDE]]` | afgeleid van `partneralimentatie_betaler` | Display, Naam-resolved | Naam van de ontvangende partij |
| `[[AlimentatieplichtigePAL]]` | afgeleid van `partneralimentatie_betaler` | Display, Naam-resolved | PARTIJ_AANDUIDING van betalende partij |
| `[[AlimentatiegerechtigdePAL]]` | afgeleid van `partneralimentatie_betaler` | Display, Naam-resolved | PARTIJ_AANDUIDING van ontvangende partij |
| `[[HijZijPALplichtige]]` | geslacht alimentatieplichtige | Display | `hij` / `zij` |
| `[[HijZijPALgerechtigde]]` | geslacht alimentatiegerechtigde | Display | `hij` / `zij` |
| `[[HemHaarPALplichtige]]` | geslacht alimentatieplichtige | Display | `hem` / `haar` |
| `[[HemHaarPALgerechtigde]]` | geslacht alimentatiegerechtigde | Display | `hem` / `haar` |
| `[[ZijnHaarPALplichtige]]` | geslacht alimentatieplichtige | Display | `zijn` / `haar` |
| `[[ZijnHaarPALgerechtigde]]` | geslacht alimentatiegerechtigde | Display | `zijn` / `haar` |
| `[[zijnofhaarPALgerechtigde]]` | geslacht alimentatiegerechtigde | Display | `zijn` / `haar` (alias) |
| `[[HOOGTE_PARTNERALIMENTATIE]]` | `hoogte_partneralimentatie` | Display | Bedrag |
| `[[PARTNERALIMENTATIE_INGANGSDATUM]]` | `partneralimentatie_ingangsdatum` | Display | Datum |
| `[[AFKOOP_BEDRAG]]` | `afkoop_bedrag` | Display | Bedrag |
| `[[BIJDRAGE_HYPOTHEEKRENTE_BEDRAG]]` | `bijdrage_hypotheekrente_bedrag` | Display | Bedrag |
| `[[BIJDRAGE_HYPOTHEEKRENTE_TOT_WANNEER]]` | `bijdrage_hypotheekrente_tot_wanneer` | Display | Label: `verkoop van de woning`, `de akte van verdeling`, `een nader bepaalde datum` |
| `[[BIJDRAGE_HYPOTHEEKRENTE_TOT_DATUM]]` | `bijdrage_hypotheekrente_tot_datum` | Display | Datum |
| `[[BIJDRAGE_HYPOTHEEKRENTE_INGANGSDATUM]]` | `bijdrage_hypotheekrente_ingangsdatum` | Display | Datum |
| `[[BIJDRAGE_HYPOTHEEKRENTE_EINDDATUM]]` | `bijdrage_hypotheekrente_tot_datum` | Display | Datum (alias) |
| `[[EINDE_BIJDRAGE_HYPOTHEEKRENTE]]` | afgeleid van `bijdrage_hypotheekrente_tot_wanneer` + datum | Display | Tekst: `de verkoop van de woning`, `de akte van verdeling`, of datum |
| `[[AFSTAND_TENZIJ_OMSTANDIGHEID]]` | `afstand_tenzij_omstandigheid` | Display | Vrije tekst |
| `partneralimentatie_betaler` | `partneralimentatie_betaler` | Conditie | `partij1`, `partij2`, `geen` |
| `PartneralimentatieBetaler` | `partneralimentatie_betaler` | Conditie | idem (PascalCase alias) |
| `partneralimentatie_van_toepassing` | afgeleid | Conditie | `true` (als betaler != geen), `false` |
| `afstand_recht` | `afstand_recht` | Conditie | `afstand`, `afstand_tenzij`, `onvoldoende_draagkracht`, `geen_behoefte`, `jusvergelijking` |
| `jusvergelijking` | `jusvergelijking` | Conditie | `true` / `false` |
| `bijdrage_hypotheekrente` | `bijdrage_hypotheekrente` | Conditie | `true` / `false` |
| `bijdrage_hypotheekrente_tot_wanneer` | `bijdrage_hypotheekrente_tot_wanneer` | Conditie | `verkoop_woning`, `akte_van_verdeling`, `tot_datum` |
| `einde_bijdrage_hypotheekrente` | `bijdrage_hypotheekrente_tot_wanneer` | Conditie | idem (alias) |
| `partneralimentatie_afkopen` | `partneralimentatie_afkopen` | Conditie | `true` / `false` |
| `afkoop_type` | `afkoop_type` | Conditie | `bruto`, `afstemming` |

### Substap 2: Termijnen & Indexering

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[INDEXERING_EERSTE_JAAR]]` | `indexering_eerste_jaar` | Display | Jaartal |
| `[[WIJZIGINGSOMSTANDIGHEDEN]]` | `wijzigingsomstandigheden` | Display | Vrije tekst |
| `[[GEEN_WIJZIGINGSOMSTANDIGHEDEN]]` | `geen_wijzigingsomstandigheden` | Display | Vrije tekst |
| `[[CONTRACTUELE_TERMIJN_JAREN]]` | `contractuele_termijn_jaren` | Display | Getal |
| `[[CONTRACTUELE_TERMIJN_INGANGSDATUM]]` | `contractuele_termijn_ingangsdatum` | Display | Datum |
| `[[BEPERKT_NIET_WIJZIGINGSBEDING]]` | `beperkt_niet_wijzigingsbeding` | Display | Vrije tekst |
| `[[BEPERKT_WIJZIGINGSBEDING]]` | `beperkt_wijzigingsbeding` | Display | Vrije tekst |
| `niet_wijzigingsbeding` | `niet_wijzigingsbeding` | Conditie | `geen`, `niet_wijzigingsbeding`, `beperkt_niet`, `beperkt`, `afkoop` |
| `indexering_type` | `indexering_type` | Conditie | `wettelijk`, `uitgesloten`, `alternatief` |
| `wettelijke_termijn` | `wettelijke_termijn` | Conditie | `standaard`, `tot_aow`, `10_jaar`, `kind_12` |
| `verlenging_termijn` | `verlenging_termijn` | Conditie | `wettelijk`, `contractueel_afwijkend`, `beperkt_wijziging`, `wijziging_mogelijk`, `verkorting_mogelijk`, `hertrouw`, `afwijkend_1160` |

### Substap 3: Afwijkingen & Eigeninkomsten

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[KORTINGSREGELING_PERCENTAGE]]` | `kortingsregeling_percentage1` | Display | Getal (%) |
| `[[KORTINGSREGELING_EERST_PERCENTAGE]]` | `kortingsregeling_percentage1` | Display | Getal (%) |
| `[[KORTINGSREGELING_DAARNA_PERCENTAGE]]` | `kortingsregeling_percentage2` | Display | Getal (%) |
| `[[INKOMENSDREMPEL_BEDRAG]]` | `inkomensdrempel_bedrag` | Display | Bedrag |
| `[[INKOMENSDREMPEL_BEDRAG_DAARNA]]` | `inkomensdrempel_bedrag_daarna` | Display | Bedrag |
| `[[VERMOGEN_GRENS_BEDRAG]]` | `vermogen_grens_bedrag` | Display | Bedrag |
| `[[VERMOGEN_FICTIEF_RENDEMENT]]` | `vermogen_fictief_rendement` | Display | Percentage |
| `[[PERIODE_DOORBETALEN_1160]]` | `periode_doorbetalen_1160` | Display | Tekst (default: "zes maanden") |
| `[[HerlevingMaanden]]` | `herleving_maanden` | Display | Getal |
| `eigen_inkomsten_regeling` | `eigen_inkomsten_regeling` | Conditie | `true` / `false` |
| `kortingsregeling` | `kortingsregeling` | Conditie | `niet_korten`, `inkomen`, `inkomen_vermogen`, `eerst_dan` |
| `inkomensdrempel` | `inkomensdrempel` | Conditie | `true` / `false` |
| `vermogen_regeling` | `vermogen_regeling` | Conditie | `meetellen`, `grens`, `niet_meetellen` |
| `afwijking_maatstaven` | `afwijking_maatstaven` | Conditie | `true` / `false` |
| `afwijking_1160` | `afwijking_1160` | Conditie | `true` / `false` |
| `hoe_afwijken_1160` | `hoe_afwijken_1160` | Conditie | `doorbetaling`, `opschorting` |
| `periode_doorbetalen_1160` | `periode_doorbetalen_1160` | Conditie | Vrije tekst |

---

## Stap 1: Woning (Echtelijke woning)

### Basis (alle woningtypes)

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[WONING_ADRES]]` | `woning_adres` | Display | Tekst |
| `[[WONING_STRAAT]]` | `woning_straat` | Display | Tekst |
| `[[WONING_HUISNUMMER]]` | `woning_huisnummer` | Display | Tekst |
| `[[WONING_POSTCODE]]` | `woning_postcode` | Display | Tekst |
| `[[WONING_PLAATS]]` | `woning_plaats` | Display | Tekst |
| `[[WONING_VOLLEDIG_ADRES]]` | samengesteld | Display | "Straat nr, postcode, plaats" |
| `woning_soort` | `woning_soort` | Conditie | `koopwoning`, `huurwoning` |
| `woning_adres_keuze` | `woning_adres_keuze` | Conditie | `partij1`, `partij2`, `anders` |
| `woning_status_vermogen` | `woning_status_vermogen` | Conditie | `gemeenschappelijk`, `uitgesloten`, `vergoeden` |

### Huurwoning

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[HuurrechtToekomtAan]]` | `huurrecht_toekomt_aan` | Display, Naam-resolved | partij1→naam, partij2→naam, beiden_verlaten→"Partijen verlaten beiden de huurwoning" |
| `[[HUURRECHT_ANDERE_DATUM]]` | `huurrecht_andere_datum` | Display | Datum |
| `[[HUUR_VERPLICHTINGEN_OVERNAME_DATUM]]` | `huur_verplichtingen_overname_datum` | Display | Datum |
| `[[HuurBorgAan]]` | `huur_borg_aan` | Display, Naam-resolved | partij1→naam, partij2→naam |
| `huurrecht_toekomt_aan` | `huurrecht_toekomt_aan` | Conditie | `partij1`, `partij2`, `beiden_verlaten` |
| `huurrecht_wanneer` | `huurrecht_wanneer` | Conditie | `na_inschrijving`, `andere_datum` |
| `huur_verzoek_rechter` | `huur_verzoek_rechter` | Conditie | `true` / `false` |
| `huur_verplichtingen_voldaan` | `huur_verplichtingen_voldaan` | Conditie | `true` / `false` |
| `huur_borg_toedelen` | `huur_borg_toedelen` | Conditie | `true` / `false` |
| `huur_borg_aan` | `huur_borg_aan` | Conditie | `partij1`, `partij2`, `ieder_helft` |

### Koopwoning — Basisgegevens & Toedeling

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[WONING_TOEGEDEELD_AAN]]` | `koop_toedeling` | Display, Naam-resolved | partij1→naam, partij2→naam, of raw waarde |
| `[[WONING_WOZ_WAARDE]]` | `koop_woz_waarde` | Display | Bedrag |
| `[[WONING_TOEDELING_WAARDE]]` | `koop_toedeling_waarde` | Display | Bedrag |
| `[[WONING_LAATPRIJS]]` | `koop_laatprijs` | Display | Bedrag |
| `[[WONING_OVERBEDELING]]` | `koop_overbedeling_woning` | Display | Bedrag |
| `[[WONING_OVERBEDELING_SPAARPRODUCTEN]]` | `koop_overbedeling_spaarproducten` | Display | Bedrag |
| `[[MAKELAAR_VERKOOP]]` | `koop_makelaar_verkoop` | Display | Tekst |
| `[[ONTSLAG_HOOFDELIJKHEID_DATUM]]` | `koop_ontslag_hoofdelijkheid_datum` | Display | Datum |
| `koop_toedeling` | `koop_toedeling` | Conditie | `verkoop`, `partij1`, `partij2`, `onverdeeld` |
| `koop_nieuwbouw` | `koop_nieuwbouw` | Conditie | `true` / `false` |
| `koop_ontslag_hoofdelijkheid` | `koop_ontslag_hoofdelijkheid` | Conditie | `true` / `false` |
| `koop_volmacht_notaris` | `koop_volmacht_notaris` | Conditie | `true` / `false` |
| `koop_medewerking_leveren` | `koop_medewerking_leveren` | Conditie | `true` / `false` |

### Koopwoning — Kadaster & Notaris

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[KADASTRAAL_GEMEENTE]]` | `koop_kadastraal_gemeente` | Display | Tekst |
| `[[KADASTRAAL_SECTIE]]` | `koop_kadastraal_sectie` | Display | Tekst |
| `[[KADASTRAAL_PERCEEL]]` | `koop_kadastraal_perceel` | Display | Tekst |
| `[[KADASTRAAL_ARE]]` | `koop_kadastraal_are` | Display | Getal |
| `[[KADASTRAAL_CENTIARE]]` | `koop_kadastraal_centiare` | Display | Getal |
| `[[KADASTRAAL_AANDUIDING]]` | `koop_kadastraal_aanduiding` | Display | Tekst |
| `[[KADASTRAAL_OPPERVLAKTE]]` | `koop_kadastraal_oppervlakte` | Display | Getal |
| `[[KADASTRAAL_VOLLEDIGE_NOTATIE]]` | samengesteld | Display | "gemeente X, sectie Y, nummer Z, groot N are en M centiare" |
| `[[NOTARIS_MR]]` | `koop_notaris_mr` | Display | Tekst |
| `[[NOTARIS_STANDPLAATS]]` | `koop_notaris_standplaats` | Display | Tekst |
| `[[NOTARIS_LEVERING_DATUM]]` | `koop_notaris_levering_datum` | Display | Datum |
| `[[HYPOTHEEK_NOTARIS_MR]]` | `koop_notaris_hypotheek_mr` (of fallback `koop_notaris_mr`) | Display | Tekst |
| `[[HYPOTHEEK_NOTARIS_STANDPLAATS]]` | `koop_notaris_hypotheek_standplaats` (of fallback) | Display | Tekst |
| `[[HYPOTHEEK_NOTARIS_DATUM]]` | `koop_notaris_hypotheek_datum` (of fallback) | Display | Datum |
| `koop_kadastraal_vermelden` | `koop_kadastraal_vermelden` | Conditie | `true` / `false` |
| `koop_notaris_vermelden` | `koop_notaris_vermelden` | Conditie, Catalogus | `true` / `false` |
| `koop_notaris_hypotheek_zelfde` | `koop_notaris_hypotheek_zelfde` | Conditie | `true` / `false` |

### Koopwoning — Privevermogen

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[PRIVEVERMOGEN_VORDERING_BEDRAG]]` | `koop_privevermogen_vordering_bedrag` | Display | Bedrag |
| `[[PRIVEVERMOGEN_REDEN]]` | `koop_privevermogen_reden` | Display | Vrije tekst |
| `koop_privevermogen_investering` | `koop_privevermogen_investering` | Conditie | `true` / `false` |
| `koop_investering_na_2012` | `koop_investering_na_2012` | Conditie | `true` / `false` |
| `koop_privevermogen_hoe` | `koop_privevermogen_hoe` | Conditie | `inleg_aanschaf`, `tussentijdse_aflossing`, `verbouwing` |
| `koop_privevermogen_vordering` | `koop_privevermogen_vordering` | Conditie | `partij1_op_partij2`, `partij2_op_partij1`, `beiden` |

### Koopwoning — Lasten

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `koop_lasten_woning` | `koop_lasten_woning` | Conditie | `true` / `false` |
| `koop_hypotheekrente` | `koop_hypotheekrente` | Conditie | `partij1`, `partij2`, `ieder_helft`, `anders` |
| `koop_maandelijkse_aflossing` | `koop_maandelijkse_aflossing` | Conditie | `partij1`, `partij2`, `ieder_helft`, `anders` |
| `koop_premie_inleg` | `koop_premie_inleg` | Conditie | `partij1`, `partij2`, `ieder_helft`, `anders` |
| `koop_aanslag_woz` | `koop_aanslag_woz` | Conditie | `partij1`, `partij2`, `ieder_helft`, `anders` |
| `koop_gebruikerslasten` | `koop_gebruikerslasten` | Conditie | `partij1`, `partij2`, `ieder_helft`, `anders` |
| `koop_klein_onderhoud` | `koop_klein_onderhoud` | Conditie | `partij1`, `partij2`, `ieder_helft`, `anders` |
| `koop_groot_onderhoud` | `koop_groot_onderhoud` | Conditie | `partij1`, `partij2`, `ieder_helft`, `anders` |

---

## Stap 2: Vermogensverdeling

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[INBOEDEL]]` | `inboedel` | Display | Vrije tekst |
| `[[VERMOGENSVERDELING_OPMERKINGEN]]` | `vermogensverdeling_opmerkingen` | Display | Vrije tekst |
| `[[INBOEDEL_VERDELING_BEDRAG]]` | `inboedel_verdeling_bedrag` | Display | Bedrag |
| `[[INBOEDEL_SIERADEN_BEDRAG]]` | `inboedel_sieraden_bedrag` | Display | Bedrag |
| `[[INBOEDEL_LEVERING_DATUM]]` | `inboedel_levering_datum` | Display | Datum |
| `inboedel_status` | `inboedel_status` | Conditie | `gemeenschappelijk`, `uitgesloten`, `verrekenen` |
| `inboedel_verdeling` | `inboedel_verdeling` | Conditie | `gesloten_beurzen`, `partij1_vergoedt`, `partij2_vergoedt` |
| `inboedel_overzicht` | `inboedel_overzicht` | Conditie | `true` / `false` |
| `inboedel_bijlage_aanhechten` | `inboedel_bijlage_aanhechten` | Conditie | `true` / `false` |
| `inboedel_sieraden` | `inboedel_sieraden` | Conditie | `eigen_bezit`, `partij1_betaalt_partij2`, `partij2_betaalt_partij1` |
| `inboedel_levering` | `inboedel_levering` | Conditie | `datum`, `inschrijving_beschikking`, `feitelijk_bezit` |

---

## Stap 3: Pensioen

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[PENSIOEN_OPMERKINGEN]]` | `pensioen_opmerkingen` | Display | Vrije tekst |
| `[[BIJZONDER_PARTNERPENSIOEN]]` | `bijzonder_partnerpensioen` | Display | Raw waarde |
| `[[BIJZONDER_PARTNERPENSIOEN_BEDRAG]]` | `bijzonder_partnerpensioenbedrag` | Display | Tekst |
| `bijzonder_partnerpensioen` | `bijzonder_partnerpensioen` | Conditie | `geen`, `omzetten`, `afzien`, `gedeeltelijk` |

> **Let op:** Individuele pensioenen worden als JSON-array opgeslagen (`pensioenen` kolom). Per pensioen: `verdeling` = `verevenen`, `conversie`, `afzien`, `afkopen`, `verrekenen`, `anders`. Per pensioen: `bijzonderPartnerpensioen` = `standaard`, `afstand`, `geen`, `afwijken`.

---

## Stap 4: Fiscaal

| Placeholder | DB-veld | Type | Opties / Formaat |
|---|---|---|---|
| `[[FiscaleToetsingTekst]]` | afgeleid van `fiscaal_advies_keuze` | Display | Gegenereerde tekst |
| `[[FiscaalPartnerschapTekst]]` | afgeleid van `fiscaal_partnerschap_keuze` | Display | Gegenereerde tekst |
| `[[EigenWoningTekst]]` | afgeleid (als `eigen_woning_sectie_opnemen = true`) | Display | Gegenereerde tekst |
| `[[IbOndernemingTekst]]` | afgeleid (als `ib_onderneming_sectie_opnemen = true`) | Display | Gegenereerde tekst |
| `[[AanmerkelijkBelangTekst]]` | afgeleid (als `aanmerkelijk_belang_opnemen = true`) | Display | Gegenereerde tekst |
| `[[TerbeschikkingstellingTekst]]` | afgeleid (als `terbeschikkingstelling_opnemen = true`) | Display | Gegenereerde tekst |
| `[[SchenkbelastingTekst]]` | afgeleid (als `schenkbelasting_opnemen = true`) | Display | Gegenereerde tekst |
| `[[DraagplichtHeffingenTekst]]` | afgeleid van `draagplicht_heffingen_tot` + `draagplicht_heffingen_jaar` | Display | Gegenereerde tekst |
| `[[VerrekeningLijfrentenTekst]]` | afgeleid (als opnemen = true) | Display | Gegenereerde tekst |
| `[[AfkoopVerrekeningTekst]]` | afgeleid (als opnemen = true) | Display | Gegenereerde tekst |
| `[[OptimalisatieAangiftenTekst]]` | afgeleid (als opnemen = true) | Display | Gegenereerde tekst |
| `[[OverigeFiscaleBepalingenTekst]]` | altijd gegenereerd | Display | Gegenereerde tekst |
| `fiscaal_advies_keuze` | `convenant_fiscaal.fiscaal_advies_keuze` | Conditie | `door_adviseur`, `buiten_mediation`, `geen_advies` |
| `fiscaal_partnerschap_keuze` | `convenant_fiscaal.fiscaal_partnerschap_keuze` | Conditie | `zelfstandig`, `onderling_overleg` |
| `eigen_woning_einddatum_bewust` | `convenant_fiscaal.eigen_woning_einddatum_bewust` | Conditie | `true` / `false` |
| `eigen_woning_sectie_opnemen` | `convenant_fiscaal.eigen_woning_sectie_opnemen` | Conditie | `true` / `false` |
| `ib_onderneming_sectie_opnemen` | `convenant_fiscaal.ib_onderneming_sectie_opnemen` | Conditie | `true` / `false` |
| `aanmerkelijk_belang_opnemen` | `convenant_fiscaal.aanmerkelijk_belang_opnemen` | Conditie | `true` / `false` |
| `aanmerkelijk_belang_van_toepassing` | `convenant_fiscaal.aanmerkelijk_belang_van_toepassing` | Conditie | `wel`, `niet` |
| `aanmerkelijk_belang_afrekening` | `convenant_fiscaal.aanmerkelijk_belang_afrekening` | Conditie | `wel`, `geen` |
| `terbeschikkingstelling_opnemen` | `convenant_fiscaal.terbeschikkingstelling_opnemen` | Conditie | `true` / `false` |
| `terbeschikkingstelling_keuze` | `convenant_fiscaal.terbeschikkingstelling_keuze` | Conditie | `directe_verschuldigdheid`, `uitgesteld` |
| `schenkbelasting_opnemen` | `convenant_fiscaal.schenkbelasting_opnemen` | Conditie | `true` / `false` |
| `draagplicht_heffingen_tot` | `convenant_fiscaal.draagplicht_heffingen_tot` | Conditie | `partij1`, `partij2`, `gelijkelijk`, `verhouding` |
| `draagplicht_heffingen_jaar` | `convenant_fiscaal.draagplicht_heffingen_jaar` | Conditie | Jaartal |
| `verrekening_lijfrenten_pensioen_opnemen` | `convenant_fiscaal.verrekening_lijfrenten_pensioen_opnemen` | Conditie | `true` / `false` |
| `afkoop_alimentatie_verrekening_opnemen` | `convenant_fiscaal.afkoop_alimentatie_verrekening_opnemen` | Conditie | `true` / `false` |
| `optimalisatie_aangiften_opnemen` | `convenant_fiscaal.optimalisatie_aangiften_opnemen` | Conditie | `true` / `false` |
| `optimalisatie_voordeel_verdeling` | `convenant_fiscaal.optimalisatie_voordeel_verdeling` | Conditie | `gelijk`, `partij1`, `partij2` |
| `hypotheekrente_aftrek` | `convenant_info.hypotheekrente_aftrek` | Conditie | `partij1`, `partij2`, `beiden` |

---

## Stap 5: Kwijting & Huwelijksgoederenregime

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[HUWELIJKSGOEDERENREGIME]]` | afgeleid van `soort_relatie` + `overeenkomst_gemaakt` + `datum_aanvang_relatie` | Display | Raw waarde (bijv. `huwelijksvoorwaarden`) |
| `[[HUWELIJKSGOEDERENREGIME_OMSCHRIJVING]]` | afgeleid | Display | Nederlandse tekst (bijv. "huwelijkse voorwaarden") |
| `[[HUWELIJKSGOEDERENREGIME_UITZONDERING]]` | `huwelijksgoederenregime_uitzondering` | Display | Vrije tekst |
| `[[HUWELIJKSGOEDERENREGIME_ANDERS]]` | `huwelijksgoederenregime_anders` | Display | Vrije tekst |
| `[[HUWELIJKSVOORWAARDEN_DATUM]]` | `huwelijksvoorwaarden_datum` | Display | Datum |
| `[[HUWELIJKSVOORWAARDEN_NOTARIS]]` | `huwelijksvoorwaarden_notaris` | Display | Tekst |
| `[[HUWELIJKSVOORWAARDEN_PLAATS]]` | `huwelijksvoorwaarden_notaris_plaats` | Display | Tekst |
| `[[SLOTBEPALINGEN]]` | `slotbepalingen` | Display | Vrije tekst |
| `huwelijksgoederenregime` | afgeleid | Conditie | `algehele_gemeenschap_voor_2018`, `beperkte_gemeenschap_na_2018`, `huwelijksvoorwaarden`, `partnerschapsvoorwaarden`, `samenlevingsovereenkomst`, `geen_overeenkomst` |
| `kwijting_akkoord` | `kwijting_akkoord` | Conditie | `true` / `false` |

> **Huwelijksgoederenregime** wordt automatisch afgeleid:
> - Gehuwd + voorwaarden → `huwelijksvoorwaarden`
> - Gehuwd + datum < 2018 → `algehele_gemeenschap_voor_2018`
> - Gehuwd + datum >= 2018 → `beperkte_gemeenschap_na_2018`
> - Geregistreerd partnerschap volgt zelfde logica
> - Samenwonend + overeenkomst → `samenlevingsovereenkomst`
> - Samenwonend zonder → `geen_overeenkomst`

---

## Stap 6: Considerans & Ondertekening

### Considerans

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[HUWELIJKSDATUM]]` | `huwelijksdatum` | Display | Datum |
| `[[HUWELIJKSPLAATS]]` | `huwelijksplaats` | Display | Tekst |
| `[[MEDIATOR_NAAM]]` | `mediator_naam` | Display | Tekst |
| `[[MEDIATOR_PLAATS]]` | `mediator_plaats` | Display | Tekst |
| `[[RECHTBANK]]` | `rechtbank` | Display | Tekst |
| `[[RECHTBANK_LOCATIE]]` | `rechtbank_locatie` | Display | Tekst |
| `[[ADVOCAAT_PARTIJ1]]` | `advocaat_partij1` | Display | Tekst |
| `[[ADVOCAAT_PARTIJ2]]` | `advocaat_partij2` | Display | Tekst |
| `[[SPAARREKENING_KINDEREN_NUMMERS]]` | `spaarrekening_kinderen_nummers` | Display | Tekst |
| `[[ERKENNINGSDATUM]]` | `erkenningsdatum` | Display | Datum |
| `[[MINDERJARIGE_KINDEREN_NAMEN]]` | afgeleid van kinderen | Display | "Emma en Luuk" |
| `[[MINDERJARIGE_KINDEREN_ZIJN_IS]]` | afgeleid van aantal | Display | `zijn` / `is` |
| `is_mediation` | `is_mediation` | Conditie | `true` / `false` |
| `heeft_vaststellingsovereenkomst` | `heeft_vaststellingsovereenkomst` | Conditie | `true` / `false` |
| `heeft_kinderen_uit_huwelijk` | `heeft_kinderen_uit_huwelijk` | Conditie | `true` / `false` |
| `heeft_kinderen_voor_huwelijk` | `heeft_kinderen_voor_huwelijk` | Conditie | `true` / `false` |
| `heeft_spaarrekeningen_kinderen` | `heeft_spaarrekeningen_kinderen` | Conditie | `true` / `false` |
| `heeft_kinderen` | afgeleid | Conditie | `true` / `false` |

### Ondertekening

| Placeholder | DB-veld (convenant_info) | Type | Opties / Formaat |
|---|---|---|---|
| `[[ONDERTEKEN_PLAATS_PARTIJ1]]` | `ondertekening_plaats_partij1` | Display | Tekst |
| `[[ONDERTEKEN_PLAATS_PARTIJ2]]` | `ondertekening_plaats_partij2` | Display | Tekst |
| `[[ONDERTEKEN_DATUM_PARTIJ1]]` | `ondertekening_datum_partij1` | Display | Datum |
| `[[ONDERTEKEN_DATUM_PARTIJ2]]` | `ondertekening_datum_partij2` | Display | Datum |

---

## Computed Fields (BuildEvaluationContext)

Deze velden worden automatisch berekend en zijn beschikbaar voor condities:

| Veld | Bron | Type | Waarde |
|---|---|---|---|
| `AantalKinderen` | `data.Kinderen.Count` | Getal | Totaal aantal kinderen |
| `AantalMinderjarigeKinderen` | berekend op geboortedatum | Getal | Aantal < 18 jaar |
| `HeeftKinderen` | `data.Kinderen.Count > 0` | Boolean | `true` / `false` |
| `HeeftAlimentatie` | `data.Alimentatie != null` | Boolean | `true` / `false` |
| `HeeftKinderrekening` | `alimentatie.IsKinderrekeningBetaalwijze` | Boolean | `true` / `false` |
| `IsCoOuderschap` | `gezagPartij == 1` | Boolean | `true` / `false` |
| `HeeftGezamenlijkGezag` | `gezagPartij == 1` | Boolean | `true` / `false` |
| `IsAnoniem` | `data.IsAnoniem` | Boolean | `true` / `false` |
| `DossierStatus` | `data.Status` | Tekst | Dossierstatus |
| `Partij1_Geslacht` | `partij1.Geslacht` | Tekst | `man`, `vrouw` |
| `Partij1_IsMan` | afgeleid | Boolean | `true` / `false` |
| `Partij1_IsVrouw` | afgeleid | Boolean | `true` / `false` |
| `Partij2_Geslacht` | `partij2.Geslacht` | Tekst | `man`, `vrouw` |
| `Partij2_IsMan` | afgeleid | Boolean | `true` / `false` |
| `Partij2_IsVrouw` | afgeleid | Boolean | `true` / `false` |

---

## Naam-resolved placeholders

Deze placeholders resolven automatisch `partij1`/`partij2` naar de werkelijke naam:

| Placeholder | Raw waarde → Resolved |
|---|---|
| `[[HuurrechtToekomtAan]]` | `partij1` → PARTIJ1_AANDUIDING, `partij2` → PARTIJ2_AANDUIDING, `beiden_verlaten` → "Partijen verlaten beiden de huurwoning" |
| `[[HuurBorgAan]]` | `partij1` → PARTIJ1_AANDUIDING, `partij2` → PARTIJ2_AANDUIDING |
| `[[WONING_TOEGEDEELD_AAN]]` | `partij1` → PARTIJ1_AANDUIDING, `partij2` → PARTIJ2_AANDUIDING |
| `[[AlimentatieplichtigePAL]]` | PARTIJ_AANDUIDING van de betalende partij |
| `[[AlimentatiegerechtigdePAL]]` | PARTIJ_AANDUIDING van de ontvangende partij |

> **Belangrijk:** Bij condities op naam-resolved velden gebruik je de **raw** waarde (`partij1`, `partij2`), niet de resolved naam. Gebruik hiervoor het **snake_case** conditieveld (bijv. `huur_borg_aan`), niet het PascalCase display-veld (`HuurBorgAan`).
