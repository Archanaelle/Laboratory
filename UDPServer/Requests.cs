using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Data.SqlClient;
using System.Web;

namespace UDPServer
{
    class Requests
    {
        //Form D0 command
        public byte[] GetARSNumber()
        {
            byte[] request = GetBytesFromString("00 A5 01 26 00 A8 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 FB 09 00 " + 
                "A5 FF 00 00 00 00 00 00 00 D0 0F 48 29 19 18 12 15 00 F9 00 11 00 00 00 00 00 00 00 00 00 00 00 00 00 " + 
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " + 
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " + 
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " + 
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 7D D1");

            return request;
        }

        //Кнопка поиска индивида
        public byte[] FindIndivid()
        {
            string buf = "", s = "";

            buf += "7F ";
            buf += "00 ";
            buf += "00 "; //номер сети
            buf += "00 ";
            buf += "00 ";
            buf += "00 ";

            buf += "00 ";
            buf += "01 "; //номер ретранслятора, исполнителя команды


            buf += "DF "; //код команды
            buf += "BF "; //байт опций

            buf += "00 ";
            buf += "00 "; //количество считываемых часовых записей 
            buf += "00 "; //номер сети для абонентов

            buf += "20 ";
            buf += "4A ";
            buf += "0D "; //тип и номер конкретного абонента

            buf += "00 ";
            buf += "00 "; //CRC16

            byte[] writebytes = calculateCRC(buf);

            byte[] ARS = calculateCRC("00 26 00 09 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 00 00"); //модемная часть
                                     //00 26 00 F4 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 D3 4C

            
            byte[] answer = new byte[ARS.Length + writebytes.Length];

            Array.Copy(ARS, answer, ARS.Length);
            Array.Copy(writebytes, 0, answer, ARS.Length, writebytes.Length);

            return answer;
        }

        //Команда поиска индивида
        public byte[] FindIndivid(string number_s)
        {
            byte[] number = BitConverter.GetBytes(Convert.ToInt32(number_s));
            string buf = "", s = "";

            buf += "7F ";
            buf += "00 ";
            buf += "00 "; //номер сети
            buf += "00 ";
            buf += "00 ";
            buf += "00 ";

            buf += "00 ";
            buf += "01 "; //номер ретранслятора, исполнителя команды


            buf += "DF "; //код команды
            buf += "BF "; //байт опций

            buf += "00 ";
            buf += "00 "; //количество считываемых часовых записей 
            buf += "00 "; //номер сети для абонентов

            buf += "20 ";
            //------------------указываем номер индивида без префика в 16СС-------------------
            s = number[0].ToString();

            buf += s + " ";
            s = number[1].ToString();

            buf += s + " ";
            //--------------------------------------------------------------------------------
            buf += "00 ";
            buf += "00 "; //CRC16

            byte[] writebytes = calculateCRC(buf);

            byte[] ARS = calculateCRC("00 26 00 09 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 00 00"); //модемная часть
            //00 26 00 F4 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 D3 4C


            byte[] answer = new byte[ARS.Length + writebytes.Length];

            Array.Copy(ARS, answer, ARS.Length);
            Array.Copy(writebytes, 0, answer, ARS.Length, writebytes.Length);

            return answer;
        }

        //Команда прокладки маршрута
        public byte[] D0(DateTime date, string number, string option)
        {
            string buf = "", prefix = "", s = "", sdate = "";

            sdate = date.ToString();

            prefix = number.Substring(0,2);
            number = number.Substring(2, number.Length - 2);
            byte[] _number = BitConverter.GetBytes(Convert.ToInt32(number, 10));

            buf += "FF "; //тип пакета
            buf += "00 ";
            buf += "00 "; 
            buf += "00 00 00 ";
            buf += "00 00 ";
            buf += "D0 ";
            buf += option + " ";
            buf += sdate.Substring(17, 2) + " "; //hh
            buf += sdate.Substring(14, 2) + " "; //mm
            buf += sdate.Substring(11, 2) + " "; //ss
            buf += sdate.Substring(0, 2) + " "; //dd
            buf += sdate.Substring(3, 2) + " "; //mm
            buf += sdate.Substring(8, 2) + " "; //yy
            buf += "00 ";
            buf += prefix + " ";
            //------------------указываем номер прибора без префика в 16СС-------------------
            s = Convert.ToString(_number[0], 16);

            buf += s + " ";

            s = Convert.ToString(_number[1], 16);
            if (_number[1] == 0x00)
            {
                s = "00";
            }
            buf += s + " ";
            
            //--------------------------------------------------------------------------------
            buf += "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ";
            buf += "00 ";
            buf += "00 "; //CRC16

            byte[] writebytes = calculateCRC(buf);

            Random rnd = new Random();
            int counter = rnd.Next(1, 210);
            byte[] ARS = calculateCRC("01 26 00 " + Convert.ToString(counter,16) + " 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 00 00"); //модемная часть
            //00 26 00 F4 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 D3 4C

            
            byte[] answer = new byte[ARS.Length + writebytes.Length];

            Array.Copy(ARS, answer, ARS.Length);
            Array.Copy(writebytes, 0, answer, ARS.Length, writebytes.Length);

            return answer;
        }

        //Команда поиска приборов
        public byte[] D2(DateTime date, string uspdnumber, string devicenumber)
        {
            string buf = "", prefix = "", s = "", sdate = "";

            sdate = date.ToString();

            prefix = uspdnumber.Substring(0, 2);
            uspdnumber = uspdnumber.Substring(2, uspdnumber.Length - 2);
            byte[] _uspdnumber = BitConverter.GetBytes(Convert.ToInt32(uspdnumber, 10));
            byte[] number = BitConverter.GetBytes(Convert.ToInt32(devicenumber, 10));


            buf += "FF "; //тип пакета
            buf += "00 ";
            buf += "00 ";
            buf += "00 00 00 ";

            //------------------указываем номер УСПД без префика в 16СС-------------------
            if (_uspdnumber[1] == 0x00)
            {
                buf += "00 ";
                s = Convert.ToString(_uspdnumber[0], 16);

                buf += s + " ";
            }
            else
            {
                s = Convert.ToString(_uspdnumber[0], 16);

                buf += s + " ";

                s = Convert.ToString(_uspdnumber[1], 16);
                if (_uspdnumber[1] == 0x00)
                {
                    s = "00";
                }
                buf += s + " ";
            }

            //--------------------------------------------------------------------------------
            /* 
            FF 
            00 
            00 
            00 00 00 
            00 A1 //number of retranslator
            D2 
            DF 
            00 00 
            00 
            01 12 2C 
            */

            buf += "D2 ";
            buf += "DF ";
            buf += "00 ";
            buf += "00 ";
            buf += "00 ";
            //------------------указываем номер прибора без префика в 16СС-------------------
            buf += Convert.ToString(number[2], 16) + " ";
            buf += Convert.ToString(number[1], 16) + " ";
            buf += Convert.ToString(number[0], 16) + " ";
            //-------------------------------------------------------------------------------

            //------------------указываем номер УСПД без префика в 16СС-------------------
            buf += "00 ";
            buf += prefix + " ";
            s = Convert.ToString(_uspdnumber[0], 16);

            buf += s + " ";

            s = Convert.ToString(_uspdnumber[1], 16);
            if (_uspdnumber[1] == 0x00)
            {
                s = "00";
            }
            buf += s + " ";

            //--------------------------------------------------------------------------------
            buf += "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 " +
                   "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ";
            buf += "00 ";
            buf += "00 "; //CRC16

            byte[] writebytes = calculateCRC(buf);

            byte[] ARS = calculateCRC("01 26 00 51 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 00 00"); //модемная часть
            //00 26 00 F4 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 D3 4C


            byte[] answer = new byte[ARS.Length + writebytes.Length];

            Array.Copy(ARS, answer, ARS.Length);
            Array.Copy(writebytes, 0, answer, ARS.Length, writebytes.Length);

            return answer;
        }

        //Команда вычитки объема архива
        public byte[] DF(DateTime date, string uspdnumber, string devicenumber)
        {
            string buf = "", prefix = "", s = "", sdate = "";

            sdate = date.ToString();

            prefix = uspdnumber.Substring(0, 2);
            uspdnumber = uspdnumber.Substring(2, uspdnumber.Length - 2);
            byte[] _uspdnumber = BitConverter.GetBytes(Convert.ToInt32(uspdnumber, 10));
            


            buf += "7F "; //тип пакета
            buf += "00 ";
            buf += "00 "; //web number
            buf += "00 00 00 ";

            //------------------указываем номер УСПД без префика в 16СС-------------------
            if (_uspdnumber[1] == 0x00)
            {
                buf += "00 ";
                s = Convert.ToString(_uspdnumber[0], 16);

                buf += s + " ";
            }
            else
            {
                s = Convert.ToString(_uspdnumber[0], 16);

                buf += s + " ";

                s = Convert.ToString(_uspdnumber[1], 16);
                if (_uspdnumber[1] == 0x00)
                {
                    s = "00";
                }
                buf += s + " ";
            }

            //--------------------------------------------------------------------------------
            /* 
            FF 
            00 
            00 
            00 00 00 
            00 A1 //number of retranslator
            D2 
            DF 
            00 00 
            00 
            01 12 2C 
            */

            buf += "DF ";
            buf += "B8 ";
            buf += "00 ";
            buf += "00 ";
            buf += "00 ";
            //------------------указываем номер прибора без префика в 16СС-------------------
            prefix = devicenumber.Substring(0, 2);

            devicenumber = devicenumber.Substring(2, devicenumber.Length - 2);
            byte[] number = BitConverter.GetBytes(Convert.ToInt32(devicenumber, 10));

            buf += prefix + " ";
            buf += Convert.ToString(number[1], 16) + " ";
            buf += Convert.ToString(number[0], 16) + " ";
            //-------------------------------------------------------------------------------
            buf += "00 ";
            buf += "00 "; //CRC16

            byte[] writebytes = calculateCRC(buf);

            byte[] ARS = calculateCRC("00 26 00 50 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 00 00"); //модемная часть
            //00 26 00 F4 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 D3 4C


            byte[] answer = new byte[ARS.Length + writebytes.Length];

            Array.Copy(ARS, answer, ARS.Length);
            Array.Copy(writebytes, 0, answer, ARS.Length, writebytes.Length);

            return answer;
        }

        //Команда вычитки архива
        public byte[] ONEF(string uspdnumber, string length1, string length2)
        {
            string buf = "", prefix = "", s = "";

            prefix = uspdnumber.Substring(0, 2);
            uspdnumber = uspdnumber.Substring(2, uspdnumber.Length - 2);
            byte[] _uspdnumber = BitConverter.GetBytes(Convert.ToInt32(uspdnumber, 10));



            buf += "00 "; //тип пакета
            buf += "00 ";
            buf += "00 "; //web number
            buf += "00 00 00 ";

            //------------------указываем номер УСПД без префика в 16СС-------------------
            if (_uspdnumber[1] == 0x00)
            {
                buf += "00 ";
                s = Convert.ToString(_uspdnumber[0], 16);

                buf += s + " ";
            }
            else
            {
                s = Convert.ToString(_uspdnumber[0], 16);

                buf += s + " ";

                s = Convert.ToString(_uspdnumber[1], 16);
                if (_uspdnumber[1] == 0x00)
                {
                    s = "00";
                }
                buf += s + " ";
            }

            buf += "1F ";
            buf += "00 ";
            buf += length1 + " ";
            buf += length2 + " ";
            buf += "00 00 00 00 ";
            buf += "00 00"; //CRC

            byte[] writebytes = calculateCRC(buf);

            byte[] ARS = calculateCRC("00 26 00 50 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 00 00"); //модемная часть
            //00 26 00 F4 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 D3 4C


            byte[] answer = new byte[ARS.Length + writebytes.Length];

            Array.Copy(ARS, answer, ARS.Length);
            Array.Copy(writebytes, 0, answer, ARS.Length, writebytes.Length);

            return answer;
        }

        //Кнопка настройки арсика
        public byte[] SetSettings(byte[] ip, string port)
        {
            string buf = "", s = "";

            s = "00 12 00 00 00 00 00 00 00 00 F3 02 ";

            s += port.Substring(0, 2) + " ";
            s += port.Substring(2, 2) + " ";

            buf = Convert.ToString(ip[0], 16);
            s += buf + " ";
            buf = Convert.ToString(ip[1], 16);
            s += buf + " ";
            buf = Convert.ToString(ip[2], 16);
            s += buf + " ";
            buf = Convert.ToString(ip[3], 16);
            s += buf;

            s += " 00 00"; //CRC
            byte[] answer = calculateCRC(s);

            return answer;
        }

        public byte[] test()
        {
            string s =               "00 A5 00 12 00 00 00 00 00 00 00 00 fe 00 00 00 00 00 00 00 ";

            byte[] answer = calculateCRC(s);

            return answer;
        }

        public byte[] Find()
        {
            string buf = "", s = "";

            byte[] data = new byte[274];  //пакет байт без стартового пакета для вычисления контрольной суммы CRC

            data[0] = System.Convert.ToByte("FF", 16); //Тип пакета
            data[1] = System.Convert.ToByte("00", 16); //00
            data[2] = System.Convert.ToByte("00", 16); //Номер сети для ретранслятора
            data[3] = System.Convert.ToByte("00", 16);
            data[4] = System.Convert.ToByte("00", 16);
            data[5] = System.Convert.ToByte("00", 16);
            data[6] = System.Convert.ToByte("45", 16); //Номер ретранслятора
            data[7] = System.Convert.ToByte("FF", 16); //Номер ретранслятора
            data[8] = System.Convert.ToByte("D0", 16); //D2 - код команды
            data[9] = System.Convert.ToByte("04", 16); //Байт опций 
            //Байты времени
            data[10] = System.Convert.ToByte("01", 16); //секунды
            data[11] = System.Convert.ToByte("01", 16); //минуты
            data[12] = System.Convert.ToByte("00", 16); //часы
            data[13] = System.Convert.ToByte("01", 16); //день
            data[14] = System.Convert.ToByte("01", 16); //месяц
            data[15] = System.Convert.ToByte("15", 16); //Год

            for (int i = 16; i < 274; i++)
            {
                data[i] = System.Convert.ToByte("00", 16);
            }



            ushort summ = _crc16(data); //Вычисление контрольной суммы CRC
            s = System.Convert.ToString(summ, 16);

            //Стартовый пакет байт
            byte[] writebytes = new byte[276];
            writebytes[0] = System.Convert.ToByte("00", 16);
            writebytes[1] = System.Convert.ToByte("A5", 16);

            for (int i = 0, j = 2; i < data.Length; i++, j++)
            {
                writebytes[j] = data[i];
            }

            //Добавление в общий пакет полученыых результатов вычисления CRC
            for (int i = 0; i < s.Length / 2; i++)
            {
                buf += s[i];
            }

            writebytes[274] = System.Convert.ToByte(buf, 16);
            buf = "";

            for (int i = s.Length / 2; i < s.Length; i++)
            {
                buf += s[i];
            }

            writebytes[275] = System.Convert.ToByte(buf, 16);

            return writebytes;
        }

        //Кнопка считывания данных из памяти АРС
        public byte[] GetDataFromARS()
        {
            string buf = "", s = "";
            //00 00 00 00 00 00 00 31 1F 01 01 50 00 00 00 00 56 0F
            buf += "00 ";
            buf += "00 ";
            buf += "00 "; //номер сети
            buf += "00 ";
            buf += "00 ";
            buf += "00 ";

            buf += "00 ";
            buf += "01 "; //номер ретранслятора, исполнителя команды


            buf += "1F "; //код команды
            buf += "01 "; //байт опций

            buf += "01 ";
            buf += "50 "; //количество считываемых часовых записей
            buf += "00 "; //номер сети для абонентов

            buf += "00 ";
            buf += "00 ";
            buf += "00 "; //тип и номер конкретного абонента

            buf += "00 ";
            buf += "00 "; //CRC16

            byte[] writebytes = calculateCRC(buf);

            byte[] ARS = calculateCRC("00 26 00 0A 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 00 00"); //модемная часть
            //00 26 00 F4 00 00 00 00 00 00 F0 00 00 00 00 00 00 00 D3 4C


            byte[] answer = new byte[ARS.Length + writebytes.Length];

            Array.Copy(ARS, answer, ARS.Length);
            Array.Copy(writebytes, 0, answer, ARS.Length, writebytes.Length);

            return answer;
        }

        //Функция расчета контрольной суммы
        public ushort _crc16(byte[] data)
        {
            ushort crc = 0xFFFF;

            for (int j = 0; j < data.Length; j++)
            {
                byte mask = 0x80;
                for (int i = 0; i < 8; i++)
                {
                    ushort bit = 0x00;

                    if ((data[j] & mask) != 0x00)
                    {
                        bit = 0x01;
                    }

                    if ((crc & 0x8000) != 0)
                    {
                        crc <<= 1;
                        crc += bit;
                        crc ^= 0x1021;
                    }
                    else
                    {
                        crc <<= 1;
                        crc += bit;
                    }
                    mask >>= 1;
                }
            }

            return crc;
        }

        public byte[] calculateCRC(string request)
        {
            byte[] buffer = GetBytesFromString(request);

            string buf = "", s = "";

            ushort checksumm = _crc16(buffer); //Вычисление контрольной суммы CRC
            s = System.Convert.ToString(checksumm, 16);

            //пакет байт для D0
            byte[] writebytes = new byte[buffer.Length + 2];
            //Add synchrobytes
            writebytes[0] = System.Convert.ToByte("00", 16);
            writebytes[1] = System.Convert.ToByte("A5", 16);

            for (int i = 0, j = 2; i < buffer.Length; i++, j++)
            {
                writebytes[j] = buffer[i];
            }

            //Добавление в общий пакет полученыых результатов вычисления CRC
            for (int i = 0; i < s.Length / 2; i++)
            {
                buf += s[i];
            }

            writebytes[writebytes.Length - 2] = System.Convert.ToByte(buf, 16);
            buf = "";

            for (int i = s.Length / 2; i < s.Length; i++)
            {
                buf += s[i];
            }

            writebytes[writebytes.Length - 1] = System.Convert.ToByte(buf, 16);

            return writebytes;
        }

        public double Inversion(byte[] data, int factor)
        {
            sbyte buf = new sbyte();
            buf = (sbyte)data[0];

            int val = 0;
            double answer = 0;

            val = (int)buf;
            val <<= 8;

            val |= (ushort)data[1];
            val <<= 8;

            val |= (ushort)data[2];

            string s = System.Convert.ToString(val, 10);
            val = System.Convert.ToInt32(s);

            switch (factor)
            {
                case (1): answer = val * 0.1; break;
                case (2): answer = val * 0.01; break;
                case (3): answer = val * 0.001; break;
                case (4): answer = val * 0.0001; break;
                case (5): answer = val * 0.00001; break;
                default:
                    break;
            }

            return answer;
        }

        //функция перевода строки в список байт
        public byte[] GetBytesFromString(string data)
        {

            try
            {
                List<byte> list = new List<byte>();
                string cash = "";
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] != ' ')
                    {
                        cash += data[i];
                    }
                    else
                    {
                        list.Add(Convert.ToByte(cash, 16));
                        cash = "";
                    }
                }
                if (cash != "")
                {
                    list.Add(Convert.ToByte(cash, 16));
                }

                byte[] bytes = new byte[list.Count];

                for (int i = 0; i < list.Count; i++)
                {
                    bytes[i] = list[i];
                }

                return bytes;
            }
            catch (Exception)
            {
                byte[] bytes = new byte[0];
                return bytes;
            }

        }

    }
}
