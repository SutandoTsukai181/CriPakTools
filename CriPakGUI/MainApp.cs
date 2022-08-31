using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CriPakGUI
{
    public class MainApp
    {
        public const string BuildTime = "BuildData 20220901";
        private MainApp()
        {

        }
        public static MainApp Instance
        {
            get
            {
                if (_instance == null) _instance = new MainApp();
                return _instance;
            }
        }

        private static MainApp _instance;

        public MyPackage currentPackage = new MyPackage();

        public void Initialize()
        {

        }

    }
}
