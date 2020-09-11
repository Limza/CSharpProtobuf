using System;
using System.IO;
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

    public void Connect(string uid)
    {
        this.uid = uid;
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

        this.requestLogin();
    }

    void requestLogin()
    {
        this.status = "logging in";
        var msg = new Network.Packet.CLoginReq{
            Userid = this.uid,
            BinVer = "?",
            DataVer = "166214_22116",
            Market = "DEV",
            Name = "patitest",
            Os = "Windows",
            Stamp = 1599189634,
            Token = "b99addc465bb614a6abd8763c1d32c4cbcaca7c4e783446835e70db92b0deff3"
        };

        this.Write(Network.ProtocolCode.CLoginReq, msg);
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

        this.receive(4, out byte[] buffer);
        
        // TODO : 임시로 리시브에서 모든 데이터 받은걸 가정
        var lenBuffer = new byte[4];
        Array.Copy(buffer, lenBuffer, 4);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lenBuffer);
        var len = BitConverter.ToInt32(lenBuffer, 0);
        
        var dataBuffer = new byte[len];
        Array.Copy(buffer, 4, dataBuffer, 0, len);

        var receiveData = Network.ServerMessage.Parser.ParseFrom(dataBuffer);

        var loginData = Network.Packet.SLoginRes.Parser.ParseFrom(receiveData.Payload);
        Debug.Log(loginData);
    }

    bool receive(in int len, out byte[] buffer)
    {
        buffer = new byte[1024];
        try
        {
            int byteCount = this.socket.Receive(buffer);
            if (byteCount > 0)
            {
                // TODO : 임시로 바로 넣어줌, 원래는 while문을 타서 len까지 버퍼를 받아야 한다
            }
        }
        catch (SocketException e)
        {
            Debug.Log(e.Message);
            return false;
        }

        Debug.Log(buffer.ToString());
        return true;
    }
}
