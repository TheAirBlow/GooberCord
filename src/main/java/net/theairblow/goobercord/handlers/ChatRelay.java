package net.theairblow.goobercord.handlers;

import net.minecraft.client.Minecraft;
import net.minecraft.client.network.NetHandlerPlayClient;
import net.minecraft.network.NetworkManager;
import net.minecraftforge.client.event.ClientChatEvent;
import net.minecraftforge.event.ServerChatEvent;
import net.minecraftforge.fml.common.eventhandler.EventPriority;
import net.minecraftforge.fml.common.eventhandler.SubscribeEvent;
import net.minecraftforge.fml.common.network.FMLNetworkEvent;
import net.theairblow.goobercord.Configuration;
import net.theairblow.goobercord.GooberCord;
import net.theairblow.goobercord.api.ChatSocket;

import java.util.Objects;

public class ChatRelay {
    @SubscribeEvent(priority = EventPriority.LOWEST)
    public static void onSendChat(ClientChatEvent event) {
        final Minecraft minecraft = Minecraft.getMinecraft();
        final String message = event.getOriginalMessage();
        if (message.toLowerCase().startsWith(Configuration.prefix)) return;
        event.setCanceled(true);
        minecraft.ingameGUI.getChatGUI().addToSentMessages(message);
        ChatSocket.local(message);
    }

    @SubscribeEvent()
    public static void onReceiveChat(ServerChatEvent event) {
        final Minecraft minecraft = Minecraft.getMinecraft();
        GooberCord.LOGGER.info("<{}> {}", event.getPlayer().getName(), event.getMessage());
        ChatSocket.global(event.getMessage());
    }

    @SubscribeEvent()
    public static void onServerJoin(FMLNetworkEvent.ClientConnectedToServerEvent event) {
        if (event.isLocal()) return;
        final Minecraft minecraft = Minecraft.getMinecraft();
        final NetHandlerPlayClient handler = Objects.requireNonNull(minecraft.getConnection());
        ChatSocket.join(handler.getNetworkManager().getRemoteAddress().toString());
    }

    @SubscribeEvent()
    public static void onServerLeave(FMLNetworkEvent.ClientDisconnectionFromServerEvent event) {
        ChatSocket.leave();
    }
}
