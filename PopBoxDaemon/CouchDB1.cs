using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace PigeonDaemon6214
{
    
    public class OrderItem : CouchObject
    {
        public string product_name { get; set; }
        public string product_id { get; set; }
        public string status { get; set; }
        public string location { get; set; }
        public string slot_loc { get; set; }
        public string emote_text { get; set; }
        public string customer_name { get; set; }
        public string created_by { get; set; }
        public DateTime created_at { get; set; }
        public string updated_by { get; set; }
        public DateTime updated_at { get; set; }

    }
    public class CouchObject
    {
        public string _id { get; set; }
        public string _rev { get; set; }
    }
    public class ViewResponse
    {
        public int Total_rows { get; set; }
        public int Offset { get; set; }
        public List<ViewResponseItem> Rows { get; set; }
    }
    public class ViewResponseItem
    {
        public string id { get; set; }
        public string key { get; set; }
        public object value { get; set; }
    }
    public class Credentials
    {
        public string name;
        public string password;
    }
}