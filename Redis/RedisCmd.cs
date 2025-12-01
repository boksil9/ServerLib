using Google.Protobuf.WellKnownTypes;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GameServer
{
    public interface IRedisCommand
    {
        ValueTask ExecuteAsync(IDatabase db);
    }

    public class RedisStringSetCmd : IRedisCommand
    {
        private readonly RedisKey _key;
        private readonly RedisValue _value;
        private readonly TimeSpan? _expiry;
        private readonly Action<bool>? _callBack;

        public RedisStringSetCmd(RedisKey key, RedisValue val, TimeSpan? expiry = null, Action<bool>? callback = null)
        {
            _key = key;
            _value = val;
            _expiry = expiry;
            _callBack = callback;
        }

        public async ValueTask ExecuteAsync(IDatabase db)
        {
            var res = await db.StringSetAsync(_key, _value, _expiry);
            
            if (res)
                _callBack?.Invoke(res);
        }
    }

    public class RedisStringGetCmd<T> : IRedisCommand
    {
        private readonly RedisKey _key;
        private readonly Func<RedisValue, T> _converter;
        private readonly Action<T>? _callback;

        public RedisStringGetCmd(RedisKey key, Func<RedisValue, T> converter, Action<T>? callback = null)
        {
            _key = key;
            _converter = converter;
            _callback = callback;
        }

        public async ValueTask ExecuteAsync(IDatabase db)
        {
            var value = await db.StringGetAsync(_key);
            if (value.HasValue == false)
            {
                Console.WriteLine($"Redis String Get Failed. Key : {_key.ToString()}");
                return;
            }

            T converted = _converter(value);
            _callback?.Invoke(converted);
        }
    }

    public class RedisHSetCmd<T> : IRedisCommand
    {
        private readonly RedisKey _key;
        private readonly RedisValue _field;
        private readonly RedisValue _value;
        private readonly Action<bool> _callback;

        public RedisHSetCmd(RedisKey key, RedisValue field, RedisValue value, Action<bool>? callback)
        {
            _key = key;
            _field = field;
            _value = value;
            _callback = callback;
        }

        public async ValueTask ExecuteAsync(IDatabase db)
        {
            var res = await db.HashSetAsync(_key, _field, _value);
            if(res)
                _callback.Invoke(res);
        }
    }

    public class RedisHGetCmd<T> : IRedisCommand
    {
        private readonly RedisKey _key;
        private readonly RedisValue _field;
        private readonly Func<RedisValue,T> _converter;
        private readonly Action<T> _callBack;

        public RedisHGetCmd(RedisKey key, RedisValue field, Func<RedisValue, T> converter, Action<T>? callback = null)
        {
            _key = key;
            _field = field;
            _converter = converter;
            _callBack = callback;
        }

        public async ValueTask ExecuteAsync(IDatabase db)
        {
            RedisValue value = await db.HashGetAsync(_key, _field);
            if(value.HasValue == false)
            {
                Console.WriteLine($"Redis Hash Get Failed. Key : {_key.ToString()}, Field : {_field.ToString()}");
                return;
            }

            T converted = _converter(value);
            _callBack.Invoke(converted);
        }
    }
    public class RedisPublishCmd<T> : IRedisCommand
    {
        private readonly RedisChannel _channel;
        private readonly RedisValue _message;
        private readonly Action<long>? _callback;

        public RedisPublishCmd(RedisChannel channel, RedisValue message, Action<long>? callback = null)
        {
            _channel = channel;
            _message = message;
            _callback = callback;
        }

        public async ValueTask ExecuteAsync(IDatabase db)
        {
            var sub = db.Multiplexer.GetSubscriber();
            var res = await sub.PublishAsync(_channel, _message);

            _callback.Invoke(res);
        }
    }
}
