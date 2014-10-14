﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Class FlowAnalysisPass

        ''' <summary>
        ''' The flow analysis pass.  This pass reports required diagnostics for unreachable
        ''' statements and uninitialized variables (through the call to FlowAnalysisWalker.Analyze).
        ''' </summary>
        ''' <param name = "method">the method to be analyzed</param>
        ''' <param name = "block">the method's body</param>
        ''' <param name = "diagnostics">the receiver of the reported diagnostics</param>
        Public Shared Sub Analyze(method As MethodSymbol, block As BoundBlock, diagnostics As DiagnosticBag)
            Dim compilation = method.DeclaringCompilation
            Dim sourceMethod As SourceMethodSymbol = TryCast(method, SourceMethodSymbol)
            Analyze(compilation, method, block, diagnostics)
        End Sub

        Private Shared Sub Analyze(compilation As VBCompilation, method As MethodSymbol, block As BoundBlock, diagnostics As DiagnosticBag)
            ControlFlowPass.Analyze(New FlowAnalysisInfo(compilation, method, block), diagnostics, True)
            DataFlowPass.Analyze(New FlowAnalysisInfo(compilation, method, block), diagnostics, True)
        End Sub

    End Class

End Namespace
