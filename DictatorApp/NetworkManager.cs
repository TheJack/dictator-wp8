using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DictatorApp
{
    class NetworkManager
    {
        public static readonly string HOST_NAME = /*"10.255.1.18";/*/"10.0.249.104";
        public const int PORT_NUMBER = 3001;
        public const int TIMEOUT_MILLISECONDS = 5000;
        static Socket _socket;
        static BackgroundWorker worker;
        static StringBuilder recvBuffer = new StringBuilder();

        static ManualResetEvent _connectDone = new ManualResetEvent(false);
        static ManualResetEvent _recvDone = new ManualResetEvent(false);

        public static event EventHandler<GameStartedEventArgs> GameStarted;
        public static event EventHandler<MessageEventArgs> ConnectError;
        public static event EventHandler GameEnded;

        public static void Connect()
        {
            string hostName = HOST_NAME;
            int portNumber = PORT_NUMBER;
            
            string result = string.Empty;

            // Create DnsEndPoint. The hostName and port are passed in to this method.
            DnsEndPoint hostEntry = new DnsEndPoint(hostName, portNumber);

            // Create a stream-based, TCP socket using the InterNetwork Address Family. 
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Create a SocketAsyncEventArgs object to be used in the connection request
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = hostEntry;

            // Inline event handler for the Completed event.
            // Note: This event handler was implemented inline in order to make this method self-contained.
            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
            {
                // Retrieve the result of this request
                result = e.SocketError.ToString();
                _connectDone.Set(); 
            });

            _connectDone.Reset();
            _socket.ConnectAsync(socketEventArg);
            _connectDone.WaitOne();

            if (_socket.Connected)
            {
                worker = new BackgroundWorker();
                worker.WorkerSupportsCancellation = true;
                worker.DoWork += worker_DoWork;
                worker.RunWorkerAsync();
            }
            else
            {
                if (ConnectError != null)
                {
                    ConnectError(null, new MessageEventArgs(result));
                }
            }

            Debug.WriteLine("Connect: " + result);
        }

        public static void Disconnect()
        {
            if (_socket != null)
            {
                _socket.Close();
                _socket.Dispose();
                _socket = null;
            }

            if (worker != null)
            {
                worker.CancelAsync();
                worker = null;
            }

            if (_recvDone != null)
            {
                _recvDone.Set();
            }

            if (_connectDone != null)
            {
                _connectDone.Set();
            }

            if (GameEnded != null)
            {
                GameEnded(null, EventArgs.Empty);
            }
        }

        static void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Debug.WriteLine("receive worker started");

            while (!worker.CancellationPending)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = _socket.RemoteEndPoint;

                byte[] payload = new byte[4096];
                args.SetBuffer(payload, 0, payload.Length);

                string dataFromServer = "";
                args.Completed += delegate(object s, SocketAsyncEventArgs args1)
                {
                    dataFromServer = Encoding.UTF8.GetString(args1.Buffer, 0, args1.BytesTransferred);
                    _recvDone.Set();
                };

                _recvDone.Reset();
                Debug.WriteLine("ReceiveAsync");
                _socket.ReceiveAsync(args);

                _recvDone.WaitOne();
                Debug.WriteLine("Received data: " + dataFromServer);
                if (!String.IsNullOrWhiteSpace(dataFromServer))
                {
                    recvBuffer.Append(dataFromServer);
                    if (dataFromServer.Contains('\n'))
                    {
                        OnMessageReceived();
                    }
                }

                System.Threading.Thread.Sleep(50);
            }
        }

        private static void OnMessageReceived()
        {
            if (recvBuffer.Length == 0)
            {
                return;
            }

            string[] messages = recvBuffer.ToString().Split('\n');
            for (int i = 0; i < messages.Length - 1; i++)
            {
                HandleMessage(messages[i]);
            }

            if (messages.Length > 1)
            {
                recvBuffer.Clear();
                recvBuffer.Append(messages[messages.Length - 1]);
            }
        }

        private static void HandleMessage(string message)
        {
            Debug.WriteLine("HandleMessage: " + message);
            string[] args = message.Split(',');
            if (args[0] == "start")
            {
                int numPlayers = int.Parse(args[1]);
                List<string> players = new List<string>();
                for (int i = 2; i < args.Length; i++)
                {
                    players.Add(args[i]);
                }

                if (GameStarted != null)
                {
                    GameStarted(null, new GameStartedEventArgs(players));
                }
            }
            else if (args[0] == "round")
            {
                int round = int.Parse(args[1]);
                string word = args[2];
                int timeout = int.Parse(args[3]);
                GameManager.PushRound(round, word, timeout);
            }
            else if (args[0] == "update_scores")
            {
                List<int> scores = new List<int>();
                for (int i = 1; i < args.Length; i++)
                {
                    scores.Add(int.Parse(args[i]));
                }

                GameManager.UpdateScores(scores);
            }
            else if (args[0] == "update_typing_state")
            {
                List<bool> states = new List<bool>();
                for (int i = 1; i < args.Length; i++)
                {
                    states.Add(int.Parse(args[i]) == 1);
                }

                GameManager.UpdateTypingState(states);
            }
            else if (args[0] == "your_score")
            {
                int myScore = int.Parse(args[1]);
                GameManager.MyStats.Score = myScore;
            }
            else if (args[0] == "end")
            {
                List<int> scores = new List<int>();
                for (int i = 1; i < args.Length; i++)
                {
                    scores.Add(int.Parse(args[i]));
                }

                GameManager.UpdateScores(scores);
                Disconnect();
            }
        }

        public static string Send(string data)
        {
            string response = "Operation Timeout";

            // We are re-using the _socket object initialized in the Connect method
            if (_socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

                // Set properties on context object
                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                socketEventArg.UserToken = null;

                // Inline event handler for the Completed event.
                // Note: This event handler was implemented inline in order 
                // to make this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                {
                    response = e.SocketError.ToString();
                    Debug.WriteLine("Send: " + response);
                });

                // Add the data to be sent into the buffer
                byte[] payload = Encoding.UTF8.GetBytes(data);
                socketEventArg.SetBuffer(payload, 0, payload.Length);

                Debug.WriteLine("Sending data: " + data);
                // Make an asynchronous Send request over the socket
                _socket.SendAsync(socketEventArg);
            }
            else
            {
                response = "Socket is not initialized";
            }

            return response;
        }

        public static void InitializeSession(string username)
        {
            BackgroundWorker connectWorker = new BackgroundWorker();
            connectWorker.DoWork += (sender, e) =>
            {
                Connect();
                Send("set_name," + username + "\n");
                Send("play\n");
            };
            connectWorker.RunWorkerAsync();
        }
    }
}
