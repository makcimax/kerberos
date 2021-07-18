using System;
using System.Windows.Forms;

namespace TestClient
{
    internal static class Program
    {
        /// <summary>
        /// Основная точка входа в приложение. 
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault( false );
            Application.Run( new ClientForm() );
        }
    }
}