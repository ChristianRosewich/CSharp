# B0006-Testable-CSharp-Generation

## Parent
- Feature: F0001-IEC-to-CSharp-Testability

## Description
Generate testable C# artifacts for the supported IEC subset with a structure that preserves deterministic execution and enables direct unit testing.

## Value
This item delivers the first usable bridge from IEC source semantics to executable C# code.

## Scope
- Generate C# for supported `FUNCTION`, `FUNCTION_BLOCK`, and `PROGRAM` shapes
- Preserve deterministic state transitions and cycle-based execution behavior
- Keep generated code suitable for direct MSTest-based assertions

## Out of Scope
- Full optimization or formatting customization
- Unsupported IEC language areas

## Acceptance Criteria
- Supported IEC input generates compilable C# output
- Generated code can be executed in tests with deterministic results
- Output structure matches the execution model defined earlier in the roadmap

## Assumptions
- Semantic analysis provides the information needed for generation

## Open Questions
- Whether generation should target source files, Roslyn syntax trees, or both
- Which namespace and type layout conventions should be used for generated artifacts
