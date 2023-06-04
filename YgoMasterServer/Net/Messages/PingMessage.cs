﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace YgoMaster.Net.Messages
{
    class PingMessage : NetMessage
    {
        public DateTime RequestTime;
        public DuelRoomTableState DuelingState;

        public override void Read(BinaryReader reader)
        {
            RequestTime = Utils.ConvertEpochTime(reader.ReadInt64());
            DuelingState = (DuelRoomTableState)reader.ReadByte();
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(Utils.GetEpochTime(RequestTime));
            writer.Write((byte)DuelingState);
        }
    }
}
