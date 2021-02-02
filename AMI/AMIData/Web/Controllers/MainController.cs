using AMI.Methods;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AMI.AMIData.Web.Controllers
{
    abstract public class MainController<T> : ControllerBase
    {
        public bool Authenticate()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var tokenValue))
                return false;
            Log.LogS("request token recieved " + tokenValue);
            return true;

        }

        internal abstract T PrivitizeObject(T obj);
        internal T[] PrivitizeObjects(T[] items)
        {
            for(int i = 0; i < items.Length; i++)
                items[i] = PrivitizeObject(items[i]);

            return items;
        }

        public async Task ToJson(T item)
            => await Json(PrivitizeObject(item));

        public async Task ToJson(T[] item)
            => await Json(PrivitizeObjects(item));

        public async Task Json(object obj)
            => await JsonWriteAsync(Utils.JSON(obj));

        public async Task JsonWriteAsync(string json)
        {
            Response.Headers.Add("Content-Type", "application/json");
            await Response.WriteAsync(json);
        }

        public async Task<bool> NullError(T item)
        {
            if (item == null)
            {
                await Error("Item not found");
                return true;
            }

            return false;
        }

        public async Task Error(string error)
         => await Json(new Dictionary<string, string>() { { "error", error } });
    }
}
