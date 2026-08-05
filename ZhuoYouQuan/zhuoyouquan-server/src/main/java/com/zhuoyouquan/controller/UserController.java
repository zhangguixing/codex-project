package com.zhuoyouquan.controller;

import com.zhuoyouquan.common.ApiConstants;
import com.zhuoyouquan.common.Result;
import com.zhuoyouquan.dto.UserProfileRequest;
import com.zhuoyouquan.service.UserService;
import com.zhuoyouquan.vo.UserVO;
import io.swagger.v3.oas.annotations.Operation;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/users")
public class UserController {
    private final UserService users;

    public UserController(UserService users) {
        this.users = users;
    }

    @GetMapping("/me")
    @Operation(summary = "当前用户资料")
    public Result<UserVO> me(HttpServletRequest r) {
        return Result.ok(users.getProfile(userId(r)));
    }

    @PutMapping("/me")
    @Operation(summary = "更新当前用户资料")
    public Result<UserVO> update(HttpServletRequest r, @Valid @RequestBody UserProfileRequest q) {
        return Result.ok(users.updateProfile(userId(r), q));
    }

    private Long userId(HttpServletRequest r) {
        return (Long) r.getAttribute(ApiConstants.USER_ID);
    }
}
