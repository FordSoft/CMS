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
            DeployEnvironment result = null;

            switch (context.Request.Url.Host)
            {
                case "tissot1.brainworks.ru":
                case "tissot2.brainworks.ru":
                case "2nano.brainworks.ru":
                case "demo1.brainworks.ru":
                    {
                        result = new DeployEnvironment();
                        result.SqlServerConfigBaseDirectory = @"G:\sales\Web\Config\demo1";
                        result.ChildSitesBasePhysicalPath = @"G:\sales\Web\Config\demo1\Cms_Data";
                        result.BaseVirtualPath = "~/Config/demo1/Cms_Data/";
                        result.RootDataFile = @"G:\sales\Web\Config\demo1\Cms_Data";

                        break;
                    }
                case "demo2.brainworks.ru":
                    {
                        result = new DeployEnvironment();
                        result.SqlServerConfigBaseDirectory = @"G:\sales\Web\Config\demo2";
                        result.ChildSitesBasePhysicalPath = @"G:\sales\Web\Config\demo2\Cms_Data";
                        result.BaseVirtualPath = "~/Config/demo2/Cms_Data/";
                        result.RootDataFile = @"G:\sales\Web\Config\demo2\Cms_Data";

                        break;
                    }
            }
            if (result != null && !string.IsNullOrWhiteSpace(result.RootDataFile))
            {
                result.ContentPath = Path.Combine(result.RootDataFile, "Contents");
                result.ContentVirtualPath = result.BaseVirtualPath + "Contents";
                result.AccountPath = Path.Combine(result.RootDataFile, "Account");

            }
            return result;
        }
    }
}
