using System;
using System.Globalization;
using System.Windows.Data;

namespace ChurchBudget_v3_WPF.Services
{
    public class ConvertService
    {
        // ВАШ СУЩЕСТВУЮЩИЙ МЕТОД
        public decimal SafeToDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0.00m;
            decimal.TryParse(value.ToString().Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result);
            return result;
        }

        // Основная логика конвертации (работает с decimal для точности)
        public static string CurrencyToWords(decimal amount)
        {
            long rub = (long)Math.Floor(amount);
            int kop = (int)Math.Round((double)(amount - rub) * 100);

            if (rub == 0)
                return "Ноль рублей " + kop.ToString("D2") + " коп.";

            string[] ones = { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            string[] onesThousands = { "", "одна", "две", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            string[] tens = { "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
            string[] teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
            string[] hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };

            string result = "";

            // 1. Обработка тысяч (до 100 000 рублей)
            if (rub >= 1000)
            {
                long thousands = rub / 1000;
                rub %= 1000;

                if (thousands >= 10 && thousands < 20)
                {
                    result += teens[thousands - 10] + " тысяч ";
                }
                else
                {
                    if (thousands >= 20)
                    {
                        result += tens[thousands / 10] + " ";
                        thousands %= 10;
                    }

                    if (thousands > 0)
                    {
                        result += onesThousands[thousands] + " ";
                    }

                    if (thousands == 1) result += "тысяча ";
                    else if (thousands >= 2 && thousands <= 4) result += "тысячи ";
                    else result += "тысяч ";
                }
            }

            // 2. Обработка сотен
            if (rub >= 100)
            {
                result += hundreds[rub / 100] + " ";
                rub %= 100;
            }

            // 3. Обработка десятков и единиц
            if (rub >= 10 && rub < 20)
            {
                result += teens[rub - 10] + " ";
            }
            else
            {
                if (rub >= 20)
                {
                    result += tens[rub / 10] + " ";
                    rub %= 10;
                }

                if (rub > 0)
                {
                    result += ones[rub] + " ";
                }
            }

            // Делаем первую букву заглавной для красоты
            string finalResult = result.Trim();
            if (!string.IsNullOrEmpty(finalResult))
            {
                finalResult = char.ToUpper(finalResult[0]) + finalResult.Substring(1);
            }

            return finalResult + " руб. " + kop.ToString("D2") + " коп.";
        }

        // Перегрузка для поддержки типа double (для совместимости со старым кодом)
        public string CurrencyToWords(double amount)
        {
            return CurrencyToWords((decimal)amount);
        }
    }
}