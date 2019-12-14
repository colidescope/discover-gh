using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Discover.Properties;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Discover
{
    public class Continuous : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Inputs class.
        /// </summary>
        public Continuous()
          : base("Continuous", "Continuous",
              "Create a set of continuous input parameters",
              "Discover", "Inputs")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of input.", GH_ParamAccess.item, "Input");
            pManager.AddIntervalParameter("Range", "R", "Range of parameter values.", GH_ParamAccess.item, new Interval(0.0, 1.0));
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

        public string Input_id { get; set; } = Helpers.GenerateID(8);

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
            string name = "";
            DA.GetData<string>(0, ref name);

            Interval range = new Interval(0.0, 1.0);
            DA.GetData<Interval>(1, ref range);

            double min = range.Min;
            double max = range.Max;

            int count = 0;
            DA.GetData<int>(2, ref count);

            string server_msg = "";
            DA.GetData<string>(3, ref server_msg);

            if (Equals(server_msg, "100") || Equals(server_msg, "200"))
            {

                string url;
                if (Equals(server_msg, "100"))
                {
                    url = "http://127.0.0.1:5000/api/v1.0/register-input";
                }
                else
                {
                    url = "http://127.0.0.1:5000/api/v1.0/get-input";
                }

                string input_def = "{\"id\": \"" + Input_id + "\", \"name\": \"" + name + "\", \"type\": \"" + "Continuous" + "\", \"min\": " + min.ToString() + ", \"max\": " + max.ToString() + ", \"num\": " + count.ToString() + "}";
                
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

                    Input_id = json.input_id;

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
                        
                    Helpers.Print(DA, Input_id + ": Not connected to Discover server.");
                    DA.SetDataList(1, generate_random_continuous(min, max, count));
                }

            }
        }

        private static List<double> generate_random_continuous(double min, double max, double count)
        {
            List<double> inputs = new List<double>();

            for (var i = 0; i < count; i++)
            {
                inputs.Add(Helpers.RandomDouble(min, max));
            }

            return inputs;
        }

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // First add our own field.
            writer.SetString("Input_id", Input_id);
            // Then call the base class implementation.
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            // First read our own field.
            Input_id = reader.GetString("Input_id");
            // Then call the base class implementation.
            return base.Read(reader);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.continuous;
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