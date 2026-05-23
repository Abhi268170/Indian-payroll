using FluentAssertions;
using Payroll.Application.Utilities;
using Xunit;

namespace Payroll.Application.Tests.Utilities;

public class CsvParserTests
{
    [Fact]
    public void Parse_BasicCsv_ReturnsDataRowsOnly()
    {
        string csv = "Employee Code,LOP Days\nEMP001,3\nEMP002,2";
        IReadOnlyList<string[]> rows = CsvParser.Parse(csv);
        rows.Should().HaveCount(2);
        rows[0].Should().Equal("EMP001", "3");
        rows[1].Should().Equal("EMP002", "2");
    }

    [Fact]
    public void Parse_BomPrefixed_StripsBoM()
    {
        string csv = "﻿Employee Code,LOP Days\nEMP001,3";
        IReadOnlyList<string[]> rows = CsvParser.Parse(csv);
        rows.Should().HaveCount(1);
        rows[0][0].Should().Be("EMP001");
    }

    [Fact]
    public void Parse_QuotedFieldWithComma_ParsedAsSingleCell()
    {
        string csv = "Name,Amount\n\"Smith, John\",1000";
        IReadOnlyList<string[]> rows = CsvParser.Parse(csv);
        rows[0][0].Should().Be("Smith, John");
        rows[0][1].Should().Be("1000");
    }

    [Fact]
    public void Parse_EscapedDoubleQuote_UnescapedInResult()
    {
        string csv = "Code,Name\nB001,\"O\"\"Brien\"";
        IReadOnlyList<string[]> rows = CsvParser.Parse(csv);
        rows[0][1].Should().Be("O\"Brien");
    }

    [Fact]
    public void Parse_BlankRowsInterspersed_Skipped()
    {
        string csv = "Code,Days\n\nEMP001,2\n\nEMP002,1\n";
        IReadOnlyList<string[]> rows = CsvParser.Parse(csv);
        rows.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_WhitespacePaddedCells_Trimmed()
    {
        string csv = "Code,Days\n  EMP001  ,  3  ";
        IReadOnlyList<string[]> rows = CsvParser.Parse(csv);
        rows[0][0].Should().Be("EMP001");
        rows[0][1].Should().Be("3");
    }

    [Fact]
    public void Parse_HeaderOnly_ReturnsEmpty()
    {
        string csv = "Employee Code,LOP Days";
        IReadOnlyList<string[]> rows = CsvParser.Parse(csv);
        rows.Should().BeEmpty();
    }

    [Fact]
    public void Parse_EmptyString_ReturnsEmpty()
    {
        IReadOnlyList<string[]> rows = CsvParser.Parse(string.Empty);
        rows.Should().BeEmpty();
    }

    [Fact]
    public void Parse_SingleColumnCsv_EachRowHasOneElement()
    {
        string csv = "Code\nEMP001\nEMP002";
        IReadOnlyList<string[]> rows = CsvParser.Parse(csv);
        rows.Should().HaveCount(2);
        rows[0].Should().HaveCount(1);
        rows[0][0].Should().Be("EMP001");
    }

    [Fact]
    public void Parse_CrLfLineEndings_ParsedCorrectly()
    {
        string csv = "Code,Days\r\nEMP001,3\r\nEMP002,1";
        IReadOnlyList<string[]> rows = CsvParser.Parse(csv);
        rows.Should().HaveCount(2);
        rows[0].Should().Equal("EMP001", "3");
    }

    [Fact]
    public void Parse_ThreeColumnRow_AllFieldsReturned()
    {
        string csv = "Employee Code,Component Code,Amount\nEMP001,BONUS,5000\nEMP002,COMMISSION,3000";
        IReadOnlyList<string[]> rows = CsvParser.Parse(csv);
        rows.Should().HaveCount(2);
        rows[0].Should().Equal("EMP001", "BONUS", "5000");
        rows[1].Should().Equal("EMP002", "COMMISSION", "3000");
    }
}
