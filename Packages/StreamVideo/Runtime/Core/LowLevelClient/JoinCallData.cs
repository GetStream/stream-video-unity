namespace StreamVideo.Core.LowLevelClient
{
    internal readonly struct JoinCallData
    {
        public readonly StreamCallType Type;
        public readonly string Id;
        public readonly bool Create;
        public readonly bool Ring;
        public readonly bool Notify;
        
        /// <summary>
        /// If the participant is migrating from the SFU
        /// </summary>
        public readonly string MigratingFromSfu;

        //StreamTODO: map all CallRequestInternalDTO fields here and create helper method like ToInternalCallRequest

        public JoinCallData(StreamCallType type, string id, bool create, bool ring, bool notify)
        {
            Type = type;
            Id = id;
            Create = create;
            Ring = ring;
            Notify = notify;
            MigratingFromSfu = string.Empty;
        }
        
        public JoinCallData(StreamCallType type, string id, bool create, bool ring, bool notify, string migratingFromSfu)
        {
            Type = type;
            Id = id;
            Create = create;
            Ring = ring;
            Notify = notify;
            MigratingFromSfu = migratingFromSfu;
        }

        public JoinCallData CloneWithMigratingFromSfu(string migratingFromSfu)
        {
            return new JoinCallData(Type, Id, Create, Ring, Notify, migratingFromSfu);
        }
    }
}