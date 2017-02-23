using System;
using System.Threading;
using System.Text;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;

using OPC.Common;
using OPC.Data;
using OPC.Data.Interface;
using System.Net;
using System.Net.Sockets;

namespace UDPServer
{
    public partial class Form1 : Form
    {
        Thread listen;
        Thread connect;

        public Process thisprocess;			// running OS process

        public string selectedOpcSrv;			// Name (ProgID) of selected OPC-server
        public OpcServer theSrv = null;			// root OPCDA object
        public OpcGroup theGrp = null;			// the only one OPC-Group in this example
        public OpcGroup theTargGrp = null;

        public string itmFullID;					// fully qualified OPC namespace path
        public int itmHandleClient;			// 0 if no current item selected
        public int itmHandleServer;
        public OPCACCESSRIGHTS itmAccessRights;
        public TypeCode itmTypeCode;				// saved data type of current item

        public bool first_activated = false;	// workaround to show SelServer Form on applic. start
        public bool opc_connected = false;		// flag if connected

        public string rootname = "Root";			// string of TreeView root (dummy)
        public string selectednode;
        public string selecteditem;				// item in ListView

        // ***********************************************************	EDIT THIS :
        const string serverProgID = "KRUG.OPC.DA.Modbus";		// ProgID of OPC server

        const string itemA = "IndividPLC.Device.Value.TBoiler";						// fully qualified ID of a VT_I4 item
        const string itemB = "IndividPLC.Device.Value.TSupply";						// fully qualified ID of a VT_R8 item

        const string itemC = "IndividPLC.Device.Target.TBoiler";

        private OPCItemDef[] itemDefs = new OPCItemDef[2];
        private int[] handlesSrv = new int[2] { 0, 0 };

        private OPCItemDef[] itemTarget = new OPCItemDef[1];
        private int[] handlesTargetSrv = new int[1] { 0 };

        public void Work()
        {
            theSrv = new OpcServer();
            theSrv.Connect(serverProgID);
            Thread.Sleep(500);				// we are faster then some servers!

            // add our only working group
            theGrp = theSrv.AddGroup("Group", false, 900);
            theTargGrp = theSrv.AddGroup("Target", false, 900);

            // add two items and save server handles
            itemDefs[0] = new OPCItemDef(itemA, true, 1, VarEnum.VT_EMPTY);
            itemDefs[1] = new OPCItemDef(itemB, true, 2, VarEnum.VT_EMPTY);


            //
            itemTarget[0] = new OPCItemDef(itemC, true, 1, VarEnum.VT_EMPTY);

            OPCItemResult[] rItm;

            theGrp.AddItems(itemDefs, out rItm);

            if (rItm == null)
                return;
            
            if (HRESULTS.Failed(rItm[0].Error) || HRESULTS.Failed(rItm[1].Error))
            {
                AddTotextBox("OPC Tester: AddItems - some failed", textBox4);
                theGrp.Remove(true); 
                theSrv.Disconnect(); 
                return; 
            };
            
            handlesSrv[0] = rItm[0].HandleServer;
            handlesSrv[1] = rItm[1].HandleServer;

            OPCItemResult[] rItmTarg;
            theTargGrp.AddItems(itemTarget, out rItmTarg);

            if (rItmTarg == null)
                return;

            if (HRESULTS.Failed(rItmTarg[0].Error))
            {
                AddTotextBox("OPC Tester: AddItems - some failed", textBox4);
                theGrp.Remove(true);
                theSrv.Disconnect();
                return;
            };

            handlesTargetSrv[0] = rItmTarg[0].HandleServer;

            //----------------------------------------------------------------------------------
            // asynch read our two items
            theGrp.SetEnable(true);
            theGrp.Active = true;
            theGrp.DataChanged += new DataChangeEventHandler(this.theGrp_DataChange);
            theGrp.ReadCompleted += new ReadCompleteEventHandler(this.theGrp_ReadComplete);
            int CancelID;
            int[] aE;
            theGrp.Read(handlesSrv, 55667788, out CancelID, out aE);

            // some delay for asynch read-complete callback (simplification)
            Thread.Sleep(500);


            // asynch write
            theTargGrp.SetEnable(true);
            theTargGrp.Active = true;
            object[] itemValues = new object[1];
            itemValues[0] = (int)450;

            theTargGrp.WriteCompleted += new WriteCompleteEventHandler(this.theGrp_WriteComplete);
            theTargGrp.Write(handlesTargetSrv, itemValues, 99887766, out CancelID, out aE);

            // some delay for asynch write-complete callback (simplification)
            Thread.Sleep(500);

            theGrp.DataChanged -= new DataChangeEventHandler(this.theGrp_DataChange);
            theGrp.ReadCompleted -= new ReadCompleteEventHandler(this.theGrp_ReadComplete);
            theGrp.WriteCompleted -= new WriteCompleteEventHandler(this.theGrp_WriteComplete);
        }

