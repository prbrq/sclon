namespace Sclon.Morphology;

/// <summary>
/// Движок склонения русских слов и фраз по падежам.
/// Поддерживает существительные (1, 2, 3 склонения) и прилагательные.
/// </summary>
public static class RussianDeclensionEngine
{
    // ────────── Склонение существительных ──────────

    /// <summary>Просклонять существительное.</summary>
    public static string DeclineNoun(string word, RussianCase targetCase)
    {
        var info = RussianAnalyzer.Analyze(word);
        return DeclineNoun(info, targetCase);
    }

    public static string DeclineNoun(WordInfo info, RussianCase targetCase)
    {
        if (info.PartOfSpeech != PartOfSpeech.Noun)
            return info.Original;

        if (targetCase == RussianCase.Nominative)
            return info.Original;

        var w = info.Original;
        char last = w[^1];

        // Разносклоняемые слова на -мя
        if (w.EndsWith("мя") && info.Gender == GrammaticalGender.Neuter && w.Length > 3)
            return DeclineMyaStem(w, targetCase);

        // Путь
        if (w == "путь")
            return DeclinePut(targetCase);

        // Дитя
        if (w == "дитя")
            return DeclineDitya(targetCase);

        // Singularia/Pluralia — упрощённо
        return info.Gender switch
        {
            // ── 1-е склонение (мужской и средний род) ──
            GrammaticalGender.Masculine => DeclineMasculine(info, targetCase),
            GrammaticalGender.Neuter => DeclineNeuter(info, targetCase),
            // ── 2-е склонение (женский и мужской на -а/-я) ──
            GrammaticalGender.Feminine when last == 'а' || last == 'я' => DeclineFeminineA(info, targetCase),
            // ── 3-е склонение (женский на -ь) ──
            GrammaticalGender.Feminine when last == 'ь' => DeclineFeminineSoft(info, targetCase),
            // ── По умолчанию ──
            _ => info.Original
        };
    }

    // 1-е склонение: мужской род
    private static string DeclineMasculine(WordInfo info, RussianCase targetCase)
    {
        var w = info.Original;
        char last = w[^1];
        string stem;
        bool animate = info.Animacy == Animacy.Animate;

        if (last == 'й')
        {
            stem = w[..^1];
            return targetCase switch
            {
                RussianCase.Genitive => stem + "я",
                RussianCase.Dative => stem + "ю",
                RussianCase.Accusative => animate ? stem + "я" : stem + "я",
                RussianCase.Instrumental => stem + "ем",
                RussianCase.Prepositional => stem + "е",
                _ => w
            };
        }

        if (last == 'ь')
        {
            // Для слов с беглой гласной используем специальную основу
            string fleetingStem = RussianAnalyzer.GetDeclensionStem(info);
            // Если основа отличается от w[..^1], значит есть беглая гласная
            if (fleetingStem != info.Original)
                stem = fleetingStem;
            else
                stem = w[..^1];

            return targetCase switch
            {
                RussianCase.Genitive => stem + "я",
                RussianCase.Dative => stem + "ю",
                RussianCase.Accusative => animate ? stem + "я" : w,
                RussianCase.Instrumental => stem + "ем",
                RussianCase.Prepositional => stem + "е",
                _ => w
            };
        }

        // Твёрдая основа — используем GetDeclensionStem для учёта беглых гласных
        stem = RussianAnalyzer.GetDeclensionStem(info);
        if (IsHissing(last))
        {
            return targetCase switch
            {
                RussianCase.Genitive => stem + "а",
                RussianCase.Dative => stem + "у",
                RussianCase.Accusative => animate ? stem + "а" : w,
                RussianCase.Instrumental => stem + "ом",
                RussianCase.Prepositional => stem + "е",
                _ => w
            };
        }

        // Обычная согласная (твёрдая основа)
        return targetCase switch
        {
            RussianCase.Genitive => stem + "а",
            RussianCase.Dative => stem + "у",
            RussianCase.Accusative => animate ? stem + "а" : w,
            RussianCase.Instrumental => stem + "ом",
            RussianCase.Prepositional => stem + "е",
            _ => w
        };
    }

