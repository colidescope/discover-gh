using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using Discover.Properties;
using Grasshopper.Kernel;

namespace Discover
{
    public class Categorical : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Inputs class.
        /// </summary>
        public Categorical()
          : base("Categorical", "Categorical",
              "Create a set of categorical input parameters",
              "Discover", "Inputs")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Number", "N", "Number of options to choose from.", GH_ParamAccess.item, 4);
            pManager.AddIntegerParameter("Count", "C", "Number of parameters to generate.", GH_ParamAccess.item, 10);
            pManager.AddTextParameter("Message", "M", "Server message.", GH_ParamAccess.item, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Component status.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Values", "V", "Parameter values.", GH_ParamAccess.list);
        }
        protected override void ExpireDownStreamObjects()
        {
            if (UpdateOutput)
                base.ExpireDownStreamObjects();
        }
        private bool UpdateOutput { get; set; }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int num = 0;
            DA.GetData<int>(0, ref num);

            int count = 0;
            DA.GetData<int>(1, ref count);

            string server_msg = "";
            DA.GetData<string>(2, ref server_msg);

            if (Equals(server_msg, "100") || Equals(server_msg, "200"))
            {
                string input_def = "{\"id\": \"" + input_id + "\", \"name\": \"" + "Input_Categorical" + "\", \"type\": \"" + "Categorical" + "\", \"opt\": " + num.ToString() + ", \"num\": " + count.ToString() + "}";
                string url = "http://127.0.0.1:5000/api/v1.0/input-ack";

                Tuple<bool, string> result = Helpers.PostToServer(url, input_def);
                string message = result.Item2;

                if (!result.Item1)
                {
                    Helpers.Print(DA, "Error:" + message);
                    throw new Exception(message);
                }
                else
                {
                    var serializer = new JavaScriptSerializer();
                    var json = serializer.Deserialize<InputMSG>(message);

                    Helpers.Print(DA, json.status);
                    DA.SetDataList(1, json.input_vals);
                }
                UpdateOutput = true;
            }
            else
            {
                if (Equals(server_msg, "True"))
                {
                    UpdateOutput = true;
                }
                else
                {
                    if (Equals(server_msg, "False"))
                    {
                        UpdateOutput = false;
                    }
                    else
                    {
                        UpdateOutput = true;
                    }

                    Helpers.Print(DA, input_id + ": Not connected to Discover server.");
                    DA.SetDataList(1, generate_random_categorical(num, count));
                }
            }

            
        }

        private static List<int> generate_random_categorical(int num, double count)
        {
            List<int> inputs = new List<int>();

            for (var i = 0; i < count; i++)
            {
                inputs.Add(Helpers.RandomInt(num));
            }

            return inputs;
        }

        private readonly string input_id = Helpers.GenerateID(8);

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.categorical;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2b5fac53-edef-4854-b918-04d6d6846b6c"); }
        }
    }
}