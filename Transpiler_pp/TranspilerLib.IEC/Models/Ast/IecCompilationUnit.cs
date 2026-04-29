using System;
using System.Collections.Generic;
using System.Linq;

namespace TranspilerLib.IEC.Models.Ast;

/// <summary>
/// Represents a typed IEC root node that groups declarations and executable statements.
/// The class intentionally keeps the first implementation slice small and focused on the
/// structures that are needed for deterministic interpretation and later code generation.
/// </summary>
public sealed class IecCompilationUnit : IecAstNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IecCompilationUnit"/> class.
    /// </summary>
    /// <param name="declarations">The declarations that belong to the compilation unit.</param>
    /// <param name="statements">The executable statements that belong to the compilation unit.</param>
    /// <param name="sourcePos">Zero-based source position or a negative value when unknown.</param>
    public IecCompilationUnit(IEnumerable<IecVariableDeclaration>? declarations = null, IEnumerable<IecStatement>? statements = null, int sourcePos = -1)
        : base(sourcePos)
    {
        Declarations = declarations?.ToArray() ?? Array.Empty<IecVariableDeclaration>();
        Statements = statements?.ToArray() ?? Array.Empty<IecStatement>();
        Symbols = new IecSymbolTable(Declarations);
    }

    /// <summary>
    /// Gets the declarations contained in the compilation unit.
    /// </summary>
    public IReadOnlyList<IecVariableDeclaration> Declarations { get; }

    /// <summary>
    /// Gets the symbol table derived from the contained declarations.
    /// </summary>
    public IecSymbolTable Symbols { get; }

    /// <summary>
    /// Gets the executable statements contained in the compilation unit.
    /// </summary>
    public IReadOnlyList<IecStatement> Statements { get; }
}
