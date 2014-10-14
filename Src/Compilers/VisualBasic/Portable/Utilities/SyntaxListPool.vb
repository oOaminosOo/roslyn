﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
    Friend Class SyntaxListPool
        Private _freeList As New Stack(Of SyntaxListBuilder)

        Friend Function Allocate() As SyntaxListBuilder
            If _freeList.Count > 0 Then
                Return _freeList.Pop
            End If
            Return SyntaxListBuilder.Create()
        End Function

        Friend Function Allocate(Of TNode As VBSyntaxNode)() As SyntaxListBuilder(Of TNode)
            Return New SyntaxListBuilder(Of TNode)(Me.Allocate)
        End Function

        Friend Function AllocateSeparated(Of TNode As VBSyntaxNode)() As SeparatedSyntaxListBuilder(Of TNode)
            Return New SeparatedSyntaxListBuilder(Of TNode)(Me.Allocate)
        End Function

        Friend Sub Free(item As SyntaxListBuilder)
            If item IsNot Nothing Then
                item.Clear()
                _freeList.Push(item)
            End If
        End Sub
    End Class
End Namespace


