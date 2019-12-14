using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Discover.Properties;
using Grasshopper.Kernel;

namespace Discover
{
    public class Sequence : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Inputs class.
        /// </summary>
        public Sequence()
          : base("Sequence", "Sequence",
              "Create a sequence of input parameters",
              "Discover", "Inputs")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of input.", GH_ParamAccess.item, "Input");
            pManager.AddIntegerParameter("Number", "I", "Number of items in the sequence.", GH_ParamAccess.item, 4);
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

            int count = 0;
            DA.GetData<int>(1, ref count);

            string server_msg = "";
            DA.GetData<string>(2, ref server_msg);


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

                string input_def = "{\"id\": \"" + Input_id + "\", \"name\": \"" + "Input_Sequence" + "\", \"type\": \"" + "Sequence" + "\", \"num\": " + count.ToString() + "}";

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
                    DA.SetDataList(1, generate_random_seq(count));
                }

            }
        }        

        private static List<int> generate_random_seq(int count)
        {
            List<int> inputs = Helpers.MakeRange(count);
            return Helpers.ShuffleList(inputs);
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
                return Resources.sequence;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5671f9c4-037d-4137-94fc-6384365959b1"); }
        }
    }
}