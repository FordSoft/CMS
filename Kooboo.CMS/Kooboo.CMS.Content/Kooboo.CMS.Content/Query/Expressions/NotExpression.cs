﻿#region License
// 
// Copyright (c) 2013, Kooboo team
// 
// Licensed under the BSD License
// See the file LICENSE.txt for details.
// 
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OData;

namespace Kooboo.CMS.Content.Query.Expressions
{
    public class NotExpression : IWhereExpression
    {
        public NotExpression(IWhereExpression expression)
        {
            InnerExpression = expression;
        }
        public IWhereExpression InnerExpression { get; private set; }
        public virtual OQuery OQueryExpression { get; set; }
    }
}
