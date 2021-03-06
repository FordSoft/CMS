﻿#region License
// 
// Copyright (c) 2013, Kooboo team
// 
// Licensed under the BSD License
// See the file LICENSE.txt for details.
// 
#endregion
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Kooboo.CMS.Content.Models;
using Kooboo.CMS.Content.Persistence.Default;
using System.Diagnostics;
using System.Reflection;
using System.Security.Policy;
using System.Web;
using Kooboo.CMS.Common;
using Kooboo.CMS.Common.Persistence.Non_Relational;
using Kooboo.CMS.Common.Runtime;
using Kooboo.CMS.Content.Query.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OData;
using Sales.Shared.BackEnd;
using Kooboo.Extensions.Extensions;
using Kooboo.CMS.Content.Extensions;
using Kooboo.CMS.Content.Query;
using Newtonsoft.Json.Serialization;
using Sales.Shared.Collections;
using Sales.Shared.Helpers.Auth;

namespace Kooboo.CMS.Content.Persistence.SqlServer
{    
    public class SQLServerTransactionUnit : ITransactionUnit
    {
        public static SQLServerTransactionUnit Current
        {
            get
            {
                return CallContext.Current.GetObject<SQLServerTransactionUnit>("TextContent-SQLServerTransactionUnit");
            }
            set
            {
                CallContext.Current.RegisterObject("TextContent-SQLServerTransactionUnit", value);
            }
        }

        public SQLServerTransactionUnit(Repository repository)
        {
            this.repository = repository;
        }
        private Repository repository;
        private IEnumerable<SqlCommand> commands = new SqlCommand[0];
        private List<Action> postActions = new List<Action>();
        public void RegisterCommand(params SqlCommand[] command)
        {
            commands = commands.Concat(command);
        }
        public void RegisterPostAction(Action action)
        {
            postActions.Add(action);
        }
        public void Rollback()
        {
            Clear();
        }

