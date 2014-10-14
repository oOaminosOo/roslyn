﻿using System.Collections.Generic;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class VariableDeclarationDefinition : CodeDefinition
    {
        public ITypeSymbol TypeOpt { get; private set; }
        public IList<CommonSyntaxNode> VariableDeclarators { get; private set; }

        public VariableDeclarationDefinition(ITypeSymbol typeOpt, IList<CommonSyntaxNode> variableDeclarators)
        {
            this.TypeOpt = typeOpt;
            this.VariableDeclarators = variableDeclarators;
        }

        protected override CodeDefinition Clone()
        {
            return new VariableDeclarationDefinition(this.TypeOpt, this.VariableDeclarators);
        }

        public override void Accept(ICodeDefinitionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(ICodeDefinitionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override TResult Accept<TArgument, TResult>(ICodeDefinitionVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.Visit(this, argument);
        }
    }
}