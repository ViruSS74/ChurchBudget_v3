using System;

namespace ChurchBudget_v3_WPF.Reports
{
    public class CashOrderInDataModel
    {
        public string OrganizationName { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string FromPersonGenitive { get; set; }
        public string Basis { get; set; }
        public decimal Amount { get; set; }
        public string Appendix { get; set; }
        public string RectorName { get; set; }
        public string TreasurerName { get; set; }
        public string PassportInfo { get; set; }

        // Автономный и стопроцентный перевод суммы в настоящую пропись
        public string AmountInWords
        {
            get
            {
                long rubles = (long)Math.Floor(Amount);
                int kopecks = (int)Math.Round((Amount - rubles) * 100);

                string rublesText = RuCurrencyStr(rubles);
                return $"{rublesText} {kopecks:D2} коп.";
            }
        }

        // Локальный чистый метод прописи чисел для РБ/РФ стандартов кассы
        private static string RuCurrencyStr(long val)
        {
            if (val == 0) return "ноль рублей";

            string[] ones = { "", "один", "два", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            string[] onesFem = { "", "одна", "две", "три", "четыре", "пять", "шесть", "семь", "восемь", "девять" };
            string[] teens = { "десять", "одиннадцать", "двенадцать", "тринадцать", "четырнадцать", "пятнадцать", "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать" };
            string[] tens = { "", "десять", "двадцать", "тридцать", "сорок", "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто" };
            string[] hundreds = { "", "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот" };

            string result = "";

            // Тысячи
            if (val >= 1000)
            {
                long thousands = val / 1000;
                val %= 1000;

                if (thousands >= 100) { result += hundreds[thousands / 100] + " "; thousands %= 100; }
                if (thousands >= 10 && thousands < 20) { result += teens[thousands - 10] + " "; thousands = 0; }
                else
                {
                    if (thousands >= 20) { result += tens[thousands / 10] + " "; thousands %= 10; }
                    if (thousands > 0) result += onesFem[thousands] + " ";
                }

                long tMod = thousands % 10;
                if (thousands == 1) result += "тысяча ";
                else if (thousands >= 2 && thousands <= 4) result += "тысячи ";
                else result += "тысяч ";
            }

            // Сотни, десятки, единицы рублей
            if (val >= 100) { result += hundreds[val / 100] + " "; val %= 100; }
            if (val >= 10 && val < 20) { result += teens[val - 10] + " "; val = 0; }
            else
            {
                if (val >= 20) { result += tens[val / 10] + " "; val %= 10; }
                if (val > 0) result += ones[val] + " ";
            }

            // Склонение слова "рублей"
            long rMod = val % 10;
            if (val == 1) result += "рубль";
            else if (val >= 2 && val <= 4) result += "рубля";
            else result += "рублей";

            // Делаем первую букву заглавной
            result = result.Trim();
            if (result.Length > 0)
                result = char.ToUpper(result[0]) + result.Substring(1);

            return result;
        }
    }
}
