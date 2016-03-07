using System;
using System.Linq;
using System.Threading;
using Microsoft.Web.Administration;

namespace Kooboo.Extensions.IIS
{
    /// <summary>
    /// IIS helper
    /// </summary>
    public static class IISHelper
    {
        /// <summary>
        /// Gets site state.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        ///     Unpublished
        ///     Starting = 0,
        ///     Started = 1,
        ///     Stopping = 2,
        ///     Stopped = 3,
        ///     Unknown = 4
        /// </returns>
        public static string GetState(string name)
        {
            var iisManager = new ServerManager();
            var site = GetSite(iisManager, name, false);
            return site == null ? "Unpublished" : site.State.ToString();
        }

        /// <summary>
        /// Cnange sites the state.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="state">The state.</param>
        public static void SiteState(string name, StateOperationSite state)
        {
            var iisManager = new ServerManager();
            var site = GetSite(iisManager, name);
            
            switch (state)
            {
                case StateOperationSite.Start:
                    {
                        site.Start();
                        break;
                    }
                case StateOperationSite.Stop:
                    {
                        site.Stop();
                        break;
                    }
                case StateOperationSite.Restart:
                    {
                        site.Stop();
                        site.Start();
                        break;
                    }
            }
        }

        /// <summary>
        /// Create the web site.
        /// </summary>
        /// <param name="appPoolName">Name of the application pool.</param>
        /// <param name="name">The name.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="ip">The ip.</param>
        /// <param name="domains">The domains.</param>
        /// <param name="port">The port.</param>
        /// <param name="physicalPath">The physical path.</param>
        public static void CreateWebSite(string appPoolName, string name, string protocol, string ip, string[] domains, string port, string physicalPath)
        {
            var iisManager = new ServerManager();
            var site = GetSite(iisManager, name, false);
            if (site != null)
            {
                site.Stop();
            }

            if (site == null)
            {
                var mainDomain = domains[0];
                string bindingInfo = string.Format(@"{0}:{1}:{2}", ip, port, mainDomain);
                iisManager.Sites.Add(name, "http", bindingInfo, physicalPath);
                iisManager.CommitChanges();
                site = iisManager.Sites.First(s => s.Name == name);
                site.ServerAutoStart = true;
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

            Thread.Sleep(1000);
            site.Start();
        }

        /// <summary>
        /// Gets the site.
        /// </summary>
        /// <param name="serverManager">The server manager.</param>
        /// <param name="name">The name.</param>
        /// <param name="hasThrow">if set to <c>true</c> [has throw].</param>
        /// <returns></returns>
        /// <exception cref="System.NullReferenceException"></exception>
        private static Site GetSite(ServerManager serverManager, string name, bool hasThrow = true)
        {
            var site = serverManager.Sites.FirstOrDefault(s => s.Name == name);
            if (site == null && hasThrow)
                throw new NullReferenceException(string.Format("site not found: '{0}'", name));

            return site;
        }

        /// <summary>
        /// Adds the application pools.
        /// </summary>
        /// <param name="applicationPoolName">Name of the application pool.</param>
        /// <param name="site">The site.</param>
        private static void AddApplicationPools(string applicationPoolName, Site site)
        {
            foreach (var app in site.Applications)
            {
                app.ApplicationPoolName = applicationPoolName;
            }
        }

        /// <summary>
        /// Adds the bindings.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <param name="bindings">The bindings.</param>
        /// <param name="protocol">The protocol.</param>
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
