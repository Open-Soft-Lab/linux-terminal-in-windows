using System;

public static class Logger
{
    // Цвета для разных типов сообщений
    private static readonly ConsoleColor ErrorColor = ConsoleColor.Red;
    private static readonly ConsoleColor WarningColor = ConsoleColor.Yellow;
    private static readonly ConsoleColor InfoColor = ConsoleColor.Green;
    private static readonly ConsoleColor DebugColor = ConsoleColor.Blue;

    /// <summary>
    /// Выводит сообщение об ошибке.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    public static void Error(string message)
    {
        WriteColoredMessage(message, ErrorColor);
    }

    /// <summary>
    /// Выводит предупреждение.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    public static void Warning(string message)
    {
        WriteColoredMessage(message, WarningColor);
    }

    /// <summary>
    /// Выводит информационное сообщение.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    public static void Info(string message)
    {
        WriteColoredMessage(message, InfoColor);
    }

    /// <summary>
    /// Выводит отладочное сообщение.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    public static void Debug(string message)
    {
        WriteColoredMessage(message, DebugColor);
    }

    /// <summary>
    /// Выводит сообщение с указанным цветом.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="color">Цвет текста.</param>
    private static void WriteColoredMessage(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color; // Устанавливаем цвет текста
        Console.WriteLine(message);      // Выводим сообщение
        Console.ResetColor();            // Сбрасываем цвет текста
    }
}