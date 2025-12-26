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
