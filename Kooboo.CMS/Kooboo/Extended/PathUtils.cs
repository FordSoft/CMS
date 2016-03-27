using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace Kooboo.Extended
{

    public class DeployEnvironment
    {
        public string SqlServerConfigBaseDirectory { get; set; }

        public string ChildSitesBasePhysicalPath { get; set; }

        public string BaseVirtualPath { get; set; }

        public string RootDataFile { get; set; }

        public string ContentPath { get; set; }
        public string ContentVirtualPath { get; set; }

        public string AccountPath { get; set; }
    }

    public static class PathUtils
    {
        public static DeployEnvironment GetDeployEnvironment(HttpContext context)
        {
            var result = new DeployEnvironment();

            switch (context.Request.Url.Port)
            {
                case 81:
                    {
                        result.SqlServerConfigBaseDirectory = @"C:\git\Kooboo.Cms\CMS\Kooboo.CMS\Kooboo.CMS.Web\Config\demo1";
                        result.ChildSitesBasePhysicalPath = @"C:\git\Kooboo.Cms\CMS\Kooboo.CMS\Kooboo.CMS.Web\Config\demo1\Cms_Data";
                        result.BaseVirtualPath = "~/Config/demo1/Cms_Data/";
                        result.RootDataFile = @"C:\git\Kooboo.Cms\CMS\Kooboo.CMS\Kooboo.CMS.Web\Config\demo1\Cms_Data";

                        break;
                    }
                case 82:
                    {
                        result.SqlServerConfigBaseDirectory = @"C:\git\Kooboo.Cms\CMS\Kooboo.CMS\Kooboo.CMS.Web\Config\demo2";
                        result.ChildSitesBasePhysicalPath = @"C:\git\Kooboo.Cms\CMS\Kooboo.CMS\Kooboo.CMS.Web\Config\demo2\Cms_Data";
                        result.BaseVirtualPath = "~/Config/demo2/Cms_Data/";
                        result.RootDataFile = @"C:\git\Kooboo.Cms\CMS\Kooboo.CMS\Kooboo.CMS.Web\Config\demo2\Cms_Data";

                        break;
                    }
            }
            if (!string.IsNullOrWhiteSpace(result.RootDataFile))
            {
                result.ContentPath = Path.Combine(result.RootDataFile, "Contents");
                result.ContentVirtualPath = result.BaseVirtualPath + "Contents";
                result.AccountPath = Path.Combine(result.RootDataFile, "Account");

            }
            return result;            
        }
    }
}
