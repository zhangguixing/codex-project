package com.zhuoyouquan.config;

import lombok.Data;
import org.springframework.boot.context.properties.ConfigurationProperties;

@Data
@ConfigurationProperties(prefix = "app.wechat")
public class WechatProperties {
    private String appId;
    private String appSecret;
    private boolean mockLogin = true;
}
