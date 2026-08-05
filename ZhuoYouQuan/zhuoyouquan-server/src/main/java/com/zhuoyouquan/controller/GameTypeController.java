package com.zhuoyouquan.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zhuoyouquan.common.Result;
import com.zhuoyouquan.entity.GameType;
import com.zhuoyouquan.mapper.GameTypeMapper;
import io.swagger.v3.oas.annotations.Operation;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping("/api/game-types")
public class GameTypeController {
    private final GameTypeMapper games;

    public GameTypeController(GameTypeMapper games) {
        this.games = games;
    }

    @GetMapping
    @Operation(summary = "可用桌游类型")
    public Result<List<GameType>> list() {
        return Result.ok(games.selectList(new LambdaQueryWrapper<GameType>().eq(GameType::getEnabled, 1).orderByAsc(GameType::getSortOrder)));
    }
}
