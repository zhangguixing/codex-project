package com.zhuoyouquan.config;

import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.info.Info;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class OpenApiConfig {
    @Bean
    public OpenAPI zhuoyouquanOpenApi() {
        return new OpenAPI().info(new Info().title("桌游�?MVP API").version("v1").description("微信小程序桌游组局服务接口"));
    }
}
