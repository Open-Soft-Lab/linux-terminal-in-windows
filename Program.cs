using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

class EnhancedLinuxLikeCmd
{
    private static List<string> commandHistory = new List<string>();
    private static int historyIndex = -1;

    static void Main(string[] args)
    {
        string currentDirectory = Directory.GetCurrentDirectory();

        Logger.Info("Улучшенная командная строка запущена.");

        while (true)
        {
            Console.Write($"{currentDirectory}$ ");
            string? input = ReadInputWithHistory();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            commandHistory.Add(input);
            historyIndex = commandHistory.Count;

            ExecuteCommand(input, ref currentDirectory);
        }
    }

    static string? ReadInputWithHistory()
    {
        string input = "";
        ConsoleKeyInfo keyInfo;

        while (true)
        {
            keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return input;
            }
            else if (keyInfo.Key == ConsoleKey.UpArrow)
            {
                if (historyIndex > 0)
                {
                    historyIndex--;
                    input = commandHistory[historyIndex];
                    Console.CursorLeft = 0;
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                    Console.CursorLeft = 0;
                    Console.Write($"{Directory.GetCurrentDirectory()}$ {input}");
                }
            }
            else if (keyInfo.Key == ConsoleKey.DownArrow)
            {
                if (historyIndex < commandHistory.Count - 1)
                {
                    historyIndex++;
                    input = commandHistory[historyIndex];
                    Console.CursorLeft = 0;
                    Console.Write(new string(' ', Console.WindowWidth - 1));
                    Console.CursorLeft = 0;
                    Console.Write($"{Directory.GetCurrentDirectory()}$ {input}");
                }
            }
            else if (keyInfo.Key == ConsoleKey.Tab)
            {
                input = AutoComplete.Complete(input); // Используем автодополнение
                Console.CursorLeft = 0;
                Console.Write(new string(' ', Console.WindowWidth - 1));
                Console.CursorLeft = 0;
                Console.Write($"{Directory.GetCurrentDirectory()}$ {input}");
            }
            else if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0)
                {
                    input = input.Substring(0, input.Length - 1);
                    Console.Write("\b \b");
                }
            }
            else
            {
                input += keyInfo.KeyChar;
                Console.Write(keyInfo.KeyChar);
            }
        }
    }

    static void ExecuteCommand(string input, ref string currentDirectory)
    {
        try
        {
            string[] commands = input.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            string? previousOutput = null;

            foreach (var cmd in commands)
            {
                previousOutput = ExecuteSingleCommand(cmd.Trim(), ref currentDirectory, previousOutput);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Ошибка: {ex.Message}");
        }
    }

    static string? ExecuteSingleCommand(string command, ref string currentDirectory, string? input = null)
    {
        try
        {
            string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0].ToLower();
            string args = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";

            // Обработка переключения дисков (например, "C:" или "D:")
            if (cmd.EndsWith(":"))
            {
                ChangeDrive(cmd, ref currentDirectory); // Передаём currentDirectory по ссылке
                return null;
            }

            // Обработка перенаправления вывода
            if (args.Contains(">") || args.Contains(">>"))
            {
                return HandleOutputRedirection(cmd, args, currentDirectory, input);
            }

            string? result = null;

            switch (cmd)
            {
                case "cd":
                    ChangeDirectory(ref currentDirectory, args);
                    break;
                case "ls":
                    result = ListDirectory(currentDirectory, args);
                    break;
                case "pwd":
                    result = currentDirectory;
                    break;
                case "cat":
                    result = PrintFileContent(args);
                    break;
                case "mkdir":
                    CreateDirectory(args);
                    break;
                case "rm":
                    RemoveFileOrDirectory(args);
                    break;
                case "cp":
                    CopyFileOrDirectory(args);
                    break;
                case "mv":
                    MoveFileOrDirectory(args);
                    break;
                case "grep":
                    result = ExecuteGrep(args, input ?? "");
                    break;
                case "exit":
                    Logger.Info("Завершение работы.");
                    Environment.Exit(0);
                    break;
                default:
                    result = ExecuteExternalCommand(cmd, args, currentDirectory, input);
                    break;
            }

            // Вывод результата, если он есть
            if (result != null)
            {
                Logger.Info(result);
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error($"Ошибка выполнения команды '{command}': {ex.Message}");
            return null;
        }
    }

    static void ChangeDrive(string driveLetter, ref string currentDirectory)
    {
        try
        {
            // Проверяем, что буква диска корректна (например, "C:", "D:")
            if (driveLetter.Length != 2 || driveLetter[1] != ':')
            {
                Logger.Error("Неправильный формат буквы диска. Используйте формат 'C:' или 'D:'.");
                return;
            }

            // Приводим букву диска к верхнему регистру
            driveLetter = driveLetter.ToUpper();

            // Проверяем, существует ли диск
            var drives = DriveInfo.GetDrives();
            var targetDrive = drives.FirstOrDefault(d => d.Name.StartsWith(driveLetter));

            if (targetDrive == null)
            {
                Logger.Error($"Диск {driveLetter} не найден.");
                return;
            }

            // Меняем текущий диск
            Environment.CurrentDirectory = targetDrive.RootDirectory.FullName;
            currentDirectory = Environment.CurrentDirectory; // Обновляем текущую директорию
            Logger.Info($"Переключение на диск {driveLetter} выполнено. Текущая директория: {currentDirectory}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Ошибка при переключении диска: {ex.Message}");
        }
    }

    static string? HandleOutputRedirection(string command, string args, string currentDirectory, string? input)
    {
        string[] redirectionParts = args.Split(new[] { '>', '>' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (redirectionParts.Length < 2)
        {
            Logger.Error("Неправильный синтаксис перенаправления вывода.");
            return null;
        }

        string cmdArgs = redirectionParts[0].Trim();
        string filePath = redirectionParts[1].Trim();
        bool append = args.Contains(">>");

        string? output = ExecuteSingleCommand(command, ref currentDirectory, input);

        if (output != null)
        {
            try
            {
                if (append)
                {
                    File.AppendAllText(filePath, output + Environment.NewLine);
                }
                else
                {
                    File.WriteAllText(filePath, output);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка записи в файл '{filePath}': {ex.Message}");
            }
        }

        return null;
    }

    static void ChangeDirectory(ref string currentDirectory, string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
            {
                currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            else
            {
                string newPath = Path.GetFullPath(Path.Combine(currentDirectory, path));
                if (Directory.Exists(newPath))
                {
                    currentDirectory = newPath;
                }
                else
                {
                    Logger.Error($"cd: {path}: Директория не найдена.");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"cd: {ex.Message}");
        }
    }

    static string? ListDirectory(string currentDirectory, string arguments)
    {
        try
        {
            bool detailed = arguments.Contains("-l"); // Поддержка аргумента -l

            string[] filesAndDirs = Directory.GetFileSystemEntries(currentDirectory);

            if (detailed)
            {
                var details = filesAndDirs.Select(path =>
                {
                    var info = new FileInfo(path);
                    return $"{info.LastWriteTime} {(info.Attributes.HasFlag(FileAttributes.Directory) ? "<DIR>" : info.Length.ToString())} {Path.GetFileName(path)}";
                });

                return string.Join(Environment.NewLine, details);
            }
            else
            {
                return string.Join(Environment.NewLine, filesAndDirs.Select(Path.GetFileName));
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"ls: {ex.Message}");
            return null;
        }
    }

    static string? PrintFileContent(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
            else
            {
                Logger.Error($"cat: {filePath}: Файл не найден.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"cat: {ex.Message}");
            return null;
        }
    }

    static void CreateDirectory(string directoryPath)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);
            Logger.Info($"Директория '{directoryPath}' создана.");
        }
        catch (Exception ex)
        {
            Logger.Error($"mkdir: {ex.Message}");
        }
    }

    static void RemoveFileOrDirectory(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                Logger.Info($"Файл '{path}' удалён.");
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Logger.Info($"Директория '{path}' удалена.");
            }
            else
            {
                Logger.Error($"rm: {path}: Файл или директория не найдены.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"rm: {ex.Message}");
        }
    }

    static void CopyFileOrDirectory(string arguments)
    {
        try
        {
            string[] parts = arguments.Split(new[] { ' ' }, 2);
            if (parts.Length < 2)
            {
                Logger.Error("cp: Недостаточно аргументов.");
                return;
            }

            string source = parts[0];
            string destination = parts[1];

            if (File.Exists(source))
            {
                File.Copy(source, destination);
                Logger.Info($"Файл '{source}' скопирован в '{destination}'.");
            }
            else if (Directory.Exists(source))
            {
                CopyDirectory(source, destination);
                Logger.Info($"Директория '{source}' скопирована в '{destination}'.");
            }
            else
            {
                Logger.Error($"cp: {source}: Файл или директория не найдены.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"cp: {ex.Message}");
        }
    }

    static void MoveFileOrDirectory(string arguments)
    {
        try
        {
            string[] parts = arguments.Split(new[] { ' ' }, 2);
            if (parts.Length < 2)
            {
                Logger.Error("mv: Недостаточно аргументов.");
                return;
            }

            string source = parts[0];
            string destination = parts[1];

            if (File.Exists(source))
            {
                File.Move(source, destination);
                Logger.Info($"Файл '{source}' перемещён в '{destination}'.");
            }
            else if (Directory.Exists(source))
            {
                Directory.Move(source, destination);
                Logger.Info($"Директория '{source}' перемещена в '{destination}'.");
            }
            else
            {
                Logger.Error($"mv: {source}: Файл или директория не найдены.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"mv: {ex.Message}");
        }
    }

    static void CopyDirectory(string sourceDir, string destinationDir)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDir);
        DirectoryInfo[] dirs = dir.GetDirectories();

        if (!Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string tempPath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(tempPath, false);
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string tempPath = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, tempPath);
        }
    }

    static string? ExecuteGrep(string args, string input)
    {
        try
        {
            string[] parts = args.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
            {
                Logger.Error("grep: Не указан шаблон.");
                return null;
            }

            string pattern = parts[0];
            string text = input;

            var matchingLines = text.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                .Where(line => line.Contains(pattern))
                .ToList();

            return string.Join(Environment.NewLine, matchingLines);
        }
        catch (Exception ex)
        {
            Logger.Error($"grep: {ex.Message}");
            return null;
        }
    }

    static string? ExecuteExternalCommand(string command, string arguments, string workingDirectory, string? input = null)
    {
        try
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = input != null,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(processStartInfo)!)
            {
                if (input != null)
                {
                    process.StandardInput.WriteLine(input);
                    process.StandardInput.Close();
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(error))
                {
                    Logger.Error(error);
                }

                return output;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{command}: {ex.Message}");
            return null;
        }
    }
}