using System;
using System.IO.Ports;
using System.Threading;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;

using MySql.Data;
using MySql.Data.MySqlClient;

// 20200813 version begin 
using log4net;
using log4net.Config;
using System.Reflection;
// 20200813 version begin
namespace PopBoxDaemon
{
    class Program
    {

        // Create a new TCP chat client
        static SerialPort PopBoxPort = null;
        static string PopBoxDevicetype = "PopBox";
        public static HttpClient Client = new HttpClient();

        public static List<int> motorAddresses = null;
        // 20200813 version begin
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // 20200813 version end
        static void Main(string[] args)
        {
            // 20200813 version begin
            // Load configuration
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            // version 1.2 : auto renew the disconnected sql connection
            log.Info("PopBox Daemon version 1.2 Started!");
            // 20200813 version end

            // The appsettings.json files “Copy to Output Directory” property 
            // should also be set to “Copy if newer” so that the application is able to access it
            // when published.
            var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            String PopBoxCom = configuration.GetConnectionString("PopBoxCom");
            //String sensorCom = configuration.GetConnectionString("SensorCom");
            String PopBoxCnt = configuration.GetConnectionString("PopBoxCnt");

            //Console.WriteLine("iotCom : " + iotCom);
            //Console.WriteLine("iotCnt : " + iotCnt);
            log.Info("PopBoxCom : " + PopBoxCom);
            log.Info("PopBoxCnt : " + PopBoxCnt);

            // Connect to iot com port
            //SerialPort iotPort = new SerialPort(iotCom);
            PopBoxPort = new SerialPort(PopBoxCom);
            PopBoxPort.BaudRate = 9600;
            PopBoxPort.ErrorReceived += PopBoxPort_ErrorReceived;
            PopBoxPort.Open();

            //string connStr = "server=localhost;user=root;database=crowndb;port=3306;password=crown@12345";
            // Standard Charter : Database
            string connStr = "server=localhost;user=crown;database=crowndb;port=3306;password=busca";
            MySqlConnection conn = new MySqlConnection(connStr);
            MySqlConnection conn2 = new MySqlConnection(connStr);
            //Console.WriteLine("Connecting to MySQL...");
            log.Info("Connecting to MySQL...");
            conn.Open();
            conn2.Open();
            //Console.WriteLine("Connected to MySQL...");
            log.Info("Connected to MySQL...");
            string selectSql = null;
            selectSql += "select devicename, actuation_status from devicestate ";
            //selectSql += " where devicetype = 'PopBox' and deviceavailability = 'X' ";
            selectSql += " where devicetype = '" + PopBoxDevicetype + "'";
            //selectSql += " and actuation_status in ( 'PUSH', 'OPENING', 'START-OPEN' ) order by devicename";
            // 2020-09-03, google tumbler demo for just ust P3 for tumbler : start
            selectSql += " and actuation_status in ( 'OPEN', 'LOCK' ) order by devicename";
            // 2020-09-03, google tumbler demo for just ust P3 for tumbler : end

            string updateSql = null;
            updateSql += "update devicestate set actuation_status = @actuation_status ";
            updateSql += " where devicename = @devicename and devicetype = '" + PopBoxDevicetype + "'";
            //updateSql +=   " and deviceavailability = 'X' ";

            MySqlCommand readCmd = new MySqlCommand(selectSql, conn);
            //readCmd.Prepare();

            MySqlCommand updateCmd = new MySqlCommand(updateSql, conn2);
            //updateCmd.Parameters.AddWithValue("@devicename", "");
            //updateCmd.Parameters.AddWithValue("@actuation_status", "");
            //updateCmd.Prepare();
            MySqlDataReader rdr = null;

            //int PopBoxNo = 0;
            string PopBoxNo = "";
            //byte[] bytesToSend;
            // translate the no. of PopBox to address
            //motorAddresses = configuration.GetConnectionString("MotorAddresses").Split(',').Select(int.Parse).ToList();
            //int motorAddress = 0;
            string devicename = null;
            string actuation_status = null;
            string PopBoxReply;
            string PopBoxCmd;
            bool repeat;
            //IotReply pr = new IotReply();
            var reconnect = true;
            do
            {
                try
                {
                    if (reconnect)
                    {
                        conn.Close();
                        conn = new MySqlConnection(connStr);
                        conn.Open();
                        readCmd = new MySqlCommand(selectSql, conn);
                        readCmd.Prepare();

                        conn2.Close();
                        conn2 = new MySqlConnection(connStr);
                        conn2.Open();
                        updateCmd = new MySqlCommand(updateSql, conn2);
                        updateCmd.Parameters.AddWithValue("@devicename", "");
                        updateCmd.Parameters.AddWithValue("@actuation_status", "");
                        updateCmd.Prepare();
                        reconnect = false;
                    }
                    rdr = readCmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        repeat = true;
                        // 20210205: reconnect connection when it is closed
                        devicename = rdr.GetString("devicename");
                        actuation_status = rdr.GetString("actuation_status");
                        PopBoxNo = devicename.Substring(1, 2);

                        //motorAddress = GetAddressByIot(iotNo);

                        //Console.WriteLine($"{devicename}, {actuation_status}");
                        log.Info($"Cmd : {devicename}, {actuation_status}");

                        if (actuation_status == "OPEN")
                        {
                            //bytesToSend = new byte[8] { Convert.ToByte(motorAddress), 0x09, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01 };
                            // SGP : for Trinamic TMCM-6214, just have one card, so the first address is 0x00 

                            //bytesToSend = new byte[8] { 0x01, 0x09, Convert.ToByte(motorAddress), 0x02, 0x00, 0x00, 0x00, 0x01 };
                            //bytesToSend = AddByteToArray(bytesToSend, Checksum(bytesToSend));
                            // iotPort.Write(bytesToSend, 0, bytesToSend.Length);
                            PopBoxCmd = "O" + PopBoxNo;
                            PopBoxPort.WriteLine(PopBoxCmd); //pass to arduino
                            Thread.Sleep(500); // if its fast enough to send then no need to remove flush 
                            PopBoxPort.BaseStream.Flush(); 

                            // wait the action to be completed
                            updateCmd.Parameters["@devicename"].Value = devicename;
                            updateCmd.Parameters["@actuation_status"].Value = "OPENING";
                            updateCmd.ExecuteNonQuery();
                            //

                            /* no need to wait for reply, because Iot lock will be automatically locked again
                            Thread.Sleep(500);
                            //pr = GetReply();
                            //if (pr.iotStatus == 1)

                            iotReply = iotPort.ReadLine().Substring(0, 3);
                            if (iotReply == iotCmd)
                            {
                                log.Info($"Reply : {devicename}, {actuation_status}");
                                updateCmd.Parameters["@devicename"].Value = devicename;
                                updateCmd.Parameters["@actuation_status"].Value = "OPENED";
                                updateCmd.ExecuteNonQuery();
                            } // set global user variable = 1 to open the door
                            */

                            // new logic

                            Thread.Sleep(500); // possible for the immediate lock 

                            log.Info($"Reply : {devicename}, {actuation_status}");
                            updateCmd.Parameters["@devicename"].Value = devicename;
                            updateCmd.Parameters["@actuation_status"].Value = "LOCKED";
                            updateCmd.ExecuteNonQuery();
                            //PopBoxPort.BaseStream.Flush();
                            repeat = false; 


                        }
                        else if (actuation_status == "LOCK")
                        {
                            PopBoxCmd = "C" + PopBoxNo;
                            PopBoxPort.WriteLine(PopBoxCmd);
                            PopBoxPort.BaseStream.Flush();
                            Thread.Sleep(500);
                            //pr = GetReply();
                            //if (pr.iotStatus == 1)

                            PopBoxReply = PopBoxPort.ReadLine().Substring(0, 3);
                            if (PopBoxReply == PopBoxCmd)
                            {
                                log.Info($"Reply : {devicename}, {actuation_status}");
                                updateCmd.Parameters["@devicename"].Value = devicename;
                                updateCmd.Parameters["@actuation_status"].Value = "LOCKED";
                                updateCmd.ExecuteNonQuery();
                
                            } // set global user variable = 1 to open the door

                        }
                        Thread.Sleep(100);
                    }
                    rdr.Close();

                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                    log.Info(ex.ToString());
                    reconnect = true;
                }
                Thread.Sleep(100);
            } while (true);
            //conn.Close();

        }

