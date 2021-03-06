﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class ParameterSymbolKey : AbstractSymbolKey<ParameterSymbolKey>
        {
            private readonly SymbolKey containerKey;
            private readonly string metadataName;

            internal ParameterSymbolKey(IParameterSymbol symbol, Visitor visitor)
            {
                this.containerKey = GetOrCreate(symbol.ContainingSymbol, visitor);
                this.metadataName = symbol.MetadataName;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var container = containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var parameters = GetAllSymbols(container).SelectMany(s => Resolve(compilation, s));
                return CreateSymbolInfo(parameters);
            }

            private IEnumerable<IParameterSymbol> Resolve(Compilation compilation, ISymbol container)
            {
                if (container is IMethodSymbol)
                {
                    return ((IMethodSymbol)container).Parameters.Where(p => Equals(compilation, p.MetadataName, this.metadataName));
                }
                else if (container is IPropertySymbol)
                {
                    return ((IPropertySymbol)container).Parameters.Where(p => Equals(compilation, p.MetadataName, this.metadataName));
                }
                else
                {
                    return SpecializedCollections.EmptyEnumerable<IParameterSymbol>();
                }
            }

            internal override bool Equals(ParameterSymbolKey other, ComparisonOptions options)
            {
                return
                    Equals(options.IgnoreCase, other.metadataName, this.metadataName) &&
                    other.containerKey.Equals(this.containerKey, options);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(
                    GetHashCode(options.IgnoreCase, this.metadataName),
                    this.containerKey.GetHashCode(options));
            }
        }
    }
}