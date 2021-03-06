﻿namespace PassiveBOT.Models
{
    using System.Collections.Generic;
    using System.IO;

    using Discord;

    /// <summary>
    ///     The object used for initializing and using our database
    /// </summary>
    public class DatabaseObject
    {
        /// <summary>
        ///     The backup folder.
        /// </summary>
        public string BackupFolder => Directory.CreateDirectory("Backup").FullName;

        /// <summary>
        ///     Gets or sets Time period for full backup
        /// </summary>
        public string FullBackup { get; set; } = "0 */6 * * *";

        /// <summary>
        ///     Gets or sets Time period for incremental backup
        /// </summary>
        public string IncrementalBackup { get; set; } = "0 2 * * *";

        /// <summary>
        ///     Gets or sets a value indicating whether the config is created.
        /// </summary>
        public bool IsConfigCreated { get; set; }

        /// <summary>
        ///     Gets or sets The name.
        /// </summary>
        public string Name { get; set; } = "RavenBOT";

        /// <summary>
        ///     Gets or sets the prefix override
        /// </summary>
        public string PrefixOverride { get; set; } = null;

        /// <summary>
        ///     Gets or sets whether to delete server configs for servers which no longer contain the bot
        /// </summary>
        public bool DenyConfigDeletion { get; set; } = false;

        /// <summary>
        ///     Gets or sets The urls.
        /// </summary>
        public List<string> Urls { get; set; } = new List<string>();
        
        /// <summary>
        ///     Gets or sets the proxy to be used for all http client requests
        /// </summary>
        public string ProxyUrl { get; set; } = null;

        /// <summary>
        ///     Gets or sets a value indicating whether the default bot prefix will be overridden
        /// </summary>
        public bool UsePrefixOverride { get; set; } = false;

        public LogSeverity LogSeverity { get; set; } = LogSeverity.Info;

        public bool LogToDatabase { get; set; } = true;
        
        public string TranslateAPIKey { get; set; } = null;

        public bool RunPartner { get; set; } = true;

        public bool RunBirthday { get; set; } = true;

        public bool RunGame { get; set; } = true;

        public bool RunTranslate { get; set; } = true;
    }
}