    // 1-е склонение: средний род
    private static string DeclineNeuter(WordInfo info, RussianCase targetCase)
    {
        var w = info.Original;
        char last = w[^1];
        string stem;

        if (last == 'о')
        {
            stem = w[..^1];
            // Если основа на шипящую: -ом, -е
            if (stem.Length > 0 && IsHissing(stem[^1]))
            {
                return targetCase switch
                {
                    RussianCase.Genitive => stem + "а",
                    RussianCase.Dative => stem + "у",
                    RussianCase.Accusative => w,
                    RussianCase.Instrumental => stem + "ом",
                    RussianCase.Prepositional => stem + "е",
                    _ => w
                };
            }
            return targetCase switch
            {
                RussianCase.Genitive => stem + "а",
                RussianCase.Dative => stem + "у",
                RussianCase.Accusative => w,
                RussianCase.Instrumental => stem + "ом",
                RussianCase.Prepositional => stem + "е",
                _ => w
            };
        }

        if (last == 'е' || last == 'ё')
        {
            stem = w[..^1];
            // Если основа на шипящую: -ем/-ём
            bool hissingStem = stem.Length > 0 && IsHissing(stem[^1]);
            string instEnding = last == 'ё' || hissingStem ? "ём" : "ем";
            
            return targetCase switch
            {
                RussianCase.Genitive => stem + "я",
                RussianCase.Dative => stem + "ю",
                RussianCase.Accusative => w,
                RussianCase.Instrumental => stem + instEnding,
                RussianCase.Prepositional => stem + "е",
                _ => w
            };
        }

        // -ие, -ье → особенное склонение
        if (w.EndsWith("ие") && w.Length > 3)
        {
            stem = w[..^2];
            return targetCase switch
            {
                RussianCase.Genitive => stem + "ия",
                RussianCase.Dative => stem + "ию",
                RussianCase.Accusative => w,
                RussianCase.Instrumental => stem + "ием",
                RussianCase.Prepositional => stem + "ии",
                _ => w
            };
        }

        return w;
    }

    // 2-е склонение: женский (и мужской) род на -а/-я
    private static string DeclineFeminineA(WordInfo info, RussianCase targetCase)
    {
        var w = info.Original;
        char last = w[^1];
        string stem;
        bool animate = info.Animacy == Animacy.Animate;

        if (last == 'а')
        {
            stem = w[..^1];
            // Твёрдая основа
            if (stem.Length > 0 && IsHissing(stem[^1]))
            {
                // Шипящая основа: -а → -и (род), -е (дат, предл), -у (вин), -ой (твор)
                return targetCase switch
                {
                    RussianCase.Genitive => stem + "и",
                    RussianCase.Dative => stem + "е",
                    RussianCase.Accusative => animate ? stem + "у" : stem + "у",
                    RussianCase.Instrumental => stem + "ой",
                    RussianCase.Prepositional => stem + "е",
                    _ => w
                };
            }
            // После к, г, х особенность: -а → -и (род), -е (дат), -у (вин), -ой (твор), -е (предл)
            // Но в родительном: к, г, х + а → и
            char? prev = stem.Length > 0 ? stem[^1] : null;
            if (prev is 'к' or 'г' or 'х')
            {
                return targetCase switch
                {
                    RussianCase.Genitive => stem + "и",
                    RussianCase.Dative => stem + "е",
                    RussianCase.Accusative => stem + "у",
                    RussianCase.Instrumental => stem + "ой",
                    RussianCase.Prepositional => stem + "е",
                    _ => w
                };
            }
            
            return targetCase switch
            {
                RussianCase.Genitive => stem + "ы",
                RussianCase.Dative => stem + "е",
                RussianCase.Accusative => stem + "у",
                RussianCase.Instrumental => stem + "ой",
                RussianCase.Prepositional => stem + "е",
                _ => w
            };
        }

        if (last == 'я')
        {
            stem = w[..^1];
            return targetCase switch
            {
                RussianCase.Genitive => stem + "и",
                RussianCase.Dative => stem + "е",
                RussianCase.Accusative => stem + "ю",
                RussianCase.Instrumental => stem + "ей",
                RussianCase.Prepositional => stem + "е",
                _ => w
            };
        }

        // -ия
        if (w.EndsWith("ия"))
        {
            stem = w[..^2];
            return targetCase switch
            {
                RussianCase.Genitive => stem + "ии",
                RussianCase.Dative => stem + "ии",
                RussianCase.Accusative => stem + "ию",
                RussianCase.Instrumental => stem + "ией",
                RussianCase.Prepositional => stem + "ии",
                _ => w
            };
        }

        return w;
    }

