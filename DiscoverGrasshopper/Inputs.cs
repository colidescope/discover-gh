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
    public class Inputs : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Inputs class.
        /// </summary>
        public Inputs()
          : base("Inputs", "Inputs",
              "Get inputs from Discover",
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
            pManager.AddIntegerParameter("min", "min", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("max", "max", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("num", "num", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("conn", "conn", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("out", "out", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("input_vals", "input_vals", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "";
            string type = "";
            int min = 0;
            int max = 0;
            int num = 0;
            DA.GetData<string>(0, ref name);
            DA.GetData<string>(1, ref type);
            DA.GetData<int>(2, ref min);
            DA.GetData<int>(3, ref max);
            DA.GetData<int>(4, ref num);
            string input_def = "{\"id\": \"" + input_id + "\", \"name\": \"" + name + "\", \"type\": \"" + type + "\", \"min\": " + min.ToString() + ", \"max\": " + max.ToString() + ", \"num\": " + num.ToString() + "}";

            //Restrieve the input values from the server
            string url = "http://127.0.0.1:5000/api/v1.0/input_ack";
            string result = "";

            try
            {

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(input_def);
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

            Print(DA, message.status);
            DA.SetDataList(1, message.input_vals);
        }

        void Print(IGH_DataAccess DA, string message)
        {
            DA.SetData(0, message);
        }

        void Print(IGH_DataAccess DA, string message, params object[] items)
        {
            DA.SetData(0, String.Format(message, items));
        }

        // <Custom additional code> 
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

        string input_id = RandomString(8);


        public class Message
        {
            public List<double> input_vals { get; set; }
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
                return Resources.discover_inputs;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d2024328-33de-4b45-a454-d1d08bd27c7d"); }
        }
    }
}