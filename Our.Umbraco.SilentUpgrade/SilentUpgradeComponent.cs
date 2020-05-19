/*
 *  Silent Upgrade for Umbraco v8.
 *  ---------------------------------
 * 
 *  HEALTH WARNING: Look this is doing some internal umbraco 'messing' about, using this
 *                  to auto upgrade your umbraco installations is done at your own risk.
 * 
 *  This code will run when your site is in Upgrade mode.
 *  This happens when the version in web.config or the database migrations don't 
 *  match what the umbraco code thinks should be the version.
 *      
 *  We intercept the process at this point, run the 'required' steps to update the site
 *  and then remove the redirect to /install that the core httpModule has put in place
 *  for the update. 
 *  
 *  Will only run when app setting is precent in web.config to turn it on.
 *  
 *  <add key="SilentUpgrade" value="true />
 */

using System;
using System.Configuration;
using System.Web;
using NPoco.Expressions;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations.Install;
using Umbraco.Core.Migrations.Upgrade;
using Umbraco.Core.Persistence;
using Umbraco.Web;
using Umbraco.Web.JavaScript;

namespace Our.Umbraco.SilentUpgrade
{
    /// <summary>
    ///  if the site is in Upgrade mode, register our component
    /// </summary>
    [RuntimeLevel(MaxLevel = RuntimeLevel.Upgrade, MinLevel = RuntimeLevel.Upgrade)]
    public class SilentUpgradeComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<SilentUpgradeComponent>();
        }
    }

    /// <summary>
    ///  SilentUpgradeComponent -  Runs the 'upgreade' steps.
    /// </summary>
    public class SilentUpgradeComponent : IComponent
    {
        private readonly IRuntimeState runtimeState;
        private readonly IGlobalSettings globalSettings;
        private readonly DatabaseBuilder databaseBuilder;
        private readonly IProfilingLogger logger;
        private readonly IUmbracoDatabaseFactory databaseFactory;

        private readonly bool SilentUpgradeOn = false;
        private bool Upgraded = false; 

        public SilentUpgradeComponent(IRuntimeState runtimeState,
            IGlobalSettings settings,
            IProfilingLogger logger,
            IUmbracoDatabaseFactory databaseFactory,
            DatabaseBuilder databaseBuilder)
        {
            this.runtimeState = runtimeState;
            this.globalSettings = settings;
            this.logger = logger;

            this.databaseFactory = databaseFactory;
            this.databaseBuilder = databaseBuilder;

            // Check to see if Silent Upgrade is on in web.config
            //  <add key="SilientUpgrade" value="true" />
            var silentUpgrade = ConfigurationManager.AppSettings["SilentUpgrade"].TryConvertTo<bool>();
            if (silentUpgrade.Success && silentUpgrade.Result)
            {
                logger.Info<SilentUpgradeComposer>("SilentUpgrade is On");
                SilentUpgradeOn = true;

                // we only need to worry about the request when we are on.
                UmbracoModule.EndRequest += UmbracoModule_EndRequest;
            }
            else
            {
                logger.Info<SilentUpgradeComposer>("SilentUpgrade is Off");
            }
        }

        public void Initialize()
        {
            // double check - is this on in config 
            // are we in upgrade mode (we should be - we only register in upgrade)
            if (!SilentUpgradeOn || runtimeState.Level != RuntimeLevel.Upgrade)
                return;

            // Do we need to lock? the upgrade so it only happens once? 
            logger.Debug<SilentUpgradeComponent>("Silently upgrading the Site");

            // The 'current' steps for upgrading Umbraco
            var initialVersion = globalSettings.ConfigurationStatus;
            var targetVersion = UmbracoVersion.SemanticVersion.ToSemanticString();

            try
            {


                SilentUpgrade.FireUpgradeStarting(initialVersion, targetVersion);

                //
                // If these steps change in the core then this will be wrong.
                //
                // We don't run the file permission step, we assume you have that sorted already.
                // and besides its internal and we can't get to the tests 
                //      FilePermissionHelper.RunFilePermissionTestSuite(out result);

                // Step: 'DatabaseInstallStep'
                var result = databaseBuilder.CreateSchemaAndData();
                if (!result.Success)
                {
                    // failed.
                    throw new Exception("Upgrade Failed - Create Schema");
                }

                // Step: 'DatabaseUpgradeStep'
                var plan = new UmbracoPlan();
                logger.Debug<SilentUpgradeComponent>("Running Migrations {initialState} to {finalState}", plan.InitialState, plan.FinalState);

                var upgrade = databaseBuilder.UpgradeSchemaAndData(plan);
                if (!upgrade.Success)
                    throw new Exception("Upgrade Failed - Upgrade Schema");

                // Step: 'SetUmbracoVersionStep'

                // Update the version number inside the web.config
                logger.Debug<SilentUpgradeComponent>("Updating version in the web.config {version}", targetVersion);
                // Doing this essentially restats the site.
                globalSettings.ConfigurationStatus = targetVersion;

                // put something in the log.
                logger.Info<SilentUpgradeComponent>("Silent Upgrade Completed {version}", targetVersion);

                SilentUpgrade.FireUpgradeComplete(true, initialVersion, targetVersion);

                Upgraded = true;
            }
            catch(Exception ex)
            {
                SilentUpgrade.FireUpgradeComplete(false, initialVersion, targetVersion, ex.Message);

                logger.Warn<SilentUpgradeComponent>(ex, "Silent Upgrade Failed");
                Upgraded = false; // if this is false, we should fall through to the 'standard' upgrade path.
            }
        }


        /// <summary>
        ///  At the end of a upgrade request - remove the /install redirect
        /// </summary>
        private void UmbracoModule_EndRequest(object sender, global::Umbraco.Web.Routing.UmbracoRequestEventArgs e)
        {
            if (Upgraded)
            {
                Upgraded = false;

                IncrementClientDependency(e.HttpContext);

                e.HttpContext.Response.Redirect(e.HttpContext.Request.Url.AbsoluteUri);
            }
        }

        /// <summary>
        ///  changes the client dependency number (and cleans the cache) - so we get no browser odds
        /// </summary>
        private void IncrementClientDependency(HttpContextBase httpContext)
        {
            // Update ClientDependency version
            var clientDependencyConfig = new ClientDependencyConfiguration(logger);
            var clientDependencyUpdated = clientDependencyConfig.UpdateVersionNumber(
                UmbracoVersion.SemanticVersion, DateTime.UtcNow, "yyyyMMdd");
            // Delete ClientDependency temp directories to make sure we get fresh caches
            var clientDependencyTempFilesDeleted = clientDependencyConfig.ClearTempFiles(httpContext);

        }


        public void Terminate()
        {
            // do nothing.
        }
    }
}
