using System;
using System.Collections;
using System.Collections.Generic;
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
    
    bool isConnect = false;
    string status;
    string uid, nick;
    AsynchronousClient asyncClient;
    List<byte> receiveBufferList = new List<byte>();
    Coroutine msgTranslateCo;

    public void Init(in string uid, in string nick)
    {
        this.uid = uid;
        this.nick = nick;

        this.startAsyncClient();
    }

    void startAsyncClient()
    {
        this.asyncClient = new AsynchronousClient(new IPEndPoint(IPAddress.Parse(this.serverIp), this.serverPort));
        this.status = "connecting " + this.asyncClient.ConnectCount.ToString();
        
        this.asyncClient.ConnectCallback = (data, error) => {
            if (SocketError.Success == error)
            {
                this.isConnect = true;
                this.requestLogin();
                this.msgTranslateCo = StartCoroutine(this.msgTranslate());
            }
            else
                this.status = "connecting " + this.asyncClient.ConnectCount.ToString();
        };
        
        this.asyncClient.ReceiveCallback = (data, error) => {
            if (this.asyncClient == null || SocketError.Success != error)
            {
                Debug.Log(error);
                this.reStart();
                return;
            }
            this.receiveBufferList.AddRange(data);
        };

        this.asyncClient.Start();
    }

    void requestLogin()
    {
        this.status = "logging in";
        var msg = new Network.Packet.CLoginReq();
        msg.Userid = this.uid;
        
        msg.BinVer = "?";
        msg.DataVer = "166214_22116";
        msg.Market = "DEV";
        // msg.Os = "Windows";
        // msg.Stamp = 1599189634;
        // msg.Token = "b99addc465bb614a6abd8763c1d32c4cbcaca7c4e783446835e70db92b0deff3";

        this.Write(Network.ProtocolCode.CLoginReq, msg);
    }

    void reStart()
    {
        if (this.isConnect)
        {
            this.isConnect = false;
            this.receiveBufferList.Clear();
            
            if (this.msgTranslateCo != null)
                StopCoroutine(this.msgTranslateCo);
            this.msgTranslateCo = null;
            
            this.asyncClient.Close();
            this.asyncClient = null;
            
            this.startAsyncClient();
        }
    }

    public void Write(in Network.ProtocolCode protocolCode, IMessage msg)
    {
        // TODO: 로그인 요청 아닐경우 구현 필요

        var p = new Network.ClientMessage();
        p.Payload = msg.ToByteString();
        p.PacketId = 0;
        p.Pcode = protocolCode;
        this.send(p.ToByteArray(), p.PacketId);
    }

    void send(byte[] pktByte, in int packetId)
    {
        if (this.asyncClient == null)
        {
            // TODO: socket null일때 예외처리
            this.reStart();
            return;
        }

        var lenByte = BitConverter.GetBytes(pktByte.Length);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(lenByte);

        List<byte> sendByteList = new List<byte>();
        sendByteList.AddRange(lenByte);
        sendByteList.AddRange(pktByte);

        this.asyncClient.Send(sendByteList.ToArray());
    }

    int msgLen = 0;
    IEnumerator msgTranslate()
    {
        // TODO : 데이터를 읽는 과정에서, msgLen이 문제가 있거나, Parser에 문제가 있다면,
        // 그 즉시 모든 receive 받은 패킷들을 패기하고, 후속 조치를 취해야 한다
        // ex) 게임을 재시작 한다거나, 이전 receive패킷들은 무시해버리는 식으로
        while (true)
        {
            yield return new WaitForEndOfFrame();

            if (this.msgLen == 0)
            {
                if (this.receiveBufferList.Count < 4)
                    continue;
                var lenBuffer = this.receiveBufferList.GetRange(0, 4).ToArray();
                this.receiveBufferList.RemoveRange(0, 4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lenBuffer);
                this.msgLen = BitConverter.ToInt32(lenBuffer, 0);
            }

            if (this.receiveBufferList.Count < this.msgLen)
                continue;
            var dataBuffer = this.receiveBufferList.GetRange(0, this.msgLen).ToArray();
            this.receiveBufferList.RemoveRange(0, this.msgLen);

            var receiveData = Network.ServerMessage.Parser.ParseFrom(dataBuffer);
            var loginData = Network.Packet.SLoginRes.Parser.ParseFrom(receiveData.Payload);
            Debug.Log(loginData);

            this.msgLen = 0;
        }
    }
}
