using System.Text.RegularExpressions;

namespace Sclon.Morphology;

/// <summary>Анализирует русское слово: выделяет основу, род, число, одушевлённость.</summary>
public partial class RussianAnalyzer
{
    // Гласные буквы
    private static readonly HashSet<char> Vowels =
    [
        'а',
        'е',
        'ё',
        'и',
        'о',
        'у',
        'ы',
        'э',
        'ю',
        'я',
    ];

    // Шипящие
    private static readonly HashSet<char> Hissing = ['ж', 'ш', 'ч', 'щ'];

    // Звонкие/глухие для базовой фонетики
    private static readonly HashSet<char> SoftConsonants = ['й', 'ч', 'щ'];

    // Предлоги и неизменяемые частицы
    private static readonly HashSet<string> Prepositions =
    [
        "без",
        "близ",
        "в",
        "во",
        "для",
        "до",
        "за",
        "из",
        "изо",
        "к",
        "ко",
        "на",
        "над",
        "о",
        "об",
        "обо",
        "от",
        "ото",
        "перед",
        "передо",
        "по",
        "под",
        "подо",
        "при",
        "про",
        "с",
        "со",
        "у",
        "через",
        "чрез",
    ];

    private static readonly HashSet<string> Conjunctions =
    [
        "и",
        "а",
        "но",
        "да",
        "или",
        "либо",
        "то",
        "как",
        "что",
        "чтобы",
        "если",
        "когда",
        "потому",
        "так",
        "же",
    ];

    // Определители рода по окончанию (для существительных)
    private static readonly HashSet<string> FeminineEndings = ["а", "я", "ия"];
    private static readonly HashSet<string> MasculineEndingsConsonant = ["ь"]; // мягкий знак

    // Список одушевлённых существительных (базовый, можно расширять)
    private static readonly HashSet<string> AnimateNouns =
    [
        "человек",
        "мужчина",
        "женщина",
        "мальчик",
        "девочка",
        "ребёнок",
        "ребенок",
        "кот",
        "кошка",
        "собака",
        "пёс",
        "пес",
        "попугай",
        "рыба",
        "птица",
        "друг",
        "подруга",
        "парень",
        "девушка",
        "учитель",
        "врач",
        "студент",
        "школьник",
        "папа",
        "мама",
        "дедушка",
        "бабушка",
        "брат",
        "сестра",
        "сын",
        "дочь",
        "дядя",
        "тётя",
        "юноша",
    ];