    // 3-е склонение: женский род на -ь
    private static string DeclineFeminineSoft(WordInfo info, RussianCase targetCase)
    {
        var w = info.Original;
        string stem = w[..^1];
        char? prev = stem.Length > 0 ? stem[^1] : null;

        // Основа на шипящую  — добавляется -и, -и, -ь, -ью, -и
        // Если на ж/ш/ч/щ
        string genitiveEnding = "и";
        string dativeEnding = "и";
        string accusativeEnding = "ь";
        string instrumentalEnding;
        string prepositionalEnding = "и";

        if (prev is 'ж' or 'ш' or 'ч' or 'щ')
        {
            instrumentalEnding = "ью";
        }
        else
        {
            instrumentalEnding = "ью";
        }

        return targetCase switch
        {
            RussianCase.Genitive => stem + genitiveEnding,
            RussianCase.Dative => stem + dativeEnding,
            RussianCase.Accusative => stem + accusativeEnding, // неодуш. = им.п.
            RussianCase.Instrumental => stem + instrumentalEnding,
            RussianCase.Prepositional => stem + prepositionalEnding,
            _ => w
        };
    }

    // Разносклоняемые на -мя
    private static string DeclineMyaStem(string w, RussianCase targetCase)
    {
        string stem = w[..^2]; // убираем -мя
        return targetCase switch
        {
            RussianCase.Genitive => stem + "мени",
            RussianCase.Dative => stem + "мени",
            RussianCase.Accusative => w,
            RussianCase.Instrumental => stem + "менем",
            RussianCase.Prepositional => stem + "мени",
            _ => w
        };
    }

    // Слово "путь"
    private static string DeclinePut(RussianCase targetCase)
    {
        return targetCase switch
        {
            RussianCase.Genitive => "пути",
            RussianCase.Dative => "пути",
            RussianCase.Accusative => "путь",
            RussianCase.Instrumental => "путём",
            RussianCase.Prepositional => "пути",
            _ => "путь"
        };
    }

    // Слово "дитя"
    private static string DeclineDitya(RussianCase targetCase)
    {
        return targetCase switch
        {
            RussianCase.Genitive => "дитяти",
            RussianCase.Dative => "дитяти",
            RussianCase.Accusative => "дитя",
            RussianCase.Instrumental => "дитятей",
            RussianCase.Prepositional => "дитяти",
            _ => "дитя"
        };
    }

    // ────────── Склонение прилагательных ──────────

    /// <summary>Просклонять прилагательное (согласовать с родом, числом, падежом, одушевлённостью).</summary>
    public static string DeclineAdjective(string adjective, GrammaticalGender gender, 
                                           GrammaticalNumber number, RussianCase targetCase,
                                           Animacy animacy = Animacy.Inanimate)
    {
        var info = RussianAnalyzer.Analyze(adjective);
        if (info.PartOfSpeech != PartOfSpeech.Adjective)
            return adjective;

        return DeclineAdjectiveForm(adjective, gender, number, targetCase, animacy);
    }

    /// <summary>Склонение прилагательного с учётом его морфологии.</summary>
    private static string DeclineAdjectiveForm(string adj, GrammaticalGender gender, 
                                                GrammaticalNumber number, RussianCase targetCase,
                                                Animacy animacy = Animacy.Inanimate)
    {
        // Определяем тип основы
        bool isSoft = IsAdjectiveSoftStem(adj);
        bool isHissing = IsAdjectiveHissingStem(adj);

        if (number == GrammaticalNumber.Plural)
        {
            return DeclineAdjectivePlural(adj, targetCase, isSoft, isHissing, animacy);
        }

        return gender switch
        {
            GrammaticalGender.Masculine => DeclineAdjectiveMasculine(adj, targetCase, isSoft, isHissing, animacy),
            GrammaticalGender.Feminine => DeclineAdjectiveFeminine(adj, targetCase, isSoft, isHissing),
            GrammaticalGender.Neuter => DeclineAdjectiveNeuter(adj, targetCase, isSoft, isHissing),
            _ => adj
        };
    }

    private static string DeclineAdjectiveMasculine(string adj, RussianCase targetCase, bool isSoft, bool isHissing, Animacy animacy = Animacy.Inanimate)
    {
        // Выделяем основу: убираем -ый, -ий, -ой
        string stem = isSoft ? ExtractSoftAdjStem(adj) : ExtractAdjStem(adj);

        if (targetCase == RussianCase.Nominative)
            return adj;

        if (targetCase == RussianCase.Accusative)
        {
            // Для одушевлённых — как родительный; для неодушевлённых — как именительный
            if (animacy == Animacy.Animate)
                return stem + (isSoft || isHissing ? "его" : "ого");
            else
                return adj; // неодуш. = им.п.
        }

        return targetCase switch
        {
            RussianCase.Genitive => stem + (isSoft || isHissing ? "его" : "ого"),
            RussianCase.Dative => stem + (isSoft || isHissing ? "ему" : "ому"),
            RussianCase.Instrumental => stem + (isSoft || isHissing ? "им" : "ым"),
            RussianCase.Prepositional => stem + (isSoft || isHissing ? "ем" : "ом"),
            _ => adj
        };
    }

