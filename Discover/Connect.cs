using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discover.Properties;
using Grasshopper.Kernel;

namespace Discover
{
    public class Connect : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Connect class.
        /// </summary>
        public Connect()
          : base("Discover", "Discover",
              "Connect to the Discover server.",
              "Discover", "Discover")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Connect", "C", "Connect to Discover server.", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Component status.", GH_ParamAccess.item);
            pManager.AddTextParameter("Message", "M", "Server message.", GH_ParamAccess.item);
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
            Message = "v.19.12";

            //DA.DisableGapLogic();

            if (OnPingDocument().IsFilePathDefined == false)
            {
                throw new Exception("Please save your Grasshopper file before connecting to Discover server.");
            }

            bool connect = false;
            DA.GetData<bool>(0, ref connect);

            if (connect == true)
            {
                //for next false
                UpdateOutput = false;

                string local_file = OnPingDocument().FilePath;

                string[] file_path = local_file.Split('\\');
                string path_for_json = string.Join("\\\\", file_path);
                string file_name = Path.GetFileNameWithoutExtension(local_file);

                string[] new_file_dir = file_path.Take(file_path.Length - 1).ToArray();
                string[] folders = { "discover", "temp" };
                string new_file_path = string.Join("\\\\", new_file_dir.Concat(folders));

                if (!Directory.Exists(new_file_path))
                {
                    DirectoryInfo di = Directory.CreateDirectory(new_file_path);
                    //Print(DA, "New directory created at {0}.", Directory.GetCreationTime(new_file_path));
                }
;
                string connection_file_name = file_name + "." + connection_id;
                string path = new_file_path + "\\" + connection_file_name;

                string json = "{\"path\": \"" + path_for_json + "\", \"id\": \"" + connection_id + "\"}";
                string url = "http://127.0.0.1:5000/api/v1.0/connect";

                Tuple<bool,string> result = Helpers.PostToServer(url, json);
                string message = result.Item2;

                if (!result.Item1)
                {
                    Helpers.Print(DA, "Error:" + message);
                    DA.SetData(1, "Error.");
                    //throw new Exception(message);
                }
                else
                {
                    Helpers.Print(DA, message);
                    DA.SetData(1, "100");
                    addFileWatcher(DA, path);
                }

            }
            else
            {

                UpdateOutput = true;
                DA.SetData(1, "200");

                //string json = "{\"id\": \"" + connection_id + "\"}";
                //string url = "http://127.0.0.1:5000/api/v1.0/check-connection";

                //Tuple<bool, string> result = Helpers.PostToServer(url, json);
                //string message = result.Item2;

                //if (!result.Item1)
                //{
                //    Helpers.Print(DA, "Error:" + message);
                //    DA.SetData(1, false);
                //    //throw new Exception(message);
                //}
                //else
                //{
                //    if (string.Equals(message, "true\n"))
                //    {
                //        Helpers.Print(DA, "Connected to Discover server with ID: " + connection_id);
                //        DA.SetData(1, true);
                //    }
                //    else
                //    {
                //        Helpers.Print(DA, connection_id + ": Not connected to Discover server.");
                //        DA.SetData(1, false);
                //    }
                //}

            }
        }

        readonly string connection_id = Helpers.GenerateID(8);

        //set up a list to contain all your filewatchers
        private List<GH_FileWatcher> watchers = new List<GH_FileWatcher>();

        //method to add a new watcher. Checks if the file has already been added to avoid duplicates
        private void addFileWatcher(IGH_DataAccess DA, string path)
        {
            if (watchers.Count > 0)
            {
                GH_FileWatcher watcher = watchers[0];
                if (watcher.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    Helpers.Print(DA, "Connection already exists");
                    return;
                }
                else
                {
                    Helpers.Print(DA, "File watcher disposed");
                    watcher.Dispose();
                    watchers.Clear();
                }
            }

            GH_FileWatcher new_watcher = GH_FileWatcher.CreateFileWatcher(path, GH_FileWatcherEvents.All, new GH_FileWatcher.FileChangedSimple(fileChanged));
            new_watcher.Buffer = new TimeSpan(1);
            //Print(new_watcher.Buffer.Ticks.ToString());
            watchers.Add(new_watcher);

            Helpers.Print(DA, "New connection created");
        }

        void fileChanged(string filePath)
        {
            //cause the component to fire a new solution.
            ExpireSolution(true);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.discover;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c72fbecf-0dae-48ae-a14a-82d5956c04e7"); }
        }
    }
}