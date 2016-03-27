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
    public class WhereContainsExpression : BinaryExpression
    {
        public WhereContainsExpression(string fieldName, object value)
            : this(null, fieldName, value)
        {

        }
        public WhereContainsExpression(IExpression expression, string fieldName, object value)
            : base(expression, fieldName, value)
        {
            OQueryExpression = OQuery.From(null).Where(string.Format("item => item.{0}.Contains('{1}')", fieldName, value));
        }

    }
}
