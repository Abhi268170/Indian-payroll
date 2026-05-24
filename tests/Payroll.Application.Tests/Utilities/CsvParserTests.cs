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

    [Fact]
    public void SplitIntoChunks_RowsExactMultiple_ProducesEvenChunks()
    {
        List<string[]> rows = Enumerable.Range(1, 10).Select(i => new[] { $"EMP{i:000}" }).ToList();
        List<IReadOnlyList<string[]>> chunks = CsvParser.SplitIntoChunks(rows, 5).ToList();
        chunks.Should().HaveCount(2);
        chunks[0].Should().HaveCount(5);
        chunks[1].Should().HaveCount(5);
    }

    [Fact]
    public void SplitIntoChunks_RowsNotMultiple_LastChunkSmaller()
    {
        List<string[]> rows = Enumerable.Range(1, 7).Select(i => new[] { $"EMP{i:000}" }).ToList();
        List<IReadOnlyList<string[]>> chunks = CsvParser.SplitIntoChunks(rows, 5).ToList();
        chunks.Should().HaveCount(2);
        chunks[0].Should().HaveCount(5);
        chunks[1].Should().HaveCount(2);
    }

    [Fact]
    public void SplitIntoChunks_FewerRowsThanChunkSize_SingleChunk()
    {
        List<string[]> rows = Enumerable.Range(1, 3).Select(i => new[] { $"EMP{i:000}" }).ToList();
        List<IReadOnlyList<string[]>> chunks = CsvParser.SplitIntoChunks(rows, 500).ToList();
        chunks.Should().HaveCount(1);
        chunks[0].Should().HaveCount(3);
    }

    [Fact]
    public void ReconstructCsv_Roundtrip_ParseProducesOriginalRows()
    {
        List<string[]> original = [["EMP001", "BONUS", "5000"], ["EMP002", "LEAVE ENCASHMENT", "3000"]];
        string csv = CsvParser.ReconstructCsv(original);
        IReadOnlyList<string[]> parsed = CsvParser.Parse(csv);
        parsed.Should().HaveCount(2);
        parsed[0].Should().Equal("EMP001", "BONUS", "5000");
        parsed[1].Should().Equal("EMP002", "LEAVE ENCASHMENT", "3000");
    }

    [Fact]
    public void ReconstructCsv_FieldWithComma_Escaped()
    {
        List<string[]> rows = [["EMP001", "Smith, John", "1000"]];
        string csv = CsvParser.ReconstructCsv(rows);
        IReadOnlyList<string[]> parsed = CsvParser.Parse(csv);
        parsed[0][1].Should().Be("Smith, John");
    }
}
