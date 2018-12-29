using System;
using System.IO;



namespace CommanderPortraitLoader {
    public class Logger
    {
        static string filePath = $"{CommanderPortraitLoader.ModDirectory}/CommanderPortraitLoader.log";
        public static void LogError(Exception ex)
        {
            if (CommanderPortraitLoader.DebugLevel >= 1)
            {
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    var prefix = "[CommanderPortraitLoader @ " + DateTime.Now.ToString() + "]";
                    writer.WriteLine("Message: " + ex.Message + "<br/>" + Environment.NewLine + "StackTrace: " + ex.StackTrace + "" + Environment.NewLine);
                    writer.WriteLine("----------------------------------------------------------------------------------------------------" + Environment.NewLine);
                }
            }
        }

        public static void LogLine(String line)
        {
            if (CommanderPortraitLoader.DebugLevel >= 2)
            {
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    var prefix = "[CommanderPortraitLoader @ " + DateTime.Now.ToString() + "]";
                    writer.WriteLine(prefix + line);
                }
            }
        }
    }
}