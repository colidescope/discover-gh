using System;
using System.Web.Script.Serialization;
using Discover.Properties;
using Grasshopper.Kernel;
using System.Windows.Forms;

namespace Discover
{
    public class Objective : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Output class.
        /// </summary>
        public Objective()
          : base("Objective", "Objective",
              "Assign an output as an objective and send it to Discover.",
              "Discover", "Outputs")
        {
            //Message = "Minimize";
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Name of objective.", GH_ParamAccess.item, "Objective");
            pManager.AddNumberParameter("Value", "V", "Objective value coming from model.", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Component status.", GH_ParamAccess.item);
        }

        public string Goal { get; set; } = "Minimize";

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);

            var item = Menu_AppendItem(menu, "Objective type");
            Menu_AppendItem(item.DropDown, "Minimize", Option_selected, true, Goal == "Minimize");
            Menu_AppendItem(item.DropDown, "Maximize", Option_selected, true, Goal == "Maximize");
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

            double val = 0;
            DA.GetData<double>(1, ref val);

            string output_def = "{\"id\": \"" + output_id + "\", \"name\": \"" + name + "\", \"type\": \"" + "Objective" + "\", \"goal\": \"" + Goal + "\", \"value\": " + val.ToString() + "}";
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

                Helpers.Print(DA, "[" + output_id + "] " + json.status);

                if (string.Equals(json.status, "run next"))
                {
                    Helpers.PingServer("http://127.0.0.1:5000/api/v1.0/next");
                }
            }

            Message = Goal;
        }

        private readonly string output_id = Helpers.GenerateID(8);

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // First add our own field.
            writer.SetString("Goal", Goal);
            // Then call the base class implementation.
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            // First read our own field.
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
                return Resources.objective;
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