namespace Payroll.Application.Utilities;

/// <summary>
/// Parses narrow, fixed-format CSV text into rows of string arrays.
/// Handles UTF-8 BOM, quoted fields (RFC 4180), blank lines, and whitespace trimming.
/// The first row (header) is always skipped.
/// </summary>
public static class CsvParser
{
    public static IReadOnlyList<string[]> Parse(string csvText)
    {
        if (string.IsNullOrWhiteSpace(csvText))
            return [];

        // Strip UTF-8 BOM if present
        if (csvText.StartsWith('﻿'))
            csvText = csvText[1..];

        List<string[]> rows = [];
        bool isFirstRow = true;

        foreach (string line in SplitLines(csvText))
        {
            if (isFirstRow) { isFirstRow = false; continue; }
            if (string.IsNullOrWhiteSpace(line)) continue;

            rows.Add(ParseRow(line));
        }

        return rows;
    }

    public static IEnumerable<IReadOnlyList<string[]>> SplitIntoChunks(IReadOnlyList<string[]> rows, int chunkSize = 500)
    {
        for (int i = 0; i < rows.Count; i += chunkSize)
            yield return rows.Skip(i).Take(chunkSize).ToList();
    }

    public static string ReconstructCsv(IReadOnlyList<string[]> rows)
    {
        var sb = new System.Text.StringBuilder();
        // Emit a dummy header row so Parse() can skip it
        sb.AppendLine("__header__");
        foreach (string[] row in rows)
            sb.AppendLine(string.Join(',', row.Select(EscapeField)));
        return sb.ToString();
    }

    private static string EscapeField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            return $"\"{field.Replace("\"", "\"\"")}\"";
        return field;
    }

    private static IEnumerable<string> SplitLines(string text) =>
        text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

    private static string[] ParseRow(string line)
    {
        List<string> fields = [];
        int i = 0;

        while (i <= line.Length)
        {
            string field;
            if (i < line.Length && line[i] == '"')
            {
                // Quoted field
                i++; // skip opening quote
                int start = i;
                System.Text.StringBuilder sb = new();
                while (i < line.Length)
                {
                    if (line[i] == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            // Escaped double-quote
                            sb.Append('"');
                            i += 2;
                        }
                        else
                        {
                            i++; // skip closing quote
                            break;
                        }
                    }
                    else
                    {
                        sb.Append(line[i]);
                        i++;
                    }
                }
                field = sb.ToString().Trim();
                // Skip comma separator
                if (i < line.Length && line[i] == ',') i++;
            }
            else
            {
                // Unquoted field — read until comma or end
                int start = i;
                while (i < line.Length && line[i] != ',') i++;
                field = line[start..i].Trim();
                if (i < line.Length) i++; // skip comma
                else i++; // advance past end to exit loop
            }

            fields.Add(field);

            // Exit if we consumed the whole line
            if (i >= line.Length + 1) break;
        }

        return [.. fields];
    }
}
