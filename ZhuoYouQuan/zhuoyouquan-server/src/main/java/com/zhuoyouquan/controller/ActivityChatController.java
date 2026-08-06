package com.zhuoyouquan.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zhuoyouquan.common.ApiConstants;
import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.common.Result;
import com.zhuoyouquan.dto.ChatRequest;
import com.zhuoyouquan.entity.Activity;
import com.zhuoyouquan.entity.ActivityChat;
import com.zhuoyouquan.entity.ActivityJoin;
import com.zhuoyouquan.mapper.ActivityChatMapper;
import com.zhuoyouquan.mapper.ActivityJoinMapper;
import com.zhuoyouquan.mapper.ActivityMapper;
import com.zhuoyouquan.service.impl.UserServiceImpl;
import com.zhuoyouquan.vo.ChatVO;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDateTime;
import java.util.List;

@RestController
@RequestMapping("/api/activities/{activityId}/chat")
public class ActivityChatController {
    private final ActivityChatMapper chats;
    private final ActivityMapper activities;
    private final ActivityJoinMapper joins;
    private final UserServiceImpl users;

    public ActivityChatController(ActivityChatMapper chats, ActivityMapper activities, ActivityJoinMapper joins, UserServiceImpl users) {
        this.chats = chats;
        this.activities = activities;
        this.joins = joins;
        this.users = users;
    }

    @GetMapping
    public Result<List<ChatVO>> list(HttpServletRequest r, @PathVariable Long activityId) {
        Long uid = (Long) r.getAttribute(ApiConstants.USER_ID);
        check(uid, activityId);
        return Result.ok(chats.selectList(new LambdaQueryWrapper<ActivityChat>().eq(ActivityChat::getActivityId, activityId).orderByAsc(ActivityChat::getCreatedAt)).stream().map(this::vo).toList());
    }

    @PostMapping
    public Result<Void> send(HttpServletRequest r, @PathVariable Long activityId, @Valid @RequestBody ChatRequest q) {
        Long uid = (Long) r.getAttribute(ApiConstants.USER_ID);
        check(uid, activityId);
        String content = q.getContent() == null ? "" : q.getContent().trim();
        if (content.isEmpty()) throw new BizException("消息内容不能为空");
        ActivityChat chat = new ActivityChat();
        chat.setActivityId(activityId);
        chat.setUserId(uid);
        if (q.getParentId() != null) {
            ActivityChat parent = chats.selectById(q.getParentId());
            if (parent == null || !activityId.equals(parent.getActivityId())) throw new BizException("回复的消息不存在");
            chat.setParentId(parent.getParentId() == null ? parent.getId() : parent.getParentId());
            chat.setReplyToUserId(parent.getUserId());
        }
        chat.setContent(content);
        chat.setCreatedAt(LocalDateTime.now());
        chats.insert(chat);
        return Result.ok(null);
    }

    private void check(Long uid, Long id) {
        Activity a = activities.selectById(id);
        if (a == null) throw new BizException("活动不存在");
        if (a.getOrganizerId().equals(uid)) return;
        boolean joined = joins.selectCount(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId, id).eq(ActivityJoin::getUserId, uid).eq(ActivityJoin::getStatus, "JOINED")) > 0;
        if (!joined) throw new BizException("报名后才能参与讨论");
    }

    private ChatVO vo(ActivityChat x) {
        ChatVO v = new ChatVO();
        v.setId(x.getId());
        v.setParentId(x.getParentId());
        v.setContent(x.getContent());
        v.setCreatedAt(x.getCreatedAt());
        v.setUser(users.getProfile(x.getUserId()));
        if (x.getReplyToUserId() != null) v.setReplyToUser(users.getProfile(x.getReplyToUserId()));
        return v;
    }
}