        public void Commit()
        {
            var connectionString = repository.GetConnectionString();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var trans = conn.BeginTransaction())
                {
                    foreach (var command in commands)
                    {
                        SQLServerHelper.LogCommand(repository, command);
                        try
                        {
                            SQLServerHelper.ResetParameterNullValue(command);
                            command.Transaction = trans;
                            command.Connection = conn;
                            command.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            throw new KoobooException(e.Message + "SQL:" + command.CommandText, e);
                        }
                    }

                    trans.Commit();
                }
            }
            //Execute post content events
            foreach (var action in postActions)
            {
                action();
            }
            Clear();
        }

        private void Clear()
        {
            commands = new SqlCommand[0];
            postActions = new List<Action>();
        }
        public void Dispose()
        {
            Clear();
            Current = null;
        }
    }
    [Kooboo.CMS.Common.Runtime.Dependency.Dependency(typeof(ITextContentProvider), Order = 2)]
    [Kooboo.CMS.Common.Runtime.Dependency.Dependency(typeof(IContentProvider<TextContent>), Order = 2)]
    public class TextContentProvider : ITextContentProvider
    {
        TextContentDbCommands dbCommands = new TextContentDbCommands();
        #region ITextContentProvider Members

        public void AddCategories(Models.TextContent content, params Models.Category[] categories)
        {
            SQLServerHelper.BatchExecuteNonQuery(content.GetRepository(),
                categories.Select(it => dbCommands.AddCategory(content.GetRepository(), it)).ToArray());
        }

        public void DeleteCategories(Models.TextContent content, params Models.Category[] categories)
        {
            SQLServerHelper.BatchExecuteNonQuery(content.GetRepository(),
                 categories.Select(it => dbCommands.DeleteCategory(content.GetRepository(), it)).ToArray());
        }
        public void ClearCategories(TextContent content)
        {
            SQLServerHelper.BatchExecuteNonQuery(content.GetRepository(),
                dbCommands.ClearCategories(content));
        }

        #endregion

        #region IContentProvider<TextContent> Members

        public void Add(Models.TextContent content)
        {
            try
            {
                content.StoreFiles();
                ((IPersistable)content).OnSaving();

                var folder = content.GetFolder().GetActualFolder();
                var schema = content.GetSchema().GetActualSchema();
                if (folder != null && folder.StoreInAPI)
                {
                    var proxy = new BackendProxy();
                    
                    var additionalData = new Dictionary<string, object>()
                    {
                        {"CreatedBy", AuthHelper.GetCurrentUserName()},
                        {"ModifiedBy", AuthHelper.GetCurrentUserName()},
                        {"OwnerId", AuthHelper.GetCurrentUserName()}
                    };

                    //Get payload
                    //
                    var payload = JsonConvert.SerializeObject(content, 
                        new CustomJsonDictionaryConverter(schema.GetJsonSerializationIgnoreProperties(), additionalData));
                    
                    //Send data to API
                    //      
                    proxy.Execute("POST", schema.Name, payload);
                }
                else
                {
                    var command = dbCommands.Add(content);
                    if (command != null)
                    {
                        if (SQLServerTransactionUnit.Current != null)
                        {
                            SQLServerTransactionUnit.Current.RegisterCommand(command);
                            SQLServerTransactionUnit.Current.RegisterPostAction(delegate () { ((IPersistable)content).OnSaved(); });
                        }
                        else
                        {
                            SQLServerHelper.BatchExecuteNonQuery(content.GetRepository(), command);
                            ((IPersistable)content).OnSaved();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

        }
        
        public void Update(Models.TextContent @new, Models.TextContent old)
        {
            @new.StoreFiles();

            ((IPersistable)@new).OnSaving();

            var folder = @new.GetFolder().GetActualFolder();
            var schema = @new.GetSchema().GetActualSchema();
            if (folder != null && folder.StoreInAPI)
            {
                var proxyBackend = new BackendProxy();

                //Add additional data
                //
                var additionalData = new Dictionary<string, object>
                {
                    {"ModifiedBy", AuthHelper.GetCurrentUserName()}
                };
                
                //Get payload
                //
                var payload = JsonConvert.SerializeObject(@new, new CustomJsonDictionaryConverter(schema.GetJsonSerializationIgnoreProperties(), additionalData));

                //Send data to API
                // 
                proxyBackend.Execute("PUT", string.Format("{0}({1})", schema.Name, @new.Id), payload);
            }
            else
            {
                var command = dbCommands.Update(@new);
                if (SQLServerTransactionUnit.Current != null)
                {
                    SQLServerTransactionUnit.Current.RegisterCommand(command);
                    SQLServerTransactionUnit.Current.RegisterPostAction(delegate () { ((IPersistable)@new).OnSaved(); });
                }
                else
                {
                    SQLServerHelper.BatchExecuteNonQuery(@new.GetRepository(), command);
                    ((IPersistable)@new).OnSaved();
                }
            }
        }

        public void Delete(Models.TextContent content)
        {
            var command = dbCommands.Delete(content);
            if (SQLServerTransactionUnit.Current != null)
            {
                SQLServerTransactionUnit.Current.RegisterCommand(command);
                SQLServerTransactionUnit.Current.RegisterPostAction(delegate() { TextContentFileHelper.DeleteFiles(content); });
            }
            else
            {
                SQLServerHelper.BatchExecuteNonQuery(content.GetRepository(), command);
                TextContentFileHelper.DeleteFiles(content);
            }

        }
        
        public static List<string> _schems = new List<string>() { "Test", "Test2" };

        private OQuery CreateMainQuery(string name, string folderName)
        {
            return OQuery
                .From(name)
                .Let("FolderName", folderName)
                .Where("item => FolderName == $FolderName");
        }

        public object GetResult(TextFolder folder, Schema schema, Kooboo.CMS.Content.Query.Expressions.Expression expression, Kooboo.CMS.Content.Query.Expressions.CallType callType)
        {
            try
            {
                switch (callType)
                {
                    //Count
                    //
                    case Query.Expressions.CallType.Count:
                        {
                            var query = Attach(CreateMainQuery(schema.Name + "/Default.Count()", folder.FullName), expression);
                            return Sales.Shared.BackEnd.BackendQuery.Get<int>(query.ToString());                            
                        }
                    //FirstOrDefault
                    //
                    case CallType.First:
                    case CallType.FirstOrDefault:
                        {

                            var query = Attach(CreateMainQuery(schema.Name, folder.FullName).Take(1), expression);
                            var result = ContentHelper.ParseTextContent(schema, BackendQuery.Get(query.ToString()));
                            
                            if (result == null && callType == CallType.First)
                                throw new InvalidOperationException(SR.GetString("NoElements"));

                            if (result == null || result.Length == 0)
                                return result;

                            return result[0];
                        }
                    //List
                    //
                    case CallType.Unspecified:
                        {
                            var query = Attach(CreateMainQuery(schema.Name, folder.FullName), expression);
                            return ContentHelper.ParseTextContent(schema, BackendQuery.Get(query.ToString()));
                        }
                    default: throw new NotImplementedException(string.Format("CallType: '{0}' not implemented", callType));
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        
        public OQuery Attach(OQuery src, IExpression expression)
        {

            if (expression is TakeExpression && expression.OQueryExpression != null)
            {
                src.Take(expression.OQueryExpression._top);
            }
            else if (expression is SkipExpression && expression.OQueryExpression != null)
            {
                src.Skip(expression.OQueryExpression._skip);
            }
            else if (expression is OrderExpression)
            {
                var orderExpression = (OrderExpression)expression;
                if (orderExpression.Descending)
                    src.OrderbyDesc(orderExpression.FieldName);
                else
                    src.Orderby(orderExpression.FieldName);
            }
            else if (expression is AndAlsoExpression)
            {
                var andAlsoExpression = (AndAlsoExpression) expression;
                if (andAlsoExpression.Left != null)
                    Attach(src, andAlsoExpression.Left);

                if (andAlsoExpression.Right != null)
                    Attach(src, andAlsoExpression.Right);
            }
            else if (expression is OrElseExpression)
            {
                var orElseExpression = (OrElseExpression) expression;
                if (orElseExpression.Left != null)
                    Attach(src, orElseExpression.Left);

                if (orElseExpression.Right != null)
                    Attach(src, orElseExpression.Right);

            }


            if (expression.OQueryExpression != null)
            {
                src.Attach(expression.OQueryExpression);
            }


            if (expression is Expression)
            {
                var exp = (Expression)expression;
                if (exp.InnerExpression != null)
                    return Attach(src, exp.InnerExpression);
            }

            return src;
        }

        public object Execute(Query.IContentQuery<Models.TextContent> query)
        {
            /*
            Categories            
            SELECT * FROM [ef1].[dbo].[Test2] category
               WHERE  EXISTS(
                        SELECT ContentCategory.CategoryUUID 
                            FROM [fullips.__ContentCategory] ContentCategory,
                                (SELECT * FROM [ef1].[dbo].[Tests] content WHERE ([UUID] = 'F562CCDW9FGM53WE') AND FolderName='Test' )content
                            WHERE content.UUID = ContentCategory.UUID AND ContentCategory.CategoryUUID = category.UUID 
                      ) AND 1=1 AND FolderName='Test2' ORDER BY Id DESC
                      
            */

            object result = null;
            
            //Get content from API service
            //
            if (query is TextContentQuery && ((TextContentQuery)query).Folder != null && ((TextContentQuery)query).Folder.GetActualFolder().StoreInAPI)
            {
                var folder = ((TextContentQuery)query).Folder;
                var schema = ((Kooboo.CMS.Content.Query.TextContentQuery)query).Schema;

                if (query.Expression is Query.Expressions.CallExpression)
                {
                    result = GetResult(folder, schema, (Expression)query.Expression, ((Query.Expressions.CallExpression)query.Expression).CallType);
                }
                else if (query.Expression is Query.Expressions.TakeExpression
                    || query.Expression is Kooboo.CMS.Content.Query.Expressions.OrderExpression)
                {
                    result = GetResult(folder, schema, (Expression)query.Expression, Kooboo.CMS.Content.Query.Expressions.CallType.Unspecified);
                }
                if (result == null)
                    return new TextContent[] {};

                return result;
            }

            var translator = new QueryProcessor.TextContentTranslator();
            var executor = translator.Translate(query);
            result = executor.Execute();

            return result;
        }

        #endregion


        #region Import/Export
        public IEnumerable<IDictionary<string, object>> ExportSchemaData(Schema schema)
        {
            string sql = string.Format("SELECT * FROM [{0}] ", schema.GetTableName());
            List<IDictionary<string, object>> list = new List<IDictionary<string, object>>();
            SqlConnection connection;
            using (var reader = SQLServerHelper.ExecuteReader(schema.Repository, new SqlCommand() { CommandText = sql }, out connection))
            {
                try
                {
                    while (reader.Read())
                    {
                        list.Add(reader.ToContent<TextContent>(new TextContent()));
                    }
                }
                finally
                {
                    reader.Close();
                    connection.Close();
                }
            }
            return list;
        }

        public IEnumerable<Category> ExportCategoryData(Repository repository)
        {
            string sql = string.Format("SELECT UUID,CategoryFolder,CategoryUUID FROM [{0}] ", repository.GetCategoryTableName());
            List<Category> list = new List<Category>();
            SqlConnection connection;
            using (var reader = SQLServerHelper.ExecuteReader(repository, new SqlCommand() { CommandText = sql }, out connection))
            {
                try
                {
                    while (reader.Read())
                    {
                        Category category = new Category();
                        category.ContentUUID = reader.GetString(0);
                        category.CategoryFolder = reader.GetString(1);
                        category.CategoryUUID = reader.GetString(2);
                        list.Add(category);
                    }

                }
                finally
                {
                    reader.Close();
                    connection.Close();
                }
            }
            return list;
        }

        public void ImportSchemaData(Schema schema, IEnumerable<IDictionary<string, object>> data)
        {
            SQLServerHelper.ExecuteNonQuery(schema.Repository,
             data.Select(it => dbCommands.Add(GetContent(schema, it))).Where(it => it != null).ToArray());

        }
        private static TextContent GetContent(Schema schema, IDictionary<string, object> item)
        {
            var content = new TextContent(item);
            content.Repository = schema.Repository.Name;
            return content;
        }

        public void ImportCategoryData(Repository repository, IEnumerable<Category> data)
        {
            SQLServerHelper.ExecuteNonQuery(repository,
               data.Select(it => dbCommands.AddCategory(repository, it)).ToArray());
        }
        #endregion

        #region ExecuteQuery

        public void ExecuteNonQuery(Repository repository, string queryText, System.Data.CommandType commandType = System.Data.CommandType.Text, params  KeyValuePair<string, object>[] parameters)
        {
            var command = new System.Data.SqlClient.SqlCommand(queryText);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters.Select(it => new SqlParameter() { ParameterName = it.Key, Value = it.Value }).ToArray());
            }
            command.CommandType = commandType;
            SQLServerHelper.ExecuteNonQuery(repository, command);
        }

        public IEnumerable<IDictionary<string, object>> ExecuteQuery(Repository repository, string queryText, System.Data.CommandType commandType = System.Data.CommandType.Text, params  KeyValuePair<string, object>[] parameters)
        {
            var command = new System.Data.SqlClient.SqlCommand(queryText);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters.Select(it => new SqlParameter() { ParameterName = it.Key, Value = it.Value }).ToArray());
            }
            command.CommandType = commandType;
            List<IDictionary<string, object>> list = new List<IDictionary<string, object>>();
            SqlConnection connection;
            using (var dataReader = SQLServerHelper.ExecuteReader(repository, command, out connection))
            {
                try
                {
                    while (dataReader.Read())
                    {
                        TextContent content = new TextContent();
                        dataReader.ToContent(content);
                        list.Add(content);
                    }
                }
                finally
                {
                    dataReader.Close();
                    connection.Close();
                }
            }
            return list;
        }

        public object ExecuteScalar(Repository repository, string queryText, System.Data.CommandType commandType = System.Data.CommandType.Text, params  KeyValuePair<string, object>[] parameters)
        {
            var command = new System.Data.SqlClient.SqlCommand(queryText);
            if (parameters != null && parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters.Select(it => new SqlParameter() { ParameterName = it.Key, Value = it.Value }).ToArray());
            }
            command.CommandType = commandType;
            return SQLServerHelper.ExecuteScalar(repository, command);
        }
        #endregion

        #region CreateTransaction
        public ITransactionUnit CreateTransaction(Repository repository)
        {
            var unit = new SQLServerTransactionUnit(repository);

            SQLServerTransactionUnit.Current = unit;

            return unit;
        }
        #endregion

        #region QueryCategories
        public IEnumerable<Category> QueryCategories(TextContent content)
        {
            List<Category> list = new List<Category>();
            SqlConnection connection;
            using (var dataReader = SQLServerHelper.ExecuteReader(content.GetRepository(),
                dbCommands.QueryCategories(content), out connection))
            {
                try
                {
                    while (dataReader.Read())
                    {
                        Category category = new Category()
                        {
                            CategoryFolder = dataReader.GetString(dataReader.GetOrdinal("CategoryFolder")),
                            CategoryUUID = dataReader.GetString(dataReader.GetOrdinal("CategoryUUID")),
                            ContentUUID = dataReader.GetString(dataReader.GetOrdinal("UUID")),
                        };
                        list.Add(category);
                    }

                }
                finally
                {
                    dataReader.Close();
                    connection.Close();
                }
            }
            return list;
        }
        #endregion
    }
}
