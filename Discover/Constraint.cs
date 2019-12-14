using System;
using System.Web.Script.Serialization;
using Discover.Properties;
using Grasshopper.Kernel;
using System.Windows.Forms;

namespace Discover
{
    public class Constraint : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Output class.
        /// </summary>
        public Constraint()
          : base("Constraint", "Constraint",
              "Assign an output as a constraint and send it to Discover.",
              "Discover", "Outputs")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of constraint.", GH_ParamAccess.item, "Objective");
            pManager.AddNumberParameter("Target", "T", "Target of constraint.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Value", "V", "Constraint value coming from model.", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Component status.", GH_ParamAccess.item);
        }

        public string Output_id { get; set; } = Helpers.GenerateID(8);
        public string Goal { get; set; } = "Less than";

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);

            var item = Menu_AppendItem(menu, "Constraint type");
            Menu_AppendItem(item.DropDown, "Less than", Option_selected, true, Goal == "Less than");
            Menu_AppendItem(item.DropDown, "Greater than", Option_selected, true, Goal == "Greater than");
            Menu_AppendItem(item.DropDown, "Equals", Option_selected, true, Goal == "Equals");
        }

        private void Option_selected(Object sender, EventArgs e)
        {
            RecordUndoEvent("Set goal");
            var item = sender as ToolStripMenuItem;
            Goal = item.Text;
            ExpireSolution(true);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string name = "";
            DA.GetData<string>(0, ref name);

            double target = 0;
            DA.GetData<double>(1, ref target);

            double val = 0;
            DA.GetData<double>(2, ref val);

            string output_def = "{\"id\": \"" + Output_id + "\", \"name\": \"" + name + "\", \"type\": \"" + "Constraint" + "\", \"goal\": \"" + Goal + "\", \"target\": \"" + target + "\", \"value\": " + val.ToString() + "}";
            string url = "http://127.0.0.1:5000/api/v1.0/send-output";

            Tuple<bool, string> result = Helpers.PostToServer(url, output_def);
            string message = result.Item2;

            if (!result.Item1)
            {
                Helpers.Print(DA, "Error:" + message);
                throw new Exception(message);
            }
            else
            {
                var serializer = new JavaScriptSerializer();
                var json = serializer.Deserialize<OutputMSG>(message);

                //Output_id = json.output_id;

                Helpers.Print(DA, "[" + Output_id + "] " + json.status);

                if (string.Equals(json.status, "run next"))
                {
                    Helpers.PingServer("http://127.0.0.1:5000/api/v1.0/next");
                }
            }

            Message = Goal;
        }

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // First add our own field.
            writer.SetString("Output_id", Output_id);
            writer.SetString("Goal", Goal);
            // Then call the base class implementation.
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            // First read our own field.
            Output_id = reader.GetString("Output_id");
            Goal = reader.GetString("Goal");
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
                return Resources.constraint;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2a50b2d6-ede5-4ed1-9f03-284b2d8f2c0e"); }
        }
    }
}