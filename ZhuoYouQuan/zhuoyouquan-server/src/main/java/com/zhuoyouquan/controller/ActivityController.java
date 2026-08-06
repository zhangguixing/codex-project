package com.zhuoyouquan.controller;

import com.zhuoyouquan.common.ApiConstants;
import com.zhuoyouquan.common.Result;
import com.zhuoyouquan.dto.ActivityCreateRequest;
import com.zhuoyouquan.dto.ActivityMessageRequest;
import com.zhuoyouquan.dto.ActivityQuery;
import com.zhuoyouquan.service.impl.ActivityServiceImpl;
import com.zhuoyouquan.vo.ActivityVO;
import com.zhuoyouquan.vo.PageVO;
import io.swagger.v3.oas.annotations.Operation;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/activities")
public class ActivityController {
    private final ActivityServiceImpl activities;

    public ActivityController(ActivityServiceImpl activities) {
        this.activities = activities;
    }

    @GetMapping
    @Operation(summary = "��б���ɸѡ")
    public Result<PageVO<ActivityVO>> list(HttpServletRequest r, @ModelAttribute ActivityQuery q) {
        return Result.ok(activities.list(q, uidOrNull(r)));
    }

    @GetMapping("/{id}")
    @Operation(summary = "�����")
    public Result<ActivityVO> detail(HttpServletRequest r, @PathVariable Long id) {
        return Result.ok(activities.detail(id, uidOrNull(r)));
    }

    @PostMapping
    @Operation(summary = "�����")
    public Result<ActivityVO> create(HttpServletRequest r, @Valid @RequestBody ActivityCreateRequest q) {
        return Result.ok(activities.create(uid(r), q));
    }

    @PutMapping("/{id}")
    @Operation(summary = "修改活动")
    public Result<ActivityVO> update(HttpServletRequest r, @PathVariable Long id, @Valid @RequestBody ActivityCreateRequest q) {
        return Result.ok(activities.update(uid(r), id, q));
    }

    @PostMapping("/{id}/join")
    @Operation(summary = "�����")
    public Result<Void> join(HttpServletRequest r, @PathVariable Long id) {
        activities.join(uid(r), id);
        return Result.ok(null);
    }

    @DeleteMapping("/{id}/join")
    @Operation(summary = "ȡ������")
    public Result<Void> cancel(HttpServletRequest r, @PathVariable Long id) {
        activities.cancelJoin(uid(r), id);
        return Result.ok(null);
    }

    @PostMapping("/{id}/end")
    @Operation(summary = "结束活动")
    public Result<Void> end(HttpServletRequest r, @PathVariable Long id) {
        activities.end(uid(r), id);
        return Result.ok(null);
    }

    @PostMapping("/{id}/messages")
    @Operation(summary = "给活动发起人留言")
    public Result<Void> leaveMessage(HttpServletRequest r, @PathVariable Long id, @Valid @RequestBody ActivityMessageRequest request) {
        activities.leaveMessage(uid(r), id, request);
        return Result.ok(null);
    }

    @PostMapping("/{id}/participants/{participantId}/check-in")
    @Operation(summary = "发起人确认参与者到场")
    public Result<Void> checkIn(HttpServletRequest r, @PathVariable Long id, @PathVariable Long participantId) {
        activities.checkIn(uid(r), id, participantId);
        return Result.ok(null);
    }

    @PostMapping("/{id}/broadcasts")
    public Result<Void> broadcast(HttpServletRequest r, @PathVariable Long id, @Valid @RequestBody ActivityMessageRequest request) {
        activities.broadcast(uid(r), id, request);
        return Result.ok(null);
    }

    @GetMapping("/mine")
    @Operation(summary = "�ҵĻ")
    public Result<PageVO<ActivityVO>> mine(HttpServletRequest r, @RequestParam(defaultValue = "joined") String role) {
        return Result.ok(activities.mine(uid(r), role));
    }

    private Long uid(HttpServletRequest r) {
        return (Long) r.getAttribute(ApiConstants.USER_ID);
    }

    private Long uidOrNull(HttpServletRequest r) {
        return (Long) r.getAttribute(ApiConstants.USER_ID);
    }
}
