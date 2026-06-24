using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleAutomaticStorageSystem.Stub.Tools;

internal class ConsoleInput
{
    public bool InputAction(string message)
    {
        Console.CursorVisible = true;
        Console.WriteLine(message);
        Console.Write("＞");
        while (true)
        {
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Escape)
            {
                Console.CursorVisible = false;
                Console.WriteLine();
                return false;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                Console.CursorVisible = false;
                Console.WriteLine();
                return true;
            }
        }
    }

    public string? InputString()
    {
        string? input = "";

        while (true)
        {
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Escape)
            {
                return null;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return input;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (input.Length > 0)
                {
                    input = input[..^1];
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                input += key.KeyChar;
                Console.Write(key.KeyChar);
            }
        }
    }
}
