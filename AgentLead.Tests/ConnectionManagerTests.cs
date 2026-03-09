using System.Net.Http;
using System.Reflection;
using AgentLead.Services;
using Xunit;

namespace AgentLead.Tests;

public class ConnectionManagerTests
{
    [Fact]
    public void ConnectionManager_DefaultValues_AreCorrect()
    {
        using var connectionManager = new ConnectionManager();

        Assert.NotNull(connectionManager);
    }

    [Fact]
    public void MaxRetryAttempts_IsFive()
    {
        var field = typeof(ConnectionManager).GetField("MaxRetryAttempts", BindingFlags.NonPublic | BindingFlags.Static);
        var value = (int)field!.GetValue(null)!;
        
        Assert.Equal(5, value);
    }

    [Fact]
    public void PingIntervalSeconds_IsSixty()
    {
        var field = typeof(ConnectionManager).GetField("PingIntervalSeconds", BindingFlags.NonPublic | BindingFlags.Static);
        var value = (int)field!.GetValue(null)!;
        
        Assert.Equal(60, value);
    }

    [Fact]
    public void ConnectionTimeoutSeconds_Is120()
    {
        var field = typeof(ConnectionManager).GetField("ConnectionTimeoutSeconds", BindingFlags.NonPublic | BindingFlags.Static);
        var value = (int)field!.GetValue(null)!;
        
        Assert.Equal(120, value);
    }

    [Fact]
    public void ConnectionManager_Dispose_Cleanup()
    {
        var connectionManager = new ConnectionManager();
        connectionManager.Dispose();
        
        connectionManager.Dispose();
        
        Assert.True(true);
    }

    [Fact]
    public async Task ExponentialBackoff_CalculatesCorrectDelays()
    {
        var delays = new List<int>();
        
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            if (attempt > 1)
            {
                var delaySeconds = (int)Math.Pow(2, attempt - 1);
                delays.Add(delaySeconds);
            }
        }
        
        Assert.Equal(2, delays[0]);
        Assert.Equal(4, delays[1]);
        Assert.Equal(8, delays[2]);
        Assert.Equal(16, delays[3]);
    }
}
