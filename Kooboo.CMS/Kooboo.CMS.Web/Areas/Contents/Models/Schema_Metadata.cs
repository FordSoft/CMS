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
using System.Web;
using System.ComponentModel.DataAnnotations;

using Kooboo.Web.Mvc;
using Kooboo.CMS.Content.Persistence;
using Kooboo.CMS.Web.Areas.Contents.Controllers;
using System.Web.Mvc;
using Kooboo.CMS.Content.Services;
using Kooboo.CMS.Content.Models;
using System.ComponentModel;
using System.Web.Routing;
using Kooboo.CMS.Web.Models;
using Kooboo.ComponentModel;
using Kooboo.Web.Mvc.Grid2.Design;
using Kooboo.CMS.Web.Grid2;

namespace Kooboo.CMS.Web.Areas.Contents.Models
{
    [MetadataFor(typeof(Schema))]
    [Grid(Checkable = true, IdProperty = "UUID")]
    public class Schema_Metadata
    {
        [Required(ErrorMessage = "Required")]
        [RemoteEx("IsNameAvailable", "ContentType", RouteFields = "RepositoryName,SiteName", AdditionalFields = "old_Key")]
        [GridColumn(Order = 1, GridColumnType = typeof(SortableGridColumn), GridItemColumnType = typeof(EditGridActionItemColumn))]
        [RegularExpression(RegexPatterns.FileName, ErrorMessage = "A name cannot contain a space or any of the following characters:\\/:*?<>|~")]
        public string Name { get; set; }

        [DisplayName("Tree style data")]
        [Description("Create the tree style data and management interface.")]
        public bool IsTreeStyle
        {
            get;
            set;
        }

        [RegularExpression(RegexPatterns.FileName, ErrorMessage = "The parameters must specify a comma, no spaces or special characters")]
        [DisplayName("Ignored attributes")]
        [Description("The list of attributes to exclude when transmission service API")]
        public string JsonSerializationIgnoreProperties { get; set; }

    }
}