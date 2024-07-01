package net.theairblow.goobercord.handlers;

import net.minecraft.client.Minecraft;
import net.minecraftforge.client.event.ClientChatEvent;
import net.minecraftforge.fml.common.eventhandler.EventPriority;
import net.minecraftforge.fml.common.eventhandler.SubscribeEvent;
import net.theairblow.goobercord.Configuration;
import net.theairblow.goobercord.GooberCord;
import net.theairblow.goobercord.api.GooberAPI;

public class Commands {
    @SubscribeEvent(priority = EventPriority.HIGHEST)
    public static void onChat(ClientChatEvent event) {
        final Minecraft minecraft = Minecraft.getMinecraft();
        final String message = event.getOriginalMessage();
        if (!message.toLowerCase().startsWith("!gc")) return;
        event.setCanceled(true);
        minecraft.ingameGUI.getChatGUI().addToSentMessages(message);
        final String[] args = message.split(" ");
        if (args.length == 1) {
            GooberCord.send("§cGooberCord commands:\n" +
                "§2!gc link [code] - §3Links Minecraft account to Discord\n" +
                "§2!gc unlink [code] - §3Unlinks Minecraft account from Discord\n" +
                "§2!gc links - §3Lists all linked Discord accounts/guilds\n\n" +
                "§6Current global chat prefix: §r'%s'", Configuration.prefix);
            return;
        }

        switch (args[1]) {
            case "link": {
                if (args.length < 3) {
                    GooberCord.sendPrefix("§6Usage: §c!gc link [code]");
                    return;
                }

                if (GooberAPI.link(args[2])) {
                    GooberCord.sendPrefix("§2Successfully linked this account to Discord!");
                    return;
                }

                GooberCord.sendPrefix("§4This code either does not exist, expired or was used!");
                break;
            }
            case "unlink": {
                if (args.length < 3) {
                    GooberCord.sendPrefix("§6Usage: §c!gc unlink [code]");
                    return;
                }

                if (GooberAPI.unlink(args[2])) {
                    GooberCord.sendPrefix("§2Successfully unlinked this account from Discord!");
                    return;
                }

                GooberCord.sendPrefix("§4This code either does not exist or wasn't used by you!");
                break;
            }
            case "links": {
                GooberAPI.Link[] links = GooberAPI.links();
                if (links.length == 0) {
                    GooberCord.sendPrefix("§4You haven't used any linking codes!");
                    return;
                }

                GooberCord.send("§c====== §2Linked Accounts§c ======");
                for (GooberAPI.Link link : links)
                    GooberCord.send("§3-> §a'%s'\n  §3^ §aUser: %s, guild: %s",
                        link.code, link.userId, link.guildId);
                break;
            }
            default:
                GooberCord.sendPrefix("§4Unknown command, type !gc for help.");
                break;
        }
    }
}
