using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Web.Administration;

namespace Kooboo.Extensions
{
    public static class IISManagerHelper
    {
        public static void CreateWebSite(string appPoolName, string name, string protocol, string ip, string[] domains, string port, string physicalPath)
        {
            try
            {
                var iisManager = new ServerManager();
                
                var site = iisManager.Sites.FirstOrDefault(s => s.Name == name);
                if (site == null)
                {
                    var mainDomain = domains[0];
                    string bindingInfo = string.Format(@"{0}:{1}:{2}", ip, port, mainDomain);
                    iisManager.Sites.Add(name, "http", bindingInfo, physicalPath);
                    iisManager.CommitChanges();
                    site = iisManager.Sites.First(s => s.Name == name);

                    site.Applications.First().ApplicationPoolName = appPoolName;
                }
                //add bindings
                //
                AddBindings(site, domains.Select(d => string.Format(@"{0}:{1}:{2}", ip, port, d)).ToArray(), protocol);

                //change application pool
                //
                AddApplicationPools(appPoolName, site);

                //save changes
                //
                iisManager.CommitChanges();
                
                site.Start();
            }
            catch (Exception e)
            {
                
                throw e;
            }
        }

        private static void AddApplicationPools(string applicationPoolName, Site site)
        {
            foreach (var app in site.Applications)
            {
                app.ApplicationPoolName = applicationPoolName;
            }
        }

        private static void AddBindings(Site site, string[] bindings, string protocol)
        {
            site.Bindings.Clear();
            foreach (var binding in bindings)
            {
                site.Bindings.Add(binding, protocol);
            }
        }
    }
}
