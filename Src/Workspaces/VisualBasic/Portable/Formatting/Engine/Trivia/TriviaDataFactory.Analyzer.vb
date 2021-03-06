﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class TriviaDataFactory
        Private Class Analyzer
            Public Shared Function Leading(token As SyntaxToken) As AnalysisResult
                Dim result As AnalysisResult

                Analyze(token.LeadingTrivia, result)
                Return result
            End Function

            Public Shared Function Trailing(token As SyntaxToken) As AnalysisResult
                Dim result As AnalysisResult

                Analyze(token.TrailingTrivia, result)
                Return result
            End Function

            Public Shared Function Between(token1 As SyntaxToken, token2 As SyntaxToken) As AnalysisResult
                If (Not token1.HasTrailingTrivia) AndAlso (Not token2.HasLeadingTrivia) Then
                    Return Nothing
                End If

                Dim result As AnalysisResult

                Analyze(token1.TrailingTrivia, result)
                Analyze(token2.LeadingTrivia, result)
                Return result
            End Function

            Private Shared Sub Analyze(list As SyntaxTriviaList, ByRef result As AnalysisResult)
                If list.Count = 0 Then
                    Return
                End If

                For Each trivia In list
                    If trivia.VBKind = SyntaxKind.WhitespaceTrivia Then
                        AnalyzeWhitespacesInTrivia(trivia, result)
                    ElseIf trivia.VBKind = SyntaxKind.EndOfLineTrivia Then
                        AnalyzeLineBreak(trivia, result)
                    ElseIf trivia.VBKind = SyntaxKind.CommentTrivia OrElse trivia.VBKind = SyntaxKind.DocumentationCommentTrivia Then
                        result.HasComments = True
                    ElseIf trivia.VBKind = SyntaxKind.DisabledTextTrivia OrElse trivia.VBKind = SyntaxKind.SkippedTokensTrivia Then
                        result.HasSkippedOrDisabledText = True
                    ElseIf trivia.VBKind = SyntaxKind.LineContinuationTrivia Then
                        AnalyzeLineContinuation(trivia, result)
                    ElseIf trivia.VBKind = SyntaxKind.ColonTrivia Then
                        result.HasColonTrivia = True
                    Else
                        Contract.ThrowIfFalse(SyntaxFacts.IsPreprocessorDirective(trivia.VBKind))

                        result.HasPreprocessor = True
                    End If
                Next
            End Sub

            Private Shared Sub AnalyzeLineContinuation(trivia As SyntaxTrivia, ByRef result As AnalysisResult)
                result.LineBreaks += 1

                result.HasTrailingSpace = trivia.ToFullString().Length <> 3
                result.HasLineContinuation = True
                result.HasOnlyOneSpaceBeforeLineContinuation = result.Space = 1 AndAlso result.Tab = 0

                result.HasTabAfterSpace = False
                result.Space = 0
                result.Tab = 0
            End Sub

            Private Shared Sub AnalyzeLineBreak(trivia As SyntaxTrivia, ByRef result As AnalysisResult)
                ' if there was any space before line break, then we have trailing spaces
                If result.Space > 0 OrElse result.Tab > 0 Then
                    result.HasTrailingSpace = True
                End If

                ' reset space and tab information
                result.LineBreaks += 1

                result.HasTabAfterSpace = False
                result.Space = 0
                result.Tab = 0
                result.TreatAsElastic = result.TreatAsElastic Or trivia.IsElastic()
            End Sub

            Private Shared Sub AnalyzeWhitespacesInTrivia(trivia As SyntaxTrivia, ByRef result As AnalysisResult)
                ' trivia already has text. getting text should be noop
                Debug.Assert(trivia.VBKind = SyntaxKind.WhitespaceTrivia)
                Debug.Assert(trivia.Width = trivia.FullWidth)

                Dim space As Integer = 0
                Dim tab As Integer = 0
                Dim unknownWhitespace As Integer = 0

                Dim text = trivia.ToString()
                For i As Integer = 0 To trivia.Width - 1
                    If text(i) = " "c Then
                        space += 1
                    ElseIf text(i) = vbTab Then
                        If result.Space > 0 Then
                            result.HasTabAfterSpace = True
                        End If

                        tab += 1
                    Else
                        unknownWhitespace += 1
                    End If
                Next i

                ' set result
                result.Space += space
                result.Tab += tab
                result.HasUnknownWhitespace = result.HasUnknownWhitespace Or unknownWhitespace > 0
                result.TreatAsElastic = result.TreatAsElastic Or trivia.IsElastic()
            End Sub

            Friend Structure AnalysisResult
                Friend Property LineBreaks() As Integer
                Friend Property Space() As Integer
                Friend Property Tab() As Integer

                Friend Property HasTabAfterSpace() As Boolean
                Friend Property HasUnknownWhitespace() As Boolean
                Friend Property HasTrailingSpace() As Boolean
                Friend Property HasSkippedOrDisabledText() As Boolean

                Friend Property HasComments() As Boolean
                Friend Property HasPreprocessor() As Boolean

                Friend Property HasOnlyOneSpaceBeforeLineContinuation() As Boolean
                Friend Property HasLineContinuation() As Boolean
                Friend Property HasColonTrivia() As Boolean
                Friend Property TreatAsElastic() As Boolean
            End Structure
        End Class
    End Class
End Namespace