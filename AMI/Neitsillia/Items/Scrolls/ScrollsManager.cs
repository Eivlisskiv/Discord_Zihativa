using AMI.Handlers;
using AMI.Methods;
using AMI.Neitsillia.Items.ItemPartials;
using AMI.Neitsillia.User.PlayerPartials;
using Discord;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace AMI.Neitsillia.Items.Scrolls
{
    public static class ScrollsManager
    {
        private static Dictionary<string, string>[] definitions = new Dictionary<string, string>[]
        {
            new Dictionary<string, string>()
            {
                { "Scroll Of Homecoming", "Teleports you back to the last safe area you visited." }
            }
        };

        private static Dictionary<string, Func<Player, int, IMessageChannel, Task>> functions
            = new Dictionary<string, Func<Player, int, IMessageChannel, Task>>();

        private static bool VerifyFunction(string name, out Func<Player, int, IMessageChannel, Task> func)
        {
            if (!functions.TryGetValue(name, out func))
            {
                Type scrolls = typeof(Scrolls);
                MethodInfo method = scrolls.GetMethod(name.Replace(' ', '_'));
                if (method == null) return false;
                func = ToDelegate(name, method);
                return func != null;
            }

            return true;
        }

        private static Func<Player, int, IMessageChannel, Task> ToDelegate(string name, MethodInfo method)
        {
            try
            {
                Func<Player, int, IMessageChannel, Task> func = (Func<Player, int, IMessageChannel, Task>)
                    method.CreateDelegate(typeof(Func<Player, int, IMessageChannel, Task>));
                functions.Add(name, func);
                return func;
            }
            catch (Exception e)
            {
                Log.LogS(e);
                return null;
            }
        }

        public static Item Load(string name)
        {
            (int tier, string definition) = FindDefinition(name);
            if (tier == -1 || definition == null) return null;
            if (!VerifyFunction(name, out _))
            {
                _ = UniqueChannels.Instance.SendToLog($"{name} is missing loaded method");
                return null;
            }
            return ToItem(name, tier, definition);
        }

        private static (int, string) FindDefinition(string name)
        {
            for(int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i].TryGetValue(name, out string definition))
                    return (i, definition);
            }
            return (-1, null);
        }

        private static Item ToItem(string name, int tier, string definition)
            => new Item(false)
            {
                type = Item.IType.Scroll,
                name = name,
                originalName = name,
                description = definition,
                durability = tier * 100,
            };

        public static async Task Use(Player player, int slot, Item scroll, IMessageChannel channel)
        {
            if(!VerifyFunction(scroll.name, out Func<Player, int, IMessageChannel, Task> func))
            {
                _ = UniqueChannels.Instance.SendToLog($"{scroll.name} is missing loaded method");
                await channel.SendMessageAsync("The scroll failed to produce any effect. (Scroll effect coiuld not be found or was disabled)");
                return;
            }

            await func(player, slot, channel);
        }
    }
}
