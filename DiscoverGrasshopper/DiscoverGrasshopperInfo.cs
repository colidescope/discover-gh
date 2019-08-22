using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace DiscoverGrasshopper
{
    public class DiscoverGrasshopperInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Discover Grasshopper";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("57c5ce40-ba75-453b-93a9-8a17ed5adef1");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
