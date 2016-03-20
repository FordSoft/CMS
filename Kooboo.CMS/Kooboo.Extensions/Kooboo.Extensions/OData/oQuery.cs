using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OData
{
    /// <summary>
    /// oQuery class is used to provide a fluent style API for generating OData query fragments on WP7
    /// 
    /// Example:
    /// var query = OQuery.From("Titles")
    ///                   .Let("lastYear", new DateTime(2010, 1, 1))
    ///                   .Where("item => item.DateModified >= $lastYear")
    ///                   .Take(100)
    ///                   .Skip(50);
    /// Uri uri = new Uri("http://odata.netflix.com/Catalog"+query.ToString());
    /// </summary>
    public class OQuery
    {
        private string _collection;
        private List<string> _whereClauses = new List<string>();
        private List<string> _orderByClauses = new List<string>();
        private List<string> _expandClauses = new List<string>();
        private Dictionary<string, object> _letMap = new Dictionary<string, object>();
        private string _select;
        public int _skip = 0;
        public int _top = 0;

        private int _iCh = 0;
        private string _clause = "";
        private StringBuilder _newClause;
        private string _itemReference = "";

        public static OQuery From(string collection)
        {
            return new OQuery(collection ?? "__");
        }

        public OQuery Attach(OQuery query)
        {
            //if (query._letMap != null && q)

            //if (query._top != _top)
            //{
            //    _top = query._top;
            //}

            //if (query._skip != _skip)
            //{
            //    _skip = query._skip;
            //}

            if (query._orderByClauses.Any())
            {
                query._orderByClauses.ForEach(c => _orderByClauses.Add(c));
            }

            if (query._whereClauses.Any())
            {
                query._whereClauses.ForEach(c => _whereClauses.Add(c));
            }

            //Let's
            //
            if (query._letMap.Any())
            {
                foreach (var let in query._letMap)
                {
                    _letMap[let.Key] = let.Value;
                }
            }

            //TODO
            if (query._select != _select)
            {
                query._select = _select;
            }

            if (query._expandClauses.Any())
            {
                query._expandClauses.ForEach(c => _expandClauses.Add(c));
            }
            //LET, SELECT
            return this;
        }


        private OQuery(string collection)
        {
            _collection = collection;
            if (collection[0] != '/')
                _collection = "/" + collection;
            else
                _collection = collection;

            _iCh = 0;
        }


        /// <summary>
        /// Allows you to assign "local" names so you can refer to them from the where clause
        /// Example:
        ///     var foobar=123.0;
        ///  oQuery.From("Items")
        ///         .Let("x", foobar)
        ///         .Where("$.Length > $foobar")
        /// </summary>
        /// <param name="name" type="String">
        ///     The name you want to give the expression
        /// </param>
        /// <param name="value" type="object">
        ///     The value  you want to have substitued in the where clause
        /// </param>
        /// <returns type="oQuery" />
        public OQuery Let(string name, object value)
        {
            _letMap.Add(name, value);
            return this;
        }

        /// <summary>
        /// Allows you to limit the result set to count items
        /// Example:
        ///  oQuery.From("Items")
        ///         .Take(10)
        /// </summary>
        /// <param name="count" type="number">
        ///     The count of items to take
        /// </param>
        /// <returns type="oQuery" />
        public OQuery Take(int count)
        {
            _top = count;
            return this;
        }

        /// <summary>
        /// Allows you to skip count items
        /// Example:
        ///  oQuery.From("Items")
        ///         .Skip(10)
        /// </summary>
        /// <param name="count" type="number">
        ///     The count of items to skip
        /// </param>
        /// <returns type="oQuery" />
        public OQuery Skip(int count)
        {
            _skip = count;
            return this;
        }

        /// <summary>
        /// Allows you to expand property 
        /// Example:
        ///  oQuery.From("Items")
        ///         .Expand("Foo.Bar, Blat.Blot")
        /// </summary>
        /// <param name="propertyList" type="string">
        ///     comma delimited list of property paths with a . as a seperator
        /// </param>
        /// <returns type="oQuery" />
        public OQuery Expand(string propertyList)
        {
            var properties = propertyList.Split(new char[] { ',', ' ' });
            return this.Expand(properties);
        }

        /// <summary>
        /// Allows you to expand property 
        /// Example:
        ///  oQuery.From("Items")
        ///         .Expand("Foo.Bar, Blat.Blot")
        /// </summary>
        /// <param name="propertyList" type="string">
        ///     comma delimited list of property paths with a . as a seperator
        /// </param>
        /// <returns type="oQuery" />
        public OQuery Expand(IEnumerable<string> properties)
        {
            foreach (var prop in properties)
            {
                _expandClauses.Add(prop);
            }
            return this;
        }

        /// <summary>
        /// Allows you to specify orderby clause
        /// Example:
        ///  oQuery.From("Items")
        ///         .OrderBy("Foo.Bar")
        /// </summary>
        /// <param name="propertyList" type="string">
        ///     property paths with a . as a seperator
        /// </param>
        /// <returns type="oQuery" />
        public OQuery Orderby(string property)
        {
            _orderByClauses.Add(_parsePropertyPath(property));
            return this;
        }

        /// <summary>
        /// Allows you to specify orderby clause with descending order
        /// Example:
        ///  oQuery.From("Items")
        ///         .OrderByDesc("Foo.Bar")
        /// </summary>
        /// <param name="property" type="string">
        ///     property paths with a . as a seperator
        /// </param>
        /// <returns type="oQuery" />
        public OQuery OrderbyDesc(string property)
        {
            _orderByClauses.Add(_parsePropertyPath(property) + " desc");
            return this;
        }

        /// <summary>
        /// Allows you to specify a WHERE clause
        /// Example:
        ///  oQuery.From("Items")
        ///         .Where("$.x.Y > 13 && $.foo.Contains('blat')")
        /// NOTE: If you call WHERE multiple times there is an implicit AND between the clauses
        /// </summary>
        /// <param name="clause" type="string">
        ///     a string which represents the expression in C# style syntax
        ///  Property references to the object in the collection are via $.Property1.Property2 syntax
        ///  LET property references are simply $NAME where NAME is the value given to the LET clause
        ///  All functions which are in C# style will get mapped correctly to ODATA URI format
        ///      Example: "$.foo.blat.Contains('text')" becomes Substringof(foo/blat, 'text')
        /// </param>
        /// <returns type="oQuery" />
        public OQuery Where(string clause)
        {
            _whereClauses.Add(clause);
            return this;
        }

        /// <summary>
        /// Allows you to specify the projection and also returns the URI fragment for the whole oQuery 
        /// Example:
        ///  oQuery.From("Items")
        ///         .OrderByDesc("Foo.Bar, Blat.Blot")
        ///         .Select()
        /// </summary>
        /// <param name="propertyList" type="string">
        ///     comma delimited list of property paths with a . as a seperator
        /// </param>
        /// <returns type="oQuery" />
        public OQuery Select(string propertyList)
        {
            _select = propertyList;
            return this;
        }

        /// <summary>
        /// Allows you to specify the projection and also returns the URI fragment for the whole oQuery 
        /// Example:
        ///  oQuery.From("Items")
        ///         .OrderByDesc("Foo.Bar, Blat.Blot")
        ///         .Select()
        /// </summary>
        /// <param name="properties" type="IEnumerable<string>">
        ///     list of property paths with a . as a seperator
        /// </param>
        /// <returns type="oQuery" />
        public OQuery Select(IEnumerable<string> properties)
        {
            _select = String.Join(",", properties.ToArray());
            return this;
        }

        /// <summary>
        /// returns URI representation of query
        /// </summary>
        /// <returns type="string">string for uri that can be used to submit to ODATA service</returns>
        public override string ToString()
        {
            _iCh = 0;
            _clause = "";
            _newClause = new StringBuilder();
            _itemReference = "";
            StringBuilder uri = new StringBuilder(_collection);
            var clauseConnector = "?";

            // $filter
            if (_whereClauses.Count > 0)
            {
                uri.AppendFormat("{0}$filter=", clauseConnector);
                for (var iWhere = 0; iWhere < _whereClauses.Count; iWhere++)
                {
                    if (iWhere > 0)
                        uri.Append(" and ");
                    uri.AppendFormat("({0})", _parseFilter(_whereClauses[iWhere]));
                }
                clauseConnector = "&";
            }

            // $orderby
            if (_orderByClauses.Count > 0)
            {
                uri.AppendFormat("{0}$orderby=", clauseConnector);
                for (var iOrderBy = 0; iOrderBy < _orderByClauses.Count; iOrderBy++)
                {
                    if (iOrderBy > 0)
                        uri.Append(",");
                    uri.Append(_parsePropertyPath(_orderByClauses[iOrderBy]));
                }
                clauseConnector = "&";
            }

            // $skip
            if (_skip != 0)
            {
                uri.AppendFormat("{0}$skip={1}", clauseConnector, _skip);
                clauseConnector = "&";
            }

            // $top
            if (_top != 0)
            {
                uri.AppendFormat("{0}$top={1}", clauseConnector, _top);
                clauseConnector = "&";
            }

            // $expand
            if (_expandClauses.Count > 0)
            {
                uri.AppendFormat("{0}$expand=", clauseConnector);
                for (var iExpand = 0; iExpand < _expandClauses.Count; iExpand++)
                {
                    if (iExpand > 0)
                        uri.Append(",");
                    uri.Append(_parsePropertyPath(_expandClauses[iExpand]));
                }
                clauseConnector = "&";
            }

            // select
            if (!String.IsNullOrEmpty(_select))
            {
                var selectPaths = _select.Split(new char[] { ',' });

                uri.AppendFormat("{0}$select=", clauseConnector);

                for (var iSelect = 0; iSelect < selectPaths.Length; iSelect++)
                {
                    if (iSelect > 0)
                        uri.Append(",");
                    uri.Append(_parsePropertyPath(selectPaths[iSelect]));
                }
                clauseConnector = "&";
            }

            if (true)
            {
                uri.Append("&$count=true");
            }
            return uri.ToString();
        }


        public string _parseFilter(string whereClause)
        {
            _iCh = 0;
            _newClause = new StringBuilder();

            var i = whereClause.IndexOf("=>");
            if (i >= 0)
            {
                _itemReference = whereClause.Substring(0, i).Trim() + '.';
                _clause = whereClause.Substring(i + 2);
            }

            _parse();

            return _newClause.ToString();
        }

        public void _parse()
        {

            for (; _iCh < _clause.Length; _iCh++)
            {
                var ch = _clause[_iCh];

                switch (ch)
                {
                    case '$':
                        // then it's a LET reference
                        _parseLetReference();
                        break;

                    case '+':
                        _newClause.Append(" add ");
                        break;

                    case '-':
                        _newClause.Append(" sub ");
                        break;

                    case '*':
                        _newClause.Append(" mul ");
                        break;

                    case '/':
                        _newClause.Append(" div ");
                        break;

                    case '%':
                        _newClause.Append(" mod ");
                        break;

                    case '>':
                        {
                            var ch2 = _clause[_iCh + 1];
                            if (ch2 == '=')
                            {
                                _newClause.Append(" ge ");
                                _iCh++;
                            }
                            else
                            {
                                _newClause.Append(" gt ");
                            }
                        }
                        break;

                    case '<':
                        {
                            var ch2 = _clause[_iCh + 1];
                            if (ch2 == '=')
                            {
                                _newClause.Append(" le ");
                                _iCh++;
                            }
                            else
                            {
                                _newClause.Append(" lt ");
                            }
                        }
                        break;

                    case '=':
                        {
                            var ch2 = _clause[_iCh + 1];
                            if (ch2 == '=')
                            {
                                _newClause.Append(" eq ");
                                _iCh++;
                            }
                            else
                            {
                                // THIS IS INVALID, SHOULD I ALLOW?
                                _newClause.Append(ch);
                            }
                        }
                        break;

                    case '!':
                        {
                            var ch2 = _clause[_iCh + 1];
                            if (ch2 == '=')
                            {
                                _newClause.Append(" neq ");
                                _iCh++;
                            }
                            else
                            {
                                // THIS IS INVALID, SHOULD I ALLOW?
                                _newClause.Append(" ! ");
                            }
                        }
                        break;

                    case '|':
                        {
                            var ch2 = _clause[_iCh + 1];
                            if (ch2 == '|')
                            {
                                _newClause.Append(" or ");
                                _iCh++;
                            }
                            else
                            {
                                // THIS IS INVALID, SHOULD I ALLOW?
                                _newClause.Append(" | ");
                            }
                        }
                        break;

                    case '&':
                        {
                            var ch2 = _clause[_iCh + 1];
                            if (ch2 == '&')
                            {
                                _newClause.Append(" and ");
                                _iCh++;
                            }
                            else
                            {
                                // THIS IS INVALID, SHOULD I ALLOW?
                                _newClause.Append(" & ");
                            }
                        }
                        break;

                    case '\'':
                        _parseQuotedString();
                        break;


                    default:
                        //if(_clause.substr(_iCh, _itemReference.Count) == _itemReference)
                        if (_clause.IndexOf(_itemReference, _iCh) == _iCh)
                        {
                            _iCh += _itemReference.Length;
                            _parseObjectReference();
                        }
                        else
                            _newClause.Append(ch);
                        break;
                }
            }
        }

        public void _parseQuotedString()
        {
            // add quote
            var ch = _clause[_iCh++];
            _newClause.Append(ch);

            for (; _iCh < _clause.Length; _iCh++)
            {
                ch = _clause[_iCh];
                if (ch == '\'')
                {
                    // check for double quote
                    if ((_iCh + 1) < _clause.Length)
                    {
                        var ch2 = _clause[_iCh + 1];
                        if (ch2 != '\'')
                        {
                            _newClause.Append(ch);
                            return; // all done
                        }
                        else
                        {
                            _newClause.Append(ch);
                            _newClause.Append(ch2);
                            _iCh++;
                        }
                    }
                    else
                    {
                        _newClause.Append(ch);
                        return; // all done
                    }
                }
                else
                    _newClause.Append(ch);
            }
        }


        public void _parseLetReference()
        {
            // skip "$"
            _iCh++;

            var reference = "";
            for (; _iCh < _clause.Length; _iCh++)
            {
                var ch = _clause[_iCh];
                if (!Char.IsLetterOrDigit(ch))
                {
                    _iCh--; // move cursor back so other functions can use it
                    break;
                }
                else
                    reference += ch;
            }

            if (_letMap.ContainsKey(reference))
            {
                object value = _letMap[reference];
                switch (value.GetType().FullName)
                {
                    case "System.DateTime":
                        _newClause.AppendFormat("datetime'{0}Z'", ((DateTime)value).ToString("s"));
                        break;
                    case "System.Int16":
                    case "System.Int32":
                    case "System.Int64":
                        _newClause.Append(value.ToString());
                        break;

                    case "System.String":
                        // TODO Need better quote handling for escaping, this is not correct
                        _newClause.AppendFormat("'{0}'", value.ToString().Replace("'", "''"));
                        break;
                    default:
                        // TODO Need better quote handling for escaping, this is not correct
                        _newClause.AppendFormat("'{0}'", value.ToString().Replace("'", "''"));
                        break;
                }
            }
            else
            {
                // must not have been a LET reference, just pass through
                _newClause.AppendFormat("${0}", reference);
                return;
            }

        }

        public string _parseObjectReference()
        {
            string reference = "";

            for (; _iCh < _clause.Length; _iCh++)
            {
                var ch = _clause[_iCh];
                // replace . with /
                if (ch == '.')
                {
                    var current = _clause.Substring(_iCh);
                    if (_mapFunctionSwap(current, reference, "Contains", "contains"))
                        return "";
                    if (_mapFunction(current, reference, "EndsWith"))
                        return "";
                    if (_mapFunction(current, reference, "StartsWith"))
                        return "";
                    if (_mapFunction(current, reference, "Length"))
                        return "";
                    if (_mapFunction(current, reference, "IndexOf"))
                        return "";
                    if (_mapFunction(current, reference, "Replace"))
                        return "";
                    if (_mapFunction(current, reference, "Substring"))
                        return "";
                    if (_mapFunction(current, reference, "ToLower"))
                        return "";
                    if (_mapFunction(current, reference, "ToUpper"))
                        return "";
                    if (_mapFunction(current, reference, "Trim"))
                        return "";
                    if (_mapFunction(current, reference, "Concat"))
                        return "";
                    if (_mapProperty(current, reference, "Day"))
                        return "";
                    if (_mapProperty(current, reference, "Hour"))
                        return "";
                    if (_mapProperty(current, reference, "Minute"))
                        return "";
                    if (_mapProperty(current, reference, "Month"))
                        return "";
                    if (_mapProperty(current, reference, "Second"))
                        return "";
                    if (_mapProperty(current, reference, "Year"))
                        return "";
                    reference += "/";
                }
                else if (Char.IsLetterOrDigit(ch))
                    reference += ch;
                else
                {
                    _iCh--; // move cursor back so next public can use it
                            // this could be a public invocation
                    _newClause.Append(reference);
                    return reference; // all done
                }
            }
            return reference;
        }


        public string _parsePropertyPath(string propertyPath)
        {
            if (propertyPath.StartsWith("$."))
                propertyPath = propertyPath.Substring(2);
            return propertyPath.Replace(".", "/");
        }

        public bool _mapFunctionSwap(string current, string reference, string inFunction, string outFunction)
        {
            var f = '.' + inFunction + "(";
            if (String.IsNullOrEmpty(outFunction))
                outFunction = inFunction.ToLower();

            if (current.StartsWith(f))
            {
                string value = current.Substring(f.Length);
                int iEnd = value.IndexOf('"', 1);
                value = value.Substring(0, iEnd + 1);

                if (outFunction == "contains")
                    _newClause.AppendFormat("{0}({2},{1}", outFunction, value, reference);
                else
                    _newClause.AppendFormat("{0}({1},{2}", outFunction, value, reference);

                _iCh += f.Length + iEnd;
                return true;
            }
            return false;
        }

        public bool _mapFunction(string current, string reference, string inFunction)
        {
            return _mapFunction(current, reference, inFunction, inFunction);
        }

        public bool _mapFunction(string current, string reference, string inFunction, string outFunction)
        {
            var f = '.' + inFunction + "(";

            if (String.IsNullOrEmpty(outFunction))
                outFunction = inFunction.ToLower();

            if (current.StartsWith(f))
            {
                _newClause.AppendFormat("{0}({1},", outFunction, reference);
                _iCh += f.Length - 1;
                return true;
            }
            return false;
        }

        public bool _mapProperty(string current, string reference, string inFunction)
        {
            return _mapProperty(current, reference, inFunction, inFunction);
        }

        public bool _mapProperty(string current, string reference, string inFunction, string outFunction)
        {
            var f = '.' + inFunction;

            if (!String.IsNullOrEmpty(outFunction))
                outFunction = inFunction.ToLower();

            var chEnd = current[f.Length];
            if (current.StartsWith(f) &&
                !Char.IsLetterOrDigit(chEnd) &&
               (chEnd != '.'))
            {
                _newClause.AppendFormat("{0}({1})", outFunction, reference);
                _iCh += f.Length;
                return true;
            }
            return false;
        }
    }
}