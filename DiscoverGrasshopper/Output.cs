using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using DiscoverGrasshopper.Properties;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DiscoverGrasshopper
{
    public class Output : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Output class.
        /// </summary>
        public Output()
          : base("Output", "Output",
              "Send output to Discover",
              "Discover", "IO")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("name", "name", "", GH_ParamAccess.item);
            pManager.AddTextParameter("type", "type", "", GH_ParamAccess.item);
            pManager.AddTextParameter("goal", "goal", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("val", "val", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("out", "out", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "";
            string type = "";
            string goal = "";
            double val = 0;
            DA.GetData<string>(0, ref name);
            DA.GetData<string>(1, ref type);
            DA.GetData<string>(2, ref goal);
            DA.GetData<double>(3, ref val);

            string output = "{\"id\": \"" + output_id + "\", \"name\": \"" + name + "\", \"type\": \"" + type + "\", \"goal\": \"" + goal + "\", \"value\": " + val.ToString() + "}";
            //    Print(output);
            //Send the output value to the server
            string url = "http://127.0.0.1:5000/api/v1.0/send_output";
            string result = "";

            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(output);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                using (WebResponse response = ex.Response)
                {
                    var httpResponse = (HttpWebResponse)response;

                    using (Stream data = response.GetResponseStream())
                    {
                        StreamReader sr = new StreamReader(data);
                        throw new Exception(sr.ReadToEnd());
                    }
                }
            }

            //    Print(result);

            var serializer = new JavaScriptSerializer();
            var message = serializer.Deserialize<Message>(result);

            string status = message.status;
            Print(DA, status);
            //If the server respond with 'run next' status then all outputs registered already sended his values so it's safe to call /next enpoint
            if (string.Equals(status, "run next"))
            {
                //All registered output components have finished. Ask for next generation to server.
                string new_url = "http://127.0.0.1:5000/api/v1.0/next";

                try
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(new_url);
                    httpWebRequest.Timeout = 1;
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";

                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(output);
                    }

                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        result = streamReader.ReadToEnd();
                    }
                }
                catch { }

            }

            //    input_vals = message.input_vals;
        }

        // <Custom additional code> 
        void Print(IGH_DataAccess DA, string message)
        {
            DA.SetData(0, message);
        }

        void Print(IGH_DataAccess DA, string message, params object[] items)
        {
            DA.SetData(0, String.Format(message, items));
        }

        private static Random random = new Random((int)DateTime.Now.Ticks);

        private static string RandomString(int length)
        {
            const string pool = "abcdefghijklmnopqrstuvwyxzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var builder = new StringBuilder();

            for (var i = 0; i < length; i++)
            {
                var c = pool[random.Next(0, pool.Length)];
                builder.Append(c);
            }

            return builder.ToString();
        }

        string output_id = RandomString(8);


        public class Message
        {
            //    public List<double> input_vals { get; set; }
            public string status { get; set; }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.discover_outputs;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2eb82019-b157-4437-a79f-37df0edc89dc"); }
        }
    }
}