﻿using MLAPI;
using System.IO;
using UnityEngine;

public class SyncPosition : NetworkedBehaviour
{
    private float lastSentTime;
    public float PosUpdatesPerSecond = 20;


    private void Awake()
    {
        if(isServer)
        {
            RegisterMessageHandler("PositionUpdate", OnRecievePositionUpdate);
        }
        else
        {
            RegisterMessageHandler("SetClientPosition", OnSetClientPosition);
        }
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;
        if(Time.time - lastSentTime > (1f / PosUpdatesPerSecond))
        {
            using(MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(transform.position.x);
                    writer.Write(transform.position.y);
                    writer.Write(transform.position.z);
                }
                SendToServer("PositionUpdate", "PositionUpdates", stream.ToArray());
            }
            lastSentTime = Time.time;
        }
        transform.Translate(new Vector3(Input.GetAxis("Horizontal") * Time.deltaTime, Input.GetAxis("Vertical") * Time.deltaTime, 0));
    }

    //This gets called on all clients except the one the position update is about.
    void OnSetClientPosition(int clientId, byte[] data)
    {
        using (MemoryStream stream = new MemoryStream(data))
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                uint networkId = reader.ReadUInt32();
                if (networkId != objectNetworkId)
                    return;
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                GetNetworkedObject(networkId).transform.position = new Vector3(x, y, z);
            }
        }
    }

    //This gets called on the server when a client sends it's position.
    void OnRecievePositionUpdate(int clientId, byte[] data)
    {
        //This makes it behave like a HLAPI Command. It's only invoked on the same object that called it.
        if (clientId != ownerClientId)
            return;
        using (MemoryStream readStream = new MemoryStream(data))
        {
            using (BinaryReader reader = new BinaryReader(readStream))
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                transform.position = new Vector3(x, y, z);
            }
            using (MemoryStream writeStream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(writeStream))
                {
                    writer.Write(objectNetworkId);
                    writer.Write(transform.position.x);
                    writer.Write(transform.position.y);
                    writer.Write(transform.position.z);
                }
                //Sends the position to all clients except the one who requested it. Similar to a Rpc with a if(isLocalPlayer) return;
                SendToNonLocalClients("SetClientPosition", "PositionUpdates", writeStream.ToArray());
            }
        }
    }
}
