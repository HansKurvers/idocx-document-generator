using System;
using System.Collections.Generic;

namespace scheidingsdesk_document_generator.Models
{
    /// <summary>
    /// Complete convenant info data including partneralimentatie, woning, vermogensverdeling, and pensioen
    /// Retrieved from dbo.convenant_info table
    /// </summary>
    public class ConvenantInfoData
    {
        // =====================================================
        // PARTNERALIMENTATIE
        // =====================================================

        // Substap 0: Behoefteberekening & Draagkracht
        public bool? DuurzaamGescheiden { get; set; }
        public DateTime? DuurzaamGescheidenDatum { get; set; }
        public decimal? VoorlopigePartneralimentatieBedrag { get; set; }
        public bool? AlimentatieBerekeningAanhechten { get; set; }
        public string? BerekeningMethode { get; set; } // hofnorm, behoeftelijst
        public decimal? NettoGezinsinkomen { get; set; }
        public decimal? KostenKinderenPartneralimentatie { get; set; }
        public decimal? NettoBehoefte { get; set; }
        public decimal? BrutoAanvullendeBehoefte { get; set; }
        public decimal? BrutoJaarinkomenPartij1 { get; set; }
        public decimal? DraagkrachtloosInkomenPartij1 { get; set; }
        public decimal? DraagkrachtPartij1 { get; set; }
        public decimal? BrutoJaarinkomenPartij2 { get; set; }
        public decimal? DraagkrachtloosInkomenPartij2 { get; set; }
        public decimal? DraagkrachtPartij2 { get; set; }
        public string? VerdiencapaciteitType { get; set; } // werkelijk, verdiencapaciteit, geen
        public decimal? EigenInkomstenBedrag { get; set; }
        public decimal? VerdiencapaciteitBedrag { get; set; }

        // Substap 1: Bedrag & Afkoop
        public string? PartneralimentatieBetaler { get; set; } // partij1, partij2, geen
        public decimal? HoogtePartneralimentatie { get; set; }
        public DateTime? PartneralimentatieIngangsdatum { get; set; }
        public string? AfstandRecht { get; set; } // afstand, afstand_tenzij, geen_draagkracht, geen_behoefte, volledig, tenzij
        public string? AfstandTenzijOmstandigheid { get; set; }
        public bool? Jusvergelijking { get; set; }
        public bool? BijdrageHypotheekrente { get; set; }
        public decimal? BijdrageHypotheekrenteBedrag { get; set; }
        public string? BijdrageHypotheekrenteTotWanneer { get; set; }
        public DateTime? BijdrageHypotheekrenteTotDatum { get; set; }
        public DateTime? BijdrageHypotheekrenteIngangsdatum { get; set; }
        public bool? PartneralimentatieAfkopen { get; set; }
        public string? AfkoopType { get; set; } // bruto, afstemming
        public decimal? AfkoopBedrag { get; set; }

        // Substap 2: Termijnen & Indexering
        public string? NietWijzigingsbeding { get; set; } // volledig, beperkt_niet, beperkt_wijziging, afkoop
        public string? Wijzigingsomstandigheden { get; set; }
        public string? GeenWijzigingsomstandigheden { get; set; }
        public string? IndexeringType { get; set; } // wettelijk, uitgesloten, alternatief
        public int? IndexeringEersteJaar { get; set; }
        public string? WettelijkeTermijn { get; set; } // standaard, tot_aow, 10_jaar, kind_12
        public string? VerlengingTermijn { get; set; } // wettelijk, contractueel_afwijkend
        public int? ContractueleTermijnJaren { get; set; }
        public DateTime? ContractueleTermijnIngangsdatum { get; set; }

        // Substap 3: Afwijkingen & Eigeninkomsten
        public bool? Afwijking1160 { get; set; }
        public string? HoeAfwijken1160 { get; set; } // doorbetaling, opschorting
        public string? PeriodeDoorbetalen1160 { get; set; }

