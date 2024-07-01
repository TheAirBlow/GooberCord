package net.theairblow.goobercord.api;

import com.google.common.collect.Lists;
import com.google.gson.Gson;
import com.google.gson.JsonArray;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import net.minecraft.client.Minecraft;
import net.minecraft.util.Session;
import net.theairblow.goobercord.Configuration;
import net.theairblow.goobercord.GooberCord;
import org.apache.commons.codec.digest.DigestUtils;
import org.apache.http.NameValuePair;
import org.apache.http.client.entity.UrlEncodedFormEntity;
import org.apache.http.client.methods.*;
import org.apache.http.client.utils.URIBuilder;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.impl.client.HttpClients;
import org.apache.http.message.BasicNameValuePair;

import java.io.InputStreamReader;
import java.io.Reader;
import java.net.URI;
import java.util.ArrayList;
import java.util.List;

public class GooberAPI {
    private static final CloseableHttpClient client = HttpClients.createDefault();
    private static String token;

    public static Boolean auth() {
        if (token != null) return true;
        final Minecraft minecraft = Minecraft.getMinecraft();
        final Session session = minecraft.getSession();
        try {
            // Forgive me, I don't know how to do HTTP in java that isn't ugly as FUCK
            // Not like anyone other than me will read or want to modify this anyway
            URI uri = new URIBuilder(Configuration.server).setPath("/auth/begin").build();
            HttpPost post = new HttpPost(uri.toString());
            List<NameValuePair> params = new ArrayList<>();
            params.add(new BasicNameValuePair("username",
                    minecraft.getSession().getProfile().getName()));
            post.setEntity(new UrlEncodedFormEntity(params));

            CloseableHttpResponse response = client.execute(post);
            int code = response.getStatusLine().getStatusCode();
            if (code != 200) throw new Exception("Received invalid status code " + code);
            Reader reader = new InputStreamReader(response.getEntity().getContent());
            JsonObject obj = (JsonObject) new JsonParser().parse(reader);
            token = obj.get("token").getAsString();

            minecraft.getSessionService().joinServer(session.getProfile(),
                    session.getToken(), DigestUtils.sha1Hex(token));

            uri = new URIBuilder(Configuration.server).setPath("/auth/verify").build();
            post = new HttpPost(uri.toString());
            post.setHeader("Authorization", "Bearer " + token);

            response = client.execute(post);
            code = response.getStatusLine().getStatusCode();
            if (code != 200) throw new Exception("Received invalid status code " + code);
            reader = new InputStreamReader(response.getEntity().getContent());
            obj = (JsonObject) new JsonParser().parse(reader);
            token = obj.get("token").getAsString();

            return true;
        } catch (Exception e) {
            GooberCord.LOGGER.fatal("Failed to authorize", e);
            token = null; return false;
        }
    }

    public static String getToken() {
        return token;
    }

    public static Boolean link(String code) {
        try {
            URI uri = new URIBuilder(Configuration.server).setPath("/discord/link/" + code).build();
            HttpPut req = new HttpPut(uri.toString());
            req.setHeader("Authorization", "Bearer " + token);
            CloseableHttpResponse response = client.execute(req);
            int status = response.getStatusLine().getStatusCode();
            return status == 200;
        } catch (Exception e) {
            return false;
        }
    }

    public static Boolean unlink(String code) {
        try {
            URI uri = new URIBuilder(Configuration.server).setPath("/discord/link/" + code).build();
            HttpDelete req = new HttpDelete(uri.toString());
            req.setHeader("Authorization", "Bearer " + token);
            CloseableHttpResponse response = client.execute(req);
            int status = response.getStatusLine().getStatusCode();
            return status == 200;
        } catch (Exception e) {
            return false;
        }
    }

    public static Link[] links() {
        try {
            URI uri = new URIBuilder(Configuration.server).setPath("/discord/links").build();
            HttpGet req = new HttpGet(uri.toString());
            req.setHeader("Authorization", "Bearer " + token);
            CloseableHttpResponse response = client.execute(req);
            int status = response.getStatusLine().getStatusCode();
            if (status != 200) throw new Exception("Received invalid status code " + status);
            Reader reader = new InputStreamReader(response.getEntity().getContent());
            return new Gson().fromJson(reader, Link[].class);
        } catch (Exception e) {
            return new Link[] { };
        }
    }

    public class Link {
        public String code;
        public Long userId;
        public Long guildId;
        public String uuid;
        public String expireAt;
    }
}
