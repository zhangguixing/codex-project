package com.zhuoyouquan.service;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.zhuoyouquan.entity.Activity;
import com.zhuoyouquan.entity.ActivityJoin;
import com.zhuoyouquan.entity.ActivityReminderLog;
import com.zhuoyouquan.entity.Message;
import com.zhuoyouquan.mapper.ActivityJoinMapper;
import com.zhuoyouquan.mapper.ActivityMapper;
import com.zhuoyouquan.mapper.ActivityReminderLogMapper;
import com.zhuoyouquan.mapper.MessageMapper;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.time.LocalDateTime;
import java.util.List;

@Component
public class ActivityReminderScheduler {
    private final ActivityMapper activities;
    private final ActivityJoinMapper joins;
    private final ActivityReminderLogMapper logs;
    private final MessageMapper messages;

    public ActivityReminderScheduler(ActivityMapper activities, ActivityJoinMapper joins, ActivityReminderLogMapper logs, MessageMapper messages) {
        this.activities = activities;
        this.joins = joins;
        this.logs = logs;
        this.messages = messages;
    }

    @Scheduled(cron = "0 0 * * * *")
    public void createUpcomingReminders() {
        LocalDateTime now = LocalDateTime.now();
        sendForWindow(now, now.plusHours(24), "ONE_DAY", "活动将在约 24 小时后开始", "提前确认路线和到店时间，准时赴约。");
        sendForWindow(now, now.plusHours(2), "TWO_HOURS", "活动将在约 2 小时后开始", "准备出发吧，别忘了带上需要的游戏和物品。");
    }

    private void sendForWindow(LocalDateTime from, LocalDateTime to, String type, String title, String suffix) {
        List<Activity> upcoming = activities.selectList(new LambdaQueryWrapper<Activity>()
                .in(Activity::getStatus, "OPEN", "FULL")
                .between(Activity::getStartTime, from, to));
        for (Activity activity : upcoming) {
            for (ActivityJoin join : joins.selectList(new LambdaQueryWrapper<ActivityJoin>()
                    .eq(ActivityJoin::getActivityId, activity.getId()).eq(ActivityJoin::getStatus, "JOINED"))) {
                long existing = logs.selectCount(new LambdaQueryWrapper<ActivityReminderLog>()
                        .eq(ActivityReminderLog::getActivityId, activity.getId())
                        .eq(ActivityReminderLog::getUserId, join.getUserId())
                        .eq(ActivityReminderLog::getReminderType, type));
                if (existing > 0) continue;
                ActivityReminderLog log = new ActivityReminderLog();
                log.setActivityId(activity.getId());
                log.setUserId(join.getUserId());
                log.setReminderType(type);
                log.setCreatedAt(LocalDateTime.now());
                logs.insert(log);
                Message message = new Message();
                message.setUserId(join.getUserId());
                message.setType("ACTIVITY_REMINDER");
                message.setTitle(title);
                message.setContent("《" + activity.getTitle() + "》" + suffix);
                message.setTargetId(activity.getId());
                message.setCreatedAt(LocalDateTime.now());
                messages.insert(message);
            }
        }
    }
}
