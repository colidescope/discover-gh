﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using DiscoverGrasshopper.Properties;
using Grasshopper;
using Grasshopper.Kernel;
using Quobject.SocketIoClientDotNet.Client;
using Rhino.Geometry;

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
            pManager.AddTextParameter("out", "out", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("active", "active", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool connect = false;
            DA.GetData<bool>(0, ref connect);
            if (connect == true)
            {

                if (OnPingDocument().IsFilePathDefined == false)
                {
                    throw new Exception("Please save your Grasshopper file before connecting to Discover server.");
                }

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

                //addFileWatcher(DA, path);
                listenToServer();
                DA.SetData(1, false);
            }
            else
            {
                //System.Threading.Thread.Sleep(100);
                //active = DateTime.Now.Ticks;
                Print(DA, "Connection ID: " + connection_id);
                DA.SetData(1, true);
            }
        }


        private Socket socket = null;
        private bool expirationPending = false;

        private void listenToServer()
        {
            if (socket != null)
            {
                socket.Close();
            }
            socket = IO.Socket("http://localhost:5000/");
            socket.On("execute job", () =>
            {
                if (!expirationPending)
                {
                    ExpireSecure();
                }
            });
            socket.Connect();
        }

        private void ExpireSecure()
        {
            if (OnPingDocument().SolutionState != GH_ProcessStep.Process)
            {
                Instances.DocumentEditor.BeginInvoke((Action)delegate ()
                {
                    ExpireSolution(true);
                    expirationPending = false;
                });
            }
            else
            {
                expirationPending = true;
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

        void Print(IGH_DataAccess DA, string message)
        {
            DA.SetData(0, message);
        }

        void Print(IGH_DataAccess DA, string message, params object[] items)
        {
            DA.SetData(0, String.Format(message, items));
        }

        void fileChanged(string filePath)
        {
            //cause the component to fire a new solution.
            //Print("file changed");
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