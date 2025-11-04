using ServerLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace GameServer
{
    public class RedisManager
    {
        public static RedisManager Instance { get; } = new RedisManager();
        
        private ConnectionMultiplexer _redis { get; set; }
        private IDatabase _db { get; set; }

        Queue<RedisCmdBase> _cmdQueue = new Queue<RedisCmdBase>();
        private object _lock = new object();

        public bool Init()
        {
            // TODO EndPoint Config
            _redis = ConnectionMultiplexer.Connect(new ConfigurationOptions { EndPoints = { "127.0.01:6379" } });
            _db = _redis.GetDatabase();

            if(_redis.IsConnected == false)
            {
                Console.WriteLine("Connect to Redis failed!");
                return false;
            }

            return true;
        }

        public void PushCmd(RedisCmdBase cmd)
        {
            lock(_lock)
            {
                _cmdQueue.Enqueue(cmd);
            }
        }

        RedisCmdBase PopCmd()
        {
            lock(_lock)
            {
                if(_cmdQueue.Count == 0)
                    return null;

                return _cmdQueue.Dequeue();
            }
        }

        public void RunCmd()
        {
            while (true)
            {
                var cmd = PopCmd();
                if (cmd == null)
                    break;

                cmd.Execute(_db);
            }
        }
    }
}
