using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AssFontSubset
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0) {
                Task.Run(() => {
                    using (var client = new WebClient()) {
                        try {
                            byte[] buf = client.DownloadData("https://raw.githubusercontent.com/youlun/AssFontSubset/master/AssFontSubset/Properties/AssemblyInfo.cs");
                            string data = Encoding.UTF8.GetString(buf);
                            var match = Regex.Match(data, @"\[assembly: AssemblyVersion\(""([0-9\.]*?)""\)\]", RegexOptions.ECMAScript | RegexOptions.Compiled);
                            if (match.Groups.Count > 1) {
                                var onlineVer = new Version(match.Groups[1].Value);
                                var localVer = Assembly.GetEntryAssembly().GetName().Version;
                                if (onlineVer > localVer) {
                                    MessageBox.Show("发现新版本，请去 GitHub 主页下载", "新版", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                        } catch { }
                    }
                });
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(args));
        }
    }
}
