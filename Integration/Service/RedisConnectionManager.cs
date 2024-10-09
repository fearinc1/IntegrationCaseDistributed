using StackExchange.Redis;
using System;

namespace Integration.Service;

public class RedisConnectionManager
{
    private static Lazy<ConnectionMultiplexer> lazyConnection;

    static RedisConnectionManager()
    {
        // Set up the connection to Redis, e.g., localhost or a remote Redis instance
        lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            // Connect to your Redis server, e.g., on localhost:6379
            return ConnectionMultiplexer.Connect("localhost:6379");
        });
    }

    public static ConnectionMultiplexer Connection => lazyConnection.Value;

    public static IDatabase GetDatabase()
    {
        return Connection.GetDatabase();
    }

    public static void CloseConnection()
    {
        if (Connection != null && Connection.IsConnected)
        {
            Connection.Close();
        }
    }
}
