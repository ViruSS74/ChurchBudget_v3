using System;

public static class ReportHelper
{
    public static string FormatShortName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "________________";
        var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3)
            return $"{parts[1][0]}.{parts[2][0]}. {parts[0]}"; // И.И. Иванов
        if (parts.Length == 2)
            return $"{parts[1][0]}. {parts[0]}";
        return fullName;
    }

    public static string NumberToWords(int number)
    {
        string[] ones = { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
        string[] teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
        string[] tens = { "", "", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };

        if (number == 0) return "ноль";
        if (number < 10) return ones[number];
        if (number < 20) return teens[number - 10];
        if (number < 100) return tens[number / 10] + (number % 10 > 0 ? " " + ones[number % 10] : "");
        return number.ToString();
    }
}
