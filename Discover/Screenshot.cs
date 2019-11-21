using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.DocObjects.Tables;
using System.Web.Script.Serialization;
using Discover.Properties;


namespace Discover
{
    public class Screenshot : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Screenshot class.
        /// </summary>
        public Screenshot()
          : base("Capture screenshot", "Screenshot",
              "Capture a screenshot for each design",
              "Discover", "Discover")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("View name", "N", "Name of viewport to capture.", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Message", "M", "Server message.", GH_ParamAccess.item, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Component status.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string view_name = "";
            DA.GetData<string>(0, ref view_name);

            string server_msg = "";
            DA.GetData<string>(1, ref server_msg);

            string post = "{\"id\": \"" + screenshot_id + "\"}";

            //if (!active)
            if (Equals(server_msg, "100"))
            {
                string url = "http://127.0.0.1:5000/api/v1.0/ss-register-id";

                Tuple<bool, string> result = Helpers.PostToServer(url, post);
                string message = result.Item2;

                if (!result.Item1)
                {
                    Helpers.Print(DA, "Error:" + message);
                    throw new Exception(message);
                }
                else
                {
                    var serializer = new JavaScriptSerializer();
                    var json = serializer.Deserialize<ScreenshotMSG>(message);

                    Helpers.Print(DA, json.status);
                    addFileWatcher(DA, json.path);
                }

            }

            if (active)
            {
                RhinoView view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView;

                if (view_name == "")
                {
                    Helpers.Print(DA, "Using active viewport " + view.MainViewport.Name);
                }
                else
                {

                    ViewTable view_table = Rhino.RhinoDoc.ActiveDoc.Views;
                    RhinoView[] views = view_table.GetViewList(true, false);

                    bool exists = false;

                    for (int i = 0; i < views.Length; i++)
                    {
                        view = views[i];
                        string vp_name = view.MainViewport.Name;

                        if (string.Equals(vp_name, view_name))
                        {
                            Helpers.Print(DA, "Viewport " + view_name + " found");
                            exists = true;
                            break;
                        }
                    }

                    if (exists)
                    {
                    }
                    else
                    {
                        Helpers.Print(DA, "New viewport " + view_name + " created");
                        Rectangle rec = new Rectangle(0, 0, 800, 600);
                        RhinoView new_view = view_table.Add(view_name, Rhino.Display.DefinedViewportProjection.Perspective, rec, true);
                        new_view.MainViewport.WorldAxesVisible = false;
                        new_view.MainViewport.ConstructionGridVisible = false;
                        new_view.MainViewport.ConstructionAxesVisible = false;
                        new_view.MainViewport.DisplayMode = DisplayModeDescription.FindByName("shaded");

                        view = new_view;
                    }


                    Bitmap bitmap = view.CaptureToBitmap(false, false, false);

                    string url = "http://127.0.0.1:5000/api/v1.0/ss-get-path";

                    Tuple<bool, string> result = Helpers.PostToServer(url, post);
                    string message = result.Item2;

                    if (!result.Item1)
                    {
                        Helpers.Print(DA, "Error:" + message);
                        throw new Exception(message);
                    }
                    else
                    {
                        var serializer = new JavaScriptSerializer();
                        var json = serializer.Deserialize<ScreenshotMSG>(message);

                        if (string.Equals(json.status, "success"))
                        {
                            string[] components = { json.path, "png" };
                            string path = string.Join(".", components);

                            Helpers.Print(DA, "Screenshot captured to: " + path);
                            bitmap.Save(path);
                        }
                        else
                        {
                            Helpers.Print(DA, json.status);
                        }
                    }

                    Helpers.PingServer("http://127.0.0.1:5000/api/v1.0/ss-done");

                    
                }
                //else
                //{
                //    Helpers.Print(DA, screenshot_id + " active.");
                //}

             active = false;
            }
        }

        readonly string screenshot_id = Helpers.GenerateID(8);

        bool active = false;

        //set up a list to contain all your filewatchers
        List<GH_FileWatcher> watchers = new List<GH_FileWatcher>();

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
                    Helpers.Print(DA, "file watcher disposed");
                    watcher.Dispose();
                    watchers.Clear();
                }
            }

            GH_FileWatcher new_watcher = GH_FileWatcher.CreateFileWatcher(path, GH_FileWatcherEvents.All, new GH_FileWatcher.FileChangedSimple(fileChanged));
            new_watcher.Buffer = new TimeSpan(1);
            watchers.Add(new_watcher);

            Helpers.Print(DA, "New connection created");
        }

        void fileChanged(string filePath)
        {
            active = true;
            ExpireSolution(true);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.screenshot;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2af526b9-57be-4e49-a834-b7ee5bd76c38"); }
        }
    }
}