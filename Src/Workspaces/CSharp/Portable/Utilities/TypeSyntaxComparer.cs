﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal class TypeSyntaxComparer : IComparer<TypeSyntax>
    {
        private readonly IComparer<SyntaxToken> tokenComparer;
        internal IComparer<NameSyntax> NameComparer;

        internal TypeSyntaxComparer(IComparer<SyntaxToken> tokenComparer)
        {
            this.tokenComparer = tokenComparer;
        }

        public int Compare(TypeSyntax x, TypeSyntax y)
        {
            if (x == y)
            {
                return 0;
            }

            x = UnwrapType(x);
            y = UnwrapType(y);

            if (x is NameSyntax && y is NameSyntax)
            {
                return NameComparer.Compare((NameSyntax)x, (NameSyntax)y);
            }

            // we have two predefined types, or a predefined type and a normal C# name.  We only need
            // to compare the first tokens here.
            return tokenComparer.Compare(x.GetFirstToken(includeSkipped: true), y.GetFirstToken());
        }

        private TypeSyntax UnwrapType(TypeSyntax type)
        {
            while (true)
            {
                switch (type.CSharpKind())
                {
                    case SyntaxKind.ArrayType:
                        type = ((ArrayTypeSyntax)type).ElementType;
                        break;
                    case SyntaxKind.PointerType:
                        type = ((PointerTypeSyntax)type).ElementType;
                        break;
                    case SyntaxKind.NullableType:
                        type = ((NullableTypeSyntax)type).ElementType;
                        break;
                    default:
                        return type;
                }
            }
        }
    }
}