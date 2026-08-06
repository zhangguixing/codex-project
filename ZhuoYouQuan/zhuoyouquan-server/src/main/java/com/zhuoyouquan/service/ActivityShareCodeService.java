package com.zhuoyouquan.service;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.config.WechatProperties;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestClient;

import java.time.Instant;
import java.util.Base64;
import java.util.Map;

@Service
public class ActivityShareCodeService {
    private final WechatProperties wechat;
    private final ObjectMapper mapper;
    private volatile String accessToken;
    private volatile Instant tokenExpiresAt = Instant.EPOCH;

    public ActivityShareCodeService(WechatProperties wechat, ObjectMapper mapper) {
        this.wechat = wechat;
        this.mapper = mapper;
    }

    public String create(Long activityId) {
        if (wechat.getAppId() == null || wechat.getAppId().isBlank() || wechat.getAppSecret() == null || wechat.getAppSecret().isBlank())
            throw new BizException("微信小程序凭证未配置，无法生成太阳码");
        try {
            byte[] image = RestClient.create().post()
                    .uri("https://api.weixin.qq.com/wxa/getwxacodeunlimit?access_token={token}", token())
                    .body(Map.of("scene", "id=" + activityId, "page", "pages/detail/index", "check_path", false))
                    .retrieve().body(byte[].class);
            if (image == null || image.length < 100) throw new BizException("太阳码生成失败");
            String text = new String(image);
            if (text.startsWith("{")) {
                JsonNode error = mapper.readTree(text);
                throw new BizException("太阳码生成失败：" + error.path("errmsg").asText());
            }
            return Base64.getEncoder().encodeToString(image);
        } catch (BizException e) {
            throw e;
        } catch (Exception e) {
            throw new BizException("微信太阳码服务不可用");
        }
    }

    private synchronized String token() throws Exception {
        if (accessToken != null && Instant.now().isBefore(tokenExpiresAt)) return accessToken;
        String body = RestClient.create().get().uri("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={id}&secret={secret}", wechat.getAppId(), wechat.getAppSecret()).retrieve().body(String.class);
        JsonNode node = mapper.readTree(body);
        if (node.has("errcode") || node.path("access_token").asText().isBlank())
            throw new BizException("获取微信凭证失败：" + node.path("errmsg").asText());
        accessToken = node.path("access_token").asText();
        tokenExpiresAt = Instant.now().plusSeconds(Math.max(60, node.path("expires_in").asLong(7200) - 300));
        return accessToken;
    }
}