        public void PLCRead()
        {
            try
            {
                // asynch read our two items
                theGrp.SetEnable(true);
                theGrp.Active = true;
                theGrp.DataChanged += new DataChangeEventHandler(this.theGrp_DataChange);
                theGrp.ReadCompleted += new ReadCompleteEventHandler(this.theGrp_ReadComplete);
                int CancelID;
                int[] aE;
                Random rnd = new Random();
                int transactionID = rnd.Next(1, 55667788);

                theGrp.Read(handlesSrv, transactionID, out CancelID, out aE);  

                // some delay for asynch read-complete callback (simplification)
                Thread.Sleep(500);

                theGrp.DataChanged -= new DataChangeEventHandler(this.theGrp_DataChange);
                theGrp.ReadCompleted -= new ReadCompleteEventHandler(this.theGrp_ReadComplete);
                //theGrp.WriteCompleted -= new WriteCompleteEventHandler(this.theGrp_WriteComplete);
                //theGrp.RemoveItems(handlesSrv, out aE); //теперь здесь
                //theGrp.Remove(false);

            }
            catch (Exception e )
            {
                SetText("Unexpected Error:" + e.Message);
                return;
            }

        }

        public void PLCWrite()
        {
            try
            {
                
                theSrv = new OpcServer();
                theSrv.Connect(serverProgID);
                Thread.Sleep(500);				// we are faster then some servers!

                //add our only working group
                theGrp = theSrv.AddGroup("Group", false, 900);

                 //add two items and save server handles
                itemDefs[0] = new OPCItemDef(itemA, true, 1, VarEnum.VT_EMPTY);
                itemDefs[1] = new OPCItemDef(itemB, true, 2, VarEnum.VT_EMPTY);

                OPCItemResult[] rItm;

                theGrp.AddItems(itemDefs, out rItm);

                if (rItm == null)
                    return;

                if (HRESULTS.Failed(rItm[0].Error) || HRESULTS.Failed(rItm[1].Error))
                {
                    AddTotextBox("OPC Tester: AddItems - some failed", textBox4);
                    theGrp.Remove(true);
                    theSrv.Disconnect();
                    return;
                };

                if (handlesSrv[0] == 0)
                {
                    handlesSrv[0] = rItm[0].HandleServer;
                    handlesSrv[1] = rItm[1].HandleServer;
                }
                


                // asynch read our two items
                theGrp.SetEnable(true);
                theGrp.Active = true;
                theGrp.DataChanged += new DataChangeEventHandler(this.theGrp_DataChange);
                theGrp.ReadCompleted += new ReadCompleteEventHandler(this.theGrp_ReadComplete);
                int CancelID;
                int[] aE;
                Random rnd = new Random();
                int transactionID = rnd.Next(1, 55667788);

                theGrp.Read(handlesSrv, transactionID, out CancelID, out aE);

                // some delay for asynch read-complete callback (simplification)
                Thread.Sleep(500);

                theGrp.DataChanged -= new DataChangeEventHandler(this.theGrp_DataChange);
                theGrp.ReadCompleted -= new ReadCompleteEventHandler(this.theGrp_ReadComplete);
                //theGrp.WriteCompleted -= new WriteCompleteEventHandler(this.theGrp_WriteComplete);
                //theGrp.RemoveItems(handlesSrv, out aE); //теперь здесь
                //theGrp.Remove(false);

            }
            catch (Exception e)
            {
                SetText("Unexpected Error:" + e.Message);
                return;
            }

        }
        // ------------------------------ events -----------------------------

        public void theGrp_DataChange(object sender, DataChangeEventArgs e)
        {
            //Console.WriteLine("DataChange event: gh={0} id={1} me={2} mq={3}", e.groupHandleClient, e.transactionID, e.masterError, e.masterQuality);
            //AddTotextBox("\r\nDataChange event: gh = " + e.groupHandleClient + " id = " + e.transactionID + " me = " + e.masterError + " mq = " + e.masterQuality, textBox4);
            SetText("\r\nDataChange event: gh = " + e.groupHandleClient + " id = " + e.transactionID + " me = " + e.masterError + " mq = " + e.masterQuality);
            foreach (OPCItemState s in e.sts)
            {
                if (HRESULTS.Succeeded(s.Error))
                    //Console.WriteLine(" ih={0} v={1} q={2} t={3}", s.HandleClient, s.DataValue, s.Quality, s.TimeStamp);
                    //AddTotextBox("\r\n ih = " + s.HandleClient + " v = " + s.DataValue + " q = " + s.Quality + " t = " + s.TimeStamp, textBox4);
                    SetText("\r\n ih = " + s.HandleClient + " v = " + s.DataValue + " q = " + s.Quality + " t = " + s.TimeStamp);
                else
                    //Console.WriteLine(" ih={0}    ERROR=0x{1:x} !", s.HandleClient, s.Error);
                //AddTotextBox("\r\n ih = " + s.HandleClient + " ERROR = 0x" + s.Error, textBox4);
                    SetText("\r\n ih = " + s.HandleClient + " ERROR = 0x" + s.Error);
            }
        }

