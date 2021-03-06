﻿namespace PassiveBOT.Modules.GuildCommands
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Commands;

    using PassiveBOT.Context;
    using PassiveBOT.Extensions.PassiveBOT;
    using PassiveBOT.Services;

    /// <summary>
    ///     The tags module.
    /// </summary>
    [RequireContext(ContextType.Guild)]
    [Group("Tag")]
    [Alias("Tags")]
    [Summary("Tags are like shortcuts for messages, you can use one to have the bot respond with a specific message that you have pre-set.")]
    public class Tags : Base
    {
        public Tags(TagService service)
        {
            Service = service;
        }

        private TagService Service { get; }

        /// <summary>
        ///     adds a tag
        /// </summary>
        /// <param name="tagName">
        ///     The tagName.
        /// </param>
        /// <param name="tagMessage">
        ///     The tagMessage.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="Exception">
        ///     Throws if a tag exists with the name or if admin check fails
        /// </exception>
        [Command("add")]
        [Summary("adds a tag to the server")]
        public async Task AddTagAsync(string tagName, [Remainder] string tagMessage)
        {
            var t = Service.GetTagSetup(Context.Guild.Id);
            if (t.Enabled)
            {
                if (t.Tags.Any(x => string.Equals(x.Key, tagName, StringComparison.CurrentCultureIgnoreCase)))
                {
                    throw new Exception("There is already a tag with this name in the server. Please delete it then add the new tag.");
                }

                var tg = new TagService.TagSetup.Tag { Name = tagName, Content = tagMessage, CreatorId = Context.User.Id, Creator = $"{Context.User}" };
                t.Tags.TryAdd(tagName.ToLower(), tg);
                t.Save();
                await SimpleEmbedAsync("Tag Added!");
            }
            else
            {
                throw new Exception("Tagging is not enabled in this server!");
            }
        }

        /// <summary>
        ///     Deletes a Tag
        /// </summary>
        /// <param name="tagName">
        ///     The tagName.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="Exception">
        ///     throws if the name is invalid
        /// </exception>
        [Command("del")]
        [Summary("Removes a tag from the server")]
        public async Task DelTagAsync(string tagName)
        {
            var t = Service.GetTagSetup(Context.Guild.Id);
            if (t.Tags.Count > 0)
            {
                t.Tags.TryGetValue(tagName.ToLower(), out var tag);

                if (tag == null)
                {
                    throw new Exception("Invalid Tag Name");
                }

                if (CheckAdmin.IsAdmin(Context))
                {
                    t.Tags.TryRemove(tagName.ToLower(), out _);
                    await SimpleEmbedAsync("Tag Deleted using Admin Permissions");
                }
                else if (tag.CreatorId == Context.User.Id)
                {
                    t.Tags.TryRemove(tagName.ToLower(), out _);
                    await SimpleEmbedAsync("Tag Deleted By Owner");
                }
                else
                {
                    await SimpleEmbedAsync("You do not own this tag");
                }

                t.Save();
            }
            else
            {
                throw new Exception("This server has no tags.");
            }
        }

        /// <summary>
        ///     gets a tag or tag list
        /// </summary>
        /// <param name="tagName">
        ///     The tag name.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        [Command(RunMode = RunMode.Async)]
        [Summary("Gets a tag")]
        [Remarks("Lists all tag names if none provided")]
        public async Task TagAsync(string tagName = null)
        {
            var t = Service.GetTagSetup(Context.Guild.Id);
            if (tagName == null)
            {
                var tags = t.Tags;
                if (tags.Count > 0)
                {
                    var tagList = string.Join(", ", tags.Select(x => x.Value.Name));
                    await ReplyAsync($"**Tags:**\n{tagList}");
                }
                else
                {
                    await ReplyAsync("This server has no tags yet.");
                }
            }
            else
            {
                var embed = new EmbedBuilder();
                if (t.Tags.Count > 0)
                {
                    if (!t.Tags.ContainsKey(tagName.ToLower()))
                    {
                        await ReplyAsync($"No tag with the name **{tagName}** exists.");
                    }
                    else
                    {
                        var tag = t.Tags[tagName.ToLower()];
                        var own = Context.Guild.GetUser(tag.CreatorId);
                        var ownerName = own?.Username ?? tag.Creator;

                        embed.AddField(tag.Name, tag.Content);
                        embed.WithFooter(x => { x.Text = $"Tag Owner: {ownerName} | Uses: {tag.Uses} | Command Invoker: {Context.User.Username}"; });

                        tag.Uses++;
                        t.Save();
                        await ReplyAsync(embed.Build());
                    }
                }
            }
        }
    }
}