package com.zhuoyouquan.controller;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zhuoyouquan.common.ApiConstants;
import com.zhuoyouquan.common.BizException;
import com.zhuoyouquan.common.Result;
import com.zhuoyouquan.dto.ActivityReviewRequest;
import com.zhuoyouquan.entity.Activity;
import com.zhuoyouquan.entity.ActivityJoin;
import com.zhuoyouquan.entity.UserComment;
import com.zhuoyouquan.entity.UserCredit;
import com.zhuoyouquan.mapper.ActivityJoinMapper;
import com.zhuoyouquan.mapper.ActivityMapper;
import com.zhuoyouquan.mapper.UserCommentMapper;
import com.zhuoyouquan.mapper.UserCreditMapper;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.bind.annotation.*;

import java.time.LocalDateTime;
import java.util.List;

@RestController
@RequestMapping("/api/activities/{activityId}/reviews")
public class ActivityReviewController {
    private final ActivityMapper activities;
    private final ActivityJoinMapper joins;
    private final UserCommentMapper comments;
    private final UserCreditMapper credits;

    public ActivityReviewController(ActivityMapper activities, ActivityJoinMapper joins, UserCommentMapper comments, UserCreditMapper credits) {
        this.activities = activities;
        this.joins = joins;
        this.comments = comments;
        this.credits = credits;
    }

    @PostMapping
    @Transactional
    public Result<Void> review(HttpServletRequest request, @PathVariable Long activityId, @Valid @RequestBody ActivityReviewRequest body) {
        Long reviewerId = (Long) request.getAttribute(ApiConstants.USER_ID);
        Activity activity = activities.selectById(activityId);
        if (activity == null) throw new BizException("活动不存在");
        if (!"ENDED".equals(activity.getStatus()) && activity.getStartTime().isAfter(LocalDateTime.now()))
            throw new BizException("活动结束后才能评价");
        if (reviewerId.equals(body.getUserId())) throw new BizException("不能评价自己");
        if (!participated(activity, reviewerId) || !participated(activity, body.getUserId()))
            throw new BizException("仅可评价同场参与者");
        UserComment comment = comments.selectOne(new LambdaQueryWrapper<UserComment>().eq(UserComment::getActivityId, activityId)
                .eq(UserComment::getFromUserId, reviewerId).eq(UserComment::getToUserId, body.getUserId()));
        if (comment == null) {
            comment = new UserComment();
            comment.setActivityId(activityId);
            comment.setFromUserId(reviewerId);
            comment.setToUserId(body.getUserId());
            comment.setCreatedAt(LocalDateTime.now());
            comments.insert(comment);
        }
        comment.setPunctualScore(body.getPunctualScore());
        comment.setFriendlyScore(body.getFriendlyScore());
        comment.setSkillScore(body.getSkillScore());
        comment.setCommunicationScore(body.getCommunicationScore());
        comment.setContent(body.getContent() == null ? null : body.getContent().trim());
        comments.updateById(comment);
        refreshCredit(body.getUserId());
        return Result.ok(null);
    }

    private boolean participated(Activity activity, Long userId) {
        return activity.getOrganizerId().equals(userId) || joins.selectCount(new LambdaQueryWrapper<ActivityJoin>().eq(ActivityJoin::getActivityId, activity.getId()).eq(ActivityJoin::getUserId, userId).eq(ActivityJoin::getStatus, "JOINED")) > 0;
    }

    private void refreshCredit(Long userId) {
        List<UserComment> all = comments.selectList(new LambdaQueryWrapper<UserComment>().eq(UserComment::getToUserId, userId));
        double average = all.stream().mapToDouble(c -> (c.getPunctualScore() + c.getFriendlyScore() + c.getSkillScore() + c.getCommunicationScore()) / 4d).average().orElse(5d);
        int score = Math.max(60, Math.min(100, (int) Math.round(60 + average * 8)));
        UserCredit credit = credits.selectOne(new LambdaQueryWrapper<UserCredit>().eq(UserCredit::getUserId, userId));
        if (credit == null) {
            credit = new UserCredit();
            credit.setUserId(userId);
            credit.setScore(score);
            credit.setLevel(score >= 92 ? "优质玩家" : score >= 80 ? "可靠玩家" : "成长玩家");
            credit.setUpdatedAt(LocalDateTime.now());
            credits.insert(credit);
            return;
        }
        credit.setScore(score);
        credit.setLevel(score >= 92 ? "优质玩家" : score >= 80 ? "可靠玩家" : "成长玩家");
        credit.setUpdatedAt(LocalDateTime.now());
        credits.updateById(credit);
    }
}
