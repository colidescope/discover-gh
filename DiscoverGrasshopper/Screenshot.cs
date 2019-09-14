using System;
using System.Collections.Generic;

using System.Text;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using System.Net;
using System.IO;
using System.Web.Script.Serialization;
using DiscoverGrasshopper.Properties;
using Grasshopper;
using System.Threading;
using WebSocketSharp;

namespace DiscoverGrasshopper
{
    public class Screenshot : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Screenshot class.
        /// </summary>
        public Screenshot()
          : base("Screenshot", "Screenshot",
              "Capture a screenshot for each design",
              "Discover", "Discover")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("conn", "conn", "", GH_ParamAccess.item);
            pManager.AddTextParameter("view_name", "view_name", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("out", "out", "", GH_ParamAccess.item);
            pManager.AddTextParameter("WSMessage", "WSMessage", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.SetData(1, webSocketMessage);
            string url = "";
            string post = "{\"id\": \"" + screenshot_id + "\"}";
            string result = "";
            bool conn = false;
            string view_name = "";
            DA.GetData<bool>(0, ref conn);
            DA.GetData<string>(1, ref view_name);

            var serializer = new JavaScriptSerializer();

            if (!conn)
            {
                url = "http://127.0.0.1:5000/api/v1.0/ss-register-id";
                result = PostToServer(url, post);

                var message = serializer.Deserialize<Message>(result);
                Print(DA, message.status);
                //addFileWatcher(DA, message.path);
                listenToServer();
            }
            else
            {
                RhinoView view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView;

                if (view_name == null)
                {
                    Print(DA, "Using active viewport " + view.MainViewport.Name);
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
                            Print(DA, "Viewport " + view_name + " found");
                            exists = true;
                            break;
                        }
                    }

                    if (exists)
                    {

                    }
                    else
                    {
                        Print(DA, "New viewport " + view_name + " created");
                        Rectangle rec = new Rectangle(0, 0, 800, 600);
                        RhinoView new_view = view_table.Add(view_name, Rhino.Display.DefinedViewportProjection.Perspective, rec, true);
                        new_view.MainViewport.WorldAxesVisible = false;
                        new_view.MainViewport.ConstructionGridVisible = false;
                        new_view.MainViewport.ConstructionAxesVisible = false;
                        new_view.MainViewport.DisplayMode = DisplayModeDescription.FindByName("shaded");

                        view = new_view;
                    }

                }

                if (active)
                {
                    Bitmap bitmap = view.CaptureToBitmap(false, false, false);

                    url = "http://127.0.0.1:5000/api/v1.0/ss-get-path";
                    result = PostToServer(url, post);

                    var message = serializer.Deserialize<Message>(result);

                    if (string.Equals(message.status, "success"))
                    {
                        string[] components = { message.path, "png" };
                        string path = string.Join(".", components);
                        //          Print(path);
                        Print(DA, "Screenshot captured to: " + path);
                        bitmap.Save(path);
                    }
                    else
                    {
                        Print(DA, message.status);
                    }

                    url = "http://127.0.0.1:5000/api/v1.0/ss-done";
                    result = PostToServer(url, post);

                    active = false;
                }
                else
                {
                    Print(DA, screenshot_id + " active.");
                }

            }
        }

        private WebSocket ws = null;
        private string webSocketMessage = null;

        private void listenToServer()
        {
            if (ws == null)
            {
                ws = new WebSocket("ws://localhost:5000/socket.io/?EIO=3&transport=websocket");
                ws.OnMessage += (sender, e) =>
                {
                    if (e.Data.StartsWith("42[\"execute post-job\""))
                    {
                        webSocketMessage = e.Data;
                        active = true;
                        ExpireSecure();
                    }
                };
                ws.OnError += (sender, e) =>
                {
                    ws = null;
                    webSocketMessage = e.Message;
                    listenToServer();
                };
                ws.OnClose += (sender, e) =>
                {
                    ws = null;
                    webSocketMessage = "Closed";
                    listenToServer();
                };
                ws.Connect();
            }
        }

        //private Socket socket = null;
        /*

        private void listenToServer()
        {
            if (socket == null)
            {
                socket = IO.Socket("http://localhost:5000/");
                socket.On(Socket.EVENT_CONNECT, () =>
                {
                    socket.On("execute post-job", () =>
                    {
                        active = true;
                        ExpireSecure();
                    });
                    ExpireSecure();
                });
                socket.Connect();
            }
        }
        */

        private void ExpireSecure()
        {
            if (OnPingDocument().SolutionState != GH_ProcessStep.Process)
            {
                Instances.DocumentEditor.BeginInvoke((Action)delegate ()
                {
                    ExpireSolution(true);
                });
            }
            else
            {
                Thread.Sleep(100);
                ExpireSecure();
            }
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

        string screenshot_id = RandomString(8);


        private static string PostToServer(string url, string post)
        {
            string result = "";

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
            return result;
        }

        public class Message
        {
            public string status { get; set; }
            public string path { get; set; }
        }


        void Print(IGH_DataAccess DA, string message)
        {
            DA.SetData(0, message);
        }

        void Print(IGH_DataAccess DA, string message, params object[] items)
        {
            DA.SetData(0, String.Format(message, items));
        }



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
                    Print(DA, "Connection already exists");
                    return;
                }
                else
                {
                    Print(DA, "file watcher disposed");
                    watcher.Dispose();
                    watchers.Clear();
                }
            }

            GH_FileWatcher new_watcher = GH_FileWatcher.CreateFileWatcher(path, GH_FileWatcherEvents.All, new GH_FileWatcher.FileChangedSimple(fileChanged));
            new_watcher.Buffer = new TimeSpan(1);
            //Print(new_watcher.Buffer.Ticks.ToString());
            watchers.Add(new_watcher);

            Print(DA, "New connection created");
        }

        void fileChanged(string filePath)
        {
            //cause the component to fire a new solution.
            //Print("file changed");
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
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.discover_screenshot;
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