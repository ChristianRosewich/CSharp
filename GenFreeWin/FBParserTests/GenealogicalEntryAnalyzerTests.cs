using System;
using FBParser;
using FBParser.Analysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FBParserTests;

[TestClass]
public sealed class GenealogicalEntryAnalyzerTests
{
    private static readonly GenealogicalEntryAnalyzerConfiguration TestConfiguration = new()
    {
        ProtectSpace = " ",
        BirthMarker = "*",
        BaptismMarkers = ["*)", "~"],
        BurialMarkers = ["†)", "="],
        MarriageMarkers = ["⚭", "oo", "∞"],
        DeathMarkers = ["†", "+"],
        StillbornMarkers = ["†*", "+*"],
        FallenMarker = "gefallen",
        DivorceMarker = "o/o",
        MissingMarkers = ["vermisst", "vermißt"],
        EmigrationMarkers = ["ausgewandert", "wanderte"],
        DateModifiers = ["ca", "um", "ab", "von", "vor", "nach", "err.", "seit", "zwischen", "frühestens", "spätestens"],
        SinceDateModifier = "seit",
        AgeMarker = "alt",
        IndefiniteArticles = ["eine", "ein"],
        DefiniteArticles = ["der", "die", "das"],
        AkaMarker = "genannt",
        BecameMarker = "wurde",
        DescriptionMarkers = ["ledig", "Witwer", "Witwe"],
        ResidenceMarkers = ["lebte", "leb", "wohnte", "wohnhaft", "wohnt", "Herkunft"],
        PlaceMarkers = ["in", "aus", "nach", "am", "bei", "im", "auf der"],
        UnknownMarkers = ["…", "...", ".."],
        PropertyMarkers = ["baute", "kaufte", "erwarb"],
        AddressMarkers = ["str.", "siedl", "straße", "gasse", "weg", "platz", "pfad"],
        ReligionMarkers = ["rk.", "kath.", "ev.", "evang.", "ref.", "reform.", "luth."],
        MonthNames = ["", "Januar", "Februar", "März", "April", "Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember"],
        UpperUmlautMarkers = ["Ä", "Ö", "Ü"],
        InPlaceMarker = "in",
        ToPlaceMarker = "nach",
        FromPlaceMarker = "aus",
        InMonthPlaceMarker = "im",
        OnDatePlaceMarker = "am",
        MarriagePartnerMarker = "mit",
        IsValidDate = static date =>
            !date.Contains('\n')
            && !date.Contains('\r')
            && !date.Contains('\t')
            && !date.Contains(',')
            && !date.Contains(':')
            && !date.Contains(';')
            && !date.Contains('<')
            && !date.Contains('>')
            && !date.Contains('+')
            && !date.Contains('*')
            && !date.Contains('|'),
        IsValidPlace = static place => place == string.Empty
            || place == "..."
            || PascalCompat.UpperCharset.Contains(place[0])
            || place[0] is '"' or '“'
            || FBEntryParser.TestFor(place, 1, ["Ä", "Ö", "Ü"]),
        Warning = static _ => { },
    };

    [TestMethod]
    [DataRow("* 12.03.1900", ParserEventType.evt_Birth)]
    [DataRow("† 12.03.1900", ParserEventType.evt_Death)]
    [DataRow("⚭ 12.03.1900", ParserEventType.evt_Marriage)]
    [DataRow("o/o 12.03.1900", ParserEventType.evt_Divorce)]
    [DataRow("rk.", ParserEventType.evt_Religion)]
    public void GetEntryType_DetectsKnownEntryMarkers(string text, ParserEventType expected)
    {
        var sut = new GenealogicalEntryAnalyzer(TestConfiguration);

        var result = sut.GetEntryType(text, out _, out _);

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TrimPlaceByMonth_RemovesTrailingMonth()
    {
        var sut = new GenealogicalEntryAnalyzer(TestConfiguration);
        var place = "Berlin März";

        sut.TrimPlaceByMonth(ref place);

        Assert.AreEqual("Berlin", place);
    }

    [TestMethod]
    public void TrimPlaceByModif_RemovesTrailingDateModifier()
    {
        var sut = new GenealogicalEntryAnalyzer(TestConfiguration);
        var place = "Berlin vor";

        sut.TrimPlaceByModif(ref place);

        Assert.AreEqual("Berlin", place);
    }

    [TestMethod]
    public void AnalyseEntry_AssignsDefaultPlaceForOccupationEntry()
    {
        var sut = new GenealogicalEntryAnalyzer(TestConfiguration);
        var entry = "Schneider 12.03.1900";

        sut.AnalyseEntry(ref entry, "Bern", 0, out var entryType, out var data, out var place, out var date);

        Assert.AreEqual(ParserEventType.evt_Last, entryType);
        Assert.AreEqual("Schneider", entry);
        Assert.AreEqual(string.Empty, data);
        Assert.AreEqual("Bern", place);
        Assert.AreEqual("12.03.1900", date);
    }
}
