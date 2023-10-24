﻿namespace DistributedKeyValueStore.NET
{
    internal abstract class Message
    {
        public Message() { }
    }
    internal abstract class KeyValueMessage : Message
    {
        public uint Key { get; private set; }
        public string? Value { get; private set; }

        public KeyValueMessage(uint Key, string? value)
        {
            this.Key = Key;
            Value = value;
        }
    }

    internal abstract class KeyMessage : Message
    {
        public uint Key { get; private set; }

        public KeyMessage(uint Key)
        {
            this.Key = Key;
        }
    }

    internal abstract class NodeMessage : Message
    {
        public uint Id { get; private set; }
        public NodeMessage(uint id)
        {
            Id = id;
        }
    }

    internal class ReadMessage : KeyMessage
    {
        public int GetId { get; private set; }
        public ReadMessage(uint Key, int readId) : base(Key)
        {
            GetId = readId;
        }
    }
    internal class ReadResponseMessage : KeyValueMessage
    {
        public int GetId { get; private set; }
        public bool PreWriteBlock { get; private set; }
        public uint Version { get; private set; }
        public ReadResponseMessage(uint key, string? value, int getId, uint version, bool preWriteBlock) : base(key, value)
        {
            GetId = getId;
            Version = version;
            PreWriteBlock = preWriteBlock;
        }

        public ReadResponseMessage(uint key, string? value, int getId) : this(key, value, getId, 0, false)
        { }
    }

    internal class GetMessage : KeyMessage
    {
        public GetMessage(uint Key) : base(Key) { }
    }

    internal class GetResponseMessage : KeyValueMessage
    {
        public bool Timeout { get; private set; }
        public GetResponseMessage(uint Key, string? value) : base(Key, value)
        {
            Timeout = false;
        }

        public GetResponseMessage(uint Key, bool timeout) : base(Key, null)
        {
            Timeout = timeout;
        }
    }

    internal class PreWriteMessage : KeyMessage
    {
        public PreWriteMessage(uint Key) : base(Key) { }
    }

    internal class PreWriteResponseMessage : Message
    {
        public uint Key { get; private set; }
        public bool Result { get; private set; }
        public uint Version { get; private set; }
        public PreWriteResponseMessage(uint key, bool result, uint version)
        {
            this.Key = key;
            this.Result = result;
            this.Version = version;
        }
    }

    internal class WriteMessage : KeyValueMessage
    {
        public uint Version { get; private set; }
        public WriteMessage(uint Key, string? value, uint version) : base(Key, value)
        {
            this.Version = version;
        }
    }

    internal class UpdateMessage : KeyValueMessage
    {
        public UpdateMessage(uint Key, string? value) : base(Key, value) { }
    }

    internal class StartMessage : NodeMessage
    {
        public uint AskNode { get; private set; }
        public StartMessage(uint id, uint askNode) : base(id)
        {
            AskNode = askNode;
        }
    }

    internal class AddNodeMessage : NodeMessage
    {
        public AddNodeMessage(uint id) : base(id)
        {
        }
    }
    internal class RemoveNodeMessage : NodeMessage
    {
        public RemoveNodeMessage(uint id) : base(id)
        {
        }
    }

    internal class GetNodeListMessage : NodeMessage
    {
        public GetNodeListMessage(uint id) : base(id)
        {
        }
    }

    internal class GetNodeListResponseMessage : NodeMessage
    {
        public SortedSet<uint> Nodes { get; private set; }
        public GetNodeListResponseMessage(uint id, SortedSet<uint> nodes) : base(id)
        {
            this.Nodes = nodes;
        }
    }

    internal class TestMessage : Message
    {

    }
}
