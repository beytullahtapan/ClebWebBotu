using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MessageSend : ApplicationCommandModule
{
    [SlashCommand("SendMessage", "Göndermek istediğiniz mesaj")]
    [RequireOwner]
    public async Task SendMessage(InteractionContext ctx,
        [Option("message", "Göndermek İstediğiniz Mesaj")] string message,
        [Option("channel", "Göndermek İstediğiniz Kanal")] DiscordChannel channelId,
        [Option("pin", "Mesaj sabitlenecek mi? (YES/NO)")] bool pin,
        [Option("etiket", "Mesajda etiket olacak mı? (YES/NO)")] bool etiket)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("İşlem Yapılıyor.."));

        if (channelId == null)
        {
            await ctx.Channel.SendMessageAsync("Belirtilen kanal bulunamadı.");
            return;
        }

        if (etiket)
        {
            if(pin)
            {
                var send = await channelId.SendMessageAsync(message + "\n @everyone");
                await send.PinAsync();
            }
            await channelId.SendMessageAsync(message + "\n @everyone");
        }
        else
        {
            if(pin)
            {
                var send = await channelId.SendMessageAsync(message);
                await send.PinAsync();
            }
            await channelId.SendMessageAsync(message);
        }
       

      
    }
}


