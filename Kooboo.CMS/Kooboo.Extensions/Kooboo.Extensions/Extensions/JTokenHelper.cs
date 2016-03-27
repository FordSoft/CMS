using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kooboo.Extensions.Extensions
{
    public static class JTokenHelper
    {
        public static T GetValue<T>(this JToken jToken, string key, T defaultValue = default(T))
        {
            dynamic ret = jToken[key];
            if (ret == null) return defaultValue;
            if (ret is JObject) return JsonConvert.DeserializeObject<T>(ret.ToString());
            return (T)ret;
        }
    }
}
