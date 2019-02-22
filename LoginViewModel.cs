using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace NotificationIcon.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _loginID;
        public string LoginID
        {
            get { return _loginID; }
            set { _loginID = value; OnPropertyUpdate("LoginID"); }
        }
        private string _loginPasswd;
        public string LoginPasswd
        {
            get { return _loginPasswd; }
            set { _loginPasswd = value; OnPropertyUpdate("LoginPasswd"); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyUpdate(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
