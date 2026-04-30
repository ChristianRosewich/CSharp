using System;
using System.Collections.Generic;
using System.Linq;

namespace TranspilerLib.IEC.Models.Ast;

/// <summary>
/// Extracts typed IEC declarations from declaration text blocks.
/// The first implementation slice targets exported method/interface text and keeps the
/// parsing rules intentionally small and deterministic for the current test fixtures.
/// </summary>
public static class IecDeclarationExtractor
{
    /// <summary>
    /// Extracts typed variable declarations from IEC declaration text.
    /// </summary>
    /// <param name="declarationText">The raw declaration text.</param>
    /// <returns>The extracted declarations.</returns>
    public static IReadOnlyList<IecVariableDeclaration> ExtractDeclarations(string declarationText)
    {
        var declarations = new List<IecVariableDeclaration>();
        var lines = declarationText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n');

        var currentSection = IecDeclarationSection.Unknown;
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmedLine = RemoveInlineComment(lines[i]).Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            currentSection = trimmedLine.ToUpperInvariant() switch
            {
                "VAR_INPUT" => IecDeclarationSection.Input,
                "VAR_OUTPUT" => IecDeclarationSection.Output,
                "VAR_IN_OUT" => IecDeclarationSection.InOut,
                "VAR_INST" => IecDeclarationSection.Instance,
                "VAR" => IecDeclarationSection.Local,
                "END_VAR" => IecDeclarationSection.Unknown,
                _ => currentSection,
            };

            if (currentSection == IecDeclarationSection.Unknown || !trimmedLine.Contains(':', StringComparison.Ordinal))
            {
                continue;
            }

            var colonIndex = trimmedLine.IndexOf(':', StringComparison.Ordinal);
            var identifier = trimmedLine[..colonIndex].Trim();
            if (string.IsNullOrWhiteSpace(identifier))
            {
                continue;
            }

            var remainder = trimmedLine[(colonIndex + 1)..].Trim().TrimEnd(';').Trim();
            string? initializerText = null;
            var initializerIndex = remainder.IndexOf(":=", StringComparison.Ordinal);
            if (initializerIndex >= 0)
            {
                initializerText = remainder[(initializerIndex + 2)..].Trim();
                remainder = remainder[..initializerIndex].Trim();
            }

            var typeName = string.IsNullOrWhiteSpace(remainder) ? null : remainder;
            declarations.Add(new IecVariableDeclaration(identifier, typeName, currentSection, initializerText, i));
        }

        return declarations;
    }

    /// <summary>
    /// Creates a lightweight compilation unit from declaration and implementation text.
    /// The current implementation extracts declarations and leaves executable statements
    /// to the existing statement parsing pipeline.
    /// </summary>
    /// <param name="declarationText">The raw declaration text.</param>
    /// <param name="implementationText">The raw implementation text.</param>
    /// <returns>The typed IEC compilation unit.</returns>
    public static IecCompilationUnit CreateCompilationUnit(string declarationText, string implementationText)
    {
        _ = implementationText;
        var declarations = ExtractDeclarations(declarationText);
        return new IecCompilationUnit(declarations, Array.Empty<IecStatement>());
    }

    private static string RemoveInlineComment(string line)
    {
        var commentIndex = line.IndexOf("//", StringComparison.Ordinal);
        return commentIndex >= 0 ? line[..commentIndex] : line;
    }
}