        public void theGrp_ReadComplete(object sender, ReadCompleteEventArgs e)
        {
            //Console.WriteLine("ReadComplete event: gh={0} id={1} me={2} mq={3}", e.groupHandleClient, e.transactionID, e.masterError, e.masterQuality);
            //AddTotextBox("\r\nReadComplete event: gh = " + e.groupHandleClient + " id = " + e.transactionID + " me = " + e.masterError + " mq = " + e.masterQuality, textBox4);
            SetText("\r\nReadComplete event: gh = " + e.groupHandleClient + " id = " + e.transactionID + " me = " + e.masterError + " mq = " + e.masterQuality);
            foreach (OPCItemState s in e.sts)
            {
                if (HRESULTS.Succeeded(s.Error))
                    //Console.WriteLine(" ih={0} v={1} q={2} t={3}", s.HandleClient, s.DataValue, s.Quality, s.TimeStamp);
                    //AddTotextBox("\r\n ih = " + s.HandleClient + " v = " + s.DataValue + " q = " + s.Quality + " t = " + s.TimeStamp, textBox4);
                    SetText("\r\n ih = " + s.HandleClient + " v = " + s.DataValue + " q = " + s.Quality + " t = " + s.TimeStamp);
                else
                    //Console.WriteLine(" ih={0}    ERROR=0x{1:x} !", s.HandleClient, s.Error);
                    //AddTotextBox("\r\n ih = " + s.HandleClient + " ERROR = 0x" + s.Error, textBox4);
                SetText("\r\n ih = " + s.HandleClient + " ERROR = 0x" + s.Error);
            }
        }

        public void theGrp_WriteComplete(object sender, WriteCompleteEventArgs e)
        {
            //Console.WriteLine("WriteComplete event: gh={0} id={1} me={2}", e.groupHandleClient, e.transactionID, e.masterError);
            //AddTotextBox("\r\nWriteComplete event: gh = " + e.groupHandleClient + " id = " + e.transactionID + " me = " + e.masterError, textBox4);
            SetText("\r\nWriteComplete event: gh = " + e.groupHandleClient + " id = " + e.transactionID + " me = " + e.masterError);
            foreach (OPCWriteResult r in e.res)
            {
                if (HRESULTS.Succeeded(r.Error))
                    //Console.WriteLine(" ih={0} e={1}", r.HandleClient, r.Error);
                    //AddTotextBox("\r\n ih = " + r.HandleClient + " e = " + r.Error, textBox4);
                    SetText("\r\n ih = " + r.HandleClient + " e = " + r.Error);
                else
                    //Console.WriteLine(" ih={0}    ERROR=0x{1:x} !", r.HandleClient, r.Error);
                    //AddTotextBox("\r\n ih = " + r.HandleClient + " ERROR = 0x" + r.Error, textBox4);
                    SetText("\r\n ih = " + r.HandleClient + " ERROR = 0x" + r.Error);
            }
        }




