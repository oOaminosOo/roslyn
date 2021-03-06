﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------------------------------------
' Contains hand-written factories for the SyntaxNodes. Most factories are
' code-generated into SyntaxNodes.vb, but some are easier to hand-write.
'-----------------------------------------------------------------------------------------------------------

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports InternalSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Class SyntaxFactory
        Friend Shared Function IntegerLiteralToken(text As String, base As LiteralBase, typeSuffix As TypeCharacter, value As ULong, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode) As IntegerLiteralTokenSyntax
            Debug.Assert(text IsNot Nothing)
            Select Case typeSuffix
                Case TypeCharacter.ShortLiteral
                    Return New IntegerLiteralTokenSyntax(Of Short)(SyntaxKind.IntegerLiteralToken, text, leadingTrivia, trailingTrivia, base, typeSuffix, CShort(value))

                Case TypeCharacter.UShortLiteral
                    Return New IntegerLiteralTokenSyntax(Of UShort)(SyntaxKind.IntegerLiteralToken, text, leadingTrivia, trailingTrivia, base, typeSuffix, CUShort(value))

                Case TypeCharacter.IntegerLiteral, TypeCharacter.Integer
                    Return New IntegerLiteralTokenSyntax(Of Integer)(SyntaxKind.IntegerLiteralToken, text, leadingTrivia, trailingTrivia, base, typeSuffix, CInt(value))

                Case TypeCharacter.UIntegerLiteral
                    Return New IntegerLiteralTokenSyntax(Of UInteger)(SyntaxKind.IntegerLiteralToken, text, leadingTrivia, trailingTrivia, base, typeSuffix, CUInt(value))

                Case TypeCharacter.LongLiteral, TypeCharacter.Long
                    Return New IntegerLiteralTokenSyntax(Of Long)(SyntaxKind.IntegerLiteralToken, text, leadingTrivia, trailingTrivia, base, typeSuffix, CLng(value))

                Case TypeCharacter.ULongLiteral
                    Return New IntegerLiteralTokenSyntax(Of ULong)(SyntaxKind.IntegerLiteralToken, text, leadingTrivia, trailingTrivia, base, typeSuffix, value)

                Case TypeCharacter.None
                    Dim useIntegerType As Boolean = False

                    If base = LiteralBase.Decimal Then
                        useIntegerType = (value <= Integer.MaxValue)
                    Else
                        useIntegerType = ((value And (Not &HFFFFFFFFUL)) = 0)
                    End If

                    If useIntegerType Then
                        Return New IntegerLiteralTokenSyntax(Of Integer)(SyntaxKind.IntegerLiteralToken, text, leadingTrivia, trailingTrivia, base, typeSuffix, CInt(value))
                    Else
                        Return New IntegerLiteralTokenSyntax(Of Long)(SyntaxKind.IntegerLiteralToken, text, leadingTrivia, trailingTrivia, base, typeSuffix, CLng(value))
                    End If

                Case Else
                    Throw New ArgumentException("typeSuffix")
            End Select
        End Function

        Friend Shared Function FloatingLiteralToken(text As String, typeSuffix As TypeCharacter, value As Double, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode) As FloatingLiteralTokenSyntax
            Debug.Assert(text IsNot Nothing)
            Select Case typeSuffix
                Case TypeCharacter.DoubleLiteral, TypeCharacter.Double, TypeCharacter.None
                    Return New FloatingLiteralTokenSyntax(Of Double)(SyntaxKind.FloatingLiteralToken, text, leadingTrivia, trailingTrivia, typeSuffix, value)
                Case TypeCharacter.SingleLiteral, TypeCharacter.Single
                    Return New FloatingLiteralTokenSyntax(Of Single)(SyntaxKind.FloatingLiteralToken, text, leadingTrivia, trailingTrivia, typeSuffix, CSng(value))
                Case Else
                    Throw New ArgumentException("typeSuffix")
            End Select
        End Function

        ''' <summary>
        ''' Create an identifier node without brackets or type character.
        ''' </summary>
        Friend Shared Function Identifier(text As String, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode) As IdentifierTokenSyntax
            Debug.Assert(text IsNot Nothing)
            Return New SimpleIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, text, leadingTrivia, trailingTrivia)
        End Function

        ''' <summary>
        ''' Create an identifier node with brackets or type character.
        ''' </summary>
        Friend Shared Function Identifier(text As String, possibleKeywordKind As SyntaxKind, isBracketed As Boolean, identifierText As String, typeCharacter As TypeCharacter, leadingTrivia As VisualBasicSyntaxNode, trailingTrivia As VisualBasicSyntaxNode) As IdentifierTokenSyntax
            Debug.Assert(text IsNot Nothing)
            Return New ComplexIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, text, leadingTrivia, trailingTrivia, possibleKeywordKind, isBracketed, identifierText, typeCharacter)
        End Function

        ''' <summary>
        ''' Create an identifier node without brackets or type character or trivia.
        ''' </summary>
        Friend Shared Function Identifier(text As String) As IdentifierTokenSyntax
            Debug.Assert(text IsNot Nothing)
            Return New SimpleIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, text, Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Create a missing identifier.
        ''' </summary>
        Friend Shared Function MissingIdentifier() As IdentifierTokenSyntax
            Return New SimpleIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, "", Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Create a missing contextual keyword.
        ''' </summary>
        Friend Shared Function MissingIdentifier(kind As SyntaxKind) As IdentifierTokenSyntax
            Return New ComplexIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, "", Nothing, Nothing, kind, False, "", TypeCharacter.None)
        End Function

        ''' <summary>
        ''' Create a missing keyword.
        ''' </summary>
        Friend Shared Function MissingKeyword(kind As SyntaxKind) As KeywordSyntax
            Return New KeywordSyntax(kind, "", Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Create a missing punctutation mark.
        ''' </summary>
        Friend Shared Function MissingPunctuation(kind As SyntaxKind) As PunctuationSyntax
            Return New PunctuationSyntax(kind, "", Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Create a missing string literal.
        ''' </summary>
        Friend Shared Function MissingStringLiteral() As StringLiteralTokenSyntax
            Return StringLiteralToken("", "", Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Create a missing character literal.
        ''' </summary>
        Friend Shared Function MissingCharacterLiteralToken() As CharacterLiteralTokenSyntax
            Return CharacterLiteralToken("", Nothing, Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Create a missing integer literal.
        ''' </summary>
        Friend Shared Function MissingIntegerLiteralToken() As IntegerLiteralTokenSyntax
            Return IntegerLiteralToken("", LiteralBase.Decimal, TypeCharacter.None, Nothing, Nothing, Nothing)
        End Function

        ''' <summary>
        ''' Creates a copy of a token.
        ''' <para name="err"></para>
        ''' <para name="trivia"></para>
        ''' </summary>
        ''' <returns>The new token</returns>
        Friend Shared Function MissingToken(kind As SyntaxKind) As SyntaxToken
            Dim t As SyntaxToken

            Select Case kind
                Case SyntaxKind.StatementTerminatorToken
                    t = SyntaxFactory.Token(Nothing, SyntaxKind.StatementTerminatorToken, Nothing, String.Empty)

                Case SyntaxKind.EndOfFileToken
                    t = SyntaxFactory.EndOfFileToken()

                Case SyntaxKind.AddHandlerKeyword,
                SyntaxKind.AddressOfKeyword,
                SyntaxKind.AliasKeyword,
                SyntaxKind.AndKeyword,
                SyntaxKind.AndAlsoKeyword,
                SyntaxKind.AsKeyword,
                SyntaxKind.BooleanKeyword,
                SyntaxKind.ByRefKeyword,
                SyntaxKind.ByteKeyword,
                SyntaxKind.ByValKeyword,
                SyntaxKind.CallKeyword,
                SyntaxKind.CaseKeyword,
                SyntaxKind.CatchKeyword,
                SyntaxKind.CBoolKeyword,
                SyntaxKind.CByteKeyword,
                SyntaxKind.CCharKeyword,
                SyntaxKind.CDateKeyword,
                SyntaxKind.CDecKeyword,
                SyntaxKind.CDblKeyword,
                SyntaxKind.CharKeyword,
                SyntaxKind.CIntKeyword,
                SyntaxKind.ClassKeyword,
                SyntaxKind.CLngKeyword,
                SyntaxKind.CObjKeyword,
                SyntaxKind.ConstKeyword,
                SyntaxKind.ContinueKeyword,
                SyntaxKind.CSByteKeyword,
                SyntaxKind.CShortKeyword,
                SyntaxKind.CSngKeyword,
                SyntaxKind.CStrKeyword,
                SyntaxKind.CTypeKeyword,
                SyntaxKind.CUIntKeyword,
                SyntaxKind.CULngKeyword,
                SyntaxKind.CUShortKeyword,
                SyntaxKind.DateKeyword,
                SyntaxKind.DecimalKeyword,
                SyntaxKind.DeclareKeyword,
                SyntaxKind.DefaultKeyword,
                SyntaxKind.DelegateKeyword,
                SyntaxKind.DimKeyword,
                SyntaxKind.DirectCastKeyword,
                SyntaxKind.DoKeyword,
                SyntaxKind.DoubleKeyword,
                SyntaxKind.EachKeyword,
                SyntaxKind.ElseKeyword,
                SyntaxKind.ElseIfKeyword,
                SyntaxKind.EndKeyword,
                SyntaxKind.EnumKeyword,
                SyntaxKind.EraseKeyword,
                SyntaxKind.ErrorKeyword,
                SyntaxKind.EventKeyword,
                SyntaxKind.ExitKeyword,
                SyntaxKind.FalseKeyword,
                SyntaxKind.FinallyKeyword,
                SyntaxKind.ForKeyword,
                SyntaxKind.FriendKeyword,
                SyntaxKind.FunctionKeyword,
                SyntaxKind.GetKeyword,
                SyntaxKind.GetTypeKeyword,
                SyntaxKind.GetXmlNamespaceKeyword,
                SyntaxKind.GlobalKeyword,
                SyntaxKind.GoToKeyword,
                SyntaxKind.HandlesKeyword,
                SyntaxKind.IfKeyword,
                SyntaxKind.ImplementsKeyword,
                SyntaxKind.ImportsKeyword,
                SyntaxKind.InKeyword,
                SyntaxKind.InheritsKeyword,
                SyntaxKind.IntegerKeyword,
                SyntaxKind.InterfaceKeyword,
                SyntaxKind.IsKeyword,
                SyntaxKind.IsNotKeyword,
                SyntaxKind.LetKeyword,
                SyntaxKind.LibKeyword,
                SyntaxKind.LikeKeyword,
                SyntaxKind.LongKeyword,
                SyntaxKind.LoopKeyword,
                SyntaxKind.MeKeyword,
                SyntaxKind.ModKeyword,
                SyntaxKind.ModuleKeyword,
                SyntaxKind.MustInheritKeyword,
                SyntaxKind.MustOverrideKeyword,
                SyntaxKind.MyBaseKeyword,
                SyntaxKind.MyClassKeyword,
                SyntaxKind.NameOfKeyword,
                SyntaxKind.NamespaceKeyword,
                SyntaxKind.NarrowingKeyword,
                SyntaxKind.NextKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.NotKeyword,
                SyntaxKind.NothingKeyword,
                SyntaxKind.NotInheritableKeyword,
                SyntaxKind.NotOverridableKeyword,
                SyntaxKind.ObjectKeyword,
                SyntaxKind.OfKeyword,
                SyntaxKind.OnKeyword,
                SyntaxKind.OperatorKeyword,
                SyntaxKind.OptionKeyword,
                SyntaxKind.OptionalKeyword,
                SyntaxKind.OrKeyword,
                SyntaxKind.OrElseKeyword,
                SyntaxKind.OverloadsKeyword,
                SyntaxKind.OverridableKeyword,
                SyntaxKind.OverridesKeyword,
                SyntaxKind.ParamArrayKeyword,
                SyntaxKind.PartialKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.PropertyKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.RaiseEventKeyword,
                SyntaxKind.ReadOnlyKeyword,
                SyntaxKind.ReDimKeyword,
                SyntaxKind.REMKeyword,
                SyntaxKind.RemoveHandlerKeyword,
                SyntaxKind.ResumeKeyword,
                SyntaxKind.ReturnKeyword,
                SyntaxKind.SByteKeyword,
                SyntaxKind.SelectKeyword,
                SyntaxKind.SetKeyword,
                SyntaxKind.ShadowsKeyword,
                SyntaxKind.SharedKeyword,
                SyntaxKind.ShortKeyword,
                SyntaxKind.SingleKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.StepKeyword,
                SyntaxKind.StopKeyword,
                SyntaxKind.StringKeyword,
                SyntaxKind.StructureKeyword,
                SyntaxKind.SubKeyword,
                SyntaxKind.SyncLockKeyword,
                SyntaxKind.ThenKeyword,
                SyntaxKind.ThrowKeyword,
                SyntaxKind.ToKeyword,
                SyntaxKind.TrueKeyword,
                SyntaxKind.TryKeyword,
                SyntaxKind.TryCastKeyword,
                SyntaxKind.TypeOfKeyword,
                SyntaxKind.UIntegerKeyword,
                SyntaxKind.ULongKeyword,
                SyntaxKind.UShortKeyword,
                SyntaxKind.UsingKeyword,
                SyntaxKind.WhenKeyword,
                SyntaxKind.WhileKeyword,
                SyntaxKind.WideningKeyword,
                SyntaxKind.WithKeyword,
                SyntaxKind.WithEventsKeyword,
                SyntaxKind.WriteOnlyKeyword,
                SyntaxKind.XorKeyword,
                SyntaxKind.EndIfKeyword,
                SyntaxKind.GosubKeyword,
                SyntaxKind.VariantKeyword,
                SyntaxKind.WendKeyword,
                SyntaxKind.OutKeyword
                    t = SyntaxFactory.MissingKeyword(kind)

                Case SyntaxKind.AggregateKeyword,
                SyntaxKind.AllKeyword,
                SyntaxKind.AnsiKeyword,
                SyntaxKind.AscendingKeyword,
                SyntaxKind.AssemblyKeyword,
                SyntaxKind.AutoKeyword,
                SyntaxKind.BinaryKeyword,
                SyntaxKind.ByKeyword,
                SyntaxKind.CompareKeyword,
                SyntaxKind.CustomKeyword,
                SyntaxKind.DescendingKeyword,
                SyntaxKind.DisableKeyword,
                SyntaxKind.DistinctKeyword,
                SyntaxKind.EnableKeyword,
                SyntaxKind.EqualsKeyword,
                SyntaxKind.ExplicitKeyword,
                SyntaxKind.ExternalSourceKeyword,
                SyntaxKind.ExternalChecksumKeyword,
                SyntaxKind.FromKeyword,
                SyntaxKind.GroupKeyword,
                SyntaxKind.InferKeyword,
                SyntaxKind.IntoKeyword,
                SyntaxKind.IsFalseKeyword,
                SyntaxKind.IsTrueKeyword,
                SyntaxKind.JoinKeyword,
                SyntaxKind.KeyKeyword,
                SyntaxKind.MidKeyword,
                SyntaxKind.OffKeyword,
                SyntaxKind.OrderKeyword,
                SyntaxKind.PreserveKeyword,
                SyntaxKind.RegionKeyword,
                SyntaxKind.SkipKeyword,
                SyntaxKind.StrictKeyword,
                SyntaxKind.TextKeyword,
                SyntaxKind.TakeKeyword,
                SyntaxKind.UnicodeKeyword,
                SyntaxKind.UntilKeyword,
                SyntaxKind.WarningKeyword,
                SyntaxKind.WhereKeyword
                    ' These are identifiers that have a contextual kind
                    Return SyntaxFactory.MissingIdentifier(kind)

                Case SyntaxKind.ExclamationToken,
                    SyntaxKind.CommaToken,
                    SyntaxKind.HashToken,
                    SyntaxKind.AmpersandToken,
                    SyntaxKind.SingleQuoteToken,
                    SyntaxKind.OpenParenToken,
                    SyntaxKind.CloseParenToken,
                    SyntaxKind.OpenBraceToken,
                    SyntaxKind.CloseBraceToken,
                    SyntaxKind.DoubleQuoteToken,
                    SyntaxKind.SemicolonToken,
                    SyntaxKind.AsteriskToken,
                    SyntaxKind.PlusToken,
                    SyntaxKind.MinusToken,
                    SyntaxKind.DotToken,
                    SyntaxKind.SlashToken,
                    SyntaxKind.ColonToken,
                    SyntaxKind.LessThanToken,
                    SyntaxKind.LessThanEqualsToken,
                    SyntaxKind.LessThanGreaterThanToken,
                    SyntaxKind.EqualsToken,
                    SyntaxKind.GreaterThanToken,
                    SyntaxKind.GreaterThanEqualsToken,
                    SyntaxKind.BackslashToken,
                    SyntaxKind.CaretToken,
                    SyntaxKind.ColonEqualsToken,
                    SyntaxKind.AmpersandEqualsToken,
                    SyntaxKind.AsteriskEqualsToken,
                    SyntaxKind.PlusEqualsToken,
                    SyntaxKind.MinusEqualsToken,
                    SyntaxKind.SlashEqualsToken,
                    SyntaxKind.BackslashEqualsToken,
                    SyntaxKind.CaretEqualsToken,
                    SyntaxKind.LessThanLessThanToken,
                    SyntaxKind.GreaterThanGreaterThanToken,
                    SyntaxKind.LessThanLessThanEqualsToken,
                    SyntaxKind.GreaterThanGreaterThanEqualsToken,
                    SyntaxKind.QuestionToken
                    t = SyntaxFactory.MissingPunctuation(kind)

                Case SyntaxKind.FloatingLiteralToken
                    t = SyntaxFactory.FloatingLiteralToken("", TypeCharacter.None, Nothing, Nothing, Nothing)

                Case SyntaxKind.DecimalLiteralToken
                    t = SyntaxFactory.DecimalLiteralToken("", TypeCharacter.None, Nothing, Nothing, Nothing)

                Case SyntaxKind.DateLiteralToken
                    t = SyntaxFactory.DateLiteralToken("", Nothing, Nothing, Nothing)

                Case SyntaxKind.XmlNameToken
                    t = SyntaxFactory.XmlNameToken("", SyntaxKind.XmlNameToken, Nothing, Nothing)

                Case SyntaxKind.XmlTextLiteralToken
                    t = SyntaxFactory.XmlTextLiteralToken("", "", Nothing, Nothing)

                Case SyntaxKind.SlashGreaterThanToken,
                    SyntaxKind.LessThanSlashToken,
                    SyntaxKind.LessThanExclamationMinusMinusToken,
                    SyntaxKind.MinusMinusGreaterThanToken,
                    SyntaxKind.LessThanQuestionToken,
                    SyntaxKind.QuestionGreaterThanToken,
                    SyntaxKind.LessThanPercentEqualsToken,
                    SyntaxKind.PercentGreaterThanToken,
                    SyntaxKind.BeginCDataToken,
                    SyntaxKind.EndCDataToken
                    t = SyntaxFactory.MissingPunctuation(kind)

                Case SyntaxKind.IdentifierToken
                    t = SyntaxFactory.MissingIdentifier()

                Case SyntaxKind.IntegerLiteralToken
                    t = MissingIntegerLiteralToken()

                Case SyntaxKind.StringLiteralToken
                    t = SyntaxFactory.MissingStringLiteral()

                Case SyntaxKind.CharacterLiteralToken
                    t = SyntaxFactory.MissingCharacterLiteralToken()

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select
            Return t
        End Function

        Friend Shared Function BadToken(SubKind As SyntaxSubKind, text As String, precedingTrivia As VisualBasicSyntaxNode, followingTrivia As VisualBasicSyntaxNode) As BadTokenSyntax
            Return New BadTokenSyntax(SyntaxKind.BadToken, SubKind, Nothing, Nothing, text, precedingTrivia, followingTrivia)
        End Function

        ''' <summary>
        ''' Create an end-of-text token.
        ''' </summary>
        Friend Shared Function EndOfFileToken(precedingTrivia As SyntaxTrivia) As PunctuationSyntax
            Return New PunctuationSyntax(SyntaxKind.EndOfFileToken, "", precedingTrivia, Nothing)
        End Function

        ''' <summary>
        ''' Create an end-of-text token.
        ''' </summary>
        Friend Shared Function EndOfFileToken() As PunctuationSyntax
            Return New PunctuationSyntax(SyntaxKind.EndOfFileToken, "", Nothing, Nothing)
        End Function

        Friend Shared Function Identifier(text As String, isBracketed As Boolean, baseText As String, typeCharacter As TypeCharacter, precedingTrivia As VisualBasicSyntaxNode, followingTrivia As VisualBasicSyntaxNode) As IdentifierTokenSyntax
            Debug.Assert(text IsNot Nothing)
            Return New ComplexIdentifierSyntax(SyntaxKind.IdentifierToken, Nothing, Nothing, text, precedingTrivia, followingTrivia, SyntaxKind.IdentifierToken, isBracketed, baseText, typeCharacter)
        End Function

        Private Shared _notMissingEmptyToken As PunctuationSyntax = Nothing
        Friend Shared ReadOnly Property NotMissingEmptyToken() As PunctuationSyntax
            Get
                If _notMissingEmptyToken Is Nothing Then
                    _notMissingEmptyToken = New PunctuationSyntax(SyntaxKind.EmptyToken, "", Nothing, Nothing)
                End If
                Return _notMissingEmptyToken
            End Get
        End Property

        Private Shared _missingEmptyToken As PunctuationSyntax = Nothing
        Friend Shared ReadOnly Property MissingEmptyToken() As PunctuationSyntax
            Get
                If _missingEmptyToken Is Nothing Then
                    _missingEmptyToken = New PunctuationSyntax(SyntaxKind.EmptyToken, "", Nothing, Nothing)
                    _missingEmptyToken.ClearFlags(GreenNode.NodeFlags.IsNotMissing)
                End If
                Return _missingEmptyToken
            End Get
        End Property

        Private Shared _statementTerminatorToken As PunctuationSyntax = Nothing
        Friend Shared ReadOnly Property StatementTerminatorToken() As PunctuationSyntax
            Get
                If _statementTerminatorToken Is Nothing Then
                    _statementTerminatorToken = New PunctuationSyntax(SyntaxKind.StatementTerminatorToken, "", Nothing, Nothing)
                    _statementTerminatorToken.SetFlags(GreenNode.NodeFlags.IsNotMissing)
                End If
                Return _statementTerminatorToken
            End Get
        End Property

        Private Shared _colonToken As PunctuationSyntax = Nothing
        Friend Shared ReadOnly Property ColonToken() As PunctuationSyntax
            Get
                If _colonToken Is Nothing Then
                    _colonToken = New PunctuationSyntax(SyntaxKind.ColonToken, "", Nothing, Nothing)
                    _colonToken.SetFlags(GreenNode.NodeFlags.IsNotMissing)
                End If
                Return _colonToken
            End Get
        End Property

        Private Shared _missingExpr As ExpressionSyntax = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("", Nothing, Nothing))
        Friend Shared Function MissingExpression() As ExpressionSyntax
            Return _missingExpr
        End Function

        Private Shared ReadOnly _emptyStatement As EmptyStatementSyntax = SyntaxFactory.EmptyStatement(NotMissingEmptyToken)
        Friend Shared Function EmptyStatement() As EmptyStatementSyntax
            Return _emptyStatement
        End Function

        Private Shared ReadOnly _omittedArgument As OmittedArgumentSyntax = SyntaxFactory.OmittedArgument(NotMissingEmptyToken)
        Friend Shared Function OmittedArgument() As OmittedArgumentSyntax
            Return _omittedArgument
        End Function

        Public Shared Function TypeBlock(ByVal blockKind As SyntaxKind, ByVal begin As TypeStatementSyntax, ByVal [inherits] As SyntaxList(Of InheritsStatementSyntax), ByVal [implements] As SyntaxList(Of ImplementsStatementSyntax), ByVal members As SyntaxList(Of StatementSyntax), ByVal [end] As EndBlockStatementSyntax) As TypeBlockSyntax
            Select Case blockKind
                Case SyntaxKind.ModuleBlock
                    Return SyntaxFactory.ModuleBlock(begin, [inherits], [implements], members, [end])

                Case SyntaxKind.ClassBlock
                    Return SyntaxFactory.ClassBlock(begin, [inherits], [implements], members, [end])

                Case SyntaxKind.StructureBlock
                    Return SyntaxFactory.StructureBlock(begin, [inherits], [implements], members, [end])

                Case SyntaxKind.InterfaceBlock
                    Return SyntaxFactory.InterfaceBlock(begin, [inherits], [implements], members, [end])

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(blockKind)
            End Select
        End Function

        Public Shared Function TypeStatement(ByVal statementKind As SyntaxKind, ByVal attributes As SyntaxList(Of VisualBasicSyntaxNode), ByVal modifiers As SyntaxList(Of VisualBasicSyntaxNode), ByVal keyword As KeywordSyntax, ByVal identifier As IdentifierTokenSyntax, ByVal typeParameterList As TypeParameterListSyntax) As TypeStatementSyntax
            Select Case statementKind
                Case SyntaxKind.ModuleStatement
                    Return SyntaxFactory.ModuleStatement(attributes, modifiers, keyword, identifier, typeParameterList)

                Case SyntaxKind.ClassStatement
                    Return SyntaxFactory.ClassStatement(attributes, modifiers, keyword, identifier, typeParameterList)

                Case SyntaxKind.StructureStatement
                    Return SyntaxFactory.StructureStatement(attributes, modifiers, keyword, identifier, typeParameterList)

                Case SyntaxKind.InterfaceStatement
                    Return SyntaxFactory.InterfaceStatement(attributes, modifiers, keyword, identifier, typeParameterList)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(statementKind)
            End Select
        End Function
    End Class
End Namespace

