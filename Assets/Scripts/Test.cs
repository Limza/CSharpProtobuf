using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.Protobuf;

public class Test : MonoBehaviour
{
    void Start()
    {
        // login
        {
            NetworkMgr.GetInstance.Connect("77777");
        }

        {
            var cr = new Network.Packet.CCherryRoasting();
            cr.DescId = "1";
            cr.Oid = 2;
            cr.StoreKey = 3;
        }

        {
            var cr = new Network.Packet.CCherryRoasting
            {
                DescId = "1", Oid = 2, StoreKey = 3
            };

                        
        }

        // ex CLoginReq
        {
            // var msg = new Network.Packet.CLoginReq{
            //     binVer	"?"	string
		    //     dataVer	"166214_22116"	string
		    //     market	"DEV"	string
		    //     name	"patitest"	string
		    //     os	"Windows"	string
		    //     stamp	1599189634	number
		    //     token	"b99addc465bb614a6abd8763c1d32c4cbcaca7c4e783446835e70db92b0deff3"	string
		    //     userid	"77777"	string

            // };
            

            // var protocol = new Network.ClientMessage();
            // protocol.Pcode = Network.ProtocolCode.CLoadData;
            // protocol.Payload = msg.ToByteString();
        }
    }
}
