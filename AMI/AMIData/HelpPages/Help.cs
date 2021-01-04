using AMYPrototype.Commands;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AMI.AMIData.HelpPages
{
    partial class Help
    {
        private static ReflectionCache<Help> reflectionCache = new ReflectionCache<Help>();

        private static List<string> _categories;
        internal static List<string> Categories
        {
            get
            {
                if (_categories == null)
                {
                    List<PropertyInfo> fields = new List<PropertyInfo>(typeof(Help).GetProperties());
                    _categories = new List<string>(fields.Where(f => f.Name.StartsWith("H_")).Select(f => f.Name.Substring(2, f.Name.Length - 2)));
                }

                return _categories;
            }
        }

        public static string GetName(string name)
        {
            Help h = new Help(name:name, _try:false);
            return h.embed == null ? null : h.name;
        }

        readonly string prefix;

        internal string name;
        internal Embed embed;

        private Help(int raw) { }

        public Help(string prefix = "~", string name = "main", bool _try = true)
        {
            this.prefix = prefix;
            this.name = name.ToLower();
            embed = reflectionCache.GetProperty<Embed>($"H_{this.name}", this) ?? (_try ? H_help : null); 
        }

        Embed Embed(params EmbedFieldBuilder[] fields) => Embed($"Welcome to the new help interface! ", fields);

        Embed Embed(string message, params EmbedFieldBuilder[] fields) => DUtils.BuildEmbed("Help",
            message
            + Environment.NewLine + $"For a list of all commands, use the `{prefix}module` command."
            + Environment.NewLine + $"For details on a specific command, use `{prefix}chelp {{command name}}`."
            + Environment.NewLine + $"Get an invite to the support server with the `{prefix}support` command",
            null, Color.DarkRed, fields
        ).Build();

        //Generals
        public Embed H_help => Embed($"Help category {name} could not be found", AllHelps);
        public Embed H_server => Embed(Basics_Server);

        EmbedFieldBuilder AllHelps => DUtils.NewField("All available help categories",
            $"Type `{prefix}help <category>` to view the commands help for the indicated category." + Environment.NewLine
            + $"`{string.Join("`, `", Categories)}`"
            );

        EmbedFieldBuilder Basics_Server => DUtils.NewField("Server Basics",
            $"`{prefix}prefix` **Change the prefix**" + Environment.NewLine
            + $"`{prefix}setchannel` **Assign channels**" + Environment.NewLine
            + $"`{prefix}help` **Get other commands help**" + Environment.NewLine
        );
    }
}
