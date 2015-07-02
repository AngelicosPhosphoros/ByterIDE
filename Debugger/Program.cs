using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CompilerLib;
using System.IO;

namespace Debugger
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(params string[] args)
        {
            if (args.Length != 1 ||  !File.Exists(args[0]))
                return;
            ByterProject project = new ByterProject();
            project.LoadFiles(args[0]);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            OutputForm outf = new OutputForm();
            DebugForm debug = new DebugForm(project, project.MainMethod, outf);
            debug.IsMain = true;
            Application.Run(debug);
        }
    }
}
