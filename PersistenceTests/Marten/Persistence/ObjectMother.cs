﻿using Wolverine;
using Wolverine.Transports;

namespace PersistenceTests.Marten.Persistence;

public static class ObjectMother
{
    public static Envelope Envelope()
    {
        return new Envelope
        {
            Data = new byte[] { 1, 2, 3, 4 },
            MessageType = "Something",
            Destination = TransportConstants.ScheduledUri,
            ContentType = "application/json"
        };
    }
}