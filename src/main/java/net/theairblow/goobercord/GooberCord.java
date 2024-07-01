package net.theairblow.goobercord;

import net.minecraft.client.Minecraft;
import net.minecraft.util.text.TextComponentString;
import net.minecraft.util.text.TextComponentTranslation;
import net.minecraftforge.common.MinecraftForge;
import net.minecraftforge.fml.common.event.FMLPreInitializationEvent;
import net.minecraftforge.fml.common.Mod;
import net.theairblow.goobercord.api.ChatSocket;
import net.theairblow.goobercord.api.GooberAPI;
import net.theairblow.goobercord.handlers.ChatRelay;
import net.theairblow.goobercord.handlers.Commands;
import org.apache.logging.log4j.Logger;

@Mod(modid = GooberCord.MOD_ID, name = GooberCord.MOD_NAME,
        version = GooberCord.VERSION, clientSideOnly = true)
public class GooberCord {
    public static final String MOD_ID = "goobercord";
    public static final String MOD_NAME = "GooberCord";
    public static final String VERSION = "1.0.0";
    public static Logger LOGGER;

    @Mod.EventHandler
    public void preInit(FMLPreInitializationEvent event) {
        MinecraftForge.EVENT_BUS.register(ChatRelay.class);
        MinecraftForge.EVENT_BUS.register(Commands.class);
        LOGGER = event.getModLog();
        if (GooberAPI.auth())
            ChatSocket.start();
    }

    public static void send(String str, Object... objs) {
        final Minecraft minecraft = Minecraft.getMinecraft();
        minecraft.addScheduledTask(() -> minecraft.ingameGUI.getChatGUI()
                .printChatMessage(new TextComponentString(String.format(str, objs))));
    }

    public static void sendPrefix(String str, Object... objs) {
        final Minecraft minecraft = Minecraft.getMinecraft();
        minecraft.addScheduledTask(() -> minecraft.ingameGUI.getChatGUI()
                .printChatMessage(new TextComponentTranslation(
                        "goobercord.local.prefix", String.format(str, objs))));
    }
}
