using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TranspilerLib.Data;
using TranspilerLib.Interfaces.Code;
using TranspilerLib.IEC.Models.Scanner;

namespace TranspilerLib.IEC.Models.Ast;

/// <summary>
/// Provides the first integration point between the existing IEC code block model
/// and the new typed IEC AST model.
/// </summary>
public static class IecAstMapper
{
    private static readonly IReadOnlyDictionary<IecBinaryOperator, int> _binaryOperatorPrecedence = new Dictionary<IecBinaryOperator, int>
    {
        [IecBinaryOperator.Or] = 1,
        [IecBinaryOperator.And] = 2,
        [IecBinaryOperator.Equal] = 3,
        [IecBinaryOperator.NotEqual] = 3,
        [IecBinaryOperator.LessThan] = 3,
        [IecBinaryOperator.LessThanOrEqual] = 3,
        [IecBinaryOperator.GreaterThan] = 3,
        [IecBinaryOperator.GreaterThanOrEqual] = 3,
        [IecBinaryOperator.Add] = 4,
        [IecBinaryOperator.Subtract] = 4,
        [IecBinaryOperator.Multiply] = 5,
        [IecBinaryOperator.Divide] = 5,
    };

    private static readonly IReadOnlyDictionary<string, IecBinaryOperator> _binaryOperators = new Dictionary<string, IecBinaryOperator>(StringComparer.OrdinalIgnoreCase)
    {
        ["+"] = IecBinaryOperator.Add,
        ["-"] = IecBinaryOperator.Subtract,
        ["*"] = IecBinaryOperator.Multiply,
        ["/"] = IecBinaryOperator.Divide,
        ["="] = IecBinaryOperator.Equal,
        ["<>"] = IecBinaryOperator.NotEqual,
        ["<"] = IecBinaryOperator.LessThan,
        ["<="] = IecBinaryOperator.LessThanOrEqual,
        ["=<"] = IecBinaryOperator.LessThanOrEqual,
        [">"] = IecBinaryOperator.GreaterThan,
        [">="] = IecBinaryOperator.GreaterThanOrEqual,
        ["AND"] = IecBinaryOperator.And,
        ["OR"] = IecBinaryOperator.Or,
    };

    /// <summary>
    /// Tries to obtain a typed assignment statement from the supplied code block.
    /// The first implementation slice only supports already attached typed nodes.
    /// This keeps the migration incremental and avoids reparsing raw code strings during execution.
    /// </summary>
    /// <param name="block">The code block to inspect.</param>
    /// <param name="statement">Receives the typed assignment statement when available.</param>
    /// <returns><c>true</c> when a typed assignment statement was found; otherwise <c>false</c>.</returns>
    public static bool TryGetAssignmentStatement(ICodeBlock block, out IecAssignmentStatement? statement)
    {
        statement = (block as IECCodeBlock)?.AstNode as IecAssignmentStatement;
        return statement != null;
    }

    /// <summary>
    /// Tries to create and attach a typed assignment statement for the supplied IEC block.
    /// The first implementation slice supports assignments whose target is a variable-like block
    /// and whose right side can be mapped from the existing expression subtree.
    /// </summary>
    /// <param name="block">The block to inspect and enrich.</param>
    /// <returns><c>true</c> when a typed assignment statement was attached; otherwise <c>false</c>.</returns>
    public static bool TryAttachAssignmentStatement(ICodeBlock block)
    {
        if (block is not IECCodeBlock iecBlock || block.Type != CodeBlockType.Assignment)
        {
            return false;
        }

        if (block.SubBlocks.Count == 0)
        {
            return false;
        }

        if (!TryMapIdentifierExpression(block.SubBlocks[0], out var target))
        {
            return false;
        }

        if (!TryMapAssignmentValue(block, out var value))
        {
            return false;
        }

        iecBlock.AstNode = new IecAssignmentStatement(target, value, block.SourcePos);
        return true;
    }

    private static bool TryMapAssignmentValue(ICodeBlock assignmentBlock, out IecExpression? expression)
    {
        expression = null;
        if (assignmentBlock.SubBlocks.Count < 2)
        {
            return false;
        }

        if (assignmentBlock.SubBlocks.Count == 2)
        {
            return TryMapExpression(assignmentBlock.SubBlocks[1], out expression);
        }

        var candidates = assignmentBlock.SubBlocks.Skip(1).ToList();
        return TryMapExpressionGroup(candidates, out expression);
    }

