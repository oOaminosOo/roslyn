﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Module FormattingHelpers
        Public Function IsLessThanInAttribute(token As SyntaxToken) As Boolean
            ' < in attribute
            If token.VBKind = SyntaxKind.LessThanToken AndAlso
               token.Parent.VBKind = SyntaxKind.AttributeList AndAlso
               DirectCast(token.Parent, AttributeListSyntax).LessThanToken.Equals(token) Then
                Return True
            End If

            Return False
        End Function

        Public Function IsGreaterThanInAttribute(token As SyntaxToken) As Boolean
            ' > in attribute
            If token.VBKind = SyntaxKind.GreaterThanToken AndAlso
               token.Parent.VBKind = SyntaxKind.AttributeList AndAlso
               DirectCast(token.Parent, AttributeListSyntax).GreaterThanToken.Equals(token) Then
                Return True
            End If

            Return False
        End Function

        Public Function IsQuoteInXmlString(token As SyntaxToken) As Boolean
            If token.Parent Is Nothing Then
                Return False
            End If

            If (token.VBKind = SyntaxKind.DoubleQuoteToken OrElse
                token.VBKind = SyntaxKind.SingleQuoteToken) AndAlso
               token.Parent.VBKind = SyntaxKind.XmlString AndAlso
               (DirectCast(token.Parent, XmlStringSyntax).StartQuoteToken.Equals(token) OrElse
                DirectCast(token.Parent, XmlStringSyntax).EndQuoteToken.Equals(token)) Then
                Return True
            End If

            Return False
        End Function

        Public Function IsContentInXmlString(token As SyntaxToken) As Boolean
            If token.Parent Is Nothing Then
                Return False
            End If

            If token.Parent.VBKind = SyntaxKind.XmlString AndAlso
                Not DirectCast(token.Parent, XmlStringSyntax).StartQuoteToken.Equals(token) AndAlso
                Not DirectCast(token.Parent, XmlStringSyntax).EndQuoteToken.Equals(token) Then
                Return True
            End If

            Return False
        End Function

        Public Function IsXmlToken(token As SyntaxToken) As Boolean
            ' <%=
            ' <!--
            ' />
            ' </
            ' %>
            ' <?
            ' ?>
            ' -->
            ' <![CDATA[
            ' ]]>
            Select Case token.VBKind
                Case SyntaxKind.LessThanPercentEqualsToken,
                     SyntaxKind.LessThanExclamationMinusMinusToken,
                     SyntaxKind.SlashGreaterThanToken,
                     SyntaxKind.LessThanSlashToken,
                     SyntaxKind.PercentGreaterThanToken,
                     SyntaxKind.LessThanQuestionToken,
                     SyntaxKind.QuestionGreaterThanToken,
                     SyntaxKind.MinusMinusGreaterThanToken,
                     SyntaxKind.BeginCDataToken,
                     SyntaxKind.EndCDataToken
                    Return True
            End Select

            If token.Parent Is Nothing Then
                Return False
            End If

            ' < in empty element
            If token.VBKind = SyntaxKind.LessThanToken AndAlso
               token.Parent.VBKind = SyntaxKind.XmlEmptyElement Then
                Dim xmlElement = DirectCast(token.Parent, XmlEmptyElementSyntax)
                If xmlElement.LessThanToken = token Then
                    Return True
                End If
                Return False
            End If

            ' @ in xml attribute access expression
            If token.VBKind = SyntaxKind.AtToken AndAlso
               token.Parent.VBKind = SyntaxKind.XmlAttributeAccessExpression Then
                Dim xmlMemberAccess = DirectCast(token.Parent, XmlMemberAccessExpressionSyntax)
                If xmlMemberAccess.Token2 = token Then
                    Return True
                End If
                Return False
            End If

            ' : in xml prefix
            If token.VBKind = SyntaxKind.ColonToken AndAlso
               token.Parent.VBKind = SyntaxKind.XmlPrefix Then
                Dim xmlElement = DirectCast(token.Parent, XmlPrefixSyntax)
                If xmlElement.ColonToken = token Then
                    Return True
                End If
                Return False
            End If

            ' = in xml attribute
            If token.VBKind = SyntaxKind.EqualsToken AndAlso
               token.Parent.VBKind = SyntaxKind.XmlAttribute Then
                Dim xmlElement = DirectCast(token.Parent, XmlAttributeSyntax)
                If xmlElement.EqualsToken = token Then
                    Return True
                End If
                Return False
            End If

            ' = in documentation comment cref attribute
            If token.VBKind = SyntaxKind.EqualsToken AndAlso
               token.Parent.VBKind = SyntaxKind.XmlCrefAttribute Then
                Dim xmlElement = DirectCast(token.Parent, XmlCrefAttributeSyntax)
                If xmlElement.EqualsToken = token Then
                    Return True
                End If
                Return False
            End If

            ' = in documentation comment name attribute
            If token.VBKind = SyntaxKind.EqualsToken AndAlso
               token.Parent.VBKind = SyntaxKind.XmlNameAttribute Then
                Dim xmlElement = DirectCast(token.Parent, XmlNameAttributeSyntax)
                If xmlElement.EqualsToken = token Then
                    Return True
                End If
                Return False
            End If

            ' < in xml namespace imports
            If token.VBKind = SyntaxKind.LessThanToken AndAlso
               token.Parent.VBKind = SyntaxKind.XmlNamespaceImportsClause Then
                Dim xmlElement = DirectCast(token.Parent, XmlNamespaceImportsClauseSyntax)
                If xmlElement.LessThanToken = token Then
                    Return True
                End If
                Return False
            End If

            ' > in xml namespace imports
            If token.VBKind = SyntaxKind.GreaterThanToken AndAlso
               token.Parent.VBKind = SyntaxKind.XmlNamespaceImportsClause Then
                Dim xmlElement = DirectCast(token.Parent, XmlNamespaceImportsClauseSyntax)
                If xmlElement.GreaterThanToken = token Then
                    Return True
                End If
                Return False
            End If

            ' < in xml literals
            If token.VBKind = SyntaxKind.LessThanToken AndAlso
               token.Parent.VBKind = SyntaxKind.XmlBracketedName Then
                Dim xmlBracketedName = DirectCast(token.Parent, XmlBracketedNameSyntax)
                If xmlBracketedName.LessThanToken = token Then
                    Return True
                End If
                Return False
            End If

            ' > in xml literals
            If token.VBKind = SyntaxKind.GreaterThanToken AndAlso
               token.Parent.VBKind = SyntaxKind.XmlBracketedName Then
                Dim xmlBracketedName = DirectCast(token.Parent, XmlBracketedNameSyntax)
                If xmlBracketedName.GreaterThanToken = token Then
                    Return True
                End If
                Return False
            End If

            If token.Parent.Parent Is Nothing Then
                Return False
            End If

            ' < in xml element
            If token.VBKind = SyntaxKind.LessThanToken AndAlso
               token.Parent.Parent.VBKind = SyntaxKind.XmlElement Then
                Dim xmlElement = DirectCast(token.Parent.Parent, XmlElementSyntax)
                If xmlElement.StartTag.LessThanToken = token Then
                    Return True
                End If
                Return False
            End If

            ' > in xml element
            If token.VBKind = SyntaxKind.GreaterThanToken AndAlso
               token.Parent.Parent.VBKind = SyntaxKind.XmlElement Then
                Dim xmlElement = DirectCast(token.Parent.Parent, XmlElementSyntax)
                If IsGreaterThanInXmlTag(xmlElement.StartTag, token) OrElse
                   IsGreaterThanInXmlTag(xmlElement.EndTag, token) Then
                    Return True
                End If
                Return False
            End If

            ' > in start/end tag
            If token.VBKind = SyntaxKind.GreaterThanToken AndAlso IsGreaterThanInXmlTag(token.Parent, token) Then
                Return True
            End If

            Return False
        End Function

        Public Function IsGreaterThanInXmlTag(tag As SyntaxNode, token As SyntaxToken) As Boolean
            Dim startTag = TryCast(tag, XmlElementStartTagSyntax)
            If startTag IsNot Nothing Then
                Return startTag.GreaterThanToken = token
            End If

            Dim endTag = TryCast(tag, XmlElementEndTagSyntax)
            If endTag IsNot Nothing Then
                Return endTag.GreaterThanToken = token
            End If

            Return False
        End Function

        Public Function IsQuestionInNullableType(currentToken As SyntaxToken) As Boolean
            If currentToken.VBKind <> SyntaxKind.QuestionToken Then
                Return False
            End If

            Return TypeOf currentToken.Parent Is NullableTypeSyntax OrElse
                   TypeOf currentToken.Parent Is ModifiedIdentifierSyntax
        End Function

        Public Function IsColonAfterAttributeTarget(previousToken As SyntaxToken, currentToken As SyntaxToken) As Boolean
            If currentToken.VBKind <> SyntaxKind.ColonToken Then
                Return False
            End If

            Return TypeOf previousToken.Parent Is AttributeTargetSyntax
        End Function

        Public Function IsExclamationInDictionaryAccess(token As SyntaxToken) As Boolean
            If token.VBKind <> SyntaxKind.ExclamationToken Then
                Return False
            End If

            ' * !
            Dim memberAccess = TryCast(token.Parent, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing Then
                Return memberAccess.VBKind = SyntaxKind.DictionaryAccessExpression AndAlso memberAccess.OperatorToken = token
            End If

            If token.Parent.Parent Is Nothing Then
                Return False
            End If

            ' ! *
            memberAccess = TryCast(token.Parent, MemberAccessExpressionSyntax)
            If memberAccess Is Nothing Then
                Return True
            End If

            Return memberAccess.VBKind = SyntaxKind.DictionaryAccessExpression AndAlso memberAccess.OperatorToken = token
        End Function

        Public Function IsParenInArgumentList(token As SyntaxToken) As Boolean
            Dim argumentList = TryCast(token.Parent, ArgumentListSyntax)
            If argumentList Is Nothing Then
                Return False
            End If

            Return argumentList.OpenParenToken.Equals(token) OrElse argumentList.CloseParenToken.Equals(token)
        End Function

        Public Function IsParenInBinaryCondition(token As SyntaxToken) As Boolean
            Dim binaryCondition = TryCast(token.Parent, BinaryConditionalExpressionSyntax)
            If binaryCondition Is Nothing Then
                Return False
            End If

            Return binaryCondition.OpenParenToken.Equals(token) OrElse binaryCondition.CloseParenToken.Equals(token)
        End Function

        Public Function IsParenInTernaryCondition(token As SyntaxToken) As Boolean
            Dim ternaryCondition = TryCast(token.Parent, TernaryConditionalExpressionSyntax)
            If ternaryCondition Is Nothing Then
                Return False
            End If

            Return ternaryCondition.OpenParenToken.Equals(token) OrElse ternaryCondition.CloseParenToken.Equals(token)
        End Function

        Public Function IsXmlTokenInXmlDeclaration(token As SyntaxToken) As Boolean
            Dim xmlDeclaration = TryCast(token.Parent, XmlDeclarationSyntax)
            If xmlDeclaration Is Nothing Then
                Return False
            End If

            Return xmlDeclaration.LessThanQuestionToken.Equals(token) OrElse xmlDeclaration.QuestionGreaterThanToken.Equals(token)
        End Function

        Public Function IsMemberAccessDotWithoutExpression(token As SyntaxToken) As Boolean
            If token.VBKind <> SyntaxKind.DotToken Then
                Return False
            End If

            Dim memberAccess = TryCast(token.Parent, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing Then
                Return memberAccess.Expression Is Nothing AndAlso memberAccess.OperatorToken = token
            End If

            Dim xmlMemberAccess = TryCast(token.Parent, XmlMemberAccessExpressionSyntax)
            If xmlMemberAccess IsNot Nothing Then
                Return xmlMemberAccess.Base Is Nothing AndAlso xmlMemberAccess.Token1 = token
            End If

            Return False
        End Function

        Public Function IsNamedFieldInitializerDot(token As SyntaxToken) As Boolean
            Dim namedFieldInitializer = TryCast(token.Parent, NamedFieldInitializerSyntax)
            If namedFieldInitializer Is Nothing Then
                Return False
            End If

            Return namedFieldInitializer.DotToken = token
        End Function

        Friend Function IsOverloadableOperator(token As SyntaxToken) As Boolean
            If token.Parent.IsKind(SyntaxKind.OperatorStatement) Then
                Return DirectCast(token.Parent, OperatorStatementSyntax).OperatorToken = token
            End If

            Return False
        End Function

    End Module
End Namespace