﻿namespace PassiveBOT.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    using global::Discord;

    using global::Discord.Commands;

    using global::Discord.WebSocket;

    using Microsoft.Extensions.DependencyInjection;

    using PassiveBOT.Discord.Context;
    using PassiveBOT.Discord.Extensions;
    using PassiveBOT.Discord.TypeReaders;
    using PassiveBOT.Models;

    /// <summary>
    /// The event handler.
    /// </summary>
    public class EventHandler
    {
        /// <summary>
        /// Messages that have already been translated.
        /// </summary>
        private readonly Dictionary<ulong, LanguageMap.LanguageCode> translated = new Dictionary<ulong, LanguageMap.LanguageCode>();

        /// <summary>
        /// true = check and update all missing servers on start.
        /// </summary>
        private bool guildCheck = true;

        /// <summary>
        /// Displays bot invite on connection Once then gets toggled off.
        /// </summary>
        private bool hideInvite;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHandler"/> class.
        /// </summary>
        /// <param name="client">
        /// The client.
        /// </param>
        /// <param name="config">
        /// The config.
        /// </param>
        /// <param name="service">
        /// The service.
        /// </param>
        /// <param name="commandService">
        /// The command service.
        /// </param>
        public EventHandler(DiscordShardedClient client, ConfigModel config, IServiceProvider service, CommandService commandService)
        {
            Client = client;
            Config = config;
            Provider = service;
            CommandService = commandService;
            CancellationToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets the config.
        /// </summary>
        private ConfigModel Config { get; }

        /// <summary>
        /// Gets the provider.
        /// </summary>
        private IServiceProvider Provider { get; }

        /// <summary>
        /// Gets the client.
        /// </summary>
        private DiscordShardedClient Client { get; }

        /// <summary>
        /// Gets the command service.
        /// </summary>
        private CommandService CommandService { get; }

        /// <summary>
        /// Gets or sets the cancellation token.
        /// </summary>
        private CancellationTokenSource CancellationToken { get; set; }

        /// <summary>
        /// The initialize async.
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task InitializeAsync()
        {
            //Ensure that the EmojiTypeReader is initialized so we can parse an emoji as a parameter
            CommandService.AddTypeReader(typeof(Emoji), new EmojiTypeReader());

            // This will add all our modules to the command service, allowing them to be accessed as necessary
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly());
            LogHandler.LogMessage("RavenBOT: Modules Added");
        }

        /// <summary>
        /// Triggers when a shard is ready
        /// </summary>
        /// <param name="socketClient">
        /// The socketClient.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task ShardReady(DiscordSocketClient socketClient)
        {
            await socketClient.SetActivityAsync(new Game($"Shard: {socketClient.ShardId}"));

            /*
            //Here we select at random out 'playing' Message.
             var Games = new Dictionary<ActivityType, string[]>
            {
                {ActivityType.Listening, new[]{"YT/PassiveModding", "Tech N9ne"} },
                {ActivityType.Playing, new[]{$"{Config.Prefix}help"} },
                {ActivityType.Watching, new []{"YT/PassiveModding"} }
            };
            var RandomActivity = Games.Keys.ToList()[Random.Next(Games.Keys.Count)];
            var RandomName = Games[RandomActivity][Random.Next(Games[RandomActivity].Length)];
            await socketClient.SetActivityAsync(new Game(RandomName, RandomActivity));
            LogHandler.LogMessage($"Game has been set to: [{RandomActivity}] {RandomName}");
            Games.Clear();
            */

            if (guildCheck)
            {
                // This will check to ensure that all our servers are initialized, whilst also allowing the bot to continue starting
                _ = Task.Run(() =>
                {
                    // This will load all guild models and retrieve their IDs
                    var Servers = Provider.GetRequiredService<DatabaseHandler>().Query<GuildModel>().Select(x => Convert.ToUInt64(x.ID)).ToList();

                    // Now if the bots server list contains a guild but 'Servers' does not, we create a new object for the guild
                    foreach (var Guild in socketClient.Guilds.Select(x => x.Id))
                    {
                        if (!Servers.Contains(Guild))
                        {
                            Provider.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.CREATE, new GuildModel { ID = Guild }, Guild);
                        }
                    }

                    // We also auto-remove any servers that no longer use the bot, to reduce un-necessary disk usage. 
                    // You may want to remove this however if you are storing things and want to keep them.
                    // You should also disable this if you are working with multiple shards.
                    if (Client.Shards.Count == 1)
                    {
                        foreach (var Server in Servers)
                        {
                            if (!socketClient.Guilds.Select(x => x.Id).Contains(Convert.ToUInt64(Server)))
                            {
                                Provider.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.DELETE, id: Server);
                            }
                        }
                    }

                    // Ensure that this is only run once as the bot initially connects.
                    guildCheck = false;
                });
            }

            LogHandler.LogMessage($"Shard: {socketClient.ShardId} Ready");
            if (!hideInvite)
            {
                LogHandler.LogMessage($"Invite: https://discordapp.com/oauth2/authorize?client_id={Client.CurrentUser.Id}&scope=bot&permissions=2146958591");
                hideInvite = true;
            }
        }

        /// <summary>
        /// Triggers when a shard connects.
        /// </summary>
        /// <param name="socketClient">
        /// The Client.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal Task ShardConnected(DiscordSocketClient socketClient)
        {
            Task.Run(()
                => CancellationToken.Cancel()).ContinueWith(x
                => CancellationToken = new CancellationTokenSource());
            LogHandler.LogMessage($"Shard: {socketClient.ShardId} Connected with {socketClient.Guilds.Count} Guilds and {socketClient.Guilds.Sum(x => x.MemberCount)} Users");
            return Task.CompletedTask;
        }

        /// <summary>
        /// This logs discord messages to our LogHandler
        /// </summary>
        /// <param name="message">
        /// The Message.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal Task Log(LogMessage message)
        {
            return Task.Run(() => LogHandler.LogMessage(message.Message, message.Severity));
        }

        /// <summary>
        /// This will auto-remove the bot from servers as it gets removed. NOTE: Remove this if you want to save configs.
        /// </summary>
        /// <param name="guild">
        /// The guild.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal Task LeftGuild(SocketGuild guild)
        {
            return Task.Run(()
                => Provider.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.DELETE, id: guild.Id));
        }

        /// <summary>
        /// This event is triggered every time the a user sends a Message in a channel, dm etc. that the bot has access to view.
        /// </summary>
        /// <param name="socketMessage">
        /// The socket Message.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task MessageReceivedAsync(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage Message) || Message.Channel is IDMChannel)
            {
                return;
            }

            var context = new Context(Client, Message, Provider);

            if (Config.LogUserMessages)
            {
                //Log user messages if enabled.
                LogHandler.LogMessage(context);
            }

            if (context.User.IsBot)
            {
                //Filter out all bot messages from triggering commands.
                return;
            }

            var argPos = 0;

            // Filter out all messages that don't start with our Bot PrefixSetup, bot mention or server specific PrefixSetup.
            if (!(Message.HasStringPrefix(Config.Prefix, ref argPos) || Message.HasMentionPrefix(context.Client.CurrentUser, ref argPos) || Message.HasStringPrefix(context.Server.Settings.Prefix.CustomPrefix, ref argPos)))
            {
                return;
            }

            // Here we attempt to execute a command based on the user Message
            var result = await CommandService.ExecuteAsync(context, argPos, Provider, MultiMatchHandling.Best);

            // Generate an error Message for users if a command is unsuccessful
            if (!result.IsSuccess)
            {
                var _ = Task.Run(() => CmdError(context, result, argPos));
            }
            else
            {
                if (Config.LogCommandUsages)
                {
                    LogHandler.LogMessage(context);
                }
            }
        }

        /// <summary>
        /// The _client_ reaction added.
        /// </summary>
        /// <param name="Message">
        /// The message.
        /// </param>
        /// <param name="Channel">
        /// The channel.
        /// </param>
        /// <param name="Reaction">
        /// The reaction.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task ReactionAdded(Cacheable<IUserMessage, ulong> Message, ISocketMessageChannel Channel, SocketReaction Reaction)
        {
            LogHandler.LogMessage("Reaction Detected", LogSeverity.Verbose);
            if (!Message.HasValue)
            {
                return;
            }

            try
            {
                if (Message.Value.Author.IsBot || Reaction.User.Value.IsBot || Message.Value.Embeds.Any())
                {
                    return;
                }

                var guild = Provider.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.LOAD, null, (Channel as SocketGuildChannel).Guild.Id.ToString());
                if (!guild.Settings.Translate.EasyTranslate)
                {
                    return;
                }

                //Check custom matches first
                var languageType = guild.Settings.Translate.CustomPairs.FirstOrDefault(x => x.EmoteMatches.Any(val => val == Reaction.Emote.Name));

                if (languageType == null)
                {
                    //If no custom matches, check default matches
                    languageType = LanguageMap.DefaultMap.FirstOrDefault(x => x.EmoteMatches.Any(val => val == Reaction.Emote.Name));
                    if (languageType == null)
                    {
                        return;
                    }
                }

                if (translated.Any(x => x.Key == Reaction.MessageId && x.Value == languageType.Language))
                {
                    return;
                }

                var embed = new EmbedBuilder { Title = "Translate", Color = Color.Blue };
                var original = TextManagement.FixLength(Message.Value.Content);
                var language = TranslateMethods.LanguageCodeToString(languageType.Language);
                var file = TranslateMethods.TranslateMessage(language, Message.Value.Content);
                var response = TextManagement.FixLength(TranslateMethods.HandleResponse(file));
                embed.AddField($"Translated [{language} || {Reaction.Emote}]", $"{response}", true);
                embed.AddField($"Original [{file[2]}]", $"{original}", true);
                embed.AddField("Info", $"Original Author: {Message.Value.Author}\n" + $"Reactor: {Reaction.User.Value}", true);

                if (guild.Settings.Translate.DMTranslations)
                {
                    await Reaction.User.Value.SendMessageAsync("", false, embed.Build());
                }
                else
                {
                    await Channel.SendMessageAsync("", false, embed.Build());
                    translated.Add(Reaction.MessageId, languageType.Language);
                }
            }
            catch (Exception e)
            {
                LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
            }
        }

        /// <summary>
        /// Generates an error Message based on a command error.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="result">
        /// The result.
        /// </param>
        /// <param name="argPos">
        /// The arg pos.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task CmdError(Context context, IResult result, int argPos)
        {
            string errorMessage;
            if (result.Error == CommandError.UnknownCommand)
            {
                errorMessage = "**Command:** N/A";
            }
            else
            {
                // Search the commandservice based on the Message, then respond accordingly with information about the command.
                var search = CommandService.Search(context, argPos);
                var cmd = search.Commands.FirstOrDefault();
                errorMessage = $"**Command Name:** `{cmd.Command.Name}`\n" +
                               $"**Summary:** `{cmd.Command?.Summary ?? "N/A"}`\n" +
                               $"**Remarks:** `{cmd.Command?.Remarks ?? "N/A"}`\n" +
                               $"**Aliases:** {(cmd.Command.Aliases.Any() ? string.Join(" ", cmd.Command.Aliases.Select(x => $"`{x}`")) : "N/A")}\n" +
                               $"**Parameters:** {(cmd.Command.Parameters.Any() ? string.Join(" ", cmd.Command.Parameters.Select(x => x.IsOptional ? $" `<(Optional){x.Name}>` " : $" `<{x.Name}>` ")) : "N/A")}\n" +
                               "**Error Reason**\n" +
                               $"{result.ErrorReason}";
            }

            try
            {
                await context.Channel.SendMessageAsync(string.Empty, false, new EmbedBuilder
                {
                    Title = "ERROR",
                    Description = errorMessage
                }.Build());
            }
            catch
            {
                // ignored
            }

            await LogError(result, context);
        }

        /// <summary>
        /// Logs specified errors based on type.
        /// </summary>
        /// <param name="result">
        /// The result.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        internal async Task LogError(IResult result, Context context)
        {
            switch (result.Error)
            {
                case CommandError.MultipleMatches:
                    if (Config.LogCommandUsages)
                    {
                        LogHandler.LogMessage(context, result.ErrorReason, LogSeverity.Error);
                    }

                    break;
                case CommandError.ObjectNotFound:
                    if (Config.LogCommandUsages)
                    {
                        LogHandler.LogMessage(context, result.ErrorReason, LogSeverity.Error);
                    }

                    break;
                case CommandError.Unsuccessful:
                    await context.Channel.SendMessageAsync("You may have found a bug. Please report this error in my server https://discord.me/Passive");
                    break;
            }
        }
    }
}