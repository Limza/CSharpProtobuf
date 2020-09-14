using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using SingletonPattern;
using UnityEngine;

public class NetworkMgr : Singleton<NetworkMgr>
{
    public string serverIp = "127.0.0.1";
    public int serverPort = 3927;
    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    
    string status;
    string uid;

    public void Init(string uid)
    {
        this.uid = uid;
        StartCoroutine(this.serverUpdate());
    }

    public void Write(in Network.ProtocolCode protocolCode, IMessage msg)
    {
        // TODO: 로그인 요청 아닐경우 구현 필요

        var p = new Network.ClientMessage();
        p.Payload = msg.ToByteString();
        p.PacketId = 0;
        p.Pcode = protocolCode;
        var pkt = p.ToByteString();
        this.send(pkt, p.PacketId);
    }

    IEnumerator serverUpdate()
    {
        // 접속이 끊겨도 계속 시도
        while (true)
        {
            this.connect();
            this.requestLogin();

            while (true)
            {
                yield return new WaitForEndOfFrame();
                this.receive(4, out byte[] lenBuffer);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lenBuffer);
                var len = BitConverter.ToInt32(lenBuffer, 0);
                
                this.receive(len, out byte[] dataBuffer);
                var receiveData = Network.ServerMessage.Parser.ParseFrom(dataBuffer);
                var loginData = Network.Packet.SLoginRes.Parser.ParseFrom(receiveData.Payload);
                Debug.Log(loginData);
            }
        }
    }

    void connect()
    {
        var endPoint = new IPEndPoint(IPAddress.Parse(this.serverIp), this.serverPort);

        // 계속 접속 시도
        var connectCount = 0;
        while (true)
        {
            try
            {
                socket.Connect(endPoint);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.TimedOut)
                {
                    connectCount++;
                    this.status = "connecting " + connectCount.ToString();
                }
                else if (e.SocketErrorCode == SocketError.ConnectionRefused)
                    this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                else if (e.SocketErrorCode == SocketError.IsConnected)
                    break;
            }
        }
    }

    void requestLogin()
    {
        this.status = "logging in";
        var msg = new Network.Packet.CLoginReq{
            Userid = this.uid,
            BinVer = "?",
            DataVer = "166214_22116",
            Market = "DEV",
            // Os = "Windows",
            // Stamp = 1599189634,
            // Token = "b99addc465bb614a6abd8763c1d32c4cbcaca7c4e783446835e70db92b0deff3"
        };

        this.Write(Network.ProtocolCode.CLoginReq, msg);
    }

    void send(Google.Protobuf.ByteString pkt, in int packetId)
    {
        if (this.socket == null)
        {
            // TODO: socket null일때 예외처리
            return;
        }

        // >i4 == 빅엔디안 인티져 4바이트 압축
        // >I1 == 빅엔디안 언사인드 인티져 1바이트 압축

        var pktByte = pkt.ToByteArray();
        var lenByte = BitConverter.GetBytes(pktByte.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lenByte);
        
        BitConverter.ToInt32(lenByte, 0);

        var sendByte = new byte[lenByte.Length + pktByte.Length];
        Array.Copy(lenByte, 0, sendByte, 0, lenByte.Length);
        Array.Copy(pktByte, 0, sendByte, lenByte.Length, pktByte.Length);

        this.socket.Send(sendByte);
    }

    byte[] receiveBuffer = null;
    bool receive(in int len, out byte[] outBuffer)
    {
        outBuffer = new byte[len];
        // len의 길이만큼 out 버퍼를 채원준다
        while (this.receiveBuffer == null || this.receiveBuffer.Length < len)
        {
            var tempBuffer = new byte[1024];
            try
            {
                int byteCount = this.socket.Receive(tempBuffer);
                if (byteCount > 0)
                {
                    if (this.receiveBuffer == null)
                    {
                        this.receiveBuffer = new byte[byteCount];
                        Array.Copy(tempBuffer, 0, this.receiveBuffer, 0, byteCount);
                    }
                    else
                    {
                        var oldBuffer = this.receiveBuffer;
                        this.receiveBuffer = new byte[oldBuffer.Length + byteCount];
                        oldBuffer.CopyTo(this.receiveBuffer, 0);
                        tempBuffer.CopyTo(this.receiveBuffer, oldBuffer.Length);
                    }
                }
            }
            catch (SocketException e)
            {
                this.receiveBuffer = null;
                Debug.Log(e.Message);
                return false;
            }
        }

        Array.Copy(this.receiveBuffer, 0, outBuffer, 0, len);
        var receiveBufferList = this.receiveBuffer.ToList();
        receiveBufferList.RemoveRange(0, len);
        this.receiveBuffer = receiveBufferList.ToArray();
        return true;
    }
}
