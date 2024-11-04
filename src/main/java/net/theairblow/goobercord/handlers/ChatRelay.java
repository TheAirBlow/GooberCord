package net.theairblow.goobercord.handlers;

import net.minecraft.client.Minecraft;
import net.minecraft.client.gui.GuiChat;
import net.minecraft.client.network.NetHandlerPlayClient;
import net.minecraft.util.text.TextComponentString;
import net.minecraft.util.text.TextComponentTranslation;
import net.minecraftforge.client.event.ClientChatEvent;
import net.minecraftforge.client.event.ClientChatReceivedEvent;
import net.minecraftforge.fml.client.FMLClientHandler;
import net.minecraftforge.fml.common.eventhandler.EventPriority;
import net.minecraftforge.fml.common.eventhandler.SubscribeEvent;
import net.minecraftforge.fml.common.gameevent.InputEvent;
import net.minecraftforge.fml.common.network.FMLNetworkEvent;
import net.theairblow.goobercord.Configuration;
import net.theairblow.goobercord.GooberCord;
import net.theairblow.goobercord.api.ChatSocket;

import java.net.InetSocketAddress;

public class ChatRelay {
    @SubscribeEvent(priority = EventPriority.LOWEST)
    public static void onSendChat(ClientChatEvent event) {
        final Minecraft minecraft = Minecraft.getMinecraft();
        String message = event.getOriginalMessage();
        if (!message.toLowerCase().startsWith(Configuration.prefix)) return;
        minecraft.ingameGUI.getChatGUI().addToSentMessages(message);
        message = message.substring(1);
        ChatSocket.local(message);
        minecraft.ingameGUI.getChatGUI().printChatMessage(
            new TextComponentTranslation("goobercord.local.message",
                "§6" + minecraft.player.getName(), message));
        event.setCanceled(true);
    }

    @SubscribeEvent()
    public static void onReceiveChat(ClientChatReceivedEvent event) {
        ChatSocket.global(event.getMessage().getUnformattedText());
    }

    @SubscribeEvent()
    public static void onServerJoin(FMLNetworkEvent.ClientConnectedToServerEvent event) {
        if (event.isLocal()) return;
        final NetHandlerPlayClient handler = (NetHandlerPlayClient) FMLClientHandler.instance().getClientPlayHandler();
        final InetSocketAddress address = (InetSocketAddress) handler.getNetworkManager().getRemoteAddress();
        ChatSocket.join(address.getAddress().getHostAddress() + ":" + address.getPort());
    }

    @SubscribeEvent()
    public static void onServerLeave(FMLNetworkEvent.ClientDisconnectionFromServerEvent event) {
        ChatSocket.leave();
    }

    @SubscribeEvent()
    public static void onEvent(InputEvent.KeyInputEvent event) {
        final Minecraft minecraft = Minecraft.getMinecraft();
        if (!GooberCord.keybinds[0].isPressed()) return;

        minecraft.displayGuiScreen(new GuiChat(Configuration.prefix));
    }
}
