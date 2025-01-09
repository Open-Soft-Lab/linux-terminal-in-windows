using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class AutoComplete
{
    // Список поддерживаемых команд
    private static readonly List<string> Commands = new List<string>
    {
        "cd", "ls", "pwd", "cat", "mkdir", "rm", "cp", "mv", "exit", "grep"
    };

    /// <summary>
    /// Автодополнение для команд и путей.
    /// </summary>
    /// <param name="input">Текущий ввод пользователя.</param>
    /// <returns>Автодополненная строка.</returns>
    public static string Complete(string input)
    {
        string[] parts = input.Split(' ');
        string lastPart = parts.Last();

        if (parts.Length == 1) // Автодополнение команд
        {
            var matches = Commands.Where(c => c.StartsWith(lastPart, StringComparison.OrdinalIgnoreCase)).ToList();

            if (matches.Count == 1)
            {
                return matches[0];
            }
            else if (matches.Count > 1)
            {
                Console.WriteLine();
                Console.WriteLine(string.Join(" ", matches));
                return input;
            }
        }
        else // Автодополнение путей
        {
            string path = lastPart;
            string? directory = Path.GetDirectoryName(path) ?? ".";
            string fileName = Path.GetFileName(path);

            if (Directory.Exists(directory))
            {
                var matches = Directory.GetFileSystemEntries(directory, fileName + "*")
                    .Select(Path.GetFileName)
                    .ToList();

                if (matches.Count == 1)
                {
                    parts[parts.Length - 1] = Path.Combine(directory, matches[0]!);
                    return string.Join(" ", parts);
                }
                else if (matches.Count > 1)
                {
                    Console.WriteLine();
                    Console.WriteLine(string.Join(" ", matches));
                    return input;
                }
            }
        }

        return input;
    }
}