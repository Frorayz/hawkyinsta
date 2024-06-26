using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using System.IO;
using Instagram_Reels_Bot.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using Instagram_Reels_Bot.Services;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace Instagram_Reels_Bot.Modules
{
    public class TextCommands : ModuleBase
    {
        private readonly IConfiguration _config;

        public TextCommands(IConfiguration config)
        {
            _config = config;
        }

        private string ExtractInstagramUrl(string input)
        {
            var urlMatch = Regex.Match(input, @"https?:\/\/(www\.)?instagram\.com\/[a-zA-Z0-9_\/]+");
            return urlMatch.Success ? urlMatch.Value : null;
        }

        [Command("reel", RunMode = RunMode.Async)]
        public async Task ReelParser([Remainder] string args = null)
        {
            string url = ExtractInstagramUrl(args);
            if (url != null)
                await Responder(url, Context);
        }

        [Command("p", RunMode = RunMode.Async)]
        public async Task PostParser([Remainder] string args = null)
        {
            string url = ExtractInstagramUrl(args);
            if (url != null)
                await Responder(url, Context);
        }

        [Command("tv", RunMode = RunMode.Async)]
        public async Task TVParser([Remainder] string args = null)
        {
            string url = ExtractInstagramUrl(args);
            if (url != null)
                await Responder(url, Context);
        }

        [Command("stories", RunMode = RunMode.Async)]
        public async Task StoryParser([Remainder] string args = null)
        {
            string url = ExtractInstagramUrl(args);
            if (url != null)
                await Responder(url, Context);
        }

        [Command("profile", RunMode = RunMode.Async)]
        public async Task ProfileParser([Remainder] string args = null)
        {
            if (!Whitelist.IsServerOnList(Context.Guild.Id))
                return;

            using (Context.Channel.EnterTypingState())
            {
                InstagramProcessor instagram = new InstagramProcessor(InstagramProcessor.AccountFinder.GetIGAccount());
                string url = ExtractInstagramUrl(args);
                if (url == null)
                    return;

                InstagramProcessorResponse response = await instagram.PostRouter(url, (int)Context.Guild.PremiumTier, 1);

                if (!response.success)
                {
                    await Context.Message.ReplyAsync(response.error);
                    return;
                }

                if (!response.onlyAccountData)
                {
                    await Responder(url, Context);
                    return;
                }

                IGEmbedBuilder embed = new IGEmbedBuilder(response, Context.User.Username);
                IGComponentBuilder component = new IGComponentBuilder(response, Context.User.Id, _config);

                await Context.Message.ReplyAsync(embed: embed.AutoSelector(), allowedMentions: AllowedMentions.None, components: component.AutoSelector());
                DiscordTools.SuppressEmbeds(Context);
            }
        }

        private static async Task Responder(string url, ICommandContext context)
        {
            if (!Whitelist.IsServerOnList(context.Guild.Id))
            {
                return;
            }
            using (context.Channel.EnterTypingState())
            {
                InstagramProcessor instagram = new InstagramProcessor(InstagramProcessor.AccountFinder.GetIGAccount());

                InstagramProcessorResponse response = await instagram.PostRouter(url, (int)context.Guild.PremiumTier, 1);

                if (!response.success)
                {
                    await context.Message.ReplyAsync(response.error);
                    return;
                }

                var _builder = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile(path: "config.json");
                var _config = _builder.Build();

                IGEmbedBuilder embed = (!string.IsNullOrEmpty(_config["DisableTitle"]) && _config["DisableTitle"].ToLower() == "true") ? (new IGEmbedBuilder(response)) : (new IGEmbedBuilder(response, context.User.Username));
                IGComponentBuilder component = new IGComponentBuilder(response, context.User.Id, _config);

                if (response.isVideo)
                {
                    if (response.stream != null)
                    {
                        using (Stream stream = new MemoryStream(response.stream))
                        {
                            FileAttachment attachment = new FileAttachment(stream, "HawkyMedia.mp4", "A HawkyInsta Video");
                            await context.Message.Channel.SendFileAsync(attachment, embed: embed.AutoSelector(), components: component.AutoSelector());
                        }
                    }
                    else
                    {
                        await context.Message.ReplyAsync(response.contentURL.ToString(), embed: embed.AutoSelector(), allowedMentions: AllowedMentions.None, components: component.AutoSelector());
                    }
                }
                else
                {
                    if (response.stream != null)
                    {
                        using (Stream stream = new MemoryStream(response.stream))
                        {
                            FileAttachment attachment = new FileAttachment(stream, "HawkyMedia.jpg", "A HawkyInsta Picture");
                            await context.Channel.SendFileAsync(attachment, embed: embed.AutoSelector(), components: component.AutoSelector());
                        }
                    }
                    else
                    {
                        await context.Message.ReplyAsync(embed: embed.AutoSelector(), allowedMentions: AllowedMentions.None, components: component.AutoSelector());
                    }
                }

                DiscordTools.SuppressEmbeds(context);
            }
        }
    }
}
