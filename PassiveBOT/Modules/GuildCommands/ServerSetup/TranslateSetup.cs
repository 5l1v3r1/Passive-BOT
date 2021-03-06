﻿namespace PassiveBOT.Modules.GuildCommands.ServerSetup
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Addons.PrefixService;
    using Discord.Commands;

    using PassiveBOT.Context;
    using PassiveBOT.Models;
    using PassiveBOT.Preconditions;
    using PassiveBOT.Services;

    /// <summary>
    ///     The translate setup module
    /// </summary>
    [Group("TranslateSetup")]
    [Summary("Setup for quick translation via reactions.")]
    [RequireContext(ContextType.Guild)]
    [RequireAdmin]
    public class TranslateSetup : Base
    {
        public TranslateSetup(PrefixService prefixService, TranslationService translateService)
        {
            PrefixService = prefixService;
            _Translate = translateService;
        }

        private TranslationService _Translate { get; }

        private PrefixService PrefixService { get; }

        /// <summary>
        ///     Tutorial on translation command usage
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        [Command("HowTo")]
        [Summary("QuickTranslate Command Help")]
        public Task HowToAsync()
        {
            var pre = PrefixService.GetPrefix(Context.Guild.Id);
            return SimpleEmbedAsync("**Translation Help**\n" + "What is QuickTranslation?\n" + "Quick translation enables you to react to a message with a specific emoji and have it auto-translated into a specific language.\n" + "You can add an Emoji and Set its language using the Add Command and Remove one using the Remove Command\n" + "The Quick Translation system can be toggled using the Toggle Command\n\n" + "**Usage**\n" + $"`{pre}TranslateSetup Add <Emoji> <Language>` - Adds a quick translation configuration\n" + $"`{pre}TranslateSetup Add :flag_us: en` - Reacting with the :flag_us: emoji will translate the message to english\n\n" + $"`{pre}TranslateSetup Remove <Emoji>` - Removes a quick translation configuration\n" + $"`{pre}TranslateSetup Remove :flag_us:` - Removed the custom configuration\n" + $"`{pre}TranslateSetup Toggle` - Toggles on or off Quick Translation via reactions\n" + $"`{pre}TranslateSetup List` - List the Custom Configuration\n" + $"`{pre}TranslateSetup Defaults` - List the Default Configuration\n\n" + "NOTE: For a list of Language Codes\n" + $"`{pre}Translate languages`\n" + "Also, \n" + "for `zh-CN - Chinese(Simplified)` use `zh_CN`\n" + "for `zh-TW - Chinese(Traditional)` use `zh_TW`\n" + "for `is - Icelandic` use `_is`");
        }

        /// <summary>
        ///     The custom emotes list
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        [Command("List")]
        [Summary("List paired languages")]
        public Task ListAsync()
        {
            var fields = Context.Server.Settings.Translate.CustomPairs.Select(x => new EmbedFieldBuilder { Name = x.Language.ToString(), Value = string.Join("\n", x.EmoteMatches), IsInline = true }).ToList();
            var embed = new EmbedBuilder { Fields = fields };
            return ReplyAsync(embed);
        }

        /// <summary>
        ///     The default emotes list
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        [Command("Defaults")]
        [Summary("List Default paired languages")]
        public Task ListDeAsync()
        {
            var fields = LanguageMap.DefaultMap.OrderByDescending(x => x.EmoteMatches.Count).Select(x => new EmbedFieldBuilder { Name = x.Language.ToString(), Value = string.Join("\n", x.EmoteMatches), IsInline = true }).ToList();
            var embed = new EmbedBuilder { Fields = fields };
            return ReplyAsync(embed);
        }

        /// <summary>
        ///     Adds a quick translate pair.
        /// </summary>
        /// <param name="emote">
        ///     The input emote.
        /// </param>
        /// <param name="languageCode">
        ///     The languageCode.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="Exception">
        ///     Throws if a pair is already matched.
        /// </exception>
        [Command("Add")]
        [Summary("Add a pair for quick translation of a language")]
        [Remarks("For a list of language codes use the `Translate Languages` Command")]
        public async Task QuickTranslatePairAsync(Emoji emote, LanguageMap.LanguageCode languageCode)
        {
            var group = Context.Server.Settings.Translate.CustomPairs.FirstOrDefault(x => x.Language == languageCode);
            if (group != null)
            {
                if (group.EmoteMatches.Any(x => x == emote.Name))
                {
                    throw new Exception("Emote already mapped to a language");
                }

                group.EmoteMatches.Add(emote.Name);
            }
            else
            {
                Context.Server.Settings.Translate.CustomPairs.Add(new GuildModel.GuildSetup.TranslateSetup.TranslationSet { EmoteMatches = new List<string> { emote.Name }, Language = languageCode });
            }

            Context.Server.Save();
            await _Translate.UpdateSetupAsync(Context.Guild.Id, Context.Server.Settings.Translate);
            await SimpleEmbedAsync("Pair Added:\n" + $"{emote.Name} => {languageCode}");
        }

        /// <summary>
        ///     Removes a custom quick translation emote.
        /// </summary>
        /// <param name="emote">
        ///     The emote.
        /// </param>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        /// <exception cref="Exception">
        ///     Throws if there is no matching emote.
        /// </exception>
        [Command("Remove")]
        [Summary("Remove a custom emote from QuickTranslation list")]
        public async Task RemoveAsync(Emoji emote)
        {
            var matchingPair = Context.Server.Settings.Translate.CustomPairs.FirstOrDefault(x => x.EmoteMatches.Contains(emote.Name));
            if (matchingPair != null)
            {
                if (matchingPair.EmoteMatches.Count == 1)
                {
                    Context.Server.Settings.Translate.CustomPairs.Remove(matchingPair);
                }
                else
                {
                    matchingPair.EmoteMatches.Remove(emote.Name);
                }
            }
            else
            {
                throw new Exception("No Matching Pair.");
            }

            Context.Server.Save();
            await _Translate.UpdateSetupAsync(Context.Guild.Id, Context.Server.Settings.Translate);
            await SimpleEmbedAsync("Removed.");
        }

        /// <summary>
        ///     Toggle quick translation in the server
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        [Command("Toggle")]
        [Summary("Toggle quick translation for the server")]
        public async Task ToggleAsync()
        {
            Context.Server.Settings.Translate.EasyTranslate = !Context.Server.Settings.Translate.EasyTranslate;
            Context.Server.Save();
            await _Translate.UpdateSetupAsync(Context.Guild.Id, Context.Server.Settings.Translate);
            await SimpleEmbedAsync($"Quick Translate Enabled: {Context.Server.Settings.Translate.EasyTranslate}");
        }

        /// <summary>
        ///     toggle dm translations in the server
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        [Command("DM")]
        [Summary("Toggle whether or not Translations will be sent in DMs")]
        public async Task ToggleDMAsync()
        {
            Context.Server.Settings.Translate.DMTranslations = !Context.Server.Settings.Translate.DMTranslations;
            Context.Server.Save();
            await _Translate.UpdateSetupAsync(Context.Guild.Id, Context.Server.Settings.Translate);
            await SimpleEmbedAsync($"DM Quick Translations: {Context.Server.Settings.Translate.DMTranslations}");
        }

        /// <summary>
        ///     The translate setup task.
        /// </summary>
        /// <returns>
        ///     The <see cref="Task" />.
        /// </returns>
        [Command("TranslateSetup", RunMode = RunMode.Async)]
        [Summary("Setup information for the translation setup module")]
        public Task TranslateSetupTaskAsync()
        {
            var translate = Context.Server.Settings.Translate;
            return SimpleEmbedAsync($"Reactions Enabled: {translate.EasyTranslate}\n" + $"DM Translations: {translate.DMTranslations}\n" + "For pairs refer to the `list` and `defaults` commands");
        }
    }
}