        //---------------------SHAITANAMANA PIZNES----------------------------
        public bool ViewItem(string opcid)
        {
            try
            {
                RemoveItem();		// first remove previous item if any

                itmHandleClient = 1234;
                OPCItemDef[] aD = new OPCItemDef[1];
                aD[0] = new OPCItemDef(opcid, true, itmHandleClient, VarEnum.VT_EMPTY);
                OPCItemResult[] arrRes;
                theGrp.AddItems(aD, out arrRes);
                if (arrRes == null)
                    return false;
                if (arrRes[0].Error != HRESULTS.S_OK)
                    return false;

                //btnItemMore.Enabled = true;
                itmHandleServer = arrRes[0].HandleServer;
                itmAccessRights = arrRes[0].AccessRights;
                itmTypeCode = VT2TypeCode(arrRes[0].CanonicalDataType);

                textBox4.Text = opcid;
                textBox4.Text = DUMMY_VARIANT.VarEnumToString(arrRes[0].CanonicalDataType);

                if ((itmAccessRights & OPCACCESSRIGHTS.OPC_READABLE) != 0)
                {
                    int cancelID;
                    theGrp.Refresh2(OPCDATASOURCE.OPC_DS_DEVICE, 7788, out cancelID);
                }
                else
                    textBox4.Text = "no read access";

                if (itmTypeCode != TypeCode.Object)				// Object=failed!
                {
                    // check if write is premitted
                    if ((itmAccessRights & OPCACCESSRIGHTS.OPC_WRITEABLE) != 0)
                        button9.Enabled = true;
                }
            }
            catch (COMException)
            {
                MessageBox.Show(this, "AddItem OPC error!", "ViewItem", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        // remove previous OPC item if any
        public bool RemoveItem()
        {
            try
            {
                if (itmHandleClient != 0)
                {
                    itmHandleClient = 0;


                    int[] serverhandles = new int[1] { itmHandleServer };
                    int[] remerrors;
                    theGrp.RemoveItems(serverhandles, out remerrors);
                    itmHandleServer = 0;
                }
            }
            catch (COMException)
            {
                MessageBox.Show(this, "RemoveItem OPC error!", "RemoveItem", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        public TypeCode VT2TypeCode(VarEnum vevt)
        {
            switch (vevt)
            {
                case VarEnum.VT_I1:
                    return TypeCode.SByte;
                case VarEnum.VT_I2:
                    return TypeCode.Int16;
                case VarEnum.VT_I4:
                    return TypeCode.Int32;
                case VarEnum.VT_I8:
                    return TypeCode.Int64;

                case VarEnum.VT_UI1:
                    return TypeCode.Byte;
                case VarEnum.VT_UI2:
                    return TypeCode.UInt16;
                case VarEnum.VT_UI4:
                    return TypeCode.UInt32;
                case VarEnum.VT_UI8:
                    return TypeCode.UInt64;

                case VarEnum.VT_R4:
                    return TypeCode.Single;
                case VarEnum.VT_R8:
                    return TypeCode.Double;

                case VarEnum.VT_BSTR:
                    return TypeCode.String;
                case VarEnum.VT_BOOL:
                    return TypeCode.Boolean;
                case VarEnum.VT_DATE:
                    return TypeCode.DateTime;
                case VarEnum.VT_DECIMAL:
                    return TypeCode.Decimal;
                case VarEnum.VT_CY:				// not supported
                    return TypeCode.Double;
            }

            return TypeCode.Object;
        }


























        public Form1()
        {
            InitializeComponent();
            //Work();
            
            connect = new Thread(new ThreadStart(StartingConnectToDevices));
            connect.Start();
            
        }

        

        Requests _request = new Requests();

        IPAddress parsedIP; //Распарсенный с прослушки UDP айпишник
        int ParsedPort; //Распарсенный с прослушки UDP порт

        // Отправка сообщения
        UdpClient udp = new UdpClient();
        bool stopReceive = false;

        bool pingARSIK = false; //Тумблер пинга от арсика получен/не получен
        bool marshruted = false; //Тумблер проложен ли маршрут к АРСиКу
        int requeried = 0; //Счетчик перезапросов к прибору

        //Длина полученного архива
        string length1 = "";
        string length2 = "";

        //------------------------

        //----------Тумблеры исполненых комманд D0, DF и т.д.--------

        bool nullf = false, achtf = false, df = false, readed = false; //тубмлеры прокладывания маршрута и вычитки приборов;
        string[] devices = new string[] { "2018952", "2018957", "2018960", "96554", "96556", "96681" }; //список устройств
        int counter = 0; //переключатель индексов в массиве устройств
        //-----------------------------------------------------------

        //Кнопка Запустить сервер

        
        public void button1_Click(object sender, EventArgs e)
        {
            stopReceive = false;
            listen = new Thread(new ThreadStart(Ping));
            listen.IsBackground = true;
            listen.Start();
        }

        //Кнопка Find Individ
        private void button4_Click(object sender, EventArgs e)
        {
            //Thread rec1 = new Thread(new ThreadStart(FindIndivid));
            //rec1.Start();
            FindARSiK();
        }

        //Кнопка Вычитать память АРС
        private void button5_Click(object sender, EventArgs e)
        {
            //
            //GetData();
            SetARSiK();
        }

        //Кнопка DF
        private void button6_Click(object sender, EventArgs e)
        {

        }

        //Кнопка Read Memory
        private void button7_Click(object sender, EventArgs e)
        {

        }

        //Функция поочередного опроса АРСиКа и всех приборов
        void StartingConnectToDevices()
        {
            // При принудительном завершении работы метода 
            // класса UdpClient Receive() и непредвиденных ситуациях 
            // возможны исключения в этом месте исходного кода,
            // заключим его в блок try чтобы не появлялись окна ошибок.

            try
            {
            // Перед созданием нового объекта закрываем старый
            // для освобождения занятых ресурсов.
                if (udp != null) udp.Close();

                udp = new UdpClient(7007);

                while (true)
                {
                    if (stopReceive)
                    {
                        udp.Close();
                        break;
                    }

                    IPEndPoint ipendpoint = null;

                    byte[] message = udp.Receive(ref ipendpoint); //Получаем из входного потока массив байт

                    //Если пришел первый пинг от арсика, ставим тумблер включенным и показываем это на форме
                    if (message[9] == 0x64 && message[12] == 0xFE && !pingARSIK)
                    {
                        parsedIP = ipendpoint.Address;
                        ParsedPort = ipendpoint.Port;

                        ColorTextBox(Color.YellowGreen, textBox2);

                        SetLabelText(label2, "Online");

                        pingARSIK = true;

                        //stopReceive = true;

                        LayingRoute();
                    }
                    else
                    {
                        if (counter < devices.Length)
                        {
                            if (!marshruted)
                            {
                                //LayingRoute_2();
                                marshruted = true;
                            }

                            if (!readed)
                            {
                                Connector(message, devices[counter]);
                            }
                            else
                            {
                                Thread.Sleep(5000);
                                painter_onliner(counter);
                                counter++;
                                nullf = false;
                                achtf = false;
                                df = false;
                                readed = false;
                                LayingRoute();
                            }
                        }
                        else
                            stopReceive = true;
                    }
                    

                    // Если дана команда остановить поток, останавливаем бесконечный цикл.
                    if (stopReceive == true) break;
                }
            }
            catch (Exception e)
            {
                AddTotextBox(e.Message + "\r\n", textBox4);
            }
        }

        // Функция читающая все пришедшие сообщения в порт, работающая в отдельном потоке.
        void Ping()
        {
            // При принудительном завершении работы метода 
            // класса UdpClient Receive() и непредвиденных ситуациях 
            // возможны исключения в этом месте исходного кода,
            // заключим его в блок try чтобы не появлялись окна ошибок.

            try
            {
                // Перед созданием нового объекта закрываем старый
                // для освобождения занятых ресурсов.
                read:
                if (udp != null) udp.Close();

                udp = new UdpClient(7007);

                while (true)
                {
                    if (stopReceive)
                    {
                        udp.Close();
                        return;
                    }
                    else
                    {
                        IPEndPoint ipendpoint = null;

                        byte[] message = udp.Receive(ref ipendpoint); //Получаем из входного потока массив байт

                        if (message[9] == 0x64 && message[12] == 0xFE)
                        {
                            parsedIP = ipendpoint.Address;
                            ParsedPort = ipendpoint.Port;

                            AddTotextBox("Ping\r\n", textBox4);
                            AddTotextBox("\r\nThis data recieved : " + DateTime.Now, textBox4);
                            AddTotextBox("\r\nThis data recieved from adress: " + ipendpoint.Address.ToString(), textBox4);
                            AddTotextBox("\r\nThis data recieved from port: " + ipendpoint.Port.ToString(), textBox4);
                            AddTotextBox("------------------------------------------------------------------------------------------\r\n\r\n", textBox4);
                            goto read;
                        }
                        else
                        {
                            if (message.Length > 24)
                            {
                                Interpretator(message);
                            }
                        }

                        AddTotextBox("\r\nThis data recieved : " + DateTime.Now, textBox4);
                        AddTotextBox("\r\nThis data recieved from adress: " + ipendpoint.Address.ToString(), textBox4);
                        AddTotextBox("\r\nThis data recieved from port: " + ipendpoint.Port.ToString(), textBox4);
                        AddTotextBox("------------------------------------------------------------------------------------------\r\n\r\n", textBox4);

                        for (int i = 0; i < message.Length; i++)
                        {
                            AddTotextBox(System.Convert.ToString(message[i], 16).ToUpper(), textBox4);
                            AddTotextBox(" ", textBox4);
                        }

                    }
                }
            }
            catch(Exception e)
            {
                AddTotextBox(e.Message + "\r\n", textBox4);
            }
        }

        void Interpretator(byte[] message)
        {
            try
            {
                    //
                    if (message[25] == 0x10 && nullf == false)
                    {
                        nullf = true;
                        LayingRoute_2();
                        return;
                    }

                    //
                    if (message[25] == 0x10 && achtf == false)
                    {
                        achtf = true;
                        ReadData("2018952");
                        return;
                    }
                
                    if (message[26] == 0x5F && df == false)
                    {
                        df = true;
                        //length1 = message[36].ToString();
                        length1 = Convert.ToString(message[36], 16);
                        //length2 = message[37].ToString();
                        length2 = Convert.ToString(message[37], 16);

                        ReadBlock();
                        return;
                    }
                 
            }
            catch (Exception e)
            {
                AddTotextBox(e.Message + "\r\n", textBox4);
            }

            return;
        }

        void Connector(byte[] message, string FullNumber)
        {
            if (message[25] == 0x10 && achtf == false)
            {
                achtf = true;
                ReadData(FullNumber);
                return;
            }


            if (message[26] == 0x5F && df == false)
            {
                df = true;
                length1 = Convert.ToString(message[36], 16);
                length2 = Convert.ToString(message[37], 16);
                ReadBlock();
                readed = true;
                return;
            }

            if (message[26] == 0x5A)
            {
                //readed = true;
                if (requeried < 3)
                {
                    requeried = 1;
                    //Thread.Sleep(5000);
                    painter_errorer(counter);
                    //Connector(message, FullNumber);
                    return;
                }
                else
                {

                }

            }
        }


        void SetARSiK()
        {
            try
            {
                //udp = new UdpClient(7007);

                // Указываем адрес отправки сообщения
                string Host = System.Net.Dns.GetHostName();
                string IP = System.Net.Dns.GetHostByName(Host).AddressList[0].ToString();

                //IPAddress MyIP = IPAddress.Parse(IP);
//                IPAddress MyIP = IPAddress.Parse("193.233.68.24");
                IPAddress MyIP = IPAddress.Parse("192.168.0.108");
                byte[] ipbytes = MyIP.GetAddressBytes();
                string port = "1B5F";

                // Формирование оправляемого сообщения и его отправка.
                // Сеть "понимает" только поток байтов и ей безразличны
                // объекты классов, строки и т.п. Поэтому преобразуем текстовое сообщение в поток байтов.
                IPAddress ip = IPAddress.Parse("192.168.0.115");

                IPEndPoint ipendpoint = new IPEndPoint(ip, 1001);

                byte[] request = _request.SetSettings(ipbytes, port);
                //byte[] request = _request.test();

                int sended = udp.Send(request, request.Length, ipendpoint);

                if (sended == request.Length)
                {
                    AddTotextBox("Data sended succefully\r\n", textBox4);
                }

                // После окончания попытки отправки закрываем UDP соединение,
                // и освобождаем занятые объектом UdpClient ресурсы.
                //udp.Close();
            }
            catch (Exception error)
            {
                AddTotextBox(error.Message + "\r\n", textBox4);
            }
        }

        //Функция формирующая запрос запроса номера АРС
        void FindARS()
        {
            try
            {
                //udp = new UdpClient(7007);

                // Указываем адрес отправки сообщения

                IPAddress ip = parsedIP;
                int port = ParsedPort;

                IPEndPoint ipendpoint = new IPEndPoint(ip, port);

                // Формирование оправляемого сообщения и его отправка.
                // Сеть "понимает" только поток байтов и ей безразличны
                // объекты классов, строки и т.п. Поэтому преобразуем текстовое сообщение в поток байтов.
                byte[] request = _request.FindIndivid();

                int sended = udp.Send(request, request.Length, ipendpoint);

                if (sended == request.Length)
                {
                    AddTotextBox("Data sended succefully\r\n", textBox4);
                }

                // После окончания попытки отправки закрываем UDP соединение,
                // и освобождаем занятые объектом UdpClient ресурсы.
                //udp.Close();
            }
            catch (Exception error)
            {
                AddTotextBox(error.Message + "\r\n", textBox4);
            }
        }

        //Функция формирующая запрос запроса номера АРС
        void FindARSiK()
        {
            try
            {
                //udp = new UdpClient(7007);

                // Указываем адрес отправки сообщения

                IPAddress ip = IPAddress.Parse("193.233.68.18");
                int port = 10001;
                //IPEndPoint ipendpoint = new IPEndPoint(ip, port);

                //IPAddress ipaddress = parsedIP;
                IPEndPoint ipendpoint = new IPEndPoint(IPAddress.Broadcast, 7007);

                // Формирование оправляемого сообщения и его отправка.
                // Сеть "понимает" только поток байтов и ей безразличны
                // объекты классов, строки и т.п. Поэтому преобразуем текстовое сообщение в поток байтов.
                //byte[] request = _request.FindIndivid();
                byte[] request = new byte[] { 0, 0xa5, 0, 0x12, 0, 0, 0, 0, 0, 0, 0, 0, 0xfe, 0, 0, 0, 0, 0, 0, 0, 0x1a, 0xe1 };

                int sended = udp.Send(request, request.Length, ipendpoint);

                if (sended == request.Length)
                {
                    AddTotextBox("Data sended succefully\r\n", textBox4);
                }

                // После окончания попытки отправки закрываем UDP соединение,
                // и освобождаем занятые объектом UdpClient ресурсы.
                //udp.Close();
            }
            catch (Exception error)
            {
                AddTotextBox(error.Message + "\r\n", textBox4);
            }
        }

        void FindIndivid()
        {
            try
            {
                // Формирование оправляемого сообщения и его отправка.
                // Сеть "понимает" только поток байтов и ей безразличны
                // объекты классов, строки и т.п. Поэтому преобразуем текстовое сообщение в поток байтов.

                IPEndPoint ipendpoint = new IPEndPoint(parsedIP, ParsedPort);

                byte[] request = _request.FindIndivid(textBox1.Text);

                int sended = udp.Send(request, request.Length, ipendpoint);

                if (sended == request.Length)
                {
                    AddTotextBox("Data sended succefully\r\n", textBox4);
                }

                // После окончания попытки отправки закрываем UDP соединение,
                // и освобождаем занятые объектом UdpClient ресурсы.
                //udp.Close();
            }
            catch (Exception error)
            {
                AddTotextBox(error.Message + "\r\n", textBox4);
            }
        }

        //Прокладка маршрута
        void LayingRoute()
        {
            try
            {
                // Формирование оправляемого сообщения и его отправка.
                // Сеть "понимает" только поток байтов и ей безразличны
                // объекты классов, строки и т.п. Поэтому преобразуем текстовое сообщение в поток байтов.

                IPEndPoint ipendpoint = new IPEndPoint(parsedIP, ParsedPort);

                byte[] request = _request.D0(DateTime.Now, "F9100", "0F");

                int sended = udp.Send(request, request.Length, ipendpoint);

                if (sended == request.Length)
                {
                    AddTotextBox("Data sended succefully\r\n", textBox4);
                }

                // После окончания попытки отправки закрываем UDP соединение,
                // и освобождаем занятые объектом UdpClient ресурсы.
                //udp.Close();
            }
            catch (Exception error)
            {
                AddTotextBox(error.Message + "\r\n", textBox4);
            }
        }

        //Прокладка маршрута
        void LayingRoute_2()
        {
            try
            {
                // Формирование оправляемого сообщения и его отправка.
                // Сеть "понимает" только поток байтов и ей безразличны
                // объекты классов, строки и т.п. Поэтому преобразуем текстовое сообщение в поток байтов.

                IPEndPoint ipendpoint = new IPEndPoint(parsedIP, ParsedPort);

                byte[] request = _request.D0(DateTime.Now, "F9100", "8F");

                int sended = udp.Send(request, request.Length, ipendpoint);

                if (sended == request.Length)
                {
                    AddTotextBox("Data sended succefully\r\n", textBox4);
                }

                // После окончания попытки отправки закрываем UDP соединение,
                // и освобождаем занятые объектом UdpClient ресурсы.
                //udp.Close();
            }
            catch (Exception error)
            {
                AddTotextBox(error.Message + "\r\n", textBox4);
            }
        }

        void SearchItem()
        {
            try
            {
                // Формирование оправляемого сообщения и его отправка.
                // Сеть "понимает" только поток байтов и ей безразличны
                // объекты классов, строки и т.п. Поэтому преобразуем текстовое сообщение в поток байтов.

                IPEndPoint ipendpoint = new IPEndPoint(parsedIP, ParsedPort);

                byte[] request = _request.D2(DateTime.Now, "F9100", "18960");

                int sended = udp.Send(request, request.Length, ipendpoint);

                if (sended == request.Length)
                {
                    AddTotextBox("Data sended succefully\r\n", textBox4);
                }

                // После окончания попытки отправки закрываем UDP соединение,
                // и освобождаем занятые объектом UdpClient ресурсы.
                //udp.Close();
            }
            catch (Exception error)
            {
                AddTotextBox(error.Message + "\r\n", textBox4);
            }
        }

        void ReadData(string FullNumber)
        {
            try
            {
                // Формирование оправляемого сообщения и его отправка.
                // Сеть "понимает" только поток байтов и ей безразличны
                // объекты классов, строки и т.п. Поэтому преобразуем текстовое сообщение в поток байтов.

                IPEndPoint ipendpoint = new IPEndPoint(parsedIP, ParsedPort);

                byte[] request = _request.DF(DateTime.Now, "F9100", FullNumber);

                int sended = udp.Send(request, request.Length, ipendpoint);

                if (sended == request.Length)
                {
                    AddTotextBox("Data sended succefully\r\n", textBox4);
                }

                // После окончания попытки отправки закрываем UDP соединение,
                // и освобождаем занятые объектом UdpClient ресурсы.
                //udp.Close();
            }
            catch (Exception error)
            {
                AddTotextBox(error.Message + "\r\n", textBox4);
            }
        }

        void ReadBlock()
        {
            try
            {
                // Формирование оправляемого сообщения и его отправка.
                // Сеть "понимает" только поток байтов и ей безразличны
                // объекты классов, строки и т.п. Поэтому преобразуем текстовое сообщение в поток байтов.

                IPEndPoint ipendpoint = new IPEndPoint(parsedIP, ParsedPort);

                byte[] request = _request.ONEF("F9100", length1, length2);

                int sended = udp.Send(request, request.Length, ipendpoint);

                if (sended == request.Length)
                {
                    AddTotextBox("\r\nData sended succefully\r\n", textBox4);
                }

                // После окончания попытки отправки закрываем UDP соединение,
                // и освобождаем занятые объектом UdpClient ресурсы.
                //udp.Close();
            }
            catch (Exception error)
            {
                AddTotextBox(error.Message + "\r\n", textBox4);
            }
        }

        //Функция формирующая запрос запроса номера АРС
        void GetData()
        {
            try
            {
                //udp = new UdpClient(7007);

                // Указываем адрес отправки сообщения

                IPAddress ip = IPAddress.Parse("192.168.0.55");
                int port = 10001;
                //IPEndPoint ipendpoint = new IPEndPoint(ip, port);

                //IPAddress ipaddress = parsedIP;
                IPEndPoint ipendpoint = new IPEndPoint(ip, port);

                // Формирование оправляемого сообщения и его отправка.
                // Сеть "понимает" только поток байтов и ей безразличны
                // объекты классов, строки и т.п. Поэтому преобразуем текстовое сообщение в поток байтов.
                byte[] request = _request.GetDataFromARS();

                int sended = udp.Send(request, request.Length, ipendpoint);

                if (sended == request.Length)
                {
                    AddTotextBox("Data sended succefully\r\n", textBox4);
                }

                // После окончания попытки отправки закрываем UDP соединение,
                // и освобождаем занятые объектом UdpClient ресурсы.
                //udp.Close();
            }
            catch (Exception error)
            {
                AddTotextBox(error.Message + "\r\n", textBox4);
            }
        }

        //кнопка остановки цикла прослушки
        private void button2_Click(object sender, EventArgs e)
        {
            // Останавливаем цикл в дополнительном потоке
            stopReceive = true;
            listen.Abort();
            //listen.Join();
            AddTotextBox("\r\nПОТОК ОСТАНОВЛЕН МАФАКА!!11\r\n", textBox4);
            
            // Принудительно закрываем объект класса UdpClient
            //if (udp != null) udp.Close();
        }

        //класс параллельного добавления текста
        public static void AddTotextBox(string s, TextBox box)
        {
            if (box.InvokeRequired) box.BeginInvoke(new Action(() => { box.AppendText(s); }));
            else box.AppendText(s);
        }

        delegate void SetTextCallback(string text);

        private void SetText(string text)
        {
            if (this.textBox4.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.textBox4.Text += text;
            }
        }

        //класс параллельного закрашивания текстбокса
        public void ColorTextBox(Color color, TextBox box)
        {
            if (box.InvokeRequired) box.BeginInvoke(new Action(() => { box.BackColor = color; }));
            else box.BackColor = color;
        }

        //класс параллельного закрашивания текстбокса
        public void SetLabelText(Label label, string text)
        {
            label.Invoke(new Action(() =>
            {
                label.Text = text;
            }));
        }

        public void painter_onliner(int counter)
        {
            
            switch (counter)
            {
                case 0:
                    {
                        ColorTextBox(Color.YellowGreen, textBox3);

                        SetLabelText(label3, "Online");

                        break;
                    }
                case 1:
                    {
                        ColorTextBox(Color.YellowGreen, textBox5);

                        SetLabelText(label5, "Online");

                        break;
                    }
                case 2:
                    {
                        ColorTextBox(Color.YellowGreen, textBox6);

                        SetLabelText(label7, "Online");

                        break;
                    }
                case 3:
                    {
                        ColorTextBox(Color.YellowGreen, textBox9);

                        SetLabelText(label13, "Online");

                        break;
                    }
                case 4:
                    {
                        ColorTextBox(Color.YellowGreen, textBox8);

                        SetLabelText(label11, "Online");

                        break;
                    }
                case 5:
                    {
                        ColorTextBox(Color.YellowGreen, textBox7);

                        SetLabelText(label9, "Online");

                        break;
                    }
                case 6:
                    {
                        ColorTextBox(Color.YellowGreen, textBox2);

                        SetLabelText(label2, "Online");

                        break;
                    }

                default:
                    Console.WriteLine("Default case");
                    break;
            }
        }

        public void painter_errorer(int counter)
        {

            switch (counter)
            {
                case 0:
                    {
                        ColorTextBox(Color.Yellow, textBox3);

                        SetLabelText(label3, "Error");

                        break;
                    }
                case 1:
                    {
                        ColorTextBox(Color.Yellow, textBox5);

                        SetLabelText(label5, "Error");

                        break;
                    }
                case 2:
                    {
                        ColorTextBox(Color.Yellow, textBox6);

                        SetLabelText(label7, "Error");

                        break;
                    }
                case 3:
                    {
                        ColorTextBox(Color.Yellow, textBox9);

                        SetLabelText(label13, "Error");

                        break;
                    }
                case 4:
                    {
                        ColorTextBox(Color.Yellow, textBox8);

                        SetLabelText(label11, "Error");

                        break;
                    }
                case 5:
                    {
                        ColorTextBox(Color.Yellow, textBox7);

                        SetLabelText(label9, "Error");

                        break;
                    }
                case 6:
                    {
                        ColorTextBox(Color.Yellow, textBox2);

                        SetLabelText(label2, "Error");

                        break;
                    }

                default:
                    Console.WriteLine("Default case");
                    break;
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            //byte[] bytes = _request.GetBytesFromString(textBox5.Text);

            textBox4.Text = "";
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Thread rec1 = new Thread(new ThreadStart(FindARS));
            rec1.Start();
        }

        private void button11_Click(object sender, EventArgs e)
        {

        }

        private void button10_Click(object sender, EventArgs e)
        {
            udp = new UdpClient(7007);

            // Указываем адрес отправки сообщения

            IPAddress ip = IPAddress.Parse("192.168.0.66");
            int port = 10001;
            //IPEndPoint ipendpoint = new IPEndPoint(ip, port);

            //IPAddress ipaddress = parsedIP;
            IPEndPoint ipendpoint = new IPEndPoint(ip, port);

            // Формирование оправляемого сообщения и его отправка.
            // Сеть "понимает" только поток байтов и ей безразличны
            // объекты классов, строки и т.п. Поэтому преобразуем текстовое сообщение в поток байтов.
            byte[] request = _request.GetARSNumber();

            int sended = udp.Send(request, request.Length, ipendpoint);

            if (sended == request.Length)
            {
                AddTotextBox("Data sended succefully\r\n", textBox4);
            }

            // После окончания попытки отправки закрываем UDP соединение,
            // и освобождаем занятые объектом UdpClient ресурсы.
            udp.Close();
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            FindIndivid();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            nullf = false;
            achtf = false; 
            df = false;
            LayingRoute();
            //ReadData();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            //Work();
            PLCRead();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //theGrp.RemoveItems(handlesSrv, out aE);
            if (theGrp != null)
            {
                theGrp.Remove(false);
                theTargGrp.Remove(false);
                theSrv.Disconnect();
                theGrp = null;
                theTargGrp = null;
                theSrv = null;
            }

            stopReceive = true;
            connect.Abort();
            Process.GetCurrentProcess().Kill();
        }
    }
}
