using System;
using System.Windows.Forms;
namespace Kthc {
 static class Program {
  [STAThread] static void Main() {
   Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
   try { Database.Initialize(); Application.Run(new MainForm()); }
   catch(Exception ex) { MessageBox.Show("Không thể mở ứng dụng. Cần cài SQL Server Express LocalDB và .NET Framework 4.8.\n\n"+ex.Message,"KTHC",MessageBoxButtons.OK,MessageBoxIcon.Error); }
  }
 }
}
