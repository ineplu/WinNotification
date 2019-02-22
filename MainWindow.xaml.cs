namespace NotificationIcon
{
    using System;
    using System.Windows;
    using System.Windows.Forms; // NotifyIcon control
    using System.Drawing; // Icon
    using System.IO;
    using System.Xml;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Text;
    using System.Windows.Navigation;
    using NotificationIcon.ViewModels;
    using mshtml;

    public partial class MainWindow : Window
    {
        //�����ܸ��ҽ��� ������ �������� ����
        public static Assembly myAssembly = Assembly.GetAssembly(typeof(MainWindow));
        public NotifyIcon notify;

        GetUrl geturl = new GetUrl();
        //XML�� ������ ������ ��´�
        Dictionary<int, XmlNodeList> mContainer = null;
        Dictionary<int, XmlNodeList> mTmpContainer = null;

        string title = "";
        string tmpTitle = "";
        string getID = "";
        string getPW = "";
        public static string token = "";
        // ȣ���ϴ� ������
        public static string domain = Properties.Resources.domain;

        public MainWindow(string inputID, string inputPW)
        {
            //���μ���üũ�� �ϰ� �ߺ������� �����Ѵ�
            Process[] procs = Process.GetProcessesByName("WinNoti");

            if (procs.Length > 1)
            {
                this.Close();
            }
            getID = inputID;
            getPW = inputPW;

            InitializeComponent();
            Browser.Source = new Uri(domain);
            Browser.LoadCompleted += BrowserOnLoadCompleted;

            ContextMenu menu = new ContextMenu();
            //���� �޴��߰�
            MenuItem mItem = new MenuItem();
            menu.MenuItems.Add(mItem);
            mItem.Text = "����";
            mItem.Click += delegate (object click, EventArgs eClick)
            {
                Environment.Exit(0);
                System.Diagnostics.Process.GetCurrentProcess().Kill();
                this.Close();
            };

            Stream iconStream = myAssembly.GetManifestResourceStream("NotificationIcon.unnamed.ico");
            Icon menuIcon = new Icon(iconStream);

            NotifyIcon ni = new NotifyIcon();
            ni.Icon = menuIcon;
            ni.Visible = true;
            ni.Text = "���ž˸��� ��Ÿ";
            ni.ContextMenu = menu;

            Timer timer = new Timer();
            timer.Interval = 300000; //�ֱ� ���� 5��
            timer.Tick += new EventHandler(timer_Tick); //�ֱ⸶�� ����Ǵ� �̺�Ʈ ���
            timer.Start();
        }

        private void BrowserOnLoadCompleted(object sender, NavigationEventArgs e)
        {
            object[] codeString = { "document.getElementById('Username').value = '" + getID + "';document.getElementsByName('Password')[0].value = '" + getPW + "';Logon.Login();" };
            Browser.InvokeScript("eval", codeString);
            this.Hide();
            Browser.LoadCompleted -= BrowserOnLoadCompleted;
            Browser.LoadCompleted += BrowserOnLoadXMLCompleted;
        }

        private void BrowserOnLoadXMLCompleted(object sender, NavigationEventArgs e)
        {
            object[] codeString = { "function getCookie(a){a=('; '+document.cookie).split('; '+a+'=');if(2==a.length)return a.pop().split(';').shift()};getCookie('LtpaToken');" };
            var data = Browser.InvokeScript("eval", codeString);
            MainWindow.token = data.ToString();

            // ��ũ��Ʈ���� �����ϱ�
            //var browser = sender as WebBrowser;
            //if (browser == null || browser.Document == null)
            //    return;
            dynamic document = Browser.Document;
            if (document.readyState != "complete")
                return;
            dynamic script = document.createElement("script");
            script.type = @"text/javascript";
            script.text = @"window.onerror = function(msg,url,line){return true;}";
            document.head.appendChild(script);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
        }
        public void timer_Tick(object sender, System.EventArgs e)
        {
            mContainer = geturl.Get_XML_Dic();

            for (int i = 0; i < mContainer.Count; i++)
            {
                Notification noti = new Notification();
                XmlNodeList xnList = mContainer[i];

                xnList = mContainer[i];
                string strUid = xnList[0].Attributes["unid"].Value;

                title = xnList[0].SelectNodes("entrydata")[1].InnerText;
                if (mTmpContainer != null)
                {
                    XmlNodeList tmpXnList = mTmpContainer[i];
                    tmpTitle = tmpXnList[0].SelectNodes("entrydata")[1].InnerText;
                }
                //�񱳰��� �������� ������ ������ �ʴ´�
                if (title != tmpTitle && tmpTitle != "")
                {
                    noti.run(title, strUid);
                }
            }
            //���簪�� �ӽ������Ѵ�
            mTmpContainer = mContainer;
        }
    }
    class Notification
    {
        // ȣ���ϴ� ������
        public static string domain = Properties.Resources.domain;
        GetUrl geturl = new GetUrl();
        NotifyIcon notifyIcon;
        string strUid = "";
        string bbsUrl = "";
        public void run(string title,string uid)
        {
            string getPathUrl = domain + "/gw/app/bult/bbslink.nsf/agGetOriDocInfo?openagent&unid=" + uid;

            XmlNodeList xnList = null;
            XmlDocument xml = geturl.Get_HttpRequest(getPathUrl);
            xnList = xml.SelectNodes("/Root"); //������ ���
            if(xnList[0].SelectNodes("Error") == null)
            {
                bbsUrl = domain + "/" + xnList[0].SelectNodes("oriDBPath")[0].InnerText + "/0/" + xnList[0].SelectNodes("oriUNID")[0].InnerText + "/Body?OpenField";
            } else
            {
                bbsUrl = domain + "/gw/app/bult/bbsbug.nsf/0/" + uid + "/Body?OpenField";
            }


            //�˸�����
            Stream iconStream = MainWindow.myAssembly.GetManifestResourceStream("NotificationIcon.NotifyIcon.ico");
            Icon normal = new Icon(iconStream);
            strUid = uid;
            this.notifyIcon = new NotifyIcon();
            this.notifyIcon.BalloonTipText = title;
            //��Ƽ������ 64���� ������ �ִ�
            this.notifyIcon.Text = title.Length > 60 ? title.Substring(0,60) + ".." : title;
            //this.notifyIcon.Icon = new System.Drawing.Icon("NotifyIcon.ico");
            this.notifyIcon.Icon = normal;
            this.notifyIcon.Visible = true;
            this.notifyIcon.ShowBalloonTip(2000);
            this.notifyIcon.BalloonTipClicked += new EventHandler(NotifyIcon1_BalloonTipClicked);
            this.notifyIcon.Click += new EventHandler(NotifyIcon1_BalloonTipClicked);
            geturl.Get_Request(title, uid, bbsUrl);

        }
        private void NotifyIcon1_BalloonTipClicked(Object sender, EventArgs e)
        {
            //Ŭ���� ��ũ�̵��� ��Ƽ ����� (���� Ÿ�����͹��� �ΰ� ���ϱ�.. �����)
            Process.Start(bbsUrl);
            this.notifyIcon.Dispose();
        }
    }
    class GetUrl
    {
        // ȣ���ϴ� ������
        public static string domain = Properties.Resources.domain;
        // ���� hook URL
        public static string slackhook = Properties.Resources.slackhook;

        public Dictionary<int, XmlNodeList> Get_XML_Dic()
        {
            Dictionary <int, XmlNodeList> mContainer = new Dictionary<int, XmlNodeList>();
            string[] aUrl = {
                //��������
                domain + "/gw/app/bult/bbslink.nsf/wViwPortalCategory?readviewentries&restricttocategory=10&count=10",
                //���̴���
                domain + "/gw/app/bult/bbs00000.nsf/wViwPortalCategory?readviewentries&restricttocategory=00004_02^01&count=10",
                //EVENT4U
                domain + "/gw/app/bult/bbs00000.nsf/wViwPortalNotice?readviewentries&restricttocategory=03&count=10",
                //�����Խ���
                domain + "/gw/app/bult/bbslink.nsf/wViwPortalCategory?readviewentries&restricttocategory=11&count=10",
                //��������
                domain + "/gw/app/bult/bbslink.nsf/wviwportalnotice?ReadViewEntries&restricttocategory=01&start=1&count=10&page=1&_=1495505848745",
                //�ý�����Ŵ��
                domain + "/gw/app/bult/bbsbug.nsf/wviwportalcategory?ReadViewEntries&restricttocategory=all&start=1&count=10&page=1&_=1495503816012"
            };
            for (int i = 0; i < aUrl.Length; i++)
            {
                XmlNodeList xnList = null;
                XmlDocument xml = Get_HttpRequest(aUrl[i]);
                xnList = xml.SelectNodes(" / viewentries/viewentry"); //������ ���
                mContainer.Add(i, xnList);
            }
            return mContainer;
        }
        public void Get_Request(string title, string uid, string bbsUrl)
        {
            string[] aUser = {
                "@ineplu",
                "@chunguhn.kim",
                "@leehk",
                "@unusedid",
                "@yeseul"
            };
            for (int i = 0; i < aUser.Length; i++)
            {
                WebRequest request = WebRequest.Create(slackhook);
                request.Credentials = CredentialCache.DefaultCredentials;
                request.Method = "POST";
                request.ContentType = "application/json; charset=UTF-8";
                string json = "{\"channel\":\"" + aUser[i] + "\",\"icon_emoji\":\":mega:\",\"username\":\"���ž˸���\",\"text\":\"<" + bbsUrl + "|" + title + ">\"}";
                byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(json);
                request.ContentLength = postBytes.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(postBytes, 0, postBytes.Length);
                requestStream.Close();
                //request.Abort();
            }
        }
        public XmlDocument Get_HttpRequest(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(request.RequestUri, new System.Net.Cookie("LtpaToken", MainWindow.token));
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Get the stream associated with the response.
            Stream receiveStream = response.GetResponseStream();
            // Pipes the stream to a higher level stream reader with the required encoding format. 
            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
            //System.Windows.MessageBox.Show(readStream.ReadToEnd());

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(readStream.ReadToEnd());

            response.Close();
            readStream.Close();

            return xml;
        }
    }
}