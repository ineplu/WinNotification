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
        //아이콘리소스를 가져다 쓰기위해 선언
        public static Assembly myAssembly = Assembly.GetAssembly(typeof(MainWindow));
        public NotifyIcon notify;

        GetUrl geturl = new GetUrl();
        //XML을 가져와 사전에 담는다
        Dictionary<int, XmlNodeList> mContainer = null;
        Dictionary<int, XmlNodeList> mTmpContainer = null;

        string title = "";
        string tmpTitle = "";
        string getID = "";
        string getPW = "";
        public static string token = "";
        // 호출하는 도메인
        public static string domain = Properties.Resources.domain;

        public MainWindow(string inputID, string inputPW)
        {
            //프로세스체크를 하고 중복실행을 방지한다
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
            //종료 메뉴추가
            MenuItem mItem = new MenuItem();
            menu.MenuItems.Add(mItem);
            mItem.Text = "종료";
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
            ni.Text = "인팍알리미 베타";
            ni.ContextMenu = menu;

            Timer timer = new Timer();
            timer.Interval = 300000; //주기 설정 5분
            timer.Tick += new EventHandler(timer_Tick); //주기마다 실행되는 이벤트 등록
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

            // 스크립트오류 무시하기
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
                //비교값이 존재하지 않으면 보내지 않는다
                if (title != tmpTitle && tmpTitle != "")
                {
                    noti.run(title, strUid);
                }
            }
            //현재값을 임시저장한다
            mTmpContainer = mContainer;
        }
    }
    class Notification
    {
        // 호출하는 도메인
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
            xnList = xml.SelectNodes("/Root"); //접근할 노드
            if(xnList[0].SelectNodes("Error") == null)
            {
                bbsUrl = domain + "/" + xnList[0].SelectNodes("oriDBPath")[0].InnerText + "/0/" + xnList[0].SelectNodes("oriUNID")[0].InnerText + "/Body?OpenField";
            } else
            {
                bbsUrl = domain + "/gw/app/bult/bbsbug.nsf/0/" + uid + "/Body?OpenField";
            }


            //알림생성
            Stream iconStream = MainWindow.myAssembly.GetManifestResourceStream("NotificationIcon.NotifyIcon.ico");
            Icon normal = new Icon(iconStream);
            strUid = uid;
            this.notifyIcon = new NotifyIcon();
            this.notifyIcon.BalloonTipText = title;
            //노티문구가 64글자 제한이 있다
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
            //클릭시 링크이동후 노티 사라짐 (추후 타임인터벌을 두고 죽일까.. 고민중)
            Process.Start(bbsUrl);
            this.notifyIcon.Dispose();
        }
    }
    class GetUrl
    {
        // 호출하는 도메인
        public static string domain = Properties.Resources.domain;
        // 슬랙 hook URL
        public static string slackhook = Properties.Resources.slackhook;

        public Dictionary<int, XmlNodeList> Get_XML_Dic()
        {
            Dictionary <int, XmlNodeList> mContainer = new Dictionary<int, XmlNodeList>();
            string[] aUrl = {
                //업무공유
                domain + "/gw/app/bult/bbslink.nsf/wViwPortalCategory?readviewentries&restricttocategory=10&count=10",
                //아이누리
                domain + "/gw/app/bult/bbs00000.nsf/wViwPortalCategory?readviewentries&restricttocategory=00004_02^01&count=10",
                //EVENT4U
                domain + "/gw/app/bult/bbs00000.nsf/wViwPortalNotice?readviewentries&restricttocategory=03&count=10",
                //자유게시판
                domain + "/gw/app/bult/bbslink.nsf/wViwPortalCategory?readviewentries&restricttocategory=11&count=10",
                //공지사항
                domain + "/gw/app/bult/bbslink.nsf/wviwportalnotice?ReadViewEntries&restricttocategory=01&start=1&count=10&page=1&_=1495505848745",
                //시스템지킴이
                domain + "/gw/app/bult/bbsbug.nsf/wviwportalcategory?ReadViewEntries&restricttocategory=all&start=1&count=10&page=1&_=1495503816012"
            };
            for (int i = 0; i < aUrl.Length; i++)
            {
                XmlNodeList xnList = null;
                XmlDocument xml = Get_HttpRequest(aUrl[i]);
                xnList = xml.SelectNodes(" / viewentries/viewentry"); //접근할 노드
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
                string json = "{\"channel\":\"" + aUser[i] + "\",\"icon_emoji\":\":mega:\",\"username\":\"인팍알리미\",\"text\":\"<" + bbsUrl + "|" + title + ">\"}";
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