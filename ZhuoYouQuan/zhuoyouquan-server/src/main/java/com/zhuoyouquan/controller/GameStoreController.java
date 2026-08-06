package com.zhuoyouquan.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.common.Result;
import com.zhuoyouquan.entity.Activity;
import com.zhuoyouquan.entity.GameStore;
import com.zhuoyouquan.mapper.ActivityMapper;
import com.zhuoyouquan.mapper.GameStoreMapper;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/stores")
public class GameStoreController {
    private final GameStoreMapper stores;
    private final ActivityMapper activities;

    public GameStoreController(GameStoreMapper stores, ActivityMapper activities) {
        this.stores = stores;
        this.activities = activities;
    }

    @GetMapping
    public Result<List<GameStore>> list(@RequestParam(required = false) String city) {
        LambdaQueryWrapper<GameStore> q = new LambdaQueryWrapper<GameStore>().eq(GameStore::getStatus, "ACTIVE").orderByDesc(GameStore::getCreatedAt);
        if (city != null && !city.isBlank()) q.eq(GameStore::getCity, city);
        return Result.ok(stores.selectList(q));
    }

    @GetMapping("/{id}")
    public Result<Map<String, Object>> detail(@PathVariable Long id) {
        GameStore store = stores.selectById(id);
        if (store == null || !"ACTIVE".equals(store.getStatus())) throw new BizException("门店不存在");
        List<Activity> sessions = activities.selectList(new LambdaQueryWrapper<Activity>().eq(Activity::getStoreName, store.getName()).in(Activity::getStatus, "OPEN", "FULL").orderByAsc(Activity::getStartTime));
        return Result.ok(Map.of("store", store, "activities", sessions));
    }
}
