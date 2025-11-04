using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GameServer
{
    public abstract class RedisCmdBase
    {
        protected RedisKey _key { get; set; }
        protected RedisValue _value { get; set; }
        protected Action<string> _callBack { get; set; }

        public abstract Task Execute(IDatabase db);
        public void ExcuteCallBack()
        {
            _callBack?.Invoke(_value);
        }

        protected RedisCmdBase(RedisKey key, Action<string> callback = null)
        {
            _key = key;
           _callBack = callback; 
        }
    }

    public class RedisSetCmd : RedisCmdBase
    {
        public RedisSetCmd(RedisKey key, RedisValue val, Action<string> callback = null) : base(key, callback)
        {
            _value = val;
        }

        public override async Task Execute(IDatabase db)
        {
            var res = await db.StringSetAsync(_key, _value);
            
            if (res)
                ExcuteCallBack();
        }
    }

    public class RedisGetCmd : RedisCmdBase
    {
        public RedisGetCmd(RedisKey key, Action<string> callback = null) : base(key, callback)
        {
        }

        public override async Task Execute(IDatabase db)
        {
            string value = await db.StringGetAsync(_key);
            if (value == null)
                return;
            
            _value = value;
            ExcuteCallBack();
        }
    }
}
