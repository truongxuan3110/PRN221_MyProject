using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
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
    /// Interaction logic for ListBids.xaml
    /// </summary>
    public partial class ListBids : Window
    {
        private readonly PRN221_ProjectContext _context;
        List<string> filterTime = new List<string>()
            {
                "Oldest -> Newest",
                "Newest -> Oldest"
            };
        public ListBids(PRN221_ProjectContext context)
        {
            InitializeComponent();
            _context = context;
            lbUsername.Content = "ABC";
            cboTimeFilter.ItemsSource = filterTime;
            cboTimeFilter.SelectedIndex = 0;
            listBids.ItemsSource = _context.Bids.Include(x => x.Item).OrderBy(b => b.BidDateTime).ToList();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void cboTimeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(cboTimeFilter.SelectedIndex == 0)
            {
                if (!string.IsNullOrEmpty(txtItemSearch.Text))
                {
                    listBids.ItemsSource = _context.Bids.Include(x => x.Item).Where(x => x.Item.ItemName.Contains(txtItemSearch.Text)).OrderBy(b => b.BidDateTime).ToList();
                }
                else
                {
                    listBids.ItemsSource = _context.Bids.Include(x => x.Item).OrderBy(b => b.BidDateTime).ToList();
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(txtItemSearch.Text))
                {
                    listBids.ItemsSource = _context.Bids.Include(x => x.Item).Where(x => x.Item.ItemName.Contains(txtItemSearch.Text)).OrderByDescending(b => b.BidDateTime).ToList();
                }
                else
                {
                    listBids.ItemsSource = _context.Bids.Include(x => x.Item).OrderByDescending(b => b.BidDateTime).ToList();
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtItemSearch.Text))
            {
                if(cboTimeFilter.SelectedIndex == 0)
                {
                    listBids.ItemsSource = _context.Bids.Include(x => x.Item).Where(x => x.Item.ItemName.Contains(txtItemSearch.Text)).OrderBy(b => b.BidDateTime).ToList();
                }
                else
                {
                    listBids.ItemsSource = _context.Bids.Include(x => x.Item).Where(x => x.Item.ItemName.Contains(txtItemSearch.Text)).OrderByDescending(b => b.BidDateTime).ToList();
                }
                
            }
        }
    }
}
