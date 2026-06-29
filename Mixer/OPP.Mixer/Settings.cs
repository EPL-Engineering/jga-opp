using KLib.IO;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPP.Mixer
{
    public class Settings
    {
        public Rectangle lastPosition = new Rectangle();

        private static string FilePath => Path.Combine(@"C:\EPL", "Mixer.xml");

        private static Settings _instance = null;
        private static Settings instance
        {
            get
            {
                if (_instance == null)
                {
                    if (File.Exists(FilePath))
                    {
                        _instance = Files.XmlDeserialize<Settings>(FilePath);
                    }
                    else
                    {
                        _instance = new Settings();
                    }
                }
                return _instance;
            }
        }

        public static Rectangle LastPosition
        {
            get { return instance.lastPosition; }
            set { instance.lastPosition = value; Save(); }
        }

        private static void Save()
        {
            Files.XmlSerialize(_instance, FilePath);
        }

    }
}
