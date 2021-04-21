using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml.Linq;

namespace Практика_1
{
    public partial class Form1 : Form
    {
        /*Хранение кодов и названий валют*/
        public struct Val
        {
            public string code;
            public string name;
        }
        public List<Val> ValList = new List<Val>();
        /******************************************/

        public Form1()
        {
            InitializeComponent();

            /*Стартовые настройки*/
            dateTimePicker1.Format = DateTimePickerFormat.Custom;
            dateTimePicker2.Format = DateTimePickerFormat.Custom;
            dateTimePicker1.MaxDate = DateTime.Today;
            dateTimePicker2.MaxDate = DateTime.Today;
            comboBox2.SelectedIndex = 0;

            /*******************************************************/

            /*Получение списка волют*/
            WebClient webClient = new WebClient();
            var xml = webClient.DownloadString("http://www.cbr.ru/scripts/XML_daily.asp");
            XDocument xDoc = XDocument.Parse(xml);
            var el = xDoc.Element("ValCurs").Elements("Valute").ToList();
            Val buf;
            for (int i = 0; i < el.Count; i++)
            {
                buf.code = el[i].Attribute("ID").Value;
                buf.name = el[i].Element("Name").Value;
                ValList.Add(buf);
                comboBox1.Items.Add(buf.name);
            }
            /***************************/
        }

        private void Control_ValueChanged(object sender, EventArgs e)
        {
            /*Оценка корректности введенных данных*/
            if (((comboBox1.SelectedIndex == -1 || comboBox2.SelectedIndex == -1 || dateTimePicker1.Value > dateTimePicker2.Value)) ||
               ((comboBox2.SelectedIndex == 1 && dateTimePicker1.Value.AddDays(7) > dateTimePicker2.Value) ||
                (comboBox2.SelectedIndex == 2 && dateTimePicker1.Value.AddMonths(1) > dateTimePicker2.Value) ||
                (comboBox2.SelectedIndex == 3 && dateTimePicker1.Value.AddYears(1) > dateTimePicker2.Value)))
            {
                MessageBox.Show("Введены некорректные данные", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            /***********************************************************************************************************/

            chart1.Series.Clear(); //Очистка графика


            switch (comboBox2.SelectedIndex) //Вывод информации, согласно шагу
            {
                case 0: //День
                    Day_Step();
                    break;
                case 1: //Неделя
                    Custom_Step(7);
                    break;
                case 2: //Месяц
                    Custom_Step(31);
                    break;
                case 3: //Год
                    Custom_Step(365);
                    break;
            }
        }

        private List<XElement> GetInfo_FromServer()
        {
            WebClient webClient = new WebClient();
            XDocument xDocument;

            xDocument = XDocument.Parse(
                webClient.DownloadString(
                    "http://www.cbr.ru/scripts/XML_dynamic.asp?date_req1=" +
                    dateTimePicker1.Value.ToString("dd/MM/yyyy") + "&date_req2=" +
                    dateTimePicker2.Value.ToString("dd/MM/yyyy") + "&VAL_NM_RQ=" +
                    ValList[comboBox1.SelectedIndex].code));

            return xDocument.Element("ValCurs").Elements("Record").ToList();
        }

        private void Day_Step()
        {
            Series a = new Series(ValList[comboBox1.SelectedIndex].name + "/Рубль");
            a.ChartType = SeriesChartType.Line;
            var el = GetInfo_FromServer();

            for (int i = 0; i < el.Count(); i++)
            {
                a.Points.AddXY(DateTime.Parse(el[i].Attribute("Date").Value),
                    Double.Parse(el[i].Element("Value").Value));
            }
            chart1.Series.Add(a);
            Add_SMA();
        }

        private void Custom_Step(int step)
        {
            Series a = new Series(ValList[comboBox1.SelectedIndex].name + "/Рубль");
            a.ChartType = SeriesChartType.Line;
            var el = GetInfo_FromServer();
            double sum = 0;
            for (int i = 0; i < el.Count(); i++)
            {
                sum += Double.Parse(el[i].Element("Value").Value);
                if (i % step == 0 && i != 0)
                {
                    a.Points.AddXY(i/step, sum / step);
                    sum = 0;
                }
            }
            chart1.Series.Add(a);
            Add_SMA();
        }

        private void Add_SMA()
        {
            Series SMA_10 = new Series("SMA 10 Days");
            Series SMA_100 = new Series("SMA 100 Days");
            SMA_10.ChartType = SeriesChartType.Line;
            SMA_100.ChartType = SeriesChartType.Line;
            var el = GetInfo_FromServer();
            double sum_10 = 0, sum_100 = 0;

            SMA_100.Points.AddXY(DateTime.Parse(el[0].Attribute("Date").Value),
                    Double.Parse(el[0].Element("Value").Value));

            for (int i = 0; i < el.Count(); i++)
            {
                sum_10 += Double.Parse(el[i].Element("Value").Value);
                sum_100 += Double.Parse(el[i].Element("Value").Value);

                if (i % 10 == 0 && i != 0)
                {
                    SMA_10.Points.AddXY(DateTime.Parse(el[i].Attribute("Date").Value), sum_10 / 10);
                    sum_10 = 0;
                }

                if (i % 100 == 0 && i != 0)
                {
                    SMA_100.Points.AddXY(DateTime.Parse(el[i].Attribute("Date").Value), sum_100 / 100);
                    sum_100 = 0;
                }
            }

            chart1.Series.Add(SMA_10);
            chart1.Series.Add(SMA_100);
        }



        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) // Изменение отображения даты в зависимости от шага
        {
            switch (comboBox2.SelectedIndex) //combobox, отвечающий за шаг
            {
                case 0:
                case 1:
                    dateTimePicker1.CustomFormat = "dd MMMM yyyy";
                    dateTimePicker2.CustomFormat = "dd MMMM yyyy";
                    break;
                case 2:
                    dateTimePicker1.CustomFormat = "MMMM yyyy";
                    dateTimePicker2.CustomFormat = "MMMM yyyy";
                    break;
                case 3:
                    dateTimePicker1.CustomFormat = "yyyy";
                    dateTimePicker2.CustomFormat = "yyyy";
                    break;
            }
        }
    }
}
