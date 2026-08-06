package com.zhuoyouquan.controller;

import com.zhuoyouquan.common.Result;
import com.zhuoyouquan.dto.WechatLoginRequest;
import com.zhuoyouquan.service.AuthService;
import com.zhuoyouquan.vo.AuthVO;
import io.swagger.v3.oas.annotations.Operation;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/auth")
public class AuthController {
    private final AuthService auth;

    public AuthController(AuthService auth) {
        this.auth = auth;
    }

    @PostMapping("/wechat-login")
    @Operation(summary = "微信登录")
    public Result<AuthVO> login(@Valid @RequestBody WechatLoginRequest request) {
        return Result.ok(auth.login(request.getCode()));
    }
}
