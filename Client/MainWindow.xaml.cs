using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
using static MongoDB.Bson.Serialization.Serializers.SerializerHelper;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using Member = DataAccess.Models.Member;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PRN221_ProjectContext dbContext;
        private Member member;
        private TcpClient tcpClient;
        private StreamWriter streamWriter;
        private StreamReader streamReader;

        private List<Item> itemListCopy;
        private List<Item> searchedItemList;
        private List<Bid> bidListCopy;

        public MainWindow()
        {
            InitializeComponent();
            cbFilter.ItemsSource = filterItems;
            cboTimeFilter.ItemsSource = filterTime;
            dbContext = new PRN221_ProjectContext();
            listAllItem.Visibility = Visibility.Collapsed;
            loginGrid.Visibility = Visibility.Visible;
            addItemGrid.Visibility = Visibility.Collapsed;
            ConnectToServer();
        }
        List<string> filterItems = new List<string> { "All", "Your Items" };

        private void AddItemBtn_Click(object sender, RoutedEventArgs e)
        {
            listAllItem.Visibility = Visibility.Collapsed;
            addItemGrid.Visibility = Visibility.Visible;
            cbItemTypes.ItemsSource = dbContext.ItemTypes.ToList();
        }

        private void cbFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbFilter.SelectedItem != null)
            {
                string selectedFilter = cbFilter.SelectedItem.ToString();


                if (selectedFilter == "Your Items")
                {
                    // Lấy ra danh sách các mục "Your Items" dựa trên memberId của member đã đăng nhập
                    if (member != null)
                    {
                        LoadListItemScreen(member.MemberId);
                    }
                }
                else
                {
                    LoadListItemScreen();
                }
            }
        }

        private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = tbSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                searchedItemList = itemListCopy;
                lvItems.ItemsSource = searchedItemList;
            }
            else
            {
                var tmp = searchedItemList
            .Where(item => item.ItemName.ToLower().Contains(searchText))
            .ToList();
                searchedItemList = tmp;
                lvItems.ItemsSource = searchedItemList;
            }
        }

        private void ConnectToServer()
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect("127.0.0.1", 8081);
                NetworkStream networkStream = tcpClient.GetStream();
                streamWriter = new StreamWriter(networkStream);
                streamReader = new StreamReader(networkStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"2. Cannot connect to the server.client Error: {ex.Message}");
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = tbUsername.Text.Trim();
            string password = tbPassword.Text.Trim();
            // Kiểm tra nếu cả hai username và password không rỗng
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Email or Password is empty!");
                return;
            }

            // Gửi yêu cầu đăng nhập lên server
            SendLoginRequest(email, password);
        }

        private void SendLoginRequest(string username, string password)
        {
            try
            {
                if (tcpClient == null || !tcpClient.Connected)
                {
                    ConnectToServer();
                }
                else
                {
                    // Gửi dữ liệu đến server qua streamWriter
                    streamWriter.WriteLine($"LOGIN|{username}|{password}");
                    streamWriter.Flush();


                    // Đọc chuỗi JSON từ server
                    string jsonMember = streamReader.ReadLine();

                    // Kiểm tra và xử lý phản hồi từ server
                    if (!string.IsNullOrEmpty(jsonMember))
                    {
                        // Chuyển đổi JSON thành đối tượng Member
                        member = JsonConvert.DeserializeObject<DataAccess.Models.Member>(jsonMember);

                        if (member != null)
                        {
                            LoadListItemScreen();
                        }
                        else
                        {
                            MessageBox.Show("Login failed. Incorrect username or password!");
                            ConnectToServer();
                        }
                    }
                    else
                    {
                        // Xử lý khi không nhận được dữ liệu từ server
                        MessageBox.Show("Empty response from server.");
                        ConnectToServer();
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"1. An client error occurred: {ex.Message}");
            }
        }

        private void LoadListItemScreen(int memberId = 0)
        {
            // Send a request to the server to load the item list
            streamWriter.WriteLine("LoadItemList|" + memberId);
            streamWriter.Flush();
            // Read the response from the server
            string response = streamReader.ReadLine();

            // Process the response, assuming it contains the item list
            if (response != null && response.StartsWith("LoadItemList|"))
            {
                string itemListJson = response.Substring("LoadItemList|".Length);
                List<Item> itemList = JsonConvert.DeserializeObject<List<Item>>(itemListJson);

                if (memberId == 0)
                {
                    itemListCopy = itemList;
                }

                searchedItemList = itemList;

                // Update the UI with the loaded item list
                lvItems.ItemsSource = itemList;
                cbFilter.SelectedIndex = 0;
                // Đã đăng nhập thành công, sử dụng thông tin của đối tượng Member ở đây
                listAllItem.Visibility = Visibility.Visible;
                loginGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                Console.WriteLine("Unexpected response from the server");
            }
        }

        private Item getInfoInAddItemForm()
        {
            int itemTypeId = (int)((cbItemTypes.SelectedItem as ItemType)?.ItemTypeId);
            string itemName = tbItemName.Text.Trim();
            if (string.IsNullOrEmpty(itemName))
            {
                MessageBox.Show("itemName must be not empty!");
                return null;
            }
            string itemDescription = tbDescription.Text;

            int minimumBidIncrement = 0;
            if (!int.TryParse(tbBidIncrement.Text, out minimumBidIncrement))
            {
                MessageBox.Show("Invalid format minimumBidIncrement!");
                return null;
            }
            DateTime enddate = dpEnd.SelectedDate ?? DateTime.Now;
            if (enddate < DateTime.Now)
            {
                MessageBox.Show("Enddate must be after current time!");
                return null;
            }
            decimal currentPrice = 0;
            if (!decimal.TryParse(tbCurrentPrice.Text, out currentPrice))
            {
                MessageBox.Show("Invalid format currentPrice!");
                return null;
            }

            return new Item
            {
                ItemTypeId = itemTypeId,
                ItemName = itemName,
                ItemDescription = itemDescription,
                SellerId = member.MemberId,
                MinimumBidIncrement = minimumBidIncrement,
                EndDateTime = enddate,
                CurrentPrice = currentPrice
            };

        }
        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Item newItem = getInfoInAddItemForm();
            if (newItem != null)
            {
                // Convert Member object to JSON string
                string newItemJson = JsonConvert.SerializeObject(newItem);

                // Send JSON string to client
                streamWriter.WriteLine("AddNewItem|" + newItemJson);
                streamWriter.Flush();


                // Đọc phản hồi từ server
                string response = streamReader.ReadLine();

                MessageBox.Show(response);

                addItemGrid.Visibility = Visibility.Collapsed;
                LoadListItemScreen();
            }
        }

        List<string> filterTime = new List<string>()
            {
                "Oldest -> Newest",
                "Newest -> Oldest"
            };
        private void ListBidsBtn_Click(object sender, RoutedEventArgs e)
        {
            lbName.Content = member.Name;

            // Send a request to the server to load the item list
            streamWriter.WriteLine("LoadBidList|" + member.MemberId);
            streamWriter.Flush();
            // Read the response from the server
            string response = streamReader.ReadLine();

            // Process the response, assuming it contains the item list
            if (response != null && response.StartsWith("LoadBidList|"))
            {
                string itemListJson = response.Substring("LoadBidList|".Length);
                List<Bid> bidList = JsonConvert.DeserializeObject<List<Bid>>(itemListJson);

                bidListCopy = bidList;


                // Update the UI with the loaded item list
                listBids.ItemsSource = bidListCopy;
                cboTimeFilter.SelectedIndex = 0;
                // Đã đăng nhập thành công, sử dụng thông tin của đối tượng Member ở đây
                listAllItem.Visibility = Visibility.Collapsed;
                listBidsGrid.Visibility = Visibility.Visible;
            }
            else
            {
                Console.WriteLine("Unexpected response from the server");
            }
        }


        private void filterBid(string itemName = "", bool isASCOrder = true)
        {
            List<Bid> bids = new List<Bid>();
            foreach (Bid bid in bidListCopy)
            {
                foreach (Item item in itemListCopy)
                {
                    if (bid.ItemId == item.ItemId && bid.BidderId == member.MemberId)
                    {
                        if (!string.IsNullOrEmpty(itemName) && item.ItemName.ToLower().Contains(itemName.ToLower()))
                        {
                            bids.Add(bid); break;
                        }
                        else if (string.IsNullOrEmpty(itemName))
                        {
                            bids.Add(bid);
                        }
                    }
                }
            }

            if (isASCOrder)
            {
                listBids.ItemsSource = bids.OrderBy(b => b.BidDateTime).ToList();
            }
            else
            {
                listBids.ItemsSource = bids.OrderByDescending(b => b.BidDateTime).ToList();
            }
        }
        private void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            filterBid(txtItemSearch.Text.Trim(), cboTimeFilter.SelectedIndex == 0);
        }
        private void cboTimeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            filterBid(txtItemSearch.Text.Trim(), cboTimeFilter.SelectedIndex == 0);
        }

        private void cancelListBidsBtn_Click(object sender, RoutedEventArgs e)
        {
            listBidsGrid.Visibility = Visibility.Collapsed;
            LoadListItemScreen();
        }

        private void lvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvItems.SelectedItem is Item itemSelected)
            {
                listAllItem.Visibility = Visibility.Collapsed;
                bidDetailsGrid.Visibility = Visibility.Visible;
                LoadDataBidDetails(itemSelected.ItemId);
            }
            else
            {
                Console.WriteLine("Lỗi");
            }
        }

        private void LoadDataBidDetails(int itemId)
        {
            streamWriter.WriteLine("GetBidDetails|" + itemId);
            streamWriter.Flush();
            // Read the response from the server
            string response = streamReader.ReadLine();

            // Process the response, assuming it contains the item list
            if (response != null && response.StartsWith("GetBidDetails|"))
            {
                string[] responseInfo = response.Split('|');
                Item item = JsonConvert.DeserializeObject<Item>(responseInfo[1]);
                Bid bid = JsonConvert.DeserializeObject<Bid>(responseInfo[2]);
                Member bidder = JsonConvert.DeserializeObject<Member>(responseInfo[3]);
                Member seller = JsonConvert.DeserializeObject<Member>(responseInfo[4]);
                ItemType itemType = JsonConvert.DeserializeObject<ItemType>(responseInfo[5]);


                txtBidId.Content = bid.BidId;
                txtBidderName.Content = bidder.Name;
                txtTypeName.Content = itemType.ItemTypeName;
                txtItemId.Content = item.ItemId;
                txtItemName.Content = item.ItemName;
                txtItemDescription.Content = item.ItemDescription;
                txtSellerName.Content = seller.Name;
                txtCurrentPrice.Content = item.CurrentPrice;
                txtMinimumBidIncrement.Content = item.MinimumBidIncrement;
                txtEndDateTime.Content = item.EndDateTime;
                txtTimeRemaining.Content = Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours) >= 0 ?
                    (Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours) + " hours") : (0 + " hours");
            }
            else
            {
                Console.WriteLine("Unexpected response from the server");
            }

        }

        private void bidBtn_Click(object sender, RoutedEventArgs e)
        {

            int itemId = int.Parse(txtItemId.Content.ToString());
            streamWriter.WriteLine("GetItemById|" + itemId);
            streamWriter.Flush();
            try
            {
                // Read the response from the server
                string response = streamReader.ReadLine();
                if (response != null && response.StartsWith("GetItemById|"))
                {
                    string[] responseInfo = response.Split('|');
                    Item item = JsonConvert.DeserializeObject<Item>(responseInfo[1]);

                    Console.WriteLine(item.ItemName + " client name");
                    if (item != null)
                    {
                        if (Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours) >= 0)
                        {
                            if (!string.IsNullOrEmpty(txtBidPrice.Text))
                            {
                                decimal bidPrice;
                                if (decimal.TryParse(txtBidPrice.Text, out bidPrice))
                                {
                                    if (bidPrice >= (item.CurrentPrice + item.MinimumBidIncrement))
                                    {
                                        Console.WriteLine("mlem ");
                                        item.CurrentPrice = bidPrice;
                                       
                                        Bid bid = new Bid();
                                        bid.ItemId = item.ItemId;
                                        bid.BidderId = member.MemberId;
                                        bid.BidDateTime = DateTime.Now;
                                        bid.BidPrice = bidPrice;

                                        // Configure the JsonSerializer to handle reference loops
                                        var settings = new JsonSerializerSettings
                                        {
                                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                                        };

                                        // Convert Member object to JSON string
                                        string newBidJson = JsonConvert.SerializeObject(bid, settings);
                                        string newItemPriceJson = JsonConvert.SerializeObject(item, settings);

                                        // Send JSON string to client
                                        streamWriter.WriteLine("AddNewBid|" + newBidJson+"|"+ newItemPriceJson);
                                        streamWriter.Flush();


                                        string resultAdded = streamReader.ReadLine();
                                        MessageBox.Show(resultAdded);
                                        LoadDataBidDetails(item.ItemId);
                                       
                                    }
                                    else
                                    {
                                        MessageBox.Show($"Bid Price must be equal to or greater than the “Current price + Minimum_bid_increment”", "Bid Item");
                                    }
                                }
                                else
                                {
                                    MessageBox.Show($"Bid Price must be is a number", "Bid Item");
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Auction time has expired", "Bid Item");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Unexpected response from the server");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Bid Item");
            }
        }

        private void bidDetailsCancelBtn_Click(object sender, RoutedEventArgs e)
        {
            bidDetailsGrid.Visibility = Visibility.Collapsed;
            LoadListItemScreen();
        }
    }
}