    private static string DeclineAdjectiveFeminine(string adj, RussianCase targetCase, bool isSoft, bool isHissing)
    {
        string stem = isSoft ? ExtractSoftAdjStem(adj) : ExtractAdjStem(adj);

        if (targetCase == RussianCase.Nominative)
            return adj;

        return targetCase switch
        {
            RussianCase.Genitive => stem + (isSoft ? "ей" : "ой"),
            RussianCase.Dative => stem + (isSoft ? "ей" : "ой"),
            RussianCase.Accusative => stem + (isSoft ? "юю" : "ую"),
            RussianCase.Instrumental => stem + (isSoft ? "ей" : "ой"),
            RussianCase.Prepositional => stem + (isSoft ? "ей" : "ой"),
            _ => adj
        };
    }

    private static string DeclineAdjectiveNeuter(string adj, RussianCase targetCase, bool isSoft, bool isHissing)
    {
        string stem = isSoft ? ExtractSoftAdjStem(adj) : ExtractAdjStem(adj);

        if (targetCase == RussianCase.Nominative || targetCase == RussianCase.Accusative)
            return adj;

        return targetCase switch
        {
            RussianCase.Genitive => stem + (isSoft ? "его" : "ого"),
            RussianCase.Dative => stem + (isSoft ? "ему" : "ому"),
            RussianCase.Instrumental => stem + (isSoft ? "им" : "ым"),
            RussianCase.Prepositional => stem + (isSoft ? "ем" : "ом"),
            _ => adj
        };
    }

    private static string DeclineAdjectivePlural(string adj, RussianCase targetCase, bool isSoft, bool isHissing, Animacy animacy = Animacy.Inanimate)
    {
        string stem = isSoft ? ExtractSoftAdjStem(adj) : ExtractAdjStem(adj);

        if (targetCase == RussianCase.Nominative)
            return adj; // -ые или -ие

        if (targetCase == RussianCase.Accusative)
        {
            if (animacy == Animacy.Animate)
                return stem + (isSoft ? "их" : "ых");
            else
                return adj; // неодуш. = им.п.
        }

        return targetCase switch
        {
            RussianCase.Genitive => stem + (isSoft ? "их" : "ых"),
            RussianCase.Dative => stem + (isSoft ? "им" : "ым"),
            RussianCase.Instrumental => stem + (isSoft ? "ими" : "ыми"),
            RussianCase.Prepositional => stem + (isSoft ? "их" : "ых"),
            _ => adj
        };
    }

    /// <summary>Извлечь основу прилагательного (твёрдое склонение).</summary>
    private static string ExtractAdjStem(string adj)
    {
        if (adj.EndsWith("ый")) return adj[..^2];
        if (adj.EndsWith("ой")) return adj[..^2];
        if (adj.EndsWith("ая")) return adj[..^2];
        if (adj.EndsWith("ое")) return adj[..^2];
        if (adj.EndsWith("ые")) return adj[..^2];
        if (adj.EndsWith("ую")) return adj[..^2];
        if (adj.EndsWith("ых")) return adj[..^2];
        if (adj.EndsWith("ым")) return adj[..^2];
        if (adj.EndsWith("ыми")) return adj[..^3];
        if (adj.EndsWith("ого")) return adj[..^3];
        if (adj.EndsWith("ому")) return adj[..^3];
        // fallback
        return adj;
    }

    /// <summary>Извлечь основу прилагательного (мягкое склонение).</summary>
    private static string ExtractSoftAdjStem(string adj)
    {
        if (adj.EndsWith("ий")) return adj[..^2];
        if (adj.EndsWith("яя")) return adj[..^2];
        if (adj.EndsWith("ее")) return adj[..^2];
        if (adj.EndsWith("ие")) return adj[..^2];
        if (adj.EndsWith("юю")) return adj[..^2];
        if (adj.EndsWith("их")) return adj[..^2];
        if (adj.EndsWith("им")) return adj[..^2];
        if (adj.EndsWith("ими")) return adj[..^3];
        if (adj.EndsWith("его")) return adj[..^3];
        if (adj.EndsWith("ему")) return adj[..^3];
        if (adj.EndsWith("ей")) return adj[..^2];
        if (adj.EndsWith("ем")) return adj[..^2];
        // fallback
        return adj;
    }

