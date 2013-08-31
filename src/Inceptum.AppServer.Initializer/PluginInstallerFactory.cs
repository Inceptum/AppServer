using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Windsor.Installer;

namespace Inceptum.AppServer.Windsor
{
    ///<summary>
    /// Factory to install facilities before installers
    ///</summary>
    public class PluginInstallerFactory : InstallerFactory
    {
        /// <summary>
        /// Select installers in correct order
        /// </summary>
        /// <param name="installerTypes">Installers to select from</param>
        /// <returns>Returns list of installers with facilities on top</returns>
        public override IEnumerable<Type> Select(IEnumerable<Type> installerTypes)
        {
            return installerTypes.OrderBy(i => !i.Name.Contains("Facility"));
        }
    }
}