package com.zhuoyouquan.config;

import com.zhuoyouquan.common.ApiConstants;
import com.zhuoyouquan.common.BizException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.stereotype.Component;
import org.springframework.web.servlet.HandlerInterceptor;

@Component
public class AuthInterceptor implements HandlerInterceptor {
    private final JwtService jwtService;

    public AuthInterceptor(JwtService jwtService) {
        this.jwtService = jwtService;
    }

    @Override
    public boolean preHandle(HttpServletRequest req, HttpServletResponse res, Object handler) {
        String uri = req.getRequestURI();
        String h = req.getHeader("Authorization");
        if ("GET".equalsIgnoreCase(req.getMethod()) && uri.matches(".*/api/activities(?:/\\d+)?") && (h == null || !h.startsWith("Bearer ")))
            return true;
        if (h == null || !h.startsWith("Bearer ")) throw new BizException("请先登录");
        try {
            req.setAttribute(ApiConstants.USER_ID, jwtService.parseUserId(h.substring(7)));
            return true;
        } catch (Exception e) {
            throw new BizException("登录已过期，请重新登录");
        }
    }
}
