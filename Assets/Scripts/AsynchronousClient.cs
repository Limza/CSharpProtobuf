using System;
using System.Net;
using System.Net.Sockets;

public class AsynchronousClient : SocketAsyncEventArgs
{
    public readonly int TimeoutMilliSec = 3000;
    public readonly int SocketBufferSize = 1024;

    public delegate void AsynchronousClientDelegate(byte[] data, SocketError error);
    public AsynchronousClientDelegate ConnectCallback;
    public AsynchronousClientDelegate ReceiveCallback;

    public int ConnectCount;

    Socket socket;

    public AsynchronousClient(EndPoint endPoint)
    {
        base.RemoteEndPoint = endPoint;
    }

    public void Start()
    {
        this.connect();

        base.UserToken = this.socket;
        base.SetBuffer(new byte[this.SocketBufferSize], 0, this.SocketBufferSize);
        base.Completed += this.receiveCompleted;
        if (!this.socket.ReceiveAsync(this))
            this.receiveCompleted(null, this);
    }

    void connect()
    {
        this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        this.socket.NoDelay = true; // nagle off

        void errorConnect(SocketError err)
        {
            this.ConnectCount++;
            this.socket.Close();
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.NoDelay = true; // nagle off
            this.ConnectCallback(null, err);
        }

        // 접속에 실패하더라도, 계속해서 접속을 시도한다
        while (true)
        {
            try
            {
                var asyncResut = this.socket.BeginConnect(base.RemoteEndPoint, null, null);
                if (asyncResut.AsyncWaitHandle.WaitOne(this.TimeoutMilliSec, true))
                    break;
                else
                    errorConnect(SocketError.TimedOut);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.TimedOut || e.SocketErrorCode == SocketError.ConnectionRefused)
                    errorConnect(e.SocketErrorCode);
                else if (e.SocketErrorCode == SocketError.IsConnected)
                    break;
            }
        }

        this.ConnectCallback(null, SocketError.Success);
    }

    void receiveCompleted(object sender, SocketAsyncEventArgs e)
    {
        if (this.socket.Connected && base.BytesTransferred > 0 && e.SocketError == SocketError.Success)
        {
            var data = new byte[base.BytesTransferred];
            Array.Copy(e.Buffer, data, base.BytesTransferred);
            this.ReceiveCallback(data, e.SocketError);
            
            if (!this.socket.ReceiveAsync(this))
                this.receiveCompleted(null, e);
        }
        else
            this.ReceiveCallback(null, e.SocketError);
    }

    public void Send(byte[] buffer)
    {
        this.socket.Send(buffer);
    }

    public void Close()
    {
        this.socket.Close();
    }
}