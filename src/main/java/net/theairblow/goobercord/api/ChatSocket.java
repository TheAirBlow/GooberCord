package net.theairblow.goobercord.api;

import com.google.gson.JsonArray;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import net.minecraft.client.Minecraft;
import net.minecraft.util.text.TextComponentTranslation;
import net.theairblow.goobercord.Configuration;
import net.theairblow.goobercord.GooberCord;
import org.apache.http.client.utils.URIBuilder;
import org.java_websocket.client.WebSocketClient;
import org.java_websocket.handshake.ServerHandshake;

import java.net.URI;
import java.net.URISyntaxException;

public class ChatSocket extends WebSocketClient {
    private static ChatSocket socket;
    private static boolean joined;

    private ChatSocket(URI uri) {
        super(uri);
        addHeader("Authorization", "Bearer " + GooberAPI.getToken());
    }

    @Override
    public void onOpen(ServerHandshake data) {
        GooberCord.LOGGER.info("[ChatSocket] Successfully opened new connection");
    }

    @Override
    public void onClose(int code, String reason, boolean remote) {
        GooberCord.LOGGER.warn("[ChatSocket] Connection was closed, reconnecting...");
        reconnect();
    }

    @Override
    public void onMessage(String message) {
        JsonObject json = new JsonParser().parse(message).getAsJsonObject();
        JsonArray args = json.get("args").getAsJsonArray();
        int type = json.get("type").getAsInt();
        switch (type) {
            case 0: // Error
                GooberCord.LOGGER.error("[ChatSocket] Received error {}", args.get(0).getAsString());
                break;
            case 1: // Ack
                break;
            case 5: // Local
                Minecraft minecraft = Minecraft.getMinecraft();
                minecraft.addScheduledTask(() -> {
                    String key = "goobercord.local.message";
                    if (args.size() > 2) {
                        minecraft.ingameGUI.getChatGUI().printChatMessage(
                            new TextComponentTranslation("goobercord.local.replying_to", args.get(2).getAsString()));
                        key = "goobercord.local.newline";
                    }

                    String username = args.get(0).getAsString();
                    for (String line : args.get(1).getAsString().split("\n")) {
                        minecraft.ingameGUI.getChatGUI().printChatMessage(
                                new TextComponentTranslation(key, username, line));
                        key = "goobercord.local.newline";
                    }
                });
                break;
            default:
                GooberCord.LOGGER.warn("[ChatSocket] Received invalid type {}", type);
                break;
        }
    }

    @Override
    public void onError(Exception e) {
        GooberCord.LOGGER.error("[ChatSocket] Caught exception", e);
        close();
    }

    public static void start() {
        if (socket != null) return;
        try {
            URI uri = new URI(Configuration.server);
            uri = new URIBuilder(uri)
                .setScheme(uri.getScheme().equals("https") ? "wss" : "ws")
                .setPath("/chat/ws").build();
            socket = new ChatSocket(uri);
            socket.connect();
        } catch (URISyntaxException e) {
            GooberCord.LOGGER.fatal("[ChatSocket] Invalid URI specified", e);
        }
    }

    public static void join(String server) {
        if (joined) leave();
        send(2, server);
        joined = true;
    }

    public static void leave() {
        if (!joined) return;
        send(3);
        joined = false;
    }

    public static void global(String message) {
        send(4, message);
    }

    public static void local(String message) {
        send(5, message);
    }

    private static void send(int type, String... args) {
        if (socket == null || !socket.isOpen()) return;
        JsonObject obj = new JsonObject();
        obj.addProperty("type", type);
        JsonArray arr = new JsonArray();
        for (String arg : args)
            arr.add(arg);
        obj.add("args", arr);
        socket.send(obj.toString());
    }
}
