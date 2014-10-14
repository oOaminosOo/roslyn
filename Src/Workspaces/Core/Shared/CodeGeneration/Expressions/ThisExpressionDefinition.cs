﻿namespace Roslyn.Services.Shared.CodeGeneration
{
    internal class ThisExpressionDefinition : ExpressionDefinition
    {
        public ThisExpressionDefinition()
        {
        }

        protected override CodeDefinition Clone()
        {
            return new ThisExpressionDefinition();
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