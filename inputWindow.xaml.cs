using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NotificationIcon.ViewModels;

namespace NotificationIcon
{
    public partial class inputWindow : Window
    {
        public inputWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
            //this.loginSubmit.Click += loginClick;
            this.DataContext = new LoginViewModel();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(this.txtId);
        }

        private void loginClick(object sender, RoutedEventArgs e)
        {
            var viewModel = this.DataContext as LoginViewModel;

            if (string.IsNullOrEmpty(viewModel.LoginID))
            {
                labelNoti.Content = "아이디를 입력해주세요";
                Keyboard.Focus(this.txtId);
                return;
            }
            if (string.IsNullOrEmpty(txtPassword.Password))
            {
                labelNoti.Content = "암호를 입력해주세요";
                Keyboard.Focus(this.txtPassword);
                return;
            }
            MainWindow MW = new MainWindow(viewModel.LoginID, txtPassword.Password);
            //MW.Show();
            Hide();
        }
    }
}
