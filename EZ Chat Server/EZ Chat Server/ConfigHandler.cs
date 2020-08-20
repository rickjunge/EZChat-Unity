using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZChatServer
{
    public static class ConfigHandler
    {
        public static void LoadConfig(out string _name, out int _port, out bool _sendConnectMessage, out string _host)
        {
            dynamic con = JsonConvert.DeserializeObject(File.ReadAllText("config.json"));
            _name = con.name;
            _port = con.port;
            _sendConnectMessage = con.sendConnectMessage;
            _host = con.host;
        }
    }
}