using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using NSspi;

namespace TestProtocol
{
    public class CustomServer
    {
        private Thread receiveThread;

        private Socket serverSocket;

        private Socket readSocket;

        private bool running;

        public CustomServer()
        {
            this.running = false;
        }

        public delegate void ReceivedAction( Message message );

        public event ReceivedAction Received;

        public event Action Disconnected;

        public event Action Stopped;

        public void StartServer( int port )
        {
            if( this.running )
            {
                throw new InvalidOperationException( "Already running" );
            }

            this.serverSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
            this.serverSocket.Bind( new IPEndPoint( IPAddress.Any, port ) );
            this.serverSocket.Listen( 1 );

            this.running = true;

            this.receiveThread = new Thread( ReceiveThreadEntry );
            this.receiveThread.Name = "SSPI Server Receive Thread";
            this.receiveThread.Start();
        }

        public void Stop()
        {
            if( this.running == false )
            {
                return;
            }

            this.running = false;

            this.serverSocket.Close();

            if( this.readSocket != null )
            {
                this.readSocket.Close();
            }

            this.receiveThread.Join();
        }

        public void Send( Message message )
        {
            if( this.running == false )
            {
                throw new InvalidOperationException( "Not connected" );
            }

            byte[] outBuffer = new byte[message.Data.Length + 8];

            ByteWriter.WriteInt32_BE( (int)message.Operation, outBuffer, 0 );
            ByteWriter.WriteInt32_BE( message.Data.Length, outBuffer, 4 );

            Array.Copy( message.Data, 0, outBuffer, 8, message.Data.Length );

            this.readSocket.Send( outBuffer, 0, outBuffer.Length, SocketFlags.None );

            Console.Out.WriteLine( "Server: Sent " + message.Operation );
        }

        private void ReceiveThreadEntry()
        {
            try
            {
                while( this.running )
                {
                    try
                    {
                        this.readSocket = this.serverSocket.Accept();
                    }
                    catch( SocketException e )
                    {
                        if( e.SocketErrorCode == SocketError.ConnectionAborted ||
                           e.SocketErrorCode == SocketError.Interrupted ||
                           e.SocketErrorCode == SocketError.OperationAborted ||
                           e.SocketErrorCode == SocketError.Shutdown )
                        {
                            // Выключение.
                            break;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    ReadLoop();
                }
            }
            catch( Exception e )
            {
                MessageBox.Show( "The SspiConnection receive thread crashed:\r\n\r\n" + e.ToString() );
            }
            finally
            {
                this.running = false;

                try
                {
                    if( this.Stopped != null )
                    {
                        this.Stopped();
                    }
                }
                finally { }
            }
        }

        private void ReadLoop()
        {
            byte[] readBuffer = new byte[65536];

            ProtocolOp operation;
            int messageLength;
            int position;
            int remaining;

            while( this.running )
            {
                try
                {
                    //                            |--4 bytes--|--4 bytes--|---N--- |
                    // Каждая команда - это TLV - | Операция  |    Длина  | Данные | 
                    int chunkLength;

                    // Прочтите операцию.    
                    this.readSocket.Receive( readBuffer, 4, SocketFlags.None );

                    // Проверьте, вышли ли мы из принимающего звонка после того, как нас отключили. 
                    if ( this.running == false ) { break; }

                    operation = (ProtocolOp)ByteWriter.ReadInt32_BE( readBuffer, 0 );

                    // Читаем длину 
                    this.readSocket.Receive( readBuffer, 4, SocketFlags.None );
                    messageLength = ByteWriter.ReadInt32_BE( readBuffer, 0 );

                    if( readBuffer.Length < messageLength )
                    {
                        readBuffer = new byte[messageLength];
                    }

                    // Чтение данных
                    // Socket.Receive может возвращать меньше данных, чем запрошено. 
                    remaining = messageLength;
                    chunkLength = 0;
                    position = 0;
                    while( remaining > 0 )
                    {
                        chunkLength = this.readSocket.Receive( readBuffer, position, remaining, SocketFlags.None );
                        remaining -= chunkLength;
                        position += chunkLength;
                    }
                }
                catch( SocketException e )
                {
                    if( e.SocketErrorCode == SocketError.ConnectionAborted ||
                        e.SocketErrorCode == SocketError.Interrupted ||
                        e.SocketErrorCode == SocketError.OperationAborted ||
                        e.SocketErrorCode == SocketError.Shutdown ||
                        e.SocketErrorCode == SocketError.ConnectionReset )
                    {
                        // Выключение.
                        break;
                    }
                    else
                    {
                        throw;
                    }
                }

                Console.Out.WriteLine( "Server: Received " + operation );

                if( this.Received != null )
                {
                    byte[] dataCopy = new byte[messageLength];
                    Array.Copy( readBuffer, 0, dataCopy, 0, messageLength );
                    Message message = new Message( operation, dataCopy );

                    try
                    {
                        this.Received( message );
                    }
                    catch( Exception )
                    { }
                }
            }

            try
            {
                if( this.Disconnected != null )
                {
                    this.Disconnected();
                }
            }
            catch { }
        }
    }
}