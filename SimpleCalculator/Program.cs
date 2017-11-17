using System;

namespace SimpleCalculator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Введите выражение");
            string old_expression = Console.ReadLine();
            string new_expression = old_expression.Replace(',', '.');
            new_expression = new_expression.Replace('x', '*');
            new_expression = new_expression.Replace('х', '*');


            char[] oldBrackets_open = { '[', '{' };
            char[] oldBrackets_close = { ']', '}' };
            char newBrackets_open = '(';
            char newBrackets_close = ')';

            for (int i = 0; i < oldBrackets_open.Length; i++)
            {
                new_expression = new_expression.Replace(oldBrackets_open[i], newBrackets_open);
                new_expression = new_expression.Replace(oldBrackets_close[i], newBrackets_close);
            }

            try
            { Console.WriteLine("Ответ : " + Result(new_expression)); }
            catch (ArgumentException)
            { Console.Write("Неправильное выражение"); }
            Console.ReadKey();

            decimal Result(string s)
            {
                var result = new Calculator();
                return result.Result(s);
            }
        }
    }
}
