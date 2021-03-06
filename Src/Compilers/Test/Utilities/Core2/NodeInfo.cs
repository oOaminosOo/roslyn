﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    //Contains the information about a SyntaxNode that is difficult to get from a variable
    //just typed as SyntaxNode. This is name/type/value information for all fields and children.
    public partial class NodeInfo
    {
        private readonly string _className;
        private readonly FieldInfo[] _fieldInfos;
        private static readonly FieldInfo[] _emptyFieldInfos = { };

        public string ClassName
        {
            get
            {
                return _className;
            }
        }

        public FieldInfo[] FieldInfos
        {
            get
            {
                if (_fieldInfos == null)
                {
                    return _emptyFieldInfos;
                }
                else
                {
                    return _fieldInfos;
                }
            }
        }

        public NodeInfo(string className, FieldInfo[] fieldInfos)
        {
            _className = className;
            _fieldInfos = fieldInfos;
        }
    }
}