    private static bool TryMapExpression(ICodeBlock block, out IecExpression? expression)
    {
        expression = null;

        if (block.Type == CodeBlockType.Operation)
        {
            if (TryMapUnaryExpression(block, out var unaryExpression))
            {
                expression = unaryExpression;
                return true;
            }

            if (TryMapBinaryExpression(block, out var binaryExpression))
            {
                expression = binaryExpression;
                return true;
            }
        }

        if (TryMapFunctionCallExpression(block, out var functionCallExpression))
        {
            expression = functionCallExpression;
            return true;
        }

        if (TryMapIdentifierExpression(block, out var identifierExpression))
        {
            expression = identifierExpression;
            return true;
        }

        if (TryMapLiteralExpression(block, out var literalExpression))
        {
            expression = literalExpression;
            return true;
        }

        return false;
    }

    private static bool TryMapExpressionGroup(IReadOnlyList<ICodeBlock> blocks, out IecExpression? expression)
    {
        expression = null;
        if (blocks.Count == 0)
        {
            return false;
        }

        if (blocks.Count == 1)
        {
            return TryMapExpression(blocks[0], out expression);
        }

        if (TryMapUnaryExpressionGroup(blocks, out var unaryExpression))
        {
            expression = unaryExpression;
            return true;
        }

        if (ContainsArgumentSeparator(blocks)
            && TryMapFunctionCallExpression(blocks, out var functionCallExpressionWithSeparators))
        {
            expression = functionCallExpressionWithSeparators;
            return true;
        }

        if (TryMapBinaryExpressionGroup(blocks, out var binaryExpression))
        {
            expression = binaryExpression;
            return true;
        }

        if (TryMapFunctionCallExpression(blocks, out var functionCallExpression))
        {
            expression = functionCallExpression;
            return true;
        }

        return false;
    }

    private static bool ContainsArgumentSeparator(IReadOnlyList<ICodeBlock> blocks)
        => blocks.Any(block => block.Type == CodeBlockType.Operation && block.Code == ",");

    private static bool TryMapUnaryExpressionGroup(IReadOnlyList<ICodeBlock> blocks, out IecUnaryExpression? expression)
    {
        expression = null;
        if (blocks.Count != 2 || blocks[0].Type != CodeBlockType.Operation)
        {
            return false;
        }

        if (!TryMapExpressionGroup(blocks.Skip(1).ToArray(), out var operandExpression))
        {
            return false;
        }

        expression = blocks[0].Code switch
        {
            "+" => new IecUnaryExpression(IecUnaryOperator.Plus, operandExpression, blocks[0].SourcePos),
            "-" => new IecUnaryExpression(IecUnaryOperator.Negate, operandExpression, blocks[0].SourcePos),
            _ => null,
        };

        return expression != null;
    }

    private static bool TryMapBinaryExpressionGroup(IReadOnlyList<ICodeBlock> blocks, out IecBinaryExpression? expression)
    {
        expression = null;
        var operatorIndex = FindSplitOperatorIndex(blocks);
        if (operatorIndex <= 0 || operatorIndex >= blocks.Count - 1)
        {
            return false;
        }

        var operatorBlock = blocks[operatorIndex];
        if (!_binaryOperators.TryGetValue(operatorBlock.Code, out var operatorType))
        {
            return false;
        }

        var leftBlocks = blocks.Take(operatorIndex).ToArray();
        var rightBlocks = blocks.Skip(operatorIndex + 1).ToArray();
        if (!TryMapExpressionGroup(leftBlocks, out var leftExpression)
            || !TryMapExpressionGroup(rightBlocks, out var rightExpression))
        {
            return false;
        }

        expression = new IecBinaryExpression(leftExpression, operatorType, rightExpression, operatorBlock.SourcePos);
        return true;
    }

    private static int FindSplitOperatorIndex(IReadOnlyList<ICodeBlock> blocks)
    {
        var bestIndex = -1;
        var bestPrecedence = int.MaxValue;

        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block.Type != CodeBlockType.Operation
                || !_binaryOperators.TryGetValue(block.Code, out var operatorType)
                || !_binaryOperatorPrecedence.TryGetValue(operatorType, out var precedence))
            {
                continue;
            }

            if (i == 0)
            {
                continue;
            }

            if (precedence <= bestPrecedence)
            {
                bestPrecedence = precedence;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static bool TryMapIdentifierExpression(ICodeBlock block, out IecIdentifierExpression? expression)
    {
        expression = null;
        if (block.Type is not CodeBlockType.Variable and not CodeBlockType.Function)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(block.Code) || block.Code.EndsWith("(", StringComparison.Ordinal))
        {
            return false;
        }

        expression = new IecIdentifierExpression(block.Code, block.SourcePos);
        return true;
    }

