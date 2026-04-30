namespace TranspilerLib.IEC.Models.Ast;

/// <summary>
/// Represents the base type for all typed IEC abstract syntax tree nodes.
/// The node stores the original source position when it is known so later
/// semantic analysis and diagnostics can reference the source input.
/// </summary>
public abstract class IecAstNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IecAstNode"/> class.
    /// </summary>
    /// <param name="sourcePos">Zero-based source position or a negative value when unknown.</param>
    protected IecAstNode(int sourcePos)
    {
        SourcePos = sourcePos;
    }

    /// <summary>
    /// Gets the zero-based character position in the source input.
    /// A negative value indicates that the position is currently unknown.
    /// </summary>
    public int SourcePos { get; }
}
