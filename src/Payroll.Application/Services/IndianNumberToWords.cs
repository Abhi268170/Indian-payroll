namespace Payroll.Application.Services;

public static class IndianNumberToWords
{
    private static readonly string[] Ones =
    [
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen",
        "Sixteen", "Seventeen", "Eighteen", "Nineteen"
    ];

    private static readonly string[] Tens =
    [
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    ];

    public static string Convert(decimal amount)
    {
        long rupees = (long)Math.Floor(amount);
        int paise = (int)Math.Round((amount - rupees) * 100);

        if (rupees == 0 && paise == 0)
            return "Indian Rupee Zero Only";

        string rupeeWords = ConvertToWords(rupees);
        string result = $"Indian Rupee {rupeeWords}";
        if (paise > 0)
            result += $" And {ConvertToWords(paise)} Paise";
        return result + " Only";
    }

    private static string ConvertToWords(long number)
    {
        if (number == 0) return "Zero";

        string words = "";

        if (number >= 10_00_00_000L)
        {
            words += ConvertToWords(number / 10_00_00_000L) + " Hundred Crore ";
            number %= 10_00_00_000L;
        }
        if (number >= 1_00_00_000L)
        {
            words += ConvertToWords(number / 1_00_00_000L) + " Crore ";
            number %= 1_00_00_000L;
        }
        if (number >= 1_00_000L)
        {
            words += ConvertToWords(number / 1_00_000L) + " Lakh ";
            number %= 1_00_000L;
        }
        if (number >= 1_000L)
        {
            words += ConvertToWords(number / 1_000L) + " Thousand ";
            number %= 1_000L;
        }
        if (number >= 100L)
        {
            words += Ones[number / 100] + " Hundred ";
            number %= 100;
        }
        if (number >= 20)
        {
            words += Tens[number / 10] + " ";
            number %= 10;
        }
        if (number > 0)
            words += Ones[number] + " ";

        return words.Trim();
    }
}