    private static bool TryMapLiteralExpression(ICodeBlock block, out IecLiteralExpression? expression)
    {
        expression = null;
        if (block.Type == CodeBlockType.Number)
        {
            expression = new IecLiteralExpression(ParseNumberLiteral(block.Code), block.SourcePos);
            return true;
        }

        if (block.Type == CodeBlockType.String)
        {
            expression = new IecLiteralExpression(block.Code, block.SourcePos);
            return true;
        }

        return false;
    }

    private static bool TryMapFunctionCallExpression(ICodeBlock block, out IecFunctionCallExpression? expression)
    {
        expression = null;
        if (block.Type != CodeBlockType.Function || !block.Code.EndsWith("(", StringComparison.Ordinal))
        {
            return false;
        }

        return TryMapFunctionCallExpressionCore(block.Code[..^1], block.SourcePos, block.SubBlocks, out expression);
    }

    private static bool TryMapFunctionCallExpression(IReadOnlyList<ICodeBlock> blocks, out IecFunctionCallExpression? expression)
    {
        expression = null;
        if (blocks.Count == 0 || blocks[0].Type != CodeBlockType.Function || !blocks[0].Code.EndsWith("(", StringComparison.Ordinal))
        {
            return false;
        }

        var functionBlocks = blocks[0].SubBlocks.Concat(blocks.Skip(1)).ToArray();
        return TryMapFunctionCallExpressionCore(blocks[0].Code[..^1], blocks[0].SourcePos, functionBlocks, out expression);
    }

    private static bool TryMapFunctionCallExpressionCore(string functionName, int sourcePos, IEnumerable<ICodeBlock> subBlocks, out IecFunctionCallExpression? expression)
    {
        expression = null;

        var arguments = new List<IecExpression>();
        foreach (var child in SplitFunctionArguments(subBlocks))
        {
            if (!TryMapExpressionGroup(child, out var argumentExpression))
            {
                return false;
            }

            arguments.Add(argumentExpression);
        }

        if (arguments.Count == 0)
        {
            return false;
        }

        expression = new IecFunctionCallExpression(functionName, arguments, sourcePos);
        return true;
    }

    private static IEnumerable<IReadOnlyList<ICodeBlock>> SplitFunctionArguments(IEnumerable<ICodeBlock> subBlocks)
    {
        var argument = new List<ICodeBlock>();
        foreach (var child in subBlocks)
        {
            if (child.Type == CodeBlockType.Bracket)
            {
                continue;
            }

            if (child.Type == CodeBlockType.Operation && child.Code == ",")
            {
                yield return argument.ToArray();
                argument.Clear();
                continue;
            }

            argument.Add(child);
        }

        if (argument.Count > 0)
        {
            yield return argument.ToArray();
        }
    }

    private static bool TryMapUnaryExpression(ICodeBlock block, out IecUnaryExpression? expression)
    {
        expression = null;
        if (block.SubBlocks.Count != 1)
        {
            return false;
        }

        if (!TryMapExpression(block.SubBlocks[0], out var operand))
        {
            return false;
        }

        expression = block.Code switch
        {
            "+" => new IecUnaryExpression(IecUnaryOperator.Plus, operand, block.SourcePos),
            "-" => new IecUnaryExpression(IecUnaryOperator.Negate, operand, block.SourcePos),
            _ => null,
        };

        return expression != null;
    }

    private static bool TryMapBinaryExpression(ICodeBlock block, out IecBinaryExpression? expression)
    {
        expression = null;
        if (block.SubBlocks.Count != 2)
        {
            return false;
        }

        if (!_binaryOperators.TryGetValue(block.Code, out var operatorType))
        {
            return false;
        }

        if (!TryMapExpression(block.SubBlocks[0], out var left) || !TryMapExpression(block.SubBlocks[1], out var right))
        {
            return false;
        }

        expression = new IecBinaryExpression(left, operatorType, right, block.SourcePos);
        return true;
    }

    private static object ParseNumberLiteral(string code)
    {
        var normalizedCode = code.Trim();
        var numberPart = normalizedCode.Contains('#', StringComparison.Ordinal)
            ? normalizedCode[(normalizedCode.IndexOf('#', StringComparison.Ordinal) + 1)..]
            : normalizedCode;

        if (bool.TryParse(numberPart, out var xBoolean))
        {
            return xBoolean;
        }

        if (numberPart.Contains('.', StringComparison.Ordinal)
            || numberPart.Contains('e', StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var fDouble))
            {
                return fDouble;
            }
        }

        if (long.TryParse(numberPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iLong))
        {
            if (iLong is >= int.MinValue and <= int.MaxValue)
            {
                return (int)iLong;
            }

            return iLong;
        }

        return numberPart;
    }
}
