using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace Discover
{
    public static class Helpers
    {

        public static Tuple<bool, string> PostToServer(string url, string post)
        {
            bool success = true;
            string result = string.Empty;

            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(post);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                success = false;
                result = ex.Message;
            }
            return Tuple.Create(success, result);
        }
                     
        public static void PingServer(string url)
        {
            string result = string.Empty;

            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.Timeout = 1;
                //httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "GET";

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (Stream stream = httpResponse.GetResponseStream())
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
            }
            catch { }
        }

        private static readonly Random random = new Random();
        private static readonly object syncLock = new object();

        public static string GenerateID(int length)
        {
            //Random random = new Random((int)DateTime.Now.Ticks);
            Random random = new Random();

            const string pool = "abcdefghijklmnopqrstuvwyxzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            string ID = "";

            for (var i = 0; i < length; i++)
            {
                var c = pool[RandomInt(pool.Length)];
                ID += c;
            }

            return ID;
        }

        public static int RandomInt(int num)
        {
            //Random random = new Random((int)DateTime.Now.Ticks);
            //Random random = new Random();

            lock (syncLock) // synchronize
            {
                return random.Next(0, num);
            }
        }

        public static double RandomDouble(double min, double max)
        {
            //Random random = new Random((int)DateTime.Now.Ticks);
            //Random random = new Random();

            double range = max - min;

            lock (syncLock) // synchronize
            {
                return random.NextDouble() * range + min;
            }
        }

        public static List<int> MakeRange(int count)
        {
            List<int> inputs = new List<int>();

            for (var i = 0; i < count; i++)
            {
                inputs.Add(i);
            }

            return inputs;
        }

        public static List<E> ShuffleList<E>(List<E> inputList)
        {
            List<E> randomList = new List<E>();

            //Random r = new Random();
            int randomIndex = 0;
            while (inputList.Count > 0)
            {
                randomIndex = RandomInt(inputList.Count); //Choose a random object in the list
                randomList.Add(inputList[randomIndex]); //add it to the new, random list
                inputList.RemoveAt(randomIndex); //remove to avoid duplicates
            }

            return randomList; //return the new random list
        }

        public static void Print(IGH_DataAccess DA, string message)
        {
            DA.SetData(0, message);
        }
    }
    public class InputMSG
    {
        public string input_id { get; set; }
        public List<double> input_vals { get; set; }
        public string status { get; set; }
    }
    public class OutputMSG
    {
        public string output_id { get; set; }
        //    public List<double> input_vals { get; set; }
        public string status { get; set; }
    }
    public class ScreenshotMSG
    {
        public string status { get; set; }
        public string path { get; set; }
    }

}
