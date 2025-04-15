using System;
using System.IO;


namespace NinjaTrader.NinjaScript.Utilities
{
    public static class TRUtilities
    {

        //private void WriteToFile(string text)
        //{
        //    sw = File.AppendText(path);  // Open the path for writing
        //    sw.WriteLine(text); // Append a new line to the file
        //    sw.Close(); // Close the file to allow future calls to access the file again.
        //}




        /// <summary>
        /// Writes a line of text to the specified file.
        /// </summary>
        /// <param name="filePath">The path of the file to write to.</param>
        /// <param name="text">The text to write.</param>
        public static void WriteToFile(string filePath, string text)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.WriteLine(text);
            }
        }

        /// <summary>
        /// Saves a log entry to the specified file if verbose logging is enabled.
        /// </summary>
        /// <param name="filePath">The path of the file to write to.</param>
        /// <param name="IsWriteToFile">Indicates whether verbose logging is enabled.</param>
        /// <param name="logEntry">The log entry to save.</param>
        public static void SaveToFile(string filePath, bool IsWriteToFile, string logEntry)
        {
            if (IsWriteToFile)
            {
                WriteToFile(filePath, logEntry);
            }
        }


       
    }
}
