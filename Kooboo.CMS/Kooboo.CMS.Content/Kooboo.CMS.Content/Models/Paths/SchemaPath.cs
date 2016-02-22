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
using Kooboo.Web.Url;
using System.IO;
using System.Net;
using Kooboo.CMS.Form;


namespace Kooboo.CMS.Content.Models.Paths
{
    public class SchemaPath : IPath
    {
        public SchemaPath(Schema schema)
        {
            if (schema == null)
                return;

            string schemaName = schema.Name;

            //fix link schems
            //
            if (schema.Name.Contains(@"\"))
            {
                var dir = new DirectoryInfo(schema.Name);
                schemaName = dir.Name;
                this.PhysicalPath = schema.Name;
                this.SettingFile = Path.Combine(PhysicalPath, PathHelper.SettingFileName);
                VirtualPath = UrlUtility.GetVirtualPath(PhysicalPath);
            }
            else
            {
                RepositoryPath repositoryPath = new RepositoryPath(schema.Repository);
                var basePhysicalPath = GetBaseDir(schema.Repository);
                this.PhysicalPath = Path.Combine(basePhysicalPath, schemaName);
                this.SettingFile = Path.Combine(PhysicalPath, PathHelper.SettingFileName);

                VirtualPath = UrlUtility.RawCombine(repositoryPath.VirtualPath, PATH_NAME, schemaName);
            }
        }
        public static string GetBaseDir(Repository repository)
        {
            return Path.Combine(new RepositoryPath(repository).PhysicalPath, PATH_NAME);
        }
        const string PATH_NAME = "Schemas";
        #region IPath Members

        public string PhysicalPath
        {
            get;
            private set;
        }

        public string VirtualPath
        {
            get;
            private set;
        }

        public string SettingFile
        {
            get;
            private set;
        }


        public bool Exists()
        {
            return File.Exists(this.SettingFile);
        }

        public void Rename(string newName)
        {
            IO.IOUtility.RenameFile(this.PhysicalPath, @newName + ".config");
        }

        #endregion       
    }
}