    /// <summary>Основа мягкая? (-ий, -яя, -ее, -ие).</summary>
    public static bool IsAdjectiveSoftStem(string adj)
    {
        return adj.EndsWith("ий") || adj.EndsWith("яя") || 
               adj.EndsWith("ее") || adj.EndsWith("ие");
    }

    /// <summary>Основа на шипящую?</summary>
    public static bool IsAdjectiveHissingStem(string adj)
    {
        // Прилагательные на -ший, -щий, -жий и т.п.
        if (adj.Length < 3) return false;
        // Проверяем последнюю согласную основы
        if (adj.EndsWith("ий") || adj.EndsWith("ый") || adj.EndsWith("ой"))
        {
            char c = adj[^3]; // буква перед окончанием
            return IsHissing(c);
        }
        if (adj.EndsWith("ая") || adj.EndsWith("яя") || 
            adj.EndsWith("ое") || adj.EndsWith("ее") ||
            adj.EndsWith("ые") || adj.EndsWith("ие"))
        {
            char c = adj[^3];
            return IsHissing(c);
        }
        return false;
    }

    // ────────── Склонение фраз ──────────

    /// <summary>Просклонять всю фразу.</summary>
    public static string DeclinePhrase(string phrase, RussianCase targetCase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return phrase;

        var words = RussianAnalyzer.Tokenize(phrase);
        if (words.Length == 0) return phrase;

        // Анализируем каждое слово
        var wordInfos = words.Select(RussianAnalyzer.Analyze).ToArray();

        // Находим главное существительное (первое существительное)
        int mainNounIndex = -1;
        for (int i = 0; i < wordInfos.Length; i++)
        {
            if (wordInfos[i].PartOfSpeech == PartOfSpeech.Noun)
            {
                mainNounIndex = i;
                break;
            }
        }

        var results = new List<string>();

        for (int i = 0; i < wordInfos.Length; i++)
        {
            var info = wordInfos[i];
            
            if (info.PartOfSpeech == PartOfSpeech.Preposition)
            {
                // Предлоги могут требовать определённого падежа, но мы просто оставляем
                results.Add(info.Original);
            }
            else if (info.PartOfSpeech == PartOfSpeech.Conjunction ||
                     info.PartOfSpeech == PartOfSpeech.Other)
            {
                results.Add(info.Original);
            }
            else if (info.PartOfSpeech == PartOfSpeech.Noun)
            {
                results.Add(DeclineNoun(info, targetCase));
            }
            else if (info.PartOfSpeech == PartOfSpeech.Adjective)
            {
                // Согласуем прилагательное с существительным
                GrammaticalGender gender = GrammaticalGender.Masculine;
                GrammaticalNumber number = GrammaticalNumber.Singular;
                Animacy animacy = Animacy.Inanimate;
                
                if (mainNounIndex >= 0)
                {
                    var nounInfo = wordInfos[mainNounIndex];
                    gender = nounInfo.Gender;
                    animacy = nounInfo.Animacy;
                    // Упрощённо: считаем единственное число
                    number = GrammaticalNumber.Singular;
                }
                
                results.Add(DeclineAdjective(info.Original, gender, number, targetCase, animacy));
            }
            else
            {
                results.Add(info.Original);
            }
        }

        // Собираем фразу с учётом оригинальных пробелов
        return ReconstructPhrase(phrase, results.ToArray());
    }

    /// <summary>Восстанавливает фразу с оригинальным форматированием.</summary>
    private static string ReconstructPhrase(string original, string[] declinedWords)
    {
        // Разделяем оригинал на части, сохраняя разделители
        var parts = System.Text.RegularExpressions.Regex.Split(original, 
            @"([\s,\.!?\:;\(\)\[\]""«»\-]+)").Where(p => p.Length > 0).ToArray();
        
        int wordIndex = 0;
        var result = new System.Text.StringBuilder();
        
        foreach (var part in parts)
        {
            // Если это разделитель — просто добавляем
            if (System.Text.RegularExpressions.Regex.IsMatch(part, @"^[\s,\.!?\:;\(\)\[\]""«»\-]+$"))
            {
                result.Append(part);
            }
            else if (wordIndex < declinedWords.Length)
            {
                // Восстанавливаем заглавную букву (первая буква фразы)
                string declined = declinedWords[wordIndex];
                if (char.IsUpper(part[0]))
                {
                    declined = char.ToUpper(declined[0]) + declined[1..];
                }
                result.Append(declined);
                wordIndex++;
            }
            else
            {
                result.Append(part);
            }
        }

        return result.ToString();
    }

    // ────────── Вспомогательные ──────────

    private static bool IsHissing(char c) =>
        c is 'ж' or 'ш' or 'ч' or 'щ';
}