        /*public static IotReply GetReply()
        {
            byte[] cha = new byte[1];
            byte[] buffer = new byte[9];
            int byteGet = 0;
            int bytesGetTotal = 0;
            List<byte> l = new List<byte>();
            try
            {
                do
                {
                    byteGet = iotPort.Read(cha, 0, 1);
                    if (byteGet == 1)
                    {
                        //reply = Combine(reply, buffer);
                        l.Add(cha[0]);
                        ++bytesGetTotal;
                    }
                } while (bytesGetTotal < 9);
                buffer = new byte[9] { l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8] };
            }
            catch (TimeoutException e)
            {
                //Console.WriteLine(e.ToString());
                log.Info(e.ToString());
            }
            //Console.WriteLine($"RS485-Reply data: {BitConverter.ToString(buffer)}");
            //log.Info($"RS485-Reply data: {BitConverter.ToString(buffer)}");

            IotReply pr = new IotReply();

            // receive address
            pr.iotNo = Convert.ToInt32(buffer[0]);
            // module address
            pr.iotStatus = Convert.ToInt32(buffer[1]);

            pr.iotNo = GetIotByAddress(pr.iotNo);
            if (pr.iotStatus == 9)
            {
                pr.error = "Reply-Address Not Equal To 2";
            }
            return pr;
        }
        */


        private static void PopBoxPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            //Console.WriteLine($"IotPort Error : {e.ToString()}");
            log.Info($"PopBoxPort Error : {e.ToString()}");
            //throw new NotImplementedException();
        }

        /*
        public static byte Checksum(byte[] data)
        {
            byte sum = 0;
            unchecked // Let overflow occur without exceptions
            {
                foreach (byte b in data)
                {
                    sum += b;
                }
            }
            return sum;
        }

        public static byte[] AddByteToArray(byte[] bArray, byte newByte)
        {
            byte[] newArray = new byte[bArray.Length + 1];
            //bArray.CopyTo(newArray, 1);
            //newArray[0] = newByte;
            bArray.CopyTo(newArray, 0);
            newArray[bArray.Length] = newByte;
            return newArray;
        }
        static public int GetAddressByIot(int address)
        {
            return motorAddresses[address - 1];
        }
        static public int GetIotByAddress(int motor)
        {
            return motorAddresses.FindIndex(x => x == motor) + 1;
        }
        */

    }

}
