namespace Sclon.Morphology;

/// <summary>Падежи русского языка.</summary>
public enum RussianCase
{
    Nominative,     // Именительный: кто? что?
    Genitive,       // Родительный:  кого? чего?
    Dative,         // Дательный:    кому? чему?
    Accusative,     // Винительный:  кого? что?
    Instrumental,   // Творительный: кем? чем?
    Prepositional   // Предложный:   о ком? о чём?
}

/// <summary>Род.</summary>
public enum GrammaticalGender
{
    Masculine,
    Feminine,
    Neuter
}

/// <summary>Число.</summary>
public enum GrammaticalNumber
{
    Singular,
    Plural
}

/// <summary>Одушевлённость.</summary>
public enum Animacy
{
    Animate,   // одушевлённое
    Inanimate  // неодушевлённое
}

/// <summary>Предполагаемая часть речи слова.</summary>
public enum PartOfSpeech
{
    Noun,
    Adjective,
    Verb,
    Preposition,
    Conjunction,
    Particle,
    Pronoun,
    Numeral,
    Adverb,
    Other
}

/// <summary>Информация о слове, необходимая для склонения.</summary>
public class WordInfo
{
    public string Original { get; set; } = "";
    public string Stem { get; set; } = "";
    public PartOfSpeech PartOfSpeech { get; set; } = PartOfSpeech.Other;
    public GrammaticalGender Gender { get; set; } = GrammaticalGender.Masculine;
    public GrammaticalNumber Number { get; set; } = GrammaticalNumber.Singular;
    public Animacy Animacy { get; set; } = Animacy.Inanimate;
    public bool IsSoft { get; set; }       // мягкая основа (заканчивается на -й, -чь, -нь и т.д.)
    public bool IsHissing { get; set; }     // шипящая основа (ж, ш, ч, щ)
    public string Ending { get; set; } = ""; // окончание в именительном падеже

    /// <summary>Склоняемая часть речи или нет.</summary>
    public bool IsDeclinable =>
        PartOfSpeech is PartOfSpeech.Noun or PartOfSpeech.Adjective;
}

/// <summary>Названия падежей и их вопросы.</summary>
public static class CaseHelper
{
    public static readonly (string Name, string Question)[] CaseInfo =
    [
        ("Именительный", "кто? что?"),
        ("Родительный",  "кого? чего?"),
        ("Дательный",    "кому? чему?"),
        ("Винительный",  "кого? что?"),
        ("Творительный", "кем? чем?"),
        ("Предложный",   "о ком? о чём?")
    ];

    public static string GetCaseName(RussianCase c) => CaseInfo[(int)c].Name;

    public static string GetCaseQuestion(RussianCase c) => CaseInfo[(int)c].Question;
}