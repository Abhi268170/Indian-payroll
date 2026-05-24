using Payroll.Application.Services;
using Xunit;

namespace Payroll.Application.Tests.PayrollRun;

public sealed class IndianNumberToWordsTests
{
    [Theory]
    [InlineData(65484, "Indian Rupee Sixty Five Thousand Four Hundred Eighty Four Only")]
    [InlineData(0, "Indian Rupee Zero Only")]
    [InlineData(100, "Indian Rupee One Hundred Only")]
    [InlineData(1000, "Indian Rupee One Thousand Only")]
    [InlineData(100000, "Indian Rupee One Lakh Only")]
    [InlineData(10000000, "Indian Rupee One Crore Only")]
    public void Convert_ReturnsExpectedWords(decimal amount, string expected)
    {
        string result = IndianNumberToWords.Convert(amount);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_WithPaise_AppendsPaiseWords()
    {
        string result = IndianNumberToWords.Convert(1000.50m);
        Assert.Contains("Fifty Paise", result);
    }
}
