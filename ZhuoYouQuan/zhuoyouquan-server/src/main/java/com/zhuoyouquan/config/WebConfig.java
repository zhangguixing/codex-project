package com.zhuoyouquan.config;

import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.servlet.config.annotation.InterceptorRegistry;
import org.springframework.web.servlet.config.annotation.ResourceHandlerRegistry;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

@Configuration
@EnableConfigurationProperties({
        JwtProperties.class, WechatProperties.class
}
)
public class WebConfig implements WebMvcConfigurer {
    private final AuthInterceptor interceptor;

    public WebConfig(AuthInterceptor interceptor) {
        this.interceptor = interceptor;
    }

    @Override
    public void addResourceHandlers(ResourceHandlerRegistry registry) {
        registry.addResourceHandler("/uploads/**").addResourceLocations("file:uploads/");
    }

    @Override
    public void addInterceptors(InterceptorRegistry r) {
        r.addInterceptor(interceptor).addPathPatterns("/api/**").excludePathPatterns("/api/auth/**", "/api/game-types", "/swagger-ui/**", "/v3/api-docs/**");
    }
}
