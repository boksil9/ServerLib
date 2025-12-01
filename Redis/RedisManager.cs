using ServerLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Threading.Channels;

namespace GameServer
{
    public class RedisManager
    {
        public static RedisManager Instance { get; } = new RedisManager();
        
        private ConnectionMultiplexer? _redis { get; set; }
        private Channel<IRedisCommand> _channel { get; set; }
        private Dictionary<string, Action<RedisChannel, RedisValue>> _subscriptions;

        private IDatabase? _db { get; set; }

        private CancellationTokenSource? _cts;
        private Task? _processTask;

        public bool IsInitialized => _redis != null && _redis.IsConnected && _db != null;

        public async Task<bool> Init(string config)
        {
            if (IsInitialized == true)
                return true;

            _redis = await ConnectionMultiplexer.ConnectAsync(config);
            if(_redis.IsConnected == false)
            {
                Console.WriteLine("Connect to Redis failed!");
                return false;
            }

            _channel = Channel.CreateUnbounded<IRedisCommand>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });   
            _db = _redis.GetDatabase();
            _cts = new CancellationTokenSource();
            _processTask = Task.Run(() => ProcessCmd(_cts.Token));

            return true;
        }

        public async Task ShutDown()
        {
            if (_cts == null)
                return;

            _channel.Writer.Complete();
            _cts.Cancel();

            try
            {
                if (_processTask != null)
                    await _processTask;
            }
            catch (OperationCanceledException) { }

            if(_redis != null)
                await _redis.CloseAsync();
        }

        public ValueTask RunCmd(IRedisCommand cmd)
        {
            if (IsInitialized == false)
                throw new InvalidOperationException("RedisManager not initialized.");

            return _channel.Writer.WriteAsync(cmd);
        }

        private async Task ProcessCmd(CancellationToken ct)
        {
            if (IsInitialized == false)
                throw new InvalidOperationException("RedisManager not initialized.");

            var reader = _channel.Reader;
            try
            {
                while(await reader.WaitToReadAsync(ct))
                {
                    while(reader.TryRead(out var cmd))
                    {
                        try
                        {
                            await cmd.ExecuteAsync(_db);
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine($"[RedisManager] Command excution failed : {e}");
                        }
                    }
                }
            }
            catch(OperationCanceledException) { }
        }

        public async Task Subscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler)
        {
            if(IsInitialized == false)
                throw new InvalidOperationException("RedisManager not initialized.");

            var sub = _redis.GetSubscriber();
            _subscriptions[channel] = handler;

            await sub.SubscribeAsync(channel, (ch, msg) =>
            {
                if (_subscriptions.TryGetValue(channel, out var h))
                    h(ch, msg);
            });
        }
    }
}
