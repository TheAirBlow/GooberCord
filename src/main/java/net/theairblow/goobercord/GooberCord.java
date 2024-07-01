package net.theairblow.goobercord;

import net.minecraftforge.common.MinecraftForge;
import net.minecraftforge.fml.common.event.FMLPreInitializationEvent;
import net.minecraftforge.fml.common.Mod;
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
        //MinecraftForge.EVENT_BUS.register(Commands.class);
        LOGGER = event.getModLog();
    }
}
