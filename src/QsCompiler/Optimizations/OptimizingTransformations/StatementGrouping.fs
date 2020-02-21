// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open Microsoft.Quantum.QsCompiler.Experimental.OptimizationTools
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations


/// The SyntaxTreeTransformation used to reorder statements depending on how they impact the program state. 
type StatementGrouping private (unsafe : string) =
    inherit TransformationBase()

    new () as this = 
        new StatementGrouping("unsafe") then
            this.Statements <- new StatementGroupingStatements(this)

/// private helper class for StatementGrouping
and private StatementGroupingStatements (parent : StatementGrouping) = 
    inherit Core.StatementTransformation(parent)

    /// Returns whether a statements is purely classical.
    /// The statement must have no classical or quantum side effects other than defining a variable.
    let isPureClassical stmt =
        let c = SideEffectChecker()
        c.Statements.onStatement stmt |> ignore
        not c.HasQuantum && not c.HasMutation && not c.HasInterrupts

    /// Returns whether a statement is purely quantum.
    /// The statement must have no classical side effects, but can have quantum side effects.
    let isPureQuantum stmt =
        let c = SideEffectChecker()
        c.Statements.onStatement stmt |> ignore
        c.HasQuantum && not c.HasMutation && not c.HasInterrupts

    /// Reorders a list of statements such that the pure classical statements occur before the pure quantum statements
    let rec reorderStatements = function
        | a :: b :: tail ->
            if isPureQuantum a && isPureClassical b
            then b :: reorderStatements (a :: tail)
            else a :: reorderStatements (b :: tail)
        | x -> x

    override this.Transform scope =
        let parentSymbols = scope.KnownSymbols
        let statements = scope.Statements |> Seq.map this.onStatement |> List.ofSeq |> reorderStatements
        QsScope.New (statements, parentSymbols)

