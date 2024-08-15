﻿using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TextTranscoder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ListView listView = (ListView)sender;
            GridViewColumn column = ((GridView)listView.View).Columns[1];
            column.Width = e.NewSize.Width - 130;
        }

        private void ElementGroup_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ItemsControl container = (ItemsControl)sender;
            TextBox textBox = (TextBox)container.Items[1];
            textBox.Width = e.NewSize.Width - 120;
        }
    }
}