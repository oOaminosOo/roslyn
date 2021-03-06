﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CaseCorrection
    Partial Class VisualBasicCaseCorrectionService
        Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly createAliasSet As Func(Of ImmutableHashSet(Of String)) =
                Function()
                    Dim model = DirectCast(Me.semanticModel.GetOriginalSemanticModel(), SemanticModel)

                    ' root should be already available
                    If Not model.SyntaxTree.HasCompilationUnitRoot Then
                        Return ImmutableHashSet.Create(Of String)()
                    End If

                    Dim root = model.SyntaxTree.GetCompilationUnitRoot()

                    Dim [set] = ImmutableHashSet.CreateBuilder(Of String)(StringComparer.OrdinalIgnoreCase)
                    For Each importsClause In root.GetAliasImportsClauses()
                        If Not String.IsNullOrWhiteSpace(importsClause.Alias.Identifier.ValueText) Then
                            [set].Add(importsClause.Alias.Identifier.ValueText)
                        End If
                    Next

                    For Each import In model.Compilation.AliasImports
                        [set].Add(import.Name)
                    Next

                    Return [set].ToImmutable()
                End Function

            Private ReadOnly syntaxFactsService As ISyntaxFactsService
            Private ReadOnly semanticModel As SemanticModel
            Private ReadOnly aliasSet As Lazy(Of ImmutableHashSet(Of String))
            Private ReadOnly cancellationToken As CancellationToken

            Sub New(syntaxFactsService As ISyntaxFactsService, semanticModel As SemanticModel, cancellationToken As CancellationToken)
                MyBase.New(visitIntoStructuredTrivia:=True)
                Me.syntaxFactsService = syntaxFactsService
                Me.semanticModel = semanticModel
                Me.aliasSet = New Lazy(Of ImmutableHashSet(Of String))(createAliasSet)
                Me.cancellationToken = cancellationToken
            End Sub

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                Dim newToken = MyBase.VisitToken(token)

                If syntaxFactsService.IsIdentifier(newToken) Then
                    Return VisitIdentifier(token, newToken)
                ElseIf syntaxFactsService.IsKeyword(newToken) OrElse syntaxFactsService.IsContextualKeyword(newToken) Then
                    Return VisitKeyword(newToken)
                ElseIf token.IsNumericLiteral() Then
                    Return VisitNumericLiteral(newToken)
                ElseIf token.IsCharacterLiteral() Then
                    Return VisitCharacterLiteral(newToken)
                End If

                Return newToken
            End Function

            Private Function VisitIdentifier(token As SyntaxToken, newToken As SyntaxToken) As SyntaxToken
                If newToken.IsMissing OrElse TypeOf newToken.Parent Is ArgumentSyntax OrElse semanticModel Is Nothing Then
                    Return newToken
                End If

                If token.Parent.IsPartOfStructuredTrivia() Then
                    Dim identifierSyntax = TryCast(token.Parent, IdentifierNameSyntax)
                    If identifierSyntax IsNot Nothing Then
                        Dim preprocessingSymbolInfo = semanticModel.GetPreprocessingSymbolInfo(identifierSyntax)
                        If preprocessingSymbolInfo.Symbol IsNot Nothing Then
                            Dim name = preprocessingSymbolInfo.Symbol.Name
                            If Not String.IsNullOrEmpty(name) AndAlso name <> token.ValueText Then
                                ' Name should differ only in case
                                Contract.Requires(name.Equals(token.ValueText, StringComparison.OrdinalIgnoreCase))

                                Return GetIdentifierWithCorrectedName(name, newToken)
                            End If
                        End If
                    End If

                    Return newToken
                Else
                    Dim methodDeclaration = TryCast(token.Parent, MethodStatementSyntax)
                    If methodDeclaration IsNot Nothing Then
                        ' If this is a partial method implementation part, then case correct the method name to match the partial method definition part.
                        Dim definitionPart As IMethodSymbol = Nothing
                        Dim otherPartOfPartial = GetOtherPartOfPartialMethod(methodDeclaration, definitionPart)
                        If otherPartOfPartial IsNot Nothing And otherPartOfPartial Is definitionPart Then
                            Return CaseCorrectIdentifierIfNamesDiffer(token, newToken, otherPartOfPartial)
                        End If
                    Else
                        Dim parameterSyntax = token.GetAncestor(Of ParameterSyntax)()
                        If parameterSyntax IsNot Nothing Then
                            ' If this is a parameter declaration for a partial method implementation part,
                            ' then case correct the parameter name to match the corresponding parameter in the partial method definition part.
                            methodDeclaration = parameterSyntax.GetAncestor(Of MethodStatementSyntax)()
                            If methodDeclaration IsNot Nothing Then
                                Dim definitionPart As IMethodSymbol = Nothing
                                Dim otherPartOfPartial = GetOtherPartOfPartialMethod(methodDeclaration, definitionPart)
                                If otherPartOfPartial IsNot Nothing And otherPartOfPartial Is definitionPart Then
                                    Dim ordinal As Integer = 0
                                    For Each param As SyntaxNode In methodDeclaration.ParameterList.Parameters
                                        If param Is parameterSyntax Then
                                            Exit For
                                        End If
                                        ordinal = ordinal + 1
                                    Next

                                    Contract.Requires(otherPartOfPartial.Parameters.Length > ordinal)
                                    Dim otherPartParam = otherPartOfPartial.Parameters(ordinal)

                                    ' We don't want to rename the parameter if names are not equal ignoring case.
                                    ' Compiler will anyways generate an error for this case.
                                    Return CaseCorrectIdentifierIfNamesDiffer(token, newToken, otherPartParam, namesMustBeEqualIgnoringCase:=True)
                                End If
                            End If
                        End If
                    End If
                End If

                Dim symbol = GetAliasOrAnySymbol(semanticModel, token.Parent, cancellationToken)
                If symbol Is Nothing Then
                    Return newToken
                End If

                If TypeOf symbol Is ITypeSymbol AndAlso DirectCast(symbol, ITypeSymbol).TypeKind = TypeKind.Error Then
                    Return newToken
                End If

                ' If it's a constructor we bind to, then we want to compare the name on the token to the
                ' name of the type.  The name of the bound symbol will be something useless like '.ctor'.
                ' However, if it's an explicit New on the right side of a member access or qualified name, we want to use "New".
                If symbol.IsConstructor Then
                    If token.IsNewOnRightSideOfDotOrBang() Then
                        Return SyntaxFactory.Identifier(newToken.LeadingTrivia, "New", newToken.TrailingTrivia)
                    End If

                    symbol = symbol.ContainingType
                End If

                Return CaseCorrectIdentifierIfNamesDiffer(token, newToken, symbol)
            End Function

            Private Function GetAliasOrAnySymbol(model As SemanticModel, node As SyntaxNode, cancellationToken As CancellationToken) As ISymbol
                Dim identifier = TryCast(node, IdentifierNameSyntax)
                If identifier IsNot Nothing AndAlso Me.aliasSet.Value.Contains(identifier.Identifier.ValueText) Then
                    Dim [alias] = model.GetAliasInfo(identifier, cancellationToken)
                    If [alias] IsNot Nothing Then
                        Return [alias]
                    End If
                End If

                Return model.GetSymbolInfo(node, cancellationToken).GetAnySymbol()
            End Function

            Private Shared Function CaseCorrectIdentifierIfNamesDiffer(
                token As SyntaxToken,
                newToken As SyntaxToken,
                symbol As ISymbol,
                Optional namesMustBeEqualIgnoringCase As Boolean = False
            ) As SyntaxToken
                If NamesDiffer(symbol, token) Then
                    If namesMustBeEqualIgnoringCase AndAlso Not String.Equals(symbol.Name, token.ValueText, StringComparison.OrdinalIgnoreCase) Then
                        Return newToken
                    End If

                    Dim correctedName = GetCorrectedName(token, symbol)
                    Return GetIdentifierWithCorrectedName(correctedName, newToken)
                End If

                Return newToken
            End Function

            Private Function GetOtherPartOfPartialMethod(methodDeclaration As MethodStatementSyntax, <Out> ByRef definitionPart As IMethodSymbol) As IMethodSymbol
                Contract.ThrowIfNull(methodDeclaration)
                Contract.ThrowIfNull(semanticModel)

                Dim methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken)
                If methodSymbol IsNot Nothing Then
                    definitionPart = If(methodSymbol.PartialDefinitionPart, methodSymbol)
                    Return If(methodSymbol.PartialDefinitionPart, methodSymbol.PartialImplementationPart)
                End If

                Return Nothing
            End Function

            Private Shared Function GetCorrectedName(token As SyntaxToken, symbol As ISymbol) As String
                If symbol.IsAttribute Then
                    If String.Equals(token.ValueText & AttributeSuffix, symbol.Name, StringComparison.OrdinalIgnoreCase) Then
                        Return symbol.Name.Substring(0, symbol.Name.Length - AttributeSuffix.Length)
                    End If
                End If

                Return symbol.Name
            End Function

            Private Shared Function GetIdentifierWithCorrectedName(correctedName As String, token As SyntaxToken) As SyntaxToken
                If token.IsBracketed Then
                    Return SyntaxFactory.BracketedIdentifier(token.LeadingTrivia, correctedName, token.TrailingTrivia)
                Else
                    Return SyntaxFactory.Identifier(token.LeadingTrivia, correctedName, token.TrailingTrivia)
                End If
            End Function

            Private Shared Function NamesDiffer(symbol As ISymbol,
                                         token As SyntaxToken) As Boolean
                If String.IsNullOrEmpty(symbol.Name) Then
                    Return False
                End If

                If symbol.Name = token.ValueText Then
                    Return False
                End If

                If symbol.IsAttribute() Then
                    If symbol.Name = token.ValueText & AttributeSuffix Then
                        Return False
                    End If
                End If

                Return True
            End Function

            Private Function VisitKeyword(token As SyntaxToken) As SyntaxToken
                If Not token.IsMissing Then
                    Dim actualText = token.ToString()
                    Dim expectedText = syntaxFactsService.GetText(token.VBKind)

                    If Not String.IsNullOrWhiteSpace(expectedText) AndAlso actualText <> expectedText Then
                        Return SyntaxFactory.Token(token.LeadingTrivia, token.VBKind, token.TrailingTrivia, expectedText)
                    End If
                End If

                Return token
            End Function

            Private Function VisitNumericLiteral(token As SyntaxToken) As SyntaxToken
                If Not token.IsMissing Then

                    ' For any numeric literal, we simply case correct any letters to uppercase.
                    ' The only letters allowed in a numeric literal are:
                    '   * Type characters: S, US, I, UI, L, UL, D, F, R
                    '   * Hex/Octal literals: H, O and A, B, C, D, E, F
                    '   * Exponent: E
                    '   * Time literals: AM, PM 

                    Dim actualText = token.ToString()
                    Dim expectedText = actualText.ToUpperInvariant()

                    If actualText <> expectedText Then
                        Return SyntaxFactory.ParseToken(expectedText).WithLeadingTrivia(token.LeadingTrivia).WithTrailingTrivia(token.TrailingTrivia)
                    End If
                End If

                Return token
            End Function

            Private Function VisitCharacterLiteral(token As SyntaxToken) As SyntaxToken
                If Not token.IsMissing Then

                    ' For character literals, we case correct the type character to "c".
                    Dim actualText = token.ToString()

                    If actualText.EndsWith("C") Then
                        Dim expectedText = actualText.Substring(0, actualText.Length - 1) & "c"
                        Return SyntaxFactory.ParseToken(expectedText).WithLeadingTrivia(token.LeadingTrivia).WithTrailingTrivia(token.TrailingTrivia)
                    End If
                End If

                Return token
            End Function

            Public Overrides Function VisitTrivia(trivia As SyntaxTrivia) As SyntaxTrivia
                trivia = MyBase.VisitTrivia(trivia)

                If trivia.VBKind = SyntaxKind.CommentTrivia AndAlso trivia.Width >= 3 Then
                    Dim remText = trivia.ToString().Substring(0, 3)
                    Dim remKeywordText As String = syntaxFactsService.GetText(SyntaxKind.REMKeyword)
                    If remText <> remKeywordText AndAlso SyntaxFacts.GetKeywordKind(remText) = SyntaxKind.REMKeyword Then
                        Dim expectedText = remKeywordText & trivia.ToString().Substring(3)
                        Return SyntaxFactory.CommentTrivia(expectedText)
                    End If
                End If

                Return trivia
            End Function

        End Class
    End Class
End Namespace