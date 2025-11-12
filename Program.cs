using System;
using System.Windows.Forms;

namespace MunicipalServicesApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Seed demo service requests for the Service Request Status page
            SampleIssueSeeder.Seed();

            Application.Run(new MainForm());
        }
    }
}
