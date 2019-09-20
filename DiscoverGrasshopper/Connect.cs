using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using DiscoverGrasshopper.Properties;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using WebSocketSharp;

namespace DiscoverGrasshopper
{
    public class Connect : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Connect class.
        /// </summary>
        public Connect()
          : base("Connect", "Connect",
              "Connect to the Discover server",
              "Discover", "Discover")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("connect", "connect", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("out", "out", "", GH_ParamAccess.item); //Out parameter to be used by Print method
            pManager.AddBooleanParameter("active", "active", "", GH_ParamAccess.item); //Determine if child component should register within the server or do the actuall work
            pManager.AddTextParameter("WSMessage", "WSMessage", "", GH_ParamAccess.item); //Show the received WebSocket event (Only for testing purpose)
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.SetData(2, webSocketMessage); //Show the lattest WebSocket message saved
            bool connect = false;
            DA.GetData<bool>(0, ref connect); //Get input value
            if (connect)
            {
                //The Grasshopper .gh file shuold be saved before using Discover
                if (!OnPingDocument().IsFilePathDefined)
                {
                    throw new Exception("Please save your Grasshopper file before connecting to Discover server.");
                }

                //Logic to create a folder to save all temps for this .gh

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
                    Print(DA, "New directory created at {0}.", Directory.GetCreationTime(new_file_path));
                }


                string[] components = { file_name, connection_id };
                string connection_file_name = string.Join(".", components);

                string[] components1 = { new_file_path, connection_file_name };
                string path = string.Join("\\", components1);

                //string time_now = DateTime.Now.ToString();
                //System.IO.File.WriteAllText(path, time_now);

                //Request to the /connect endpoint on the server
                string json = "{\"path\": \"" + path_for_json + "\", \"id\": \"" + connection_id + "\"}";
                string url = "http://127.0.0.1:5000/api/v1.0/connect";
                string result = "";

                try
                {

                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                    httpWebRequest.ContentType = "application/json";
                    httpWebRequest.Method = "POST";

                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(json);
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

                Print(DA, result);

                //path_out = path;
                listenToServer(); //After connecting create the Client WebSocket and start listening
                DA.SetData(1, false); //Tell the child components we ain't active so they should register
            }
            else
            {
                //System.Threading.Thread.Sleep(100);
                //active = DateTime.Now.Ticks;
                Print(DA, "Connection ID: " + connection_id);
                DA.SetData(1, true); //Tell the child components we are active so they should do the actuall work
            }
        }

        private WebSocket ws = null;
        private string webSocketMessage = null;

        private void listenToServer()
        {
            if (ws == null) //Ensure we only create the client WebSocket once
            {
                ws = new WebSocket("ws://localhost:5000/socket.io/?EIO=3&transport=websocket");
                //Add callback when an event reach the client
                ws.OnMessage += (sender, e) =>
                {
                    //The expected event text should start with 42[{event name}...
                    if (e.Data.StartsWith("42[\"execute job\"")) //execute job means we need to expire and all child recompute their values
                    {
                        webSocketMessage = e.Data;
                        ExpireSecure(); //We expire this component and all his dependant by consecuence
                    }
                };
                //On error try to reconnect
                ws.OnError += (sender, e) =>
                {
                    ws = null;
                    listenToServer();
                };
                //On close try to reconnect
                ws.OnClose += (sender, e) =>
                {
                    ws = null;
                    listenToServer();
                };
                //After setting all listeners we try to connect to server SocketIO endpoint.
                ws.Connect();
            }
        }
        ///<summary>
        ///Alternative to ExpireSolution(true) that wait until the solution ain't proccessing any component 
        ///</summary>
        private void ExpireSecure()
        {
            if (OnPingDocument().SolutionState != GH_ProcessStep.Process)
            {
                //This ensure that ExpireSolution it's called on the Rhino Thread and not on the WebSocket thread
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

        private static readonly Random random = new Random((int)DateTime.Now.Ticks);

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

        string connection_id = RandomString(8);

        void Print(IGH_DataAccess DA, string message)
        {
            DA.SetData(0, message);
        }

        void Print(IGH_DataAccess DA, string message, params object[] items)
        {
            DA.SetData(0, String.Format(message, items));
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
                return Resources.discover_connect;
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