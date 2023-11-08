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
        public MainWindow()
        {
            InitializeComponent();
            dbContext = new PRN221_ProjectContext();
            listAllItem.Visibility = Visibility.Collapsed;
            loginGrid.Visibility = Visibility.Visible;
            addItemGrid.Visibility = Visibility.Collapsed;
            ConnectToServer();
        }

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
                        var yourItems = dbContext.Items.Where(item => item.SellerId == member.MemberId).ToList();
                        lvItems.ItemsSource = yourItems;
                    }
                }
                else
                {
                    lvItems.ItemsSource = dbContext.Items.ToList();
                }               
            }
        }

        private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = tbSearch.Text.Trim();
            if(string.IsNullOrEmpty(searchText) )
            {
                lvItems.ItemsSource = dbContext.Items.ToList();
            }
            else
            {
                // Thực hiện tìm kiếm trong cơ sở dữ liệu theo văn bản người dùng nhập
                var searchedList = dbContext.Items.Where(item => item.ItemName.ToLower().Contains(searchText.ToLower())).ToList();
                lvItems.ItemsSource = searchedList;
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
            string email  = tbUsername.Text.Trim();
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
                         member = Newtonsoft.Json.JsonConvert.DeserializeObject<Member>(jsonMember);

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

        private void LoadListItemScreen()
        {
            List<string> filterItems = new List<string> { "All", "Your Items" };
            cbFilter.ItemsSource = filterItems;
            lvItems.ItemsSource = dbContext.Items.ToList();
            cbFilter.SelectedIndex = 0;
            // Đã đăng nhập thành công, sử dụng thông tin của đối tượng Member ở đây
            listAllItem.Visibility = Visibility.Visible;
            loginGrid.Visibility = Visibility.Collapsed;
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
            if(!int.TryParse(tbBidIncrement.Text, out minimumBidIncrement))
            {
                MessageBox.Show("Invalid format minimumBidIncrement!");
                return null;
            }
            DateTime enddate = dpEnd.SelectedDate ?? DateTime.Now;
            if(enddate < DateTime.Now)
            {
                MessageBox.Show("Enddate must be after current time!");
                return null;
            }
            decimal currentPrice = 0;
            if(!decimal.TryParse(tbCurrentPrice.Text, out currentPrice))
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
                dbContext.Items.Add(newItem);
                dbContext.SaveChanges();
                MessageBox.Show("Added new Item");

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
            cboTimeFilter.ItemsSource = filterTime;
            cboTimeFilter.SelectedIndex = 0;
            listBids.ItemsSource = dbContext.Bids.Include(x => x.Item).Where(x=>x.BidderId==member.MemberId).OrderBy(b => b.BidDateTime).ToList();
            listAllItem.Visibility = Visibility.Collapsed;
            listBidsGrid.Visibility = Visibility.Visible;
        }

        private void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtItemSearch.Text))
            {
                if (cboTimeFilter.SelectedIndex == 0)
                {
                    listBids.ItemsSource = dbContext.Bids.Include(x => x.Item).Where(x => x.Item.ItemName.Contains(txtItemSearch.Text) && x.BidderId == member.MemberId).OrderBy(b => b.BidDateTime).ToList();
                }
                else
                {
                    listBids.ItemsSource = dbContext.Bids.Include(x => x.Item).Where(x => x.Item.ItemName.Contains(txtItemSearch.Text) && x.BidderId == member.MemberId).OrderByDescending(b => b.BidDateTime).ToList();
                }

            }
        }
        private void cboTimeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboTimeFilter.SelectedIndex == 0)
            {
                if (!string.IsNullOrEmpty(txtItemSearch.Text))
                {
                    listBids.ItemsSource = dbContext.Bids.Include(x => x.Item).Where(x => x.Item.ItemName.Contains(txtItemSearch.Text) && x.BidderId == member.MemberId).OrderBy(b => b.BidDateTime).ToList();
                }
                else
                {
                    listBids.ItemsSource = dbContext.Bids.Include(x => x.Item).Where(x => x.BidderId == member.MemberId).OrderBy(b => b.BidDateTime).ToList();
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(txtItemSearch.Text))
                {
                    listBids.ItemsSource = dbContext.Bids.Include(x => x.Item).Where(x => x.Item.ItemName.Contains(txtItemSearch.Text) && x.BidderId == member.MemberId).OrderByDescending(b => b.BidDateTime).ToList();
                }
                else
                {
                    listBids.ItemsSource = dbContext.Bids.Include(x => x.Item).Where(x => x.BidderId == member.MemberId).OrderByDescending(b => b.BidDateTime).ToList();
                }
            }
        }

        private void cancelListBidsBtn_Click(object sender, RoutedEventArgs e)
        {
            listBidsGrid.Visibility = Visibility.Collapsed;
            LoadListItemScreen();
        }

        private void lvItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lvItems.SelectedItem is Item itemSelected)
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
            Item item = dbContext.Items.SingleOrDefault(x => x.ItemId == itemId);
            Bid bid = dbContext.Bids.SingleOrDefault(x => x.ItemId == itemId && x.BidPrice == item.CurrentPrice);
            Member bidder = dbContext.Members.SingleOrDefault(x => x.MemberId == bid.BidderId);
            Member seller = dbContext.Members.SingleOrDefault(x => x.MemberId == item.SellerId);
            ItemType itemType = dbContext.ItemTypes.SingleOrDefault(x => x.ItemTypeId == item.ItemTypeId);

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
            txtTimeRemaining.Content = Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours) >= 0 ? (Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours) + " hours") : (0 + " hours");
        }

        private void bidBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Item item = dbContext.Items.SingleOrDefault(x => x.ItemId == int.Parse(txtItemId.Content.ToString()));
                if (item != null)
                {
                    if (Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours) >= 0)
                    {
                        if (!string.IsNullOrEmpty(txtBidPrice.Text))
                        {
                            decimal bidPrice;
                            if (decimal.TryParse(txtBidPrice.Text, out bidPrice))
                            {
                                if (bidPrice >= item.CurrentPrice + item.MinimumBidIncrement)
                                {
                                    item.CurrentPrice = bidPrice;
                                    dbContext.Items.Update(item);

                                    Bid bid = new Bid();
                                    bid.ItemId = item.ItemId;
                                    bid.BidderId = 1;
                                    bid.BidDateTime = DateTime.Now;
                                    bid.BidPrice = bidPrice;
                                    dbContext.Bids.Add(bid);

                                    dbContext.SaveChanges();
                                    LoadDataBidDetails(item.ItemId);
                                    MessageBox.Show($"Bidded successfull", "Bid Item");
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