        // =====================================================
        // WONING - Basis
        // =====================================================
        public string? WoningAdresKeuze { get; set; } // partij1, partij2, anders
        public string? WoningAdres { get; set; }
        public string? WoningPostcode { get; set; }
        public string? WoningHuisnummer { get; set; }
        public string? WoningToevoeging { get; set; }
        public string? WoningStraat { get; set; }
        public string? WoningPlaats { get; set; }
        public string? WoningSoort { get; set; } // koopwoning, huurwoning
        public string? WoningStatusVermogen { get; set; } // gemeenschappelijk, uitgesloten, vergoeden

        // =====================================================
        // WONING - Huurwoning
        // =====================================================
        public string? HuurrechtToekomtAan { get; set; } // partij1, partij2, beiden_verlaten
        public string? HuurrechtWanneer { get; set; } // na_inschrijving, andere_datum
        public DateTime? HuurrechtAndereDatum { get; set; }
        public bool? HuurVerzoekRechter { get; set; }
        public bool? HuurVerplichtingenVoldaan { get; set; }
        public DateTime? HuurVerplichtingenOvernameDatum { get; set; }
        public bool? HuurBorgToedelen { get; set; }
        public string? HuurBorgAan { get; set; } // partij1, partij2, ieder_helft

        // =====================================================
        // WONING - Koopwoning
        // =====================================================
        public bool? KoopNieuwbouw { get; set; }
        public bool? KoopPrivevermogenInvestering { get; set; }
        public string? KoopToedeling { get; set; } // verkoop, partij1, partij2, onverdeeld
        public string? Hypotheken { get; set; } // JSON array
        public string? KewSewBew { get; set; } // JSON array

        // Kadaster & Notaris
        public bool? KoopKadastraalVermelden { get; set; }
        public string? KoopKadastraalGemeente { get; set; }
        public string? KoopKadastraalGemeenteCode { get; set; }
        public string? KoopKadastraalSectie { get; set; }
        public string? KoopKadastraalPerceel { get; set; }
        public int? KoopKadastraalAre { get; set; }
        public int? KoopKadastraalCentiare { get; set; }
        public string? KoopKadastraalAanduiding { get; set; }
        public int? KoopKadastraalOppervlakte { get; set; }
        public int? KoopBouwjaar { get; set; }
        public string? KoopGebruiksdoel { get; set; }
        public int? KoopGebruiksoppervlakte { get; set; }
        public string? KoopNotarisMr { get; set; }
        public string? KoopNotarisStandplaats { get; set; }
        public DateTime? KoopNotarisLeveringDatum { get; set; }
        public bool? KoopNotarisHypotheekZelfde { get; set; }
        public string? KoopNotarisHypotheekMr { get; set; }
        public string? KoopNotarisHypotheekStandplaats { get; set; }
        public DateTime? KoopNotarisHypotheekDatum { get; set; }

        // Privevermogen
        public bool? KoopInvesteringNa2012 { get; set; }
        public string? KoopPrivevermogenHoe { get; set; } // inleg_aanschaf, tussentijdse_aflossing, verbouwing
        public string? KoopPrivevermogenReden { get; set; }
        public string? KoopPrivevermogenVordering { get; set; } // partij1_op_partij2, partij2_op_partij1
        public decimal? KoopPrivevermogenVorderingBedrag { get; set; }

        // Waarde
        public decimal? KoopWozWaarde { get; set; }
        public string? KoopWozPeildatum { get; set; }
        public decimal? KoopToedelingWaarde { get; set; }
        public decimal? KoopKoopsom { get; set; }
        public bool? KoopOntslagHoofdelijkheid { get; set; }
        public DateTime? KoopOntslagHoofdelijkheidDatum { get; set; }
        public string? KoopMakelaarVerkoop { get; set; }
        public decimal? KoopLaatprijs { get; set; }
        public bool? KoopVolmachtNotaris { get; set; }
        public bool? KoopMedewerkingLeveren { get; set; }
        public decimal? KoopOverbedelingWoning { get; set; }
        public decimal? KoopOverbedelingSpaarproducten { get; set; }

