using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Catcon {
    static class Program {
        [STAThread]static int Main(string[] args) {
            try{Native.SetDpiContext(new IntPtr(-4));}catch(EntryPointNotFoundException){Native.SetProcessDPIAware();}
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            string root=AppDomain.CurrentDomain.BaseDirectory;
            try {
                if(args.Length>0&&args[0]=="--self-test")return Validation.Run(root,args.Length>1?Path.GetFullPath(args[1]):Path.Combine(root,"test-results.json"));
                if(args.Length>0&&args[0]=="--export-art"){ArtExport.Run(root,args.Length>1?Path.GetFullPath(args[1]):root);return 0;}
                if(args.Length>0&&args[0]=="--export-garden"){GardenExport.Run(root,args.Length>1?Path.GetFullPath(args[1]):root);return 0;}
                if(args.Length>0&&args[0]=="--smoke-test")return Validation.Smoke(root,args.Length>1?Path.GetFullPath(args[1]):Path.Combine(root,"smoke-results.json"));
                bool created;
                using(var mutex=new Mutex(true,"Local\\Catcon.Desktop.1",out created)){
                    if(!created){MessageBox.Show("蜜柑已经在桌面上了。可以从系统托盘打开小窝。","Catcon");return 0;}
                    Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                    Application.ThreadException+=delegate(object sender,ThreadExceptionEventArgs e){Storage.Log(e.Exception);MessageBox.Show(e.Exception.Message,"Catcon 运行错误");};
                    var preferences=Storage.Load();PetWindow pet;
                    try{pet=new PetWindow(root,preferences);}catch(Exception ex){
                        if(preferences.SkinId=="mikan")throw;
                        Storage.Log(ex);preferences.SkinId="mikan";pet=new PetWindow(root,preferences);
                        MessageBox.Show("上次选择的皮肤未能加载，已恢复蜜柑默认皮肤。","Catcon");
                    }
                    bool first=!File.Exists(Storage.SettingsPath);
                    if(first)pet.Shown+=delegate{pet.BeginInvoke((Action)delegate{pet.OpenPanel();});};
                    Application.Run(pet);
                    mutex.ReleaseMutex();
                }
                return 0;
            }catch(Exception ex){Storage.Log(ex);if(args.Length==0)MessageBox.Show("Catcon 未能启动。请保留 exe 旁的 assets 和 config 文件夹。\n\n"+ex.Message,"Catcon",MessageBoxButtons.OK,MessageBoxIcon.Error);else{try{File.WriteAllText(Path.Combine(root,"command-error.txt"),ex.ToString());}catch{}}return 1;}
        }
    }
}
