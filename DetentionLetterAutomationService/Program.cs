using System.ServiceProcess;
using System.Threading;

namespace DetentionLetterAutomationService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {



#if DEBUG
            //If the mode is in debugging
            //create a new service instance
            DetentionLetterAutomationService myService = new DetentionLetterAutomationService();
            //call the start method - this will start the Timer.
            myService.Start();
            //Set the Thread to sleep
            Thread.Sleep(100000);
            //Call the Stop method-this will stop the Timer.
            //myService.Stop();

#else
            /*
             * ========================================================================
             * WINDOWS SERVICE MODE (Legacy)
             * ========================================================================
             * The following code runs the application as a Windows Service.
             *
             * MIGRATION TO DYNAMICS 365 ONLINE PLUGIN:
             * ========================================================================
             * This Windows Service has been converted to a Dynamics 365 Online Plugin.
             * The timer-based processing logic has been migrated to:
             *
             * Location: /Plugins/DetentionLetterPlugin.cs
             *
             * KEY CHANGES:
             * ----------------------------------------
             * 1. Windows Service Timer → Plugin Event-Based Execution
             *    - Timer (_aTimer_Elapsed) → Plugin Execute method
             *    - Runs on schedule via Power Automate/Workflow instead of Windows Timer
             *
             * 2. Service Lifecycle → Plugin Lifecycle
             *    - OnStart/OnStop → Plugin Registration/Unregistration
             *    - Continuous running → On-demand execution
             *
             * 3. Execution Model
             *    - Windows Service: Runs continuously on server
             *    - D365 Plugin: Runs in Azure cloud, triggered by events or schedule
             *
             * DEPLOYMENT OPTIONS:
             * ----------------------------------------
             * Option A: Scheduled Processing (Recommended - mimics original timer)
             *   - Create custom action in Dynamics 365
             *   - Register plugin on custom action
             *   - Schedule via Power Automate Cloud Flow (every X minutes)
             *
             * Option B: Event-Driven Processing
             *   - Register plugin on Sales Order Update/Create
             *   - Processes immediately when order conditions are met
             *
             * BENEFITS OF PLUGIN APPROACH:
             * ----------------------------------------
             * ✓ Cloud-native: Runs in Azure, no on-premise infrastructure
             * ✓ Scalable: Automatically scales with Dynamics 365
             * ✓ Integrated: Direct access to Dynamics 365 data and services
             * ✓ Maintainable: Managed through Dynamics 365 admin interface
             * ✓ Reliable: Built-in retry and error handling
             *
             * DEPLOYMENT STEPS:
             * ----------------------------------------
             * 1. Build the plugin project in Release mode
             * 2. Sign the assembly with a strong name key
             * 3. Register plugin assembly using Plugin Registration Tool
             * 4. Register plugin step on appropriate message/entity
             * 5. Create Power Automate flow for scheduled execution (if needed)
             * 6. Configure environment variables/secure config
             * 7. Test in Sandbox environment
             * 8. Deploy to Production
             *
             * For detailed instructions, see: /Plugins/README.md
             * ========================================================================
             */

            ////The following is the default code - You may fine tune
            ////the code to create one instance of the service on the top
            ////and use the instance variable in both debug and release mode
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new DetentionLetterAutomationService()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }
    }
}
