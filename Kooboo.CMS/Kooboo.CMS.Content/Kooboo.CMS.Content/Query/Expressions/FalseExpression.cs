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
    public class FalseExpression : IWhereExpression
    {
        public virtual OQuery OQueryExpression { get; set; }
    }
}
