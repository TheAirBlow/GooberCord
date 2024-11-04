package net.theairblow.goobercord;

import net.minecraftforge.common.config.Config;

@Config(modid = "goobercord")
public class Configuration {
    @Config.Comment("GooberCord server's REST API endpoint")
    @Config.Name("Server URL")
    public static String server = "https://gc.sussy.dev/";

    @Config.Comment("Discord message prefix")
    @Config.Name("Local Prefix")
    public static String prefix = "~";
}
