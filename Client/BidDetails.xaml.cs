using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for BidDetails.xaml
    /// </summary>
    public partial class BidDetails : Window
    {
        private readonly PRN221_ProjectContext _context;
        public BidDetails(PRN221_ProjectContext context)
        {
            InitializeComponent();
            _context = context;
            LoadData();
        }

        private void LoadData()
        {
            Item item = _context.Items.SingleOrDefault(x => x.ItemId == 1);
            Bid bid = _context.Bids.SingleOrDefault(x => x.ItemId == 1 && x.BidPrice==item.CurrentPrice);
            Member bidder = _context.Members.SingleOrDefault(x => x.MemberId == bid.BidderId);
            Member seller = _context.Members.SingleOrDefault(x => x.MemberId == item.SellerId);
            ItemType itemType = _context.ItemTypes.SingleOrDefault(x => x.ItemTypeId == item.ItemTypeId);

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
            txtTimeRemaining.Content = Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours) >= 0 ? (Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours)+" hours") : (0 + " hours");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Item item = _context.Items.SingleOrDefault(x => x.ItemId == int.Parse(txtItemId.Content.ToString()));
                if (item != null)
                {
                    if (Math.Round((item.EndDateTime - DateTime.Now).Value.TotalHours) >= 0)
                    {
                        if (!string.IsNullOrEmpty(txtBidPrice.Text))
                        {
                            decimal bidPrice;
                            if(decimal.TryParse(txtBidPrice.Text, out bidPrice))
                            {
                                if (bidPrice >= item.CurrentPrice + item.MinimumBidIncrement)
                                {
                                    item.CurrentPrice = bidPrice;
                                    _context.Items.Update(item);

                                    Bid bid = new Bid();
                                    bid.ItemId = item.ItemId;
                                    bid.BidderId = 1;
                                    bid.BidDateTime = DateTime.Now;
                                    bid.BidPrice = bidPrice;
                                    _context.Bids.Add(bid);

                                    _context.SaveChanges();
                                    LoadData();
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ListBids listBids = new ListBids(_context);
            listBids.ShowDialog();
        }
    }
}
