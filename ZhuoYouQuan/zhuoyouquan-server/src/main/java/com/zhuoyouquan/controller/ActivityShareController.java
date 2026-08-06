package com.zhuoyouquan.controller;

import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.common.Result;
import com.zhuoyouquan.mapper.ActivityMapper;
import com.zhuoyouquan.service.ActivityShareCodeService;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.Map;

@RestController
@RequestMapping("/api/activities/{activityId}/share-code")
public class ActivityShareController {
    private final ActivityMapper activities;
    private final ActivityShareCodeService codes;

    public ActivityShareController(ActivityMapper activities, ActivityShareCodeService codes) {
        this.activities = activities;
        this.codes = codes;
    }

    @GetMapping
    public Result<Map<String, String>> create(@PathVariable Long activityId) {
        if (activities.selectById(activityId) == null) throw new BizException("活动不存在");
        return Result.ok(Map.of("imageBase64", codes.create(activityId)));
    }
}
