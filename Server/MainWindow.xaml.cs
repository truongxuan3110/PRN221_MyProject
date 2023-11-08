using DataAccess.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PRN221_ProjectContext dbContext;
        private TcpListener tcpListener;
        private Socket socketForServer;
        private NetworkStream networkStream;
        private StreamWriter streamWriter;
        private StreamReader streamReader;
        public MainWindow()
        {
            InitializeComponent();
            dbContext = new PRN221_ProjectContext();

            // Tạo một luồng mới để lắng nghe kết nối
            Thread listenerThread = new Thread(new ThreadStart(StartListen));
            listenerThread.Start();
        }

        private void StartListen()
        {
            try
            {
                tcpListener = new TcpListener(System.Net.IPAddress.Any, 8081);
                tcpListener.Start();

                while (true)
                {
                    socketForServer = tcpListener.AcceptSocket();
                    Task.Run(() => Runserver(socketForServer));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"1. An server error occurred: {ex.Message}");
                return;
            }
        }

        private void Runserver(Socket socket)
        {
            bool isCloseConnection = false;

            try
            {
                using (networkStream = new NetworkStream(socket))
                using (streamReader = new StreamReader(networkStream))
                using (streamWriter = new StreamWriter(networkStream))
                {

                    // Đọc yêu cầu đăng nhập từ client
                    string loginRequest = streamReader.ReadLine();

                    if (loginRequest != null && loginRequest.StartsWith("LOGIN"))
                    {
                        string[] loginInfo = loginRequest.Split('|');
                        string email = loginInfo[1];
                        string password = loginInfo[2];

                        // Kiểm tra tài khoản trong cơ sở dữ liệu
                        var member = CheckAccount(email, password);

                        // Convert Member object to JSON string
                        string memberJson = JsonConvert.SerializeObject(member);

                        // Send JSON string to client
                        streamWriter.WriteLine(memberJson);
                        streamWriter.Flush();

                        if (member == null)
                        {
                            isCloseConnection = true;
                        }

                        while (!isCloseConnection)
                        {
                            // nếu networkStream chưa bị đóng và socket chưa bị đóng
                            if (networkStream.CanRead)
                            {
                                if (member != null)
                                {
                                    Console.WriteLine("server server");
                                    isCloseConnection = true;
                                }
                            }
                            else
                            {
                                // nếu networkStream bị đóng hoặc socket bị đóng
                                isCloseConnection = true;
                                Thread.Sleep(5000);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"2.An server error occurred: {ex.Message}");
            }
            finally
            {
                Cleanup();
                socket.Close();
            }
        }

        private Member CheckAccount(string username, string password)
        {
            // Kiểm tra users trong db
           return dbContext.Members.FirstOrDefault(u => u.Email == username && u.Password == password);
        }

        private void Cleanup()
        {
            try
            {
                streamReader?.Close();
                streamWriter?.Close();
                networkStream?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"5. An server error occurred: {ex.Message}");
            }
        }
    }
}