        // Lasten
        public bool? KoopLastenWoning { get; set; }
        public string? KoopHypotheekrente { get; set; } // partij1, partij2, ieder_helft, anders
        public string? KoopMaandelijkseAflossing { get; set; }
        public string? KoopPremieInleg { get; set; }
        public string? KoopAanslagWoz { get; set; }
        public string? KoopGebruikerslasten { get; set; }
        public string? KoopKleinOnderhoud { get; set; }
        public string? KoopGrootOnderhoud { get; set; }

        // =====================================================
        // VERMOGENSVERDELING
        // =====================================================
        public string? Bankrekeningen { get; set; } // JSON array
        public string? Beleggingen { get; set; } // JSON array
        public string? Voertuigen { get; set; } // JSON array
        public string? Verzekeringen { get; set; } // JSON array
        public string? Schulden { get; set; } // JSON array
        public string? Vorderingen { get; set; } // JSON array
        public string? Inboedel { get; set; }
        public string? VermogensverdelingOpmerkingen { get; set; }

        // =====================================================
        // PENSIOEN
        // =====================================================
        public string? Pensioenen { get; set; } // JSON array
        public string? PensioenOpmerkingen { get; set; }
        public string? BijzonderPartnerpensioen { get; set; } // geen, omzetten, afzien, gedeeltelijk
        public string? BijzonderPartnerpensioenbedrag { get; set; }

        // =====================================================
        // FISCAAL - Extra velden (naast ConvenantFiscaalData)
        // =====================================================
        public int? FiscaalJaar { get; set; }
        public string? BelastingaangifteAfspraken { get; set; }
        public string? HypotheekrenteAftrek { get; set; } // partij1, partij2, beiden
        public string? FiscaalOpmerkingen { get; set; }

        // =====================================================
        // KWIJTING & HUWELIJKSVOORWAARDEN
        // =====================================================
        public string? Huwelijksgoederenregime { get; set; } // algehele_gemeenschap_voor_2018, beperkte_gemeenschap_na_2018, huwelijksvoorwaarden
        public string? HuwelijksgoederenregimeUitzondering { get; set; }
        public string? HuwelijksgoederenregimeAnders { get; set; }
        public DateTime? HuwelijksvoorwaardenDatum { get; set; }
        public string? HuwelijksvoorwaardenNotaris { get; set; }
        public string? HuwelijksvoorwaardenNotarisPlaats { get; set; }
        public bool? KwijtingAkkoord { get; set; }
        public string? Slotbepalingen { get; set; }

        // =====================================================
        // CONSIDERANS
        // =====================================================
        public DateTime? Huwelijksdatum { get; set; }
        public string? Huwelijksplaats { get; set; }
        public bool? IsMediation { get; set; }
        public string? MediatorNaam { get; set; }
        public string? MediatorPlaats { get; set; }
        public string? Rechtbank { get; set; }
        public string? RechtbankLocatie { get; set; }
        public string? AdvocaatPartij1 { get; set; }
        public string? AdvocaatPartij2 { get; set; }
        public bool? HeeftVaststellingsovereenkomst { get; set; }
        public bool? HeeftKinderenUitHuwelijk { get; set; }
        public bool? HeeftKinderenVoorHuwelijk { get; set; }
        public DateTime? Erkenningsdatum { get; set; }
        public bool? HeeftSpaarrekeningenKinderen { get; set; }
        public string? SpaarrekeningKinderenNummers { get; set; }

        // =====================================================
        // ONDERTEKENING
        // =====================================================
        public string? OndertekeningPlaatsPartij1 { get; set; }
        public string? OndertekeningPlaatsPartij2 { get; set; }
        public DateTime? OndertekeningDatumPartij1 { get; set; }
        public DateTime? OndertekeningDatumPartij2 { get; set; }

        // =====================================================
        // META
        // =====================================================
        public DateTime AangemaaktOp { get; set; }
        public DateTime GewijzigdOp { get; set; }
    }
}
