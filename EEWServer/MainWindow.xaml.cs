using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using static EEWServer.Response;

namespace EEWServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            Start();

        }

        public async void Start()
        {
            var sb = FindResource("StartUp") as Storyboard;
            sb.Begin();
            await Task.Delay(4000);
            Status.Text = "設定読み込み";
            NIED_Access = Properties.Settings.Default.NIED;
            iedred_Access = Properties.Settings.Default.iedred;
            Yahoo_Access = Properties.Settings.Default.Yahoo;
            Status.Text = "JSON取得開始";
            Clocktimer = CreateClockTimer();
            Clocktimer.Start();
            await Task.Delay(2000);
            progressBar.Value = 50;
            Status.Text = "Httpサーバーを起動中";
            await Task.Delay(900);
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                this.Close();//管理者権限がない場合
            }
            _ = Task.Run(() => Server());
            await Task.Delay(300);
            progressBar.Value = 100;
            sb = FindResource("Main") as Storyboard;
            sb.Begin();

        }

        string? time;
        string? time2;
        string eew_JSON;
        bool Yahoo_EEW = false;
        bool iedred_EEW = false;
        DateTime dtTime = new DateTime(2021, 02, 13, 23, 08, 02);
        int Count = 301;
        int JSON_Count = 0;
        int NIED_Access = 1;
        int iedred_Access = 1;
        int Yahoo_Access = 1;
        private DispatcherTimer Clocktimer;
        private DispatcherTimer CreateClockTimer()
        {
            // タイマー生成
            var t = new DispatcherTimer(DispatcherPriority.Normal);

            // タイマーイベントの発生間隔を1秒に設定
            t.Interval = TimeSpan.FromSeconds(1);

            // タイマーイベントの定義
            t.Tick += async (sender, e) =>
            {
                await Task.Run(() =>
                {
                    Count++;
                    if (Count > 300)
                    {
                        Count = 0;
                        try
                        {
                            System.Net.Sockets.UdpClient objSck;
                            System.Net.IPEndPoint ipAny =
                                new System.Net.IPEndPoint(
                                System.Net.IPAddress.Any, 0);
                            objSck = new System.Net.Sockets.UdpClient(ipAny);

                            // UDP送信
                            Byte[] sdat = new Byte[48];
                            sdat[0] = 0xB;
                            objSck.Send(sdat, sdat.GetLength(0),
                                "time.windows.com", 123);

                            // UDP受信
                            Byte[] rdat = objSck.Receive(ref ipAny);

                            // 1900年1月1日からの経過時間(日時分秒)
                            long lngAllS; // 1900年1月1日からの経過秒数
                            long lngD;    // 日
                            long lngH;    // 時
                            long lngM;    // 分
                            long lngS;    // 秒

                            // 1900年1月1日からの経過秒数
                            lngAllS = (long)(
                                      rdat[40] * Math.Pow(2, (8 * 3)) +
                                      rdat[41] * Math.Pow(2, (8 * 2)) +
                                      rdat[42] * Math.Pow(2, (8 * 1)) +
                                      rdat[43]);

                            lngD = lngAllS / (24 * 60 * 60); // 日
                            lngS = lngAllS % (24 * 60 * 60); // 残りの秒数
                            lngH = lngS / (60 * 60);         // 時
                            lngS = lngS % (60 * 60);         // 残りの秒数
                            lngM = lngS / 60;                // 分
                            lngS = lngS % 60;                // 秒

                            // DateTime型への変換
                            dtTime = new DateTime(1900, 1, 1);
                            dtTime = dtTime.AddDays(lngD);
                            dtTime = dtTime.AddHours(lngH);
                            dtTime = dtTime.AddMinutes(lngM);
                            dtTime = dtTime.AddSeconds(lngS);
                            // グリニッジ標準時から日本時間への変更
                            dtTime = dtTime.AddHours(9).AddSeconds(-1);
                            time = dtTime.ToString("yyyyMMddHHmmss");
                            time2 = dtTime.ToString("yyyyMMdd");
                        }
                        catch
                        {

                        }
                    }
                    else
                    {
                        dtTime = dtTime.AddSeconds(1);
                        time = dtTime.ToString("yyyyMMddHHmmss");
                        time2 = dtTime.ToString("yyyyMMdd");
                    }
                    this.Dispatcher.Invoke((Action)(async () =>
                    {
                        Clock_text.Text = time;
                        JSON_Count++;
                        if (JSON != null && JSON.items != null)
                        {
                            //時間が過ぎた緊急地震速報を削除
                            var i = 0;
                            if (JSON_Count % iedred_Access == 0) { await Task.Run(() => Iedred()); }
                            while (i != JSON.items.Count)//時間が過ぎたやつを探す
                            {
                                var a = dtTime - JSON.items[i].reportTime;
                                if (a.TotalSeconds > 210)
                                {
                                    JSON.items.RemoveAt(i);
                                    i--;
                                }
                                i++;
                            }
                            if (JSON.items.Count == 0)
                            {
                                JSON = new HypoInfo();//からのJSONを作成
                            }
                            eew_JSON = JsonConvert.SerializeObject(JSON);//JSON生成
                        }
                        if (JSON_Count % NIED_Access == 0) { await Task.Run(() => NIED()); }
                        if (JSON_Count % Yahoo_Access == 0) { await Task.Run(() => Yahoo()); }
                        if (JSON != null && JSON.items != null)
                        {
                            //時間が過ぎた緊急地震速報を削除
                            var i = 0;
                            while (i != JSON.items.Count)//時間が過ぎたやつを探す
                            {
                                var a = dtTime - JSON.items[i].reportTime;
                                if (a.TotalSeconds > 210)
                                {
                                    JSON.items.RemoveAt(i);
                                    i--;
                                }
                                i++;
                            }
                            if (JSON.items.Count == 0)
                            {
                                JSON = new HypoInfo();//からのJSONを作成
                            }
                            eew_JSON = JsonConvert.SerializeObject(JSON);//JSON生成
                        }
                        Access.Text = $"{Access_Count.ToString()}req/s";
                        Access_Count = 0;
                    }));
                });
            };

            // 生成したタイマーを返す
            return t;
        }

        int Access_Count = 0;

        public async void Server()
        {
            // HTTPリスナー作成
            HttpListener listener = new HttpListener();

            // リスナー設定
            listener.Prefixes.Clear();
            listener.Prefixes.Add(@"http://+:23057/");


            // リスナー開始
            listener.Start();
            int log_Count = 1;
            string log = "起動しました。\n";
            this.Dispatcher.Invoke((Action)(() =>
            {
                TextBoxLog.Text = log;
            }));

            while (true)
            {
                // リクエスト取得
                HttpListenerContext context = await listener.GetContextAsync();
                Task task = new Task(async() =>
                {
                    HttpListenerRequest request = context.Request;
                    // レスポンス取得
                    HttpListenerResponse response = context.Response;
                    response.Headers.Clear();
                    response.ContentEncoding = Encoding.UTF8;
                    response.ContentType = "application/json";
                    log_Count++;
                    if (log_Count > 200)
                    {
                        log_Count = 0;
                        log = "";
                    }
                    log += $"RawUrl:{request.RawUrl}\n";
                    log += $"UserHostAdress:{request.UserHostAddress}\n";
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        TextBoxLog.Text = log;
                    }));
                    // JSONを返す
                    try
                    {
                        if (request != null)
                        {
                            if (request.UserHostAddress == "127.0.0.1:23057")
                            {
                                Access_Count += 1;
                                if (request.RawUrl == "/NIED" || request.RawUrl == "/NIED/")
                                {
                                    byte[] text = Encoding.UTF8.GetBytes($"{NIED_JSON}");
                                    response.OutputStream.Write(text, 0, text.Length);
                                }
                                else if (request.RawUrl == "/iedred" || request.RawUrl == "/iedred/")
                                {
                                    byte[] text = Encoding.UTF8.GetBytes($"{iedred_src}");
                                    response.OutputStream.Write(text, 0, text.Length);
                                }
                                else
                                {
                                    byte[] text = Encoding.UTF8.GetBytes($"{eew_JSON}");
                                    response.OutputStream.Write(text, 0, text.Length);
                                }
                            }
                            else
                            {
                                response.StatusCode = 403;
                                log += $"接続を拒否しました。\n";
                                this.Dispatcher.Invoke((Action)(() =>
                                {
                                    TextBoxLog.Text = log;
                                }));
                            }
                        }
                        else
                        {
                            response.StatusCode = 404;
                        }
                    }
                    catch (Exception ex)
                    {
                        log += $"レスポンスでエラーが発生{ex}\n";
                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            TextBoxLog.Text = log;
                        }));
                    }
                    response.Close();
                });
                task.Start();

            }
        }

        HypoInfo JSON = new HypoInfo();
        string? iedred_src = null;
        string? iedred_src_old = null;
        public Task Iedred()
        {
            Dispatcher.Invoke((Action)(async () =>
            {
                iedred_src_old = iedred_src;
                iedred.Root? eew = null;
                try
                {
                    var handler = new HttpClientHandler()
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    };
                    using (var req = new HttpClient(handler))
                    using (var res = await req.GetAsync($"https://api.iedred7584.com/eew/json/"))
                    {
                        if (res.IsSuccessStatusCode)
                        {
                            iedred_src = await res.Content.ReadAsStringAsync();
                            eew = JsonConvert.DeserializeObject<iedred.Root>(iedred_src);
                            iedred_Status.Text = "iedred API: OK";
                        }
                        else
                        {
                            eew = null;
                            iedred_Status.Text = "iedred API: NG";
                        }

                    }
                }
                catch (Exception ex)
                {
                    iedred_Status.Text = "iedred API: NG";
                }
                if (eew == null)
                {
                    iedred_Status.Text = "iedred API: NG";
                }
                if (eew != null && iedred_src_old != iedred_src)
                {
                    if (eew.ParseStatus == "Success")//APIでエラーが発生していないとき
                    {
                        if (eew.Title.String == "緊急地震速報（予報）" || eew.Title.Detail == "緊急地震速報（警報）")//多分訓練報以外のとき
                        {
                            if (eew.Title.Detail == "キャンセル（取り消し）情報")//キャンセルされたとき
                            {
                                if (JSON.items == null)//中身がからのとき
                                {
                                    //何か入れよっかな
                                }
                                else
                                {
                                    var i = 0;
                                    while (i != JSON.items.Count)//reportIdを捜索
                                    {
                                        if (eew.EventID.ToString() == JSON.items[i].reportId)//受信したJSONの中に該当するreportIdが有った場合
                                        {
                                            if (eew.Serial > int.Parse(JSON.items[i].reportNum))//該当のものに入れる
                                            {
                                                JSON.items[i].reportTime = DateTime.ParseExact(eew.AnnouncedTime.String, "yyyy/MM/dd HH:mm:ss", null);
                                                JSON.items[i].isCancel = "true";
                                                JSON.items[i].originTime = DateTime.ParseExact(eew.OriginTime.String, "yyyy/MM/dd HH:mm:ss", null);
                                                JSON.items[i].reportNum = eew.Serial.ToString();
                                                JSON.items[i].reportId = eew.EventID.ToString();
                                            }
                                            break;
                                        }
                                        i++;
                                    }
                                }
                            }
                            else//それ以外
                            {
                                if (JSON.items == null)//中身がからのとき
                                {

                                    JSON = new HypoInfo();
                                    JSON.items = new List<Item>();
                                    JSON.items.Add(new Item() { });
                                    var i = 0;
                                    JSON.items[i] = new Item();
                                    JSON.items[i].latitude = $"N{eew.Hypocenter.Location.Lat.ToString()}";
                                    JSON.items[i].longitude = $"E{eew.Hypocenter.Location.Long.ToString()}";
                                    JSON.items[i].reportTime = DateTime.ParseExact(eew.AnnouncedTime.String, "yyyy/MM/dd HH:mm:ss", null);
                                    JSON.items[i].regionCode = eew.Hypocenter.Code.ToString();
                                    JSON.items[i].regionName = eew.Hypocenter.Name;
                                    JSON.items[i].isCancel = "false";
                                    JSON.items[i].depth = eew.Hypocenter.Location.Depth.String.Replace("約", "");
                                    JSON.items[i].calcintensity = eew.MaxIntensity.To.Replace("0", "");
                                    if (eew.Type.Detail.Contains("最終の緊急地震速報"))
                                    {
                                        JSON.items[i].isFinal = "true";
                                    }
                                    else
                                    {
                                        JSON.items[i].isFinal = "false";
                                    }
                                    JSON.items[i].originTime = DateTime.ParseExact(eew.OriginTime.String, "yyyy/MM/dd HH:mm:ss", null);
                                    JSON.items[i].reportNum = eew.Serial.ToString();
                                    JSON.items[i].reportId = eew.EventID.ToString();
                                    JSON.items[i].magnitude = eew.Hypocenter.Magnitude.Float.ToString();
                                    if (!JSON.items[i].magnitude.Contains("."))
                                    {
                                        JSON.items[i].magnitude += ".0";
                                    }
                                    JSON.items[i].isWarning = eew.Warn.ToString();
                                }
                                else//中身がすでにある時
                                {
                                    var i = 0;
                                    bool find = false;
                                    while (i != JSON.items.Count)//reportIdを捜索
                                    {
                                        if (eew.EventID.ToString() == JSON.items[i].reportId)//受信したJSONの中に該当するreportIdが有った場合
                                        {
                                            if (eew.Serial > int.Parse(JSON.items[i].reportNum))//該当のものに入れる
                                            {
                                                JSON.items[i].latitude = $"N{eew.Hypocenter.Location.Lat.ToString()}";
                                                JSON.items[i].longitude = $"E{eew.Hypocenter.Location.Long.ToString()}";
                                                JSON.items[i].reportTime = DateTime.ParseExact(eew.AnnouncedTime.String, "yyyy/MM/dd HH:mm:ss", null);
                                                JSON.items[i].regionCode = eew.Hypocenter.Code.ToString();
                                                JSON.items[i].regionName = eew.Hypocenter.Name;
                                                JSON.items[i].isCancel = "false";
                                                JSON.items[i].depth = eew.Hypocenter.Location.Depth.String.Replace("約", "");
                                                JSON.items[i].calcintensity = eew.MaxIntensity.To.Replace("0", "");
                                                if (eew.Type.Detail.Contains("最終の緊急地震速報"))
                                                {
                                                    JSON.items[i].isFinal = "true";
                                                }
                                                else
                                                {
                                                    JSON.items[i].isFinal = "false";
                                                }
                                                JSON.items[i].originTime = DateTime.ParseExact(eew.OriginTime.String, "yyyy/MM/dd HH:mm:ss", null);
                                                JSON.items[i].reportNum = eew.Serial.ToString();
                                                JSON.items[i].reportId = eew.EventID.ToString();
                                                JSON.items[i].magnitude = eew.Hypocenter.Magnitude.Float.ToString();
                                                if (!JSON.items[i].magnitude.Contains("."))
                                                {
                                                    JSON.items[i].magnitude += ".0";
                                                }
                                                JSON.items[i].isWarning = eew.Warn.ToString();
                                            }

                                            find = true;
                                            break;
                                        }
                                        i++;
                                    }
                                    if (!find)
                                    {
                                        i = 0;
                                        var JSON_count = JSON.items.Count;

                                        i = 0;
                                        JSON.items.Add(new Item() { });
                                        //ここから内容を入れていく
                                        i = JSON_count;
                                        JSON.items[i] = new Item();
                                        JSON.items[i].latitude = $"N{eew.Hypocenter.Location.Lat.ToString()}";
                                        JSON.items[i].longitude = $"E{eew.Hypocenter.Location.Long.ToString()}";
                                        JSON.items[i].reportTime = DateTime.ParseExact(eew.AnnouncedTime.String, "yyyy/MM/dd HH:mm:ss", null);
                                        JSON.items[i].regionCode = eew.Hypocenter.Code.ToString();
                                        JSON.items[i].regionName = eew.Hypocenter.Name;
                                        JSON.items[i].isCancel = "false";
                                        JSON.items[i].depth = eew.Hypocenter.Location.Depth.String.Replace("約", "");
                                        JSON.items[i].calcintensity = eew.MaxIntensity.To.Replace("0", "");
                                        if (eew.Type.Detail.Contains("最終の緊急地震速報"))
                                        {
                                            JSON.items[i].isFinal = "true";
                                        }
                                        else
                                        {
                                            JSON.items[i].isFinal = "false";
                                        }
                                        JSON.items[i].originTime = DateTime.ParseExact(eew.OriginTime.String, "yyyy/MM/dd HH:mm:ss", null);
                                        JSON.items[i].reportNum = eew.Serial.ToString();
                                        JSON.items[i].reportId = eew.EventID.ToString();
                                        JSON.items[i].magnitude = eew.Hypocenter.Magnitude.Float.ToString();
                                        if (!JSON.items[i].magnitude.Contains("."))
                                        {
                                            JSON.items[i].magnitude += ".0";
                                        }
                                        JSON.items[i].isWarning = eew.Warn.ToString();
                                    }
                                }
                            }
                        }
                    }
                }
            }));
            return Task.CompletedTask;
        }

        string? src = null;
        public Task Yahoo()
        {
            Dispatcher.Invoke((Action)(async () =>
            {
                //EEW取得
                Yahoo.Rootobject? eew = null;

                try
                {
                    var handler = new HttpClientHandler()
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    };
                    using (var req = new HttpClient(handler))
                    using (var res = await req.GetAsync($"https://weather-kyoshin.east.edge.storage-yahoo.jp/RealTimeData/{time2}/{time}.json"))
                    {
                        if (res.IsSuccessStatusCode)
                        {
                            src = await res.Content.ReadAsStringAsync();
                            eew = JsonConvert.DeserializeObject<Yahoo.Rootobject>(src);
                            Yahoo_Status.Text = "Yahoo API: OK";
                        }
                        else
                        {
                            eew = null;
                        }

                    }
                }
                catch (Exception ex)
                {

                }
                if (eew == null)
                {
                    await Task.Delay(300);
                    try
                    {
                        var handler = new HttpClientHandler()
                        {
                            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                        };
                        using (var req = new HttpClient(handler))
                        using (var res = await req.GetAsync($"https://weather-kyoshin.west.edge.storage-yahoo.jp/RealTimeData/{time2}/{time}.json"))
                        {
                            if (res.IsSuccessStatusCode)
                            {
                                src = await res.Content.ReadAsStringAsync();
                                eew = JsonConvert.DeserializeObject<Yahoo.Rootobject>(src);
                                Yahoo_Status.Text = "Yahoo API: OK";
                            }
                            else
                            {
                                eew = null;
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                    }


                }

                if (eew == null)
                {
                    Yahoo_Status.Text = "Yahoo API: NG";
                }
                if (eew != null)
                {
                    if (eew.hypoInfo == null)
                    {
                        Yahoo_EEW = false;//Yahooの緊急地震速報が発表されていないとき

                    }
                    else
                    {
                        Yahoo_EEW = true;
                        if (JSON.items == null)//中身がからのとき
                        {
                            JSON = new HypoInfo();
                            JSON.items = new List<Item>();
                            JSON.items.Add(new Item() { });
                            var i = 0;
                            JSON.items[i] = new Item();
                            JSON.items[i].latitude = eew.hypoInfo.items[0].longitude;
                            JSON.items[i].longitude = eew.hypoInfo.items[0].longitude;
                            JSON.items[i].reportTime = eew.hypoInfo.items[0].reportTime;
                            JSON.items[i].regionCode = eew.hypoInfo.items[0].regionCode;
                            JSON.items[i].regionName = eew.hypoInfo.items[0].regionName;
                            JSON.items[i].isCancel = eew.hypoInfo.items[0].isCancel;
                            JSON.items[i].depth = eew.hypoInfo.items[0].depth;
                            JSON.items[i].calcintensity = eew.hypoInfo.items[0].calcintensity.Replace("0", "");
                            JSON.items[i].isFinal = eew.hypoInfo.items[0].isFinal;
                            JSON.items[i].originTime = eew.hypoInfo.items[0].originTime;
                            JSON.items[i].reportNum = eew.hypoInfo.items[0].reportNum;
                            JSON.items[i].reportId = eew.hypoInfo.items[0].reportId;
                            JSON.items[i].magnitude = eew.hypoInfo.items[0].magnitude;
                            if (!JSON.items[i].magnitude.Contains("."))
                            {
                                JSON.items[i].magnitude += ".0";
                            }
                            JSON.items[i].isWarning = "false";
                            if (eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "5-" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "5+" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "6-" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "6+" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "7")
                            {
                                JSON.items[i].isWarning = "true";
                            }
                        }
                        else//中身がすでにある時
                        {
                            var i = 0;
                            bool find = false;
                            while (i != JSON.items.Count)//reportIdを捜索
                            {
                                if (eew.hypoInfo.items[0].reportId == JSON.items[i].reportId)//受信したJSONの中に該当するreportIdが有った場合
                                {
                                    if (int.Parse(eew.hypoInfo.items[0].reportNum) > int.Parse(JSON.items[i].reportNum))//該当のものに入れる
                                    {
                                        JSON.items[i].latitude = eew.hypoInfo.items[0].latitude;
                                        JSON.items[i].longitude = eew.hypoInfo.items[0].longitude;
                                        JSON.items[i].reportTime = eew.hypoInfo.items[0].reportTime;
                                        JSON.items[i].regionCode = eew.hypoInfo.items[0].regionCode;
                                        JSON.items[i].regionName = eew.hypoInfo.items[0].regionName;
                                        JSON.items[i].isCancel = eew.hypoInfo.items[0].isCancel;
                                        JSON.items[i].depth = eew.hypoInfo.items[0].depth;
                                        JSON.items[i].calcintensity = eew.hypoInfo.items[0].calcintensity.Replace("0", "");
                                        JSON.items[i].isFinal = eew.hypoInfo.items[0].isFinal;
                                        JSON.items[i].originTime = eew.hypoInfo.items[0].originTime;
                                        JSON.items[i].reportNum = eew.hypoInfo.items[0].reportNum;
                                        JSON.items[i].reportId = eew.hypoInfo.items[0].reportId;
                                        JSON.items[i].magnitude = eew.hypoInfo.items[0].magnitude;
                                        if (!JSON.items[i].magnitude.Contains("."))
                                        {
                                            JSON.items[i].magnitude += ".0";
                                        }
                                        if (eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "5-" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "5+" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "6-" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "6+" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "7")
                                        {
                                            JSON.items[i].isWarning = "true";
                                        }
                                    }

                                    find = true;
                                    break;
                                }
                                i++;
                            }
                            if (!find)
                            {
                                i = 0;
                                var JSON_count = JSON.items.Count;

                                i = 0;
                                JSON.items.Add(new Item() { });
                                //ここから内容を入れていく
                                i = JSON_count;
                                JSON.items[i] = new Item();
                                JSON.items[i].latitude = eew.hypoInfo.items[0].latitude;
                                JSON.items[i].longitude = eew.hypoInfo.items[0].longitude;
                                JSON.items[i].reportTime = eew.hypoInfo.items[0].reportTime;
                                JSON.items[i].regionCode = eew.hypoInfo.items[0].regionCode;
                                JSON.items[i].regionName = eew.hypoInfo.items[0].regionName;
                                JSON.items[i].isCancel = eew.hypoInfo.items[0].isCancel;
                                JSON.items[i].depth = eew.hypoInfo.items[0].depth;
                                JSON.items[i].calcintensity = eew.hypoInfo.items[0].calcintensity.Replace("0", "");
                                JSON.items[i].isFinal = eew.hypoInfo.items[0].isFinal;
                                JSON.items[i].originTime = eew.hypoInfo.items[0].originTime;
                                JSON.items[i].reportNum = eew.hypoInfo.items[0].reportNum;
                                JSON.items[i].reportId = eew.hypoInfo.items[0].reportId;
                                JSON.items[i].magnitude = eew.hypoInfo.items[0].magnitude;
                                if (!JSON.items[i].magnitude.Contains("."))
                                {
                                    JSON.items[i].magnitude += ".0";
                                }
                                JSON.items[i].isWarning = "false";
                                if (eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "5-" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "5+" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "6-" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "6+" || eew.hypoInfo.items[0].calcintensity.Replace("0", "") == "7")
                                {
                                    JSON.items[i].isWarning = "true";
                                }
                            }

                        }
                    }
                }


            }));
            return Task.CompletedTask;
        }

        string? NIED_JSON = null;
        public async Task NIED()
        {
            try
            {
                var handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                using (var req = new HttpClient(handler))
                using (var res = await req.GetAsync($"http://www.kmoni.bosai.go.jp/webservice/hypo/eew/{time}.json"))
                {
                    if (res.IsSuccessStatusCode)
                    {
                        NIED_JSON = await res.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        NIED_JSON = null;
                    }

                }
            }
            catch (Exception ex)
            {
            }
            Dispatcher.Invoke((Action)(async () =>
            {
                if (NIED_JSON == null)
                {
                    NIED_Status.Text = "NIED API: NG";
                }
                else
                {
                    NIED_Status.Text = "NIED API: OK";
                }
            }));
        }

        private void Settings1_Click(object sender, RoutedEventArgs e)
        {
            NIED_Access = Convert.ToInt32(slider.Value);
            Properties.Settings.Default.NIED = Convert.ToInt32(slider.Value);
            iedred_Access = Convert.ToInt32(slider_iedred.Value);
            Properties.Settings.Default.iedred = Convert.ToInt32(slider_iedred.Value);
            Yahoo_Access = Convert.ToInt32(slider_Yahoo.Value);
            Properties.Settings.Default.Yahoo = Convert.ToInt32(slider_Yahoo.Value);
            Properties.Settings.Default.Save();
        }

        private void Settings_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            slider.Value = NIED_Access;
            slider_iedred.Value = iedred_Access;
            slider_Yahoo.Value = Yahoo_Access;
        }

        private void Settings_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            slider.Value = NIED_Access;
            slider_iedred.Value = iedred_Access;
            slider_Yahoo.Value = Yahoo_Access;
        }
    }



}