using DataAccess.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Collections.Generic;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PRN221_ProjectContext dbContext;
        private TcpListener tcpListener;

        public MainWindow()
        {
            InitializeComponent();
            dbContext = new PRN221_ProjectContext();

            // Tạo một luồng mới để lắng nghe kết nối
            Task.Factory.StartNew(() => StartListen());
        }

        private void StartListen()
        {
            try
            {
                tcpListener = new TcpListener(System.Net.IPAddress.Any, 8081);
                tcpListener.Start();

                while (true)
                {
                    Socket socketForServer = tcpListener.AcceptSocket();

                    // Mỗi kết nối từ client được xử lý trong một Task riêng biệt
                    Task.Run(() => Runserver(socketForServer));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"xxx. An server error occurred: {ex.Message}");
            }
        }

        private void Runserver(Socket socket)
        {
            NetworkStream networkStream = null;
            StreamWriter streamWriter = null;
            StreamReader streamReader = null;
            bool isCloseConnection = false;

            try
            {
                networkStream = new NetworkStream(socket);
                streamReader = new StreamReader(networkStream);
                streamWriter = new StreamWriter(networkStream);


                // Đọc yêu cầu đăng nhập từ client
                string loginRequest = streamReader.ReadLine();

                if (loginRequest != null && loginRequest.StartsWith("LOGIN"))
                {
                    string[] loginInfo = loginRequest.Split('|');
                    string email = loginInfo[1];
                    string password = loginInfo[2];
                    //sendmail();
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

                                ReceiveRequestFromClient(socket);
                                isCloseConnection = false;
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
            catch (Exception ex)
            {
                Console.WriteLine($"2.An server error occurred: {ex.Message}");
            }
            finally
            {
                Cleanup(streamReader, streamWriter, networkStream, socket);
            }
        }



        private void ReceiveRequestFromClient(Socket socket)
        {
            try
            {
                ////// Kiểm tra xem kết nối đã đóng chưa
                //if (socket.Poll(10, SelectMode.SelectRead) && socket.Available == 0)
                //{
                //    // Nếu kết nối đã đóng, thì thực hiện kết nối lại
                //    tcpListener.Stop();
                //    StartListen(); // Gọi lại hàm để lắng nghe kết nối mới
                //}
                //else
                //{
                while (true)
                {
                    using (NetworkStream networkStream = new NetworkStream(socket))
                    using (StreamWriter streamWriter = new StreamWriter(networkStream))
                    using (StreamReader streamReader = new StreamReader(networkStream))
                    {

                        string requestFromClient = streamReader.ReadLine();
                        if (requestFromClient != null)
                        {
                            string[] requestInfo = requestFromClient.Split('|');
                            if (requestInfo[0].Equals("AddNewItem"))
                            {
                                string jsonItem = requestInfo[1];

                                if (!string.IsNullOrEmpty(jsonItem))
                                {
                                    Item newItem = JsonConvert.DeserializeObject<Item>(jsonItem);

                                    dbContext.Items.Add(newItem);
                                    dbContext.SaveChanges();
                                    int newItemId = newItem.ItemId;

                                    Bid bid = new Bid
                                    {
                                        ItemId = newItemId,
                                        BidderId = newItem.SellerId,
                                        BidDateTime = DateTime.Now,
                                        BidPrice = newItem.CurrentPrice
                                    };
                                    dbContext.Bids.Add(bid);
                                    dbContext.SaveChanges(true);
                                    streamWriter.WriteLine("Added new Item");
                                    streamWriter.Flush();
                                }
                                else
                                {
                                    Console.WriteLine("Server Error: jsonItem is null");
                                }
                            }
                            else if (requestInfo[0].Equals("LoadItemList"))
                            {
                                string jsonFilterItems = requestInfo[1];
                                if (!string.IsNullOrEmpty(jsonFilterItems))
                                {
                                    LoadItemList(socket, int.Parse(jsonFilterItems));
                                }
                                else
                                {
                                    Console.WriteLine("Server Error: jsonFilterItems is null");
                                }
                            }
                            else if (requestInfo[0].Equals("LoadBidList"))
                            {
                                LoadBidList(socket, int.Parse(requestInfo[1]));
                            }
                            else if (requestInfo[0].Equals("GetBidDetails"))
                            {
                                GetBidDetails(socket, int.Parse(requestInfo[1]));
                            }else if (requestInfo[0].Equals("GetItemById"))
                            {
                                GetItemById(socket, int.Parse(requestInfo[1]));
                            }else if (requestInfo[0].Equals("AddNewBid"))
                            {
                                AddNewBid(socket, requestInfo[1], requestInfo[2]);
                            }
                        }
                        else
                        {
                            Console.WriteLine("requestFromClient is null");
                        }
                    }
                }
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"huhu. An server error occurred while ReceiveRequestFromClient: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
            }
        }

        private void GetItemById(Socket socket, int itemId)
        {
            using (NetworkStream networkStream = new NetworkStream(socket))
            using (StreamWriter streamWriter = new StreamWriter(networkStream))
            using (StreamReader streamReader = new StreamReader(networkStream))
            {
                Item item = dbContext.Items.SingleOrDefault(x => x.ItemId == itemId);


                // Configure the JsonSerializer to handle reference loops
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                string itemJson = JsonConvert.SerializeObject(item, settings);

               

                // Send the JSON string to the client
                streamWriter.WriteLine("GetItemById|" + itemJson);
                streamWriter.Flush();
            }
        }

        private void AddNewBid(Socket socket, string itemJson, string bidJson)
        {
            using (NetworkStream networkStream = new NetworkStream(socket))
            using (StreamWriter streamWriter = new StreamWriter(networkStream))
            using (StreamReader streamReader = new StreamReader(networkStream))
            {
                try
                {
                    Bid bid = JsonConvert.DeserializeObject<Bid>(bidJson);
                    Item updatedItem = JsonConvert.DeserializeObject<Item>(itemJson);

                    // Thêm mới Bid vào cơ sở dữ liệu
                    dbContext.Bids.Add(bid);
                    dbContext.SaveChanges();

                    // Cập nhật thông tin của Item trong cơ sở dữ liệu
                    var existingItem = dbContext.Items.Find(updatedItem.ItemId);

                    if (existingItem != null)
                    {
                        // Nếu tồn tại, cập nhật thông tin của Item đã tồn tại
                        existingItem.CurrentPrice = updatedItem.CurrentPrice;
                        dbContext.SaveChanges();

                        streamWriter.WriteLine("Added new Bid and updated Item");
                        streamWriter.Flush();
                    }
                    else
                    {
                        streamWriter.WriteLine("Item not found");
                        streamWriter.Flush();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An server error occurred while adding new Bid and updating Item: {ex.Message}");
                    Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                }
            }
        }


        private void GetBidDetails(Socket socket, int itemId)
        {
            using (NetworkStream networkStream = new NetworkStream(socket))
            using (StreamWriter streamWriter = new StreamWriter(networkStream))
            using (StreamReader streamReader = new StreamReader(networkStream))
            {
                Item item = dbContext.Items.SingleOrDefault(x => x.ItemId == itemId);
                Bid bid = dbContext.Bids.SingleOrDefault(x => x.ItemId == itemId && x.BidPrice == item.CurrentPrice);
                Member bidder = dbContext.Members.SingleOrDefault(x => x.MemberId == bid.BidderId);
                Member seller = dbContext.Members.SingleOrDefault(x => x.MemberId == item.SellerId);
                ItemType itemType = dbContext.ItemTypes.SingleOrDefault(x => x.ItemTypeId == item.ItemTypeId);


                // Configure the JsonSerializer to handle reference loops
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                string itemJson = JsonConvert.SerializeObject(item, settings);
                string bidJson = JsonConvert.SerializeObject(bid, settings);
                string bidderJson = JsonConvert.SerializeObject(bidder, settings);
                string sellerJson = JsonConvert.SerializeObject(seller, settings);
                string itemTypeJson = JsonConvert.SerializeObject(itemType, settings);

               

                // Send the JSON string to the client
                streamWriter.WriteLine("GetBidDetails|" + itemJson + "|" + bidJson + "|" + bidderJson + "|" + sellerJson + "|" + itemTypeJson);
                streamWriter.Flush();
            }
        }

        private void LoadItemList(Socket socket, int memberId = 0)
        {
            using (NetworkStream networkStream = new NetworkStream(socket))
            using (StreamWriter streamWriter = new StreamWriter(networkStream))
            using (StreamReader streamReader = new StreamReader(networkStream))
            {
                List<Item> itemList = new List<Item>();
                if (memberId != 0)
                {
                    // Retrieve the items from the database based on the logged-in member
                    itemList = dbContext.Items.Where(item => item.SellerId == memberId).ToList();
                }
                else
                {
                    itemList = dbContext.Items.ToList();
                }

                // Configure the JsonSerializer to handle reference loops
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                // Convert the list of items to a JSON string using the settings
                string itemListJson = JsonConvert.SerializeObject(itemList, settings);

                // Send the JSON string to the client
                streamWriter.WriteLine("LoadItemList|" + itemListJson);
                streamWriter.Flush();
            }

        }

        private void LoadBidList(Socket socket, int memberId = 0)
        {
            using (NetworkStream networkStream = new NetworkStream(socket))
            using (StreamWriter streamWriter = new StreamWriter(networkStream))
            using (StreamReader streamReader = new StreamReader(networkStream))
            {
                List<Bid> bidList = new List<Bid>();

                if (memberId != 0)
                {
                    // Retrieve the items from the database based on the logged-in member
                    bidList = dbContext.Bids.Where(bid => bid.BidderId == memberId).ToList();
                }
                else
                {
                    bidList = dbContext.Bids.ToList();
                }


                // Configure the JsonSerializer to handle reference loops
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                // Convert the list of items to a JSON string using the settings
                string bidListJson = JsonConvert.SerializeObject(bidList, settings);

                // Send the JSON string to the client
                streamWriter.WriteLine("LoadBidList|" + bidListJson);
                streamWriter.Flush();
            }
        }

        private void sendmail()
        {
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com");
            smtpClient.Port = 587;
            smtpClient.Credentials = new NetworkCredential("truongx62001@gmail.com", "vvjd sbsw topm upgh");
            smtpClient.EnableSsl = true;

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("truongx62001@gmail.com");
            mail.To.Add("truongndhe150878@fpt.edu.vn");
            mail.Subject = "Hello Honey";
            mail.Body = "Test gui mail";

            smtpClient.Send(mail);
        }

        private Member CheckAccount(string username, string password)
        {
            // Kiểm tra users trong db
            return dbContext.Members.FirstOrDefault(u => u.Email == username && u.Password == password);
        }

        private void Cleanup(StreamReader streamReader, StreamWriter streamWriter, NetworkStream networkStream, Socket socket)
        {
            try
            {
                streamReader?.Close();
                streamWriter?.Close();
                networkStream?.Close();
                socket?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"haha. An server error occurred: {ex.Message}");
                Console.WriteLine($"Inner Exception: {ex.InnerException?.Message}");

            }
        }
    }
}
