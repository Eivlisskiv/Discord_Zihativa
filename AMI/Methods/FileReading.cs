using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;

namespace AMI.Methods
{
    static class FileReading
    {
        public static T LoadJSON<T>(string path)
        {
            while (true)
            {
                try
                {
                    StreamReader file = new StreamReader(path);
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;
                    string data = file.ReadToEnd();
                    file.Close();
                    T a = JsonConvert.DeserializeObject<T>(data);
                    Thread.CurrentThread.Priority = ThreadPriority.Normal;
                    return a;
                }
                catch (Exception e)
                {
                    if (e.Message.EndsWith("it is being used by another process."))
                        Thread.Yield();
                    else throw e;
                }
            }
        }
        public static void SaveJSON<T>(T item, string path)
        {
            bool done = false;
            while (!done)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(item);
                    StreamWriter file = new StreamWriter(path);
                    Thread.CurrentThread.Priority = ThreadPriority.Highest;
                    file.Write(json);
                    file.Close();
                    done = true;
                    Thread.CurrentThread.Priority = ThreadPriority.Normal;
                }
                catch (Exception) { Thread.Sleep(1000); }
            }
        }
    }
}