    // Исключения — слова, где окончание не определяет род
    private static readonly Dictionary<string, GrammaticalGender> GenderExceptions = new()
    {
        // Мужской род, но окончание -а/-я (1-е склонение)
        ["папа"] = GrammaticalGender.Masculine,
        ["дядя"] = GrammaticalGender.Masculine,
        ["дедушка"] = GrammaticalGender.Masculine,
        ["юноша"] = GrammaticalGender.Masculine,
        ["мужчина"] = GrammaticalGender.Masculine,
        ["воевода"] = GrammaticalGender.Masculine,
        ["староста"] = GrammaticalGender.Masculine,
        ["судья"] = GrammaticalGender.Masculine,
        // Средний род на -я
        ["время"] = GrammaticalGender.Neuter,
        ["имя"] = GrammaticalGender.Neuter,
        ["племя"] = GrammaticalGender.Neuter,
        ["семя"] = GrammaticalGender.Neuter,
        ["темя"] = GrammaticalGender.Neuter,
        ["стремя"] = GrammaticalGender.Neuter,
        ["бремя"] = GrammaticalGender.Neuter,
        ["вымя"] = GrammaticalGender.Neuter,
        ["пламя"] = GrammaticalGender.Neuter,
        ["знамя"] = GrammaticalGender.Neuter,
        ["полымя"] = GrammaticalGender.Neuter,
        // Мужской род на -ь
        ["дождь"] = GrammaticalGender.Masculine,
        ["камень"] = GrammaticalGender.Masculine,
        ["корень"] = GrammaticalGender.Masculine,
        ["гусь"] = GrammaticalGender.Masculine,
        ["конь"] = GrammaticalGender.Masculine,
        ["огонь"] = GrammaticalGender.Masculine,
        ["пень"] = GrammaticalGender.Masculine,
        ["день"] = GrammaticalGender.Masculine,
        ["рубль"] = GrammaticalGender.Masculine,
        ["словарь"] = GrammaticalGender.Masculine,
        ["лагерь"] = GrammaticalGender.Masculine,
        ["янтарь"] = GrammaticalGender.Masculine,
        ["путь"] = GrammaticalGender.Masculine,
        ["гвоздь"] = GrammaticalGender.Masculine,
        ["медведь"] = GrammaticalGender.Masculine,
        // Женский род на -ь (3-е склонение)
        ["ночь"] = GrammaticalGender.Feminine,
        ["дочь"] = GrammaticalGender.Feminine,
        ["мышь"] = GrammaticalGender.Feminine,
        ["печь"] = GrammaticalGender.Feminine,
        ["речь"] = GrammaticalGender.Feminine,
        ["вещь"] = GrammaticalGender.Feminine,
        ["рожь"] = GrammaticalGender.Feminine,
        ["тишь"] = GrammaticalGender.Feminine,
        ["глушь"] = GrammaticalGender.Feminine,
        ["сушь"] = GrammaticalGender.Feminine,
        ["молодёжь"] = GrammaticalGender.Feminine,
        ["ложь"] = GrammaticalGender.Feminine,
        ["дрожь"] = GrammaticalGender.Feminine,
        ["брошь"] = GrammaticalGender.Feminine,
        ["помощь"] = GrammaticalGender.Feminine,
        ["связь"] = GrammaticalGender.Feminine,
        ["жизнь"] = GrammaticalGender.Feminine,
        ["смерть"] = GrammaticalGender.Feminine,
        ["любовь"] = GrammaticalGender.Feminine,
        ["кровь"] = GrammaticalGender.Feminine,
        ["морковь"] = GrammaticalGender.Feminine,
        ["церковь"] = GrammaticalGender.Feminine,
        ["тетрадь"] = GrammaticalGender.Feminine,
        ["кровать"] = GrammaticalGender.Feminine,
        ["площадь"] = GrammaticalGender.Feminine,
        ["радость"] = GrammaticalGender.Feminine,
        ["печаль"] = GrammaticalGender.Feminine,
        ["дверь"] = GrammaticalGender.Feminine,
        ["соль"] = GrammaticalGender.Feminine,
        ["пыль"] = GrammaticalGender.Feminine,
        ["сталь"] = GrammaticalGender.Feminine,
        ["ткань"] = GrammaticalGender.Feminine,
        ["осень"] = GrammaticalGender.Feminine,
        ["тень"] = GrammaticalGender.Feminine,
        ["степь"] = GrammaticalGender.Feminine,
        ["модель"] = GrammaticalGender.Feminine,
        ["нефть"] = GrammaticalGender.Feminine,
        ["власть"] = GrammaticalGender.Feminine,
        ["честь"] = GrammaticalGender.Feminine,
    };

    // Слова с беглой гласной: основа без гласной (пёс→пс, день→дн, огонь→огн, котёл→котл)
    private static readonly Dictionary<string, string> FleetingVowels = new()
    {
        ["пёс"] = "пс",
        ["пес"] = "пс",
        ["день"] = "дн",
        ["пень"] = "пн",
        ["огонь"] = "огн",
        ["камень"] = "камн",
        ["корень"] = "корн",
        ["котёл"] = "котл",
        ["котёл"] = "котл",
        ["рот"] = "рт",
        ["лоб"] = "лб",
        ["лёд"] = "льд",
        ["лёд"] = "льд",
        ["сон"] = "сн",
        ["лев"] = "льв",
        ["потолок"] = "потолк",
        ["угол"] = "угл",
        ["ветер"] = "ветр",
        ["ветр"] = "ветр",
        ["огонь"] = "огн",
        ["парень"] = "парн",
        ["хозяин"] = "хозя",
        ["боец"] = "бойц",
        ["молодец"] = "молодц",
        ["купец"] = "купц",
        ["отец"] = "отц",
        ["зонтик"] = "зонтик", // не беглая, но -ик- может выпадать: зонтик→зонтика (сохраняется)
        ["певец"] = "певц",
        ["жнец"] = "жнец",
    };

    /// <summary>Разбить фразу на слова (токены).</summary>
    public static string[] Tokenize(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return [];

        // Разделяем по пробелам, знакам препинания
        var tokens = MyRegex()
            .Split(phrase)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0 && !char.IsPunctuation(t[0]))
            .ToArray();

