#region License
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
    public class AndAlsoExpression : IWhereExpression
    {
        public AndAlsoExpression(IWhereExpression left, IWhereExpression right)
        {
            this.Left = left;
            this.Right = right;
        }
        public IWhereExpression Left { get; private set; }
        public IWhereExpression Right { get; private set; }
        public virtual OQuery OQueryExpression { get; set; }
    }
}
