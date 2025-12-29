using System;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace DetentionLetterAutomationService.Plugins
{
    /// <summary>
    /// Base class for all Dynamics 365 plugins
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        protected string PluginClassName { get; }

        /// <summary>
        /// Constructor to set the plugin class name
        /// </summary>
        protected PluginBase(Type pluginType)
        {
            PluginClassName = pluginType.ToString();
        }

        /// <summary>
        /// Main execution method for the plugin
        /// </summary>
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new InvalidPluginExecutionException("serviceProvider");
            }

            // Obtain the tracing service
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            // Obtain the organization service reference
            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                tracingService.Trace($"Entered {PluginClassName}.Execute()");
                tracingService.Trace($"Correlation Id: {context.CorrelationId}");
                tracingService.Trace($"Initiating User: {context.InitiatingUserId}");
                tracingService.Trace($"Message: {context.MessageName}");
                tracingService.Trace($"Depth: {context.Depth}");

                // Execute the plugin logic
                ExecutePlugin(tracingService, context, service, serviceFactory);

                tracingService.Trace($"Exiting {PluginClassName}.Execute()");
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                tracingService.Trace($"Exception: {ex.ToString()}");
                throw new InvalidPluginExecutionException($"An error occurred in {PluginClassName}.", ex);
            }
            catch (Exception ex)
            {
                tracingService.Trace($"Exception: {ex.ToString()}");
                Logger.Error($"{DateTime.Today.ToLongDateString()} Exception in {PluginClassName} - {ex.StackTrace}");
                Logger.Error($"Message ------ {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Abstract method that must be implemented by derived plugin classes
        /// </summary>
        protected abstract void ExecutePlugin(
            ITracingService tracingService,
            IPluginExecutionContext context,
            IOrganizationService service,
            IOrganizationServiceFactory serviceFactory);
    }
}
