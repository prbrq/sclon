using System.Text;
using Sclon.Morphology;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║    Склонятор — склонение фраз по падежам ║");
Console.WriteLine("╚══════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("Введите фразу (или 'выход' для выхода): ");
    Console.ResetColor();

    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (
        input.Equals("выход", StringComparison.OrdinalIgnoreCase)
        || input.Equals("exit", StringComparison.OrdinalIgnoreCase)
        || input.Equals("q", StringComparison.OrdinalIgnoreCase)
    )
    {
        break;
    }

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  Фраза: \"{input}\"");
    Console.ResetColor();
    Console.WriteLine();

    foreach (RussianCase rCase in Enum.GetValues<RussianCase>())
    {
        string declined = RussianDeclensionEngine.DeclinePhrase(input, rCase);

        string caseName = CaseHelper.GetCaseName(rCase);
        string question = CaseHelper.GetCaseQuestion(rCase);

        if (
            declined.Equals(input, StringComparison.OrdinalIgnoreCase)
            && rCase != RussianCase.Nominative
        )
        {
            // Склонение не изменилось — возможно, не удалось
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  {caseName, -14} ({question, -14}) → {declined}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"  {caseName, -14} ({question, -14}) → ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(declined);
            Console.ResetColor();
        }
    }

    Console.WriteLine();
    Console.WriteLine(new string('─', 50));
    Console.WriteLine();
}