        return tokens;
    }

    /// <summary>Анализирует слово.</summary>
    public static WordInfo Analyze(string word)
    {
        var info = new WordInfo { Original = word };

        // Проверка на предлоги и союзы
        if (Prepositions.Contains(word))
        {
            info.PartOfSpeech = PartOfSpeech.Preposition;
            return info;
        }
        if (Conjunctions.Contains(word))
        {
            info.PartOfSpeech = PartOfSpeech.Conjunction;
            return info;
        }

        // Попытка определить часть речи по окончанию
        DeterminePartOfSpeech(info);
        DetermineGender(info);
        DetermineAnimacy(info);
        DetermineSoftHissing(info);

        return info;
    }

    private static void DeterminePartOfSpeech(WordInfo info)
    {
        var w = info.Original;
        if (w.Length == 0)
            return;

        char last = w[^1];
        char? prev = w.Length >= 2 ? w[^2] : null;

        // Наречия распространённые
        if (w.EndsWith("о") && w.Length > 3 && !IsLikelyNoun(w))
        {
            // неоднозначно — может быть существительным ср.р. или наречием
        }

        // Считаем существительным, если заканчивается на типичные окончания
        if (
            Vowels.Contains(last)
            || last == 'ь'
            || last == 'й'
            || (IsConsonant(last) && last != 'ь' && last != 'й' && last != 'ъ')
        )
        {
            // По умолчанию — существительное или прилагательное
            // Прилагательные обычно заканчиваются на -ый, -ий, -ой, -ая, -яя, -ое, -ее, -ые, -ие
            if (
                (
                    w.EndsWith("ый")
                    || w.EndsWith("ий")
                    || w.EndsWith("ой")
                    || w.EndsWith("ая")
                    || w.EndsWith("яя")
                    || w.EndsWith("ое")
                    || w.EndsWith("ее")
                    || w.EndsWith("ые")
                    || w.EndsWith("ие")
                )
                && w.Length > 3
            )
            {
                info.PartOfSpeech = PartOfSpeech.Adjective;
            }
            else
            {
                info.PartOfSpeech = PartOfSpeech.Noun;
            }
        }
    }

    private static bool IsLikelyNoun(string w)
    {
        return GenderExceptions.ContainsKey(w);
    }

    private static void DetermineGender(WordInfo info)
    {
        if (info.PartOfSpeech != PartOfSpeech.Noun)
            return;

        var w = info.Original;

        // Проверка исключений
        if (GenderExceptions.TryGetValue(w, out var exGender))
        {
            info.Gender = exGender;
            return;
        }

        char last = w[^1];

        if (last == 'а' || last == 'я')
        {
            // -а, -я → женский род (кроме исключений)
            // Если 2-я буква с конца гласная и слово длинное — может быть ср.р. (например, "дитя")
            info.Gender = GrammaticalGender.Feminine;
            // Но если это -мя (бремя, время, семя...) — средний род
            if (w.EndsWith("мя") && w.Length > 3)
                info.Gender = GrammaticalGender.Neuter;
        }
        else if (last == 'о' || last == 'е' || last == 'ё')
        {
            info.Gender = GrammaticalGender.Neuter;
        }
        else if (last == 'ь')
        {
            // Мягкий знак — неопределённо, ставим мужской по умолчанию,
            // но проверим через список частотных окончаний
            // Окончания женского рода 3-го склонения: -ость, -есть, -жь, -шь, -чь, -щь
            // -ость, -есть, -жь, -шь, -чь, -щь женского рода
            // Но не -тель, -арь (мужской род — профессии)
            if (w.EndsWith("ость") || w.EndsWith("есть"))
            {
                info.Gender = GrammaticalGender.Feminine;
            }
            else if (w.EndsWith("жь") || w.EndsWith("шь") || w.EndsWith("чь") || w.EndsWith("щь"))
            {
                info.Gender = GrammaticalGender.Feminine;
            }
            else if (w.EndsWith("ль"))
            {
                // -ль: женский род (соль, сталь, печаль), кроме профессий на -тель, -арь
                if (w.EndsWith("тель") || w.EndsWith("арь"))
                    info.Gender = GrammaticalGender.Masculine;
                else
                    info.Gender = GrammaticalGender.Feminine;
            }
            else if (w.EndsWith("нь")) // ткань, осень — женский
            {
                info.Gender = GrammaticalGender.Feminine;
            }
            else if (w.EndsWith("сь")) // связь, Русь
            {
                info.Gender = GrammaticalGender.Feminine;
            }
            else
            {
                info.Gender = GrammaticalGender.Masculine;
            }
        }
        else if (last == 'й')
        {
            info.Gender = GrammaticalGender.Masculine;
        }
        else if (IsConsonant(last))
        {
            info.Gender = GrammaticalGender.Masculine;
        }
    }

    private static void DetermineAnimacy(WordInfo info)
    {
        if (info.PartOfSpeech != PartOfSpeech.Noun)
            return;

        if (AnimateNouns.Contains(info.Original))
        {
            info.Animacy = Animacy.Animate;
            return;
        }

        // Суффиксы одушевлённости: -тель, -ист, -ец, -щик, -чик, -арь, -ак, -як
        var w = info.Original;
        if (
            w.EndsWith("тель")
            || w.EndsWith("ист")
            || w.EndsWith("ец")
            || w.EndsWith("щик")
            || w.EndsWith("чик")
            || w.EndsWith("арь")
            || w.EndsWith("ак")
            || w.EndsWith("як")
            || w.EndsWith("лог")
            || w.EndsWith("вед")
            || w.EndsWith("ёр")
            || w.EndsWith("ер")
            || w.EndsWith("атор")
            || w.EndsWith("итор")
            || w.EndsWith("ик")
            || w.EndsWith("ник")
            || w.EndsWith("ун")
            || w.EndsWith("ок")
        )
        {
            info.Animacy = Animacy.Animate;
        }

        // Женские профессии и люди
        if (
            info.Gender == GrammaticalGender.Feminine
            && (
                w.EndsWith("ица")
                || w.EndsWith("ница")
                || w.EndsWith("чица")
                || w.EndsWith("щица")
                || w.EndsWith("ша")
                || w.EndsWith("иха")
            )
        )
        {
            info.Animacy = Animacy.Animate;
        }
    }

    private static void DetermineSoftHissing(WordInfo info)
    {
        if (info.PartOfSpeech != PartOfSpeech.Noun && info.PartOfSpeech != PartOfSpeech.Adjective)
            return;

        var w = info.Original;
        if (w.Length == 0)
            return;

        char last = w[^1];
        if (Hissing.Contains(last))
            info.IsHissing = true;
        if (last == 'й' || last == 'ь' || last == 'ч' || last == 'щ')
            info.IsSoft = true;
    }

    /// <summary>Возвращает "основу" слова (без окончания Им.п., если удаётся выделить).</summary>
    public static string GetStem(WordInfo info)
    {
        if (info.PartOfSpeech != PartOfSpeech.Noun)
            return info.Original;

        var w = info.Original;
        if (w.Length <= 2)
            return w; // короткие слова не усекаем

        // Проверка на беглую гласную
        if (FleetingVowels.TryGetValue(w, out var fleetingStem))
            return fleetingStem;

        return info.Gender switch
        {
            GrammaticalGender.Feminine when w.EndsWith("а") => w[..^1],
            GrammaticalGender.Feminine when w.EndsWith("я") => w[..^1],
            GrammaticalGender.Masculine when w.EndsWith("й") => w[..^1],
            GrammaticalGender.Masculine when w.EndsWith("ь") => w[..^1],
            GrammaticalGender.Neuter when w.EndsWith("о") => w[..^1],
            GrammaticalGender.Neuter when w.EndsWith("е") => w[..^1],
            GrammaticalGender.Neuter when w.EndsWith("ё") => w[..^1],
            GrammaticalGender.Neuter when w.EndsWith("мя") => w[..^2],
            _ => w, // мужской на согласный — основа = слово
        };
    }

    /// <summary>
    /// Возвращает stem для склонения (с учётом беглых гласных).
    /// Для мужских слов с беглой гласной возвращает основу без неё.
    /// </summary>
    public static string GetDeclensionStem(WordInfo info)
    {
        var w = info.Original;

        // Слова с беглой гласной
        if (FleetingVowels.TryGetValue(w, out var fleetingStem))
            return fleetingStem;

        // Суффиксы -ец, -ок, -ик — у некоторых беглая гласная
        // Упрощённо: для слов на -ец, -ок убираем гласную
        if (
            w.EndsWith("ец")
            && w.Length > 3
            && info.PartOfSpeech == PartOfSpeech.Noun
            && info.Gender == GrammaticalGender.Masculine
        )
        {
            return w[..^2] + "ц"; // боец→бойц, певец→певц, купец→купц
        }
        if (
            w.EndsWith("ок")
            && w.Length > 3
            && info.PartOfSpeech == PartOfSpeech.Noun
            && info.Gender == GrammaticalGender.Masculine
        )
        {
            // Не все на -ок имеют беглую, но многие: замок→замк, носок→носк
            char before = w[^3];
            if (IsConsonant(before))
                return w[..^2] + "к";
        }

        return GetStem(info);
    }

    /// <summary>Выделенная stateless проверка на согласную.</summary>
    public static bool IsConsonant(char c)
    {
        return char.IsLetter(c) && !Vowels.Contains(c) && c != 'ь' && c != 'ъ';
    }

    [GeneratedRegex(@"[\s,\.!?\:;\(\)\[\]""«»\-]+")]
    private static partial Regex MyRegex();
}
