using System;
using System.IO;

namespace AMI.Methods
{
    static class Log
    {
        public static void LogS(string logs)
        {
            Console.WriteLine(Environment.NewLine + DateTime.Now + ": " + logs);
            try
            {
                using (StreamWriter log = new StreamWriter("Log.log", true))
                    log.WriteLine(Environment.NewLine +  DateTime.Now + ": " + logs);
            }
            catch (Exception) { }
        }
        public static string LogS(Exception logs, string extra = null)
        {
            string error =  DateTime.Now + $": {extra} {logs.Message} +>> {Environment.NewLine}"
                + logs.StackTrace;
            LogS(error);
            return error;
        }
    }
}
