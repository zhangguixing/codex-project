package com.zhuoyouquan.controller;

import com.zhuoyouquan.common.ApiConstants;
import com.zhuoyouquan.common.Result;
import com.zhuoyouquan.dto.FavoriteRequest;
import com.zhuoyouquan.service.FavoriteService;
import io.swagger.v3.oas.annotations.Operation;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/favorites")
public class FavoriteController {
    private final FavoriteService favorites;

    public FavoriteController(FavoriteService favorites) {
        this.favorites = favorites;
    }

    @PostMapping
    @Operation(summary = "����ղ�")
    public Result<Void> add(HttpServletRequest r, @Valid @RequestBody FavoriteRequest q) {
        favorites.add(uid(r), q);
        return Result.ok(null);
    }

    @DeleteMapping
    @Operation(summary = "ȡ���ղ�")
    public Result<Void> remove(HttpServletRequest r, @RequestParam String targetType, @RequestParam Long targetId) {
        favorites.remove(uid(r), targetType, targetId);
        return Result.ok(null);
    }

    private Long uid(HttpServletRequest r) {
        return (Long) r.getAttribute(ApiConstants.USER_ID);
    }
}
