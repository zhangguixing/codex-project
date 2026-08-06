package com.zhuoyouquan.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zhuoyouquan.common.ApiConstants;
import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.common.Result;
import com.zhuoyouquan.dto.ReportRequest;
import com.zhuoyouquan.entity.Report;
import com.zhuoyouquan.entity.UserBlock;
import com.zhuoyouquan.mapper.ActivityMapper;
import com.zhuoyouquan.mapper.ReportMapper;
import com.zhuoyouquan.mapper.UserBlockMapper;
import com.zhuoyouquan.mapper.UserMapper;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDateTime;

@RestController
public class SafetyController {
    private final UserBlockMapper blocks;
    private final ReportMapper reports;
    private final UserMapper users;
    private final ActivityMapper activities;

    public SafetyController(UserBlockMapper blocks, ReportMapper reports, UserMapper users, ActivityMapper activities) {
        this.blocks = blocks;
        this.reports = reports;
        this.users = users;
        this.activities = activities;
    }

    @PostMapping("/api/users/{userId}/block")
    public Result<Void> block(HttpServletRequest request, @PathVariable Long userId) {
        Long current = (Long) request.getAttribute(ApiConstants.USER_ID);
        if (current.equals(userId)) throw new BizException("不能拉黑自己");
        if (users.selectById(userId) == null) throw new BizException("用户不存在");
        if (blocks.selectCount(new LambdaQueryWrapper<UserBlock>().eq(UserBlock::getUserId, current).eq(UserBlock::getBlockedUserId, userId)) == 0) {
            UserBlock block = new UserBlock();
            block.setUserId(current);
            block.setBlockedUserId(userId);
            block.setCreatedAt(LocalDateTime.now());
            blocks.insert(block);
        }
        return Result.ok(null);
    }

    @DeleteMapping("/api/users/{userId}/block")
    public Result<Void> unblock(HttpServletRequest request, @PathVariable Long userId) {
        blocks.delete(new LambdaQueryWrapper<UserBlock>().eq(UserBlock::getUserId, (Long) request.getAttribute(ApiConstants.USER_ID)).eq(UserBlock::getBlockedUserId, userId));
        return Result.ok(null);
    }

    @PostMapping("/api/reports")
    public Result<Void> report(HttpServletRequest request, @Valid @RequestBody ReportRequest body) {
        String type = body.getTargetType().trim().toUpperCase();
        if (!"USER".equals(type) && !"ACTIVITY".equals(type)) throw new BizException("仅支持举报用户或活动");
        if (("USER".equals(type) ? users.selectById(body.getTargetId()) : activities.selectById(body.getTargetId())) == null)
            throw new BizException("举报对象不存在");
        Report report = new Report();
        report.setReporterId((Long) request.getAttribute(ApiConstants.USER_ID));
        report.setTargetType(type);
        report.setTargetId(body.getTargetId());
        report.setReason(body.getReason().trim());
        report.setStatus("PENDING");
        report.setCreatedAt(LocalDateTime.now());
        reports.insert(report);
        return Result.ok(null);
    }
}
