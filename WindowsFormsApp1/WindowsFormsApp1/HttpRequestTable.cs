using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Data;
using System.Windows.Forms;
using System.Threading.Tasks;
using Newtonsoft.Json;
/*Задание: Разработать приложение, которое взаимодействует с WEB-сервисом распознаваниеномеров.рф:45555 
 *          и отображает графически количество распознанных автомобилей за последние 7 дней.
 */

namespace Mallenom.Automarshal.HTTP.TestTask
{
    class Entry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("plate")]
        public string Plate { get; set; }

        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }

        [JsonProperty("videoChannel")]
        public IdName VideoChannel { get; set; }

        [JsonProperty("direction")]
        public string Direction { get; set; }

        [JsonProperty("links")]
        public Relation[] Links { get; set; }

    }

    public class IdName
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class Relation
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("color")]
        public int Color { get; set; }

        [JsonProperty("fields")]
        public PropertyPerson[] Fields { get; set; }
    }

    public class PropertyPerson
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    class HttpRequestTable
    {
        HttpWebRequest      request;
        HttpWebResponse     response;
        StringBuilder       jsonText;
        List<Entry>         entryList;
        Timer               timerRequest; // Таймер на повторный запрос
        DateTime            currDateTime; // текущая дата

        int countEntries;       // Кол-во всех записей за 7 дней
        int countRecognized;    // Кол-во распознанных номеров
        int countNotRecognized; // Кол-во нераспознанных номеров
        int countAutomobile;    // Кол-во распознанных автомобилей

        string  addressUrl;
      
        int     offset;

        const   int INTERVAL_TIMER = 100;
        const   int QUANTITY_DAYS = 7;
        const   string  URL = "http://распознаваниеномеров.рф:45555/api/v1/vehicles";
        const string DATE_FORMAT = "dd/MM/yyyy HH:mm";

        public DataTable    Table;
        
        public HttpRequestTable()
        {
            countEntries=0;
            countRecognized=0;
            countNotRecognized=0;
            countAutomobile=0;

            offset = 0;

            Table = new DataTable();
            Table.Columns.Add("Номер",          typeof(string));
            Table.Columns.Add("Дата/Время",     typeof(string));
            Table.Columns.Add("Направление",    typeof(string));
            Table.Columns.Add("Камера",         typeof(string));
            Table.Columns.Add("Статус",         typeof(string));
            Table.Columns.Add("ФИО",            typeof(string));
            
            entryList = new List<Entry>();

            timerRequest = new Timer();
            
            timerRequest.Interval = INTERVAL_TIMER;
            timerRequest.Tick += new EventHandler(TimerRequest_Tick);
            jsonText = new StringBuilder();
        }
        private void TimerRequest_Tick(object Sender, EventArgs e)
        {
            timerRequest.Enabled = false;
            RequestGet(offset);
        }

        public void RequestGet(int offset)
        {
            currDateTime = DateTime.Now;
            jsonText.Clear();
            addressUrl = URL + "?offset=" + offset.ToString();

            request = (HttpWebRequest)WebRequest.Create(addressUrl);
            request.Method = "GET";
            request.Accept = "application/json";

            response = (HttpWebResponse)request.GetResponse();

            StreamReader reader = new StreamReader(response.GetResponseStream());
            jsonText.Append(reader.ReadToEnd());
            
            response.Close();
            FillTable();
        }
        private void FillTable()
        {
            countEntries = 0;
            countRecognized = 0;
            countNotRecognized = 0;
            countAutomobile = 0;

            string status;
            string fullname;

            TimeSpan diffTime; // разница от текущей даты до последней записи в entryArr

            //string strJsonText = jsonText.ToString();
            Newtonsoft.Json.Linq.JObject obj = Newtonsoft.Json.Linq.JObject.Parse(jsonText.ToString());

            Entry[] entryArr = JsonConvert.DeserializeObject<Entry[]>(obj["entries"].ToString());
            
            foreach (Entry recognObj in entryArr)
            {
                status = "";
                fullname = "";
                if (recognObj.Links != null)
                {
                    status = recognObj.Links[0].DisplayName;
                    if(recognObj.Links[0].Fields!=null)
                    {
                        fullname = recognObj.Links[0].Fields[0].Value;
                    }
                    else
                    {
                        fullname = "";
                    }
                }
                else
                {
                    status  = "";
                   
                }
                diffTime = currDateTime.Subtract(recognObj.TimeStamp);
                if (diffTime.Days < QUANTITY_DAYS)
                {
                    Table.Rows.Add(recognObj.Plate, recognObj.TimeStamp.ToString(DATE_FORMAT), recognObj.Direction, recognObj.VideoChannel.Name, status, fullname);
                    entryList.Add(recognObj);
                }
                    
            }
        
            diffTime = currDateTime.Subtract(entryArr.Last().TimeStamp);
            if (diffTime.Days < QUANTITY_DAYS)
            {
                offset += 15;
                timerRequest.Enabled = true;
            }
            else
            {
                countEntries = entryList.Count;
                List<string> plates = new List<string>();
                foreach(Entry item in entryList)
                {
                    if (item.Plate.IndexOf("#") == -1)
                    {
                        countRecognized++;
                        ///
                        if (plates.Count != 0)
                        {
                            if(!plates.Exists(x => x == item.Plate))
                            {
                                plates.Add(item.Plate);
                            }
                        }
                        else
                            plates.Add(item.Plate);
                    }
                    else
                        countNotRecognized++;

                }
                countAutomobile = plates.Count();
                Table.Rows.Add("Всего: "+ countEntries.ToString(), 
                    "Распознано: "+countRecognized.ToString(), 
                    "Нераспознано: "+countNotRecognized.ToString(),
                    "Автомобилей: "+countAutomobile.ToString());
                MessageBox.Show("За последние "+QUANTITY_DAYS.ToString()+" дней" + "\r\n" +
                    "Всего записей: " + countEntries.ToString() + "\r\n" +
                    "Распознано: " + countRecognized.ToString() + "\r\n" +
                    "Нераспознано: " + countNotRecognized.ToString() + "\r\n" +
                    "Автомобилей: " + countAutomobile.ToString());
            }
        }
    }
}
