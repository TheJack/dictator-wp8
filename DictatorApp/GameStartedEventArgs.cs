using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DictatorApp
{
    class GameStartedEventArgs : EventArgs
    {
        public List<string> Players { get; private set; }

        public GameStartedEventArgs(List<string> names)
        {
            Players = names;
        }
    }

    class MessageEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public MessageEventArgs(string message)
        {
            this.Message = message;
        }
    }
}
